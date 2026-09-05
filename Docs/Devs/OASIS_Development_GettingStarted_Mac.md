# OASIS Development — Getting Started (macOS)

This guide gets you set up to **develop and build** the OASIS repository on **macOS** (Intel or Apple Silicon): clone, build the solution, run tests, and find your way around the codebase.

---

## What is OASIS?

OASIS is the universal Web4/Web5 infrastructure: WEB4 (data aggregation, identity, providers) and WEB5 STAR (gamification, metaverse, OAPPs). The repo includes APIs, STAR CLI, STAR ODK, providers, ONODE, and more. See the main [README](../../README.md) and [Developer Documentation Index](./DEVELOPER_DOCUMENTATION_INDEX.md).

---

## Prerequisites

| Tool | Purpose | Install (examples) |
|------|----------|--------------------|
| **Git** | Clone and version control | Xcode Command Line Tools or `brew install git` |
| **.NET 8 SDK** | Build C# solutions | [Install .NET on macOS](https://learn.microsoft.com/en-us/dotnet/core/install/macos). Homebrew: `brew install dotnet@8` |
| **Node.js 18+** (optional) | For web UIs (STARNET Web UI, oasisweb4.com) | `brew install node` or nvm |

Command-line builds only need Git and the .NET 8 SDK.

---

## Clone the repository

```bash
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS
```

Use the actual repo URL if you’re working from a fork or private clone.

---

## Build the solution

From the repo root:

```bash
dotnet build "The OASIS.sln"
```

To build a smaller subset (faster):

- **Core only:** `dotnet build "The OASIS Core Only.sln"`
- **No tests:** `dotnet build "The OASIS - NoTests.sln"`
- **Minimal:** `dotnet build "The OASIS Minimal.sln"`

On Apple Silicon, .NET runs natively (arm64); the solutions build without extra configuration.

---

## Run tests (optional)

```bash
dotnet test "The OASIS.sln"
```

Or run tests for a specific project under `STAR ODK/` or `Providers/`.

---

## Project structure (high level)

| Path | Contents |
|------|----------|
| **ONODE/** | WEB4 OASIS API (Web API, core) |
| **STAR ODK/** | STAR CLI, STAR CLI Lib, STAR Web API, STAR Web UI, STAR core |
| **OASIS Architecture/** | Core libraries (OASIS.API.Core, Common, etc.) |
| **Providers/** | Blockchain, cloud, storage, network providers |
| **OASIS Omniverse/** | ODOOM, OQuake, STARAPIClient, game integrations |
| **Docs/** | Documentation; **Docs/Devs/** for developer guides |

Solutions at repo root: `The OASIS.sln`, `The OASIS Core Only.sln`, `The OASIS - NoTests.sln`, `The OASIS Minimal.sln`.

---

## Run key projects (examples)

- **STAR CLI:** See [STAR CLI Getting Started (Mac)](./STAR_CLI_GettingStarted_Mac.md). From repo: `cd "STAR ODK/NextGenSoftware.OASIS.STAR.CLI"` then `dotnet run`.
- **OASIS Omniverse (ODOOM/OQuake):** See [OASIS Omniverse — macOS](../../OASIS%20Omniverse/Docs/GettingStarted_Mac.md) for build and run.
- **WEB4/STAR APIs:** `dotnet run --project <path-to-.csproj>` from the repo root.

---

## Next steps

- **[Developer Documentation Index](./DEVELOPER_DOCUMENTATION_INDEX.md)** — Full list of docs, APIs, tutorials.
- **[Development Environment Setup](./DEVELOPMENT_ENVIRONMENT_SETUP.md)** — Detailed environment and tooling.
- **[OASIS Quick Start Guide](./OASIS_Quick_Start_Guide.md)** — First steps with OASIS.
- **STAR CLI:** [STAR CLI Getting Started (Mac)](./STAR_CLI_GettingStarted_Mac.md) and [STAR CLI Quick Start](./STAR_CLI_QUICK_START_GUIDE.md).
- **OASIS Omniverse (games):** [Getting Started — macOS](../../OASIS%20Omniverse/Docs/GettingStarted_Mac.md).

---

## Troubleshooting

| Issue | What to do |
|-------|------------|
| **"dotnet" not found** | Install .NET 8 SDK (`brew install dotnet@8`) and restart Terminal. |
| **Build fails (missing projects)** | Ensure you cloned the full repo; try `The OASIS Core Only.sln` or `The OASIS - NoTests.sln`. |
| **NuGet restore errors** | Run `dotnet restore "The OASIS.sln"`. Check network/proxy if packages don’t restore. |
| **Apple Silicon** | .NET 8 supports arm64; no extra steps needed for building. |
