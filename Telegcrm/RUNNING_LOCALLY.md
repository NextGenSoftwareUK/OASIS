# Running Telegram CRM Locally - Complete Guide

## ✅ Current Status

The Telegcrm project **builds successfully** and is ready for integration!

## Quick Integration Steps

### 1. Test the Build

```bash
cd /Volumes/Storage/OASIS_CLEAN/Telegcrm
./test-local.sh
```

Or manually:
```bash
dotnet build
```

### 2. Add to WebAPI Project

Edit: `NextGenSoftware.OASIS.API.ONODE.WebAPI/NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`

Add this line in the `<ItemGroup>` section (or create new one):
```xml
<ProjectReference Include="..\..\Telegcrm\Telegcrm.csproj" />
```

### 3. Get OASIS Avatar ID

You need your friend's OASIS Avatar ID. Run this to find it:

```bash
# Option 1: If you know the username
curl -X POST "http://localhost:5000/api/avatar/authenticate" \
  -H "Content-Type: application/json" \
  -d '{"username": "username", "password": "password"}'

# The response will include avatarId
```

Or check in MongoDB:
```bash
mongosh "your-connection-string"
use OASISAPI_DEV
db.avatars.find({username: "username"}).pretty()
```

### 4. Update Configuration

Add to `NextGenSoftware.OASIS.API.ONODE.WebAPI/OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "MongoDBOASIS": {
      "ConnectionString": "mongodb+srv://..."
    }
  },
  "TelegramCRM": {
    "OasisAvatarId": "YOUR-AVATAR-GUID-HERE"
  }
}
```

### 5. Modify TelegramBotService

Edit: `NextGenSoftware.OASIS.API.ONODE.WebAPI/Services/TelegramBotService.cs`

**Add using:**
```csharp
using Telegcrm.Services;
using Telegcrm.Integration;
```

**Add field:**
```csharp
private readonly TelegramCrmService _crmService;
private readonly Guid _crmAvatarId;
```

**Update constructor:**
```csharp
public TelegramBotService(
    // ... existing parameters
    TelegramCrmService crmService,
    IConfiguration configuration)
{
    // ... existing code
    _crmService = crmService;
    _crmAvatarId = Guid.Parse(
        configuration["TelegramCRM:OasisAvatarId"] ?? Guid.Empty.ToString()
    );
}
```

**Modify HandleUpdateAsync** (around line 96):
```csharp
if (update.Message is not { } message)
    return;

// NEW: Store in CRM
try
{
    await TelegramBotServiceIntegration.StoreMessageInCrmAsync(
        _crmService,
        message,
        _crmAvatarId,
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

### 6. Register Services

Find where services are registered (search for `AddSingleton` or `services.Add`). Add:

```csharp
using Telegcrm.Services;

// Get MongoDB connection
var mongoConn = Configuration["StorageProviders:MongoDBOASIS:ConnectionString"] 
    ?? "mongodb://localhost:27017";

// Register CRM service
services.AddSingleton(new TelegramCrmService(mongoConn));
```

### 7. Build and Run

```bash
cd /Volumes/Storage/OASIS_CLEAN
dotnet build

cd NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### 8. Test

1. **Send a message to your Telegram bot**
2. **Check MongoDB:**
```bash
mongosh "your-connection-string"
use TelegramCRM
db.conversations.find().pretty()
db.messages.find().pretty()
```

3. **Test API:**
```bash
curl "http://localhost:5000/api/telegram-crm/conversations?oasisAvatarId=YOUR-GUID"
```

## Verification

✅ Project builds  
✅ MongoDB accessible  
✅ Avatar ID configured  
✅ Services registered  
✅ Bot receiving messages  
✅ Messages in MongoDB  
✅ API returns data  

## Troubleshooting

**Build errors:**
- Ensure project reference is added correctly
- Run `dotnet restore` first
- Check .NET 8.0 is installed

**No messages stored:**
- Check console for CRM errors
- Verify MongoDB connection
- Check Avatar ID is correct
- Ensure HandleUpdateAsync modification is in place

**API not working:**
- Check controller is accessible
- Verify routing
- Check CORS settings if accessing from browser

## Success Indicators

When working correctly, you should see:
- Messages appearing in MongoDB `TelegramCRM` database
- Conversations auto-created
- Contacts auto-created
- API endpoints returning data
- No errors in console

## Next Steps

Once running:
1. Build a simple dashboard UI
2. Add more CRM features
3. Set up follow-up notifications
4. Add search UI

---

**Status**: ✅ Ready to integrate and run locally!

