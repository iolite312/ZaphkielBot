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

## Architecture

```
Discord ──POST /api/discord/interactions──► DiscordInteraction (HTTP fn) ──► DiscordInteractionService
                                              verifies Ed25519 signature        │ queries answered directly (read side)
                                                                                │ commands enqueued, instant reply
                                                                                ▼
                                                              Storage Queue  incident-commands
                                                                                ▼
                                       ProcessIncidentCommand (queue fn) ──► mediator ──► CreateIncident / ResolveIncident handlers
                                                                                │            ├─ Azure Table Storage (Incidents, IncidentLookup)
                                                                                │            ├─ GitHub REST API (open / close issue)
                                                                                │            └─ Discord REST API (announcements)
GitHub ──POST /api/github/webhook──► GitHubWebhook (HTTP fn) ──► GitHubWebhookService
          verifies HMAC-SHA256        issue_comment created / issues closed
                                                                                ▼
                                                              Storage Queue  github-events
                                                                                ▼
                                       ProcessGitHubEvent (queue fn) ──► RecordGitHubComment / ResolveIncident handlers ──► Discord channel

GET /api/analytics?from=&to=   GET /api/incidents/{id}   (HTTP fns, function key)  ──► query handlers ──► Table Storage
```

| Project | Tier | Contents |
|---|---|---|
| `ZaphkielBot` | entry points | the 6 Azure Functions (thin: read request, call a service, map the result) |
| `ZaphkielBot.Core` | business tier | domain model + validation, CQRS commands/queries/handlers, mediator, interaction & webhook services, interfaces |
| `ZaphkielBot.Infrastructure` | data tier | Table Storage store, Storage Queue, GitHub/Discord HTTP clients, signature verifiers |
| `ZaphkielBot.Tests` | | xUnit tests using stubs/mocks only (no Azure, no network) |

### Rubric mapping

- **Cloud database architecture** – Table Storage. `Incidents`: PartitionKey = creation month, RowKey = reversed ticks + id
  (newest first). Analytics read one partition per month with a RowKey range, never a table scan. `IncidentLookup` is a
  secondary index (by incident id, spread over up to 256 partitions; by GitHub issue number) so webhooks can find an
  incident by point read. Stores `Severity`, `CreatedAt`, `ResolvedAt` for severity breakdowns and MTTR.
- **N-tier / Azure Functions** – see the table above; 6 functions (2 HTTP webhooks, 2 queue processors, 2 query endpoints).
- **Loosely coupled** – Core depends only on interfaces (`IIncidentReader/Writer`, `IGitHubIssueClient`, `IDiscordNotifier`,
  `ICommandQueue`, signature verifiers); every handler and service is unit tested with stubs and NSubstitute mocks.
- **CQRS** – `ICommand`/`IQuery` with separate handlers, a hand-written mediator (`Core/Cqrs`), separate read and write store
  interfaces, and commands travelling through Azure Storage Queues (one queue per producer so GitHub bursts never delay incident intake).

### Security

- No secrets in code or git: `.env` (git-ignored) locally, Application Settings / Key Vault references in Azure.
  Options are validated at startup, so a missing secret fails fast with a clear message.
- Both public endpoints are authenticated by signature, not by obscurity: Discord Ed25519 and GitHub HMAC-SHA256
  (constant-time compare, computed over the raw body bytes). Unsigned or tampered requests get 401 and queue nothing.
- Query endpoints require a function key.
- GitHub token: fine-grained PAT limited to one repo, "Issues: read/write" only.
- Relayed text cannot ping anyone: Discord messages send `allowed_mentions: {parse: []}`; `@` is neutralised in GitHub issue bodies.
- Slash commands are restricted to the designated channel (`Discord__IncidentChannelId`).

## Running it

1. **Discord**: create an application at <https://discord.com/developers/applications>, add a bot, invite it to your server
   (scopes `bot` + `applications.commands`, permission *Send Messages* in the incident channel).
2. **GitHub**: create a fine-grained PAT (one repo, Issues read/write).
3. Fill in `.env` (see `.env.example` for where each value comes from).
4. `docker compose up --build` – starts Azurite and the Functions app on <http://localhost:7071>.
5. `scripts/register-discord-commands.sh` – registers `/incident` in your server.
6. Discord and GitHub must reach your machine over public HTTPS, e.g. `cloudflared tunnel --url http://localhost:7071`
   (or a Dev Tunnel / ngrok). Then:
   - Discord → *General Information* → **Interactions Endpoint URL** = `https://<tunnel>/api/discord/interactions`
   - GitHub repo → *Settings → Webhooks*: payload URL `https://<tunnel>/api/github/webhook`, content type
     `application/json`, secret = `GitHub__WebhookSecret`, events: **Issues** and **Issue comments**.
7. Analytics: `GET /api/analytics?from=2026-10-01&to=2026-10-31` needs a function key (`?code=…`; in the local container the
   keys live in Azurite's blob storage – easiest is to try it after deploying to Azure).

Tests: `dotnet test`

## Known trade-offs (proof of concept)

- Queue delivery is at-least-once. Handlers are idempotent, except that a crash between "GitHub issue created" and
  "issue number saved" would open a duplicate issue on retry.
- Updates use last-writer-wins (`ETag.All`); a comment and a resolve racing on the same incident could in theory overwrite each other's fields.
- Messages that fail 5 times land in `<queue>-poison` (e.g. a bad GitHub token); nothing processes them automatically.
- The `IncidentLookup` GitHub-issue partition (`gh`) is a single partition – fine for one repository, would need sharding for many.
