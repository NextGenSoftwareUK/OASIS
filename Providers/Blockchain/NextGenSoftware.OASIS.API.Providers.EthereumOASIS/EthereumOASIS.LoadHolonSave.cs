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
            var result=new OASISResult<IEnumerable<IHolon>>();
            var committed=new List<string>();
            var prepared=false;
            try
            {
                var plan=PlanStoredGraph(holons,saveChildren,recursive,maxChildDepth,curentChildDepth);
                var createHistory=IsVersionControlEnabled;
                prepared=true;
                if(!IsProviderActivated)
                {
                    var activation=await ActivateProviderAsync();
                    if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
                }
                foreach(var node in plan.Nodes)
                {
                    node.Publication.ValidateUnchanged();
                    var staged=await UpsertPlannedStorageHistoryAsync(node.Write,createHistory);
                    var receipt=staged.Receipt;
                    if(!StorageReceiptSucceeded(receipt))
                    {
                        OASISErrorHandling.HandleError(ref result,"Ethereum graph write reverted.");
                        if(!continueOnError) break;
                    }
                    else
                    {
                        committed.Add(receipt.TransactionHash);
                        node.Publication.ValidateUnchanged();
                        node.Source.Id=node.Record.Id;node.Source.ParentHolonId=node.Record.ParentHolonId;
                        node.Source.Version=staged.Record.Version;node.Source.VersionId=staged.Record.VersionId;node.Source.PreviousVersionId=staged.Record.PreviousVersionId;
                        if(node.Source is Holon source)source.ChildRoleManifestJson=staged.Record.ChildRoleManifestJson;
                        CommitEthereumStorageIdentity(node.Source);
                        node.Record.Version=staged.Record.Version;node.Record.VersionId=staged.Record.VersionId;node.Record.PreviousVersionId=staged.Record.PreviousVersionId;
                        node.Record.ChildRoleManifestJson=staged.Record.ChildRoleManifestJson;
                        CommitEthereumStorageIdentity(node.Record);
                    }
                }
                if(!result.IsError) { result.Result=plan.Roots; result.IsSaved=true; }
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,$"Ethereum graph save failed: {cause.Message}",cause);
                if(!prepared && cause is ArgumentException) result.ErrorCode="HYPERDRIVE_PAYLOAD_INVALID";
            }
            FencePartialStorageWrite(result,committed);
            return result;
        }

        public override OASISResult<IHolon> DeleteHolon(Guid id)
        {
            var result = new OASISResult<IHolon>();
            string errorMessage = "Error in DeleteHolon method in EthereumOASIS while deleting holon. Reason: ";

            try
            {
                var holonEntityId = HashUtility.GetNumericHash(id.ToString());
                var requestTransaction = SubmitStorageRequestAsync(new ContractDefinition.DeleteHolonByIdentityFunction { EntityId=holonEntityId, Identity=id.ToString() }).GetAwaiter().GetResult();

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
                var requestTransaction = await SubmitStorageRequestAsync(new ContractDefinition.DeleteHolonByIdentityFunction { EntityId=holonEntityId, Identity=id.ToString() });

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
            return DeleteHolonByProviderKeyAsync(providerKey).GetAwaiter().GetResult();
        }

        public async Task<OASISResult<IHolon>> DeleteHolonByProviderKeyAsync(string providerKey)
        {
            var result = new OASISResult<IHolon>();
            try
            {
                var holonResult = await LoadHolonAsync(providerKey,loadChildren:false);
                if (holonResult.IsError)return holonResult;
                if (holonResult.Result == null)throw new InvalidDataException("Ethereum provider-key lookup returned no holon.");
                // Keep native acknowledgement flags, error codes and reconciliation markers intact.
                var deleted = await DeleteHolonAsync(holonResult.Result.Id);
                if(!deleted.IsError&&deleted.IsDeleted)deleted.Result=holonResult.Result;
                return deleted;
            }
            catch (Exception ex)
            {
                OASISErrorHandling.HandleError(ref result, $"Error deleting holon by provider key from Ethereum: {ex.Message}", ex);
            }
            return result;
        }

        public override OASISResult<IHolon> LoadHolon(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
            => LoadHolonAsync(id,loadChildren,recursive,maxChildDepth,continueOnError,loadChildrenFromProvider,version).GetAwaiter().GetResult();

        public override async Task<OASISResult<IHolon>> LoadHolonAsync(Guid id, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            var result=new OASISResult<IHolon>();
            try
            {
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                if(maxChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth));
                if(loadChildren && loadChildrenFromProvider)
                {
                    var collection=await LoadAllHolonsAsync(HolonType.All,loadChildren,recursive,maxChildDepth,0,continueOnError,true,version);
                    if(collection.IsError)
                    {
                        OASISErrorHandling.HandleError(ref result,collection.Message,collection.Exception);
                        result.ErrorCode=collection.ErrorCode;
                        return result;
                    }
                    result.Result=collection.Result.SingleOrDefault(row=>row.Id == id)
                        ?? throw new KeyNotFoundException("No Ethereum holon exists at the requested GUID.");
                }
                else
                {
                    if(!IsProviderActivated)
                    {
                        var activation=await ActivateProviderAsync();
                        if(activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
                    }
                    var row=(await _nextGenSoftwareOasisService.GetHolonByIdQueryAsync(HashUtility.GetNumericHash(id.ToString())))?.ReturnValue1
                        ?? throw new InvalidDataException("Ethereum native holon response was missing.");
                    var holon=ReadPrivateStorageVersion<Holon>("holon",row.EntityId,row.Info,id,version);
                    if(holon==null){result.ErrorCode=OASISResultErrorCodes.RecordNotFound;throw new KeyNotFoundException("Ethereum holon version not found.");}
                    result.Result=CreateHolonProjector(new[] {holon},loadChildren,recursive,maxChildDepth,0,false)(holon);
                }
                result.IsLoaded=true;
            }
            catch(Exception cause)
            {
                OASISErrorHandling.HandleError(ref result,$"Error loading native Ethereum holon: {cause.Message}",cause);
                if(cause is KeyNotFoundException)result.ErrorCode=OASISResultErrorCodes.RecordNotFound;
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
            var result = await LoadAllHolonsAsync(type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
            return ProjectParentSnapshot(result,id,type,loadChildren,recursive,maxChildDepth,curentChildDepth);
        }

        public override OASISResult<IEnumerable<IHolon>> LoadHolonsForParent(string providerKey, HolonType type = HolonType.All, bool loadChildren = true,bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            return LoadHolonsForParentAsync(providerKey, type, loadChildren, recursive, maxChildDepth, curentChildDepth, continueOnError, loadChildrenFromProvider, version).Result;
        }

        public override async Task<OASISResult<IEnumerable<IHolon>>> LoadHolonsForParentAsync(string providerKey, HolonType type = HolonType.All, bool loadChildren = true, bool recursive = true, int maxChildDepth = 0, int curentChildDepth = 0, bool continueOnError = true, bool loadChildrenFromProvider = false, int version = 0)
        {
            if (!Guid.TryParseExact(providerKey,"D",out var id) || id == Guid.Empty)
            {
                var result = new OASISResult<IEnumerable<IHolon>>();
                var cause = new ArgumentException("Ethereum parent provider key must be a nonempty D-format record GUID.",nameof(providerKey));
                OASISErrorHandling.HandleError(ref result,cause.Message,cause);
                return result;
            }
            return await LoadHolonsForParentAsync(id,type,loadChildren,recursive,maxChildDepth,curentChildDepth,continueOnError,loadChildrenFromProvider,version);
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
                if(version<0)throw new ArgumentOutOfRangeException(nameof(version));
                if (maxChildDepth < 0 || curentChildDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxChildDepth),"Child depth values must be nonnegative.");
                if (!IsProviderActivated)
                {
                    var activation = await ActivateProviderAsync();
                    if (activation.IsError) throw new InvalidOperationException(activation.Message,activation.Exception);
                }
                var block = new Nethereum.RPC.Eth.DTOs.BlockParameter(await Web3Client.Eth.Blocks.GetBlockNumber.SendRequestAsync());
                var count = await _nextGenSoftwareOasisService.GetHolonsCountQueryAsync(block);
                var records = new List<IHolon>();
                var currentRecords=new List<IHolon>();
                var identities = new HashSet<Guid>();
                for (BigInteger index = 0; index < count; index++)
                {
                    var row = (await _nextGenSoftwareOasisService.ContractHandler
                        .QueryDeserializingToObjectAsync<ContractDefinition.GetHolonAtFunction,ContractDefinition.GetHolonByIdOutputDTO>(
                            new ContractDefinition.GetHolonAtFunction { Index=index },block)).ReturnValue1;
                    if (row == null || !Guid.TryParseExact(row.HolonId,"D",out var id))
                        throw new InvalidDataException("Ethereum holon has an invalid public GUID.");
                    if (!identities.Add(id))
                        throw new InvalidDataException("Ethereum holon collection contains a missing or duplicate identity.");
                    var decoded=DecryptStorageDocument<Holon>("holon",row.EntityId,row.Info,id);
                    currentRecords.Add(decoded.Record);
                    var holon=version==0?decoded.Record:await RecordHistory.ReadAsync(id,decoded.Document,version,NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.OasisJson.DeserializeStored<Holon>);
                    if(holon!=null)records.Add(holon);
                }
                var project=CreateHolonProjector(loadChildrenFromProvider?currentRecords:records,loadChildren,recursive,maxChildDepth,curentChildDepth,loadChildrenFromProvider);
                result.Result = records.Where(row=>type == HolonType.All || row.HolonType == type).Select(project).ToList();
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
