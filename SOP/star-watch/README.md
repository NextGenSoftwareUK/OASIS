# STAR Watch

> Background SOP intelligence for your existing tools. No new app required.

STAR Watch is a lightweight CLI daemon that observes activity in Slack, Salesforce, GitHub, and other tools your team already uses. When it recognises a SOP step being completed, it records the proof automatically. When it needs a human decision, it asks — right where they already are.

---

## Install

```bash
npm install -g @oasis/star-watch
```

## Quick start

```bash
# 1. Authenticate with OASIS
star connect

# 2. Connect your first integration
star add slack

# 3. Start watching
star watch
```

## Commands

| Command | Description |
|---------|-------------|
| `star connect` | Authenticate with OASIS and configure your org |
| `star add <connector>` | Connect an integration: `slack`, `salesforce`, `github`, `email`, `jira` |
| `star watch` | Start the daemon (foreground) |
| `star watch --daemon` | Start as a background process |
| `star link <source> <id> --avatar <email>` | Link a tool user to an OASIS Avatar |
| `star status` | Show connected integrations and active SOP runs |

## How it works

1. **Watchers** observe activity across your connected tools and normalise events into a common schema.
2. **Matcher** checks observed events against active SOP runs using rule-based triggers first, then BRAID semantic matching.
3. **Action engine** auto-completes steps when confidence is high, or escalates to the assigned avatar via Slack DM.
4. **Proof holons** are written to STARNET for every completed step — auto or human.

## Config

Config is stored at `~/.star/config.json`. Avatar links at `~/.star/links.json`. Logs at `~/.star/logs/`.

## Privacy

- No message content is stored. Only event metadata is sent to STAR API.
- Watchers only activate for channels/objects you explicitly configure.
- Can run fully on-premise against a self-hosted STAR API instance.
