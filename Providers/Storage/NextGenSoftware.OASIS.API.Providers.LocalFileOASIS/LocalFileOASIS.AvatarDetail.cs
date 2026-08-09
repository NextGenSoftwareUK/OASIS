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
        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IAvatar> LoadAvatarByVerificationToken(string verificationToken, int version = 0)
        {
            return LoadAvatarByVerificationTokenAsync(verificationToken, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByVerificationTokenAsync(string verificationToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                if (Directory.Exists(_avatarFolderPath))
                {
                    foreach (var file in Directory.GetFiles(_avatarFolderPath, "*.json"))
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            if (avatar != null && avatar.VerificationToken != null &&
                                avatar.VerificationToken.Equals(verificationToken, StringComparison.Ordinal))
                            {
                                result.Result = avatar;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar loaded successfully by verification token";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar not found by verification token";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by verification token: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByResetToken(string resetToken, int version = 0)
        {
            return LoadAvatarByResetTokenAsync(resetToken, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByResetTokenAsync(string resetToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                if (Directory.Exists(_avatarFolderPath))
                {
                    foreach (var file in Directory.GetFiles(_avatarFolderPath, "*.json"))
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            if (avatar != null && avatar.ResetToken != null &&
                                avatar.ResetToken.Equals(resetToken, StringComparison.Ordinal))
                            {
                                result.Result = avatar;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar loaded successfully by reset token";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar not found by reset token";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by reset token: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByRefreshToken(string refreshToken, int version = 0)
        {
            return LoadAvatarByRefreshTokenAsync(refreshToken, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByRefreshTokenAsync(string refreshToken, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
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

                if (Directory.Exists(_avatarFolderPath))
                {
                    foreach (var file in Directory.GetFiles(_avatarFolderPath, "*.json"))
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            if (avatar != null && avatar.RefreshTokens != null &&
                                avatar.RefreshTokens.Any(r => r.Token == refreshToken))
                            {
                                result.Result = avatar;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar loaded successfully by refresh token";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar not found by refresh token";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by refresh token: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
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

                var avatars = new List<IAvatar>();
                
                if (Directory.Exists(_avatarFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            
                            if (avatar != null && avatar.Version == version)
                            {
                                avatars.Add(avatar);
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue processing other files
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = avatars;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {avatars.Count} avatars";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
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

                var avatarDetailFilePath = Path.Combine(_avatarDetailFolderPath, $"{id}.json");
                if (File.Exists(avatarDetailFilePath))
                {
                    var jsonContent = await File.ReadAllTextAsync(avatarDetailFilePath);
                    var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(jsonContent);
                    
                    if (avatarDetail != null && avatarDetail.Version == version)
                    {
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.IsLoaded = true;
                        result.Message = "Avatar detail loaded successfully";
                    }
                    else
                    {
                        result.IsError = false;
                        result.IsLoaded = false;
                        result.Message = avatarDetail == null ? "Avatar detail file corrupted" : $"Avatar detail version {version} not found";
                    }
                }
                else
                {
                    result.IsError = false;
                    result.IsLoaded = false;
                    result.Message = "Avatar detail file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
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

                // Search for avatar detail by email
                if (Directory.Exists(_avatarDetailFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarDetailFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(jsonContent);
                            
                            if (avatarDetail != null && avatarDetail.Email != null && 
                                avatarDetail.Email.Equals(avatarEmail, StringComparison.OrdinalIgnoreCase) &&
                                avatarDetail.Version == version)
                            {
                                result.Result = avatarDetail;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar detail loaded successfully by email";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log($"Error reading avatar detail file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar detail not found by email";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
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

                // Search for avatar detail by username
                if (Directory.Exists(_avatarDetailFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarDetailFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(jsonContent);
                            
                            if (avatarDetail != null && avatarDetail.Username != null && 
                                avatarDetail.Username.Equals(avatarUsername, StringComparison.OrdinalIgnoreCase) &&
                                avatarDetail.Version == version)
                            {
                                result.Result = avatarDetail;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar detail loaded successfully by username";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log($"Error reading avatar detail file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar detail not found by username";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
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

                var avatarDetails = new List<IAvatarDetail>();
                
                if (Directory.Exists(_avatarDetailFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarDetailFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatarDetail = JsonConvert.DeserializeObject<AvatarDetail>(jsonContent);
                            
                            if (avatarDetail != null && avatarDetail.Version == version)
                            {
                                avatarDetails.Add(avatarDetail);
                            }
                        }
                        catch (Exception ex)
                        {
                            LoggingManager.Log($"Error reading avatar detail file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = avatarDetails;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {avatarDetails.Count} avatar details";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

    }
}
