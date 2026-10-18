using System;
using System.IO;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public partial class LocalFileOASIS
    {
        public async Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (id == Guid.Empty) throw new ArgumentException("Avatar detail ID is required.");
                var loaded = await LoadAvatarDetailAsync(id);
                if (loaded.IsError || loaded.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail could not be loaded for deletion.", loaded.Exception);
                    return result;
                }
                if (softDelete)
                {
                    loaded.Result.IsActive = false; loaded.Result.DeletedDate = DateTime.UtcNow;
                    var saved = await SaveAvatarDetailAsync(loaded.Result);
                    if (saved.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, saved.Message, saved.Exception);
                        return result;
                    }
                }
                else
                {
                    var directory = Path.GetFullPath(_avatarDetailFolderPath);
                    var target = Path.GetFullPath(Path.Combine(directory, id + ".json"));
                    if (!string.Equals(Path.GetDirectoryName(target), directory, StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("Detail deletion path escaped its storage directory.");
                    File.Delete(target);
                }
                result.Result = true; result.IsDeleted = true; result.Message = "Avatar detail deleted.";
            }
            catch (Exception ex) { OASISErrorHandling.HandleError(ref result, "Error deleting avatar detail.", ex); }
            return result;
        }

        public OASISResult<bool> DeleteAvatarDetail(Guid id, bool softDelete = true)
            => DeleteAvatarDetailAsync(id, softDelete).GetAwaiter().GetResult();
        public async Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username, bool softDelete = true)
            => await DeleteDetailForIdentityAsync(await LoadAvatarByUsernameAsync(username), softDelete);
        public OASISResult<bool> DeleteAvatarDetailByUsername(string username, bool softDelete = true)
            => DeleteAvatarDetailByUsernameAsync(username, softDelete).GetAwaiter().GetResult();
        public async Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email, bool softDelete = true)
            => await DeleteDetailForIdentityAsync(await LoadAvatarByEmailAsync(email), softDelete);
        public OASISResult<bool> DeleteAvatarDetailByEmail(string email, bool softDelete = true)
            => DeleteAvatarDetailByEmailAsync(email, softDelete).GetAwaiter().GetResult();

        private async Task<OASISResult<bool>> DeleteDetailForIdentityAsync(OASISResult<IAvatar> avatar, bool softDelete)
        {
            if (!avatar.IsError && avatar.Result != null) return await DeleteAvatarDetailAsync(avatar.Result.Id, softDelete);
            var result = new OASISResult<bool>();
            OASISErrorHandling.HandleError(ref result, "Avatar identity could not be loaded for detail deletion.", avatar.Exception);
            return result;
        }
    }
}
