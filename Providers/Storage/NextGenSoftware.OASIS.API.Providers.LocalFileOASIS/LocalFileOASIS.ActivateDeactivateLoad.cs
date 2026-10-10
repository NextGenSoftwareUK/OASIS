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
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> ActivateProvider()
        {
            IsProviderActivated = true;
            return new OASISResult<bool>(true);
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            IsProviderActivated = false;
            return new OASISResult<bool>(true);
        }

        //public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWallets()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    try
        //    {
        //        string json = File.ReadAllText(_filePath);
        //        result.Result = JsonSerializer.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(json);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsAsync()
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

        //    try
        //    {
        //        using FileStream openStream = File.OpenRead(_filePath);
        //        result.Result = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<IProviderWallet>>>(openStream);
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        //public OASISResult<bool> SaveProviderWallets(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        string jsonString = JsonSerializer.Serialize(providerWallets);
        //        File.WriteAllText(_filePath, jsonString);
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        //public async Task<OASISResult<bool>> SaveProviderWalletsAsync(Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        using FileStream createStream = File.Create(_filePath);
        //        await JsonSerializer.SerializeAsync(createStream, providerWallets);
        //        await createStream.DisposeAsync();
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarById(Guid id)
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

        private string GetWalletFilePath(Guid id)
        {
            return string.Concat(_filePath, "wallets_", id.ToString(), ".json");
        }

        //public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        //{
        //    OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = 
        //        new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(new Dictionary<ProviderType, List<IProviderWallet>>());

        //    try
        //    {
        //        if (File.Exists(GetWalletFilePath(id)))
        //        {
        //            Dictionary<ProviderType, List<ProviderWallet>> wallets = new Dictionary<ProviderType, List<ProviderWallet>>();
        //            using FileStream openStream = File.OpenRead(GetWalletFilePath(id));
        //            //wallets = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<ProviderWallet>>>(openStream);
        //            wallets = await JsonConvert.DeserializeObject<Dictionary<ProviderType, List<ProviderWallet>>>(openStream);



        //            if (wallets != null)
        //            {
        //                foreach (ProviderType providerType in wallets.Keys)
        //                {
        //                    foreach (ProviderWallet wallet in wallets[providerType])
        //                    {
        //                        if (!result.Result.ContainsKey(providerType))
        //                            result.Result[providerType] = new List<IProviderWallet>();

        //                        result.Result[providerType].Add(wallet);
        //                    }
        //                }
        //            }
        //            else
        //                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByIdAsync method in LocalFileOASIS Provider loading wallets. Reason: Error deserializing data.");
        //        }
        //        else
        //            OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error wallets json file not found: {GetWalletFilePath(id)}");
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        /*
        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByUsername(string username)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                string json = File.ReadAllText(_filePath);
                result.Result = JsonSerializer.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(json);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByUsernameAsync(string username)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                using FileStream openStream = File.OpenRead(_filePath);
                result.Result = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<IProviderWallet>>>(openStream);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> LoadProviderWalletsForAvatarByEmail(string email)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                string json = File.ReadAllText(_filePath);
                result.Result = JsonSerializer.Deserialize<Dictionary<ProviderType, List<IProviderWallet>>>(json);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByEmailAsync(string email)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>();

            try
            {
                using FileStream openStream = File.OpenRead(_filePath);
                result.Result = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<IProviderWallet>>>(openStream);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }*/

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

    }
}
