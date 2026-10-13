using System;
using MongoDB.Bson;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Infrastructure.Singleton;
using NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Interfaces;
using Avatar = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.Avatar;
using AvatarDetail = NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Entities.AvatarDetail;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.MongoDBOASIS.Repositories
{
    public class AvatarRepository : IAvatarRepository
    {
        private MongoDbContext _dbContext;

        public void EnsurePublicIdentityIndexes()
        {
            _dbContext.Avatar.Indexes.CreateOne(new CreateIndexModel<Avatar>(
                Builders<Avatar>.IndexKeys.Ascending(x => x.HolonId), new CreateIndexOptions { Unique = true, Name = "oasis_avatar_public_identity" }));
            _dbContext.AvatarDetail.Indexes.CreateOne(new CreateIndexModel<AvatarDetail>(
                Builders<AvatarDetail>.IndexKeys.Ascending(x => x.HolonId), new CreateIndexOptions { Unique = true, Name = "oasis_avatar_detail_public_identity" }));
        }

        public async Task EnsurePublicIdentityIndexesAsync()
        {
            await _dbContext.Avatar.Indexes.CreateOneAsync(new CreateIndexModel<Avatar>(
                Builders<Avatar>.IndexKeys.Ascending(x => x.HolonId), new CreateIndexOptions { Unique = true, Name = "oasis_avatar_public_identity" }));
            await _dbContext.AvatarDetail.Indexes.CreateOneAsync(new CreateIndexModel<AvatarDetail>(
                Builders<AvatarDetail>.IndexKeys.Ascending(x => x.HolonId), new CreateIndexOptions { Unique = true, Name = "oasis_avatar_detail_public_identity" }));
        }

        public AvatarRepository(MongoDbContext dbContext)
        {
            SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
            SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
            SerializerRegister.GetInstance().RegisterSTARNETDNADiscriminator();
            _dbContext = dbContext;
        }

        public async Task<OASISResult<Avatar>> AddAsync(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType ??= new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                if (string.IsNullOrEmpty(avatar.Id)) avatar.Id = ObjectId.GenerateNewId().ToString();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
                await _dbContext.Avatar.InsertOneAsync(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in AddAsync method in AvatarRepository creating Avatar. Reason: {ex}", ex);
            }

            return result;
        }

        public async Task<OASISResult<AvatarDetail>> AddAsync(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType ??= new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                if (string.IsNullOrEmpty(avatar.Id)) avatar.Id = ObjectId.GenerateNewId().ToString();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
                await _dbContext.AvatarDetail.InsertOneAsync(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in AddAsync method in AvatarRepository creating Avatar. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<Avatar> Add(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType ??= new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                if (string.IsNullOrEmpty(avatar.Id)) avatar.Id = ObjectId.GenerateNewId().ToString();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
                _dbContext.Avatar.InsertOne(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in Add method in AvatarRepository creating Avatar. Reason: {ex}", ex);
            }

            return result;
        }

        public OASISResult<AvatarDetail> Add(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType ??= new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                if (string.IsNullOrEmpty(avatar.Id)) avatar.Id = ObjectId.GenerateNewId().ToString();
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
                _dbContext.AvatarDetail.InsertOne(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in Add method in AvatarRepository loading Avatar. Reason: {ex}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Avatar>> GetAvatarAsync(Guid id)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                result.Result = await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarAsync method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<Avatar> GetAvatar(Guid id)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                result.Result = _dbContext.Avatar.Find(filter).FirstOrDefault();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<Avatar> GetAvatar(Expression<Func<Avatar, bool>> expression)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                var filter = Builders<Avatar>.Filter.Where(expression);
                result.Result = _dbContext.Avatar.Find(filter).FirstOrDefault();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: {ex}", ex);
            }

            return result;
        }
        
        public async Task<OASISResult<Avatar>> GetAvatarAsync(Expression<Func<Avatar, bool>> expression)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                var filter = Builders<Avatar>.Filter.Where(expression);
                result.Result = await _dbContext.Avatar.Find(filter).FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarAsync method in AvatarRepository loading Avatar. Reason: {ex}", ex);
            }

            return result;
        }
        
        //TODO: Upgrade all methods to OASISResult ASAP.
        public async Task<OASISResult<Avatar>> GetAvatarAsync(string username)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                //TODO: Find out how mongo sorts descending by date! It works for non async fine (below)!
                //FilterDefinition<Avatar> filter = Builders<Avatar>.Sort.Descending(x => x.CreatedDate).(x => x.Username == username);
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                //return await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();
                List<Avatar> avatars = await _dbContext.Avatar.FindAsync(filter).Result.ToListAsync();

                //Temp workaround till can find out how mongo sorts async collections!
                avatars.Sort((x, y) => x.CreatedDate.CompareTo(y.CreatedDate));
                avatars.Reverse();

                if (avatars.Count > 0)
                    result.Result = avatars[0];
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarAsync method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<Avatar> GetAvatar(string username)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                result.Result = _dbContext.Avatar.Find(filter).SortByDescending(x => x.CreatedDate).FirstOrDefault();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Avatar>> GetAvatarAsync(string username, string password)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
                Avatar avatar = await _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefaultAsync();

                if (avatar != null && password != avatar.Password)
                    return null;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarAsync method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }

        //public OASISResult<Avatar> GetAvatar(string username, string password)
        //{
        //    OASISResult<Avatar> result = new OASISResult<Avatar>();

        //    try
        //    {
        //        FilterDefinition<Avatar> filter = Builders<Avatar>.Filter.Where(x => x.Username == username);
        //        Avatar avatar = _dbContext.Avatar.FindAsync(filter).Result.FirstOrDefault();

        //        if (avatar != null && password != avatar.Password)
        //            OASISErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: Avatar Password Is Incorrect.");
        //        else
        //            result.Result = avatar;
        //    }
        //    catch
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: {ex}");
        //    }

        //    return result;
        //}

        public async Task<OASISResult<IEnumerable<Avatar>>> GetAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<Avatar>>();
            try
            {
                result.Result = await _dbContext.Avatar.Find(x => version == 0 || x.Version == version).ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar list lookup failed.", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<Avatar>> GetAvatars(int version = 0)
        {
            var result = new OASISResult<IEnumerable<Avatar>>();
            try
            {
                result.Result = _dbContext.Avatar.Find(x => version == 0 || x.Version == version).ToList();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar list lookup failed.", ex);
            }
            return result;
        }

        public async Task<OASISResult<Avatar>> UpdateAsync(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                var replaced = await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                if (!replaced.IsAcknowledged || replaced.MatchedCount != 1)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar update did not acknowledge exactly one matched identity.");
                    return result;
                }
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in UpdateAsync method in AvatarRepository updating Avatar. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<AvatarDetail>> UpdateAsync(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var replaceResult = await _dbContext.AvatarDetail.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar).ConfigureAwait(false);
                if (!replaceResult.IsAcknowledged || replaceResult.MatchedCount == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"AvatarDetail update matched no document (HolonId={avatar.HolonId}). Active quest / XP were not persisted.");
                    return result;
                }

                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in UpdateAsync method in AvatarRepository updating Avatar. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<AvatarDetail> GetAvatarDetail(Expression<Func<AvatarDetail, bool>> expression)
        {
            var result = new OASISResult<AvatarDetail>();
            try
            {
                result.Result = _dbContext.AvatarDetail.Find(expression).FirstOrDefault();
                result.IsLoaded = result.Result != null;
                if (result.Result == null) result.Message = "Avatar detail not found";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar detail lookup failed.", ex);
            }
            return result;
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Expression<Func<AvatarDetail, bool>> expression)
        {
            var result = new OASISResult<AvatarDetail>();
            try
            {
                result.Result = await _dbContext.AvatarDetail.Find(expression).FirstOrDefaultAsync();
                result.IsLoaded = result.Result != null;
                if (result.Result == null) result.Message = "Avatar detail not found";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar detail lookup failed.", ex);
            }
            return result;
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Guid id)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var filter = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                result.Result = await _dbContext.AvatarDetail.Find(filter).FirstOrDefaultAsync();
                result.IsLoaded = result.Result != null;
                if (result.Result == null) result.Message = "Avatar detail not found";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured loading avatar detail in MongoDBOASIS Provider in AvatarRepository.GetAvatarDetailAsync. Reason: {e}", e);
            }

            return result;
        }

        public OASISResult<AvatarDetail> GetAvatarDetail(Guid id)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var filter = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                result.Result = _dbContext.AvatarDetail.Find(filter).FirstOrDefault();
                result.IsLoaded = result.Result != null;
                if (result.Result == null) result.Message = "Avatar detail not found";
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured loading avatar detail in MongoDBOASIS Provider in AvatarRepository.GetAvatarDetail. Reason: {e}", e);
            }

            return result;
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(string username)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var filter = Builders<AvatarDetail>.Filter.Where(x => x.Username == username);
                result.Result = await _dbContext.AvatarDetail.Find(filter).FirstOrDefaultAsync();

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, "Avatar Not Found");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured loading avatar detail in MongoDBOASIS Provider in AvatarRepository.GetAvatarDetailAsync. Reason: {e}");
            }

            return result;
        }

        public OASISResult<AvatarDetail> GetAvatarDetail(string username)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var filter = Builders<AvatarDetail>.Filter.Where(x => x.Username == username);
                result.Result = _dbContext.AvatarDetail.Find(filter).FirstOrDefault();

                if (result.Result == null)
                    OASISErrorHandling.HandleError(ref result, "Avatar Not Found");
            }
            catch (Exception e)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured loading avatar detail in MongoDBOASIS Provider in AvatarRepository.GetAvatarDetail. Reason: {e}");
            }

            return result;
        }

        public async Task<OASISResult<IEnumerable<AvatarDetail>>> GetAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<AvatarDetail>>();
            try
            {
                result.Result = await _dbContext.AvatarDetail.Find(x => version == 0 || x.Version == version).ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar detail list lookup failed.", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<AvatarDetail>> GetAvatarDetails(int version = 0)
        {
            var result = new OASISResult<IEnumerable<AvatarDetail>>();
            try
            {
                result.Result = _dbContext.AvatarDetail.Find(x => version == 0 || x.Version == version).ToList();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar detail list lookup failed.", ex);
            }
            return result;
        }

        public OASISResult<Avatar> Update(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                var replaced = _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                if (!replaced.IsAcknowledged || replaced.MatchedCount != 1)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar update did not acknowledge exactly one matched identity.");
                    return result;
                }
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in Update method in AvatarRepository updating Avatar. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<AvatarDetail> Update(AvatarDetail avatar)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var replaceResult = _dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
                if (!replaceResult.IsAcknowledged || replaceResult.MatchedCount == 0)
                {
                    OASISErrorHandling.HandleError(ref result, $"AvatarDetail update matched no document (HolonId={avatar.HolonId}). Active quest / XP were not persisted.");
                    return result;
                }

                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in Update method in AvatarRepository updating Avatar. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                using var session = await _dbContext.MongoClient.StartSessionAsync();
                session.StartTransaction();
                bool commitAttempted = false;
                try
                {
                    var avatar = await _dbContext.Avatar.Find(session, x => x.HolonId == id).FirstOrDefaultAsync();
                    if (avatar == null) throw new InvalidOperationException("Avatar deletion matched no persisted identity.");
                    if (softDelete)
                    {
                        if (avatar.DeletedDate != DateTime.MinValue)
                            throw new InvalidOperationException("Avatar is already soft deleted.");
                        var detail = await _dbContext.AvatarDetail.Find(session, x => x.HolonId == id).FirstOrDefaultAsync();
                        if (detail == null) throw new InvalidOperationException("Avatar detail deletion matched no persisted identity.");
                        var deletedDate = DateTime.UtcNow;
                        avatar.DeletedDate = deletedDate;
                        detail.DeletedDate = deletedDate;
                        var avatarWrite = await _dbContext.Avatar.ReplaceOneAsync(session, x => x.HolonId == id, avatar);
                        var detailWrite = await _dbContext.AvatarDetail.ReplaceOneAsync(session, x => x.HolonId == id, detail);
                        if (!avatarWrite.IsAcknowledged || avatarWrite.MatchedCount != 1 || !detailWrite.IsAcknowledged || detailWrite.MatchedCount != 1)
                            throw new InvalidOperationException("Soft deletion did not acknowledge both persisted identities.");
                    }
                    else
                    {
                        var avatarWrite = await _dbContext.Avatar.DeleteOneAsync(session, x => x.HolonId == id);
                        var detailWrite = await _dbContext.AvatarDetail.DeleteOneAsync(session, x => x.HolonId == id);
                        if (!avatarWrite.IsAcknowledged || avatarWrite.DeletedCount != 1 || !detailWrite.IsAcknowledged)
                            throw new InvalidOperationException("Permanent deletion was not acknowledged.");
                    }
                    commitAttempted = true;
                    await session.CommitTransactionAsync();
                    result.Result = true;
                }
                catch (Exception ex)
                {
                    Exception evidence = commitAttempted
                        ? new MongoTransactionReconciliationException(session.ServerSession.Id.ToJson(), session.WrappedCoreSession.CurrentTransaction.TransactionNumber, ex)
                        : ex;
                    OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion failed.", evidence);
                    if (session.IsInTransaction) await session.AbortTransactionAsync();
                }
            }
            catch (Exception ex)
            {
                var evidence = result.Exception == null ? ex : new AggregateException(result.Exception, ex);
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion session failed.", evidence);
                result.Result = false;
            }
            return result;
        }

        public OASISResult<bool> Delete(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                using var session = _dbContext.MongoClient.StartSession();
                session.StartTransaction();
                bool commitAttempted = false;
                try
                {
                    var avatar = _dbContext.Avatar.Find(session, x => x.HolonId == id).FirstOrDefault();
                    if (avatar == null) throw new InvalidOperationException("Avatar deletion matched no persisted identity.");
                    if (softDelete)
                    {
                        if (avatar.DeletedDate != DateTime.MinValue)
                            throw new InvalidOperationException("Avatar is already soft deleted.");
                        var detail = _dbContext.AvatarDetail.Find(session, x => x.HolonId == id).FirstOrDefault();
                        if (detail == null) throw new InvalidOperationException("Avatar detail deletion matched no persisted identity.");
                        var deletedDate = DateTime.UtcNow;
                        avatar.DeletedDate = deletedDate;
                        detail.DeletedDate = deletedDate;
                        var avatarWrite = _dbContext.Avatar.ReplaceOne(session, x => x.HolonId == id, avatar);
                        var detailWrite = _dbContext.AvatarDetail.ReplaceOne(session, x => x.HolonId == id, detail);
                        if (!avatarWrite.IsAcknowledged || avatarWrite.MatchedCount != 1 || !detailWrite.IsAcknowledged || detailWrite.MatchedCount != 1)
                            throw new InvalidOperationException("Soft deletion did not acknowledge both persisted identities.");
                    }
                    else
                    {
                        var avatarWrite = _dbContext.Avatar.DeleteOne(session, x => x.HolonId == id);
                        var detailWrite = _dbContext.AvatarDetail.DeleteOne(session, x => x.HolonId == id);
                        if (!avatarWrite.IsAcknowledged || avatarWrite.DeletedCount != 1 || !detailWrite.IsAcknowledged)
                            throw new InvalidOperationException("Permanent deletion was not acknowledged.");
                    }
                    commitAttempted = true;
                    session.CommitTransaction();
                    result.Result = true;
                }
                catch (Exception ex)
                {
                    Exception evidence = commitAttempted
                        ? new MongoTransactionReconciliationException(session.ServerSession.Id.ToJson(), session.WrappedCoreSession.CurrentTransaction.TransactionNumber, ex)
                        : ex;
                    OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion failed.", evidence);
                    if (session.IsInTransaction) session.AbortTransaction();
                }
            }
            catch (Exception ex)
            {
                var evidence = result.Exception == null ? ex : new AggregateException(result.Exception, ex);
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion session failed.", evidence);
                result.Result = false;
            }
            return result;
        }

        public OASISResult<bool> Delete(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = _dbContext.Avatar.Find(expression).FirstOrDefault();
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return Delete(avatar.HolonId, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion selector failed.", ex);
                return result;
            }
        }
        
        public async Task<OASISResult<bool>> DeleteAsync(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await _dbContext.Avatar.Find(expression).FirstOrDefaultAsync();
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return await DeleteAsync(avatar.HolonId, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion selector failed.", ex);
                return result;
            }
        }

        public async Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await _dbContext.Avatar.Find(x => x.Id == providerKey).FirstOrDefaultAsync();
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return await DeleteAsync(avatar.HolonId, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion selector failed.", ex);
                return result;
            }
        }

        public OASISResult<bool> Delete(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = _dbContext.Avatar.Find(x => x.Id == providerKey).FirstOrDefault();
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return Delete(avatar.HolonId, softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "MongoDB avatar deletion selector failed.", ex);
                return result;
            }
        }
    }
}
