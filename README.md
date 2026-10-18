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
