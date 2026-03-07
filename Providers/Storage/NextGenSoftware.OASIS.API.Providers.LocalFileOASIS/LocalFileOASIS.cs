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
    //public class LocalFileOASIS : OASISStorageProviderBase, IOASISLocalStorageProvider
    public class LocalFileOASIS : OASISStorageProviderBase, IOASISLocalStorageProvider
    {
        //private string _filePath = "wallets.json";
        private string _filePath = "";
        private string _basePath = "";
        private string _avatarFolderPath = "";
        private string _avatarDetailFolderPath = "";
        private string _holonDirectory = "";

        public event EventDelegates.StorageProviderError OnStorageProviderError;

        public LocalFileOASIS(string filePath = "")
        {
            this.ProviderName = "LocalFileOASIS";
            this.ProviderDescription = "LocalFile Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LocalFileOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);

            if (!string.IsNullOrEmpty(filePath))
                _filePath = filePath;

            _basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OASIS", "LocalFileOASIS");
            _avatarFolderPath = Path.Combine(_basePath, "Avatars");
            _avatarDetailFolderPath = Path.Combine(_basePath, "AvatarDetails");
            _holonDirectory = Path.Combine(_basePath, "Holons");
            
            // Ensure directories exist
            if (!Directory.Exists(_avatarFolderPath))
                Directory.CreateDirectory(_avatarFolderPath);
            if (!Directory.Exists(_avatarDetailFolderPath))
                Directory.CreateDirectory(_avatarDetailFolderPath);
            if (!Directory.Exists(_holonDirectory))
                Directory.CreateDirectory(_holonDirectory);
        }

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
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent);
                            
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
                    var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent);
                    
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
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent);
                            
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
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent);
                            
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
                            var avatar = JsonConvert.DeserializeObject<Avatar>(jsonContent);
                            
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

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
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

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                // Ensure avatar folder exists
                if (!Directory.Exists(_avatarFolderPath))
                    Directory.CreateDirectory(_avatarFolderPath);

                var avatarFilePath = Path.Combine(_avatarFolderPath, $"{Avatar.Id}.json");
                var jsonContent = JsonConvert.SerializeObject(Avatar, Formatting.Indented);
                await File.WriteAllTextAsync(avatarFilePath, jsonContent);

                result.Result = Avatar;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = "Avatar saved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
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

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                // Ensure avatar detail folder exists
                if (!Directory.Exists(_avatarDetailFolderPath))
                    Directory.CreateDirectory(_avatarDetailFolderPath);

                var avatarDetailFilePath = Path.Combine(_avatarDetailFolderPath, $"{Avatar.Id}.json");
                var jsonContent = JsonConvert.SerializeObject(Avatar, Formatting.Indented);
                await File.WriteAllTextAsync(avatarDetailFilePath, jsonContent);

                result.Result = Avatar;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = "Avatar detail saved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

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

        public async Task<OASISResult<KarmaAkashicRecord>> AddKarmaToAvatarAsync(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
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
                var karmaResult = await Avatar.KarmaEarntAsync(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
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

        public OASISResult<KarmaAkashicRecord> AddKarmaToAvatar(IAvatarDetail Avatar, KarmaTypePositive karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
        {
            return AddKarmaToAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result;
        }

        public async Task<OASISResult<KarmaAkashicRecord>> RemoveKarmaFromAvatarAsync(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
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
                var karmaResult = await Avatar.KarmaLostAsync(karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink);
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

        public OASISResult<KarmaAkashicRecord> RemoveKarmaFromAvatar(IAvatarDetail Avatar, KarmaTypeNegative karmaType, KarmaSourceType karmaSourceType, string karamSourceTitle, string karmaSourceDesc, string karmaSourceWebLink = null)
        {
            return RemoveKarmaFromAvatarAsync(Avatar, karmaType, karmaSourceType, karamSourceTitle, karmaSourceDesc, karmaSourceWebLink).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
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

                if (holon == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holon cannot be null");
                    return result;
                }

                // Ensure holon directory exists
                if (!Directory.Exists(_holonDirectory))
                    Directory.CreateDirectory(_holonDirectory);

                var holonFilePath = Path.Combine(_holonDirectory, $"{holon.Id}.json");
                var jsonContent = JsonConvert.SerializeObject(holon, Formatting.Indented);
                await File.WriteAllTextAsync(holonFilePath, jsonContent);

                // Save children if requested
                if (saveChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                {
                    foreach (var child in holon.Children)
                    {
                        var childResult = await SaveHolonAsync(child, saveChildren, recursive, maxChildDepth - 1, continueOnError, saveChildrenOnProvider);
                        if (childResult.IsError && !continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error saving child holon: {childResult.Message}");
                            return result;
                        }
                    }
                }

                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = "Holon saved successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                var savedHolons = new List<IHolon>();
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider);
                    if (saveResult.IsError)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error saving holon: {saveResult.Message}");
                            return result;
                        }
                    }
                    else
                    {
                        savedHolons.Add(saveResult.Result);
                    }
                }

                result.Result = savedHolons;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Saved {savedHolons.Count} holons successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
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

                var holonFilePath = Path.Combine(_holonDirectory, $"{id}.json");
                if (File.Exists(holonFilePath))
                {
                    var jsonContent = await File.ReadAllTextAsync(holonFilePath);
                    var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                    
                    if (holon != null && holon.Version == version)
                    {
                        // Load children if requested
                        if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                        {
                            var loadedChildren = new List<IHolon>();
                            foreach (var child in holon.Children)
                            {
                                var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                if (!childResult.IsError && childResult.Result != null)
                                {
                                    loadedChildren.Add(childResult.Result);
                                }
                                else if (childResult.IsError && !continueOnError)
                                {
                                    OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                    return result;
                                }
                            }
                            holon.Children = loadedChildren;
                        }

                        result.Result = holon;
                        result.IsError = false;
                        result.IsLoaded = true;
                        result.Message = "Holon loaded successfully";
                    }
                    else
                    {
                        result.IsError = false;
                        result.IsLoaded = false;
                        result.Message = holon == null ? "Holon file corrupted" : $"Holon version {version} not found";
                    }
                }
                else
                {
                    result.IsError = false;
                    result.IsLoaded = false;
                    result.Message = "Holon file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(id, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
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

                // Search for holon by provider key in holon directory
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.ProviderUniqueStorageKey != null && 
                                holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.LocalFileOASIS) &&
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LocalFileOASIS] == providerKey &&
                                holon.Version == version)
                            {
                                // Load children if requested
                                if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > 0)
                                {
                                    var loadedChildren = new List<IHolon>();
                                    foreach (var child in holon.Children)
                                    {
                                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childResult.IsError && childResult.Result != null)
                                        {
                                            loadedChildren.Add(childResult.Result);
                                        }
                                        else if (childResult.IsError && !continueOnError)
                                        {
                                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                            return result;
                                        }
                                    }
                                    holon.Children = loadedChildren;
                                }

                                result.Result = holon;
                                result.IsError = false;
                                result.IsLoaded = true;
                                result.Message = "Holon loaded successfully by provider key";
                                return result;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Continue searching other files
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.IsError = false;
                result.IsLoaded = false;
                result.Message = "Holon not found by provider key";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Load parent holon first
                var parentResult = await LoadHolonAsync(id, false, false, 0, continueOnError, loadChildrenFromProvider, version);
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading parent holon: {parentResult.Message}");
                    return result;
                }

                // Get children from parent holon
                var children = parentResult.Result.Children ?? new List<IHolon>();
                
                // Filter by type if specified
                if (type != HolonType.All)
                {
                    children = children.Where(c => c.HolonType == type).ToList();
                }

                // Load children recursively if requested
                if (loadChildren && children.Any() && maxChildDepth > curentChildDepth)
                {
                    var loadedChildren = new List<IHolon>();
                    foreach (var child in children)
                    {
                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                        if (!childResult.IsError && childResult.Result != null)
                        {
                            loadedChildren.Add(childResult.Result);
                        }
                        else if (childResult.IsError && !continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                            return result;
                        }
                    }
                    children = loadedChildren;
                }

                result.Result = children;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {children.Count()} holons for parent";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // Load parent holon by provider key first
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading parent holon by provider key: {parentResult.Message}");
                return result;
            }

            // Use the parent's ID to load children
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var matchingHolons = new List<IHolon>();
                
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.Version == version &&
                                (type == HolonType.All || holon.HolonType == type) &&
                                holon.MetaData != null && holon.MetaData.ContainsKey(metaKey) &&
                                holon.MetaData[metaKey]?.ToString() == metaValue)
                            {
                                // Load children if requested
                                if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > curentChildDepth)
                                {
                                    var loadedChildren = new List<IHolon>();
                                    foreach (var child in holon.Children)
                                    {
                                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childResult.IsError && childResult.Result != null)
                                        {
                                            loadedChildren.Add(childResult.Result);
                                        }
                                        else if (childResult.IsError && !continueOnError)
                                        {
                                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                            return result;
                                        }
                                    }
                                    holon.Children = loadedChildren;
                                }

                                matchingHolons.Add(holon);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Error reading holon file {file}: {ex.Message}", ex);
                                return result;
                            }
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = matchingHolons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {matchingHolons.Count} holons by metadata";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var matchingHolons = new List<IHolon>();
                
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.Version == version &&
                                (type == HolonType.All || holon.HolonType == type) &&
                                holon.MetaData != null)
                            {
                                bool matches = false;
                                if (metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All)
                                {
                                    matches = metaKeyValuePairs.All(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString() == kvp.Value);
                                }
                                else // Or
                                {
                                    matches = metaKeyValuePairs.Any(kvp => holon.MetaData.ContainsKey(kvp.Key) && holon.MetaData[kvp.Key]?.ToString() == kvp.Value);
                                }

                                if (matches)
                                {
                                    // Load children if requested
                                    if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > curentChildDepth)
                                    {
                                        var loadedChildren = new List<IHolon>();
                                        foreach (var child in holon.Children)
                                        {
                                            var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                            if (!childResult.IsError && childResult.Result != null)
                                            {
                                                loadedChildren.Add(childResult.Result);
                                            }
                                            else if (childResult.IsError && !continueOnError)
                                            {
                                                OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                                return result;
                                            }
                                        }
                                        holon.Children = loadedChildren;
                                    }

                                    matchingHolons.Add(holon);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Error reading holon file {file}: {ex.Message}", ex);
                                return result;
                            }
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = matchingHolons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {matchingHolons.Count} holons by metadata pairs";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var holons = new List<IHolon>();
                
                if (Directory.Exists(_holonDirectory))
                {
                    var jsonFiles = Directory.GetFiles(_holonDirectory, "*.json");
                    foreach (var file in jsonFiles)
                    {
                        try
                        {
                            var jsonContent = await File.ReadAllTextAsync(file);
                            var holon = JsonConvert.DeserializeObject<Holon>(jsonContent);
                            
                            if (holon != null && holon.Version == version &&
                                (type == HolonType.All || holon.HolonType == type))
                            {
                                // Load children if requested
                                if (loadChildren && holon.Children != null && holon.Children.Any() && maxChildDepth > curentChildDepth)
                                {
                                    var loadedChildren = new List<IHolon>();
                                    foreach (var child in holon.Children)
                                    {
                                        var childResult = await LoadHolonAsync(child.Id, loadChildren, recursive, maxChildDepth - curentChildDepth - 1, continueOnError, loadChildrenFromProvider, version);
                                        if (!childResult.IsError && childResult.Result != null)
                                        {
                                            loadedChildren.Add(childResult.Result);
                                        }
                                        else if (childResult.IsError && !continueOnError)
                                        {
                                            OASISErrorHandling.HandleError(ref result, $"Error loading child holon: {childResult.Message}");
                                            return result;
                                        }
                                    }
                                    holon.Children = loadedChildren;
                                }

                                holons.Add(holon);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Error reading holon file {file}: {ex.Message}", ex);
                                return result;
                            }
                            LoggingManager.Log($"Error reading holon file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                        }
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsLoaded = true;
                result.Message = $"Loaded {holons.Count} holons";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
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

                var holonFilePath = Path.Combine(_holonDirectory, $"{id}.json");
                if (File.Exists(holonFilePath))
                {
                    // Load holon first to return it
                    var loadResult = await LoadHolonAsync(id, false, false, 0, true, false, 0);
                    if (!loadResult.IsError && loadResult.Result != null)
                    {
                        // Delete the file
                        File.Delete(holonFilePath);
                        
                        result.Result = loadResult.Result;
                        result.IsError = false;
                        result.IsSaved = true;
                        result.Message = "Holon deleted successfully";
                    }
                    else
                    {
                        // File exists but couldn't load it, delete anyway
                        File.Delete(holonFilePath);
                        result.IsError = false;
                        result.Message = "Holon file deleted (but could not be loaded)";
                    }
                }
                else
                {
                    result.IsError = false;
                    result.Message = "Holon file not found";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            // Load holon by provider key first
            var loadResult = await LoadHolonAsync(providerKey, false, false, 0, true, false, 0);
            if (loadResult.IsError || loadResult.Result == null)
            {
                var result = new OASISResult<IHolon>();
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {loadResult.Message}");
                return result;
            }

            // Delete using the loaded holon's ID
            return await DeleteHolonAsync(loadResult.Result.Id);
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
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

                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Holons cannot be null");
                    return result;
                }

                // Ensure holon directory exists
                if (!Directory.Exists(_holonDirectory))
                    Directory.CreateDirectory(_holonDirectory);

                int importedCount = 0;
                foreach (var holon in holons)
                {
                    try
                    {
                        var saveResult = await SaveHolonAsync(holon, true, true, 10, true, false);
                        if (!saveResult.IsError)
                        {
                            importedCount++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingManager.Log($"Error importing holon {holon.Id}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
                result.Message = $"Imported {importedCount} holons successfully";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Load avatar
                var avatarResult = await LoadAvatarAsync(avatarId, version);
                var allData = new List<IHolon>();

                if (!avatarResult.IsError && avatarResult.Result != null)
                {
                    allData.Add(avatarResult.Result as IHolon);
                }

                // Load all holons for this avatar (as parent)
                var holonsResult = await LoadHolonsForParentAsync(avatarId, HolonType.All, true, true, 10, 0, true, false, version);
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons for avatar";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            // Load avatar by username first
            var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                return result;
            }

            // Export using the loaded avatar's ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            // Load avatar by email first
            var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress, version);
            if (avatarResult.IsError || avatarResult.Result == null)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                return result;
            }

            // Export using the loaded avatar's ID
            return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                // Load all avatars and holons
                var avatarsResult = await LoadAllAvatarsAsync(version);
                var holonsResult = await LoadAllHolonsAsync(HolonType.All, false, false, 0, 0, true, false, version);

                var allData = new List<IHolon>();
                if (!avatarsResult.IsError && avatarsResult.Result != null)
                {
                    allData.AddRange(avatarsResult.Result.Cast<IHolon>());
                }
                if (!holonsResult.IsError && holonsResult.Result != null)
                {
                    allData.AddRange(holonsResult.Result);
                }

                result.Result = allData;
                result.IsError = false;
                result.Message = $"Exported {allData.Count} holons";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting all data: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
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

                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Search parameters cannot be null");
                    return result;
                }

                var searchResults = new SearchResults
                {
                    SearchResultAvatars = new List<IAvatar>(),
                    SearchResultHolons = new List<IHolon>()
                };

                // Process search groups
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    foreach (var searchGroup in searchParams.SearchGroups)
                    {
                        // Search holons if requested
                        if (searchGroup.SearchHolons && searchGroup.HolonSearchParams != null)
                        {
                            var holonsToSearch = new List<IHolon>();

                            // Load holons based on parent ID if specified
                            if (searchParams.ParentId != Guid.Empty)
                            {
                                var parentHolonsResult = await LoadHolonsForParentAsync(
                                    searchParams.ParentId, 
                                    searchGroup.HolonType, 
                                    loadChildren, 
                                    searchParams.Recursive, 
                                    maxChildDepth, 
                                    0, 
                                    continueOnError, 
                                    false, 
                                    version);
                                if (!parentHolonsResult.IsError && parentHolonsResult.Result != null)
                                {
                                    holonsToSearch.AddRange(parentHolonsResult.Result);
                                }
                            }
                            else
                            {
                                // Load all holons of the specified type
                                var allHolonsResult = await LoadAllHolonsAsync(
                                    searchGroup.HolonType, 
                                    loadChildren, 
                                    recursive, 
                                    maxChildDepth, 
                                    0, 
                                    continueOnError, 
                                    false, 
                                    version);
                                if (!allHolonsResult.IsError && allHolonsResult.Result != null)
                                {
                                    holonsToSearch.AddRange(allHolonsResult.Result);
                                }
                            }

                            // Filter holons based on search criteria
                            foreach (var holon in holonsToSearch)
                            {
                                bool matches = true;

                                // Filter by avatar ID if specified
                                if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty)
                                {
                                    if (holon.CreatedByAvatarId != searchParams.AvatarId)
                                    {
                                        matches = false;
                                    }
                                }

                                if (matches)
                                {
                                    searchResults.SearchResultHolons.Add(holon);
                                }
                            }
                        }

                        // Search avatars if requested
                        if (searchGroup.SearchAvatars && searchGroup.AvatarSearchParams != null)
                        {
                            var avatarsToSearch = new List<IAvatar>();
                            
                            // Load all avatars
                            var allAvatarsResult = await LoadAllAvatarsAsync(version);
                            if (!allAvatarsResult.IsError && allAvatarsResult.Result != null)
                            {
                                avatarsToSearch.AddRange(allAvatarsResult.Result);
                            }

                            // Filter avatars based on search criteria
                            foreach (var avatar in avatarsToSearch)
                            {
                                bool matches = true;

                                // Filter by avatar ID if specified
                                if (searchParams.SearchOnlyForCurrentAvatar && searchParams.AvatarId != Guid.Empty)
                                {
                                    if (avatar.Id != searchParams.AvatarId)
                                    {
                                        matches = false;
                                    }
                                }

                                if (matches)
                                {
                                    searchResults.SearchResultAvatars.Add(avatar);
                                }
                            }
                        }
                    }
                }

                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                searchResults.NumberOfDuplicates = 0; // LocalFile doesn't track duplicates

                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Found {searchResults.NumberOfResults} matching results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error performing search: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        //public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        //{
        //    OASISResult<bool> result = new OASISResult<bool>();

        //    try
        //    {
        //        using FileStream createStream = File.Create(GetWalletFilePath(id));
        //        await JsonSerializer.SerializeAsync<object>(createStream, providerWallets);
        //        await createStream.DisposeAsync();
        //        result.Result = true;
        //    }
        //    catch (Exception ex)
        //    {
        //        OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
        //    }

        //    return result;
        //}

        /*
        public OASISResult<bool> SaveProviderWalletsForAvatarByUsername(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                string jsonString = JsonSerializer.Serialize(providerWallets);
                File.WriteAllText(_filePath, jsonString);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByUsernameAsync(string username, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                using FileStream createStream = File.Create(_filePath);
                await JsonSerializer.SerializeAsync(createStream, providerWallets);
                await createStream.DisposeAsync();
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public OASISResult<bool> SaveProviderWalletsForAvatarByEmail(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                string jsonString = JsonSerializer.Serialize(providerWallets);
                File.WriteAllText(_filePath, jsonString);
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByEmailAsync(string email, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                using FileStream createStream = File.Create(_filePath);
                await JsonSerializer.SerializeAsync(createStream, providerWallets);
                await createStream.DisposeAsync();
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }*/

        /*
        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            return DeleteAvatarAsync(id, softDelete).Result;
        }

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

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
                        // Soft delete: rename file to .deleted
                        var deletedFilePath = Path.Combine(_avatarFolderPath, $"{id}.deleted");
                        File.Move(avatarFilePath, deletedFilePath);
                    }
                    else
                    {
                        // Hard delete: remove file
                        File.Delete(avatarFilePath);
                    }

                    result.Result = true;
                    result.IsError = false;
                    result.Message = $"Avatar {(softDelete ? "soft deleted" : "deleted")} successfully from LocalFile";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar file not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar from LocalFile: {ex.Message}", ex);
            }
            return result;
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
                var avatarResult = await LoadAvatarByProviderKeyAsync(providerKey);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Delete avatar by ID
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }

                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = $"Avatar {(softDelete ? "soft deleted" : "deleted")} successfully by provider key from LocalFile";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from LocalFile: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByEmail(string avatarEmail, bool softDelete = true)
        {
            return DeleteAvatarByEmailAsync(avatarEmail, softDelete).Result;
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
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmail);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Delete avatar by ID
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }

                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by email from LocalFile";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from LocalFile: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<bool> DeleteAvatarByUsername(string avatarUsername, bool softDelete = true)
        {
            return DeleteAvatarByUsernameAsync(avatarUsername, softDelete).Result;
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
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Delete avatar by ID
                    var deleteResult = await DeleteAvatarAsync(avatarResult.Result.Id, softDelete);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting avatar: {deleteResult.Message}");
                        return result;
                    }

                    result.Result = deleteResult.Result;
                    result.IsError = false;
                    result.Message = "Avatar deleted successfully by username from LocalFile";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from LocalFile: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            return DeleteHolonAsync(id).Result;
        }

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonAsync(providerKey).Result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
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

                // Load holon first to get the data
                var holonResult = await LoadHolonAsync(id);
                if (holonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holon: {holonResult.Message}");
                    return result;
                }

                if (holonResult.Result != null)
                {
                    // Delete holon file from local file system
                    var filePath = Path.Combine(_holonDirectory, $"{id}.json");
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }

                    result.Result = holonResult.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from LocalFile";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from LocalFile: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
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

                if (string.IsNullOrEmpty(providerKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Provider key cannot be null or empty");
                    return result;
                }

                var filePath = Path.Combine(_basePath, $"{providerKey}.json");
                
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    result.Result = null; // Holon deleted
                    result.IsError = false;
                    result.Message = "Holon deleted successfully from local file system";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Holon file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon from local file system: {ex.Message}", ex);
            }
            return result;
        }

        // These are explicit interface implementations that delegate to the existing methods
        Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> IOASISStorageProvider.LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            return LoadProviderWalletsForAvatarByIdAsync(id);
        }

        Task<OASISResult<bool>> IOASISStorageProvider.SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            return SaveProviderWalletsForAvatarByIdAsync(id, providerWallets);
        }

        // These are explicit interface implementations that delegate to the override methods
        Task<OASISResult<IAvatar>> IOASISStorageProvider.LoadAvatarByProviderKeyAsync(string providerKey, int version)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version);
        }

        OASISResult<IAvatar> IOASISStorageProvider.LoadAvatarByProviderKey(string providerKey, int version)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        Task<OASISResult<IAvatar>> IOASISStorageProvider.LoadAvatarAsync(Guid Id, int version)
        {
            return LoadAvatarAsync(Id, version);
        }

        OASISResult<IAvatar> IOASISStorageProvider.LoadAvatar(Guid id, int version)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        Task<OASISResult<IAvatar>> IOASISStorageProvider.LoadAvatarByEmailAsync(string avatarEmail, int version)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version);
        }

        OASISResult<IAvatar> IOASISStorageProvider.LoadAvatarByEmail(string avatarEmail, int version)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        Task<OASISResult<IAvatar>> IOASISStorageProvider.LoadAvatarByUsernameAsync(string avatarUsername, int version)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version);
        }

        OASISResult<IAvatar> IOASISStorageProvider.LoadAvatarByUsername(string avatarUsername, int version)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        Task<OASISResult<IEnumerable<IAvatar>>> IOASISStorageProvider.LoadAllAvatarsAsync(int version)
        {
            return LoadAllAvatarsAsync(version);
        }

        OASISResult<IEnumerable<IAvatar>> IOASISStorageProvider.LoadAllAvatars(int version)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        Task<OASISResult<IAvatarDetail>> IOASISStorageProvider.LoadAvatarDetailAsync(Guid id, int version)
        {
            return LoadAvatarDetailAsync(id, version);
        }

        OASISResult<IAvatarDetail> IOASISStorageProvider.LoadAvatarDetail(Guid id, int version)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        Task<OASISResult<IAvatarDetail>> IOASISStorageProvider.LoadAvatarDetailByEmailAsync(string avatarEmail, int version)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version);
        }

        // Duplicate methods removed - implementations are above (see lines 785-2689)

        /*public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
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
                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.AvatarDetail)
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = holon.Id,
                                // Name is read-only in current model; skip assignment
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                PreviousVersionId = holon.PreviousVersionId
                            };
                            avatarDetails.Add(avatarDetail);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                result.Result = avatarDetails;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from local file system";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from local file system: {ex.Message}", ex);
            }
            return result;
        }
        
        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
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
                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.Avatar)
                        {
                            var avatar = new Avatar
                            {
                                Id = holon.Id,
                                // Name read-only; skip
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                PreviousVersionId = holon.PreviousVersionId
                            };
                            avatars.Add(avatar);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                result.Result = avatars;
                result.IsError = false;
                result.Message = $"Successfully loaded {avatars.Count} avatars from local file system";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
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

                var filePath = Path.Combine(_basePath, $"{Id}.json");
                
                if (File.Exists(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holon != null && holon.HolonType == HolonType.Avatar)
                    {
                        var avatar = new Avatar
                        {
                            Id = holon.Id,
                            // Name read-only; skip
                            Description = holon.Description,
                            Version = holon.Version,
                            CreatedDate = holon.CreatedDate,
                            ModifiedDate = holon.ModifiedDate,
                            MetaData = holon.MetaData,
                            PreviousVersionId = holon.PreviousVersionId
                        };
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully from local file system";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from local file");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar from local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            return LoadAvatarAsync(Id, version).Result;
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

                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.Avatar && 
                            holon.MetaData != null && holon.MetaData.ContainsKey("Email") && 
                            holon.MetaData["Email"] == avatarEmail)
                        {
                            var avatar = new Avatar
                            {
                                Id = holon.Id,
                                // Name read-only; skip
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                PreviousVersionId = holon.PreviousVersionId
                            };
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by email from local file system";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, $"Avatar with email {avatarEmail} not found in local file system");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from local file system: {ex.Message}", ex);
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

                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.Avatar && 
                            holon.MetaData != null && holon.MetaData.ContainsKey("Username") && 
                            holon.MetaData["Username"] == avatarUsername)
                        {
                            var avatar = new Avatar
                            {
                                Id = holon.Id,
                                // Name read-only; skip
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                PreviousVersionId = holon.PreviousVersionId
                            };
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by username from local file system";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, $"Avatar with username {avatarUsername} not found in local file system");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
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

                var filePath = Path.Combine(_basePath, $"{id}.json");
                
                if (File.Exists(filePath))
                {
                    var content = await File.ReadAllTextAsync(filePath);
                    var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holon != null && holon.HolonType == HolonType.AvatarDetail)
                    {
                        var avatarDetail = new AvatarDetail
                        {
                            Id = holon.Id,
                            // Name read-only; skip
                            Description = holon.Description,
                            Version = holon.Version,
                            CreatedDate = holon.CreatedDate,
                            ModifiedDate = holon.ModifiedDate,
                            MetaData = holon.MetaData,
                            PreviousVersionId = holon.PreviousVersionId
                        };
                        result.Result = avatarDetail;
                        result.IsError = false;
                        result.Message = "Avatar detail loaded successfully from local file system";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from local file");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Avatar detail file not found: {filePath}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail from local file system: {ex.Message}", ex);
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

                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.AvatarDetail && 
                            holon.MetaData != null && holon.MetaData.ContainsKey("Email") && 
                            holon.MetaData["Email"] == avatarEmail)
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = holon.Id,
                                // Name read-only; skip
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                PreviousVersionId = holon.PreviousVersionId
                            };
                            result.Result = avatarDetail;
                            result.IsError = false;
                            result.Message = "Avatar detail loaded successfully by email from local file system";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, $"Avatar detail with email {avatarEmail} not found in local file system");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from local file system: {ex.Message}", ex);
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

                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.AvatarDetail && 
                            holon.MetaData != null && holon.MetaData.ContainsKey("Username") && 
                            holon.MetaData["Username"] == avatarUsername)
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = holon.Id,
                                // Name read-only; skip
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                // ProviderKey = holon.ProviderKey, // Commented out - property doesn't exist
                                // PreviousVersionId = holon.PreviousVersionId, // Commented out - property doesn't exist
                                // NextVersionId = holon.NextVersionId // Commented out - property doesn't exist
                            };
                            result.Result = avatarDetail;
                            result.IsError = false;
                            result.Message = "Avatar detail loaded successfully by username from local file system";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, $"Avatar detail with username {avatarUsername} not found in local file system");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
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

                var files = Directory.GetFiles(_basePath, "*.json");
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.HolonType == HolonType.Avatar && holon.ProviderUniqueStorageKey != null && holon.ProviderUniqueStorageKey.ContainsKey(Core.Enums.ProviderType.LocalFileOASIS) && holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.LocalFileOASIS] == providerKey)
                        {
                            var avatar = new Avatar
                            {
                                Id = holon.Id,
                                // Name read-only; skip
                                Description = holon.Description,
                                Version = holon.Version,
                                CreatedDate = holon.CreatedDate,
                                ModifiedDate = holon.ModifiedDate,
                                MetaData = holon.MetaData,
                                PreviousVersionId = holon.PreviousVersionId
                            };
                            result.Result = avatar;
                            result.IsError = false;
                            result.Message = "Avatar loaded successfully by provider key from local file system";
                            return result;
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                OASISErrorHandling.HandleError(ref result, $"Avatar with provider key {providerKey} not found in local file system");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
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

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar cannot be null");
                    return result;
                }

                var holon = new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Name,
                    Description = Avatar.Description,
                    Version = Avatar.Version,
                    CreatedDate = Avatar.CreatedDate,
                    ModifiedDate = DateTime.Now,
                    MetaData = Avatar.MetaData,
                    PreviousVersionId = Avatar.PreviousVersionId,
                    HolonType = HolonType.Avatar
                };

                var jsonContent = JsonSerializer.Serialize(holon, new JsonSerializerOptions { WriteIndented = true });
                var filePath = Path.Combine(_basePath, $"{Avatar.Id}.json");
                
                await File.WriteAllTextAsync(filePath, jsonContent);
                
                result.Result = Avatar;
                result.IsError = false;
                result.Message = "Avatar saved successfully to local file system";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar to local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            return SaveAvatarAsync(Avatar).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
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

                if (Avatar == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar detail cannot be null");
                    return result;
                }

                var holon = new Holon
                {
                    Id = Avatar.Id,
                    Name = Avatar.Name,
                    Description = Avatar.Description,
                    Version = Avatar.Version,
                    CreatedDate = Avatar.CreatedDate,
                    ModifiedDate = DateTime.Now,
                    MetaData = Avatar.MetaData,
                    PreviousVersionId = Avatar.PreviousVersionId,
                    HolonType = HolonType.AvatarDetail
                };

                var jsonContent = JsonSerializer.Serialize(holon, new JsonSerializerOptions { WriteIndented = true });
                var filePath = Path.Combine(_basePath, $"{Avatar.Id}.json");
                
                await File.WriteAllTextAsync(filePath, jsonContent);
                
                result.Result = Avatar;
                result.IsError = false;
                result.Message = "Avatar detail saved successfully to local file system";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving avatar detail to local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            return SaveAvatarDetailAsync(Avatar).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
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

                var holons = new List<IHolon>();
                var files = Directory.GetFiles(_holonDirectory, "*.json", SearchOption.AllDirectories);
                
                foreach (var file in files)
                {
                    try
                    {
                        var content = await File.ReadAllTextAsync(file);
                        var holon = JsonSerializer.Deserialize<Holon>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        
                        if (holon != null && holon.ParentHolonId == id)
                        {
                            // Filter by type if specified
                            if (type == HolonType.All || holon.HolonType == type)
                            {
                                // Load children recursively if requested
                                if (loadChildren && recursive && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
                                {
                                    var childrenResult = await LoadHolonsForParentAsync(holon.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth + 1, continueOnError, loadChildrenFromProvider, version);
                                    if (!childrenResult.IsError && childrenResult.Result != null)
                                    {
                                        holon.Children = childrenResult.Result.ToList();
                                    }
                                }
                                holons.Add(holon);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!continueOnError)
                        {
                            OASISErrorHandling.HandleError(ref result, $"Error loading holon from file {file}: {ex.Message}", ex);
                            return result;
                        }
                        // Log error but continue with other files
                        Console.WriteLine($"Error processing file {file}: {ex.Message}");
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons for parent from local file system";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from local file system: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            // First load the parent holon to get its ID
            var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
            if (parentResult.IsError || parentResult.Result == null)
            {
                return new OASISResult<IEnumerable<IHolon>>
                {
                    IsError = true,
                    Message = $"Failed to load parent holon by provider key: {parentResult.Message}"
                };
            }

            // Then load children using the parent ID
            return await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Real LocalFile implementation: Load holons by metadata from local files
                var holons = new List<IHolon>();
                var directory = Path.Combine(_basePath, "holons");
                
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(file);
                            var holonData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                            
                            if (holonData != null && holonData.ContainsKey("MetaData"))
                            {
                                var metaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData["MetaData"].ToString());
                                if (metaData != null && metaData.ContainsKey(metaKey))
                                {
                                    var value = metaData[metaKey].ToString();
                                    if (value == metaValue)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.TryParse(holonData.GetValueOrDefault("Id", "").ToString(), out var holonId) ? holonId : CreateDeterministicGuidFromProviderKey(JsonSerializer.Deserialize<Dictionary<Core.Enums.ProviderType, string>>(holonData.GetValueOrDefault("ProviderUniqueStorageKey", "{}").ToString()) ?? new Dictionary<Core.Enums.ProviderType, string>(), Core.Enums.ProviderType.LocalFileOASIS),
                                            Name = holonData.GetValueOrDefault("Name", "").ToString(),
                                            Description = holonData.GetValueOrDefault("Description", "").ToString(),
                                            HolonType = Enum.TryParse<HolonType>(holonData.GetValueOrDefault("HolonType", HolonType.All.ToString()).ToString(), out var holonType) ? holonType : HolonType.All,
                                            ParentHolonId = Guid.Parse(holonData.GetValueOrDefault("ParentHolonId", Guid.Empty.ToString()).ToString()),
                                            ProviderUniqueStorageKey = JsonSerializer.Deserialize<Dictionary<Core.Enums.ProviderType, string>>(holonData.GetValueOrDefault("ProviderUniqueStorageKey", "{}").ToString()) ?? new Dictionary<Core.Enums.ProviderType, string>(),
                                            Version = int.Parse(holonData.GetValueOrDefault("Version", "1").ToString()),
                                            IsActive = bool.Parse(holonData.GetValueOrDefault("IsActive", "true").ToString()),
                                            CreatedDate = DateTime.Parse(holonData.GetValueOrDefault("CreatedDate", DateTime.UtcNow.ToString()).ToString()),
                                            ModifiedDate = DateTime.Parse(holonData.GetValueOrDefault("ModifiedDate", DateTime.UtcNow.ToString()).ToString()),
                                            MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.GetValueOrDefault("MetaData", "{}").ToString()) ?? new Dictionary<string, object>()
                                        };
                                        if (holon != null)
                                            holons.Add(holon);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                                throw;
                        }
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata from local files";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from local files: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKey, metaValue, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Real LocalFile implementation: Load holons by metadata pairs from local files
                var holons = new List<IHolon>();
                var directory = Path.Combine(_basePath, "holons");
                
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(file);
                            var holonData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                            
                            if (holonData != null && holonData.ContainsKey("MetaData"))
                            {
                                var metaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData["MetaData"].ToString());
                                if (metaData != null)
                                {
                                    bool matches = metaKeyValuePairMatchMode == MetaKeyValuePairMatchMode.All
                                        ? metaKeyValuePairs.All(kvp => metaData.ContainsKey(kvp.Key) && metaData[kvp.Key].ToString() == kvp.Value)
                                        : metaKeyValuePairs.Any(kvp => metaData.ContainsKey(kvp.Key) && metaData[kvp.Key].ToString() == kvp.Value);
                                    
                                    if (matches)
                                    {
                                        var holon = new Holon
                                        {
                                            Id = Guid.TryParse(holonData.GetValueOrDefault("Id", "").ToString(), out var holonId) ? holonId : CreateDeterministicGuidFromProviderKey(JsonSerializer.Deserialize<Dictionary<Core.Enums.ProviderType, string>>(holonData.GetValueOrDefault("ProviderUniqueStorageKey", "{}").ToString()) ?? new Dictionary<Core.Enums.ProviderType, string>(), Core.Enums.ProviderType.LocalFileOASIS),
                                            Name = holonData.GetValueOrDefault("Name", "").ToString(),
                                            Description = holonData.GetValueOrDefault("Description", "").ToString(),
                                            HolonType = Enum.TryParse<HolonType>(holonData.GetValueOrDefault("HolonType", HolonType.All.ToString()).ToString(), out var holonType) ? holonType : HolonType.All,
                                            ParentHolonId = Guid.Parse(holonData.GetValueOrDefault("ParentHolonId", Guid.Empty.ToString()).ToString()),
                                            ProviderUniqueStorageKey = JsonSerializer.Deserialize<Dictionary<Core.Enums.ProviderType, string>>(holonData.GetValueOrDefault("ProviderUniqueStorageKey", "{}").ToString()) ?? new Dictionary<Core.Enums.ProviderType, string>(),
                                            Version = int.Parse(holonData.GetValueOrDefault("Version", "1").ToString()),
                                            IsActive = bool.Parse(holonData.GetValueOrDefault("IsActive", "true").ToString()),
                                            CreatedDate = DateTime.Parse(holonData.GetValueOrDefault("CreatedDate", DateTime.UtcNow.ToString()).ToString()),
                                            ModifiedDate = DateTime.Parse(holonData.GetValueOrDefault("ModifiedDate", DateTime.UtcNow.ToString()).ToString()),
                                            MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.GetValueOrDefault("MetaData", "{}").ToString()) ?? new Dictionary<string, object>()
                                        };
                                        if (holon != null)
                                            holons.Add(holon);
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                                throw;
                        }
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons by metadata pairs from local files";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata pairs from local files: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Real LocalFile implementation: Load all holons from local files
                var holons = new List<IHolon>();
                var directory = Path.Combine(_basePath, "holons");
                
                if (Directory.Exists(directory))
                {
                    var files = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(file);
                            var holonData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
                            
                            if (holonData != null)
                            {
                                        var holon = new Holon
                                        {
                                            Id = Guid.TryParse(holonData.GetValueOrDefault("Id", "").ToString(), out var holonId) ? holonId : CreateDeterministicGuidFromProviderKey(JsonSerializer.Deserialize<Dictionary<Core.Enums.ProviderType, string>>(holonData.GetValueOrDefault("ProviderUniqueStorageKey", "{}").ToString()) ?? new Dictionary<Core.Enums.ProviderType, string>(), Core.Enums.ProviderType.LocalFileOASIS),
                                            Name = holonData.GetValueOrDefault("Name", "").ToString(),
                                            Description = holonData.GetValueOrDefault("Description", "").ToString(),
                                            HolonType = Enum.TryParse<HolonType>(holonData.GetValueOrDefault("HolonType", HolonType.All.ToString()).ToString(), out var holonType) ? holonType : HolonType.All,
                                            ParentHolonId = Guid.Parse(holonData.GetValueOrDefault("ParentHolonId", Guid.Empty.ToString()).ToString()),
                                            ProviderUniqueStorageKey = JsonSerializer.Deserialize<Dictionary<Core.Enums.ProviderType, string>>(holonData.GetValueOrDefault("ProviderUniqueStorageKey", "{}").ToString()) ?? new Dictionary<Core.Enums.ProviderType, string>(),
                                            Version = int.Parse(holonData.GetValueOrDefault("Version", "1").ToString()),
                                            IsActive = bool.Parse(holonData.GetValueOrDefault("IsActive", "true").ToString()),
                                            CreatedDate = DateTime.Parse(holonData.GetValueOrDefault("CreatedDate", DateTime.UtcNow.ToString()).ToString()),
                                            ModifiedDate = DateTime.Parse(holonData.GetValueOrDefault("ModifiedDate", DateTime.UtcNow.ToString()).ToString()),
                                            MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.GetValueOrDefault("MetaData", "{}").ToString()) ?? new Dictionary<string, object>()
                                        };
                                if (holon != null)
                                    holons.Add(holon);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!continueOnError)
                                throw;
                        }
                    }
                }
                
                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons from local files";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from local files: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonAsync(holon, saveChildren, recursive, maxChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                // Real LocalFile implementation: Save holon to local file
                var directory = Path.Combine(_basePath, "holons");
                Directory.CreateDirectory(directory);
                
                var fileName = $"{holon.Id}.json";
                var filePath = Path.Combine(directory, fileName);
                
                var holonData = new Dictionary<string, object>
                {
                    ["Id"] = holon.Id,
                    ["Name"] = holon.Name,
                    ["Description"] = holon.Description,
                    ["HolonType"] = holon.HolonType,
                    ["ParentHolonId"] = holon.ParentHolonId,
                    ["ProviderUniqueStorageKey"] = holon.ProviderUniqueStorageKey,
                    ["Version"] = holon.Version,
                    ["IsActive"] = holon.IsActive,
                    ["CreatedDate"] = holon.CreatedDate,
                    ["ModifiedDate"] = holon.ModifiedDate,
                    ["MetaData"] = holon.MetaData
                };
                
                var json = JsonSerializer.Serialize(holonData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
                
                result.Result = holon;
                result.IsError = false;
                result.Message = "Holon saved successfully to local file";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holon to local file: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            return SaveHolonsAsync(holons, saveChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, saveChildrenOnProvider).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                // Real LocalFile implementation: Save multiple holons to local files
                var savedHolons = new List<IHolon>();
                var directory = Path.Combine(_basePath, "holons");
                Directory.CreateDirectory(directory);
                
                foreach (var holon in holons)
                {
                    try
                    {
                        var fileName = $"{holon.Id}.json";
                        var filePath = Path.Combine(directory, fileName);
                        
                        var holonData = new Dictionary<string, object>
                        {
                            ["Id"] = holon.Id,
                            ["Name"] = holon.Name,
                            ["Description"] = holon.Description,
                            ["HolonType"] = holon.HolonType,
                            ["ParentHolonId"] = holon.ParentHolonId,
                            ["ProviderUniqueStorageKey"] = holon.ProviderUniqueStorageKey,
                            ["Version"] = holon.Version,
                            ["IsActive"] = holon.IsActive,
                            ["CreatedDate"] = holon.CreatedDate,
                            ["ModifiedDate"] = holon.ModifiedDate,
                            ["MetaData"] = holon.MetaData
                        };
                        
                        var json = JsonSerializer.Serialize(holonData, new JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(filePath, json);
                        savedHolons.Add(holon);
                    }
                    catch (Exception ex)
                    {
                        if (!continueOnError)
                            throw;
                    }
                }
                
                result.Result = savedHolons;
                result.IsError = false;
                result.Message = $"Successfully saved {savedHolons.Count} holons to local files";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error saving holons to local files: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            var result = new OASISResult<ISearchResults>();
            string errorMessage = "Error in SearchAsync method in LocalFileOASIS Provider. Reason: ";

            try
            {
                if (searchParams == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} SearchParams cannot be null");
                    return result;
                }

                var searchResults = new SearchResults();
                var foundHolons = new List<IHolon>();

                // Search through all JSON files in the directory
                var jsonFiles = Directory.GetFiles(_filePath, "*.json", SearchOption.AllDirectories);
                
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(file);
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(jsonContent);
                        
                        if (holon != null && MatchesSearchCriteria(holon, searchParams))
                        {
                            foundHolons.Add(holon);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (continueOnError)
                        {
                            LoggingManager.Log($"Error processing file {file}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                            continue;
                        }
                        else
                        {
                            throw;
                        }
                    }
                }

                searchResults.SearchResultHolons = foundHolons;
                result.Result = searchResults;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

       

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }

        private bool MatchesSearchCriteria(IHolon holon, ISearchParams searchParams)
        {
            if (holon == null || searchParams == null)
                return false;

            // Check if holon name matches search criteria
            // Adapt to current ISearchParams model: aggregate text from groups if present (basic contains logic)
            var searchText = string.Empty;
            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                // try avatar/holon params with simple meta key
                var avatarParams = searchParams.SearchGroups
                    .Select(g => g.AvatarSearchParams)
                    .Where(p => p != null)
                    .FirstOrDefault();
                var holonParams = searchParams.SearchGroups
                    .Select(g => g.HolonSearchParams)
                    .Where(p => p != null)
                    .FirstOrDefault();

                // Current interfaces do not expose MetaDataKey; fall back to group query text if present
                var firstGroup = searchParams.SearchGroups.FirstOrDefault();
                if (firstGroup is ISearchTextGroup textGroup && !string.IsNullOrWhiteSpace(textGroup.SearchQuery))
                    searchText = textGroup.SearchQuery;
            }

            if (!string.IsNullOrEmpty(searchText))
            {
                if (!holon.Name?.ToLower().Contains(searchText.ToLower()) == true &&
                    !holon.Description?.ToLower().Contains(searchText.ToLower()) == true)
                {
                    return false;
                }
            }

            // Check holon type if specified
            var desiredType = HolonType.All;
            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var firstGroup = searchParams.SearchGroups.First();
                desiredType = firstGroup.HolonType;
            }

            if (desiredType != HolonType.All && holon.HolonType != desiredType)
            {
                return false;
            }

            // Check metadata if specified
            // Basic metadata exact-match filter using first group's MetaDataKey if specified
            if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
            {
                var firstHolonParams = searchParams.SearchGroups
                    .Select(g => g.HolonSearchParams)
                    .Where(p => p != null)
                    .FirstOrDefault();

                if (firstHolonParams != null && !string.IsNullOrWhiteSpace(firstHolonParams.MetaDataKey))
                {
                    if (!holon.MetaData.ContainsKey(firstHolonParams.MetaDataKey))
                        return false;
                }
            }

            return true;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in ImportAsync method in LocalFileOASIS Provider. Reason: ";

            try
            {
                if (holons == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Holons collection cannot be null");
                    return result;
                }

                var importedCount = 0;
                var errorCount = 0;

                foreach (var holon in holons)
                {
                    try
                    {
                        var filePath = Path.Combine(_filePath, $"{holon.Id}.json");
                        var jsonContent = System.Text.Json.JsonSerializer.Serialize(holon, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
                        await File.WriteAllTextAsync(filePath, jsonContent);
                        importedCount++;
                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        LoggingManager.Log($"Error importing holon {holon.Id}: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                    }
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons. {errorCount} errors occurred.";
                
                if (errorCount > 0)
                {
                    result.IsWarning = true;
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in ExportAllDataForAvatarByIdAsync method in LocalFileOASIS Provider. Reason: ";

            try
            {
                var exportedHolons = new List<IHolon>();
                var jsonFiles = Directory.GetFiles(_filePath, "*.json", SearchOption.AllDirectories);
                
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(file);
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(jsonContent);
                        
                        if (holon != null && holon.CreatedByAvatarId == avatarId)
                        {
                            exportedHolons.Add(holon);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingManager.Log($"Error processing file {file} for export: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                    }
                }

                result.Result = exportedHolons;
                result.IsError = false;
                result.Message = $"Exported {exportedHolons.Count} holons for avatar {avatarId}";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in ExportAllDataForAvatarByUsernameAsync method in LocalFileOASIS Provider. Reason: ";

            try
            {
                // First get the avatar by username to get the avatar ID
                var avatarResult = await LoadAvatarByUsernameAsync(avatarUsername);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar not found for username {avatarUsername}");
                    return result;
                }

                // Export all data for the avatar ID
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in ExportAllDataForAvatarByEmailAsync method in LocalFileOASIS Provider. Reason: ";

            try
            {
                // First get the avatar by email to get the avatar ID
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
                if (avatarResult.IsError || avatarResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"{errorMessage} Avatar not found for email {avatarEmailAddress}");
                    return result;
                }

                // Export all data for the avatar ID
                return await ExportAllDataForAvatarByIdAsync(avatarResult.Result.Id, version);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in ExportAllAsync method in LocalFileOASIS Provider. Reason: ";

            try
            {
                var exportedHolons = new List<IHolon>();
                var jsonFiles = Directory.GetFiles(_filePath, "*.json", SearchOption.AllDirectories);
                
                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var jsonContent = await File.ReadAllTextAsync(file);
                        var holon = System.Text.Json.JsonSerializer.Deserialize<Holon>(jsonContent);
                        
                        if (holon != null)
                        {
                            exportedHolons.Add(holon);
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingManager.Log($"Error processing file {file} for export: {ex.Message}", NextGenSoftware.Logging.LogType.Warning);
                    }
                }

                result.Result = exportedHolons;
                result.IsError = false;
                result.Message = $"Exported {exportedHolons.Count} holons";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"{errorMessage} {ex.Message}", ex);
            }

            return result;
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            return ImportAsync(holons).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            return ExportAllAsync(version).Result;
        } 
        */

        /// <summary>
        /// Creates a deterministic GUID from ProviderUniqueStorageKey, falling back to provider type if key not available
        /// </summary>
        private static Guid CreateDeterministicGuidFromProviderKey(Dictionary<Core.Enums.ProviderType, string> providerKeys, Core.Enums.ProviderType providerType)
        {
            var key = providerKeys?.GetValueOrDefault(providerType) 
                ?? providerKeys?.Values?.FirstOrDefault() 
                ?? $"LocalFileOASIS:unknown";
            return CreateDeterministicGuid(key);
        }

        /// <summary>
        /// Creates a deterministic GUID from input string using SHA-256 hash
        /// </summary>
        private static Guid CreateDeterministicGuid(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return Guid.Empty;

            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes.Take(16).ToArray());
        }
    }
}