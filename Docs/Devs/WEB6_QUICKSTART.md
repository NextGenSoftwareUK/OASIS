# WEB6 Quick-Start Guide

Get from zero to a running AI task in under 5 minutes.

---

## What you'll build

A single C# program that:
1. Registers an avatar (WEB4 identity)
2. Seeds a default FAHRN agent pool
3. Dispatches an AI reasoning task via FAHRN
4. Saves the result as a Holon in WEB4 storage

---

## Prerequisites

- .NET 8+ SDK
- A running ONODE WebAPI **or** a configured storage provider (MongoDB, SQLite, LocalFile)
- At least one AI provider API key in `OASIS_DNA.json` (OpenAI, Anthropic, Groq, etc.)

---

## 1. Configure `OASIS_DNA.json`

The minimum Web6 config block — add this inside your existing `OASIS_DNA.json`:

```json
{
  "OASIS": {
    "StorageProviders": {
      "MongoDBOASIS": {
        "ConnectionString": "mongodb://localhost:27017",
        "DBName": "OASIS"
      }
    },
    "Web6": {
      "FAHRN": {
        "EMAAlpha": 0.2
      },
      "AIProviders": [
        {
          "ProviderType": "OpenAI",
          "APIKey": "sk-...",
          "DefaultModel": "gpt-4o-mini"
        }
      ]
    }
  }
}
```

---

## 2. Add NuGet references

```xml
<PackageReference Include="NextGenSoftware.OASIS.API.Core" Version="*" />
<PackageReference Include="NextGenSoftware.OASIS.Web6.Core" Version="*" />
<PackageReference Include="NextGenSoftware.OASIS.API.Providers.MongoDBOASIS" Version="*" />
```

---

## 3. The complete example

```csharp
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS;
using NextGenSoftware.OASIS.Web6.Core.Enums;
using NextGenSoftware.OASIS.Web6.Core.Managers;
using NextGenSoftware.OASIS.Web6.Core.Models;

// ── Step 1: Boot OASIS ──────────────────────────────────────────────────────
var provider = new MongoDBOASIS("mongodb://localhost:27017", "OASIS");
await provider.ActivateProviderAsync();
ProviderManager.Instance.SetAndActivateCurrentStorageProvider(provider);

// ── Step 2: Register an avatar ─────────────────────────────────────────────
var avatarManager = new AvatarManager(provider);
var registerResult = await avatarManager.RegisterAsync(
    title: "Mr",
    firstName: "Demo",
    lastName: "User",
    email: "demo@example.com",
    password: "SecurePass123!",
    username: "demouser"
);

if (registerResult.IsError)
{
    Console.WriteLine($"Register failed: {registerResult.Message}");
    return;
}

var avatarId = registerResult.Result.Id;
Console.WriteLine($"Avatar registered: {avatarId}");

// ── Step 3: Seed default FAHRN agent pool ──────────────────────────────────
// Seeds a balanced set of OpenAI + other configured providers as reasoning agents.
var fahrn = new FAHRNManager(avatarId);
var seedResult = await fahrn.SeedDefaultOpenServAgentsAsync();

if (seedResult.IsError)
{
    Console.WriteLine($"Agent seeding failed: {seedResult.Message}");
    return;
}

Console.WriteLine($"Seeded {seedResult.Result.Count} reasoning agent(s).");

// ── Step 4: Dispatch an AI task via FAHRN ──────────────────────────────────
var dispatch = await fahrn.DispatchAsync(new DispatchRequest
{
    Problem    = "Explain in two sentences why holonic data models are useful for cross-world identity.",
    TaskType   = "general",
    Mode       = DispatchMode.Serial,   // Serial = best agent first; Parallel = all at once; Decomposed = subtasks
    AvatarId   = avatarId,
    MaxCostUsd = 0.05m                  // Hard budget ceiling
});

if (dispatch.IsError)
{
    Console.WriteLine($"Dispatch failed: {dispatch.Message}");
    return;
}

Console.WriteLine($"\nFAHRN answer:\n{dispatch.Result.FinalAnswer}");
Console.WriteLine($"Agent used: {dispatch.Result.WinningAgentName}");
Console.WriteLine($"Cost: ${dispatch.Result.TotalCostUsd:F4}  Tokens: {dispatch.Result.TotalTokens}");

// ── Step 5: Save the result as a Holon ────────────────────────────────────
var holon = new Holon(HolonType.Task)
{
    Name        = "WEB6 Quick-Start Result",
    Description = dispatch.Result.FinalAnswer
};
holon.MetaData["WinningAgent"] = dispatch.Result.WinningAgentName;
holon.MetaData["CostUsd"]      = dispatch.Result.TotalCostUsd.ToString("F4");

var saveResult = await Data.SaveHolonAsync(holon, avatarId);

if (saveResult.IsError)
    Console.WriteLine($"Save failed: {saveResult.Message}");
else
    Console.WriteLine($"\nResult saved as Holon: {saveResult.Result.Id}");
```

---

## Dispatch modes explained

| Mode | When to use |
|---|---|
| `Serial` | Default. Best-scoring agent answers; others used as fallback if it fails. |
| `Parallel` | All eligible agents answer simultaneously; fastest wins. |
| `Decomposed` | Problem is split into subtasks; each sent to the most suitable agent. |
| `Debate` | Agents argue; FAHRN arbitrates. Good for controversial or multi-perspective problems. |
| `Voting` | Agents vote on the best answer; majority/weighted/unanimous strategies available. |

---

## Using the REST API instead

If you're calling the deployed ONODE endpoint rather than embedding the SDK:

```bash
# Register
curl -X POST https://api.web4.oasisomniverse.one/api/avatar/register \
  -H "Content-Type: application/json" \
  -d '{"firstName":"Demo","lastName":"User","email":"demo@example.com","password":"SecurePass123!","username":"demouser"}'

# Authenticate and get JWT
curl -X POST https://api.web4.oasisomniverse.one/api/avatar/authenticate \
  -H "Content-Type: application/json" \
  -d '{"username":"demouser","password":"SecurePass123!"}'

# Dispatch a FAHRN task (WEB6 WebAPI)
curl -X POST https://api.web6.oasisomniverse.one/v1/fahrn/solve \
  -H "Authorization: Bearer <your-jwt>" \
  -H "Content-Type: application/json" \
  -d '{"problem":"Explain holonic data models in two sentences.","taskType":"general","dispatchMode":"serial","maxCostUsd":0.05}'
```

---

## Next steps

| Topic | Doc |
|---|---|
| Full FAHRN reference (modes, scoring, BRAID) | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/FAHRNManager.cs` |
| SkillOpt — self-improving agent skills | `WEB6/NextGenSoftware.OASIS.Web6.Core/Managers/SkillOptManager.cs` |
| Holonic Memory (fractal Session→Earth) | `WEB6/NextGenSoftware.OASIS.Web6.Core/Memory/HolonicMemoryManager.cs` |
| WEB4 Holon CRUD | `Docs/Devs/API Documentation/WEB5 STAR API/Holons-API.md` |
| OASISDNA reference | `Docs/Devs/DNA_SYSTEM_GUIDE.md` |
| Try it live (no install) | `https://sandbox.oasisomniverse.one` |
