# Quick Start - Get Telegram CRM Running Locally

## Prerequisites Check

```bash
# Check .NET version (need 8.0)
dotnet --version

# Check MongoDB (if local)
mongod --version

# Or use MongoDB Atlas (cloud)
```

## Step-by-Step Setup

### 1. Add Project Reference

Edit: `NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`

Add this inside `<Project>` tag:
```xml
<ItemGroup>
  <ProjectReference Include="..\..\Telegcrm\Telegcrm.csproj" />
</ItemGroup>
```

### 2. Get Your OASIS Avatar ID

You need your friend's OASIS Avatar ID. Options:

**Option A: Use existing avatar**
```bash
# Call OASIS API to get avatar by username
curl -X POST "http://localhost:5000/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "your_username", "password": "your_password"}'

# Response will include avatarId
```

**Option B: Create new avatar for CRM**
```bash
curl -X POST "http://localhost:5000/api/avatar/create" \
  -H "Content-Type: application/json" \
  -d '{
    "username": "telegram_crm_user",
    "email": "crm@example.com",
    "password": "secure_password"
  }'
```

### 3. Update OASIS_DNA.json

Add to `NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "MongoDBOASIS": {
      "ConnectionString": "mongodb://localhost:27017"
    }
  },
  "TelegramCRM": {
    "OasisAvatarId": "PASTE-YOUR-AVATAR-GUID-HERE"
  }
}
```

### 4. Modify TelegramBotService

Edit: `NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/TelegramBotService.cs`

**Add using statements at top:**
```csharp
using Telegcrm.Services;
using Telegcrm.Integration;
```

**Add to class fields:**
```csharp
private readonly TelegramCrmService _crmService;
private readonly Guid _crmOasisAvatarId;
```

**Add to constructor:**
```csharp
public TelegramBotService(
    string botToken,
    TelegramOASIS telegramProvider,
    AvatarManager avatarManager,
    AchievementManager achievementManager,
    TimoRidesApiService timoRidesService,
    RideBookingStateManager rideStateManager,
    GoogleMapsService mapsService,
    TelegramCrmService crmService,  // ADD
    IConfiguration configuration)    // ADD for config
{
    // ... existing code
    _crmService = crmService;
    _crmOasisAvatarId = Guid.Parse(
        configuration["TelegramCRM:OasisAvatarId"] ?? Guid.Empty.ToString()
    );
}
```

**Modify HandleUpdateAsync method:**
Find this section (around line 96-100):
```csharp
if (update.Message is not { } message)
    return;
    
if (message.Text is not { } messageText)
    return;
```

Add AFTER the message check:
```csharp
if (update.Message is not { } message)
    return;
    
// NEW: Store message in CRM
try
{
    await TelegramBotServiceIntegration.StoreMessageInCrmAsync(
        _crmService,
        message,
        _crmOasisAvatarId,
        _botClient
    );
}
catch (Exception ex)
{
    Console.WriteLine($"[CRM] Error: {ex.Message}");
}

if (message.Text is not { } messageText)
    return;
```

### 5. Register Services

Find where services are registered (look for `AddSingleton` or `AddScoped` calls). Add:

```csharp
using Telegcrm.Services;
using Telegcrm.Controllers;

// Get MongoDB connection
var mongoConn = Configuration["StorageProviders:MongoDBOASIS:ConnectionString"] 
    ?? "mongodb://localhost:27017";

// Register CRM service
services.AddSingleton(new TelegramCrmService(mongoConn));
```

### 6. Build

```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet build
```

### 7. Run

```bash
cd NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### 8. Test

1. **Send a message to your Telegram bot**
2. **Check MongoDB:**
```bash
# Connect to MongoDB
mongosh

# Switch to database
use TelegramCRM

# Check collections
show collections

# View conversations
db.conversations.find().pretty()

# View messages
db.messages.find().pretty()
```

3. **Test API:**
```bash
# Replace YOUR-GUID with your OASIS Avatar ID
curl "http://localhost:5000/api/telegram-crm/conversations?oasisAvatarId=YOUR-GUID"
```

## Verification Checklist

- [ ] Project builds without errors
- [ ] MongoDB is running/accessible
- [ ] OASIS Avatar ID is set in config
- [ ] Telegram bot is receiving messages
- [ ] Messages appear in MongoDB
- [ ] API endpoints return data

## Common Issues

**"Cannot find TelegramCrmService"**
- Check project reference is added
- Rebuild solution: `dotnet clean && dotnet build`

**"MongoDB connection failed"**
- Check MongoDB is running: `mongod` or use Atlas
- Verify connection string

**"OasisAvatarId is empty"**
- Check OASIS_DNA.json has TelegramCRM section
- Verify GUID format is correct

**"Messages not being stored"**
- Check console for CRM errors
- Verify HandleUpdateAsync modification is correct
- Test with a simple message first

## Success!

If you see messages in MongoDB and API returns data, you're all set! 🎉

Next: Build a simple dashboard or use the API to view conversations.

