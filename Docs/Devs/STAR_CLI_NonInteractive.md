# STAR CLI: non-interactive & JSON shell mode

Run STAR from scripts and CI without menus or stdin prompts.

## Global flags (any position on the command line)

| Flag | Short | Purpose |
|------|-------|---------|
| `--non-interactive` | `-n` | No stdin prompts; wizards blocked; missing args → errors |
| `--json` | | Machine-readable JSON on stdout for some commands; implies quieter startup |
| `--quiet` | `-q` | Minimal banner (also set automatically with `-n` or `--json`) |
| `--yes` | `-y` | In non-interactive mode, `GetConfirmation` behaves as “yes” (use carefully for destructive flows) |
| `--username` | | Beam-in username (avoid in shared logs; prefer env vars) |
| `--password` | | Beam-in password |

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | General error / unknown command |
| `2` | Usage / missing credentials or subcommand in non-interactive mode |
| `3` | Interactive input was required but disabled (`CLIEngineNonInteractiveInputRequiredException`) |

## Beam-in (authentication)

Most commands need a beamed-in avatar.

### Commands that **skip** beam-in

No credentials required: `help`, `version`, `status`, `dna`, `ignite`, `extinguish`, `exit`.

### Otherwise

Set **environment variables** (recommended for CI):

- `STAR_CLI_USERNAME`
- `STAR_CLI_PASSWORD`

Optional if the API reports an unverified avatar:

- `STAR_CLI_EMAIL_VERIFY_TOKEN`

Or pass on the command line (less secure):

```bash
star --non-interactive --username MYUSER --password '***' oapp list
```

Or prefix the command with an **avatar beamin** sequence (credentials are stripped before the rest runs):

```bash
star --non-interactive avatar beamin MYUSER '***' oapp list
```

Beam-in **only** (no further command):

```bash
export STAR_CLI_USERNAME=...
export STAR_CLI_PASSWORD=...
star --non-interactive
```

## Examples

```bash
# Versions as JSON (no login)
star --json version

# Status
star -n status

# List OAPPs (requires credentials)
STAR_CLI_USERNAME=u STAR_CLI_PASSWORD=p star --non-interactive oapp list

# JSON + non-interactive
star --non-interactive --json version
```

## Interactive-only commands

These refuse `--non-interactive`: `bang`, `wiz`, and `light` without full positional arguments. Use explicit `light …` args or interactive mode.

## Subcommands

If you run only `oapp`, `holon`, etc. with **no** subcommand, interactive mode shows a help menu. In `--non-interactive` mode you get exit code **2** and an error: you must pass a full subcommand (e.g. `oapp list`, `holon show <id>`).

## Engine behaviour (`NextGenSoftware.CLI.Engine`)

When `CLIEngine.NonInteractive` is true:

- `GetValidInput`, `ReadPassword`, enum/guid prompts, etc. throw `CLIEngineNonInteractiveInputRequiredException` instead of reading stdin.
- `GetConfirmation` returns `false` unless `CLIEngine.AssumeYes` is true (`--yes`).

STAR CLI sets these from the parsed invocation at startup.

## Further work

Many STARNET flows still call `GetConfirmation` / wizards inside the CLI library. For full script coverage, each flow should accept **explicit IDs/names and flags** on the command line; non-interactive mode will then either succeed or fail with a clear error instead of hanging. Extend commands incrementally as needed.
