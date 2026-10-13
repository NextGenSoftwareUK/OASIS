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

        public OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            OASISResult<bool> result = new();
            try
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

            string errorMessage = "Error occured in DeleteAsync method in AvatarRepository in Sqllite Provider.";
            using var dbContextTransaction = _dbContext.Database.BeginTransaction();
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

                            OASISResult<AvatarDetailModel?> avatarDetailResult = new();
                            avatarDetailResult.IsError = true;
                            avatarDetailResult.Result =
                                _dbContext.AvatarDetails.FirstOrDefault(p =>
                                    p.Id == avatarResult.Result.Id);
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
                        deletionEntities.Add(data);
                        var dataDetail = _dbContext.AvatarDetails.Include(x => x.InventoryItems).Include(x => x.Stats).Where(x => x.Id == data.Id).ToList();
                        foreach (var detail in dataDetail)
                        {
                            deletionEntities.Add(detail);
                            RemoveAvatarDetailDependents(detail, deletionEntities);
                        }
                        _dbContext.RemoveRange(dataDetail);
                        result.Result = true;
                    }
                }
                if (!result.IsError) _dbContext.SaveChanges();
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
                dbContextTransaction.Rollback();
                DetachFailedAvatarDeletion(id.ToString(), deletionEntities);
            }
            else
                dbContextTransaction.Commit();

            return result;
            }
            catch (Exception ex)
            {
                result.Result = false;
                OASISErrorHandling.HandleError(ref result, "SQLite avatar deletion lookup or transaction failed.", ex);
                return result;
            }
        }

        private void RemoveAvatarDetailDependents(object entity, HashSet<object> deletionEntities)
        {
            var entry = _dbContext.Entry(entity);
            foreach (var navigation in entry.Navigations.Where(n => n.Metadata is Microsoft.EntityFrameworkCore.Metadata.INavigation mapped && !mapped.IsOnDependent).ToList())
            {
                navigation.Load();
                if (navigation.CurrentValue == null) continue;
                var children = navigation.Metadata.IsCollection
                    ? ((System.Collections.IEnumerable)navigation.CurrentValue).Cast<object>().ToList()
                    : new List<object> { navigation.CurrentValue };
                foreach (var child in children)
                {
                    deletionEntities.Add(child);
                    RemoveAvatarDetailDependents(child, deletionEntities);
                    _dbContext.Remove(child);
                }
            }
        }

        private async Task RemoveAvatarDetailDependentsAsync(object entity, HashSet<object> deletionEntities)
        {
            var entry = _dbContext.Entry(entity);
            foreach (var navigation in entry.Navigations.Where(n => n.Metadata is Microsoft.EntityFrameworkCore.Metadata.INavigation mapped && !mapped.IsOnDependent).ToList())
            {
                await navigation.LoadAsync();
                if (navigation.CurrentValue == null) continue;
                var children = navigation.Metadata.IsCollection
                    ? ((System.Collections.IEnumerable)navigation.CurrentValue).Cast<object>().ToList()
                    : new List<object> { navigation.CurrentValue };
                foreach (var child in children)
                {
                    deletionEntities.Add(child);
                    await RemoveAvatarDetailDependentsAsync(child, deletionEntities);
                    _dbContext.Remove(child);
                }
            }
        }

        private void DetachFailedAvatarDeletion(string id, HashSet<object> deletionEntities)
        {
            foreach (var entry in _dbContext.ChangeTracker.Entries().ToList())
            {
                var belongsToAvatar = deletionEntities.Contains(entry.Entity) || entry.Entity is AvatarModel avatar && avatar.Id == id
                    || entry.Entity is AvatarDetailModel detail && detail.Id == id;
                var avatarKey = entry.Metadata.FindProperty("AvatarId");
                if (avatarKey != null && avatarKey.ClrType == typeof(string))
                    belongsToAvatar |= (string)entry.Property("AvatarId").CurrentValue == id
                        || (string)entry.Property("AvatarId").OriginalValue == id;
                if (belongsToAvatar && (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted))
                    entry.State = EntityState.Detached;
            }
        }

        public OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = _dbContext.Avatars.FirstOrDefault(a => a.Email == avatarEmail);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return DeleteAvatar(Guid.Parse(avatar.Id), softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar deletion identity lookup failed.", ex);
                return result;
            }
        }

        public OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = _dbContext.Avatars.FirstOrDefault(a => a.Username == avatarUsername);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return DeleteAvatar(Guid.Parse(avatar.Id), softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar deletion identity lookup failed.", ex);
                return result;
            }
        }

        public async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = await _dbContext.Avatars.FirstOrDefaultAsync(a => a.Username == avatarUsername);
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

        public OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                var avatar = _dbContext.Avatars.FirstOrDefault(a => a.LocalProviderKey == providerKey);
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar deletion selector matched no persisted identity.");
                    return result;
                }
                return DeleteAvatar(Guid.Parse(avatar.Id), softDelete);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, "Avatar deletion identity lookup failed.", ex);
                return result;
            }
        }

    }
}
