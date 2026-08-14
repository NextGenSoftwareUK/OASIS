using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Response;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Objects.Avatar;
using System.Text.Json.Serialization;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Nethereum.Contracts;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using System.Numerics;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.OptimismOASIS
{
    public partial class OptimismOASIS_Legacy
    {
        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatars is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatars: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarsAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarsAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAvatarDetail is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetail: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var response = new OASISResult<IAvatarDetail>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Optimism implementation: Load AvatarDetail from Avatar's metadata
                var avatarId = id.ToString();
                var getAvatarFunction = _contract.GetFunction("getAvatar");
                var avatarData = await getAvatarFunction.CallDeserializingToObjectAsync<GetAvatarOutputDTO>(avatarId);

                if (avatarData != null && !string.IsNullOrEmpty(avatarData.Metadata))
                {
                    try
                    {
                        var metadataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(avatarData.Metadata);
                        if (metadataDict != null && metadataDict.ContainsKey("AvatarDetail"))
                        {
                            var avatarDetailJson = metadataDict["AvatarDetail"].ToString();
                            var avatarDetail = JsonSerializer.Deserialize<AvatarDetail>(avatarDetailJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (avatarDetail != null)
                            {
                                response.Result = avatarDetail;
                                response.IsError = false;
                                response.Message = "Avatar detail loaded successfully from Optimism";
                                return response;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Error parsing AvatarDetail from metadata: {ex.Message}", ex);
                        return response;
                    }
                }

                OASISErrorHandling.HandleError(ref response, $"Avatar detail with id {id} not found on Optimism");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAvatarDetailAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarDetails is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetails: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadAllAvatarDetailsAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadAllAvatarDetailsAsync: {ex.Message}");
            }
            return response;
        }

        // Holon-related methods
        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolon is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Optimism implementation: Load holon from smart contract
                var holonId = id.ToString();
                var getHolonFunction = _contract.GetFunction("getHolon");
                var holonData = await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);

                if (holonData != null)
                {
                    var holon = new Holon
                    {
                        Id = id,
                        Name = holonData.Name,
                        Description = holonData.Description,
                        HolonType = Enum.Parse<HolonType>(holonData.HolonType)
                    };

                    // Parse metadata if available
                    if (!string.IsNullOrEmpty(holonData.Metadata))
                    {
                        try
                        {
                            holon.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.Metadata);
                        }
                        catch { }
                    }

                    // Parse parent ID if available
                    if (!string.IsNullOrEmpty(holonData.ParentId) && Guid.TryParse(holonData.ParentId, out var parentId))
                    {
                        holon.ParentHolonId = parentId;
                    }

                    // Load children if requested
                    if (loadChildren && (maxChildDepth == 0 || maxChildDepth > 0))
                    {
                        var childrenResult = await LoadHolonsForParentAsync(id, HolonType.All, loadChildren, recursive, maxChildDepth, 0, continueOnError, loadChildrenFromProvider, version);
                        if (!childrenResult.IsError && childrenResult.Result != null)
                        {
                            holon.Children = childrenResult.Result.ToList();
                        }
                    }

                    response.Result = holon;
                    response.IsError = false;
                    response.Message = "Holon loaded successfully from Optimism";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref response, $"Holon with id {id} not found on Optimism");
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolon is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolon: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IHolon>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParent is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }

                if (_contract == null)
                {
                    OASISErrorHandling.HandleError(ref response, "Smart contract not initialized");
                    return response;
                }

                // Real Optimism implementation: Load all holons and filter by parent ID
                var getUserHolonsFunction = _contract.GetFunction("getUserHolons");
                var holonIds = await getUserHolonsFunction.CallAsync<List<string>>(_account.Address);

                var holons = new List<IHolon>();
                var parentIdStr = id.ToString();

                foreach (var holonId in holonIds)
                {
                    try
                    {
                        var getHolonFunction = _contract.GetFunction("getHolon");
                        var holonData = await getHolonFunction.CallDeserializingToObjectAsync<GetHolonOutputDTO>(holonId);

                        if (holonData != null && holonData.ParentId == parentIdStr)
                        {
                            var holon = new Holon
                            {
                                Id = Guid.Parse(holonId),
                                Name = holonData.Name,
                                Description = holonData.Description,
                                HolonType = Enum.Parse<HolonType>(holonData.HolonType),
                                ParentHolonId = id
                            };

                            // Parse metadata if available
                            if (!string.IsNullOrEmpty(holonData.Metadata))
                            {
                                try
                                {
                                    holon.MetaData = JsonSerializer.Deserialize<Dictionary<string, object>>(holonData.Metadata);
                                }
                                catch { }
                            }

                            // Filter by type if specified
                            if (type == HolonType.All || holon.HolonType == type)
                            {
                                // Load children if requested
                                if (loadChildren && (maxChildDepth == 0 || curentChildDepth < maxChildDepth))
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
                            OASISErrorHandling.HandleError(ref response, $"Error loading holon {holonId}: {ex.Message}", ex);
                            return response;
                        }
                    }
                }

                response.Result = holons;
                response.IsError = false;
                response.Message = $"Loaded {holons.Count} holons for parent {id} from Optimism";
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}", ex);
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParent is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParent: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsForParentAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsForParentAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaData is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaData: {ex.Message}");
            }
            return response;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaDataAsync is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaDataAsync: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var response = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!_isActivated)
                {
                    OASISErrorHandling.HandleError(ref response, "Optimism provider is not activated");
                    return response;
                }
                OASISErrorHandling.HandleError(ref response, "LoadHolonsByMetaData is not supported by Optimism provider");
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error in LoadHolonsByMetaData: {ex.Message}");
            }
            return response;
        }

    }
}
