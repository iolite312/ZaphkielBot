# ZaphkielBot – Discord incident management

Engineers report incidents with a Discord slash command. Each incident is stored in Azure Table Storage, a tracking
issue is opened on GitHub, and developer comments on that issue are relayed back into the Discord channel.
(Assignment 3. A Discord bot is used instead of Slack, with approval.)

```
/incident report title:… severity:… [description:…]   -> logs the incident + opens a GitHub issue
/incident resolve id:INC-a1b2c3d4                      -> resolves it (stores time to resolve) + closes the issue
/incident status  id:INC-a1b2c3d4                      -> shows one incident
/incident stats                                        -> counts per severity and MTTR, last 30 days
```

## Projects

| Project | Role | Contents |
|---|---|---|
| `ZaphkielShared` | models | domain (`Incident`, `Severity`…), command/query/view records, GitHub models, options. No dependencies. |
| `ZaphkielCore` | abstractions + behaviour | every interface (repositories, queue, GitHub/Discord clients, serializer…), the mediator, the queue dispatcher, the shared query handlers |
| `ZaphkielData` | data tier | Table Storage repositories (read and write), entities, mappers, queue publisher, queue message serializer, storage initializer |
| `ZaphkielBot` | Azure Functions app – Discord | `DiscordInteraction` (HTTP) and `SendDiscordMessage` (queue) functions, the Discord.Net slash command module, the business logic behind it |
| `ZaphkielProcessor` | Azure Functions app – processing | `ProcessIncidentCommand` + `ProcessGitHubEvent` (queue), `GitHubWebhook` (HTTP), `GetAnalytics` + `GetIncident` (HTTP), the command handlers, GitHub client |
| `ZaphkielTests` | | xUnit tests, only stubs/mocks (no Azure, no network) |

References: Shared ← Core ← Data ← {Bot, Processor}. Everything talks to everything else through interfaces from Core.

## Flow

```
Discord ──POST /api/discord/interactions──► [Bot] DiscordInteraction ──► Discord.Net (Ed25519 check, routing) ──► IncidentModule
                                                                                    │ reads: mediator ► query handlers ► Table Storage
                                                                                    │ writes: validated, queued, instant reply
                                                                                    ▼
                                                                  queue  incident-commands
                                                                                    ▼
                  [Processor] ProcessIncidentCommand ──► mediator ──► CreateIncident / ResolveIncident handlers
                                                                         ├─ Table Storage (Incidents, IncidentLookup)
                                                                         ├─ GitHub REST API (open / close issue)
                                                                         └─ IDiscordNotifier ──► queue discord-notifications ─┐
GitHub ──POST /api/github/webhook──► [Processor] GitHubWebhook (HMAC-SHA256 check)                                           │
          issue_comment created / issues closed ──► queue github-events                                                       │
                  [Processor] ProcessGitHubEvent ──► RecordGitHubComment / ResolveIncident handlers ──► IDiscordNotifier ─────┤
                                                                                                                              ▼
                                            [Bot] SendDiscordMessage ──► SendDiscordMessageHandler ──► Discord.Net ──► the incident's channel
```

The processor never talks to Discord: it asks for messages through the `discord-notifications` queue, so a Discord outage
never fails incident processing, and the Discord token only exists in the bot.

### Rubric mapping

- **Cloud database architecture** – Table Storage. `Incidents`: PartitionKey = creation month, RowKey = reversed ticks + id
  (newest first). Analytics read one partition per month with a RowKey range, never a table scan. `IncidentLookup` is a
  secondary index (by incident id, spread over up to 256 partitions; by GitHub issue number) so webhooks find an incident
  with point reads. `Severity`, `CreatedAt`, `ResolvedAt` are stored for severity breakdowns and MTTR (`ZaphkielData/Entities/IncidentKeys.cs`).
- **N-tier / Azure Functions** – data tier (`ZaphkielData`), business tier (`ZaphkielCore` + handlers/services in the apps),
  entry points are thin Functions: 7 in total across two Function apps.
- **Loosely coupled / testable** – every dependency is an interface; handlers and services are unit tested with stubs and
  NSubstitute mocks, and the Discord pipeline is tested end-to-end with real signed requests but no network.
- **CQRS** – `ICommand`/`IQuery` with separate handlers, a hand-written mediator (`ZaphkielCore/Cqrs`), separate read and
  write repositories, and commands travelling through three Azure Storage Queues.

### Security

- No secrets in code or git: `.env` (git-ignored) locally, Application Settings / Key Vault references in Azure. Options are
  validated at startup, so a missing secret fails fast with a clear message.
- Least privilege: compose gives the bot only Discord settings and the processor only GitHub settings.
- Both public endpoints authenticate by signature: Discord Ed25519 (checked by Discord.Net) and GitHub HMAC-SHA256
  (constant-time compare over the raw body). Unsigned or tampered requests get 401 and queue nothing.
- Query endpoints require a function key.
- GitHub token: fine-grained PAT limited to one repo, "Issues: read/write" only.
- Relayed text cannot ping anyone: every Discord message is sent with empty allowed mentions; `@` is neutralised in GitHub issue bodies.
- Slash commands are restricted to the designated channel (`Discord__IncidentChannelId`).

## Running it

1. **Discord**: create an application at <https://discord.com/developers/applications>, add a bot, invite it to your server
   (scopes `bot` + `applications.commands`, permission *Send Messages* in the incident channel).
2. **GitHub**: create a fine-grained PAT (one repo, Issues read/write).
3. Fill in `.env` (see `.env.example` for where each value comes from).
4. `docker compose up --build` – starts Azurite, the bot on <http://localhost:7071> and the processor on <http://localhost:7072>.
   The bot registers the `/incident` commands in your server by itself on startup (set `Discord__GuildId` for instant registration).
5. Discord and GitHub must reach your machine over public HTTPS, e.g. `cloudflared tunnel --url http://localhost:7071`
   (and a second tunnel for 7072), or Dev Tunnels / ngrok. Then:
   - Discord → *General Information* → **Interactions Endpoint URL** = `https://<bot-tunnel>/api/discord/interactions`
   - GitHub repo → *Settings → Webhooks*: payload URL `https://<processor-tunnel>/api/github/webhook`, content type
     `application/json`, secret = `GitHub__WebhookSecret`, events: **Issues** and **Issue comments**.
6. Analytics: `GET /api/analytics?from=2026-10-01&to=2026-10-31` on the processor needs a function key (`?code=…`).

Tests: `dotnet test`

## Known trade-offs (proof of concept)

- Queue delivery is at-least-once. Handlers are idempotent, except that a crash between "GitHub issue created" and
  "issue number saved" would open a duplicate issue on retry.
- Updates use last-writer-wins (`ETag.All`); a comment and a resolve racing on the same incident could in theory overwrite each other's fields.
- Messages that fail 5 times land in `<queue>-poison` (e.g. a bad token); nothing processes them automatically.
- Discord.Net refuses interactions older than 3 seconds (Discord's own deadline), so a cold start that slow shows "application did not respond".
- The `IncidentLookup` GitHub-issue partition (`gh`) is a single partition – fine for one repository, would need sharding for many.
