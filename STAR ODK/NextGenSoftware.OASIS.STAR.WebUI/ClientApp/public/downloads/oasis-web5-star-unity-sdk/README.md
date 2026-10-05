# OASIS Web5 STAR Unity SDK

⭐ **Unity SDK for the OASIS Web5 STAR API** - Build next-generation metaverse experiences with the STAR (Synergistic Technological Advancement & Reality) platform!

## 🌟 Features

- **STAR Metaverse Integration** - Build apps for the Our World / STAR metaverse
- **Chapters, Missions & Quests** - Complete game progression system
- **GeoNFT Management** - Location-based AR/VR NFTs
- **STAR Plugins** - Extend functionality with STARNET plugins
- **OAPP Development** - Create and deploy OAPPs (OASIS Applications)
- **Unity XR Ready** - Full VR/AR support for immersive experiences
- **Async/Await Support** - Modern C# patterns with UniTask

## 📦 Installation

### Unity Package Manager

```
https://github.com/NextGenSoftwareUK/OASIS-STAR-Unity-SDK.git
```

## 🚀 Quick Start

### 1. Initialize STAR Client

```csharp
using NextGenSoftware.OASIS.STAR.Unity;
using UnityEngine;

public class STARManager : MonoBehaviour
{
    private STARClient starClient;

    async void Start()
    {
        starClient = new STARClient(new STARConfig
        {
            BaseUrl = "https://api.star.oasis.earth/api/v1",
            ApiKey = "your-api-key"
        });

        var status = await starClient.GetSTARStatusAsync();
        Debug.Log($"STAR Platform: {status.Version}");
    }
}
```

### 2. Chapter & Quest System

```csharp
public class QuestManager : MonoBehaviour
{
    private STARClient starClient;

    public async void LoadChapter(Guid chapterId)
    {
        var chapter = await starClient.Quests.GetChapterAsync(chapterId);
        
        if (!chapter.IsError)
        {
            Debug.Log($"Chapter: {chapter.Result.Name}");
            Debug.Log($"Missions: {chapter.Result.Missions.Count}");
            
            foreach (var mission in chapter.Result.Missions)
            {
                Debug.Log($"- {mission.Name} ({mission.Quests.Count} quests)");
            }
        }
    }

    public async void StartQuest(Guid questId)
    {
        var result = await starClient.Quests.StartQuestAsync(questId);
        
        if (!result.IsError)
        {
            Debug.Log($"Quest started: {result.Result.Name}");
            Debug.Log($"Objectives: {result.Result.Objectives.Count}");
        }
    }

    public async void CompleteObjective(Guid questId, Guid objectiveId)
    {
        var result = await starClient.Quests.CompleteObjectiveAsync(questId, objectiveId);
        
        if (!result.IsError)
        {
            Debug.Log($"Objective completed! Karma earned: {result.Result.KarmaEarned}");
        }
    }
}
```

### 3. GeoNFT Integration (AR/VR)

```csharp
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GeoNFTManager : MonoBehaviour
{
    private STARClient starClient;
    private ARSession arSession;

    public async void PlaceGeoNFT(double latitude, double longitude)
    {
        var geonft = new CreateGeoNFTRequest
        {
            Name = "AR Portal",
            Description = "Interactive AR portal to metaverse",
            Latitude = latitude,
            Longitude = longitude,
            Radius = 50, // meters
            NFTData = new Dictionary<string, object>
            {
                ["model3D"] = "portal_ar.fbx",
                ["animation"] = "portal_idle",
                ["interactive"] = true
            }
        };

        var result = await starClient.GeoNFT.CreateGeoNFTAsync(geonft);
        
        if (!result.IsError)
        {
            // Spawn AR object
            SpawnARObject(result.Result);
        }
    }

    public async void DiscoverNearbyGeoNFTs()
    {
        var location = GetCurrentLocation();
        var result = await starClient.GeoNFT.GetNearbyGeoNFTsAsync(
            location.latitude,
            location.longitude,
            1000 // 1km radius
        );

        if (!result.IsError)
        {
            foreach (var geonft in result.Result)
            {
                SpawnARObject(geonft);
            }
        }
    }
}
```

### 4. OAPP Development

```csharp
public class OAPPBuilder : MonoBehaviour
{
    private STARClient starClient;

    public async void CreateOAPP()
    {
        var oapp = new CreateOAPPRequest
        {
            Name = "MyUnityGame",
            Description = "My awesome Unity game on OASIS",
            Version = "1.0.0",
            Holons = new List<Guid>(), // Data objects
            Zomes = new List<Guid>(),  // Code modules
            Plugins = new List<Guid>() // STAR plugins
        };

        var result = await starClient.OAPP.CreateOAPPAsync(oapp);
        
        if (!result.IsError)
        {
            Debug.Log($"OAPP created! ID: {result.Result.Id}");
        }
    }

    public async void DeployOAPP(Guid oappId)
    {
        var result = await starClient.OAPP.DeployOAPPAsync(oappId, new DeploymentConfig
        {
            TargetPlatform = "Unity",
            TargetProviders = new List<string> { "HoloOASIS", "IPFSOASIS" }
        });

        if (!result.IsError)
        {
            Debug.Log($"OAPP deployed to: {result.Result.DeploymentUrl}");
        }
    }
}
```

## 🎮 XR/Metaverse Features

### VR Avatar Sync

```csharp
using UnityEngine.XR;

public class VRAvatar : MonoBehaviour
{
    private STARClient starClient;
    [SerializeField] private Transform headTransform;
    [SerializeField] private Transform leftHandTransform;
    [SerializeField] private Transform rightHandTransform;

    async void Update()
    {
        // Sync VR avatar pose to OASIS
        await starClient.Avatar.UpdateAvatarPoseAsync(new AvatarPose
        {
            Head = TransformToData(headTransform),
            LeftHand = TransformToData(leftHandTransform),
            RightHand = TransformToData(rightHandTransform)
        });
    }
}
```

### Metaverse World Builder

```csharp
public class MetaverseWorld : MonoBehaviour
{
    private STARClient starClient;

    public async void CreateWorld()
    {
        var world = new CreateWorldRequest
        {
            Name = "My Metaverse",
            Description = "Custom Unity metaverse world",
            MaxPlayers = 100,
            GeoLocation = new GeoCoordinate(51.5074, -0.1278), // London
            WorldData = new Dictionary<string, object>
            {
                ["terrain"] = "terrain_data.asset",
                ["skybox"] = "custom_sky",
                ["physics"] = "realistic"
            }
        };

        var result = await starClient.World.CreateWorldAsync(world);
        
        if (!result.IsError)
        {
            Debug.Log($"World created: {result.Result.WorldUrl}");
        }
    }
}
```

## 📱 Platform Support

- ✅ PC VR (Oculus, Vive, Index)
- ✅ Standalone VR (Quest, Pico)
- ✅ Mobile AR (ARCore, ARKit)
- ✅ WebXR
- ✅ Desktop
- ✅ Mobile

## 📚 API Modules

- **QuestsAPI** - Chapters, Missions, Quests, Sub-Quests
- **GeoNFTAPI** - Location-based NFTs and AR experiences
- **OAPPAPI** - OASIS Application development and deployment
- **WorldAPI** - Metaverse world creation and management
- **PluginAPI** - STAR plugin integration
- **AvatarAPI** - Extended avatar features for metaverse

## 🔗 Integration with Web4 SDK

```csharp
// Use both SDKs together
var web4Client = new OASISClient(web4Config);
var starClient = new STARClient(starConfig);

// Authenticate once, use everywhere
var auth = await web4Client.Avatar.AuthenticateAsync(email, password);
starClient.SetAuthToken(auth.Result.JwtToken);

// Access full OASIS + STAR ecosystem
var avatar = await web4Client.Avatar.GetAvatarByIdAsync(avatarId);
var quests = await starClient.Quests.GetActiveQuestsAsync(avatarId);
```

## 📖 Full Documentation

- [STAR API Docs](https://api.star.oasis.earth/docs)
- [Unity XR Guide](https://docs.oasis.earth/unity-xr)
- [Metaverse Development](https://docs.oasis.earth/metaverse)
- [Sample Projects](https://github.com/NextGenSoftwareUK/OASIS-STAR-Unity-Examples)

## 🤝 Support

- Discord: [discord.gg/oasis](https://discord.gg/oasis)
- Forum: [forum.oasis.earth](https://forum.oasis.earth)

---

**⭐ Build the future with STAR ⭐**

