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

        public OASISResult<bool> SaveProviderWalletsForAvatarById(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                foreach (ProviderType provider in providerWallets.Keys)
                {
                    foreach (IProviderWallet providerWallet in providerWallets[provider])
                        providerWallet.CreatedByAvatar = null;
                }

                string jsonString = JsonConvert.SerializeObject(providerWallets);
                //string jsonString = JsonSerializer.Serialize<object>(providerWallets, new JsonSerializerOptions() { ReferenceHandler = ReferenceHandler.Preserve });
                //string jsonString = JsonSerializer.Serialize<ProviderWallet>(providerWallets);
                File.WriteAllText(GetWalletFilePath(id), jsonString);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result =
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(new Dictionary<ProviderType, List<IProviderWallet>>());

            try
            {
                Dictionary<ProviderType, List<ProviderWallet>> wallets = new Dictionary<ProviderType, List<ProviderWallet>>();

                if (File.Exists(GetWalletFilePath(id)))
                {
                    string json = File.ReadAllText(GetWalletFilePath(id));
                    //wallets = JsonSerializer.Deserialize<Dictionary<ProviderType, List<ProviderWallet>>>(json);
                    wallets = JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(json);

                    if (wallets != null)
                    {
                        foreach (ProviderType providerType in wallets.Keys)
                        {
                            foreach (ProviderWallet wallet in wallets[providerType])
                            {
                                if (!result.Result.ContainsKey(providerType))
                                    result.Result[providerType] = new List<IProviderWallet>();

                                result.Result[providerType].Add(wallet);
                            }
                        }
                    }
                    else
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error deserializing data.");
                }
                //else
                //    OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error wallets json file not found: {GetWalletFilePath(id)}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                foreach (ProviderType provider in providerWallets.Keys)
                {
                    foreach (IProviderWallet providerWallet in providerWallets[provider])
                        providerWallet.CreatedByAvatar = null;
                }

                string jsonString = JsonConvert.SerializeObject(providerWallets);
                //string jsonString = JsonSerializer.Serialize<object>(providerWallets, new JsonSerializerOptions() { ReferenceHandler = ReferenceHandler.Preserve });
                //string jsonString = JsonSerializer.Serialize<ProviderWallet>(providerWallets);
                File.WriteAllText(GetWalletFilePath(id), jsonString);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
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

                // Search for avatar by provider key in avatar folder
                if (Directory.Exists(_avatarFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            
                            if (avatar != null && avatar.ProviderUniqueStorageKey != null && 
                                avatar.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.LocalFileOASIS) &&
                                avatar.ProviderUniqueStorageKey[Core.Enums.ProviderType.LocalFileOASIS] == providerKey)
                            {
                                result.Result = avatar;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar loaded successfully by provider key";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue searching other files
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar not found by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
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

                var avatarFilePath = Path.Combine(_avatarFolderPath, $"{Id}.json");
                if (File.Exists(avatarFilePath))
                {
                    var jsonContent = await File.ReadAllTextAsync(avatarFilePath);
                    var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                    
                    if (avatar != null && avatar.Version == version)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.IsLoaded = true;
                        result.Message = "Avatar loaded successfully";
                    }
                    else
                    {
                        result.IsError = false;
                        result.IsLoaded = false;
                        result.Message = avatar == null ? "Avatar file corrupted" : $"Avatar version {version} not found";
                    }
                }
                else
                {
                    result.IsError = false;
                    result.IsLoaded = false;
                    result.Message = "Avatar file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
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

                // Search for avatar by email in avatar folder
                if (Directory.Exists(_avatarFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            
                            if (avatar != null && avatar.Email != null && 
                                avatar.Email.Equals(avatarEmail, StringComparison.OrdinalIgnoreCase) &&
                                avatar.Version == version)
                            {
                                result.Result = avatar;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar loaded successfully by email";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue searching other files
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar not found by email";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
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

                // Search for avatar by username in avatar folder
                if (Directory.Exists(_avatarFolderPath))
                {
                    var jsonFiles = Directory.GetFiles(_avatarFolderPath, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent, AvatarDeserializeSettings);
                            
                            if (avatar != null && avatar.Username != null && 
                                avatar.Username.Equals(avatarUsername, StringComparison.OrdinalIgnoreCase) &&
                                avatar.Version == version)
                            {
                                result.Result = avatar;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Avatar loaded successfully by username";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue searching other files
                            LoggingManager.Log($"Error reading avatar file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Avatar not found by username";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex);
            }
            return result;
        }

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
    }
}
