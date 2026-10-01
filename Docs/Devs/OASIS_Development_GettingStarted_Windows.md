# OASIS Development — Getting Started (Windows)

This guide gets you set up to **develop and build** the OASIS repository on **Windows**: clone, build the solution, run tests, and find your way around the codebase.

---

## What is OASIS?

OASIS is the universal Web4/Web5 infrastructure: WEB4 (data aggregation, identity, providers) and WEB5 STAR (gamification, metaverse, OAPPs). The repo includes APIs, STAR CLI, STAR ODK, providers, ONODE, and more. See the main [README](../../README.md) and [Developer Documentation Index](./DEVELOPER_DOCUMENTATION_INDEX.md).

---

## Prerequisites

| Tool | Purpose | Where to get it |
|------|----------|------------------|
| **Git** | Clone and version control | [git-scm.com](https://git-scm.com/download/win) |
| **.NET 8 SDK** | Build C# solutions (APIs, STAR CLI, providers) | [dotnet.microsoft.com/download/dotnet/8.0](https://dotnet.microsoft.com/download/dotnet/8.0) |
| **Visual Studio 2022** (optional) | IDE; use “.NET desktop development” and/or “ASP.NET and web” workloads | [Visual Studio](https://visualstudio.microsoft.com/) |
| **Node.js 18+** (optional) | For web UIs (e.g. STARNET Web UI, oasisweb4.com) | [nodejs.org](https://nodejs.org/) |

Command-line builds only need Git and the .NET 8 SDK.

---

## Clone the repository

```cmd
git clone https://github.com/NextGenSoftwareUK/OASIS.git
cd OASIS
```

Use the actual repo URL if you’re working from a fork or private clone.

---

## Build the solution

From the repo root:

```cmd
dotnet build The OASIS.sln
```

To build a smaller subset (faster):

- **Core only:** `dotnet build "The OASIS Core Only.sln"`
- **No tests:** `dotnet build "The OASIS - NoTests.sln"`
- **Minimal:** `dotnet build "The OASIS Minimal.sln"`

Or open `The OASIS.sln` in Visual Studio 2022 and build from the IDE (F6 or Build → Build Solution).

---

## Run tests (optional)

```cmd
dotnet test The OASIS.sln
```

Or run tests for a specific project, e.g. STAR CLI tests under `STAR ODK\`.

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

- **STAR CLI:** See [STAR CLI Getting Started (Windows)](./STAR_CLI_GettingStarted_Windows.md). From repo: `cd "STAR ODK\NextGenSoftware.OASIS.STAR.CLI"` then `dotnet run`.
- **OASIS Omniverse (ODOOM/OQuake):** See [OASIS Omniverse Docs](../../OASIS%20Omniverse/Docs/GettingStarted_Windows.md) for build and run.
- **WEB4/STAR APIs:** Run the relevant Web API project from Visual Studio or `dotnet run --project <path-to-.csproj>`.

---

## Next steps

- **[Developer Documentation Index](./DEVELOPER_DOCUMENTATION_INDEX.md)** — Full list of docs, APIs, tutorials.
- **[Development Environment Setup](./DEVELOPMENT_ENVIRONMENT_SETUP.md)** — Detailed environment and tooling.
- **[OASIS Quick Start Guide](./OASIS_Quick_Start_Guide.md)** — First steps with OASIS.
- **STAR CLI:** [STAR CLI Getting Started (Windows)](./STAR_CLI_GettingStarted_Windows.md) and [STAR CLI Quick Start](./STAR_CLI_QUICK_START_GUIDE.md).
- **OASIS Omniverse (games):** [Getting Started — Windows](../../OASIS%20Omniverse/Docs/GettingStarted_Windows.md).

---

## Troubleshooting

| Issue | What to do |
|-------|------------|
| **"dotnet" not recognized** | Install .NET 8 SDK and restart the terminal (or use “Developer Command Prompt for VS 2022”). |
| **Build fails (missing projects)** | Ensure you cloned the full repo; try `The OASIS Core Only.sln` or `The OASIS - NoTests.sln` if the full solution fails. |
| **NuGet restore errors** | Run `dotnet restore The OASIS.sln`. Check network/proxy if packages don’t restore. |
| **Long path errors** | Clone to a short path (e.g. `C:\Source\OASIS`) and ensure long path support is enabled if needed. |
