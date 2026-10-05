# OASIS Web5 STAR Unreal Engine SDK

⭐ **Unreal Engine SDK for the OASIS Web5 STAR API** - Create stunning AAA metaverse experiences with STAR!

## 🌟 Features

- **STAR Metaverse Platform** - Full integration with Our World / STAR
- **Quest & Mission System** - Complete RPG progression for metaverse
- **GeoNFT Integration** - Location-based AR/VR NFTs with UE5 world partition
- **STAR Plugins** - Extend with STARNET plugins
- **Blueprint & C++ Support** - Visual scripting + code workflows
- **Nanite & Lumen Ready** - Optimized for UE5 rendering features
- **World Partition Support** - Massive open-world metaverse experiences

## 📦 Installation

### Unreal Marketplace

1. Epic Games Launcher > Marketplace
2. Search "OASIS STAR SDK"
3. Install to Engine (UE 5.0+)

## 🚀 Quick Start

### C++ Integration

#### 1. Initialize STAR Client

```cpp
// GameMode.h
#include "STARClient.h"

UCLASS()
class AMyGameMode : public AGameModeBase
{
    GENERATED_BODY()

protected:
    UPROPERTY()
    USTARClient* STARClient;

    virtual void BeginPlay() override;
};
```

```cpp
// GameMode.cpp
void AMyGameMode::BeginPlay()
{
    Super::BeginPlay();

    STARClient = NewObject<USTARClient>();
    
    FSTARConfig Config;
    Config.BaseUrl = TEXT("https://api.star.oasis.earth/api/v1");
    Config.ApiKey = TEXT("your-api-key");
    
    STARClient->Initialize(Config);
}
```

#### 2. Quest System Integration

```cpp
UCLASS()
class UQuestManager : public UActorComponent
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable)
    void LoadChapter(const FGuid& ChapterId)
    {
        STARClient->Quests->GetChapter(
            ChapterId,
            [this](const FSTARResult<FChapter>& Result)
            {
                if (!Result.bIsError)
                {
                    CurrentChapter = Result.Data;
                    UE_LOG(LogTemp, Log, TEXT("Loaded chapter: %s"), *Result.Data.Name);
                    UE_LOG(LogTemp, Log, TEXT("Missions: %d"), Result.Data.Missions.Num());
                }
            }
        );
    }

    UFUNCTION(BlueprintCallable)
    void StartQuest(const FGuid& QuestId)
    {
        STARClient->Quests->StartQuest(
            QuestId,
            [this](const FSTARResult<FQuestProgress>& Result)
            {
                if (!Result.bIsError)
                {
                    ActiveQuests.Add(Result.Data);
                    OnQuestStarted.Broadcast(Result.Data);
                }
            }
        );
    }

    UFUNCTION(BlueprintCallable)
    void CompleteObjective(const FGuid& QuestId, const FGuid& ObjectiveId)
    {
        STARClient->Quests->CompleteObjective(
            QuestId,
            ObjectiveId,
            [this](const FSTARResult<FObjectiveComplete>& Result)
            {
                if (!Result.bIsError)
                {
                    UE_LOG(LogTemp, Log, TEXT("Karma earned: %d"), Result.Data.KarmaEarned);
                    UpdateQuestProgress(QuestId);
                }
            }
        );
    }

protected:
    UPROPERTY(BlueprintReadOnly)
    FChapter CurrentChapter;

    UPROPERTY(BlueprintReadOnly)
    TArray<FQuestProgress> ActiveQuests;

    UPROPERTY(BlueprintAssignable)
    FQuestStartedDelegate OnQuestStarted;
};
```

#### 3. GeoNFT System (AR/VR)

```cpp
UCLASS()
class UGeoNFTManager : public UActorComponent
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable)
    void PlaceGeoNFT(double Latitude, double Longitude, AActor* NFTActor)
    {
        FCreateGeoNFTRequest Request;
        Request.Name = TEXT("Metaverse Portal");
        Request.Latitude = Latitude;
        Request.Longitude = Longitude;
        Request.Radius = 50.0; // 50 meters
        Request.NFTData.Add(TEXT("ActorClass"), NFTActor->GetClass()->GetName());
        
        STARClient->GeoNFT->CreateGeoNFT(
            Request,
            [this, NFTActor](const FSTARResult<FGeoNFT>& Result)
            {
                if (!Result.bIsError)
                {
                    SpawnedGeoNFTs.Add(Result.Data.Id, NFTActor);
                    UE_LOG(LogTemp, Log, TEXT("GeoNFT placed at: %f, %f"), Latitude, Longitude);
                }
            }
        );
    }

    UFUNCTION(BlueprintCallable)
    void DiscoverNearbyGeoNFTs(const FVector& PlayerLocation)
    {
        // Convert UE coordinates to Lat/Lon
        double Latitude, Longitude;
        ConvertToGeoCoordinates(PlayerLocation, Latitude, Longitude);
        
        STARClient->GeoNFT->GetNearbyGeoNFTs(
            Latitude,
            Longitude,
            1000.0, // 1km radius
            [this](const FSTARResult<TArray<FGeoNFT>>& Result)
            {
                if (!Result.bIsError)
                {
                    for (const FGeoNFT& GeoNFT : Result.Data)
                    {
                        SpawnGeoNFTInWorld(GeoNFT);
                    }
                }
            }
        );
    }

private:
    UPROPERTY()
    TMap<FGuid, AActor*> SpawnedGeoNFTs;
};
```

### Blueprint Integration

#### Quest System Blueprint

![Quest System](docs/images/bp_quest_system.png)

**Event Graph:**
```
Event: On Player Interact
  ↓
Get STAR Client → Get Chapter
  - Chapter ID: {CurrentChapterId}
  ↓
On Success:
  - For Each Mission in Chapter.Missions
    → Display Mission Info
    → Add to Quest Log Widget
```

#### GeoNFT Placement Blueprint

![GeoNFT Placement](docs/images/bp_geonft.png)

**Nodes:**
```
Event: Place GeoNFT
  ↓
Get Player Location
  ↓
Convert to Lat/Lon
  ↓
Get STAR Client → Create GeoNFT
  - Location: {Lat, Lon}
  - NFT Data: {Actor Class, Model, Metadata}
  ↓
On Success:
  - Spawn NFT Actor in World
  - Add to GeoNFT Manager
```

## 🎮 Core Blueprints

### STAR Subsystem

Access STAR client from any blueprint:

```
Get Game Instance
  ↓
Get Subsystem (STAR Subsystem)
  ↓
Get STAR Client
```

### Data Structures

#### FChapter

- **Id** (Guid)
- **Name** (String)
- **Description** (String)
- **Missions** (Array of FMission)
- **RequiredLevel** (Int32)

#### FMission

- **Id** (Guid)
- **Name** (String)
- **Quests** (Array of FQuest)
- **RewardKarma** (Int32)

#### FGeoNFT

- **Id** (Guid)
- **Name** (String)
- **Latitude** (Double)
- **Longitude** (Double)
- **Radius** (Double)
- **NFTData** (Map of String to String)

## 🌐 Metaverse World Creation

### World Partition Integration

```cpp
UCLASS()
class AMetaverseWorld : public AWorldPartitionBuilder
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable)
    void CreateSTARWorld()
    {
        FCreateWorldRequest Request;
        Request.Name = TEXT("Epic Metaverse");
        Request.Description = TEXT("AAA metaverse built with Unreal Engine 5");
        Request.MaxPlayers = 1000;
        Request.WorldSize = FVector(100000, 100000, 10000); // 100km x 100km
        
        STARClient->World->CreateWorld(
            Request,
            [this](const FSTARResult<FMetaverseWorld>& Result)
            {
                if (!Result.bIsError)
                {
                    WorldUrl = Result.Data.WorldUrl;
                    InitializeWorldPartition(Result.Data);
                }
            }
        );
    }
};
```

### OAPP Development in Unreal

```cpp
UCLASS()
class UOAPPBuilder : public UBlueprintFunctionLibrary
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable, Category = "STAR|OAPP")
    static void PackageAsOAPP(
        const FString& ProjectName,
        const TArray<FGuid>& HolonIds,
        const TArray<FGuid>& ZomeIds)
    {
        FCreateOAPPRequest Request;
        Request.Name = ProjectName;
        Request.Description = TEXT("Unreal Engine OAPP");
        Request.Version = TEXT("1.0.0");
        Request.Holons = HolonIds;
        Request.Zomes = ZomeIds;
        Request.Platform = TEXT("UnrealEngine5");
        
        auto STARClient = GetSTARSubsystem();
        STARClient->OAPP->CreateOAPP(Request, [](const auto& Result)
        {
            if (!Result.bIsError)
            {
                UE_LOG(LogTemp, Log, TEXT("OAPP Created: %s"), *Result.Data.Id.ToString());
            }
        });
    }
};
```

## 🎯 Advanced Features

### Multiplayer Quest Sync

```cpp
// Replicated Quest Component
UCLASS(Replicated)
class UReplicatedQuestComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UPROPERTY(ReplicatedUsing = OnRep_QuestProgress)
    FQuestProgress QuestProgress;

protected:
    UFUNCTION()
    void OnRep_QuestProgress()
    {
        // Sync quest state from STAR
        STARClient->Quests->GetQuestProgress(
            QuestProgress.QuestId,
            [this](const auto& Result)
            {
                if (!Result.bIsError)
                {
                    QuestProgress = Result.Data;
                }
            }
        );
    }
};
```

### Nanite GeoNFT Streaming

```cpp
UCLASS()
class AGeoNFTActor : public AActor
{
    GENERATED_BODY()

public:
    UPROPERTY(EditAnywhere)
    UNaniteGeometryComponent* NaniteMesh;

    void LoadFromGeoNFT(const FGeoNFT& GeoNFT)
    {
        // Stream high-quality Nanite mesh from OASIS
        FString MeshUrl = GeoNFT.NFTData.FindRef(TEXT("NaniteMeshUrl"));
        
        // Async load Nanite asset
        AsyncLoadNaniteMesh(MeshUrl, [this](UStaticMesh* Mesh)
        {
            NaniteMesh->SetStaticMesh(Mesh);
        });
    }
};
```

## 📱 Platform Support

- ✅ Windows
- ✅ macOS  
- ✅ Linux
- ✅ PlayStation 5
- ✅ Xbox Series X/S
- ✅ iOS / iPadOS
- ✅ Android
- ✅ VR (PCVR, Quest, PSVR2)

## 🔧 Configuration

```ini
; DefaultGame.ini
[/Script/STARAPI.STARSettings]
DefaultBaseUrl=https://api.star.oasis.earth/api/v1
bEnableGeoNFTs=True
bEnableQuestSystem=True
MaxActiveQuests=10
GeoNFTDiscoveryRadius=1000.0

[/Script/STARAPI.MetaverseSettings]
bUseWorldPartition=True
WorldPartitionCellSize=25600
bEnableMultiplayerSync=True
```

## 📖 Documentation

- [Full API Reference](https://api.star.oasis.earth/docs)
- [Unreal STAR Guide](https://docs.oasis.earth/unreal-star)
- [Blueprint Cookbook](https://docs.oasis.earth/unreal/blueprints-star)
- [Sample Projects](https://github.com/NextGenSoftwareUK/OASIS-STAR-Unreal-Examples)

## 🤝 Support

- Discord: [discord.gg/oasis](https://discord.gg/oasis)
- Forum: [forum.oasis.earth](https://forum.oasis.earth)

---

**⭐ Build the AAA Metaverse with STAR + Unreal Engine 5 ⭐**

