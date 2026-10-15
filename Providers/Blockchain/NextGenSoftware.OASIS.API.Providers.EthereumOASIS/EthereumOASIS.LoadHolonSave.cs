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

            var committed = new List<string>();
            var saved = new List<IHolon>();
            try
            {
                foreach (var holon in holons)
                {
                    var holonEntityId = HashUtility.GetNumericHash(holon.Id.ToString());
                    var holonId = holon.Id.ToString();
                    var holonEntityInfo = SerializePrivateStorage("holon",holon);
                    
                    var createHolonResult = await _nextGenSoftwareOasisService
                        .CreateHolonRequestAndWaitForReceiptAsync(holonEntityId, holonId, holonEntityInfo);

                    if (!StorageReceiptSucceeded(createHolonResult))
                    {
                        OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, "Receipt is missing, reverted or lacks success status/identity."));
                        if(!continueOnError)
                            break;
                    }
                    else
                    {
                        committed.Add(createHolonResult.TransactionHash);
                        saved.Add(holon);
                    }
                }

                result.Result = saved;
                result.IsSaved = !result.IsError;
            }
            catch (RpcResponseException ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.RpcError), ex);
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, string.Concat(errorMessage, ex.Message), ex);
            }

            FencePartialStorageWrite(result, committed);
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in DeleteHolon method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = Web3Client.Eth.GetContractHandler(ContractAddress)
                    .SendRequestAndWaitForReceiptAsync(new ContractDefinition.DeleteHolonByIdentityFunction { EntityId=holonEntityId, Identity=id.ToString() }).GetAwaiter().GetResult();

                if (!StorageReceiptSucceeded(requestTransaction))
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
                var requestTransaction = await Web3Client.Eth.GetContractHandler(ContractAddress)
                    .SendRequestAndWaitForReceiptAsync(new ContractDefinition.DeleteHolonByIdentityFunction { EntityId=holonEntityId, Identity=id.ToString() });

                if (!StorageReceiptSucceeded(requestTransaction))
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

                var holonEntityResult = DeserializePrivateStorage<Holon>("holon",holonDto.ReturnValue1.EntityId,holonDto.ReturnValue1.Info,id);
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

                var holonEntityResult = DeserializePrivateStorage<Holon>("holon",holonDto.ReturnValue1.EntityId,holonDto.ReturnValue1.Info,id);
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

        public override Task<OASISResult<IHolon>> LoadHolonAsync(string providerKey, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadProviderIdentityAsync(providerKey,version,id=>LoadHolonAsync(id,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version));

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
                if (version != 0) throw new NotSupportedException("Ethereum collection history is not implemented; only current version 0 is supported.");
                if (maxChildDepth < 0 || curentChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth),"Child depth values must be nonnegative.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
                }
                var block = new Nethereum.RPC.Eth.DTOs.BlockParameter(await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var count = await _nextGenSoftwareOasisService.GetHolonsCountQueryAsync(block);
                var records = new List<IHolon>();
                var identities = new HashSet<Guid>();
                for (BigInteger index = 0; index < count; index++)
                {
                    var row = (await _nextGenSoftwareOasisService.ContractHandler
                        .QueryDeserializingToObjectAsync<ContractDefinition.GetHolonAtFunction,ContractDefinition.GetHolonByIdOutputDTO>(
                            new ContractDefinition.GetHolonAtFunction { Index=index },block)).ReturnValue1;
                    if (row == null || !Guid.TryParseExact(row.HolonId,"D",out var id))
                        throw new InvalidDataException("Ethereum holon has an invalid public GUID.");
                    var holon = DeserializePrivateStorage<Holon>("holon",row.EntityId,row.Info,id);
                    if (holon == null || !identities.Add(holon.Id))
                        throw new InvalidDataException("Ethereum holon collection contains a missing or duplicate identity.");
                    records.Add(holon);
                }
                var children = records.ToLookup(row=>row.ParentHolonId);
                IHolon Project(IHolon source,int depth,HashSet<Guid> path)
                {
                    if (!path.Add(source.Id)) throw new InvalidDataException("Ethereum persisted holon graph contains a cycle.");
                    // Each returned root owns its graph; projecting one root must not trim another.
                    var copy = JsonConvert.DeserializeObject<Holon>(JsonConvert.SerializeObject(source),new StoredHolonConverter());
                    copy.Children = new List<IHolon>();
                    if (loadChildren && (maxChildDepth == 0 || depth < maxChildDepth) && (recursive || depth == curentChildDepth))
                    {
                        var selected = loadChildrenFromProvider ? children[source.Id] : source.Children ?? Enumerable.Empty<IHolon>();
                        foreach (var child in selected)
                        {
                            if (child == null) throw new InvalidDataException("Ethereum persisted holon graph contains a null child.");
                            copy.Children.Add(Project(child,depth+1,path));
                        }
                    }
                    path.Remove(source.Id);
                    return copy;
                }
                result.Result = records.Where(row=>type == HolonType.All || row.HolonType == type)
                    .Select(row=>Project(row,curentChildDepth,new HashSet<Guid>())).ToList();
                result.IsLoaded = true;
            }
            catch (Exception error)
            {
                OASISErrorHandling.HandleError(ref result,$"Error loading native Ethereum holons: {error.Message}",error);
            }
            return result;
        }

    }
}
