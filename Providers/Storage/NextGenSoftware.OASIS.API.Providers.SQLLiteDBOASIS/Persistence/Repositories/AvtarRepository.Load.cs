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
            OASISResult<AvatarModel> avatarResult = new();
            var avatar = _dbContext.Avatars.FirstOrDefault(p => p.Id == id.ToString());
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

            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = _dbContext.Database.BeginTransaction();
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
                            avatarDetailResult.Result = _dbContext.AvatarDetails
                                .Where(p => p.Username == avatarResult.Result.Username).FirstOrDefault();
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
                    var data = _dbContext.Avatars.Where(x => x.Id == id.ToString()).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetails.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<AvatarModel> avatarResult = new();
            var Avatar = _dbContext.Avatars
                .Where(p => p.Email == avatarEmail)
                .FirstOrDefault();
            if (Avatar != null)
            {
                avatarResult.IsError = false;
                avatarResult.Result = Avatar;
            }
            else
            {
                avatarResult.IsError = true;
                avatarResult.Result = Avatar;
            }

            OASISResult<bool> result = new();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = _dbContext.Database.BeginTransaction();
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

                            OASISResult<AvatarDetailModel?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetails.FirstOrDefault(p =>
                                    p.Username == avatarResult.Result.Username);
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
                        OASISErrorHandling.HandleError(ref result,
                            $"{errorMessage} The avatar with email {avatarEmail} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.Avatars.Where(x => x.Email == avatarEmail).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetails.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            OASISResult<bool> result = new();
            string errorMessage = "Error occurred in DeleteAvatarAsync method in AvatarRepository in SQLite Provider.";
            var dbContextTransaction = await _dbContext.Database.BeginTransactionAsync();
            
            try
            {
                var avatar = await _dbContext.Avatars.FirstOrDefaultAsync(p => p.ProviderKey.Any(pk => pk.Value == providerKey));
                if (avatar != null)
                {
                    if (softDelete)
                    {
                        avatar.IsActive = false;
                        avatar.DeletedDate = DateTime.UtcNow;
                        _dbContext.Avatars.Update(avatar);
                    }
                    else
                    {
                        _dbContext.Avatars.Remove(avatar);
                    }
                    await _dbContext.SaveChangesAsync();
                    
                    result.Result = true;
                    result.IsError = false;
                    result.IsSaved = true;
                    result.Message = softDelete ? "Avatar soft deleted successfully" : "Avatar deleted successfully";
                }
                else
                {
                    result.Result = false;
                    result.IsError = true;
                    result.Message = "Avatar not found";
                }
            }
            catch (Exception ex)
            {
                result.IsError = true;
                result.Message = $"{errorMessage} {ex.Message}";
                await dbContextTransaction.RollbackAsync();
                return result;
            }
            
            if (result.IsError)
                await dbContextTransaction.RollbackAsync();
            else
                await dbContextTransaction.CommitAsync();
            
            return result;
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            try
            {
                var avatarEntities = _dbContext.Avatars
                    .ToList()
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault(p => p.Username == username && p.Password == password && p.Version == version);
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

        public OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            try
            {
                var avatarEntities =
                    _dbContext.Avatars
                        .ToList()
                        .Select(GetAvatarFromEntity)
                        .FirstOrDefault(p => p.Username == username && p.Version == version);
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
                var avatarEntity = _dbContext.Avatars
                    .ToList()
                    .Where(p => p.Username == username && p.Version == version)
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault();
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
                    .Where(p => p.Version == version)
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
                    .Where(p => p.Version == version)
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
                var avatarEntity = _dbContext.Avatars
                    .ToList()
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault(p => p.Username == avatarUsername);
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
                var avatarEntities = _dbContext.Avatars
                    .ToList()
                    .Select(GetAvatarFromEntity)
                    .FirstOrDefault(p => p.Id == Id && p.Version == version);
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
