using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using Telegcrm.Models;
using Telegram.Bot.Types;

namespace Telegcrm.Services
{
    /// <summary>
    /// Service for managing Telegram CRM functionality
    /// </summary>
    public class TelegramCrmService
    {
        private readonly IMongoDatabase _database;
        private readonly IMongoCollection<TelegramConversation> _conversations;
        private readonly IMongoCollection<TelegramMessage> _messages;
        private readonly IMongoCollection<TelegramContact> _contacts;
        private readonly IMongoCollection<FollowUp> _followUps;

        public TelegramCrmService(string mongoConnectionString)
        {
            try
            {
                var mongoClient = new MongoClient(mongoConnectionString);
                _database = mongoClient.GetDatabase("TelegramCRM");
                
                _conversations = _database.GetCollection<TelegramConversation>("conversations");
                _messages = _database.GetCollection<TelegramMessage>("messages");
                _contacts = _database.GetCollection<TelegramContact>("contacts");
                _followUps = _database.GetCollection<FollowUp>("followups");

                // Test connection and create indexes
                CreateIndexes();
                Console.WriteLine("✅ MongoDB connected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  MongoDB connection failed: {ex.Message}");
                Console.WriteLine("   Server will start but CRM features will be limited.");
                Console.WriteLine("   Please configure MongoDB connection string.");
                throw; // Re-throw so user knows to fix it
            }
        }

        private void CreateIndexes()
        {
            try
            {
                // Conversation indexes
                _conversations.Indexes.CreateOne(
                    new CreateIndexModel<TelegramConversation>(
                        Builders<TelegramConversation>.IndexKeys.Ascending(c => c.TelegramChatId)
                    )
                );
                _conversations.Indexes.CreateOne(
                    new CreateIndexModel<TelegramConversation>(
                        Builders<TelegramConversation>.IndexKeys.Ascending(c => c.OasisAvatarId)
                    )
                );
                _conversations.Indexes.CreateOne(
                    new CreateIndexModel<TelegramConversation>(
                        Builders<TelegramConversation>.IndexKeys.Ascending(c => c.Status)
                    )
                );

                // Message indexes
                _messages.Indexes.CreateOne(
                    new CreateIndexModel<TelegramMessage>(
                        Builders<TelegramMessage>.IndexKeys.Ascending(m => m.ConversationId)
                    )
                );
                _messages.Indexes.CreateOne(
                    new CreateIndexModel<TelegramMessage>(
                        Builders<TelegramMessage>.IndexKeys.Ascending(m => m.SentAt)
                    )
                );
                _messages.Indexes.CreateOne(
                    new CreateIndexModel<TelegramMessage>(
                        Builders<TelegramMessage>.IndexKeys.Text(m => m.Content)
                    )
                );

                // Contact indexes
                _contacts.Indexes.CreateOne(
                    new CreateIndexModel<TelegramContact>(
                        Builders<TelegramContact>.IndexKeys.Ascending(c => c.TelegramId),
                        new CreateIndexOptions { Unique = true }
                    )
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️  Warning: Could not create indexes: {ex.Message}");
                // Continue anyway - indexes will be created when first used
            }
        }

        /// <summary>
        /// Store a message in the CRM system
        /// </summary>
        public async Task<TelegramMessage> StoreMessageAsync(Message telegramMessage, Guid oasisAvatarId, bool isFromMe)
        {
            // Get or create conversation
            var conversation = await GetOrCreateConversationAsync(
                telegramMessage.Chat.Id,
                telegramMessage.Chat.Type.ToString().ToLower(),
                oasisAvatarId,
                telegramMessage.From
            );

            // Create message record
            var message = new TelegramMessage
            {
                ConversationId = conversation.Id,
                TelegramMessageId = telegramMessage.MessageId,
                FromTelegramId = telegramMessage.From?.Id ?? 0,
                FromName = $"{telegramMessage.From?.FirstName} {telegramMessage.From?.LastName}".Trim(),
                Content = telegramMessage.Text ?? telegramMessage.Caption ?? "",
                MessageType = GetMessageType(telegramMessage),
                SentAt = telegramMessage.Date.ToUniversalTime(),
                IsFromMe = isFromMe,
                IsRead = isFromMe // Messages from me are automatically "read"
            };

            // Extract media URLs if present
            if (telegramMessage.Photo != null && telegramMessage.Photo.Any())
            {
                message.MediaUrls.Add($"photo_{telegramMessage.Photo.Last().FileId}");
            }
            if (telegramMessage.Document != null)
            {
                message.MediaUrls.Add($"document_{telegramMessage.Document.FileId}");
            }

            await _messages.InsertOneAsync(message);

            // Update conversation
            conversation.LastMessageAt = message.SentAt;
            conversation.UpdatedAt = DateTime.UtcNow;
            if (!isFromMe)
            {
                conversation.UnreadCount++;
            }
            else
            {
                conversation.UnreadCount = 0; // Reset when we send a message
            }

            await _conversations.ReplaceOneAsync(
                c => c.Id == conversation.Id,
                conversation
            );

            // Update or create contact
            if (telegramMessage.From != null && !isFromMe)
            {
                await UpdateOrCreateContactAsync(telegramMessage.From, conversation.Id);
            }

            return message;
        }

        /// <summary>
        /// Get or create a conversation
        /// </summary>
        public async Task<TelegramConversation> GetOrCreateConversationAsync(
            long chatId,
            string chatType,
            Guid oasisAvatarId,
            User fromUser = null)
        {
            var existing = await _conversations.Find(
                c => c.TelegramChatId == chatId && c.OasisAvatarId == oasisAvatarId
            ).FirstOrDefaultAsync();

            if (existing != null)
            {
                return existing;
            }

            // Create new conversation
            var conversation = new TelegramConversation
            {
                TelegramChatId = chatId,
                ChatType = chatType,
                OasisAvatarId = oasisAvatarId
            };

            if (fromUser != null && chatType == "private")
            {
                conversation.ContactTelegramId = fromUser.Id;
                conversation.ContactName = $"{fromUser.FirstName} {fromUser.LastName}".Trim();
                conversation.ContactUsername = fromUser.Username;
            }

            // Auto-detect priority from keywords
            conversation.Priority = DetectPriority(fromUser?.Username ?? "");

            await _conversations.InsertOneAsync(conversation);
            return conversation;
        }

        /// <summary>
        /// Update or create a contact
        /// </summary>
        public async Task<TelegramContact> UpdateOrCreateContactAsync(User telegramUser, string conversationId)
        {
            var existing = await _contacts.Find(c => c.TelegramId == telegramUser.Id).FirstOrDefaultAsync();

            if (existing != null)
            {
                // Update last contacted
                existing.LastContactedAt = DateTime.UtcNow;
                existing.TotalMessages++;
                existing.UpdatedAt = DateTime.UtcNow;
                existing.RelationshipStatus = "active";

                await _contacts.ReplaceOneAsync(c => c.Id == existing.Id, existing);
                return existing;
            }

            // Create new contact
            var contact = new TelegramContact
            {
                TelegramId = telegramUser.Id,
                FirstName = telegramUser.FirstName,
                LastName = telegramUser.LastName,
                Username = telegramUser.Username,
                PhoneNumber = null, // Telegram User doesn't expose phone number directly
                LastContactedAt = DateTime.UtcNow,
                TotalMessages = 1,
                RelationshipStatus = "new"
            };

            await _contacts.InsertOneAsync(contact);
            return contact;
        }

        /// <summary>
        /// Get all conversations for a user
        /// </summary>
        public async Task<List<TelegramConversation>> GetConversationsAsync(Guid oasisAvatarId, string status = null)
        {
            var filter = Builders<TelegramConversation>.Filter.Eq(c => c.OasisAvatarId, oasisAvatarId);
            
            if (!string.IsNullOrEmpty(status))
            {
                filter &= Builders<TelegramConversation>.Filter.Eq(c => c.Status, status);
            }

            return await _conversations.Find(filter)
                .SortByDescending(c => c.LastMessageAt)
                .ToListAsync();
        }

        /// <summary>
        /// Get messages for a conversation
        /// </summary>
        public async Task<List<TelegramMessage>> GetMessagesAsync(string conversationId, int limit = 100)
        {
            return await _messages.Find(m => m.ConversationId == conversationId)
                .SortByDescending(m => m.SentAt)
                .Limit(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Mark conversation as read
        /// </summary>
        public async Task MarkConversationAsReadAsync(string conversationId)
        {
            var update = Builders<TelegramConversation>.Update
                .Set(c => c.LastReadAt, DateTime.UtcNow)
                .Set(c => c.UnreadCount, 0)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            await _conversations.UpdateOneAsync(
                c => c.Id == conversationId,
                update
            );

            // Mark all messages as read
            var messageUpdate = Builders<TelegramMessage>.Update
                .Set(m => m.IsRead, true);

            await _messages.UpdateManyAsync(
                m => m.ConversationId == conversationId && !m.IsRead,
                messageUpdate
            );
        }

        /// <summary>
        /// Set conversation priority
        /// </summary>
        public async Task SetConversationPriorityAsync(string conversationId, string priority)
        {
            var update = Builders<TelegramConversation>.Update
                .Set(c => c.Priority, priority)
                .Set(c => c.UpdatedAt, DateTime.UtcNow);

            await _conversations.UpdateOneAsync(
                c => c.Id == conversationId,
                update
            );
        }

        /// <summary>
        /// Add tag to conversation
        /// </summary>
        public async Task AddTagToConversationAsync(string conversationId, string tag)
        {
            var conversation = await _conversations.Find(c => c.Id == conversationId).FirstOrDefaultAsync();
            if (conversation != null && !conversation.Tags.Contains(tag))
            {
                conversation.Tags.Add(tag);
                conversation.UpdatedAt = DateTime.UtcNow;
                await _conversations.ReplaceOneAsync(c => c.Id == conversationId, conversation);
            }
        }

        /// <summary>
        /// Create a follow-up reminder
        /// </summary>
        public async Task<FollowUp> CreateFollowUpAsync(
            string conversationId,
            Guid oasisAvatarId,
            string title,
            string description,
            DateTime dueDate,
            string priority = "medium")
        {
            var followUp = new FollowUp
            {
                ConversationId = conversationId,
                CreatedByOasisAvatarId = oasisAvatarId,
                Title = title,
                Description = description,
                DueDate = dueDate,
                Priority = priority
            };

            await _followUps.InsertOneAsync(followUp);
            return followUp;
        }

        /// <summary>
        /// Get pending follow-ups
        /// </summary>
        public async Task<List<FollowUp>> GetPendingFollowUpsAsync(Guid oasisAvatarId)
        {
            return await _followUps.Find(
                f => f.CreatedByOasisAvatarId == oasisAvatarId 
                     && !f.IsCompleted 
                     && f.DueDate >= DateTime.UtcNow
            )
            .SortBy(f => f.DueDate)
            .ToListAsync();
        }

        /// <summary>
        /// Search conversations by keyword
        /// </summary>
        public async Task<List<TelegramConversation>> SearchConversationsAsync(
            Guid oasisAvatarId,
            string keyword)
        {
            // Search in message content
            var messageIds = await _messages.Find(
                Builders<TelegramMessage>.Filter.Text(keyword)
            )
            .Project(m => m.ConversationId)
            .ToListAsync();

            var conversationIds = messageIds.Distinct().ToList();

            return await _conversations.Find(
                c => c.OasisAvatarId == oasisAvatarId 
                     && conversationIds.Contains(c.Id)
            )
            .SortByDescending(c => c.LastMessageAt)
            .ToListAsync();
        }

        /// <summary>
        /// Get all contacts
        /// </summary>
        public async Task<List<TelegramContact>> GetContactsAsync(Guid? oasisAvatarId = null)
        {
            var filter = Builders<TelegramContact>.Filter.Empty;
            // Could filter by linked avatar if needed

            return await _contacts.Find(filter)
                .SortByDescending(c => c.LastContactedAt)
                .ToListAsync();
        }

        // Helper methods

        private string GetMessageType(Message message)
        {
            if (message.Text != null) return "text";
            if (message.Photo != null) return "photo";
            if (message.Document != null) return "document";
            if (message.Video != null) return "video";
            if (message.Audio != null) return "audio";
            if (message.Voice != null) return "voice";
            if (message.Sticker != null) return "sticker";
            return "unknown";
        }

        private string DetectPriority(string username)
        {
            var urgentKeywords = new[] { "urgent", "asap", "important", "critical" };
            var usernameLower = username.ToLower();
            
            if (urgentKeywords.Any(k => usernameLower.Contains(k)))
            {
                return "urgent";
            }

            return "medium";
        }
    }
}

