using System;
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

        public AvatarRepository(MongoDbContext dbContext)
        {
            SerializerRegister.GetInstance().RegisterGuidBsonSerializer();
            SerializerRegister.GetInstance().RegisterMetaDataDictionarySerializer();
            _dbContext = dbContext;
        }

        public async Task<OASISResult<Avatar>> AddAsync(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                if (avatar.HolonId == Guid.Empty)
                    avatar.HolonId = Guid.NewGuid();

                avatar.CreatedProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                await _dbContext.Avatar.InsertOneAsync(avatar);
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;
                
                await UpdateAsync(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in AddAsync method in AvatarRepository creating Avatar. Reason: {ex}");
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

                avatar.CreatedProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                await _dbContext.AvatarDetail.InsertOneAsync(avatar);
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

                await UpdateAsync(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in AddAsync method in AvatarRepository creating Avatar. Reason: {ex}");
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

                avatar.CreatedProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                _dbContext.Avatar.InsertOne(avatar);
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

                Update(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in Add method in AvatarRepository creating Avatar. Reason: {ex}");
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

                avatar.CreatedProviderType = new EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.MongoDBOASIS);

                _dbContext.AvatarDetail.InsertOne(avatar);
                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] = avatar.Id;

                Update(avatar);
                result.Result = avatar;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in Add method in AvatarRepository loading Avatar. Reason: {ex}");
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
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatar method in AvatarRepository loading Avatar. Reason: {ex}");
            }

            return result;
        }
        
        public async Task<OASISResult<Avatar>> GetAvatarAsync(Expression<Func<Avatar, bool>> expression)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                var filter = Builders<Avatar>.Filter.Where(expression);
                result.Result = _dbContext.Avatar.Find(filter).FirstOrDefault();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarAsync method in AvatarRepository loading Avatar. Reason: {ex}");
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

        public async Task<OASISResult<IEnumerable<Avatar>>> GetAvatarsAsync()
        {
            OASISResult<IEnumerable<Avatar>> result = new OASISResult<IEnumerable<Avatar>>();

            try
            {
                result.Result = await _dbContext.Avatar.FindAsync(_ => true).Result.ToListAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarsAsync method in AvatarRepository in MongoDBOASIS Provider loading avatars. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<Avatar>> GetAvatars()
        {
            OASISResult<IEnumerable<Avatar>> result = new OASISResult<IEnumerable<Avatar>>();

            try
            {
                result.Result = _dbContext.Avatar.Find(_ => true).ToList();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatars method in AvatarRepository in MongoDBOASIS Provider loading avatars. Reason: {ex}");
            }

            return result;
        }

        public async Task<OASISResult<Avatar>> UpdateAsync(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
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
                await _dbContext.AvatarDetail.ReplaceOneAsync(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
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
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();
            var filter = Builders<AvatarDetail>.Filter.Where(expression);
            result.Result = _dbContext.AvatarDetail.Find(filter).FirstOrDefault();

            if (result.Result == null)
                OASISErrorHandling.HandleError(ref result, "Avatar Not Found");

            return result;
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Expression<Func<AvatarDetail, bool>> expression)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();
            var filter = Builders<AvatarDetail>.Filter.Where(expression);
            var findResult = await _dbContext.AvatarDetail.FindAsync(filter);
            result.Result = await findResult.FirstOrDefaultAsync();

            if (result.Result == null)
                OASISErrorHandling.HandleError(ref result, "Avatar Not Found");

            return result;
        }

        public async Task<OASISResult<AvatarDetail>> GetAvatarDetailAsync(Guid id)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var filter = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
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

        public OASISResult<AvatarDetail> GetAvatarDetail(Guid id)
        {
            OASISResult<AvatarDetail> result = new OASISResult<AvatarDetail>();

            try
            {
                var filter = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
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

        public async Task<OASISResult<IEnumerable<AvatarDetail>>> GetAvatarDetailsAsync()
        {
            OASISResult<IEnumerable<AvatarDetail>> result = new OASISResult<IEnumerable<AvatarDetail>>();

            try
            {
                var cursor = await _dbContext.AvatarDetail.FindAsync(_ => true);
                result.Result = cursor.ToEnumerable();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarDetailsAsync method in AvatarRepository in MongoDBOASIS Provider loading Avatar Details. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<IEnumerable<AvatarDetail>> GetAvatarDetails()
        {
            OASISResult<IEnumerable<AvatarDetail>> result = new OASISResult<IEnumerable<AvatarDetail>>();

            try
            {
                result.Result = _dbContext.AvatarDetail.Find(_ => true).ToEnumerable();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in GetAvatarDetails method in AvatarRepository in MongoDBOASIS Provider loading Avatar Details. Reason: {ex}");
            }

            return result;
        }

        public OASISResult<Avatar> Update(Avatar avatar)
        {
            OASISResult<Avatar> result = new OASISResult<Avatar>();

            try
            {
                _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
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
                _dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatar.HolonId, replacement: avatar);
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
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            using (var session = await _dbContext.MongoClient.StartSessionAsync())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    if (softDelete)
                    {
                        OASISResult<Avatar> avatarResult = await GetAvatarAsync(id);

                        if (!avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                                OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                            else
                            {
                                if (AvatarManager.LoggedInAvatar != null)
                                    avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                avatarResult.Result.DeletedDate = DateTime.Now;
                                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);

                                OASISResult<AvatarDetail> avatarDetailResult = await GetAvatarDetailAsync(avatarResult.Result.Username);

                                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                                {
                                    if (AvatarManager.LoggedInAvatar != null)
                                        avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                    avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                    await _dbContext.AvatarDetail.ReplaceOneAsync(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                    result.Result = true;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                    }
                    else
                    {
                        FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                        _dbContext.Avatar.DeleteOne(data);

                        FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                        _dbContext.AvatarDetail.DeleteOne(dataDetail);
                        result.Result = true;
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
                }

                if (result.IsError)
                    session.AbortTransaction();
                else
                    session.CommitTransaction();
            }

            return result;
        }

        public OASISResult<bool> Delete(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            using (var session = _dbContext.MongoClient.StartSession())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    if (softDelete)
                    {
                        OASISResult<Avatar> avatarResult = GetAvatar(id);

                        if (!avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                                OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                            else
                            {
                                if (AvatarManager.LoggedInAvatar != null)
                                    avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                avatarResult.Result.DeletedDate = DateTime.Now;
                                _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);

                                OASISResult<AvatarDetail> avatarDetailResult = GetAvatarDetail(avatarResult.Result.Username);

                                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                                {
                                    if (AvatarManager.LoggedInAvatar != null)
                                        avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                    avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                    _dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                    result.Result = true;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                    }
                    else
                    {
                        FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                        _dbContext.Avatar.DeleteOne(data);

                        FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                        _dbContext.AvatarDetail.DeleteOne(dataDetail);
                        result.Result = true;
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
                }

                if (result.IsError)
                    session.AbortTransaction();
                else
                    session.CommitTransaction();
            }

            return result;
        }

        public OASISResult<bool> Delete(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            using (var session = _dbContext.MongoClient.StartSession())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    if (softDelete)
                    {
                        OASISResult<Avatar> avatarResult = GetAvatar(expression);

                        if (avatarResult.IsError || avatarResult.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with expression {expression} was not found.");
                        else
                        {
                            if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                                OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                            else
                            {
                                if (AvatarManager.LoggedInAvatar != null)
                                    avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                avatarResult.Result.DeletedDate = DateTime.Now;
                                _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);

                                OASISResult<AvatarDetail> avatarDetailResult = GetAvatarDetail(avatarResult.Result.Username);

                                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                                {
                                    if (AvatarManager.LoggedInAvatar != null)
                                        avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                    avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                    _dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                    result.Result = true;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        OASISResult<Avatar> avatarResult = GetAvatar(expression);

                        FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(expression);
                        _dbContext.Avatar.DeleteOne(data);

                        if (avatarResult.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with expression {expression} was not found.");
                        else
                        {
                            FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == avatarResult.Result.HolonId);
                            _dbContext.AvatarDetail.DeleteOne(dataDetail);
                            result.Result = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
                }

                if (result.IsError)
                    session.AbortTransaction();
                else
                    session.CommitTransaction();
            }

            return result;
        }
        
        public async Task<OASISResult<bool>> DeleteAsync(Expression<Func<Avatar, bool>> expression, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            using (var session = await _dbContext.MongoClient.StartSessionAsync())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    if (softDelete)
                    {
                        OASISResult<Avatar> avatarResult = await GetAvatarAsync(expression);

                        if (avatarResult.Result == null || avatarResult.IsError)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with expression {expression} was not found.");
                        else
                        {
                            if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                                OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                            else
                            {
                                if (AvatarManager.LoggedInAvatar != null)
                                    avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                avatarResult.Result.DeletedDate = DateTime.Now;
                                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);

                                OASISResult<AvatarDetail> avatarDetailResult = await GetAvatarDetailAsync(avatarResult.Result.Username);

                                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                                {
                                    if (AvatarManager.LoggedInAvatar != null)
                                        avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                    avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                    await _dbContext.AvatarDetail.ReplaceOneAsync(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                    result.Result = true;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarDetailResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        OASISResult<Avatar> avatarResult = await GetAvatarAsync(expression);

                        FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(expression);
                        await _dbContext.Avatar.DeleteOneAsync(data);

                        if (avatarResult.Result == null)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with expression {expression} was not found.");
                        else
                        {
                            FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == avatarResult.Result.HolonId);
                            await _dbContext.AvatarDetail.DeleteOneAsync(dataDetail);
                            result.Result = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
                }

                if (result.IsError)
                    await session.AbortTransactionAsync();
                else
                    await session.CommitTransactionAsync();
            }

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            using (var session = await _dbContext.MongoClient.StartSessionAsync())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    if (softDelete)
                    {
                        OASISResult<Avatar> avatarResult = await GetAvatarAsync(providerKey);

                        if (avatarResult.Result == null || avatarResult.IsError)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with providerKey {providerKey} was not found.");
                        else
                        {
                            if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                                OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                            else
                            {
                                if (AvatarManager.LoggedInAvatar != null)
                                    avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                avatarResult.Result.DeletedDate = DateTime.Now;
                                await _dbContext.Avatar.ReplaceOneAsync(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);

                                OASISResult<AvatarDetail> avatarDetailResult = await GetAvatarDetailAsync(avatarResult.Result.Username);

                                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                                {
                                    if (AvatarManager.LoggedInAvatar != null)
                                        avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                    avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                    await _dbContext.AvatarDetail.ReplaceOneAsync(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                    result.Result = true;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarDetailResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        OASISResult<Avatar> avatarResult = await GetAvatarAsync(providerKey);

                        FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                        await _dbContext.Avatar.DeleteOneAsync(data);

                        if (avatarResult.Result == null || avatarResult.IsError)
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with providerKey {providerKey} was not found.");
                        else
                        {
                            FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == avatarResult.Result.HolonId);
                            await _dbContext.AvatarDetail.DeleteOneAsync(dataDetail);
                            result.Result = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
                }

                if (result.IsError)
                    await session.AbortTransactionAsync();
                else
                    await session.CommitTransactionAsync();
            }

            return result;
        }

        public OASISResult<bool> Delete(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in MongoDBOASIS Provider.";

            using (var session = _dbContext.MongoClient.StartSession())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    if (softDelete)
                    {
                        OASISResult<Avatar> avatarResult = GetAvatar(providerKey);

                        if (!avatarResult.IsError && avatarResult.Result != null)
                        {
                            if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                                OASISErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                            else
                            {
                                if (AvatarManager.LoggedInAvatar != null)
                                    avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                avatarResult.Result.DeletedDate = DateTime.Now;
                                _dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);

                                OASISResult<AvatarDetail> avatarDetailResult = GetAvatarDetail(avatarResult.Result.Username);

                                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                                {
                                    if (AvatarManager.LoggedInAvatar != null)
                                        avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id.ToString();

                                    avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                    _dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                    result.Result = true;
                                }
                                else
                                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with providerKey {providerKey} was not found.");
                    }
                    else
                    {
                        OASISResult<Avatar> avatarResult = GetAvatar(providerKey);

                        FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.ProviderUniqueStorageKey[Core.Enums.ProviderType.MongoDBOASIS] == providerKey);
                        _dbContext.Avatar.DeleteOne(data);

                        if (!avatarResult.IsError && avatarResult.Result != null)
                        {
                            FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == avatarResult.Result.HolonId);
                            _dbContext.AvatarDetail.DeleteOne(dataDetail);
                            result.Result = true;
                        }
                        else
                            OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with providerKey {providerKey} was not found.");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
                }

                if (result.IsError)
                    session.AbortTransaction();
                else
                    session.CommitTransaction();
            }

            return result;
        }
    }
}