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
        public OASISResult<IEnumerable<IAvatar>> GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var allAvatarsResult = LoadAllAvatars();
                if (allAvatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {allAvatarsResult.Message}");
                    return result;
                }

                var nearby = new List<IAvatar>();
                foreach (var avatar in allAvatarsResult.Result)
                {
                    var meta = avatar.MetaData;
                    if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                    {
                        if (double.TryParse(meta["Latitude"]?.ToString(), out double aLat) &&
                            double.TryParse(meta["Longitude"]?.ToString(), out double aLong))
                        {
                            double distance = GeoHelper.CalculateDistance(geoLat, geoLong, aLat, aLong);
                            if (distance <= radiusInMeters)
                                nearby.Add(avatar);
                        }
                    }
                }

                result.Result = nearby;
                result.Message = $"Retrieved {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IEnumerable<IHolon>> GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate TRON provider: {activateResult.Message}");
                        return result;
                    }
                }

                var allHolonsResult = LoadAllHolons(Type);
                if (allHolonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {allHolonsResult.Message}");
                    return result;
                }

                var nearby = new List<IHolon>();
                foreach (var holon in allHolonsResult.Result)
                {
                    var meta = holon.MetaData;
                    if (meta != null && meta.ContainsKey("Latitude") && meta.ContainsKey("Longitude"))
                    {
                        if (double.TryParse(meta["Latitude"]?.ToString(), out double hLat) &&
                            double.TryParse(meta["Longitude"]?.ToString(), out double hLong))
                        {
                            double distance = GeoHelper.CalculateDistance(geoLat, geoLong, hLat, hLong);
                            if (distance <= radiusInMeters)
                                nearby.Add(holon);
                        }
                    }
                }

                result.Result = nearby;
                result.Message = $"Retrieved {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from TRON: {ex.Message}", ex);
            }
            return result;
        }

        // distance helpers moved to GeoHelper for reuse


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
                sb.AppendLine("// Auto-generated by TRONOASIS.NativeCodeGenesis");
                sb.AppendLine("pragma solidity ^0.8.0;");
                sb.AppendLine();
                sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "TRONContract"} {{");
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


        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var response = new OASISResult<ITransactionResponse>();

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = await ActivateProviderAsync();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate TRON provider: {activateResult.Message}");
                        return response;
                    }
                }

                await _runtimeLifecycle.WaitAsync();
                try
                {
                    if (!IsProviderActivated || _nativeTransfers == null || _nativeSigningKeyResolver == null)
                        throw new InvalidOperationException("TRON signed transfer runtime is not active or signing credentials are not configured.");
                    var privateKey = await _nativeSigningKeyResolver(fromWalletAddress);
                    var transactionId = await _nativeTransfers.SendAsync(fromWalletAddress, toWalletAddress, amount, privateKey, memoText);
                    response.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionId
                    };
                    response.IsError = false;
                    response.Message = "TRON transfer confirmed by a solidified receipt.";
                }
                finally { _runtimeLifecycle.Release(); }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref response, $"Error sending TRON transaction: {ex.Message}");
            }

            return response;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
            => SendAvatarTokenByIdAsync(fromAvatarId, toAvatarId, amount, "TRX");

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
            => SendAvatarTokenByIdAsync(fromAvatarId, toAvatarId, amount, token);

        public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
            => SendAvatarTokenAsync(fromAvatarUsername, toAvatarUsername, amount, "TRX", byEmail: false);

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

    }
}
