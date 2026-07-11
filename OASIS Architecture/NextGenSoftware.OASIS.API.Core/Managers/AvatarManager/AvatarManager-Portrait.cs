using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;

namespace NextGenSoftware.OASIS.API.Core.Managers
{
    public partial class AvatarManager : OASISManager
    {
        #region Portrait Methods

        /// <summary>
        /// Gets the avatar's portrait (base64 image) and associated metadata by avatar id.
        /// </summary>
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByIdAsync(Guid id)
        {
            OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

            if (id == Guid.Empty)
            {
                OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarPortraitByIdAsync in AvatarManager. Id is empty, please specify a valid Guid.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarDetailResult = await LoadAvatarDetailAsync(id);

            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
            {
                if (string.IsNullOrEmpty(avatarDetailResult.Result.Portrait))
                    OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarPortraitByIdAsync in AvatarManager. No portrait has been uploaded for this avatar. Please upload an image first.");
                else
                    result.Result = new AvatarPortrait { AvatarId = avatarDetailResult.Result.Id, Email = avatarDetailResult.Result.Email, Username = avatarDetailResult.Result.Username, ImageBase64 = avatarDetailResult.Result.Portrait };
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occurred in GetAvatarPortraitByIdAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);

            return result;
        }

        /// <summary>
        /// Gets the avatar's portrait (base64 image) and associated metadata by username.
        /// </summary>
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByUsernameAsync(string username)
        {
            OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

            if (string.IsNullOrEmpty(username))
            {
                OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarPortraitByUsernameAsync in AvatarManager. Username is empty, please specify a valid username.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarDetailResult = await LoadAvatarDetailByUsernameAsync(username);

            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
            {
                if (string.IsNullOrEmpty(avatarDetailResult.Result.Portrait))
                    OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarPortraitByUsernameAsync in AvatarManager. No portrait has been uploaded for this avatar. Please upload an image first.");
                else
                    result.Result = new AvatarPortrait { AvatarId = avatarDetailResult.Result.Id, Email = avatarDetailResult.Result.Email, Username = avatarDetailResult.Result.Username, ImageBase64 = avatarDetailResult.Result.Portrait };
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occurred in GetAvatarPortraitByUsernameAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);

            return result;
        }

        /// <summary>
        /// Gets the avatar's portrait (base64 image) and associated metadata by email.
        /// </summary>
        public async Task<OASISResult<AvatarPortrait>> GetAvatarPortraitByEmailAsync(string email)
        {
            OASISResult<AvatarPortrait> result = new OASISResult<AvatarPortrait>();

            if (string.IsNullOrEmpty(email))
            {
                OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarPortraitByEmailAsync in AvatarManager. Email is empty, please specify a valid email address.");
                return result;
            }

            OASISResult<IAvatarDetail> avatarDetailResult = await LoadAvatarDetailByEmailAsync(email);

            if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
            {
                if (string.IsNullOrEmpty(avatarDetailResult.Result.Portrait))
                    OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarPortraitByEmailAsync in AvatarManager. No portrait has been uploaded for this avatar. Please upload an image first.");
                else
                    result.Result = new AvatarPortrait { AvatarId = avatarDetailResult.Result.Id, Email = avatarDetailResult.Result.Email, Username = avatarDetailResult.Result.Username, ImageBase64 = avatarDetailResult.Result.Portrait };
            }
            else
                OASISErrorHandling.HandleError(ref result, $"Error occurred in GetAvatarPortraitByEmailAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);

            return result;
        }

        /// <summary>
        /// Uploads (saves) a base64 portrait image for an avatar. Identify the avatar via AvatarId, Username or Email (at least one required).
        /// </summary>
        public async Task<OASISResult<bool>> UploadAvatarPortraitAsync(Guid avatarId, string username, string email, string imageBase64)
        {
            OASISResult<bool> result = new OASISResult<bool>();
            OASISResult<IAvatarDetail> avatarDetailResult = null;

            try
            {
                if (avatarId == Guid.Empty && string.IsNullOrEmpty(username) && string.IsNullOrEmpty(email))
                {
                    OASISErrorHandling.HandleError(ref result, "Error occurred in UploadAvatarPortraitAsync in AvatarManager. You must specify at least one of AvatarId, Username or Email.");
                    return result;
                }

                if (string.IsNullOrEmpty(imageBase64))
                {
                    OASISErrorHandling.HandleError(ref result, "Error occurred in UploadAvatarPortraitAsync in AvatarManager. ImageBase64 is empty, please provide a valid base64 encoded image.");
                    return result;
                }

                if (avatarId != Guid.Empty)
                    avatarDetailResult = await LoadAvatarDetailAsync(avatarId);
                else if (!string.IsNullOrEmpty(username))
                    avatarDetailResult = await LoadAvatarDetailByUsernameAsync(username);
                else
                    avatarDetailResult = await LoadAvatarDetailByEmailAsync(email);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                {
                    avatarDetailResult.Result.Portrait = imageBase64;
                    OASISResult<IAvatarDetail> saveResult = SaveAvatarDetail(avatarDetailResult.Result);

                    if (!saveResult.IsError && saveResult.Result != null)
                    {
                        result.Result = true;
                        result.IsSaved = true;
                        result.Message = "Portrait uploaded successfully.";
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occurred in UploadAvatarPortraitAsync in AvatarManager saving avatar detail. Reason: {saveResult.Message}", saveResult.DetailedMessage);
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occurred in UploadAvatarPortraitAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occurred in UploadAvatarPortraitAsync in AvatarManager. Reason: {ex.Message}", ex);
            }

            return result;
        }

        #endregion

        #region UMA JSON Methods

        /// <summary>
        /// Gets the UMA 3D model JSON for a given avatar by id.
        /// </summary>
        public async Task<OASISResult<string>> GetAvatarUmaJsonByIdAsync(Guid id)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (id == Guid.Empty)
                {
                    OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarUmaJsonByIdAsync in AvatarManager. Id is empty, please specify a valid Guid.");
                    return result;
                }

                OASISResult<IAvatarDetail> avatarDetailResult = await LoadAvatarDetailAsync(id);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occurred in GetAvatarUmaJsonByIdAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occurred in GetAvatarUmaJsonByIdAsync in AvatarManager. Reason: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets the UMA 3D model JSON for a given avatar by username.
        /// </summary>
        public async Task<OASISResult<string>> GetAvatarUmaJsonByUsernameAsync(string username)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrEmpty(username))
                {
                    OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarUmaJsonByUsernameAsync in AvatarManager. Username is empty, please specify a valid username.");
                    return result;
                }

                OASISResult<IAvatarDetail> avatarDetailResult = await LoadAvatarDetailByUsernameAsync(username);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occurred in GetAvatarUmaJsonByUsernameAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occurred in GetAvatarUmaJsonByUsernameAsync in AvatarManager. Reason: {ex.Message}", ex);
            }

            return result;
        }

        /// <summary>
        /// Gets the UMA 3D model JSON for a given avatar by email.
        /// </summary>
        public async Task<OASISResult<string>> GetAvatarUmaJsonByEmailAsync(string email)
        {
            OASISResult<string> result = new OASISResult<string>();

            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    OASISErrorHandling.HandleError(ref result, "Error occurred in GetAvatarUmaJsonByEmailAsync in AvatarManager. Email is empty, please specify a valid email address.");
                    return result;
                }

                OASISResult<IAvatarDetail> avatarDetailResult = await LoadAvatarDetailByEmailAsync(email);

                if (!avatarDetailResult.IsError && avatarDetailResult.Result != null)
                    result.Result = avatarDetailResult.Result.UmaJson;
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occurred in GetAvatarUmaJsonByEmailAsync in AvatarManager loading avatar detail. Reason: {avatarDetailResult.Message}", avatarDetailResult.DetailedMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Unknown error occurred in GetAvatarUmaJsonByEmailAsync in AvatarManager. Reason: {ex.Message}", ex);
            }

            return result;
        }

        #endregion
    }
}
