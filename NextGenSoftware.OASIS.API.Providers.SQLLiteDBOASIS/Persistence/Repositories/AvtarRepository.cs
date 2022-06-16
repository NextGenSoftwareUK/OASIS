using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public class AvatarRepository : IAvatarRepository
    {
        private readonly DataContext _dbContext;

        public AvatarRepository(DataContext dbContext)
        {
            _dbContext = dbContext;
        }

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new();
            var avatar = _dbContext.AvatarEntities.FirstOrDefault(p => p.Id == id);
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
                            ErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetailEntities.FirstOrDefault(p =>
                                    p.Username == avatarResult.Result.Username);
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                            {
                                ErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.AvatarEntities.Where(x => x.Id == id).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new();
            var avatar = _dbContext.AvatarEntities.FirstOrDefault(p => p.Email == avatarEmail);
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
                            ErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetailEntities.FirstOrDefault(p =>
                                    p.Username == avatarResult.Result.Username);
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                            {
                                ErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        ErrorHandling.HandleError(ref result,
                            $"{errorMessage} The avatar with Email {avatarEmail} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.AvatarEntities.Where(x => x.Email == avatarEmail).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new();
            var avatar = _dbContext.AvatarEntities.FirstOrDefault(p => p.Username == avatarUsername);
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
                            ErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetailEntities.FirstOrDefault(p =>
                                    p.Username == avatarResult.Result.Username);
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                            {
                                ErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        ErrorHandling.HandleError(ref result,
                            $"{errorMessage} The avatar with Username {avatarUsername} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.AvatarEntities.Where(x => x.Username == avatarUsername).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new();
            var avatar = _dbContext.AvatarEntities.FirstOrDefault(p => p.Username == avatarUsername);
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
                            ErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetailEntities.FirstOrDefault(p =>
                                    p.Username == avatarResult.Result.Username);
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                            {
                                ErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        ErrorHandling.HandleError(ref result,
                            $"{errorMessage} The avatar with Username {avatarUsername} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.AvatarEntities.Where(x => x.Username == avatarUsername).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
                    ex);
            }

            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            throw new NotImplementedException();
        }

        public async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new();
            var avatar = _dbContext.AvatarEntities.FirstOrDefault(p => p.Id == id);
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
                            ErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = _dbContext.AvatarDetailEntities
                                .Where(p => p.Username == avatarResult.Result.Username).FirstOrDefault();
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                            {
                                ErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.AvatarEntities.Where(x => x.Id == id).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
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
            OASISResult<AvatarEntity> avatarResult = new();
            var Avatar = _dbContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefault();
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
                            ErrorHandling.HandleError(ref result,
                                $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        }
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetailEntities.FirstOrDefault(p =>
                                    p.Username == avatarResult.Result.Username);
                            if (avatarDetailResult.Result != null) avatarDetailResult.IsError = false;
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                _dbContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                            {
                                ErrorHandling.HandleError(ref result,
                                    $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                            }
                        }
                    }
                    else
                    {
                        ErrorHandling.HandleError(ref result,
                            $"{errorMessage} The avatar with email {avatarEmail} was not found.");
                    }
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = _dbContext.AvatarEntities.Where(x => x.Email == avatarEmail).FirstOrDefault();
                    if (data != null)
                    {
                        _dbContext.Remove(data);
                        var dataDetail = _dbContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null) _dbContext.Remove(dataDetail);
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}",
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
            throw new NotImplementedException();
        }

        public OASISResult<IAvatar> LoadAvatar(string username, string password, int version = 0)
        {
            try
            {
                var avatarEntities = _dbContext.AvatarEntities.FirstOrDefault(p =>
                    p.Username == username && p.Password == password && p.Version == version);
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
                    _dbContext.AvatarEntities.FirstOrDefault(p => p.Username == username && p.Version == version);
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
                var avatarEntity = await _dbContext.AvatarEntities
                    .Where(p => p.Username == username && p.Version == version).FirstOrDefaultAsync();
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
                    Result = (IAvatar) avatarEntity
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
                var avatarEntities = await _dbContext.AvatarEntities.Where(p => p.Version == version).ToListAsync();
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
                var avatarEntities = _dbContext.AvatarEntities.Where(p => p.Version == version).ToList();
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
                var avatarEntity = _dbContext.AvatarEntities.FirstOrDefault(p => p.Username == avatarUsername);
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
                var avatarEntities = _dbContext.AvatarEntities.FirstOrDefault(p => p.Id == Id && p.Version == version);
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
                    Result = (IAvatar) avatarEntities
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

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.AvatarEntities
                    .Where(p => p.Email == avatarEmail && p.Version == version).FirstOrDefaultAsync();
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

        public async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.AvatarEntities
                    .Where(p => p.Username == avatarUsername && p.Version == version).FirstOrDefaultAsync();
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

        public OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                var avatarEntity = _dbContext.AvatarEntities.FirstOrDefault(p => p.Id == Id && p.Version == version);
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
                    Result = (IAvatar) avatarEntity
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

        public OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var avatarEntity =
                    _dbContext.AvatarEntities.FirstOrDefault(p => p.Email == avatarEmail && p.Version == version);
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

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                var avatarEntity = await _dbContext.AvatarEntities
                    .Where(p => p.Username == username && p.Version == version).FirstOrDefaultAsync();
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

        public async Task<OASISResult<IAvatar>> LoadAvatarForProviderKeyAsync(string providerKey, int version = 0)
        {
            //try
            //{
            //    return await this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey && p.Version == version).ToListAsync();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public OASISResult<IAvatar> LoadAvatarForProviderKey(string providerKey, int version = 0)
        {
            //try
            //{
            //    return this.eFContext.AvatarEntities.Where(p => p.providerKey == providerKey && p.Version == version).ToList();
            //}
            //catch (Exception)
            //{
            //    throw;
            //}
            throw new NotImplementedException();
        }

        public OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(avatar);
                _dbContext.AvatarEntities.Add(avatarEntity);
                _dbContext.SaveChangesAsync();
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarEntity.FirstName + " Record saved successfully"
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(avatar);
                _dbContext.AvatarEntities.Add(avatarEntity);
                await _dbContext.SaveChangesAsync();
                //return Avatar;
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatar.FirstName + " Record saved successfully",
                    Result = avatar
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString()
                };
            }
        }

        public AvatarEntity CreateAvatarModel(IAvatar avatar) => new()
            {
                AcceptTerms = avatar.AcceptTerms,
                CreatedByAvatarId = avatar.CreatedByAvatarId,
                CreatedDate = avatar.CreatedDate,
                DeletedByAvatarId = avatar.DeletedByAvatarId,
                DeletedDate = avatar.DeletedDate,
                Description = avatar.Description,
                Email = avatar.Email,
                FirstName = avatar.FirstName,
                Id = avatar.Id,
                IsActive = avatar.IsActive,
                IsBeamedIn = avatar.IsBeamedIn,
                IsChanged = avatar.IsChanged,
                JwtToken = avatar.JwtToken,
                LastBeamedIn = avatar.LastBeamedIn,
                LastBeamedOut = avatar.LastBeamedOut,
                LastName = avatar.LastName,
                ModifiedByAvatarId = avatar.ModifiedByAvatarId,
                ModifiedDate = avatar.ModifiedDate,
                Name = avatar.FullName,
                Password = avatar.Password,
                PasswordReset = avatar.PasswordReset,
                PreviousVersionId =
                    avatar.PreviousVersionId == Guid.Empty ? Guid.NewGuid() : avatar.PreviousVersionId,
                RefreshToken = avatar.RefreshToken,
                ResetToken = avatar.ResetToken,
                ResetTokenExpires = avatar.ResetTokenExpires,
                Title = avatar.Title,
                Username = avatar.Username,
                VerificationToken = avatar.VerificationToken,
                Verified = avatar.Verified,
                Version = avatar.Version,
                Original = avatar.Original,
                AvatarId = avatar.AvatarId,
                AvatarType = avatar.AvatarType
            };
    }
}