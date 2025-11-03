# OASIS Web4 Unity SDK

🎮 **Unity SDK for the OASIS Web4 API** - Connect your Unity games and applications to the decentralized OASIS ecosystem!

## 🌟 Features

- **Complete Avatar Management** - Authentication, registration, profiles, karma
- **Data Storage** - Holons, Zomes, and all OASIS data objects
- **Provider Integration** - Connect to 80+ blockchains and storage providers
- **Async/Await Support** - Modern C# async patterns with UniTask
- **Type-Safe** - Full C# type definitions for all OASIS entities
- **Unity Editor Integration** - Inspector-friendly components and ScriptableObjects

## 📦 Installation

### Option 1: Unity Package Manager (Recommended)

1. Open Unity Package Manager (Window > Package Manager)
2. Click the '+' button and select "Add package from git URL"
3. Enter: `https://github.com/NextGenSoftwareUK/OASIS-Unity-SDK.git`

### Option 2: Manual Installation

1. Download the latest release from the [Releases page](https://github.com/NextGenSoftwareUK/OASIS-Unity-SDK/releases)
2. Extract the package to your Unity project's `Assets` folder

## 🚀 Quick Start

### 1. Setup OASIS Manager

```csharp
using NextGenSoftware.OASIS.API.Unity;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private OASISClient oasisClient;

    async void Start()
    {
        // Initialize OASIS client
        oasisClient = new OASISClient(new OASISConfig
        {
            BaseUrl = "https://api.oasis.earth/api/v1",
            ApiKey = "your-api-key"
        });

        // Test connection
        var healthResult = await oasisClient.GetHealthAsync();
        Debug.Log($"OASIS Status: {healthResult.Status}");
    }
}
```

### 2. Avatar Authentication

```csharp
using NextGenSoftware.OASIS.API.Unity;
using UnityEngine;

public class LoginManager : MonoBehaviour
{
    private OASISClient oasisClient;

    public async void LoginAvatar(string email, string password)
    {
        var authResult = await oasisClient.Avatar.AuthenticateAsync(
            email, 
            password,
            GetDeviceInfo()
        );

        if (!authResult.IsError && authResult.Result != null)
        {
            Debug.Log($"Welcome {authResult.Result.Username}!");
            PlayerPrefs.SetString("OASISToken", authResult.Result.JwtToken);
        }
        else
        {
            Debug.LogError($"Login failed: {authResult.Message}");
        }
    }

    private string GetDeviceInfo()
    {
        return $"{SystemInfo.deviceType} - {SystemInfo.deviceModel}";
    }
}
```

### 3. Working with Holons (Data Objects)

```csharp
using NextGenSoftware.OASIS.API.Unity;
using System.Collections.Generic;

public class InventoryManager : MonoBehaviour
{
    private OASISClient oasisClient;

    public async void SavePlayerInventory(Dictionary<string, object> inventory)
    {
        var holon = new CreateHolonRequest
        {
            Name = "PlayerInventory",
            Description = "Player's game inventory",
            HolonType = "Inventory",
            MetaData = inventory
        };

        var result = await oasisClient.Data.CreateHolonAsync(holon);
        
        if (!result.IsError)
        {
            Debug.Log($"Inventory saved! Holon ID: {result.Result.Id}");
        }
    }

    public async void LoadPlayerInventory()
    {
        var result = await oasisClient.Data.GetHolonsByTypeAsync("Inventory");
        
        if (!result.IsError && result.Result.Count > 0)
        {
            var inventory = result.Result[0].MetaData;
            Debug.Log($"Inventory loaded: {inventory.Count} items");
        }
    }
}
```

### 4. Karma System Integration

```csharp
using NextGenSoftware.OASIS.API.Unity;

public class KarmaManager : MonoBehaviour
{
    private OASISClient oasisClient;

    public async void AddKarma(string reason, int amount)
    {
        var karmaRequest = new AddKarmaRequest
        {
            KarmaType = "Positive",
            KarmaSourceType = "Game",
            KarmaSourceTitle = "Unity Game",
            KarmaSourceDesc = reason,
            Amount = amount
        };

        var result = await oasisClient.Avatar.AddKarmaAsync(
            GetCurrentAvatarId(), 
            karmaRequest
        );

        if (!result.IsError)
        {
            Debug.Log($"Karma added! New total: {result.Result.TotalKarma}");
        }
    }
}
```

## 🎯 Core Components

### OASISClient

Main client for all OASIS operations:

```csharp
public class OASISClient
{
    public AvatarAPI Avatar { get; }
    public DataAPI Data { get; }
    public SearchAPI Search { get; }
    public ProviderAPI Providers { get; }
    
    public OASISClient(OASISConfig config);
    public Task<HealthCheckResponse> GetHealthAsync();
}
```

### Unity-Specific Features

#### OASISManager Component

Attach to a GameObject for scene-wide OASIS access:

```csharp
public class OASISManager : MonoBehaviour
{
    public static OASISManager Instance { get; private set; }
    public OASISClient Client { get; private set; }
    
    [SerializeField] private string apiUrl = "https://api.oasis.earth/api/v1";
    [SerializeField] private string apiKey;
}
```

#### OASISConfig ScriptableObject

Create reusable configuration assets:

```csharp
[CreateAssetMenu(fileName = "OASISConfig", menuName = "OASIS/Configuration")]
public class OASISConfigAsset : ScriptableObject
{
    public string apiUrl;
    public string apiKey;
    public bool useTestnet;
    public ProviderType defaultProvider;
}
```

## 📚 API Reference

### Avatar Operations

- `AuthenticateAsync(email, password, deviceInfo)` - Login avatar
- `RegisterAsync(avatarData)` - Create new avatar
- `GetAvatarByIdAsync(avatarId)` - Load avatar details
- `UpdateAvatarAsync(avatarId, updates)` - Update avatar
- `AddKarmaAsync(avatarId, karmaRequest)` - Add karma
- `RemoveKarmaAsync(avatarId, karmaRequest)` - Remove karma

### Data Operations

- `CreateHolonAsync(holonData)` - Create data object
- `GetHolonAsync(holonId)` - Load data object
- `UpdateHolonAsync(holonId, updates)` - Update data object
- `DeleteHolonAsync(holonId)` - Delete data object
- `GetHolonsByTypeAsync(holonType)` - Query by type

### Provider Operations

- `GetProvidersAsync()` - List all providers
- `GetProviderStatusAsync(providerType)` - Check provider status
- `SetAutoFailoverAsync(enabled)` - Configure failover
- `SetAutoReplicationAsync(enabled)` - Configure replication

## 🎮 Unity-Specific Examples

### Multiplayer Avatar Sync

```csharp
using UnityEngine;
using Mirror; // or Photon, Netcode, etc.

public class NetworkedAvatar : NetworkBehaviour
{
    [SyncVar] private string oasisAvatarId;
    private OASISClient oasisClient;

    public override async void OnStartLocalPlayer()
    {
        // Load OASIS avatar data
        var result = await oasisClient.Avatar.GetAvatarByIdAsync(oasisAvatarId);
        
        if (!result.IsError)
        {
            // Apply OASIS avatar properties to game character
            ApplyAvatarData(result.Result);
        }
    }
}
```

### Persistent World State

```csharp
public class WorldStateManager : MonoBehaviour
{
    private OASISClient oasisClient;

    public async void SaveWorldState()
    {
        var worldData = new CreateHolonRequest
        {
            Name = "WorldState",
            HolonType = "GameWorld",
            MetaData = new Dictionary<string, object>
            {
                ["timestamp"] = DateTime.UtcNow,
                ["playerCount"] = GameObject.FindGameObjectsWithTag("Player").Length,
                ["gameTime"] = Time.time
            }
        };

        await oasisClient.Data.CreateHolonAsync(worldData);
    }
}
```

### Achievement System

```csharp
public class AchievementManager : MonoBehaviour
{
    private OASISClient oasisClient;

    public async void UnlockAchievement(string achievementId)
    {
        // Create achievement holon
        var achievement = new CreateHolonRequest
        {
            Name = achievementId,
            HolonType = "Achievement",
            MetaData = new Dictionary<string, object>
            {
                ["unlockedAt"] = DateTime.UtcNow,
                ["avatarId"] = GetCurrentAvatarId()
            }
        };

        var result = await oasisClient.Data.CreateHolonAsync(achievement);

        // Award karma
        if (!result.IsError)
        {
            await oasisClient.Avatar.AddKarmaAsync(
                GetCurrentAvatarId(),
                new AddKarmaRequest
                {
                    Amount = 100,
                    KarmaSourceTitle = $"Achievement: {achievementId}"
                }
            );
        }
    }
}
```

## 🔧 Advanced Configuration

### Custom Retry Logic

```csharp
var config = new OASISConfig
{
    BaseUrl = "https://api.oasis.earth/api/v1",
    ApiKey = "your-api-key",
    RetryAttempts = 3,
    RetryDelayMs = 1000,
    TimeoutSeconds = 30
};
```

### Provider Selection

```csharp
// Use specific provider
var result = await oasisClient.Data.CreateHolonAsync(
    holonData,
    ProviderType.HoloOASIS
);

// Auto-failover across providers
var config = new OASISConfig
{
    AutoFailover = true,
    PreferredProviders = new List<ProviderType>
    {
        ProviderType.IPFSOASIS,
        ProviderType.HoloOASIS,
        ProviderType.MongoDBOASIS
    }
};
```

## 📱 Platform Support

- ✅ Windows
- ✅ macOS
- ✅ Linux
- ✅ iOS
- ✅ Android
- ✅ WebGL
- ✅ Console (PlayStation, Xbox, Switch)

## 🔐 Security Best Practices

```csharp
// DON'T store API keys in code!
// Use Unity's Resources or external config

public class SecureOASISConfig : MonoBehaviour
{
    private void Start()
    {
        var config = Resources.Load<OASISConfigAsset>("OASISConfig");
        var oasisClient = new OASISClient(new OASISConfig
        {
            BaseUrl = config.apiUrl,
            ApiKey = GetSecureApiKey() // Load from secure storage
        });
    }

    private string GetSecureApiKey()
    {
        // Load from PlayerPrefs (encrypted), keychain, or secure file
        return PlayerPrefs.GetString("OASIS_API_KEY");
    }
}
```

## 🧪 Testing

```csharp
using NUnit.Framework;
using UnityEngine.TestTools;

public class OASISIntegrationTests
{
    private OASISClient oasisClient;

    [SetUp]
    public void Setup()
    {
        oasisClient = new OASISClient(new OASISConfig
        {
            BaseUrl = "https://testnet.oasis.earth/api/v1",
            UseTestnet = true
        });
    }

    [UnityTest]
    public IEnumerator TestAvatarAuthentication()
    {
        var task = oasisClient.Avatar.AuthenticateAsync(
            "test@example.com",
            "password123",
            "Unity Test"
        );

        yield return new WaitUntil(() => task.IsCompleted);

        Assert.IsFalse(task.Result.IsError);
        Assert.IsNotNull(task.Result.Result);
    }
}
```

## 📖 Documentation

- [Full API Documentation](https://api.oasis.earth/docs)
- [Unity Integration Guide](https://docs.oasis.earth/unity)
- [Sample Projects](https://github.com/NextGenSoftwareUK/OASIS-Unity-Examples)
- [Video Tutorials](https://youtube.com/@OASISPlatform)

## 🤝 Support

- Discord: [discord.gg/oasis](https://discord.gg/oasis)
- Forum: [forum.oasis.earth](https://forum.oasis.earth)
- Email: support@oasis.earth

## 📄 License

MIT License - see LICENSE file for details

---

**Built with ❤️ by NextGen Software**

