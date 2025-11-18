# Integration Example

This document shows how to integrate Telegram CRM with your existing Telegram bot.

## Step 1: Add Reference to Your WebAPI Project

In your `NextGenSoftware.OASIS.API.ONODE.WebAPI` project, add a reference to Telegcrm:

```xml
<ItemGroup>
  <ProjectReference Include="..\Telegcrm\Telegcrm.csproj" />
</ItemGroup>
```

## Step 2: Register Services in Startup/Program.cs

```csharp
using Telegcrm.Services;
using Telegcrm.Integration;

// In ConfigureServices or builder.Services:
var mongoConnectionString = Configuration["TelegramCRM:MongoConnectionString"] 
    ?? Configuration["TelegramOASIS:MongoConnectionString"]; // Fallback to existing config

services.AddSingleton(new TelegramCrmService(mongoConnectionString));

// Get OASIS Avatar ID (your friend's avatar)
var oasisAvatarId = Guid.Parse(Configuration["TelegramCRM:OasisAvatarId"]);

// Create CRM integration (will be used in TelegramBotService)
services.AddSingleton(provider =>
{
    var botClient = provider.GetRequiredService<TelegramBotClient>();
    var crmService = provider.GetRequiredService<TelegramCrmService>();
    return new TelegramCrmIntegration(crmService, botClient, oasisAvatarId);
});
```

## Step 3: Update TelegramBotService

Find your `TelegramBotService.cs` file and add CRM integration:

```csharp
using Telegcrm.Integration;

public class TelegramBotService
{
    private readonly TelegramCrmIntegration _crmIntegration;
    
    public TelegramBotService(
        string botToken,
        TelegramCrmIntegration crmIntegration, // Add this
        // ... other dependencies
    )
    {
        _crmIntegration = crmIntegration;
        // ... rest of initialization
    }

    // In your message handler:
    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        try
        {
            // NEW: Store message in CRM (do this first, before any other processing)
            await _crmIntegration.HandleIncomingMessageAsync(message);
            
            // Your existing bot logic here...
            if (message.Text != null)
            {
                switch (message.Text.Split(' ')[0])
                {
                    case "/start":
                        await HandleStartCommandAsync(message, cancellationToken);
                        break;
                    // ... other commands
                }
            }
        }
        catch (Exception ex)
        {
            // Error handling
        }
    }

    // When sending messages, also store them:
    private async Task SendMessageAsync(long chatId, string text)
    {
        var sentMessage = await _botClient.SendTextMessageAsync(chatId, text);
        
        // Store outgoing message in CRM
        await _crmIntegration.HandleOutgoingMessageAsync(sentMessage);
    }
}
```

## Step 4: Add Configuration

Update your `OASIS_DNA.json`:

```json
{
  "TelegramOASIS": {
    "BotToken": "YOUR_BOT_TOKEN",
    "MongoConnectionString": "mongodb://localhost:27017",
    // ... existing config
  },
  "TelegramCRM": {
    "MongoConnectionString": "mongodb://localhost:27017",
    "OasisAvatarId": "your-friend-oasis-avatar-guid-here"
  }
}
```

## Step 5: Test the Integration

1. Start your OASIS API
2. Send a message to your Telegram bot
3. Check MongoDB - you should see:
   - A new conversation in `conversations` collection
   - A new message in `messages` collection
   - A new contact in `contacts` collection (if it's a private chat)

4. Test API endpoints:
```bash
# Get conversations
curl "http://localhost:5000/api/telegram-crm/conversations?oasisAvatarId=YOUR-GUID"

# Get messages for a conversation
curl "http://localhost:5000/api/telegram-crm/conversations/CONV-ID/messages"
```

## Step 6: Add Authentication (Important!)

The API endpoints should be protected. Add authentication middleware:

```csharp
[Authorize] // Add this attribute to your controller
[ApiController]
[Route("api/telegram-crm")]
public class TelegramCrmController : ControllerBase
{
    // ... controller code
}
```

## Troubleshooting

### Messages not being stored
- Check MongoDB connection string
- Verify OasisAvatarId is correct
- Check bot is receiving messages (check TelegramBotService logs)

### API endpoints not working
- Ensure Telegcrm project is referenced
- Check services are registered
- Verify controller is registered in routing

### MongoDB errors
- Ensure MongoDB is running
- Check connection string format
- Verify database permissions

## Next Steps

1. Build a simple web dashboard to view conversations
2. Add notification system for follow-ups
3. Implement search UI
4. Add priority filtering
5. Create mobile-friendly views

