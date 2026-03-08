# Demo Quest Seed

Seeds the STAR API with demo quests and objectives so you can test the quest UI in **ODOOM** and **OQuake** (press **Q** to open the quest popup).

## Prerequisites

- STAR API (WEB5) and OASIS API (WEB4) running (e.g. STAR ODK + ONODE, or your deployed URLs).
- Valid avatar credentials.

## Usage

Use the same environment variables as the Test Harness:

| Variable | Description | Default |
|----------|-------------|---------|
| `STARAPI_WEB5_BASE_URL` | STAR API base URL | `http://localhost:5556` |
| `STARAPI_WEB4_BASE_URL` | OASIS API base URL (auth) | `http://localhost:5555` |
| `STARAPI_USERNAME` | Avatar username | `dellams` |
| `STARAPI_PASSWORD` | Avatar password | `test!` |

Run from the solution directory:

```bash
cd "OASIS Omniverse/STARAPIClient"
dotnet run --project TestProjects/DemoQuestSeed/DemoQuestSeed.csproj
```

Or from the repo root:

```bash
dotnet run --project "OASIS Omniverse/STARAPIClient/TestProjects/DemoQuestSeed/DemoQuestSeed.csproj"
```

The script will:

1. Authenticate with the STAR/OASIS APIs.
2. Create three demo quests with objectives for Doom and Quake.
3. Start the first quest so it appears as “In Progress” in the games.

Then open ODOOM or OQuake, log in with the same avatar, and press **Q** to view the quest popup.
