using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.IO;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS
{
    public partial class HashgraphOASIS
    {
        public async Task<OASISResult<IEnumerable<IAvatar>>> GetPlayersNearMeAsync()
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get all avatars and convert to players from Hashgraph
                var avatarsResult = await LoadAllAvatarsAsync();
                if (avatarsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var players = new List<IAvatar>();
                foreach (var avatar in avatarsResult.Result)
                {
                    var player = new Avatar
                    {
                        Id = avatar.Id,
                        Username = avatar.Username,
                        Email = avatar.Email,
                        FirstName = avatar.FirstName,
                        LastName = avatar.LastName,
                        CreatedDate = avatar.CreatedDate,
                        ModifiedDate = avatar.ModifiedDate
                    };
                    players.Add(player);
                }

                result.Result = players;
                result.IsError = false;
                result.Message = $"Successfully loaded {players.Count} players near me from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting players near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
        }

        // Removed explicit interface implementation that doesn't exist in the interface

        public async Task<OASISResult<IEnumerable<IHolon>>> GetHolonsNearMeAsync(HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get all holons from Hashgraph
                var holonsResult = await LoadAllHolonsAsync();
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var holons = holonsResult.Result?.ToList() ?? new List<IHolon>();

                // Add location metadata
                // Add metadata to holons if needed
                foreach (var holon in holons)
                {
                    if (holon.MetaData == null)
                        holon.MetaData = new Dictionary<string, object>();

                    holon.MetaData["NearMe"] = true;
                    holon.MetaData["Distance"] = 0.0; // Would be calculated based on actual location
                    holon.MetaData["Provider"] = "HashgraphOASIS";
                }

                result.Result = holons;
                result.IsError = false;
                result.Message = $"Successfully loaded {holons.Count} holons near me from Hashgraph";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Hashgraph: {ex.Message}", ex);
            }
            return result;
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
                sb.AppendLine("// Auto-generated by HashgraphOASIS.NativeCodeGenesis");
                sb.AppendLine("pragma solidity ^0.8.0;");
                sb.AppendLine();
                sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "HashgraphContract"} {{");
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
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromWalletAddress,
                    ToAddress = toWalletAddress,
                    Amount = amount,
                    Memo = memoText
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionAsync: {ex.Message}", ex);
            }
            return result;
        }


        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for avatars");
                    return result;
                }

                // Create Hashgraph transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarId} to {toAvatarId}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.HashgraphOASIS);
                var toWalletResult = await WalletManager.Instance.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.HashgraphOASIS);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for avatars");
                    return result;
                }

                var fromAddress = fromWalletResult.Result?.WalletAddress;
                var toAddress = toWalletResult.Result?.WalletAddress;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for avatars");
                    return result;
                }

                // Create Hashgraph token transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    TokenId = token,
                    Memo = $"OASIS token transaction from {fromAvatarId} to {toAvatarId}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph token transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph token transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph token transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByIdAsync with token: {ex.Message}", ex);
            }
            return result;
        }


        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for usernames");
                    return result;
                }

                // Create Hashgraph transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarUsername} to {toAvatarUsername}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarUsername);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarUsername);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for usernames");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for usernames");
                    return result;
                }

                // Create Hashgraph token transaction using Mirror Node API
                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    TokenId = token,
                    Memo = $"OASIS token transaction from {fromAvatarUsername} to {toAvatarUsername}"
                };

                // Submit transaction to Hashgraph network
                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph token transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph token transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph token transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByUsernameAsync with token: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    OASISErrorHandling.HandleError(ref result, "Hashgraph provider is not activated");
                    return result;
                }

                // Get wallet addresses using WalletHelper
                var fromWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, fromAvatarEmail);
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager.Instance, Core.Enums.ProviderType.HashgraphOASIS, toAvatarEmail);

                if (fromWalletResult.IsError || toWalletResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to get wallet addresses for emails");
                    return result;
                }

                var fromAddress = fromWalletResult.Result;
                var toAddress = toWalletResult.Result;

                if (string.IsNullOrEmpty(fromAddress) || string.IsNullOrEmpty(toAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not find wallet addresses for emails");
                    return result;
                }

                var transactionData = new HashgraphTransactionData
                {
                    FromAddress = fromAddress,
                    ToAddress = toAddress,
                    Amount = amount,
                    Memo = $"OASIS transaction from {fromAvatarEmail} to {toAvatarEmail}"
                };

                var hashgraphClient = new HashgraphClient();
                var transactionResult = await hashgraphClient.SendTransactionAsync(transactionData);

                if (transactionResult != null)
                {
                    result.Result = new NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses.TransactionResponse
                    {
                        TransactionResult = transactionResult.TransactionId ?? "Hashgraph transaction sent successfully"
                    };
                    result.IsError = false;
                    result.Message = "Hashgraph transaction sent successfully";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Failed to send Hashgraph transaction");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error in SendTransactionByEmailAsync: {ex.Message}", ex);
            }
            return result;
        }

    }
}
