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
