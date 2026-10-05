# Local Setup Guide for Telegram CRM

## Quick Start

### Step 1: Add Project Reference

Add Telegcrm to your WebAPI project. Edit `NextGenSoftware.OASIS.API.ONODE.WebAPI.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\Telegcrm\Telegcrm.csproj" />
</ItemGroup>
```

### Step 2: Update TelegramBotService

Add CRM integration to `Services/TelegramBotService.cs`:

```csharp
using Telegcrm.Services;
using Telegcrm.Integration;

// Add to constructor parameters:
private readonly TelegramCrmIntegration _crmIntegration;

public TelegramBotService(
    string botToken,
    TelegramOASIS telegramProvider,
    AvatarManager avatarManager,
    AchievementManager achievementManager,
    TimoRidesApiService timoRidesService,
    RideBookingStateManager rideStateManager,
    GoogleMapsService mapsService,
    TelegramCrmIntegration crmIntegration) // ADD THIS
{
    // ... existing code
    _crmIntegration = crmIntegration;
}

// In HandleUpdateAsync method, add after line 108:
private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
{
    // ... existing code
    
    if (update.Message is not { } message)
        return;
    
    // NEW: Store message in CRM
    try
    {
        await _crmIntegration.HandleIncomingMessageAsync(message);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"CRM Error: {ex.Message}");
    }
    
    // ... rest of existing code
}
```

### Step 3: Register Services

Find where TelegramBotService is registered (usually in Program.cs, Startup.cs, or a service registration file) and add:

```csharp
using Telegcrm.Services;
using Telegcrm.Integration;
using Telegcrm.Controllers;

// Get MongoDB connection from OASIS_DNA.json
var mongoConnectionString = Configuration["StorageProviders:MongoDBOASIS:ConnectionString"] 
    ?? "mongodb://localhost:27017";

// Register CRM service
services.AddSingleton(new TelegramCrmService(mongoConnectionString));

// Register CRM integration (requires bot token and avatar ID)
services.AddSingleton(provider =>
{
    var botToken = Configuration["TelegramOASIS:BotToken"] ?? "";
    var botClient = new TelegramBotClient(botToken);
    var crmService = provider.GetRequiredService<TelegramCrmService>();
    
    // Get OASIS Avatar ID - you'll need to set this
    // For now, use a placeholder or get from config
    var avatarId = Guid.Parse(Configuration["TelegramCRM:OasisAvatarId"] ?? Guid.Empty.ToString());
    
    return new TelegramCrmIntegration(crmService, botClient, avatarId);
});
```

### Step 4: Add Configuration

Add to `OASIS_DNA.json`:

```json
{
  "StorageProviders": {
    "MongoDBOASIS": {
      "ConnectionString": "mongodb://localhost:27017"
    }
  },
  "TelegramCRM": {
    "OasisAvatarId": "YOUR-OASIS-AVATAR-GUID-HERE"
  }
}
```

### Step 5: Build and Run

```bash
# Build the solution
cd /Volumes/Storage/OASIS_CLEAN
dotnet build

# Run the WebAPI
cd NextGenSoftware.OASIS.API.ONODE.WebAPI
dotnet run
```

### Step 6: Test

1. Send a message to your Telegram bot
2. Check MongoDB - you should see collections:
   - `conversations`
   - `messages`
   - `contacts`
   - `followups`

3. Test API:
```bash
# Get conversations (replace YOUR-GUID with your OASIS Avatar ID)
curl "http://localhost:5000/api/telegram-crm/conversations?oasisAvatarId=YOUR-GUID"
```

## Troubleshooting

### Build Errors
- Ensure .NET 8.0 SDK is installed
- Check all NuGet packages are restored: `dotnet restore`

### MongoDB Connection
- Ensure MongoDB is running: `mongod` or use MongoDB Atlas
- Check connection string in OASIS_DNA.json

### Messages Not Stored
- Check console for CRM errors
- Verify OasisAvatarId is correct
- Ensure TelegramBotService is receiving messages

### API Not Working
- Check controller is registered
- Verify routing is configured
- Check CORS if accessing from browser

## Next Steps

1. Get your OASIS Avatar ID (from OASIS API or create one)
2. Update OASIS_DNA.json with your avatar ID
3. Start MongoDB
4. Run the API
5. Send test messages
6. Check the CRM data

