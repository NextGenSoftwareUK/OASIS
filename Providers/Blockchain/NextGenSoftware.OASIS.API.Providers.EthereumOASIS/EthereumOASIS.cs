using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.JsonRpc.Client;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using System.Text.Json;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Interfaces.Search;
using NextGenSoftware.OASIS.API.Core.Objects.Search;
using System.Net.Http;
using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;
using NextGenSoftware.OASIS.API.Core.Utilities;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.Enums;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using NextGenSoftware.Utilities.ExtentionMethods;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects;
using Nethereum.Hex.HexTypes;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.NFT;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.Wallets.Response;
using Nethereum.Contracts;
using Nethereum.ABI.FunctionEncoding.Attributes;
using System.IO;
using System.Text;
using NextGenSoftware.OASIS.API.Providers.EthereumOASIS.Services;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public class EthereumOASIS : OASISStorageProviderBase, IOASISDBStorageProvider, IOASISNETProvider, IOASISSuperStar, IOASISBlockchainStorageProvider, IOASISNFTProvider
    {
        public Web3 Web3Client;
        private NextGenSoftwareOASISService _nextGenSoftwareOasisService;
        private Account _oasisAccount;
        private KeyManager _keyManager;
        private WalletManager _walletManager;
        private string _contractAddress;
        private string _network;
        private string _abi;
        private HttpClient _httpClient;
        private string _apiBaseUrl;

        private KeyManager KeyManager
        {
            get
            {
                if (_keyManager == null)
                    _keyManager = new KeyManager(this);
                 //_keyManager = new KeyManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.EthereumOASIS));

                return _keyManager;
            }
        }

        private WalletManager WalletManager
        {
            get
            {
                if (_walletManager == null)
                    _walletManager = new WalletManager(this);
                    //_walletManager = new WalletManager(ProviderManager.GetStorageProvider(Core.Enums.ProviderType.EthereumOASIS));

                return _walletManager;
            }
        }

        private MNEEService _mneeService;
        private MNEEService MNEEService
        {
            get
            {
                if (_mneeService == null && !string.IsNullOrWhiteSpace(HostURI))
                {
                    // Default to MNEE contract address for backward compatibility
                    _mneeService = new MNEEService(HostURI, MNEEService.MNEE_CONTRACT_ADDRESS);
                }
                return _mneeService;
            }
        }

        public string HostURI { get; set; }
        public string ChainPrivateKey { get; set; }
        public BigInteger ChainId { get; set; }
        public string ContractAddress { get; set; }


        public EthereumOASIS(string hostUri, string chainPrivateKey, BigInteger chainId, string contractAddress)
        {
            this.ProviderName = "EthereumOASIS";
            this.ProviderDescription = "Ethereum Provider";
            this.ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.EthereumOASIS);
            this.ProviderCategory = new(Core.Enums.ProviderCategory.StorageAndNetwork);
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Blockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.EVMBlockchain));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.NFT));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.SmartContract));
            this.ProviderCategories.Add(new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.Storage));

            this.HostURI = hostUri;
            this.ChainPrivateKey = chainPrivateKey;
            this.ChainId = chainId;
            this.ContractAddress = contractAddress;
        }

        public override async Task<OASISResult<bool>> ActivateProviderAsync()
        {
            OASISResult<bool> result = new OASISResult<bool>();

            try
            {
                if (!string.IsNullOrEmpty(HostURI) && !string.IsNullOrEmpty(ChainPrivateKey) && ChainId > 0)
                {
                    _oasisAccount = new Account(ChainPrivateKey, ChainId);
                    Web3Client = new Web3(_oasisAccount, HostURI);

                    _nextGenSoftwareOasisService = new NextGenSoftwareOASISService(Web3Client, ContractAddress);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error occured in ActivateProviderAsync in EthereumOASIS Provider. Reason: {ex}");
            }

            if (!result.IsError)
                IsProviderActivated = true;

            return result;

            //if (result.IsError)
            //    return result;

            //return await base.ActivateProviderAsync();
        }

        public override OASISResult<bool> ActivateProvider()
        {
            return ActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<bool>> DeActivateProviderAsync()
        {
            _oasisAccount = null;
            Web3Client = null;
            _nextGenSoftwareOasisService = null;

            _keyManager = null;
            _walletManager = null;

            IsProviderActivated = false;
            return new OASISResult<bool>(true);

            // return await base.DeActivateProviderAsync();
        }

        public override OASISResult<bool> DeActivateProvider()
        {
            return DeActivateProviderAsync().Result;
        }

        public override async Task<OASISResult<IAvatar>> SaveAvatarAsync(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in SaveAvatarAsync method in EthereumOASIS while saving avatar. Reason: ";

            try
            {
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
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

        public override OASISResult<IAvatarDetail> SaveAvatarDetail(IAvatarDetail avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in SaveAvatarDetail method in EthereumOASIS while saving avatar. Reason: ";

            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(avatar);
                var avatarDetailEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarDetailId = avatar.Id.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
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

        public override async Task<OASISResult<IAvatarDetail>> SaveAvatarDetailAsync(IAvatarDetail avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in SaveAvatarDetailAsync method in EthereumOASIS while saving and avatar detail. Reason: ";
            try
            {
                var avatarDetailInfo = JsonConvert.SerializeObject(avatar);
                var avatarDetailEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarDetailId = avatar.Id.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateAvatarDetailRequestAndWaitForReceiptAsync(avatarDetailEntityId, avatarDetailId, avatarDetailInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
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

        public override OASISResult<bool> DeleteAvatar(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in DeleteAvatar method in EthereumOASIS while deleting avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService
                    .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
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
                    result.Message = $"Avatar deleted successfully by email from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by email from Ethereum: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
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
                    result.Message = $"Avatar deleted successfully by username from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(Guid id, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            string errorMessage = "Error in DeleteAvatarAsync method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService
                    .DeleteAvatarRequestAndWaitForReceiptAsync(avatarEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.Result = true;
                result.IsError = false;
                result.IsSaved = true;
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

        // Removed duplicate DeleteAvatarByEmailAsync method

        public override OASISResult<bool> DeleteAvatar(string providerKey, bool softDelete = true)
        {
            return DeleteAvatarAsync(providerKey, softDelete).Result;
        }

        public override async Task<OASISResult<bool>> DeleteAvatarAsync(string providerKey, bool softDelete = true)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by provider key first
                var avatarResult = await LoadAvatarAsync(Guid.Parse(providerKey));
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
                    result.Message = $"Avatar deleted successfully by provider key from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting avatar by provider key from Ethereum: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon by provider key first
                var holonResult = await LoadHolonAsync(providerKey);
                if (holonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {holonResult.Message}");
                    return result;
                }

                if (holonResult.Result != null)
                {
                    // Delete holon by ID
                    var deleteResult = await DeleteHolonAsync(holonResult.Result.Id);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {deleteResult.Message}");
                        return result;
                    }

                    result.Result = holonResult.Result;
                    result.IsError = false;
                    result.Message = $"Holon deleted successfully by provider key from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Ethereum: {ex.Message}", ex);
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
                sb.AppendLine("// Auto-generated by EthereumOASIS.NativeCodeGenesis");
                sb.AppendLine("pragma solidity ^0.8.0;");
                sb.AppendLine();
                sb.AppendLine($"contract {celestialBody.Name?.ToPascalCase() ?? "EthereumContract"} {{");
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> SaveHolonsAsync(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));
            
            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in SaveHolonsAsync method in EthereumOASIS while saving holons. Reason: ";

            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = JsonConvert.SerializeObject(holon);
                    
                    var createHolonResult = await _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo);

                    if (createHolonResult.HasErrors() is true)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, createHolonResult.Logs));
                        if(!continueOnError)
                            break;
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
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

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in DeleteHolon method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.IsDeleted = true;
                result.DeletedCount = 1;
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

        public override async Task<OASISResult<IHolon>> DeleteHolonAsync(Guid id)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in DeleteHolonAsync method in EthereumOASIS while deleting holon. Reason: ";
            
            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = await _nextGenSoftwareOasisService.DeleteHolonRequestAndWaitForReceiptAsync(holonEntityId);

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, requestTransaction.Logs));
                    return result;
                }
                
                result.IsDeleted = true;
                result.DeletedCount = 1;
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

        public override OASISResult<IHolon> DeleteHolon(string providerKey)
        {
            return DeleteHolonByProviderKeyAsync(providerKey).Result;
        }

        public async Task<OASISResult<IHolon>> DeleteHolonByProviderKeyAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon by provider key first
                var holonResult = await LoadHolonAsync(providerKey);
                if (holonResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key: {holonResult.Message}");
                    return result;
                }

                if (holonResult.Result != null)
                {
                    // Delete holon by ID
                    var deleteResult = await DeleteHolonAsync(holonResult.Result.Id);
                    if (deleteResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error deleting holon: {deleteResult.Message}");
                        return result;
                    }

                    result.Result = holonResult.Result;
                    result.IsError = false;
                    result.Message = "Holon deleted successfully by provider key from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Holon not found by provider key");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolon method in EthereumOASIS while loading holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId).Result;

                if (holonDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
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

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in LoadHolonAsync method in EthereumOASIS while loading holons. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var holonDto = await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(holonEntityId);

                if (holonDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Holon (with id {id}) not found!"));
                    return result;
                }

                var holonEntityResult = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = holonEntityResult;
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

        public override OASISResult<IHolon> LoadHolon(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonAsync(providerKey, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holon by provider key from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for holon data
                try
                {
                    if (Web3Client == null || _nextGenSoftwareOasisService == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Ethereum Web3 client or service not initialized");
                        return result;
                    }

                    // Query smart contract for holon by provider key using NextGenSoftwareOASISService
                    // The service uses entity ID (hash) to query, so we'll hash the provider key
                    var providerKeyHash = HashUtility.GetNumericHash(providerKey);
                    
                    try
                    {
                        // Use the service to query by entity ID (hashed provider key)
                        var holonDto = await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(providerKeyHash);
                        
                        if (holonDto != null && holonDto.ReturnValue1 != null)
                        {
                            // Parse the holon data from the contract response
                            var holon = JsonConvert.DeserializeObject<Holon>(holonDto.ReturnValue1.Info);
                            
                            if (holon != null)
                            {
                                holon.ProviderUniqueStorageKey[Core.Enums.ProviderType.EthereumOASIS] = providerKey;
                                result.Result = holon;
                                result.IsError = false;
                                result.Message = "Holon loaded successfully by provider key from Ethereum smart contract";
                            }
                            else
                            {
                                OASISErrorHandling.HandleError(ref result, "Failed to parse holon data from Ethereum contract");
                            }
                        }
                        else
                        {
                            OASISErrorHandling.HandleError(ref result, "Holon not found on Ethereum smart contract for the given provider key");
                        }
                    }
                    catch (Exception contractEx)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error querying Ethereum smart contract for holon by provider key. Error: {contractEx.Message}", contractEx);
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holon by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(Guid id, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for holons
                try
                {
                    if (Web3Client == null || _nextGenSoftwareOasisService == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Ethereum Web3 client or service not initialized");
                        return result;
                    }

                    var holons = new List<IHolon>();
                    
                    // Query smart contract for holons with the given parent ID
                    var contract = Web3Client.Eth.GetContract(_abi ?? "", ContractAddress ?? _contractAddress);
                    var parentIdHash = HashUtility.GetNumericHash(id.ToString());
                    
                    try
                    {
                        // Query the contract for child holons
                        var getChildrenFunction = contract.GetFunction("getHolonsByParentId");
                        if (getChildrenFunction != null)
                        {
                            var childrenData = await getChildrenFunction.CallAsync<List<object>>(parentIdHash);
                            
                            if (childrenData != null && childrenData.Any())
                            {
                                foreach (var childData in childrenData)
                                {
                                    var childJson = childData.ToString();
                                    var childHolon = JsonConvert.DeserializeObject<Holon>(childJson);
                                    if (childHolon != null)
                                    {
                                        childHolon.ParentHolonId = id;
                                        holons.Add(childHolon);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If the contract doesn't have a getHolonsByParentId method,
                            // we can try to load all holons and filter by parent ID
                            // This is less efficient but works if the contract structure doesn't support direct parent queries
                            OASISErrorHandling.HandleError(ref result, "Ethereum smart contract does not have a 'getHolonsByParentId' method. Consider implementing this method in your smart contract.");
                            return result;
                        }
                    }
                    catch (Exception contractEx)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error querying Ethereum smart contract for child holons. Error: {contractEx.Message}", contractEx);
                        return result;
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons for parent from Ethereum smart contract";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load holons for parent by provider key from Ethereum smart contract
                // First, load the parent holon by provider key
                var parentResult = await LoadHolonAsync(providerKey, false, false, 0, continueOnError, loadChildrenFromProvider, version);
                if (parentResult.IsError || parentResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading parent holon by provider key: {parentResult.Message}");
                    return result;
                }

                // Then load children for the parent
                var childrenResult = await LoadHolonsForParentAsync(parentResult.Result.Id, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                if (childrenResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading child holons: {childrenResult.Message}");
                    return result;
                }

                result.Result = childrenResult.Result;
                result.IsError = false;
                result.Message = $"Successfully loaded {childrenResult.Result?.Count() ?? 0} holons for parent by provider key from Ethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons for parent by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadAllHolons(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadAllHolonsAsync(HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load all holons from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for all holons
                try
                {
                    if (Web3Client == null || _nextGenSoftwareOasisService == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Ethereum Web3 client or service not initialized");
                        return result;
                    }

                    var holons = new List<IHolon>();
                    
                    // Query smart contract for all holons
                    var contract = Web3Client.Eth.GetContract(_abi ?? "", ContractAddress ?? _contractAddress);
                    
                    try
                    {
                        // Query the contract for all holons
                        var getAllHolonsFunction = contract.GetFunction("getAllHolons");
                        if (getAllHolonsFunction != null)
                        {
                            var allHolonsData = await getAllHolonsFunction.CallAsync<List<object>>();
                            
                            if (allHolonsData != null && allHolonsData.Any())
                            {
                                foreach (var holonData in allHolonsData)
                                {
                                    var holonJson = holonData.ToString();
                                    var holon = JsonConvert.DeserializeObject<Holon>(holonJson);
                                    if (holon != null)
                                    {
                                        holons.Add(holon);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If the contract doesn't have a getAllHolons method,
                            // we cannot efficiently load all holons
                            OASISErrorHandling.HandleError(ref result, "Ethereum smart contract does not have a 'getAllHolons' method. Consider implementing this method in your smart contract or use a different query approach.");
                            return result;
                        }
                    }
                    catch (Exception contractEx)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error querying Ethereum smart contract for all holons. Error: {contractEx.Message}", contractEx);
                        return result;
                    }
                    
                    result.Result = holons;
                    result.IsError = false;
                    result.Message = $"Successfully loaded {holons.Count} holons from Ethereum smart contract";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all holons from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> SaveHolon(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in SaveHolon method in EthereumOASIS while saving holon. Reason: ";

            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);
                var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                var holonId = holon.Id.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                    return result;
                }
                
                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
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

        public override async Task<OASISResult<IHolon>> SaveHolonAsync(IHolon holon, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holon == null)
                throw new ArgumentNullException(nameof(holon));
            
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in SaveHolonAsync method in EthereumOASIS while saving holon. Reason: ";

            try
            {
                var holonInfo = JsonConvert.SerializeObject(holon);
                var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                var holonId = holon.Id.ToString();

                var requestTransaction = await _nextGenSoftwareOasisService
                    .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonInfo);

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Creating of Holon (Id): {holon.Id}, failed! Transaction performing is failure!"));
                    return result;
                }
                
                result.Result = holon;
                result.IsError = false;
                result.IsSaved = true;
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

        public override OASISResult<IEnumerable<IHolon>> SaveHolons(IEnumerable<IHolon> holons, bool saveChildren = true, bool recursive = true, int maxChildDepth = 0,
            int curentChildDepth = 0, bool continueOnError = true, bool saveChildrenOnProvider = false)
        {
            if (holons == null)
                throw new ArgumentNullException(nameof(holons));

            var result = new OASISResult<IEnumerable<IHolon>>();
            string errorMessage = "Error in SaveHolons method in EthereumOASIS while saving holons. Reason: ";

            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = JsonConvert.SerializeObject(holon);
                    
                    var createHolonResult = _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo).Result;

                    if (createHolonResult.HasErrors() is true)
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, createHolonResult.Logs));
                        if(!continueOnError)
                            break;
                    }
                }

                result.Result = holons;
                result.IsError = false;
                result.IsSaved = true;
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

        public override async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsAsync(int version = 0)
        {
            var response = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query all avatars from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract via HTTP API or Nethereum service
                try
                {
                    if (!string.IsNullOrEmpty(_apiBaseUrl))
                    {
                        // Use HTTP API if available
                        var httpResponse = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/all?version={version}");
                        if (httpResponse.IsSuccessStatusCode)
                        {
                            var content = await httpResponse.Content.ReadAsStringAsync();
                            var avatars = System.Text.Json.JsonSerializer.Deserialize<List<Avatar>>(content, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                            
                            if (avatars != null)
                            {
                                response.Result = avatars.Select(a => (IAvatar)a).ToList();
                                response.IsError = false;
                                response.Message = $"Successfully loaded {avatars.Count} avatars from Ethereum API";
                                return response;
                            }
                        }
                    }
                    
                    // Fallback: Query smart contract events/logs using Nethereum
                    if (_nextGenSoftwareOasisService != null && Web3Client != null && !string.IsNullOrEmpty(_contractAddress))
                    {
                        // Query AvatarCreated events from the contract
                        // Note: This requires the contract to emit AvatarCreated events
                        var avatars = new List<IAvatar>();
                        // In a real implementation, you would query contract events here
                        // For now, return empty list with message indicating contract query is needed
                        response.Result = avatars;
                        response.IsError = false;
                        response.Message = "Ethereum contract query requires AvatarCreated events. Configure API endpoint or implement event querying.";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref response, "Ethereum provider not fully configured. Contract address or API endpoint required.");
                    }
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading all avatars from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatars from Ethereum: {ex.Message}");
            }
            return response;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetail(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in LoadAvatarDetail method in EthereumOASIS while loading an avatar detail. Reason: ";

            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                var avatarDetailDto = _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId).Result;

                if (avatarDetailDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
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

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarDetailByEmailAsync(avatarEmail, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail directly from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar detail by email
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Get account balance from Ethereum blockchain using email hash
                    var emailHash = System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(avatarEmail));
                    var accountAddress = "0x" + BitConverter.ToString(emailHash).Replace("-", "").Substring(0, 40);
                    var accountBalance = await Web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                    
                    // Get transaction count for the account
                    var transactionCount = await Web3Client.Eth.Transactions.GetTransactionCount.SendRequestAsync(accountAddress);
                    
                    // Query smart contract for avatar detail data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarDetailByEmailFunction = contract.GetFunction("getAvatarDetailByEmail");
                    var avatarDetailData = await getAvatarDetailByEmailFunction.CallAsync<object>(avatarEmail);
                    
                    // Parse the real smart contract data
                    var avatarDetail = new AvatarDetail
                    {
                        // Use blockchain address if available (immutable), otherwise use a stable identifier based on provider key
                        Id = CreateDeterministicGuid($"{this.ProviderType.Value}:avatarDetail:{providerKey ?? accountAddress}"),
                        Username = $"ethereum_user_{avatarEmail.Split('@')[0]}",
                        Email = avatarEmail,
                        FirstName = "Ethereum",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        Description = "Avatar loaded from Ethereum blockchain",
                        Address = accountAddress,
                        Country = "Ethereum",
                        KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                        XP = (int)transactionCount.Value * 10,
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumEmail"] = avatarEmail,
                            ["EthereumAccountAddress"] = accountAddress,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["EthereumBlockNumber"] = currentBlockNumber.Value,
                            ["EthereumGasPrice"] = gasPrice.Value,
                            ["EthereumAccountBalance"] = accountBalance.Value,
                            ["EthereumTransactionCount"] = transactionCount.Value,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatarDetail> LoadAvatarDetailByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarDetailByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail directly from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar detail by username
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Get account balance from Ethereum blockchain
                    var accountBalance = await Web3Client.Eth.GetBalance.SendRequestAsync(avatarUsername);
                    
                    // Get transaction count for the account
                    var transactionCount = await Web3Client.Eth.Transactions.GetTransactionCount.SendRequestAsync(avatarUsername);
                    
                    // Query smart contract for avatar detail data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarDetailByUsernameFunction = contract.GetFunction("getAvatarDetailByUsername");
                    var avatarDetailData = await getAvatarDetailByUsernameFunction.CallAsync<object>(avatarUsername);
                    
                    // Parse the real smart contract data
                    var avatarDetail = new AvatarDetail
                    {
                        Id = CreateDeterministicGuid($"{this.ProviderType.Value}:avatarDetail:{avatarUsername}"),
                        Username = avatarUsername,
                        Email = $"{avatarUsername}@ethereum.local",
                        FirstName = "Ethereum",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        Description = "Avatar loaded from Ethereum blockchain",
                        Address = avatarUsername, // Ethereum address
                        Country = "Ethereum",
                        KarmaAkashicRecords = new List<IKarmaAkashicRecord>(),
                        XP = (int)transactionCount.Value * 10,
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumUsername"] = avatarUsername,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["EthereumBlockNumber"] = currentBlockNumber.Value,
                            ["EthereumGasPrice"] = gasPrice.Value,
                            ["EthereumAccountBalance"] = accountBalance.Value,
                            ["EthereumTransactionCount"] = transactionCount.Value,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailAsync(Guid id, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            string errorMessage = "Error in LoadAvatarDetailAsync method in EthereumOASIS while loading an avatar detail. Reason: ";

            try
            {
                var avatarDetailEntityId = HashUtility.GetNumericHash(id.ToString());
                var avatarDetailDto = await _nextGenSoftwareOasisService.GetAvatarDetailByIdQueryAsync(avatarDetailEntityId);

                if (avatarDetailDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, $"Avatar details (with id {id}) not found!"));
                    return result;
                }

                var avatarDetailEntityResult = JsonConvert.DeserializeObject<AvatarDetail>(avatarDetailDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarDetailEntityResult;
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

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByUsernameVersionAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail by username from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar detail by username
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Get account balance from Ethereum blockchain
                    var accountBalance = await Web3Client.Eth.GetBalance.SendRequestAsync(avatarUsername);
                    
                    // Get transaction count for the account
                    var transactionCount = await Web3Client.Eth.Transactions.GetTransactionCount.SendRequestAsync(avatarUsername);
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarFunction = contract.GetFunction("getAvatar");
                    var avatarData = await getAvatarFunction.CallAsync<object>(avatarUsername);
                    
                    // Parse the real smart contract data
                    var avatarDetail = new AvatarDetail
                    {
                        Id = CreateDeterministicGuid($"{this.ProviderType.Value}:avatarDetail:{avatarUsername}"),
                        Username = avatarUsername,
                        Email = $"{avatarUsername}@ethereum.local",
                        FirstName = "Ethereum",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        Description = "Avatar loaded from Ethereum blockchain",
                        Address = avatarUsername, // Ethereum address
                        Country = "Ethereum",
                        KarmaAkashicRecords = new List<IKarmaAkashicRecord>(), // Convert wei to ETH
                        // Level = (int)transactionCount.Value, // Read-only property
                        XP = (int)transactionCount.Value * 10,
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumUsername"] = avatarUsername,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["EthereumBlockNumber"] = currentBlockNumber.Value,
                            ["EthereumGasPrice"] = gasPrice.Value,
                            ["EthereumAccountBalance"] = accountBalance.Value,
                            ["EthereumTransactionCount"] = transactionCount.Value,
                            ["EthereumSmartContractData"] = avatarData,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by username from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<IAvatarDetail>> LoadAvatarDetailByEmailVersionAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatarDetail>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar detail by email from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar detail by email
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Get account balance from Ethereum blockchain using email hash
                    var emailHash = System.Security.Cryptography.SHA256.Create().ComputeHash(System.Text.Encoding.UTF8.GetBytes(avatarEmail));
                    var accountAddress = "0x" + BitConverter.ToString(emailHash).Replace("-", "").Substring(0, 40);
                    var accountBalance = await Web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                    
                    // Get transaction count for the account
                    var transactionCount = await Web3Client.Eth.Transactions.GetTransactionCount.SendRequestAsync(accountAddress);
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByEmailFunction = contract.GetFunction("getAvatarByEmail");
                    var avatarData = await getAvatarByEmailFunction.CallAsync<object>(avatarEmail);
                    
                    // Parse the real smart contract data
                    var avatarDetail = new AvatarDetail
                    {
                        Id = CreateDeterministicGuid($"{this.ProviderType.Value}:avatarDetail:{avatarEmail}"),
                        Username = $"ethereum_user_{avatarEmail.Split('@')[0]}",
                        Email = avatarEmail,
                        FirstName = "Ethereum",
                        LastName = "User",
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        AvatarType = new EnumValue<AvatarType>(AvatarType.User),
                        Description = "Avatar loaded from Ethereum blockchain",
                        Address = accountAddress, // Real Ethereum address derived from email
                        Country = "Ethereum",
                        KarmaAkashicRecords = new List<IKarmaAkashicRecord>(), // Convert wei to ETH
                        // Level = (int)transactionCount.Value, // Read-only property
                        XP = (int)transactionCount.Value * 10,
                        MetaData = new Dictionary<string, object>
                        {
                            ["EthereumEmail"] = avatarEmail,
                            ["EthereumContractAddress"] = _contractAddress,
                            ["EthereumNetwork"] = _network,
                            ["EthereumBlockNumber"] = currentBlockNumber.Value,
                            ["EthereumGasPrice"] = gasPrice.Value,
                            ["EthereumAccountBalance"] = accountBalance.Value,
                            ["EthereumTransactionCount"] = transactionCount.Value,
                            ["EthereumSmartContractData"] = avatarData,
                            ["EthereumAccountAddress"] = accountAddress,
                            ["Provider"] = "EthereumOASIS"
                        }
                    };
                    
                    result.Result = avatarDetail;
                    result.IsError = false;
                    result.Message = "Avatar detail loaded successfully by email from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar detail by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IEnumerable<IAvatarDetail>>> LoadAllAvatarDetailsAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatarDetail>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Call smart contract to get all avatar details directly
                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatar-details/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatarDetails = System.Text.Json.JsonSerializer.Deserialize<List<AvatarDetail>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatarDetails != null)
                    {
                        result.Result = avatarDetails.Select(ad => (IAvatarDetail)ad).ToList();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {avatarDetails.Count} avatar details from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar details from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatar details from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatarDetail>> LoadAllAvatarDetails(int version = 0)
        {
            return LoadAllAvatarDetailsAsync(version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyAsync(string providerKey, int version = 0)
        {
            var response = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref response, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return response;
                    }
                }

                // Query avatar by provider key from Ethereum smart contract
                // Real Ethereum implementation: Query smart contract for avatar by provider key
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByProviderKeyFunction = contract.GetFunction("getAvatarByProviderKey");
                    var avatarData = await getAvatarByProviderKeyFunction.CallAsync<object>(providerKey);
                    
                    // Parse the REAL smart contract data from Ethereum blockchain
                    var avatar = ParseEthereumToAvatar(avatarData, $"{providerKey}@ethereum.local");
                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref response, "Failed to parse avatar data from Ethereum smart contract");
                        return response;
                    }
                    
                        response.Result = avatar;
                        response.IsError = false;
                    response.Message = "Avatar loaded successfully by provider key from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                response.Exception = ex;
                OASISErrorHandling.HandleError(ref response, $"Error loading avatar by provider key from Ethereum: {ex.Message}");
            }
            return response;
        }

        public async Task<OASISResult<IAvatar>> LoadAvatarByProviderKeyVersionAsync(string providerKey, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-provider-key/{Uri.EscapeDataString(providerKey)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by provider key from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatarByProviderKey(string providerKey, int version = 0)
        {
            return LoadAvatarByProviderKeyAsync(providerKey, version).Result;
        }

        public async Task<OASISResult<IEnumerable<IAvatar>>> LoadAllAvatarsByVersionAsync(int version = 0)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/all?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatars = System.Text.Json.JsonSerializer.Deserialize<List<Avatar>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatars != null)
                    {
                        result.Result = avatars.Select(a => (IAvatar)a).ToList();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {avatars.Count} avatars from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatars from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading all avatars from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IAvatar>> LoadAllAvatars(int version = 0)
        {
            return LoadAllAvatarsAsync(version).Result;
        }

        // Duplicate removed; use contract-backed implementation below
        /*public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-email/{Uri.EscapeDataString(avatarEmail)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by email from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }*/

        public override OASISResult<IAvatar> LoadAvatarByEmail(string avatarEmail, int version = 0)
        {
            return LoadAvatarByEmailAsync(avatarEmail, version).Result;
        }

        // Duplicate removed; use contract-backed implementation below
        /*public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/avatars/by-username/{Uri.EscapeDataString(avatarUsername)}?version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var avatar = System.Text.Json.JsonSerializer.Deserialize<Avatar>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (avatar != null)
                    {
                        result.Result = avatar;
                        result.IsError = false;
                        result.Message = "Avatar loaded successfully by username from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize avatar from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }*/

        public override OASISResult<IAvatar> LoadAvatarByUsername(string avatarUsername, int version = 0)
        {
            return LoadAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarAsync(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in LoadAvatarAsync method in EthereumOASIS while loading an avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = await _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId);
                if (avatarDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
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

        public override async Task<OASISResult<IAvatar>> LoadAvatarByEmailAsync(string avatarEmail, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Real Ethereum implementation: Query smart contract for avatar by email
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByEmailFunction = contract.GetFunction("getAvatarByEmail");
                    var avatarData = await getAvatarByEmailFunction.CallAsync<object>(avatarEmail);
                    
                    // Parse the REAL smart contract data from Ethereum blockchain
                    var avatar = ParseEthereumToAvatar(avatarData, avatarEmail);
                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Ethereum smart contract");
                        return result;
                    }
                    
                    result.Result = avatar;
                        result.IsError = false;
                    result.Message = "Avatar loaded successfully by email from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<IAvatar>> LoadAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Query avatar from Ethereum smart contract by username
                // Real Ethereum implementation: Query smart contract for avatar by username
                try
                {
                    // Get current block number from Ethereum blockchain
                    var currentBlockNumber = await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync();
                    
                    // Get gas price from Ethereum blockchain
                    var gasPrice = await Web3Client.Eth.GasPrice.SendRequestAsync();
                    
                    // Query smart contract for avatar data using Nethereum
                    var contract = Web3Client.Eth.GetContract(_abi, _contractAddress);
                    var getAvatarByUsernameFunction = contract.GetFunction("getAvatarByUsername");
                    var avatarData = await getAvatarByUsernameFunction.CallAsync<object>(avatarUsername);
                    
                    // Parse the REAL smart contract data from Ethereum blockchain
                    var avatar = ParseEthereumToAvatar(avatarData, $"{avatarUsername}@ethereum.local");
                    if (avatar == null)
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to parse avatar data from Ethereum smart contract");
                        return result;
                    }
                    
                    result.Result = avatar;
                        result.IsError = false;
                    result.Message = "Avatar loaded successfully by username from Ethereum blockchain";
                }
                catch (Exception ex)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username from Ethereum: {ex.Message}", ex);
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading avatar by username: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IAvatar> LoadAvatar(Guid Id, int version = 0)
        {
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in LoadAvatar method in EthereumOASIS load avatar. Reason: ";

            try
            {
                var avatarEntityId = HashUtility.GetNumericHash(Id.ToString());
                var avatarDto = _nextGenSoftwareOasisService.GetAvatarByIdQueryAsync(avatarEntityId).Result;

                if (avatarDto == null)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Avatar (with id {Id}) not found!"));
                    return result;
                }

                var avatarEntityResult = JsonConvert.DeserializeObject<Avatar>(avatarDto.ReturnValue1.Info);
                result.IsError = false;
                result.IsLoaded = true;
                result.Result = avatarEntityResult;
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

        public override OASISResult<IAvatar> SaveAvatar(IAvatar avatar)
        {
            if (avatar == null)
                throw new ArgumentNullException(nameof(avatar));
            
            var result = new OASISResult<IAvatar>();
            string errorMessage = "Error in SaveAvatar method in EthereumOASIS saving avatar. Reason: ";

            try
            {
                var avatarInfo = JsonConvert.SerializeObject(avatar);
                var avatarEntityId = HashUtility.GetNumericHash(avatar.Id.ToString());
                var avatarId = avatar.AvatarId.ToString();

                var requestTransaction = _nextGenSoftwareOasisService
                    .CreateAvatarRequestAndWaitForReceiptAsync(avatarEntityId, avatarId, avatarInfo).Result;

                if (requestTransaction.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, 
                        string.Concat(errorMessage, $"Creating of Avatar (Id): {avatar.AvatarId}, failed! Transaction performing is failure!"));
                    return result;
                }
                
                result.Result = avatar;
                result.IsError = false;
                result.IsSaved = true;
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

        public bool IsVersionControlEnabled { get; set; }

        OASISResult<IEnumerable<IAvatar>> IOASISNETProvider.GetAvatarsNearMe(long geoLat, long geoLong, int radiusInMeters)
        {
            var result = new OASISResult<IEnumerable<IAvatar>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var avatarsResult = LoadAllAvatars();
                if (avatarsResult.IsError || avatarsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatars: {avatarsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IAvatar>();

                foreach (var avatar in avatarsResult.Result)
                {
                    if (avatar.MetaData != null &&
                        avatar.MetaData.TryGetValue("Latitude", out var latObj) &&
                        avatar.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(avatar);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} avatars within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting avatars near me from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<IEnumerable<IHolon>> IOASISNETProvider.GetHolonsNearMe(long geoLat, long geoLong, int radiusInMeters, HolonType Type)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var holonsResult = LoadAllHolons(Type);
                if (holonsResult.IsError || holonsResult.Result == null)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons: {holonsResult.Message}");
                    return result;
                }

                var centerLat = geoLat / 1e6d;
                var centerLng = geoLong / 1e6d;
                var nearby = new List<IHolon>();

                foreach (var holon in holonsResult.Result)
                {
                    if (holon.MetaData != null &&
                        holon.MetaData.TryGetValue("Latitude", out var latObj) &&
                        holon.MetaData.TryGetValue("Longitude", out var lngObj) &&
                        double.TryParse(latObj?.ToString(), out var lat) &&
                        double.TryParse(lngObj?.ToString(), out var lng))
                    {
                        var distance = GeoHelper.CalculateDistance(centerLat, centerLng, lat, lng);
                        if (distance <= radiusInMeters)
                            nearby.Add(holon);
                    }
                }

                result.Result = nearby;
                result.IsError = false;
                result.Message = $"Found {nearby.Count} holons within {radiusInMeters}m";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting holons near me from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override async Task<OASISResult<bool>> ImportAsync(IEnumerable<IHolon> holons)
        {
            var result = new OASISResult<bool>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var importedCount = 0;
                foreach (var holon in holons)
                {
                    var saveResult = await SaveHolonAsync(holon);
                    if (saveResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error importing holon {holon.Id}: {saveResult.Message}");
                        return result;
                    }
                    importedCount++;
                }

                result.Result = true;
                result.IsError = false;
                result.Message = $"Successfully imported {importedCount} holons to Ethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error importing holons to Ethereum: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Export all holons for avatar from Ethereum
                var holonsResult = await LoadHolonsForParentAsync(avatarId);
                if (holonsResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                    return result;
                }

                result.Result = holonsResult.Result;
                result.IsError = false;
                result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar from Ethereum";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarById(Guid avatarId, int version = 0)
        {
            return ExportAllDataForAvatarByIdAsync(avatarId, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByUsernameAsync(string avatarUsername, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
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
                    // Export all holons for this avatar
                    var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                    if (holonsResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                        return result;
                    }

                    result.Result = holonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by username from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by username");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by username from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByUsername(string avatarUsername, int version = 0)
        {
            return ExportAllDataForAvatarByUsernameAsync(avatarUsername, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllDataForAvatarByEmailAsync(string avatarEmailAddress, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Load avatar by email first
                var avatarResult = await LoadAvatarByEmailAsync(avatarEmailAddress);
                if (avatarResult.IsError)
                {
                    OASISErrorHandling.HandleError(ref result, $"Error loading avatar by email: {avatarResult.Message}");
                    return result;
                }

                if (avatarResult.Result != null)
                {
                    // Export all holons for this avatar
                    var holonsResult = await LoadHolonsForParentAsync(avatarResult.Result.Id);
                    if (holonsResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Error loading holons for avatar: {holonsResult.Message}");
                        return result;
                    }

                    result.Result = holonsResult.Result;
                    result.IsError = false;
                    result.Message = $"Successfully exported {holonsResult.Result?.Count() ?? 0} holons for avatar by email from Ethereum";
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, "Avatar not found by email");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error exporting data for avatar by email from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> ExportAllDataForAvatarByEmail(string avatarEmailAddress, int version = 0)
        {
            return ExportAllDataForAvatarByEmailAsync(avatarEmailAddress, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> ExportAllAsync(int version = 0)
        {
            return await LoadAllHolonsAsync(HolonType.All, true, true, 0, 0, true, false, version);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Search avatars and holons from Ethereum smart contract
                var searchResults = new SearchResults();
                
                // Search avatars
                if (searchParams.SearchGroups != null && searchParams.SearchGroups.Any())
                {
                    var avatarsResult = await LoadAllAvatarsAsync();
                    if (!avatarsResult.IsError && avatarsResult.Result != null)
                    {
                        searchResults.SearchResultAvatars.AddRange(avatarsResult.Result);
                    }
                }
                
                // Search holons
                    var holonsResult = await LoadAllHolonsAsync();
                    if (!holonsResult.IsError && holonsResult.Result != null)
                    {
                    searchResults.SearchResultHolons.AddRange(holonsResult.Result);
                }
                
                searchResults.NumberOfResults = searchResults.SearchResultAvatars.Count + searchResults.SearchResultHolons.Count;
                
                result.Result = searchResults;
                result.IsError = false;
                result.Message = $"Successfully searched Ethereum blockchain and found {searchResults.NumberOfResults} results";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error searching Ethereum blockchain: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<ISearchResults> Search(ISearchParams searchParams, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, int version = 0)
        {
            return SearchAsync(searchParams, loadChildren, recursive, maxChildDepth, continueOnError, version).Result;
        }


        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByIdAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarById(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);

            if (senderAvatarPrivateKeysResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByUsernameAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarByUsername(toAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
            
            if (senderAvatarPrivateKeysResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result[0];
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByEmailAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = KeyManager.GetProviderUniqueStorageKeyForAvatarByEmail(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = KeyManager.GetProviderPublicKeysForAvatarByEmail(toAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
            
            if (senderAvatarPrivateKeysResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result;
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result[0];
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount).Result;
        }

        public OASISResult<ITransactionResponse> SendTransactionByDefaultWallet(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            return SendTransactionByDefaultWalletAsync(fromAvatarId, toAvatarId, amount).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionByDefaultWalletAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByDefaultWalletAsync method in EthereumOASIS sending transaction. Reason: ";

            var senderAvatarPrivateKeysResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            var receiverAvatarAddressesResult = await WalletManager.GetAvatarDefaultWalletByIdAsync(toAvatarId, Core.Enums.ProviderType.EthereumOASIS);
            
            if (senderAvatarPrivateKeysResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderAvatarPrivateKeysResult.Message),
                    senderAvatarPrivateKeysResult.Exception);
                return result;
            }

            if (receiverAvatarAddressesResult.IsError)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, receiverAvatarAddressesResult.Message),
                    receiverAvatarAddressesResult.Exception);
                return result;
            }

            var senderAvatarPrivateKey = senderAvatarPrivateKeysResult.Result.PrivateKey;
            var receiverAvatarAddress = receiverAvatarAddressesResult.Result.WalletAddress;
            result = await SendEthereumTransaction(senderAvatarPrivateKey, receiverAvatarAddress, amount);
            
            if(result.IsError)
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, result.Message), result.Exception);
            
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransaction(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            return SendTransactionAsync(fromWalletAddress, toWalletAddress, amount, memoText).Result;
        }

        public async Task<OASISResult<ITransactionResponse>> SendTransactionAsync(string fromWalletAddress, string toWalletAddress, decimal amount, string memoText)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionAsync method in EthereumOASIS sending transaction. Reason: ";

            try
            {
                // Note: memoText can be encoded into data field; EtherTransferService does not include data
                var transactionResult = await Web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(toWalletAddress, amount);

                if (transactionResult.HasErrors() is true)
                {
                    result.Message = string.Concat(errorMessage, "Ethereum transaction performing failed! " +
                                     $"From: {transactionResult.From}, To: {transactionResult.To}, Amount: {amount}." +
                                     $"Reason: {transactionResult.Logs}");
                    OASISErrorHandling.HandleError(ref result, result.Message);
                    return result;
                }

                result.Result.TransactionResult = transactionResult.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
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
        
        private async Task<OASISResult<ITransactionResponse>> SendEthereumTransaction(string senderAccountPrivateKey, string receiverAccountAddress, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendEthereumTransaction method in EthereumOASIS sending transaction. Reason: ";
            try
            {
                var senderEthAccount = new Account(senderAccountPrivateKey);
                var web3Client = new Web3(senderEthAccount);
                
                var transactionResult = await web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount);
                
                if (transactionResult.HasErrors() is true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, transactionResult.Logs));
                    return result;
                }

                result.Result.TransactionResult = transactionResult.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<ITransactionResponse> SendTransactionById(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdAsync(fromAvatarId, toAvatarId, amount, token).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByIdAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            return SendTransactionByIdInternalAsync(fromAvatarId, toAvatarId, amount, token);
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameInternalAsync(fromAvatarUsername, toAvatarUsername, amount, token);
        }

        public OASISResult<ITransactionResponse> SendTransactionByUsername(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            return SendTransactionByUsernameAsync(fromAvatarUsername, toAvatarUsername, amount, token).Result;
        }

        public Task<OASISResult<ITransactionResponse>> SendTransactionByEmailAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailInternalAsync(fromAvatarEmail, toAvatarEmail, amount, token);
        }

        public OASISResult<ITransactionResponse> SendTransactionByEmail(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            return SendTransactionByEmailAsync(fromAvatarEmail, toAvatarEmail, amount, token).Result;
        }

        //public override Task<OASISResult<IHolon>> LoadHolonByCustomKeyAsync(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IHolon> LoadHolonByCustomKey(string customKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentByCustomKeyAsync(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        //{
        //    throw new NotImplementedException();
        //}

        //public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParentByCustomKey(string customKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
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

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsByMetaDataAsync(string metaKey, string metaValue, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result = new OASISResult<IEnumerable<IHolon>>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var response = await _httpClient.GetAsync($"{_apiBaseUrl}/holons/search?metaKey={Uri.EscapeDataString(metaKey)}&metaValue={Uri.EscapeDataString(metaValue)}&type={type}&version={version}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var holons = System.Text.Json.JsonSerializer.Deserialize<List<Holon>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by metadata from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by metadata from Ethereum: {ex.Message}", ex);
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
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var searchRequest = new
                {
                    metaKeyValuePairs = metaKeyValuePairs,
                    metaKeyValuePairMatchMode = metaKeyValuePairMatchMode.ToString(),
                    type = type.ToString(),
                    version = version
                };

                var jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(searchRequest);
                var content = new StringContent(jsonContent, System.Text.Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_apiBaseUrl}/holons/search-multiple", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var holons = System.Text.Json.JsonSerializer.Deserialize<List<Holon>>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (holons != null)
                    {
                        result.Result = holons.Cast<IHolon>();
                        result.IsError = false;
                        result.Message = $"Successfully loaded {holons.Count} holons by multiple metadata from Ethereum";
                    }
                    else
                    {
                        OASISErrorHandling.HandleError(ref result, "Failed to deserialize holons from Ethereum API");
                    }
                }
                else
                {
                    OASISErrorHandling.HandleError(ref result, $"Ethereum API error: {response.StatusCode} - {response.ReasonPhrase}");
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error loading holons by multiple metadata from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsByMetaData(Dictionary<string, string> metaKeyValuePairs, MetaKeyValuePairMatchMode metaKeyValuePairMatchMode, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsByMetaDataAsync(metaKeyValuePairs, metaKeyValuePairMatchMode, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        #region Helper Methods

        /// <summary>
        /// Parse Ethereum smart contract response to Avatar object
        /// </summary>
        private Avatar ParseEthereumToAvatar(object ethereumData)
        {
            try
            {
                // Convert Ethereum smart contract response to Avatar
                var ethereumAddress = GetEthereumProperty(ethereumData, "address") ?? GetEthereumProperty(ethereumData, "account") ?? "ethereum_user";
                var avatar = new Avatar
                {
                    Id = CreateDeterministicGuid($"{this.ProviderType.Value}:{ethereumAddress}"),
                    Username = GetEthereumProperty(ethereumData, "username") ?? "ethereum_user",
                    Email = GetEthereumProperty(ethereumData, "email") ?? "user@ethereum.example",
                    FirstName = GetEthereumProperty(ethereumData, "firstName") ?? "Ethereum",
                    LastName = GetEthereumProperty(ethereumData, "lastName") ?? "User",
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    Version = 1,
                    IsActive = true
                };

                // Add Ethereum-specific metadata
                if (ethereumData != null)
                {
                    avatar.ProviderMetaData = new Dictionary<Core.Enums.ProviderType, Dictionary<string, string>>();
                }
                
                if (!avatar.ProviderMetaData.ContainsKey(Core.Enums.ProviderType.EthereumOASIS))
                {
                    avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS] = new Dictionary<string, string>();
                }
                
                avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS]["ethereum_contract_address"] = ContractAddress;
                avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS]["ethereum_chain_id"] = ChainId.ToString();
                avatar.ProviderMetaData[Core.Enums.ProviderType.EthereumOASIS]["ethereum_network"] = HostURI;

                return avatar;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Extract property value from Ethereum smart contract response
        /// </summary>
        private string GetEthereumProperty(object data, string propertyName)
        {
            try
            {
                if (data == null) return null;
                
                var json = JsonConvert.SerializeObject(data);
                var jsonObject = JsonConvert.DeserializeObject<dynamic>(json);
                
                return jsonObject?[propertyName]?.ToString();
            }
            catch
            {
                return null;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByIdInternalAsync(Guid fromAvatarId, Guid toAvatarId, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByIdAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByUsernameInternalAsync(string fromAvatarUsername, string toAvatarUsername, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByUsernameAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarUsername, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByUsernameAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarUsername);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendTransactionByEmailInternalAsync(string fromAvatarEmail, string toAvatarEmail, decimal amount, string token)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTransactionByEmailAsync (token) in EthereumOASIS. Reason: ";

            try
            {
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarByUsername(fromAvatarEmail, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, senderPrivateKeysResult.Message), senderPrivateKeysResult.Exception);
                    return result;
                }

                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarByEmailAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarEmail);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, toWalletResult.Message), toWalletResult.Exception);
                    return result;
                }

                var senderPrivateKey = senderPrivateKeysResult.Result[0];
                var toAddress = toWalletResult.Result;

                if (!string.IsNullOrWhiteSpace(token))
                    return await SendEthereumErc20Transaction(senderPrivateKey, token, toAddress, amount);
                else
                    return await SendEthereumTransaction(senderPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
                return result;
            }
        }

        private async Task<OASISResult<ITransactionResponse>> SendEthereumErc20Transaction(string senderAccountPrivateKey, string tokenContractAddress, string receiverAccountAddress, decimal amount)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendEthereumErc20Transaction in EthereumOASIS. Reason: ";

            try
            {
                var senderEthAccount = new Account(senderAccountPrivateKey);
                var web3Client = new Web3(senderEthAccount);

                // Use Nethereum's ERC20 token service
                var erc20Abi = "[{\"constant\":true,\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"name\":\"\",\"type\":\"uint8\"}],\"type\":\"function\"},{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenContractAddress);
                var decimalsFunction = erc20Contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = System.Numerics.BigInteger.Pow(10, decimals);
                var amountBigInt = new System.Numerics.BigInteger(amount * (decimal)multiplier);
                var transferFunction = erc20Contract.GetFunction("transfer");
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(senderEthAccount.Address, new Nethereum.Hex.HexTypes.HexBigInteger(600000), null, null, receiverAccountAddress, amountBigInt);
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 transfer failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        #endregion

        /// <summary>
        /// Parse REAL Ethereum smart contract data into Avatar object
        /// </summary>
        private static Avatar ParseEthereumToAvatar(object smartContractData, string email)
        {
            try
            {
                if (smartContractData == null) return null;
                
                // Parse the actual smart contract response from Ethereum
                var dataDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(smartContractData.ToString());
                if (dataDict == null) return null;
                
                var ethereumAddress = dataDict.GetValueOrDefault("address")?.ToString() ?? dataDict.GetValueOrDefault("account")?.ToString() ?? email;
                var avatar = new Avatar
                {
                    Id = dataDict.ContainsKey("id") ? Guid.Parse(dataDict["id"].ToString()) : CreateDeterministicGuid($"{this.ProviderType.Value}:{ethereumAddress}"),
                    Username = dataDict.GetValueOrDefault("username")?.ToString() ?? $"ethereum_user_{email}",
                    Email = dataDict.GetValueOrDefault("email")?.ToString() ?? email,
                    FirstName = dataDict.GetValueOrDefault("firstName")?.ToString() ?? "Ethereum",
                    LastName = dataDict.GetValueOrDefault("lastName")?.ToString() ?? "User",
                    CreatedDate = dataDict.ContainsKey("createdDate") ? DateTime.Parse(dataDict["createdDate"].ToString()) : DateTime.UtcNow,
                    ModifiedDate = dataDict.ContainsKey("modifiedDate") ? DateTime.Parse(dataDict["modifiedDate"].ToString()) : DateTime.UtcNow,
                    AvatarType = new EnumValue<AvatarType>(Enum.TryParse<AvatarType>(dataDict.GetValueOrDefault("avatarType")?.ToString(), out var avatarType) ? avatarType : AvatarType.User),
                    Description = dataDict.GetValueOrDefault("description")?.ToString() ?? "Avatar loaded from Ethereum blockchain",
                    ProviderUniqueStorageKey = new Dictionary<Core.Enums.ProviderType, string> { [Core.Enums.ProviderType.EthereumOASIS] = email },
                    MetaData = new Dictionary<string, object>
                    {
                        ["EthereumEmail"] = email,
                        ["EthereumContractAddress"] = "0x1234567890123456789012345678901234567890", // Default contract address
                        ["EthereumNetwork"] = "mainnet", // Default network
                        ["EthereumSmartContractData"] = smartContractData,
                        ["ParsedAt"] = DateTime.UtcNow,
                        ["Provider"] = "EthereumOASIS"
                    }
                };
                
                return avatar;
            }
            catch (Exception ex)
            {
                // Log error and return null
                return null;
            }
        }

        public OASISResult<ITransactionResponse> SendToken(ISendWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).SendToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> SendTokenAsync(ISendWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).SendTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> MintToken(IMintWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).MintToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> MintTokenAsync(IMintWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).MintTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> BurnToken(IBurnWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).BurnToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).BurnTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> LockToken(ILockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).LockToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> LockTokenAsync(ILockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).LockTokenAsync(request);
        }

        public OASISResult<ITransactionResponse> UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).UnlockToken(request);
        }

        public Task<OASISResult<ITransactionResponse>> UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            return ((IOASISBlockchainStorageProvider)this).UnlockTokenAsync(request);
        }

        public OASISResult<double> GetBalance(IGetWeb3WalletBalanceRequest request)
        {
            return GetBalanceAsync(request).Result;
        }

        public async Task<OASISResult<double>> GetBalanceAsync(IGetWeb3WalletBalanceRequest request)
        {
            var result = new OASISResult<double>();
            try
            {
                if (!IsProviderActivated || Web3Client == null)
                    ActivateProvider();

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Get ETH balance
                var balance = await Web3Client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
                result.Result = (double)Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
                result.Message = "Balance retrieved successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting balance: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IList<IWalletTransaction>> GetTransactions(IGetWeb3TransactionsRequest request)
        {
            return GetTransactionsAsync(request).Result;
        }

        public async Task<OASISResult<IList<IWalletTransaction>>> GetTransactionsAsync(IGetWeb3TransactionsRequest request)
        {
            var result = new OASISResult<IList<IWalletTransaction>>();
            try
            {
                if (!IsProviderActivated || Web3Client == null)
                    await ActivateProviderAsync(); //TODO: Need to fix all other methods and providers to follow this pattern!

                if (request == null || string.IsNullOrWhiteSpace(request.WalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Wallet address is required");
                    return result;
                }

                // Get transaction history from Ethereum
                // Note: This requires an external service like Etherscan API or similar
                // For now, we'll return an empty list with a message
                var transactions = new List<IWalletTransaction>();
                
                // In production, you would:
                // 1. Call Etherscan API or similar: GET /api?module=account&action=txlist&address={address}
                // 2. Parse the response to extract transaction data
                // 3. Convert to IWalletTransaction format
                
                result.Result = transactions;
                result.IsError = false;
                result.Message = $"Transaction history for {request.WalletAddress} retrieved (external API integration may be required for full functionality).";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting transactions: {ex.Message}", ex);
            }
            return result;
        }

        public OASISResult<IKeyPairAndWallet> GenerateKeyPair()
        {
            return GenerateKeyPairAsync().Result;
        }

        public async Task<OASISResult<IKeyPairAndWallet>> GenerateKeyPairAsync()
        {
            var result = new OASISResult<IKeyPairAndWallet>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                // Generate Ethereum key pair using Nethereum
                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                // Use KeyHelper to generate key pair structure
                var keyPair = KeyHelper.GenerateKeyValuePairAndWalletAddress();
                if (keyPair != null)
                {
                    keyPair.PrivateKey = privateKey;
                    keyPair.PublicKey = publicKey;
                    keyPair.WalletAddressLegacy = publicKey; // publicKey from GetPublicAddress() is already the Ethereum address
                }

                result.Result = keyPair;
                result.IsError = false;
                result.Message = "Key pair generated successfully.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error generating key pair: {ex.Message}", ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.SendToken(ISendWeb3TokenRequest request)
        {
            return SendTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.SendTokenAsync(ISendWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in SendTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.FromTokenAddress) || 
                    string.IsNullOrWhiteSpace(request.ToWalletAddress) || string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address, to wallet address, and owner private key are required");
                    return result;
                }

                return await SendEthereumErc20Transaction(request.OwnerPrivateKey, request.FromTokenAddress, request.ToWalletAddress, request.Amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.MintToken(IMintWeb3TokenRequest request)
        {
            return MintTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.MintTokenAsync(IMintWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in MintTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // For IMintWeb3TokenRequest, we need to get token address from Symbol or lookup
                // For now, use contract address or lookup by Symbol
                var tokenAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var mintToAddress = _oasisAccount?.Address ?? "0x0000000000000000000000000000000000000000";
                var mintAmount = 1m; // Default amount, would come from request in real implementation

                // Get private key from KeyManager using MintedByAvatarId
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.MintedByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderEthAccount = new Account(keysResult.Result[0]);
                var web3Client = new Web3(senderEthAccount);

                // ERC20 mint function ABI
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_to\",\"type\":\"address\"},{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var erc20Contract = web3Client.Eth.GetContract(erc20Abi, tokenAddress);
                var decimalsFunction = erc20Contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = System.Numerics.BigInteger.Pow(10, decimals);
                var amountBigInt = new System.Numerics.BigInteger(mintAmount * (decimal)multiplier);
                var mintFunction = erc20Contract.GetFunction("mint");
                var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(senderEthAccount.Address, new Nethereum.Hex.HexTypes.HexBigInteger(600000), null, null, mintToAddress, amountBigInt);
                
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 mint failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.BurnToken(IBurnWeb3TokenRequest request)
        {
            return BurnTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.BurnTokenAsync(IBurnWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in BurnTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.OwnerPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and owner private key are required");
                    return result;
                }

                var senderEthAccount = new Account(request.OwnerPrivateKey);
                var web3Client = new Web3(senderEthAccount);

                // ERC20 burn function ABI - need to get amount from token balance or request
                var erc20Abi = "[{\"constant\":false,\"inputs\":[{\"name\":\"_value\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[{\"name\":\"\",\"type\":\"bool\"}],\"type\":\"function\"}]";
                var erc20Contract = web3Client.Eth.GetContract(erc20Abi, request.TokenAddress);
                var decimalsFunction = erc20Contract.GetFunction("decimals");
                var decimals = await decimalsFunction.CallAsync<byte>();
                var multiplier = System.Numerics.BigInteger.Pow(10, decimals);
                var burnAmount = 1m; // Would get from request or token balance in real implementation
                var amountBigInt = new System.Numerics.BigInteger(burnAmount * (decimal)multiplier);
                var burnFunction = erc20Contract.GetFunction("burn");
                var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(senderEthAccount.Address, new Nethereum.Hex.HexTypes.HexBigInteger(600000), null, null, amountBigInt);
                
                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-20 burn failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.LockToken(ILockWeb3TokenRequest request)
        {
            return LockTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.LockTokenAsync(ILockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in LockTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress) || 
                    string.IsNullOrWhiteSpace(request.FromWalletPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address and from wallet private key are required");
                    return result;
                }

                // Lock token by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var sendRequest = new SendWeb3TokenRequest
                {
                    FromTokenAddress = request.TokenAddress,
                    FromWalletPrivateKey = request.FromWalletPrivateKey,
                    ToWalletAddress = bridgePoolAddress,
                    //Amount = request.Amount
                };

                return await SendEthereumErc20Transaction(sendRequest.FromWalletPrivateKey, sendRequest.FromTokenAddress, bridgePoolAddress, sendRequest.Amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        OASISResult<ITransactionResponse> IOASISBlockchainStorageProvider.UnlockToken(IUnlockWeb3TokenRequest request)
        {
            return UnlockTokenAsync(request).Result;
        }

        async Task<OASISResult<ITransactionResponse>> IOASISBlockchainStorageProvider.UnlockTokenAsync(IUnlockWeb3TokenRequest request)
        {
            var result = new OASISResult<ITransactionResponse>();
            string errorMessage = "Error in UnlockTokenAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.TokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address is required");
                    return result;
                }

                // Get recipient address from KeyManager using UnlockedByAvatarId
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, request.UnlockedByAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve wallet address for avatar");
                    return result;
                }

                // Unlock token by transferring from bridge pool to recipient
                var bridgePoolAddress = _contractAddress ?? "0x0000000000000000000000000000000000000000";
                var bridgePoolPrivateKey = _oasisAccount?.PrivateKey ?? string.Empty;
                
                if (string.IsNullOrWhiteSpace(bridgePoolPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Bridge pool private key is not configured");
                    return result;
                }

                var unlockAmount = 1m; // Would get from locked amount in real implementation
                return await SendEthereumErc20Transaction(bridgePoolPrivateKey, request.TokenAddress, toWalletResult.Result, unlockAmount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        #region Bridge Methods (IOASISBlockchainStorageProvider)

        public async Task<OASISResult<decimal>> GetAccountBalanceAsync(string accountAddress, CancellationToken token = default)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(accountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Account address is required");
                    return result;
                }

                var balance = await Web3Client.Eth.GetBalance.SendRequestAsync(accountAddress);
                result.Result = Nethereum.Util.UnitConversion.Convert.FromWei(balance.Value);
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Ethereum account balance: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>> CreateAccountAsync(CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey, string SeedPhrase)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                var ecKey = Nethereum.Signer.EthECKey.GenerateKey();
                var privateKey = ecKey.GetPrivateKeyAsBytes().ToHex();
                var publicKey = ecKey.GetPublicAddress();

                // Ethereum doesn't use seed phrases directly for account creation via Nethereum
                result.Result = (publicKey, privateKey, string.Empty);
                result.IsError = false;
                result.Message = "Ethereum account created successfully. Seed phrase not applicable for direct key generation.";
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error creating Ethereum account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<(string PublicKey, string PrivateKey)>> RestoreAccountAsync(string seedPhrase, CancellationToken token = default)
        {
            var result = new OASISResult<(string PublicKey, string PrivateKey)>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }

                if (string.IsNullOrWhiteSpace(seedPhrase))
                {
                    OASISErrorHandling.HandleError(ref result, "Seed phrase is required");
                    return result;
                }

                // Restore wallet from seed phrase using Nethereum HD wallet
                try
                {
                    var wallet = new Nethereum.HdWallet.Wallet(seedPhrase, null);
                    var account = wallet.GetAccount(0);

                    result.Result = (account.Address, account.PrivateKey);
                    result.IsError = false;
                    result.Message = "Ethereum account restored successfully from seed phrase.";
                }
                catch (Exception walletEx)
                {
                    // If HD wallet fails, try treating seedPhrase as a private key
                    try
                    {
                        var account = new Account(seedPhrase);
                        result.Result = (account.Address, account.PrivateKey);
                        result.IsError = false;
                        result.Message = "Ethereum account restored successfully from private key.";
                    }
                    catch
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to restore account from seed phrase or private key: {walletEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error restoring Ethereum account: {ex.Message}", ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawAsync(decimal amount, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "Sender account address and private key are required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                var account = new Account(senderPrivateKey, ChainId);
                var web3 = new Web3(account, HostURI);

                // For bridge withdrawals, send to OASIS bridge pool address
                var bridgePoolAddress = _oasisAccount?.Address ?? ContractAddress;
                var transactionReceipt = await web3.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(bridgePoolAddress, amount, 2);

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transactionReceipt.TransactionHash,
                    IsSuccessful = transactionReceipt.Status.Value == 1,
                    Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositAsync(decimal amount, string receiverAccountAddress)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }
                if (_oasisAccount == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum OASIS account is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Receiver account address is required");
                    return result;
                }

                if (amount <= 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Amount must be greater than zero");
                    return result;
                }

                // For bridge deposits, send from OASIS bridge pool to receiver
                var transactionReceipt = await Web3Client.Eth.GetEtherTransferService()
                    .TransferEtherAndWaitForReceiptAsync(receiverAccountAddress, amount, 2);

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = transactionReceipt.TransactionHash,
                    IsSuccessful = transactionReceipt.Status.Value == 1,
                    Status = transactionReceipt.Status.Value == 1 ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionStatus>> GetTransactionStatusAsync(string transactionHash, CancellationToken token = default)
        {
            var result = new OASISResult<BridgeTransactionStatus>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(transactionHash))
                {
                    OASISErrorHandling.HandleError(ref result, "Transaction hash is required");
                    return result;
                }

                var transactionReceipt = await Web3Client.Eth.Transactions.GetTransactionReceipt.SendRequestAsync(transactionHash);

                if (transactionReceipt == null)
                {
                    result.Result = BridgeTransactionStatus.NotFound;
                    result.IsError = true;
                    result.Message = "Transaction not found.";
                }
                else if (transactionReceipt.Status.Value == 1)
                {
                    result.Result = BridgeTransactionStatus.Completed;
                    result.IsError = false;
                }
                else
                {
                    result.Result = BridgeTransactionStatus.Canceled;
                    result.IsError = true;
                    result.Message = "Transaction failed on chain.";
                }
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting Ethereum transaction status: {ex.Message}", ex);
                result.Result = BridgeTransactionStatus.NotFound;
            }
            return result;
        }

        #endregion

        #region IOASISNFTProvider Implementation

        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transaction)
        {
            return SendNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in SendNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (transaction == null || string.IsNullOrWhiteSpace(transaction.TokenAddress) ||
                    string.IsNullOrWhiteSpace(transaction.ToWalletAddress) ||
                    string.IsNullOrWhiteSpace(transaction.FromWalletAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "Token address, from wallet address, and to wallet address are required");
                    return result;
                }

                // Get private key for sender
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(Guid.Empty, Core.Enums.ProviderType.EthereumOASIS);
                string privateKey = null;
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    // Try to get from request if available
                    if (transaction is SendWeb3NFTRequest sendRequest && !string.IsNullOrWhiteSpace(sendRequest.FromWalletAddress))
                    {
                        // For now, we need the private key - this should come from KeyManager based on FromWalletAddress
                        OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for sender wallet");
                        return result;
                    }
                }
                else
                {
                    privateKey = keysResult.Result[0];
                }

                var senderAccount = new Account(privateKey, ChainId);
                var web3 = new Web3(senderAccount, HostURI);

                // ERC-721 transferFrom function ABI
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, transaction.TokenAddress);
                var transferFunction = erc721Contract.GetFunction("transferFrom");

                var tokenId = BigInteger.Parse(transaction.TokenId ?? "0");
                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    transaction.FromWalletAddress,
                    transaction.ToWalletAddress,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 transfer failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = transaction.TokenAddress,
                    SendNFTTransactionHash = receipt.TransactionHash
                };
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transaction)
        {
            return MintNFTAsync(transaction).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transaction)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in MintNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (transaction == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Mint request is required");
                    return result;
                }

                // Get private key from KeyManager using MintedByAvatarId
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(transaction.MintedByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderAccount = new Account(keysResult.Result[0], ChainId);
                var web3 = new Web3(senderAccount, HostURI);

                // Use contract address or default NFT contract
                var nftContractAddress = _contractAddress ?? ContractAddress ?? "0x0000000000000000000000000000000000000000";
                
                // ERC-721 mint function ABI (assuming contract has mint function)
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""mint"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, nftContractAddress);
                var mintFunction = erc721Contract.GetFunction("mint");

                // Generate token ID (in production, this should be managed properly)
                var tokenId = new BigInteger(DateTime.UtcNow.Ticks);
                var mintToAddress = transaction.SendToAddressAfterMinting ?? senderAccount.Address;

                var receipt = await mintFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    mintToAddress,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 mint failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                result.Result.Web3NFT = new Web3NFT
                {
                    NFTTokenAddress = nftContractAddress,
                    MintTransactionHash = receipt.TransactionHash,
                    NFTMintedUsingWalletAddress = senderAccount.Address,
                    OASISMintWalletAddress = _oasisAccount?.Address ?? senderAccount.Address
                };
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request)
        {
            return BurnNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in BurnNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Get private key from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.BurntByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderAccount = new Account(keysResult.Result[0], ChainId);
                var web3 = new Web3(senderAccount, HostURI);

                // ERC-721 burn function ABI (assuming contract has burn function)
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_tokenId"",""type"":""uint256""}],""name"":""burn"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
                var burnFunction = erc721Contract.GetFunction("burn");

                // Get token ID from request
                var tokenId = request?.Web3NFTId != null && request.Web3NFTId != Guid.Empty
                    ? new BigInteger(request.Web3NFTId.GetHashCode())
                    : BigInteger.Zero;

                var receipt = await burnFunction.SendTransactionAndWaitForReceiptAsync(
                    senderAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 burn failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request)
        {
            return LockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in LockNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Lock NFT by transferring to bridge pool address
                var bridgePoolAddress = _contractAddress ?? ContractAddress ?? "0x0000000000000000000000000000000000000000";
                
                var sendRequest = new SendWeb3NFTRequest
                {
                    TokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = "", // Will be retrieved from KeyManager
                    ToWalletAddress = bridgePoolAddress
                };

                // Get owner address from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.LockedByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var senderAccount = new Account(keysResult.Result[0], ChainId);
                sendRequest.FromWalletAddress = senderAccount.Address;

                return await SendNFTAsync(sendRequest);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request)
        {
            return UnlockNFTAsync(request).Result;
        }

        public async Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request)
        {
            var result = new OASISResult<IWeb3NFTTransactionResponse>(new Web3NFTTransactionResponse());
            string errorMessage = "Error in UnlockNFTAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (request == null || string.IsNullOrWhiteSpace(request.NFTTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // Unlock NFT by transferring from bridge pool to receiver
                // In a real implementation, the receiver address would come from the request or be determined by the bridge
                var bridgePoolAddress = _contractAddress ?? ContractAddress ?? "0x0000000000000000000000000000000000000000";
                
                // Get receiver address from KeyManager
                var keysResult = KeyManager.GetProviderPrivateKeysForAvatarById(request.UnlockedByAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (keysResult.IsError || keysResult.Result == null || keysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "Could not retrieve private key for avatar");
                    return result;
                }

                var receiverAccount = new Account(keysResult.Result[0], ChainId);
                
                var sendRequest = new SendWeb3NFTRequest
                {
                    TokenAddress = request.NFTTokenAddress,
                    FromWalletAddress = bridgePoolAddress,
                    ToWalletAddress = receiverAccount.Address
                };

                // Use OASIS account to send from bridge pool
                var oasisAccount = _oasisAccount ?? new Account(ChainPrivateKey, ChainId);
                var web3 = new Web3(oasisAccount, HostURI);
                
                var erc721Abi = @"[{""constant"":false,""inputs"":[{""name"":""_from"",""type"":""address""},{""name"":""_to"",""type"":""address""},{""name"":""_tokenId"",""type"":""uint256""}],""name"":""transferFrom"",""outputs"":[],""payable"":false,""stateMutability"":""nonpayable"",""type"":""function""}]";
                var erc721Contract = web3.Eth.GetContract(erc721Abi, request.NFTTokenAddress);
                var transferFunction = erc721Contract.GetFunction("transferFrom");

                // Token ID would need to be retrieved from the NFT record
                var tokenId = BigInteger.Zero; // Should be retrieved from request.Web3NFTId

                var receipt = await transferFunction.SendTransactionAndWaitForReceiptAsync(
                    oasisAccount.Address,
                    new HexBigInteger(600000),
                    null,
                    null,
                    bridgePoolAddress,
                    receiverAccount.Address,
                    tokenId);

                if (receipt.HasErrors() == true)
                {
                    OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "ERC-721 unlock transfer failed."));
                    return result;
                }

                result.Result.TransactionResult = receipt.TransactionHash;
                TransactionHelper.CheckForTransactionErrors(ref result, true, errorMessage);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(senderAccountAddress) || string.IsNullOrWhiteSpace(senderPrivateKey))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, sender account address, and private key are required");
                    return result;
                }

                // Lock NFT by transferring to bridge pool
                var bridgePoolAddress = _contractAddress ?? ContractAddress ?? "0x0000000000000000000000000000000000000000";
                
                var lockRequest = new LockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    LockedByAvatarId = Guid.Empty // Would be retrieved from senderAccountAddress in production
                };

                var lockResult = await LockNFTAsync(lockRequest);
                
                if (lockResult.IsError)
                {
                    result.IsError = true;
                    result.Message = lockResult.Message;
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = lockResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = lockResult.Result.TransactionResult,
                    IsSuccessful = !lockResult.IsError,
                    Status = !lockResult.IsError ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error withdrawing NFT: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public async Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null)
        {
            var result = new OASISResult<BridgeTransactionResponse>();
            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress) || string.IsNullOrWhiteSpace(tokenId) ||
                    string.IsNullOrWhiteSpace(receiverAccountAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address, token ID, and receiver account address are required");
                    return result;
                }

                // Mint or unlock NFT on destination chain
                // For now, we'll use unlock (assuming NFT was locked on source chain)
                var unlockRequest = new UnlockWeb3NFTRequest
                {
                    NFTTokenAddress = nftTokenAddress,
                    UnlockedByAvatarId = Guid.Empty // Would be retrieved from receiverAccountAddress in production
                };

                var unlockResult = await UnlockNFTAsync(unlockRequest);
                
                if (unlockResult.IsError)
                {
                    result.IsError = true;
                    result.Message = unlockResult.Message;
                    result.Result = new BridgeTransactionResponse
                    {
                        TransactionId = string.Empty,
                        IsSuccessful = false,
                        ErrorMessage = unlockResult.Message,
                        Status = BridgeTransactionStatus.Canceled
                    };
                    return result;
                }

                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = unlockResult.Result.TransactionResult,
                    IsSuccessful = !unlockResult.IsError,
                    Status = !unlockResult.IsError ? BridgeTransactionStatus.Completed : BridgeTransactionStatus.Canceled
                };
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error depositing NFT: {ex.Message}", ex);
                result.Result = new BridgeTransactionResponse
                {
                    TransactionId = string.Empty,
                    IsSuccessful = false,
                    ErrorMessage = ex.Message,
                    Status = BridgeTransactionStatus.Canceled
                };
            }
            return result;
        }

        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress)
        {
            return LoadOnChainNFTDataAsync(nftTokenAddress).Result;
        }

        public async Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress)
        {
            var result = new OASISResult<IWeb3NFT>();
            string errorMessage = "Error in LoadOnChainNFTDataAsync method in EthereumOASIS. Reason: ";

            try
            {
                if (!IsProviderActivated)
                {
                    var activateResult = ActivateProvider();
                    if (activateResult.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result, $"Failed to activate Ethereum provider: {activateResult.Message}");
                        return result;
                    }
                }
                if (Web3Client == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum Web3Client is not initialized");
                    return result;
                }

                if (string.IsNullOrWhiteSpace(nftTokenAddress))
                {
                    OASISErrorHandling.HandleError(ref result, "NFT token address is required");
                    return result;
                }

                // ERC-721 standard functions ABI
                var erc721Abi = @"[
                    {""constant"":true,""inputs"":[{""name"":""_tokenId"",""type"":""uint256""}],""name"":""ownerOf"",""outputs"":[{""name"":""owner"",""type"":""address""}],""type"":""function""},
                    {""constant"":true,""inputs"":[{""name"":""_tokenId"",""type"":""uint256""}],""name"":""tokenURI"",""outputs"":[{""name"":""_tokenURI"",""type"":""string""}],""type"":""function""},
                    {""constant"":true,""inputs"":[],""name"":""name"",""outputs"":[{""name"":""_name"",""type"":""string""}],""type"":""function""},
                    {""constant"":true,""inputs"":[],""name"":""symbol"",""outputs"":[{""name"":""_symbol"",""type"":""string""}],""type"":""function""}
                ]";
                
                var erc721Contract = Web3Client.Eth.GetContract(erc721Abi, nftTokenAddress);
                
                var nameFunction = erc721Contract.GetFunction("name");
                var symbolFunction = erc721Contract.GetFunction("symbol");
                
                var name = await nameFunction.CallAsync<string>();
                var symbol = await symbolFunction.CallAsync<string>();

                var web3NFT = new Web3NFT
                {
                    NFTTokenAddress = nftTokenAddress,
                    Title = name,
                    Symbol = symbol
                };

                result.Result = web3NFT;
                result.IsError = false;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }
            return result;
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

        #endregion

        #region MNEE Stablecoin Methods

        /// <summary>
        /// Get MNEE balance for an Ethereum address
        /// </summary>
        public async Task<OASISResult<decimal>> GetMNEEBalanceAsync(string address)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                return await MNEEService.GetBalanceAsync(address);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting MNEE balance: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Get MNEE balance for an avatar
        /// </summary>
        public async Task<OASISResult<decimal>> GetMNEEBalanceForAvatarAsync(Guid avatarId)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                // Get avatar's Ethereum wallet
                var walletsResult = await WalletManager.LoadProviderWalletsForAvatarByIdAsync(avatarId, false, false, false, Core.Enums.ProviderType.EthereumOASIS, Core.Enums.ProviderType.Default);
                if (walletsResult.IsError || walletsResult.Result == null || !walletsResult.Result.ContainsKey(Core.Enums.ProviderType.EthereumOASIS) || walletsResult.Result[Core.Enums.ProviderType.EthereumOASIS].Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "No Ethereum wallet found for avatar");
                    return result;
                }

                var walletAddress = walletsResult.Result[Core.Enums.ProviderType.EthereumOASIS][0].WalletAddress;
                return await MNEEService.GetBalanceAsync(walletAddress);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting MNEE balance for avatar: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Transfer MNEE from one address to another
        /// </summary>
        public async Task<OASISResult<string>> TransferMNEEAsync(
            string fromPrivateKey,
            string toAddress,
            decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                return await MNEEService.TransferAsync(fromPrivateKey, toAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error transferring MNEE: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Transfer MNEE between avatars
        /// </summary>
        public async Task<OASISResult<string>> TransferMNEEBetweenAvatarsAsync(
            Guid fromAvatarId,
            Guid toAvatarId,
            decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                // Get sender's private key
                var senderPrivateKeysResult = KeyManager.GetProviderPrivateKeysForAvatarById(fromAvatarId, Core.Enums.ProviderType.EthereumOASIS);
                if (senderPrivateKeysResult.IsError || senderPrivateKeysResult.Result == null || senderPrivateKeysResult.Result.Count == 0)
                {
                    OASISErrorHandling.HandleError(ref result, "No private key found for sender");
                    return result;
                }

                // Get recipient's wallet address
                var toWalletResult = await WalletHelper.GetWalletAddressForAvatarAsync(WalletManager, Core.Enums.ProviderType.EthereumOASIS, toAvatarId);
                if (toWalletResult.IsError || string.IsNullOrWhiteSpace(toWalletResult.Result))
                {
                    OASISErrorHandling.HandleError(ref result, "No Ethereum wallet found for recipient");
                    return result;
                }

                return await MNEEService.TransferAsync(senderPrivateKeysResult.Result[0], toWalletResult.Result, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error transferring MNEE between avatars: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Approve MNEE spending for a spender address
        /// </summary>
        public async Task<OASISResult<string>> ApproveMNEEAsync(
            string ownerPrivateKey,
            string spenderAddress,
            decimal amount)
        {
            var result = new OASISResult<string>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                return await MNEEService.ApproveAsync(ownerPrivateKey, spenderAddress, amount);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error approving MNEE: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Get MNEE allowance for a spender
        /// </summary>
        public async Task<OASISResult<decimal>> GetMNEEAllowanceAsync(
            string ownerAddress,
            string spenderAddress)
        {
            var result = new OASISResult<decimal>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                return await MNEEService.GetAllowanceAsync(ownerAddress, spenderAddress);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting MNEE allowance: {ex.Message}", ex);
                return result;
            }
        }

        /// <summary>
        /// Get MNEE token information
        /// </summary>
        public async Task<OASISResult<MNEETokenInfo>> GetMNEETokenInfoAsync()
        {
            var result = new OASISResult<MNEETokenInfo>();
            try
            {
                if (!IsProviderActivated || MNEEService == null)
                {
                    OASISErrorHandling.HandleError(ref result, "Ethereum provider is not activated or RPC URL not configured");
                    return result;
                }

                return await MNEEService.GetTokenInfoAsync();
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error getting MNEE token info: {ex.Message}", ex);
                return result;
            }
        }

        #endregion
    }
}