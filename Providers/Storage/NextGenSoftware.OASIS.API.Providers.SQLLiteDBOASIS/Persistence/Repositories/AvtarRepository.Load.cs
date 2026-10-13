using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public partial class AvatarRepository : IAvatarRepository
    {

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new();
            try
            {
            OASISResult<AvatarModel> avatarResult = new();
            var avatar = await _dbContext.Avatars.FirstOrDefaultAsync(p => p.Id == id.ToString());
            if (avatar != null)
            {
                avatarResult.IsError = false;
                avatarResult.Result = avatar;
            }
            else
            {
                avatarResult.IsError = true;
                avatarResult.Result = avatar;
            }

            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            using var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync();
            var deletionEntities = new HashSet<object>();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                        {
                            OASISErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailModel> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = await _dbContext.AvatarDetails
                                .FirstOrDefaultAsync(p => p.Id == avatarResult.Result.Id);
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.Avatars.Where(x => x.Id == avatarResult.Result.Id);
                                result.Result = true;
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = avatarResult.Result;
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        deletionEntities.Add(data);
                        var dataDetail = await _dbContext.AvatarDetails.Include(x => x.InventoryItems).Include(x => x.Stats).Where(x => x.Id == data.Id).ToListAsync();
                        foreach (var detail in dataDetail)
                        {
                            deletionEntities.Add(detail);
                            await RemoveAvatarDetailDependentsAsync(detail, deletionEntities);
                        }
                        _dbContext.RemoveRange(dataDetail);
                        result.Result = true;
                    }
                }
                if (!result.IsError) await _dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                DetachFailedAvatarDeletion(id.ToString(), deletionEntities);
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
            {
                result.Result = false;
                await dbContextTransaction.RollbackAsync();
                DetachFailedAvatarDeletion(id.ToString(), deletionEntities);
            }
            else
                await dbContextTransaction.CommitAsync();

            return result;
            }
            catch (Exception ex)
            {
                result.Result = false;
                OASISErrorHandling.HandleError(ref result, "SQLite avatar deletion lookup or transaction failed.", ex);
                return result;
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await _dbContext.Avatars.FirstOrDefaultAsync(a => a.Email == avatarEmail);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return await DeleteAvatarAsync(Guid.Parse(avatar.Id), softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar deletion identity lookup failed.", ex);
                return result;
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await _dbContext.Avatars.FirstOrDefaultAsync(a => a.LocalProviderKey == providerKey);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return await DeleteAvatarAsync(Guid.Parse(avatar.Id), softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar deletion identity lookup failed.", ex);
                return result;
            }
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            try
            {
                var avatarEntities = _dbContext.Avatars
                    .ToList()
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault(p => p.Username == username && p.Password == password && (version == 0 || p.Version == version));
                return new OASISResult<IAvatar>
                {
                    IsLoaded = avatarEntities != null,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            try
            {
                var avatarEntities =
                    _dbContext.Avatars
                        .ToList()
                        .Select(GetAvatarFromEntity)
                    .FirstOrDefault(p => p.Username == username && (version == 0 || p.Version == version));
                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            try
            {
                var row = await _dbContext.Avatars.FirstOrDefaultAsync(p => p.Username == username && p.Password == password && (version == 0 || p.Version == version));
                var avatarEntity = row == null ? null : GetAvatarFromEntity(row);
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                var avatarEntities = _dbContext.Avatars
                    .ToList()
                    .Where(p => (version == 0 || p.Version == version))
                    .Select(GetAvatarFromEntity)
                    .ToList();
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            try
            {
                var avatarEntities = _dbContext.Avatars
                    .ToList()
                    .Where(p => (version == 0 || p.Version == version))
                    .Select(GetAvatarFromEntity)
                    .ToList();
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var row = _dbContext.Avatars.FirstOrDefault(p => p.Username == avatarUsername && (version == 0 || p.Version == version));
                var avatarEntity = row == null ? null : GetAvatarFromEntity(row);
                if (avatarEntity == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntity
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            try
            {
                var row = await _dbContext.Avatars.FirstOrDefaultAsync(p => p.Id == Id.ToString() && (version == 0 || p.Version == version));
                var avatarEntities = row == null ? null : GetAvatarFromEntity(row);
                if (avatarEntities == null)
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "No Avatar Found"
                    };

                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = avatarEntities
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

    }
}
