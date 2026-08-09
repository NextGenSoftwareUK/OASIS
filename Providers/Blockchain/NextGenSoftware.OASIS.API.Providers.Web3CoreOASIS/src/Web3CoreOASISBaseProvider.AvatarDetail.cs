using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.Common;
using Nethereum.JsonRpc.Client;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Helpers;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using System.Net.Http;
using Newtonsoft.Json;
using System.Numerics;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;
using System.Threading;
using Nethereum.Web3.Accounts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Web3;
using Nethereum.Util;
using Nethereum.Hex.HexTypes;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public partial class Web3CoreOASISBaseProvider
{
    public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
    {
        return LoadAvatarDetailAsync(id, version).Result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
    {
        OASISResult<IAvatarDetail> result = new();
        string errorMessage = "Error in LoadAvatarDetailAsync method in Web3CoreOASIS while loading an avatar detail. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS detailInfo = await _web3CoreOASIS.GetAvatarDetailByIdAsync((uint)avatarDetailEntityId);

            result.IsError = false;
            result.IsLoaded = true;
            result.Result = Newtonsoft.Json.JsonConvert.DeserializeObject<AvatarDetail>(System.Text.Encoding.UTF8.GetString(detailInfo.Info))
                ?? throw new InvalidOperationException();
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
    {
        var result = new OASISResult<IAvatarDetail>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatarDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<AvatarDetail>(content);
                
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Web3Core: {ex.Message}", ex);
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var avatarDetail = Newtonsoft.Json.JsonConvert.DeserializeObject<AvatarDetail>(content);
                
                if (avatarDetail != null)
                {
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar detail from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
    {
        return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
    }

    public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        throw new Exception();
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holon = Newtonsoft.Json.JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonByProviderKeyAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        OASISResult<IHolon> result = new();
        string errorMessage = "Error in LoadHolonAsync method in Web3CoreOASIS while loading holon. Reason: ";

        if (_web3CoreOASIS is null)
        {
            OASISErrorHandling.HandleError(
                ref result, Web3CoreOASISBaseProviderHelper.ProviderNotActivatedError);
            return result;
        }

        try
        {
            int holonEntityId = HashUtility.GetNumericHash(id.ToString());
            EntityOASIS holonInfo = await _web3CoreOASIS.GetHolonByIdAsync((uint)holonEntityId);

            result.Result = Newtonsoft.Json.JsonConvert.DeserializeObject<Holon>(System.Text.Encoding.UTF8.GetString(holonInfo.Info))
                ?? throw new InvalidOperationException();
            result.IsError = false;
            result.IsLoaded = true;
        }
        catch (RpcResponseException ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
        }

        return result;
    }

    public async Task<OASISResult<IHolon>> LoadHolonByProviderKeyAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IHolon>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holon = Newtonsoft.Json.JsonConvert.DeserializeObject<Holon>(content);
                
                if (holon != null)
                {
                    result.Result = holon;
                    result.IsError = false;
                    result.Message = "Holon loaded successfully by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holon from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override OASISResult<IHolon> LoadHolonByMetaData(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IHolon>> LoadHolonByMetaDataAsync(string metaKey, string metaValue, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent/{id}?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByProviderKeyAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent-by-provider-key/{Uri.EscapeDataString(providerKey)}?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsForParentByProviderKeyAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/for-parent-by-provider-key/{Uri.EscapeDataString(providerKey)}?type={type}&version={version}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var holons = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Holon>>(content);
                
                if (holons != null)
                {
                    result.Result = holons.Cast<IHolon>();
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent by provider key from Web3Core";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Web3Core API");
                }
            }
            else
            {
                OASISErrorHandling.HandleError(ref result, $"Web3Core API error: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Web3Core: {ex.Message}", ex);
        }
        return result;
    }

    //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    //{
    //    throw new NotImplementedException();
    //}

    public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        var result = new OASISResult<IEnumerable<IHolon>>();
        try
        {
            if (!IsProviderActivated)
            {
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Web3Core is a blockchain provider, not a storage provider
            // Return empty result as blockchain providers don't store holons by metadata
            result.Result = new List<IHolon>();
            result.IsError = false;
            result.Message = "Web3Core blockchain provider does not support metadata-based holon queries";
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
                OASISErrorHandling.HandleError(ref result, "Web3Core provider is not activated");
                return result;
            }

            // Web3Core is a blockchain provider, not a storage provider
            // Return empty result as blockchain providers don't store holons by metadata
            result.Result = new List<IHolon>();
            result.IsError = false;
            result.Message = "Web3Core blockchain provider does not support metadata-based holon queries";
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata: {ex.Message}", ex);
        }
        return result;
    }

    public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
    {
        return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
    }

    public bool NativeCodeGenesis(ICelestialBody celestialBody, string outputFolder, string nativeSource)
    {
        try
        {
            if (string.IsNullOrEmpty(outputFolder))
                return false;

            string solidityFolder = Path.Combine(outputFolder, "Solidity");
            if (!Directory.Exists(solidityFolder))
                Directory.CreateDirectory(solidityFolder);

            if (!string.IsNullOrEmpty(nativeSource))
            {
                File.WriteAllText(Path.Combine(solidityFolder, "Contract.sol"), nativeSource);
                return true;
            }

            if (celestialBody == null)
                return true;

            var sb = new StringBuilder();
            sb.AppendLine("// SPDX-License-Identifier: MIT");
            sb.AppendLine("// Auto-generated by Web3CoreOASISBaseProvider.NativeCodeGenesis");
            sb.AppendLine("pragma solidity ^0.8.0;");
            sb.AppendLine();
            sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "Web3Contract"} {{");
            sb.AppendLine("    // Holon structs");

            var zomes = celestialBody.CelestialBodyCore?.Zomes;
            if (zomes != null)
            {
                foreach (var zome in zomes)
                {
                    if (zome?.Children == null) continue;

                    foreach (var holon in zome.Children)
                    {
                        if (holon == null || string.IsNullOrWhiteSpace(holon.Name)) continue;

                        var holonTypeName = holon.Name.ToPascalCase();
                        sb.AppendLine($"    struct {holonTypeName} {{");
                        sb.AppendLine("        string id;");
                        sb.AppendLine("        string name;");
                        sb.AppendLine("        string description;");
                        if (holon.Nodes != null)
                        {
                            foreach (var node in holon.Nodes)
                            {
                                if (node != null && !string.IsNullOrWhiteSpace(node.NodeName))
                                {
                                    string solidityType = "string";
                                    switch (node.NodeType)
                                    {
                                        case NodeType.Int:
                                            solidityType = "uint256";
                                            break;
                                        case NodeType.Bool:
                                            solidityType = "bool";
                                            break;
                                    }
                                    sb.AppendLine($"        {solidityType} {node.NodeName.ToSnakeCase()};");
                                }
                            }
                        }
                        sb.AppendLine("    }");
                        sb.AppendLine($"    mapping(string => {holonTypeName}) private {holonTypeName.ToCamelCase()}s;");
                        sb.AppendLine($"    string[] private {holonTypeName.ToCamelCase()}Ids;");
                        sb.AppendLine();

                        sb.AppendLine($"    function create{holonTypeName}(string memory id, string memory name, string memory description) public {{");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}s[id] = {holonTypeName}(id, name, description);");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}Ids.push(id);");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();

                        sb.AppendLine($"    function get{holonTypeName}(string memory id) public view returns (string memory, string memory, string memory) {{");
                        sb.AppendLine($"        {holonTypeName} storage {holonTypeName.ToCamelCase()} = {holonTypeName.ToCamelCase()}s[id];");
                        sb.AppendLine($"        return ({holonTypeName.ToCamelCase()}.id, {holonTypeName.ToCamelCase()}.name, {holonTypeName.ToCamelCase()}.description);");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();

                        sb.AppendLine($"    function update{holonTypeName}(string memory id, string memory name, string memory description) public {{");
                        sb.AppendLine($"        {holonTypeName} storage {holonTypeName.ToCamelCase()} = {holonTypeName.ToCamelCase()}s[id];");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}.name = name;");
                        sb.AppendLine($"        {holonTypeName.ToCamelCase()}.description = description;");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();

                        sb.AppendLine($"    function delete{holonTypeName}(string memory id) public {{");
                        sb.AppendLine($"        delete {holonTypeName.ToCamelCase()}s[id];");
                        sb.AppendLine($"        for (uint i = 0; i < {holonTypeName.ToCamelCase()}Ids.length; i++) {{");
                        sb.AppendLine($"            if (keccak256(abi.encodePacked({holonTypeName.ToCamelCase()}Ids[i])) == keccak256(abi.encodePacked(id))) {{");
                        sb.AppendLine($"                {holonTypeName.ToCamelCase()}Ids[i] = {holonTypeName.ToCamelCase()}Ids[{holonTypeName.ToCamelCase()}Ids.length - 1];");
                        sb.AppendLine($"                {holonTypeName.ToCamelCase()}Ids.pop();");
                        sb.AppendLine($"                break;");
                        sb.AppendLine($"            }}");
                        sb.AppendLine($"        }}");
                        sb.AppendLine($"    }}");
                        sb.AppendLine();
                    }
                }
            }

            sb.AppendLine("}");
            File.WriteAllText(Path.Combine(solidityFolder, "Contract.sol"), sb.ToString());
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

}
