# OASIS Web4 Unreal Engine SDK

🎮 **Unreal Engine SDK for the OASIS Web4 API** - Bring AAA metaverse experiences to life with OASIS!

## 🌟 Features

- **Blueprint & C++ Support** - Full integration for both visual and code workflows
- **Complete Avatar System** - Authentication, profiles, karma management
- **Data Storage** - Holons, Zomes, and OASIS data objects
- **Provider Integration** - 80+ blockchains and storage providers
- **Async Blueprints** - Non-blocking API calls with latent nodes
- **Type-Safe C++** - Full Unreal property system integration
- **Replication Ready** - Multiplayer-compatible data structures

## 📦 Installation

### Option 1: Unreal Marketplace (Recommended)

1. Open Epic Games Launcher
2. Navigate to Unreal Engine > Marketplace
3. Search for "OASIS Web4 SDK"
4. Click "Install to Engine"

### Option 2: Manual Installation

1. Download the latest release from [GitHub](https://github.com/NextGenSoftwareUK/OASIS-Unreal-SDK)
2. Extract to your project's `Plugins` folder
3. Regenerate project files
4. Enable the plugin in Edit > Plugins > OASIS

## 🚀 Quick Start

### C++ Integration

#### 1. Add Module Dependency

```cpp
// YourProject.Build.cs
PublicDependencyModuleNames.AddRange(new string[] { 
    "Core", 
    "CoreUObject", 
    "Engine",
    "OASISAPI"  // Add this
});
```

#### 2. Initialize OASIS Client

```cpp
// GameMode.h
#pragma once
#include "CoreMinimal.h"
#include "OASISClient.h"
#include "GameFramework/GameModeBase.h"
#include "MyGameMode.generated.h"

UCLASS()
class MYPROJECT_API AMyGameMode : public AGameModeBase
{
    GENERATED_BODY()

public:
    virtual void BeginPlay() override;

protected:
    UPROPERTY()
    UOASISClient* OASISClient;
};
```

```cpp
// GameMode.cpp
void AMyGameMode::BeginPlay()
{
    Super::BeginPlay();

    // Create OASIS client
    OASISClient = NewObject<UOASISClient>();
    
    FOASISConfig Config;
    Config.BaseUrl = TEXT("https://api.oasis.earth/api/v1");
    Config.ApiKey = TEXT("your-api-key");
    
    OASISClient->Initialize(Config);
    
    // Test connection
    OASISClient->GetHealth([](const FOASISResult<FHealthResponse>& Result)
    {
        if (!Result.bIsError)
        {
            UE_LOG(LogTemp, Log, TEXT("OASIS Status: %s"), *Result.Data.Status);
        }
    });
}
```

#### 3. Avatar Authentication (C++)

```cpp
// LoginWidget.cpp
void ULoginWidget::LoginUser(const FString& Email, const FString& Password)
{
    auto OASISClient = GetGameInstance()->GetSubsystem<UOASISSubsystem>()->GetClient();
    
    FAuthRequest AuthRequest;
    AuthRequest.Email = Email;
    AuthRequest.Password = Password;
    AuthRequest.DeviceInfo = FPlatformMisc::GetDeviceId();
    
    OASISClient->Avatar->Authenticate(
        AuthRequest,
        [this](const FOASISResult<FAuthResponse>& Result)
        {
            if (!Result.bIsError)
            {
                UE_LOG(LogTemp, Log, TEXT("Welcome %s!"), *Result.Data.Avatar.Username);
                OnLoginSuccess(Result.Data);
            }
            else
            {
                UE_LOG(LogTemp, Error, TEXT("Login failed: %s"), *Result.Message);
                OnLoginFailed(Result.Message);
            }
        }
    );
}
```

### Blueprint Integration

#### 1. Create OASIS Subsystem Blueprint

![OASIS Subsystem](docs/images/blueprint_subsystem.png)

1. Right-click in Content Browser > Blueprint Class > Game Instance Subsystem
2. Name it `BP_OASISSubsystem`
3. Open and add OASIS Client component

#### 2. Authentication Blueprint

![Auth Blueprint](docs/images/blueprint_auth.png)

**Nodes:**
- `Get OASIS Client` → `Authenticate Avatar`
  - **Email**: (String input)
  - **Password**: (String input)
  - **On Success**: Print "Welcome {Username}"
  - **On Failure**: Print "Login failed: {Error}"

#### 3. Karma System Blueprint

![Karma Blueprint](docs/images/blueprint_karma.png)

**Example: Award karma for enemy kill**

```
Event: On Enemy Killed
  ↓
Get OASIS Client
  ↓
Add Karma to Avatar
  - Avatar ID: (from player state)
  - Amount: 100
  - Karma Type: "Positive"
  - Source: "Enemy Kill"
  ↓
On Success:
  - Update UI: New Karma = {Result.TotalKarma}
  - Check Level Up: {Result.CurrentLevel}
```

## 📚 Core Classes

### UOASISClient

Main client class for all OASIS operations.

```cpp
UCLASS(BlueprintType)
class OASISAPI_API UOASISClient : public UObject
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable, Category = "OASIS")
    void Initialize(const FOASISConfig& Config);

    UFUNCTION(BlueprintCallable, Category = "OASIS|Avatar")
    void AuthenticateAvatar(
        const FAuthRequest& Request,
        const FAuthResponseDelegate& OnComplete
    );

    UFUNCTION(BlueprintCallable, Category = "OASIS|Avatar")
    void GetAvatarById(
        const FGuid& AvatarId,
        const FAvatarResponseDelegate& OnComplete
    );

    UFUNCTION(BlueprintCallable, Category = "OASIS|Data")
    void CreateHolon(
        const FCreateHolonRequest& Request,
        const FHolonResponseDelegate& OnComplete
    );
};
```

### Data Structures

```cpp
USTRUCT(BlueprintType)
struct FOASISAvatar
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    FGuid Id;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    FString Username;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    FString Email;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    int32 Karma;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    int32 Level;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    TMap<FString, FString> MetaData;
};

USTRUCT(BlueprintType)
struct FHolon
{
    GENERATED_BODY()

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    FGuid Id;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    FString Name;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    FString HolonType;

    UPROPERTY(BlueprintReadWrite, Category = "OASIS")
    TMap<FString, FString> MetaData;
};
```

## 🎮 Advanced Examples

### Multiplayer Avatar Sync

```cpp
// PlayerController.cpp
void AMyPlayerController::OnPossess(APawn* InPawn)
{
    Super::OnPossess(InPawn);

    // Load OASIS avatar data
    auto OASISClient = GetWorld()->GetGameInstance()->GetSubsystem<UOASISSubsystem>();
    
    OASISClient->GetClient()->Avatar->GetAvatarById(
        PlayerState->GetOASISAvatarId(),
        [this](const FOASISResult<FOASISAvatar>& Result)
        {
            if (!Result.bIsError)
            {
                ApplyAvatarToCharacter(Result.Data);
            }
        }
    );
}
```

### Persistent Inventory System

```cpp
UCLASS()
class UInventoryComponent : public UActorComponent
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable)
    void SaveInventory()
    {
        FCreateHolonRequest Request;
        Request.Name = TEXT("PlayerInventory");
        Request.HolonType = TEXT("Inventory");
        
        // Serialize inventory to JSON
        TSharedPtr<FJsonObject> InventoryJson = MakeShareable(new FJsonObject);
        for (const auto& Item : Items)
        {
            InventoryJson->SetNumberField(Item.Key, Item.Value);
        }
        
        FString JsonString;
        TSharedRef<TJsonWriter<>> Writer = TJsonWriterFactory<>::Create(&JsonString);
        FJsonSerializer::Serialize(InventoryJson.ToSharedRef(), Writer);
        
        Request.MetaData.Add(TEXT("items"), JsonString);
        
        OASISClient->Data->CreateHolon(Request, [](const auto& Result)
        {
            UE_LOG(LogTemp, Log, TEXT("Inventory saved! ID: %s"), *Result.Data.Id.ToString());
        });
    }
};
```

### Achievement System

```cpp
UCLASS()
class UAchievementManager : public UGameInstanceSubsystem
{
    GENERATED_BODY()

public:
    UFUNCTION(BlueprintCallable)
    void UnlockAchievement(const FString& AchievementId)
    {
        // Create achievement holon
        FCreateHolonRequest Request;
        Request.Name = AchievementId;
        Request.HolonType = TEXT("Achievement");
        Request.MetaData.Add(TEXT("unlockedAt"), FDateTime::UtcNow().ToString());
        
        OASISClient->Data->CreateHolon(Request, [this, AchievementId](const auto& Result)
        {
            if (!Result.bIsError)
            {
                // Award karma
                FAddKarmaRequest KarmaRequest;
                KarmaRequest.Amount = 100;
                KarmaRequest.KarmaSourceTitle = FString::Printf(TEXT("Achievement: %s"), *AchievementId);
                
                OASISClient->Avatar->AddKarma(GetPlayerAvatarId(), KarmaRequest, {});
            }
        });
    }
};
```

## 🌐 Multiplayer Integration

### Replication Setup

```cpp
// OASISReplicatedActor.h
UCLASS()
class AOASISReplicatedActor : public AActor
{
    GENERATED_BODY()

public:
    AOASISReplicatedActor();

    UPROPERTY(Replicated, BlueprintReadOnly)
    FGuid OASISHolonId;

    UPROPERTY(Replicated, BlueprintReadOnly)
    FOASISAvatar OwnerAvatar;

protected:
    virtual void GetLifetimeReplicatedProps(TArray<FLifetimeProperty>& OutLifetimeProps) const override;
    
    UFUNCTION(Server, Reliable)
    void Server_SyncToOASIS();
};
```

## 📱 Platform Support

- ✅ Windows
- ✅ macOS
- ✅ Linux
- ✅ iOS
- ✅ Android
- ✅ PlayStation 5
- ✅ Xbox Series X/S
- ✅ Nintendo Switch

## 🔧 Build Configuration

```ini
; DefaultEngine.ini
[/Script/OASISAPI.OASISSettings]
DefaultBaseUrl=https://api.oasis.earth/api/v1
bUseTestnet=False
bAutoFailover=True
DefaultTimeout=30.0
RetryAttempts=3

[/Script/OASISAPI.OASISProviderSettings]
PreferredProviders=(ProviderType="HoloOASIS",Priority=1)
PreferredProviders=(ProviderType="IPFSOASIS",Priority=2)
```

## 📖 Documentation

- [Full API Reference](https://api.oasis.earth/docs)
- [Unreal Integration Guide](https://docs.oasis.earth/unreal)
- [Blueprint Quick Reference](https://docs.oasis.earth/unreal/blueprints)
- [Sample Projects](https://github.com/NextGenSoftwareUK/OASIS-Unreal-Examples)

## 🤝 Support

- Discord: [discord.gg/oasis](https://discord.gg/oasis)
- Forum: [forum.oasis.earth](https://forum.oasis.earth)
- Email: support@oasis.earth

## 📄 License

MIT License - see LICENSE file for details

---

**🎮 Built for Unreal Engine 5+ | Powered by OASIS 🌐**

