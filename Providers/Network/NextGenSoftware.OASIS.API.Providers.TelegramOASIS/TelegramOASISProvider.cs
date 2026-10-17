using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Telegram.Bot;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models;
using Achievement = NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Models.Achievement;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS
{
    /// <summary>
    /// TelegramOASIS Provider - Integrates Telegram messaging platform with OASIS.
    /// Stores Telegram user mappings, group data, and achievement tracking.
    /// </summary>
    public class TelegramOASISProvider : OASISStorageProviderBase, IOASISStorageProvider
    {
        private readonly string _botToken;
        private readonly string _webhookUrl;
        private readonly string _mongoConnectionString;
        private TelegramBotClient _botClient;
        private IMongoDatabase _database;
        private IMongoCollection<TelegramAvatar> _telegramAvatars;
        private IMongoCollection<TelegramGroup> _telegramGroups;
        private IMongoCollection<Achievement> _achievements;
        private IMongoCollection<BsonDocument> _oasisAvatars;
        private IMongoCollection<BsonDocument> _oasisHolons;

        public TelegramOASISProvider(string botToken, string webhookUrl, string mongoConnectionString)
        {
            this.ProviderName = "TelegramOASIS";
            this.ProviderDescription = "Telegram Provider for social accountability and achievement tracking";

            _botToken = botToken;
            _webhookUrl = webhookUrl;
            _mongoConnectionString = mongoConnectionString;
        }

        #region Provider Activation/Deactivation

        public override OASISResult<bool> ActivateProvider()
        {
            var result = new OASISResult<bool>();

            try
            {
                if (!this.IsProviderActivated)
                {
                    if (string.IsNullOrEmpty(_botToken))
                    {
                        OASISErrorHandling.HandleError(ref result, "Telegram bot token is required");
                        return result;
                    }

                    if (string.IsNullOrEmpty(_mongoConnectionString))
                    {
                        OASISErrorHandling.HandleError(ref result, "MongoDB connection string is required");
                        return result;
                    }

                    _botClient = new TelegramBotClient(_botToken);

                    var mongoClient = new MongoClient(_mongoConnectionString);
                    _database = mongoClient.GetDatabase("TelegramOASIS");
                    _telegramAvatars = _database.GetCollection<TelegramAvatar>("telegram_avatars");
                    _telegramGroups = _database.GetCollection<TelegramGroup>("telegram_groups");
                    _achievements = _database.GetCollection<Achievement>("achievements");

                    CreateIndexes();

                    this.IsProviderActivated = true;
                    result.Result = true;
                }
            }
            catch (Exception ex)
            {
                this.IsProviderActivated = false;
                OASISErrorHandling.HandleError(ref result, $"Error activating TelegramOASIS Provider: {ex.Message}");
            }

            return result;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            return await Task.FromResult(ActivateProvider());
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            _botClient = null;
            _database = null;
            _telegramAvatars = null;
            _telegramGroups = null;
            _achievements = null;
            IsProviderActivated = false;
            return new OASISResult<bool>(value: true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            return await Task.FromResult(DeActivateProvider());
        }

        private void CreateIndexes()
        {
            var telegramIdIndex = Builders<TelegramAvatar>.IndexKeys.Ascending(x => x.TelegramId);
            _telegramAvatars.Indexes.CreateOne(new CreateIndexModel<TelegramAvatar>(telegramIdIndex));

            var avatarIdIndex = Builders<TelegramAvatar>.IndexKeys.Ascending(x => x.OasisAvatarId);
            _telegramAvatars.Indexes.CreateOne(new CreateIndexModel<TelegramAvatar>(avatarIdIndex));

            var achievementUserIndex = Builders<Achievement>.IndexKeys.Ascending(x => x.UserId);
            _achievements.Indexes.CreateOne(new CreateIndexModel<Achievement>(achievementUserIndex));

            var achievementGroupIndex = Builders<Achievement>.IndexKeys.Ascending(x => x.GroupId);
            _achievements.Indexes.CreateOne(new CreateIndexModel<Achievement>(achievementGroupIndex));

            _oasisAvatars = _database.GetCollection<BsonDocument>("oasis_avatars");
            _oasisHolons = _database.GetCollection<BsonDocument>("oasis_holons");
        }

        #endregion

        #region Telegram-Specific Methods

        public async Task<OASISResult<TelegramAvatar>> LinkTelegramToAvatarAsync(long telegramId, string telegramUsername, string firstName, string lastName, Guid oasisAvatarId)
        {
            var result = new OASISResult<TelegramAvatar>();

            try
            {
                var existingLink = await _telegramAvatars.Find(x => x.TelegramId == telegramId).FirstOrDefaultAsync();

                if (existingLink != null)
                {
                    existingLink.LastInteractionAt = DateTime.UtcNow;
                    await _telegramAvatars.ReplaceOneAsync(x => x.Id == existingLink.Id, existingLink);
                    result.Result = existingLink;
                    return result;
                }

                var telegramAvatar = new TelegramAvatar
                {
                    TelegramId = telegramId,
                    TelegramUsername = telegramUsername,
                    FirstName = firstName,
                    LastName = lastName,
                    OasisAvatarId = oasisAvatarId
                };

                await _telegramAvatars.InsertOneAsync(telegramAvatar);
                result.Result = telegramAvatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error linking Telegram to Avatar: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<TelegramAvatar>> GetTelegramAvatarByTelegramIdAsync(long telegramId)
        {
            var result = new OASISResult<TelegramAvatar>();

            try
            {
                var telegramAvatar = await _telegramAvatars.Find(x => x.TelegramId == telegramId).FirstOrDefaultAsync();

                if (telegramAvatar == null)
                    OASISErrorHandling.HandleError(ref result, $"Telegram user {telegramId} not found");
                else
                    result.Result = telegramAvatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Telegram avatar: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<TelegramAvatar>> GetTelegramAvatarByOASISIdAsync(Guid oasisAvatarId)
        {
            var result = new OASISResult<TelegramAvatar>();

            try
            {
                var telegramAvatar = await _telegramAvatars.Find(x => x.OasisAvatarId == oasisAvatarId).FirstOrDefaultAsync();

                if (telegramAvatar == null)
                    OASISErrorHandling.HandleError(ref result, $"OASIS Avatar {oasisAvatarId} not linked to Telegram");
                else
                    result.Result = telegramAvatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Telegram avatar: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<TelegramGroup>> CreateGroupAsync(string name, string description, Guid createdBy, long telegramChatId)
        {
            var result = new OASISResult<TelegramGroup>();

            try
            {
                var group = new TelegramGroup
                {
                    Name = name,
                    Description = description,
                    CreatedBy = createdBy,
                    TelegramChatId = telegramChatId
                };

                await _telegramGroups.InsertOneAsync(group);
                result.Result = group;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating group: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<TelegramGroup>> GetGroupAsync(string groupId)
        {
            var result = new OASISResult<TelegramGroup>();

            try
            {
                var group = await _telegramGroups.Find(x => x.Id == groupId).FirstOrDefaultAsync();

                if (group == null)
                    OASISErrorHandling.HandleError(ref result, $"Group {groupId} not found");
                else
                    result.Result = group;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting group: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> AddMemberToGroupAsync(string groupId, long telegramUserId)
        {
            var result = new OASISResult<bool>();

            try
            {
                var filter = Builders<TelegramGroup>.Filter.Eq(x => x.Id, groupId);
                var update = Builders<TelegramGroup>.Update.AddToSet(x => x.MemberIds, telegramUserId);

                await _telegramGroups.UpdateOneAsync(filter, update);

                var userFilter = Builders<TelegramAvatar>.Filter.Eq(x => x.TelegramId, telegramUserId);
                var userUpdate = Builders<TelegramAvatar>.Update.AddToSet(x => x.GroupIds, groupId);
                await _telegramAvatars.UpdateOneAsync(userFilter, userUpdate);

                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding member to group: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Achievement>> CreateAchievementAsync(Achievement achievement)
        {
            var result = new OASISResult<Achievement>();

            try
            {
                await _achievements.InsertOneAsync(achievement);
                result.Result = achievement;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating achievement: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<List<Achievement>>> GetUserAchievementsAsync(Guid userId)
        {
            var result = new OASISResult<List<Achievement>>();

            try
            {
                var achievements = await _achievements.Find(x => x.UserId == userId).ToListAsync();
                result.Result = achievements;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting user achievements: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<List<Achievement>>> GetGroupAchievementsAsync(string groupId)
        {
            var result = new OASISResult<List<Achievement>>();

            try
            {
                var achievements = await _achievements.Find(x => x.GroupId == groupId).ToListAsync();
                result.Result = achievements;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting group achievements: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Achievement>> UpdateAchievementStatusAsync(string achievementId, AchievementStatus status, long? verifiedBy = null)
        {
            var result = new OASISResult<Achievement>();

            try
            {
                var filter = Builders<Achievement>.Filter.Eq(x => x.Id, achievementId);
                var update = Builders<Achievement>.Update
                    .Set(x => x.Status, status)
                    .Set(x => x.CompletedAt, status == AchievementStatus.Completed ? DateTime.UtcNow : (DateTime?)null);

                if (verifiedBy.HasValue)
                    update = update.Set(x => x.VerifiedBy, verifiedBy.Value);

                var achievement = await _achievements.FindOneAndUpdateAsync(filter, update,
                    new FindOneAndUpdateOptions<Achievement> { ReturnDocument = ReturnDocument.After });

                result.Result = achievement;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error updating achievement: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<Achievement>> AddCheckInAsync(string achievementId, string message, int karmaAwarded)
        {
            var result = new OASISResult<Achievement>();

            try
            {
                var checkIn = new CheckIn
                {
                    Message = message,
                    KarmaAwarded = karmaAwarded
                };

                var filter = Builders<Achievement>.Filter.Eq(x => x.Id, achievementId);
                var update = Builders<Achievement>.Update.Push(x => x.Checkins, checkIn);

                var achievement = await _achievements.FindOneAndUpdateAsync(filter, update,
                    new FindOneAndUpdateOptions<Achievement> { ReturnDocument = ReturnDocument.After });

                result.Result = achievement;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding check-in: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> SendMessageAsync(long chatId, string message)
        {
            var result = new OASISResult<bool>();

            try
            {
                if (_botClient == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Telegram bot not initialized");
                    return result;
                }

                await _botClient.SendTextMessageAsync(chatId, message);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error sending message: {ex.Message}");
            }

            return result;
        }

        public async Task<OASISResult<List<TelegramGroup>>> GetUserGroupsAsync(long telegramUserId)
        {
            var result = new OASISResult<List<TelegramGroup>>();

            try
            {
                var groups = await _telegramGroups.Find(x => x.MemberIds.Contains(telegramUserId)).ToListAsync();
                result.Result = groups;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting user groups: {ex.Message}");
            }

            return result;
        }

        #endregion

        #region Required OASIS Storage Provider Methods

        private static BsonDocument AvatarToBson(IAvatar avatar)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(avatar, new JsonSerializerOptions { WriteIndented = false });
            var doc = BsonDocument.Parse(json);
            doc["_id"] = avatar.Id.ToString();
            return doc;
        }

        private static BsonDocument HolonToBson(IHolon holon)
        {
            var json = System.Text.Json.JsonSerializer.Serialize(holon, new JsonSerializerOptions { WriteIndented = false });
            var doc = BsonDocument.Parse(json);
            doc["_id"] = holon.Id.ToString();
            return doc;
        }

        private static IAvatar BsonToAvatar(BsonDocument doc)
        {
            var json = doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson });
            return System.Text.Json.JsonSerializer.Deserialize<Avatar>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        private static IHolon BsonToHolon(BsonDocument doc)
        {
            var json = doc.ToJson(new MongoDB.Bson.IO.JsonWriterSettings { OutputMode = MongoDB.Bson.IO.JsonOutputMode.RelaxedExtendedJson });
            return System.Text.Json.JsonSerializer.Deserialize<Holon>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await _oasisAvatars.Find(Builders<BsonDocument>.Filter.Eq("_id", id.ToString())).FirstOrDefaultAsync();
                if (doc == null) OASISErrorHandling.HandleError(ref result, $"Avatar {id} not found in TelegramOASIS.");
                else { result.Result = BsonToAvatar(doc); result.IsError = false; }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = AvatarToBson(avatar);
                await _oasisAvatars.ReplaceOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", avatar.Id.ToString()),
                    doc, new ReplaceOptions { IsUpsert = true });
                result.Result = avatar; result.IsError = false;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                var docs = await _oasisAvatars.Find(FilterDefinition<BsonDocument>.Empty).ToListAsync();
                result.Result = docs.Select(BsonToAvatar).ToList();
                result.IsError = false;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading all avatars: {ex.Message}", ex); }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (softDelete)
                {
                    var doc = await _oasisAvatars.Find(Builders<BsonDocument>.Filter.Eq("_id", id.ToString())).FirstOrDefaultAsync();
                    if (doc != null)
                    {
                        doc["DeletedDate"] = DateTime.UtcNow.ToString("o");
                        await _oasisAvatars.ReplaceOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id.ToString()), doc);
                    }
                }
                else
                {
                    await _oasisAvatars.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id.ToString()));
                }
                result.Result = true; result.IsError = false;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
            => new OASISResult<IAvatar> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override OASISResult<IAvatar> LoadAvatarByUsername(string username, int version = 0)
            => new OASISResult<IAvatar> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string username, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await _oasisAvatars.Find(Builders<BsonDocument>.Filter.Eq("Username", username)).FirstOrDefaultAsync();
                if (doc == null) OASISErrorHandling.HandleError(ref result, $"Avatar with username '{username}' not found.");
                else { result.Result = BsonToAvatar(doc); result.IsError = false; }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string email, int version = 0)
            => new OASISResult<IAvatar> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string email, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await _oasisAvatars.Find(Builders<BsonDocument>.Filter.Eq("Email", email)).FirstOrDefaultAsync();
                if (doc == null) OASISErrorHandling.HandleError(ref result, $"Avatar with email '{email}' not found.");
                else { result.Result = BsonToAvatar(doc); result.IsError = false; }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
            => new OASISResult<IAvatar> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                var doc = await _oasisAvatars.Find(Builders<BsonDocument>.Filter.Eq("providerKey", providerKey)).FirstOrDefaultAsync();
                if (doc == null) OASISErrorHandling.HandleError(ref result, $"Avatar with providerKey '{providerKey}' not found.");
                else { result.Result = BsonToAvatar(doc); result.IsError = false; }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string username, int version = 0)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string username, int version = 0)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string email, int version = 0)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string email, int version = 0)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
            => new OASISResult<IEnumerable<IAvatar>> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
            => new OASISResult<IEnumerable<IAvatarDetail>> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IAvatarDetail>> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
            => new OASISResult<IAvatar> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatarDetail)
            => new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatarDetail)
            => Task.FromResult(new OASISResult<IAvatarDetail> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<bool> DeleteAvatarByEmail(string email, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string email, bool softDelete = true)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<bool> DeleteAvatarByUsername(string username, bool softDelete = true)
            => new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." };

        public override Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string username, bool softDelete = true)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for avatar operations." });

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = await _oasisHolons.Find(Builders<BsonDocument>.Filter.Eq("_id", id.ToString())).FirstOrDefaultAsync();
                if (doc == null) OASISErrorHandling.HandleError(ref result, $"Holon {id} not found.");
                else { result.Result = BsonToHolon(doc); result.IsError = false; }
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                var filter = type == HolonType.All
                    ? FilterDefinition<BsonDocument>.Empty
                    : Builders<BsonDocument>.Filter.Eq("HolonType", type.ToString());
                var docs = await _oasisHolons.Find(filter).ToListAsync();
                result.Result = docs.Select(BsonToHolon).ToList();
                result.IsError = false;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var doc = HolonToBson(holon);
                await _oasisHolons.ReplaceOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", holon.Id.ToString()),
                    doc, new ReplaceOptions { IsUpsert = true });
                result.Result = holon; result.IsError = false;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error saving holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<IHolon> DeleteHolon(Guid id)
            => new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                await _oasisHolons.DeleteOneAsync(Builders<BsonDocument>.Filter.Eq("_id", id.ToString()));
                result.IsError = false;
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex); }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
            => new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
            => Task.FromResult(new OASISResult<IHolon> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => new OASISResult<ISearchResults> { IsError = true, Message = "Use MongoDBOASIS for search operations." };

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
            => Task.FromResult(new OASISResult<ISearchResults> { IsError = true, Message = "Use MongoDBOASIS for search operations." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaData, MetaKeyValuePairMatchMode matchMode = MetaKeyValuePairMatchMode.All, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for holon operations." });

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
            => new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for import operations." };

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
            => Task.FromResult(new OASISResult<bool> { IsError = true, Message = "Use MongoDBOASIS for import operations." });

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." });

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." });

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." });

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
            => new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." };

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
            => Task.FromResult(new OASISResult<IEnumerable<IHolon>> { IsError = true, Message = "Use MongoDBOASIS for export operations." });

        #endregion
    }
}
