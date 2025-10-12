using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.LocalFileOASIS
{
    public class LocalFileOASIS : OASISStorageProviderBase, IOASISLocalStorageProvider
    {
        //private string _filePath = "wallets.json";
        private string _filePath = "";

        public LocalFileOASIS(string filePath = "")
        {
            this.ProviderName = "LocalFileOASIS";
            this.ProviderDescription = "LocalFile Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.LocalFileOASIS);
            this.ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageLocal);

            if (!string.IsNullOrEmpty(filePath))
                _filePath = filePath;
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
                    wallets = JsonSerializer.Deserialize<Dictionary<ProviderType, List<ProviderWallet>>>(json);

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
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error wallets json file not found: {GetWalletFilePath(id)}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

        public async Task<OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>> LoadProviderWalletsForAvatarByIdAsync(Guid id)
        {
            OASISResult<Dictionary<ProviderType, List<IProviderWallet>>> result = 
                new OASISResult<Dictionary<ProviderType, List<IProviderWallet>>>(new Dictionary<ProviderType, List<IProviderWallet>>());

            try
            {
                if (File.Exists(GetWalletFilePath(id)))
                {
                    Dictionary<ProviderType, List<ProviderWallet>> wallets = new Dictionary<ProviderType, List<ProviderWallet>>();
                    using FileStream openStream = File.OpenRead(GetWalletFilePath(id));
                    wallets = await JsonSerializer.DeserializeAsync<Dictionary<ProviderType, List<ProviderWallet>>>(openStream);

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
                        OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsForAvatarByIdAsync method in LocalFileOASIS Provider loading wallets. Reason: Error deserializing data.");
                }
                else
                    OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWallets method in LocalFileOASIS Provider loading wallets. Reason: Error wallets json file not found: {GetWalletFilePath(id)}");
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in LoadProviderWalletsAsync method in LocalFileOASIS Provider loading wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

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
                string jsonString = JsonSerializer.Serialize<object>(providerWallets);
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

        public async Task<OASISResult<bool>> SaveProviderWalletsForAvatarByIdAsync(Guid id, Dictionary<ProviderType, List<IProviderWallet>> providerWallets)
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                using FileStream createStream = File.Create(GetWalletFilePath(id));
                await JsonSerializer.SerializeAsync<object>(createStream, providerWallets);
                await createStream.DisposeAsync();
                result.Result = true;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in SaveProviderWalletsAsync method in LocalFileOASIS Provider saving wallets. Reason: {ex.Message}", ex);
            }

            return result;
        }

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
                    OASISErrorHandling.HandleError(ref result, "LocalFile provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "LocalFile provider is not activated");
                    return result;
                }

                // Load avatar by provider key first
                var avatarResult = await LoadAvatarAsync(providerKey);
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
                    OASISErrorHandling.HandleError(ref result, "LocalFile provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "LocalFile provider is not activated");
                    return result;
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
                    OASISErrorHandling.HandleError(ref result, "LocalFile provider is not activated");
                    return result;
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

        public override Task<OASISResult<IHolon>> DeleteHolonAsync(string providerKey)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatar> SaveAvatar(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar Avatar)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail Avatar)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
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
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool loadChildrenFromProvider = false, bool continueOnError = true, int version = 0)
        {
            return null;
        }

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return null;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<ISearchResults>> SearchAsync(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        private string GetWalletFilePath(Guid id)
        {
            return string.Concat(_filePath, "wallets_", id.ToString(), ".json");
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByIdAsync(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<bool> Import(IEnumerable<IHolon> holons)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            throw new NotImplementedException();
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAll(int version = 0)
        {
            throw new NotImplementedException();
        } 
    }
}