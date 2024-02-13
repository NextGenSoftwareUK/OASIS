using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailAsync(Guid id, IAvatarDetail avatarDetail, bool appendChildObjects = false)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            string errorMessage = $"Error in UpdateAvatarDetailAsync method in AvatarManager for id {id}. Reason: ";

            OASISResult<IAvatarDetail> avatarDetailOriginalResult = await LoadAvatarDetailAsync(id);

            if (!avatarDetailOriginalResult.IsError && avatarDetailOriginalResult.Result != null)
                result = await UpdateAvatarDetailAsync(avatarDetailOriginalResult.Result, avatarDetail, errorMessage);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{avatarDetailOriginalResult.Message}", avatarDetailOriginalResult.DetailedMessage);

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailByEmailAsync(string email, IAvatarDetail avatarDetail, bool appendChildObjects = false)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            string errorMessage = $"Error in UpdateAvatarDetailByEmailAsync method in AvatarManager updating for email {email}. Reason: ";

            OASISResult<IAvatarDetail> avatarDetailOriginalResult = await LoadAvatarDetailByEmailAsync(email);

            if (!avatarDetailOriginalResult.IsError && avatarDetailOriginalResult.Result != null)
                result = await UpdateAvatarDetailAsync(avatarDetailOriginalResult.Result, avatarDetail, errorMessage);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{avatarDetailOriginalResult.Message}", avatarDetailOriginalResult.DetailedMessage);

            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> UpdateAvatarDetailByUsernameAsync(string username, IAvatarDetail avatarDetail, bool appendChildObjects = false)
        {
            OASISResult<IAvatarDetail> result = new OASISResult<IAvatarDetail>();
            string errorMessage = $"Error in UpdateAvatarDetailByUsernameAsync method in AvatarManager updating for username {username}. Reason: ";

            OASISResult<IAvatarDetail> avatarDetailOriginalResult = await LoadAvatarDetailByUsernameAsync(username);

            if (!avatarDetailOriginalResult.IsError && avatarDetailOriginalResult.Result != null)
                result = await UpdateAvatarDetailAsync(avatarDetailOriginalResult.Result, avatarDetail, errorMessage);
            else
                OASISErrorHandling.HandleError(ref result, $"{errorMessage}{avatarDetailOriginalResult.Message}", avatarDetailOriginalResult.DetailedMessage);

            return result;
        }
    }
}
