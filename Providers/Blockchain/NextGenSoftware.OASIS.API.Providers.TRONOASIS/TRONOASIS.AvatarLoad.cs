using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.GeoSpatialNFT.Request;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using Nethereum.Signer;
using Nethereum.Hex.HexConvertors.Extensions;
using System.IO;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS
{
    public partial class TRONOASIS
    {
        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Initialize TRON connection
                response.Result = true;
                response.Message = "TRON provider activated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error activating TRON provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            var response = new OASISResult<bool>();
            try
            {
                // Cleanup TRON connection
                response.Result = true;
                response.Message = "TRON provider deactivated successfully";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error deactivating TRON provider: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Resolve TRON wallet address from avatar ID via WalletManager
                var walletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(id, Core.Enums.ProviderType.TRONOASIS);
                var tronAddress = walletResult?.Result?.WalletAddress;
                if (string.IsNullOrEmpty(tronAddress))
                {
                    OASISErrorHandling.HandleError(ref response, $"No TRON wallet address registered for avatar {id}");
                    return response;
                }

                var accountInfo = await _tronClient.GetAccountInfoAsync(tronAddress);

                if (accountInfo != null)
                {
                    var avatar = ParseTRONToAvatar(accountInfo, id);
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from TRON successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid id, int version = 0)
        {
            return LoadAvatarAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                var accountInfo = await _tronClient.GetAccountInfoAsync(providerKey);

                if (accountInfo != null)
                {
                    var avatar = ParseTRONToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{providerKey}"));
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from TRON by provider key successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                var accountInfo = await _tronClient.GetAccountInfoByEmailAsync(avatarEmail);

                if (accountInfo != null)
                {
                    var avatar = ParseTRONToAvatar(accountInfo, CreateDeterministicGuid($"{ProviderType.Value}:{avatarEmail}"));
                    if (avatar != null)
                    {
                        response.Result = avatar;
                        response.IsError = false;
                        response.Message = "Avatar loaded from TRON by email successfully";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar from TRON response");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar not found on TRON blockchain");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by email from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                // Load avatar by username from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarByUsername";
                var parameters = new object[] { avatarUsername };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar from TRON data structure
                    var avatar = new Avatar
                    {
                        Id = avatarData.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id) ? id : CreateDeterministicGuid($"{ProviderType.Value}:{avatarUsername}"),
                        Username = avatarUsername,
                        Email = avatarData.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : $"{avatarUsername}@tron.local",
                        FirstName = avatarData.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : "",
                        LastName = avatarData.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : "",
                        CreatedDate = avatarData.TryGetProperty("createdDate", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by username from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // Load avatar detail from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarDetailById";
                var parameters = new object[] { id.ToString() };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar detail from TRON data structure
                    var avatarDetail = new AvatarDetail
                    {
                        Id = id,
                        Username = avatarDetailData.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : "",
                        Email = avatarDetailData.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "",
                        FirstName = avatarDetailData.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : "",
                        LastName = avatarDetailData.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : "",
                        Karma = avatarDetailData.TryGetProperty("karma", out var karmaProp) && long.TryParse(karmaProp.GetString(), out var karma) ? karma : 0,
                        XP = avatarDetailData.TryGetProperty("xp", out var xpProp) && int.TryParse(xpProp.GetString(), out var xp) ? xp : 0,
                        CreatedDate = avatarDetailData.TryGetProperty("createdDate", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };
                    response.Result = avatarDetail;
                    response.IsError = false;
                    response.Message = "Avatar detail loaded successfully from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            return LoadAvatarDetailAsync(id, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // Load avatar detail by email from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarDetailByEmail";
                var parameters = new object[] { avatarEmail };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar detail from TRON data structure
                    var avatarDetail = new AvatarDetail
                    {
                        Id = avatarDetailData.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id) ? id : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{avatarEmail}"),
                        Email = avatarEmail,
                        Username = avatarDetailData.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : avatarEmail.Split('@')[0],
                        FirstName = avatarDetailData.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : "",
                        LastName = avatarDetailData.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : "",
                        Karma = avatarDetailData.TryGetProperty("karma", out var karmaProp) && long.TryParse(karmaProp.GetString(), out var karma) ? karma : 0,
                        XP = avatarDetailData.TryGetProperty("xp", out var xpProp) && int.TryParse(xpProp.GetString(), out var xp) ? xp : 0,
                        CreatedDate = avatarDetailData.TryGetProperty("createdDate", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };
                    response.Result = avatarDetail;
                    response.IsError = false;
                    response.Message = "Avatar detail loaded successfully from TRON by email";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by email from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                // Load avatar detail by username from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAvatarDetailByUsername";
                var parameters = new object[] { avatarUsername };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar detail from TRON data structure
                    var avatarDetail = new AvatarDetail
                    {
                        Id = avatarDetailData.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id) ? id : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{avatarUsername}"),
                        Username = avatarUsername,
                        Email = avatarDetailData.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : $"{avatarUsername}@tron.local",
                        FirstName = avatarDetailData.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : "",
                        LastName = avatarDetailData.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : "",
                        Karma = avatarDetailData.TryGetProperty("karma", out var karmaProp) && long.TryParse(karmaProp.GetString(), out var karma) ? karma : 0,
                        XP = avatarDetailData.TryGetProperty("xp", out var xpProp) && int.TryParse(xpProp.GetString(), out var xp) ? xp : 0,
                        CreatedDate = avatarDetailData.TryGetProperty("createdDate", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        Version = version
                    };
                    response.Result = avatarDetail;
                    response.IsError = false;
                    response.Message = "Avatar detail loaded successfully from TRON by username";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar detail from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar detail by username from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                // Load all avatars from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAllAvatars";
                var parameters = new object[] { };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarsData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatars from TRON data structure
                    var avatars = new List<IAvatar>();
                    if (avatarsData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var avatarElement in avatarsData.EnumerateArray())
                        {
                            var avatar = new Avatar
                            {
                                Id = avatarElement.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id) ? id : CreateDeterministicGuid($"{ProviderType.Value}:{avatarElement.GetRawText()}"),
                                Username = avatarElement.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : "",
                                Email = avatarElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "",
                                FirstName = avatarElement.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : "",
                                LastName = avatarElement.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : "",
                                CreatedDate = avatarElement.TryGetProperty("createdDate", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow,
                                Version = version
                            };
                            avatars.Add(avatar);
                        }
                    }
                    response.Result = avatars;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {avatars.Count} avatars from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatars from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                // Load all avatar details from TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "getAllAvatarDetails";
                var parameters = new object[] { };
                
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    var avatarDetailsData = JsonSerializer.Deserialize<JsonElement>(contractResult.Result);
                    // Parse avatar details from TRON data structure
                    var avatarDetails = new List<IAvatarDetail>();
                    if (avatarDetailsData.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var avatarDetailElement in avatarDetailsData.EnumerateArray())
                        {
                            var avatarDetail = new AvatarDetail
                            {
                                Id = avatarDetailElement.TryGetProperty("id", out var idProp) && Guid.TryParse(idProp.GetString(), out var id) ? id : CreateDeterministicGuid($"{ProviderType.Value}:avatarDetail:{avatarDetailElement.GetRawText()}"),
                                Username = avatarDetailElement.TryGetProperty("username", out var usernameProp) ? usernameProp.GetString() : "",
                                Email = avatarDetailElement.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : "",
                                FirstName = avatarDetailElement.TryGetProperty("firstName", out var firstNameProp) ? firstNameProp.GetString() : "",
                                LastName = avatarDetailElement.TryGetProperty("lastName", out var lastNameProp) ? lastNameProp.GetString() : "",
                                Karma = avatarDetailElement.TryGetProperty("karma", out var karmaProp) && long.TryParse(karmaProp.GetString(), out var karma) ? karma : 0,
                                XP = avatarDetailElement.TryGetProperty("xp", out var xpProp) && int.TryParse(xpProp.GetString(), out var xp) ? xp : 0,
                                CreatedDate = avatarDetailElement.TryGetProperty("createdDate", out var createdProp) && DateTime.TryParse(createdProp.GetString(), out var created) ? created : DateTime.UtcNow,
                                ModifiedDate = DateTime.UtcNow,
                                Version = version
                            };
                            avatarDetails.Add(avatarDetail);
                        }
                    }
                    response.Result = avatarDetails;
                    response.IsError = false;
                    response.Message = $"Successfully loaded {avatarDetails.Count} avatar details from TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to load avatar details from TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading all avatar details from TRON: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (avatar == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Avatar cannot be null");
                    return response;
                }

                // Save avatar to TRON blockchain using smart contract
                var contractAddress = GetOASISContractAddress();
                var functionName = "saveAvatar";
                var parameters = new object[]
                {
                    avatar.Id.ToString(),
                    avatar.Username ?? "",
                    avatar.Email ?? "",
                    avatar.FirstName ?? "",
                    avatar.LastName ?? "",
                    avatar.Title ?? "",
                    avatar.Password ?? "",
                    (int)avatar.AvatarType.Value,
                    avatar.AcceptTerms,
                    avatar.JwtToken ?? "",
                    avatar.PasswordReset.HasValue ? ((DateTimeOffset)avatar.PasswordReset.Value).ToUnixTimeSeconds() : 0,
                    avatar.RefreshToken ?? "",
                    avatar.ResetToken ?? "",
                    avatar.ResetTokenExpires.HasValue ? ((DateTimeOffset)avatar.ResetTokenExpires.Value).ToUnixTimeSeconds() : 0,
                    avatar.VerificationToken ?? "",
                    avatar.Verified.HasValue ? ((DateTimeOffset)avatar.Verified.Value).ToUnixTimeSeconds() : 0,
                    avatar.LastBeamedIn.HasValue ? ((DateTimeOffset)avatar.LastBeamedIn.Value).ToUnixTimeSeconds() : 0,
                    avatar.LastBeamedOut.HasValue ? ((DateTimeOffset)avatar.LastBeamedOut.Value).ToUnixTimeSeconds() : 0,
                    avatar.IsBeamedIn,
                    ((DateTimeOffset)avatar.CreatedDate).ToUnixTimeSeconds(),
                    ((DateTimeOffset)DateTime.UtcNow).ToUnixTimeSeconds(),
                    avatar.Description ?? "",
                    avatar.IsActive
                };

                // Call TRON smart contract to save avatar
                var contractResult = await CallContractAsync(contractAddress, functionName, parameters);
                if (!contractResult.IsError && !string.IsNullOrWhiteSpace(contractResult.Result))
                {
                    response.Result = avatar;
                    response.IsError = false;
                    response.Message = "Avatar saved successfully to TRON";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Failed to save avatar to TRON: {contractResult.Message}");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error saving avatar to TRON: {ex.Message}", ex);
            }
            return response;
        }

    }
}
