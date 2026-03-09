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

Then open ODOOM or OQuake, **beam in (log in) with the same username/password** you used for the seed, and press **Q** to view the quest popup.

**Re-run the seed after backend/DB changes:** If you updated the API or storage (e.g. STARNETDNA/MetaData fixes for quests), re-run the seed so new demo quests are created with the correct data. Re-running creates additional quests; beam in with the same user to see them.

**If you see “No active quests” or an error in the popup:** The games only load quests for the currently beamed-in avatar. Use the same WEB5 API URL and same credentials as the seed. If the quest API fails (e.g. not authenticated, wrong URL), the popup shows the error message; the same error is written to **star_api.log** (next to the game exe or star_api.dll) and to the game console (if it consumes `star_api_consume_console_log`).
