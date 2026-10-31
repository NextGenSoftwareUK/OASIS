using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS
{
    public partial class SQLLiteDBOASIS
    {
        public OASISResult<bool> DeleteAvatarDetail(Guid id, bool softDelete = true) => _avatarDetailRepository.DeleteAvatarDetail(id, softDelete);
        public Task<OASISResult<bool>> DeleteAvatarDetailAsync(Guid id, bool softDelete = true) => _avatarDetailRepository.DeleteAvatarDetailAsync(id, softDelete);
        public OASISResult<bool> DeleteAvatarDetailByUsername(string username, bool softDelete = true) =>
            DeleteSelectedDetail(LoadAvatarDetailByUsername(username), softDelete);
        public OASISResult<bool> DeleteAvatarDetailByEmail(string email, bool softDelete = true) =>
            DeleteSelectedDetail(LoadAvatarDetailByEmail(email), softDelete);
        public async Task<OASISResult<bool>> DeleteAvatarDetailByUsernameAsync(string username, bool softDelete = true) =>
            await DeleteSelectedDetailAsync(await LoadAvatarDetailByUsernameAsync(username), softDelete);
        public async Task<OASISResult<bool>> DeleteAvatarDetailByEmailAsync(string email, bool softDelete = true) =>
            await DeleteSelectedDetailAsync(await LoadAvatarDetailByEmailAsync(email), softDelete);

        private static OASISResult<bool> SelectedDetailFailure(OASISResult<IAvatarDetail> loaded)
        {
            if (loaded.IsError) return OASISResultHelper.CopyOASISResultOnlyWithNoInnerResult<IAvatarDetail, bool>(loaded);
            return loaded.Result == null ? new OASISResult<bool> { IsError = true,
                ErrorCode = OASISResultErrorCodes.RecordNotFound, Message = "Avatar detail not found." } : null;
        }

        private OASISResult<bool> DeleteSelectedDetail(OASISResult<IAvatarDetail> loaded, bool softDelete) =>
            SelectedDetailFailure(loaded) ?? DeleteAvatarDetail(loaded.Result.Id, softDelete);
        private async Task<OASISResult<bool>> DeleteSelectedDetailAsync(OASISResult<IAvatarDetail> loaded, bool softDelete) =>
            SelectedDetailFailure(loaded) ?? await DeleteAvatarDetailAsync(loaded.Result.Id, softDelete);
    }
}
