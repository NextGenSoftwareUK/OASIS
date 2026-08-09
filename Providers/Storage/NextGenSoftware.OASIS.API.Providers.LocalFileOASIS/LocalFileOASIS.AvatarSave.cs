//using System.Text.Json;
//using System.Text.Json.Serialization;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Events;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public partial class LocalFileOASIS
    {
        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarFilePath = Path.Combine(_avatarFolderPath, $"{id}.json");
                if (File.Exists(avatarFilePath))
                {
                    if (softDelete)
                    {
                        // Load avatar, mark as deleted, and save
                        var loadResult = await LoadAvatarAsync(id);
                        if (!loadResult.IsError && loadResult.Result != null)
                        {
                            loadResult.Result.DeletedDate = DateTime.UtcNow;
                            var saveResult = await SaveAvatarAsync(loadResult.Result);
                            result.Result = !saveResult.IsError;
                            result.IsError = saveResult.IsError;
                            result.Message = saveResult.IsError ? saveResult.Message : "Avatar soft deleted successfully";
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Failed to load avatar for soft delete");
                        }
                    }
                    else
                    {
                        // Hard delete - remove file
                        File.Delete(avatarFilePath);
                        result.Result = true;
                        result.IsError = false;
                        result.Message = "Avatar deleted successfully";
                    }
                }
                else
                {
                    result.Result = false;
                    result.IsError = false;
                    result.Message = "Avatar file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByEmailAsync(string avatarEmail, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email first
                var loadResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (!loadResult.IsError && loadResult.Result != null)
                {
                    // Delete using the loaded avatar's ID
                    return await DeleteAvatarAsync(loadResult.Result.Id, softDelete);
                }
                else
                {
                    result.Result = false;
                    result.IsError = false;
                    result.Message = "Avatar not found by email";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarByUsernameAsync(string avatarUsername, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by username first
                var loadResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (!loadResult.IsError && loadResult.Result != null)
                {
                    // Delete using the loaded avatar's ID
                    return await DeleteAvatarAsync(loadResult.Result.Id, softDelete);
                }
                else
                {
                    result.Result = false;
                    result.IsError = false;
                    result.Message = "Avatar not found by username";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by provider key first
                var loadResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (!loadResult.IsError && loadResult.Result != null)
                {
                    // Delete using the loaded avatar's ID
                    return await DeleteAvatarAsync(loadResult.Result.Id, softDelete);
                }
                else
                {
                    result.Result = false;
                    result.IsError = false;
                    result.Message = "Avatar not found by provider key";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
        {
            var result = new OASISResult<KarmaAkashicRecord>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // Delegate to AvatarDetail's KarmaEarntAsync method
                var karmaResult = await Avatar.KarmaEarntAsync(karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDesc, karmaSourceWebLink);
                if (!karmaResult.IsError && karmaResult.Result != null)
                {
                    // Save the updated avatar detail
                    var saveResult = await SaveAvatarDetailAsync(Avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail after adding karma: {saveResult.Message}");
                        return result;
                    }

                    result.Result = karmaResult.Result;
                    result.IsError = false;
                    result.Message = "Karma added successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, karmaResult.Message ?? "Error adding karma");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error adding karma to avatar: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
        {
            return AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result;
        }

        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
        {
            var result = new OASISResult<KarmaAkashicRecord>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate LocalFile provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // Delegate to AvatarDetail's KarmaLostAsync method
                var karmaResult = await Avatar.KarmaLostAsync(karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDesc, karmaSourceWebLink);
                if (!karmaResult.IsError && karmaResult.Result != null)
                {
                    // Save the updated avatar detail
                    var saveResult = await SaveAvatarDetailAsync(Avatar);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail after removing karma: {saveResult.Message}");
                        return result;
                    }

                    result.Result = karmaResult.Result;
                    result.IsError = false;
                    result.Message = "Karma removed successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, karmaResult.Message ?? "Error removing karma");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error removing karma from avatar: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karmaSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
        {
            return RemoveKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karmaSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result;
        }

    }
}
