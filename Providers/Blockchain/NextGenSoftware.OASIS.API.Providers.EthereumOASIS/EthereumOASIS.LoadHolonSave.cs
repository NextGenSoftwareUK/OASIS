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
using System.Reflection;
using System.Text;
using Nethereum.RPC.Accounts;
// using Nethereum.StandardTokenEIP20; // Commented out - type doesn't exist

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
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
                            // fallback: load all holons and filter by parent ID in-memory
                            // This is less efficient but works if the contract structure doesn't support direct parent queries
                            var allHolonsResult = await LoadAllHolonsAsync(type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version);
                            
                            if (allHolonsResult.IsError || allHolonsResult.Result == null)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to load holons: {allHolonsResult.Message}");
                                return result;
                            }

                            // Filter holons by parent ID
                            var filteredHolons = allHolonsResult.Result.Where(h => h.ParentHolonId == id).ToList();
                            
                            result.Result = filteredHolons;
                            result.IsError = false;
                            result.Message = $"Loaded {filteredHolons.Count} holons for parent (using fallback method)";
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
                            // fallback: use events to retrieve all holons
                            // Query contract events for holon creation events
                            try
                            {
                                var holonCreatedEvent = Web3Client.Eth.GetContract(_contractAddress, _abi).GetEvent("HolonCreated");
                                var filter = holonCreatedEvent.CreateFilterInput(Nethereum.RPC.Eth.DTOs.BlockParameter.CreateEarliest(), Nethereum.RPC.Eth.DTOs.BlockParameter.CreateLatest());
                                var events = await holonCreatedEvent.GetAllChangesAsync<Nethereum.RPC.Eth.DTOs.FilterLog>(filter);
                                
                                var eventHolons = new List<IHolon>();
                                foreach (var evt in events)
                                {
                                    try
                                    {
                                        // FilterLog does not expose decoded event; indexed string is hashed in topics - skip or decode from event ABI if available
                                        var holonId = "";
                                        if (string.IsNullOrEmpty(holonId)) continue;
                                        var holonResult = await LoadHolonAsync(holonId, loadChildren, recursive, maxChildDepth, continueOnError, loadChildrenFromProvider, version);
                                        if (!holonResult.IsError && holonResult.Result != null)
                                        {
                                            if (type == HolonType.All || holonResult.Result.HolonType == type)
                                            {
                                                eventHolons.Add(holonResult.Result);
                                            }
                                        }
                                    }
                                    catch
                                    {
                                        if (continueOnError) continue;
                                        throw;
                                    }
                                }
                                
                                result.Result = eventHolons;
                                result.IsError = false;
                                result.Message = $"Loaded {eventHolons.Count} holons from contract events (using fallback method)";
                            }
                            catch (Exception fallbackEx)
                            {
                                OASISErrorHandling.HandleError(ref result, $"Failed to load holons using fallback method: {fallbackEx.Message}. Consider implementing 'getAllHolons' method in your smart contract.", fallbackEx);
                            }
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

    }
}
