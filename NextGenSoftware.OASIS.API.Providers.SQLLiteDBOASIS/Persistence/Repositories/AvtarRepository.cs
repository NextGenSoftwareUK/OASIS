
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Entities;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.Persistence.Repositories
{
    public class AvatarRepository : IAvatarRepository
    {
        private readonly DataContext eFContext;

        public AvatarRepository(DataContext eFContext)
        {
            this.eFContext = eFContext;
        }

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new OASISResult<AvatarEntity>();
            var Avatar = this.eFContext.AvatarEntities.Where(p => p.Id == id).FirstOrDefault();
            if(Avatar!=null)
            {
                avatarResult.IsError = false;
                avatarResult.Result = Avatar;
            }
            else
            {
                avatarResult.IsError = true;
                avatarResult.Result = Avatar;
            }
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = this.eFContext.Database.BeginTransaction();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                            ErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                                //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new OASISResult<AvatarDetailEntity>();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = this.eFContext.AvatarDetailEntities.Where(p => p.Username == avatarResult.Result.Username.ToString()).FirstOrDefault();
                            if (avatarDetailResult.Result != null)
                            {
                                avatarDetailResult.IsError = false;
                            }
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                                ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                        }
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = this.eFContext.AvatarEntities.Where(x => x.Id == id).FirstOrDefault();
                    if (data != null)
                    {
                        this.eFContext.Remove(data);
                        var dataDetail = this.eFContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if(dataDetail != null)
                        {
                            this.eFContext.Remove(dataDetail);
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
            }
            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();
            
            return result;
        }

        public OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new OASISResult<AvatarEntity>();
            var Avatar = this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefault();
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
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = this.eFContext.Database.BeginTransaction();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                            ErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new OASISResult<AvatarDetailEntity>();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = this.eFContext.AvatarDetailEntities.Where(p => p.Username == avatarResult.Result.Username.ToString()).FirstOrDefault();
                            if (avatarDetailResult.Result != null)
                            {
                                avatarDetailResult.IsError = false;
                            }
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                                ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                        }
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with Email {avatarEmail} was not found.");
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = this.eFContext.AvatarEntities.Where(x => x.Email == avatarEmail).FirstOrDefault();
                    if (data != null)
                    {
                        this.eFContext.Remove(data);
                        var dataDetail = this.eFContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null)
                        {
                            this.eFContext.Remove(dataDetail);
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
            }
            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new OASISResult<AvatarEntity>();
            var Avatar = this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
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
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = this.eFContext.Database.BeginTransaction();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                            ErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new OASISResult<AvatarDetailEntity>();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = this.eFContext.AvatarDetailEntities.Where(p => p.Username == avatarResult.Result.Username.ToString()).FirstOrDefault();
                            if (avatarDetailResult.Result != null)
                            {
                                avatarDetailResult.IsError = false;
                            }
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                                ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                        }
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with Username {avatarUsername} was not found.");
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = this.eFContext.AvatarEntities.Where(x => x.Username == avatarUsername).FirstOrDefault();
                    if (data != null)
                    {
                        this.eFContext.Remove(data);
                        var dataDetail = this.eFContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null)
                        {
                            this.eFContext.Remove(dataDetail);
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
            }
            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }
        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new OASISResult<AvatarEntity>();
            var Avatar = this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
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
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = this.eFContext.Database.BeginTransaction();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                            ErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new OASISResult<AvatarDetailEntity>();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = this.eFContext.AvatarDetailEntities.Where(p => p.Username == avatarResult.Result.Username.ToString()).FirstOrDefault();
                            if (avatarDetailResult.Result != null)
                            {
                                avatarDetailResult.IsError = false;
                            }
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                                ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                        }
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with Username {avatarUsername} was not found.");
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = this.eFContext.AvatarEntities.Where(x => x.Username == avatarUsername).FirstOrDefault();
                    if (data != null)
                    {
                        this.eFContext.Remove(data);
                        var dataDetail = this.eFContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null)
                        {
                            this.eFContext.Remove(dataDetail);
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
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
            OASISResult<AvatarEntity> avatarResult = new OASISResult<AvatarEntity>();
            var Avatar = this.eFContext.AvatarEntities.Where(p => p.Id == id).FirstOrDefault();
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
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = this.eFContext.Database.BeginTransaction();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                            ErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new OASISResult<AvatarDetailEntity>();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = this.eFContext.AvatarDetailEntities.Where(p => p.Username == avatarResult.Result.Username.ToString()).FirstOrDefault();
                            if (avatarDetailResult.Result != null)
                            {
                                avatarDetailResult.IsError = false;
                            }
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                                ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                        }
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with id {id} was not found.");
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = this.eFContext.AvatarEntities.Where(x => x.Id == id).FirstOrDefault();
                    if (data != null)
                    {
                        this.eFContext.Remove(data);
                        var dataDetail = this.eFContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null)
                        {
                            this.eFContext.Remove(dataDetail);
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
            }
            if (result.IsError)
                dbContextTransaction.Rollback();
            else
                dbContextTransaction.Commit();

            return result;
        }

        public async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            OASISResult<AvatarEntity> avatarResult = new OASISResult<AvatarEntity>();
            var Avatar = this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail).FirstOrDefault();
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
            OASISResult<bool> result = new OASISResult<bool>();
            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            var dbContextTransaction = this.eFContext.Database.BeginTransaction();
            try
            {
                if (softDelete)
                {
                    if (!avatarResult.IsError && avatarResult.Result != null)
                    {
                        if (avatarResult.Result.DeletedDate != DateTime.MinValue)
                            ErrorHandling.HandleError(ref result, $"The avatar with username {avatarResult.Result.Username} and email {avatarResult.Result.Email} and id {avatarResult.Result.Id} was already soft deleted on {avatarResult.Result.DeletedDate.ToString()} by avatar with id {avatarResult.Result.DeletedByAvatarId}. It cannot be deleted again. Please contact support if you wish this avatar to be restored or permanently deleted (cannot be reversed).");
                        else
                        {
                            //if (AvatarManager.LoggedInAvatar != null)
                            //avatarResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                            avatarResult.Result.DeletedDate = DateTime.Now;
                            //_dbContext.Avatar.ReplaceOne(filter: g => g.HolonId == avatarResult.Result.HolonId, replacement: avatarResult.Result);
                            //this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);

                            OASISResult<AvatarDetailEntity> avatarDetailResult = new OASISResult<AvatarDetailEntity>();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result = this.eFContext.AvatarDetailEntities.Where(p => p.Username == avatarResult.Result.Username.ToString()).FirstOrDefault();
                            if (avatarDetailResult.Result != null)
                            {
                                avatarDetailResult.IsError = false;
                            }
                            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                            {
                                //if (AvatarManager.LoggedInAvatar != null)
                                //    avatarDetailResult.Result.DeletedByAvatarId = AvatarManager.LoggedInAvatar.Id;

                                avatarDetailResult.Result.DeletedDate = DateTime.Now;
                                //_dbContext.AvatarDetail.ReplaceOne(filter: g => g.HolonId == avatarDetailResult.Result.HolonId, replacement: avatarDetailResult.Result);
                                this.eFContext.AvatarEntities.Where(x => x.HolonId == avatarResult.Result.HolonId);
                                result.Result = true;
                            }
                            else
                                ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar detail with username {avatarResult.Result.Username} was not found.");
                        }
                    }
                    else
                        ErrorHandling.HandleError(ref result, $"{errorMessage} The avatar with email {avatarEmail} was not found.");
                }
                else
                {
                    //FilterDefinition<Avatar> data = Builders<Avatar>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.Avatar.DeleteOne(data);

                    //FilterDefinition<AvatarDetail> dataDetail = Builders<AvatarDetail>.Filter.Where(x => x.HolonId == id);
                    //_dbContext.AvatarDetail.DeleteOne(dataDetail);
                    var data = this.eFContext.AvatarEntities.Where(x => x.Email == avatarEmail).FirstOrDefault();
                    if (data != null)
                    {
                        this.eFContext.Remove(data);
                        var dataDetail = this.eFContext.AvatarDetailEntities.Where(x => x.Username == data.Username);
                        if (dataDetail != null)
                        {
                            this.eFContext.Remove(dataDetail);
                        }
                        result.Result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandling.HandleError(ref result, $"{errorMessage} An unknown error occured, error details: {ex}", ex);
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
                var obj = this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Password == password && p.Version == version).ToList();
                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IAvatar)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(string username, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).ToList();
                return new OASISResult<IAvatar>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IAvatar)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, string password, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar Found",
                    };
                }
                else
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Version == version).ToListAsync();
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IEnumerable<IAvatar>)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Version == version).ToList();
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = true,
                    IsError = false,
                    Message = "Avatar Loaded Successfully",
                    Result = (IEnumerable<IAvatar>)obj
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IEnumerable<IAvatar>>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Id == Id && p.Version == version).ToListAsync();
                if (obj == null)
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "No Avatar Found",
                    };
                }
                else
                {

                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Username == avatarUsername && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Id == Id && p.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            try
            {
                var obj = this.eFContext.AvatarEntities.Where(p => p.Email == avatarEmail && p.Version == version).FirstOrDefault();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarAsync(string username, int version = 0)
        {
            try
            {
                var obj = await this.eFContext.AvatarEntities.Where(p => p.Username == username && p.Version == version).FirstOrDefaultAsync();
                if (obj == null)
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = false,
                        IsError = false,
                        Message = "No Avatar found",
                    };
                }
                else
                {
                    return new OASISResult<IAvatar>
                    {
                        IsLoaded = true,
                        IsError = false,
                        Message = "Avatar Loaded Successfully",
                        Result = (IAvatar)obj
                    };
                }
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsLoaded = false,
                    IsError = true,
                    Message = ex.ToString(),
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
                this.eFContext.AvatarEntities.Add(avatarEntity);
                this.eFContext.SaveChangesAsync();
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = avatarEntity.FirstName + " Record saved successfully",
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            try
            {
                AvatarEntity avatarEntity = CreateAvatarModel(Avatar);
                this.eFContext.AvatarEntities.Add(avatarEntity);
                await this.eFContext.SaveChangesAsync();
                //return Avatar;
                return new OASISResult<IAvatar>
                {
                    IsSaved = true,
                    IsError = false,
                    Message = Avatar.FirstName + " Record saved successfully",
                    Result = Avatar
                };
            }
            catch (Exception ex)
            {
                return new OASISResult<IAvatar>
                {
                    IsSaved = false,
                    IsError = true,
                    Message = ex.ToString(),
                };
            }
        }

        public AvatarEntity CreateAvatarModel(IAvatar Avatar)
        {
            AvatarEntity avatar = new AvatarEntity();
            avatar.AcceptTerms = Avatar.AcceptTerms;
            //avatar.CreatedByAvatarId = Convert.ToString(Avatar.CreatedByAvatarId);
            avatar.CreatedDate = Avatar.CreatedDate;
            //avatar.DeletedByAvatarId = Convert.ToString(Avatar.DeletedByAvatarId);
            avatar.DeletedDate = Avatar.DeletedDate;
            avatar.Description = Avatar.Description == null ? "" : Avatar.Description;
            avatar.Email = Avatar.Email == null ? "" : Avatar.Email;
            avatar.FirstName = Avatar.FirstName == null ? "" : Avatar.FirstName;
            //avatar.FullName = Avatar.FullName;
            //avatar.HolonId = Avatar.HolonId;
            avatar.Id = Avatar.Id;
            avatar.IsActive = Avatar.IsActive;
            avatar.IsBeamedIn = Avatar.IsBeamedIn;
            avatar.IsChanged = Avatar.IsChanged;
            //avatar.IsVerified = Avatar.IsVerified;
            avatar.JwtToken = Avatar.JwtToken == null ? "" : Avatar.JwtToken;
            avatar.LastBeamedIn = Avatar.LastBeamedIn;
            avatar.LastBeamedOut = Avatar.LastBeamedOut;
            avatar.LastName = Avatar.LastName;
            //avatar.ModifiedByAvatarId = Convert.ToString(Avatar.ModifiedByAvatarId);
            //avatar.ModifiedDate = Avatar.ModifiedDate;
            avatar.Name = Avatar.FullName;
            avatar.Password = Avatar.Password == null ? "" : Avatar.Password;
            avatar.PasswordReset = Avatar.PasswordReset;
            avatar.PreviousVersionId = Avatar.PreviousVersionId == Guid.Empty ? Guid.NewGuid() : Avatar.PreviousVersionId;
            avatar.RefreshToken = Avatar.RefreshToken == null ? "" : Avatar.RefreshToken;
            avatar.ResetToken = Avatar.ResetToken == null ? "" : Avatar.ResetToken;
            avatar.ResetTokenExpires = Avatar.ResetTokenExpires;
            avatar.Title = Avatar.Title == null ? "" : Avatar.Title;
            avatar.Username = Avatar.Username == null ? "" : Avatar.Username;
            avatar.VerificationToken = Avatar.VerificationToken == null ? "" : Avatar.VerificationToken;
            avatar.Verified = Avatar.Verified;
            avatar.Version = Avatar.Version;
            return avatar;
        }
    }
}
