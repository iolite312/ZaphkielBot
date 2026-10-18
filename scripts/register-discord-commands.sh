#!/usr/bin/env bash
# Registers the /incident slash command in your Discord server (guild commands show up instantly).
# Reads Discord__BotToken, Discord__ApplicationId and Discord__GuildId from .env. Safe to re-run.
set -euo pipefail
cd "$(dirname "$0")/.."

set -a
# shellcheck disable=SC1091
source .env
set +a

: "${Discord__BotToken:?Set Discord__BotToken in .env}"
: "${Discord__ApplicationId:?Set Discord__ApplicationId in .env}"
: "${Discord__GuildId:?Set Discord__GuildId in .env}"

# PUT replaces all guild commands with exactly this list.
curl --fail-with-body -sS -X PUT \
  "https://discord.com/api/v10/applications/${Discord__ApplicationId}/guilds/${Discord__GuildId}/commands" \
  -H "Authorization: Bot ${Discord__BotToken}" \
  -H "Content-Type: application/json" \
  --data @- <<'JSON'
[
  {
    "name": "incident",
    "description": "Report and track incidents",
    "options": [
      {
        "type": 1, "name": "report", "description": "Report a new incident",
        "options": [
          { "type": 3, "name": "title", "description": "Short summary", "required": true, "min_length": 3, "max_length": 100 },
          { "type": 3, "name": "severity", "description": "How bad is it?", "required": true,
            "choices": [
              { "name": "SEV1 - Critical", "value": "critical" },
              { "name": "SEV2 - High", "value": "high" },
              { "name": "SEV3 - Medium", "value": "medium" },
              { "name": "SEV4 - Low", "value": "low" }
            ] },
          { "type": 3, "name": "description", "description": "What is happening?", "required": false, "max_length": 1500 }
        ]
      },
      {
        "type": 1, "name": "resolve", "description": "Mark an incident as resolved",
        "options": [ { "type": 3, "name": "id", "description": "Incident id, e.g. INC-a1b2c3d4", "required": true } ]
      },
      {
        "type": 1, "name": "status", "description": "Show an incident",
        "options": [ { "type": 3, "name": "id", "description": "Incident id, e.g. INC-a1b2c3d4", "required": true } ]
      },
      { "type": 1, "name": "stats", "description": "Incident counts and MTTR for the last 30 days" }
    ]
  }
]
JSON
echo
