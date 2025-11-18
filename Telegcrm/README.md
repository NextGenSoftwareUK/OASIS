# Telegram CRM for OASIS

A simple CRM system for managing Telegram conversations, built on top of OASIS's existing Telegram integration.

## 🎯 Purpose

This system helps you never miss important business conversations on Telegram by:
- Automatically tracking all messages
- Organizing conversations by priority
- Managing contacts with business information
- Setting follow-up reminders
- Providing search across all conversations

## 📁 Project Structure

```
Telegcrm/
├── Models/
│   ├── TelegramConversation.cs    # Conversation/chat tracking
│   ├── TelegramMessage.cs         # Individual message storage
│   ├── TelegramContact.cs          # Contact management
│   └── FollowUp.cs                  # Follow-up reminders
├── Services/
│   └── TelegramCrmService.cs        # Core CRM business logic
├── Controllers/
│   └── TelegramCrmController.cs     # REST API endpoints
├── Integration/
│   └── TelegramCrmIntegration.cs   # Bot integration helper
├── Telegcrm.csproj                  # Project file
└── README.md                         # This file
```

## 🚀 Quick Start

### 1. Prerequisites

- .NET 8.0 SDK
- MongoDB (local or cloud instance)
- Existing OASIS Telegram integration

### 2. Configuration

Add to your `OASIS_DNA.json` or app settings:

```json
{
  "TelegramCRM": {
    "MongoConnectionString": "mongodb://localhost:27017",
    "OasisAvatarId": "your-avatar-guid-here"
  }
}
```

### 3. Integration with Telegram Bot

In your `TelegramBotService.cs`, add CRM integration:

```csharp
using Telegcrm.Services;
using Telegcrm.Integration;

// In your service initialization:
var crmService = new TelegramCrmService(mongoConnectionString);
var crmIntegration = new TelegramCrmIntegration(
    crmService,
    botClient,
    oasisAvatarId
);

// In your message handler:
public async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
{
    // Existing bot logic...
    
    // NEW: Store message in CRM
    await _crmIntegration.HandleIncomingMessageAsync(message);
    
    // Continue with your existing bot logic...
}
```

### 4. Register Services (in Startup.cs or Program.cs)

```csharp
using Telegcrm.Services;
using Telegcrm.Controllers;

// Add CRM service
var mongoConnectionString = Configuration["TelegramCRM:MongoConnectionString"];
services.AddSingleton(new TelegramCrmService(mongoConnectionString));

// Register controller (if using separate project, add reference)
```

### 5. Build and Run

```bash
cd Telegcrm
dotnet build
dotnet run
```

## 📡 API Endpoints

### Conversations

- `GET /api/telegram-crm/conversations?oasisAvatarId={guid}&status={status}` - List conversations
- `GET /api/telegram-crm/conversations/{id}` - Get conversation details
- `GET /api/telegram-crm/conversations/{id}/messages?limit=100` - Get messages
- `POST /api/telegram-crm/conversations/{id}/read` - Mark as read
- `POST /api/telegram-crm/conversations/{id}/priority` - Set priority
- `POST /api/telegram-crm/conversations/{id}/tags` - Add tag
- `GET /api/telegram-crm/conversations/search?oasisAvatarId={guid}&keyword={keyword}` - Search

### Contacts

- `GET /api/telegram-crm/contacts` - List all contacts

### Follow-ups

- `POST /api/telegram-crm/followups` - Create follow-up
- `GET /api/telegram-crm/followups/pending?oasisAvatarId={guid}` - Get pending

## 📊 Example Usage

### Get all unread conversations

```bash
curl "http://localhost:5000/api/telegram-crm/conversations?oasisAvatarId=YOUR-GUID&status=active" \
  -H "Authorization: Bearer YOUR_TOKEN"
```

### Set conversation priority

```bash
curl -X POST "http://localhost:5000/api/telegram-crm/conversations/CONV-ID/priority" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{"priority": "high"}'
```

### Create follow-up reminder

```bash
curl -X POST "http://localhost:5000/api/telegram-crm/followups" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -d '{
    "conversationId": "conv-123",
    "oasisAvatarId": "your-guid",
    "title": "Reply about payment",
    "description": "Follow up on invoice #1234",
    "dueDate": "2025-01-20T14:00:00Z",
    "priority": "high"
  }'
```

## 🔧 Features

### Automatic Message Tracking
- All incoming/outgoing messages are automatically stored
- Messages linked to conversations
- Full message history preserved

### Contact Management
- Auto-creates contacts from conversations
- Stores business information (company, email, etc.)
- Tracks relationship status

### Priority System
- Auto-detects priority from keywords
- Manual priority setting
- Priority-based sorting

### Follow-up Reminders
- Set reminders for conversations
- Track due dates
- Get pending follow-ups

### Search
- Full-text search across messages
- Filter by status, priority, tags
- Find conversations quickly

## 🗄️ Database Collections

The system creates these MongoDB collections:
- `conversations` - Conversation metadata
- `messages` - Individual messages
- `contacts` - Contact information
- `followups` - Follow-up reminders

## 🔐 Security Notes

- All endpoints should be protected with OASIS authentication
- MongoDB connection string should be kept secure
- Consider adding rate limiting for production

## 🚧 Next Steps

This is a simple MVP. Future enhancements could include:
- Web dashboard UI
- AI-powered summarization
- Sentiment analysis
- Advanced analytics
- Mobile app
- Multi-platform support (WhatsApp, Discord)

## 📝 License

MIT License - Part of OASIS ecosystem

## 🤝 Contributing

This is part of the OASIS project. Contributions welcome!

---

**Status**: MVP - Ready for integration and testing

