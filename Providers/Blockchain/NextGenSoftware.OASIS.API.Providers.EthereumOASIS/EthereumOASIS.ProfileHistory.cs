using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS;

public partial class EthereumOASIS
{
    private async Task<OASISResult<TInterface>> SaveProfileHistoryAsync<TInterface,TRecord>(TInterface source,string kind)
        where TInterface:class,IHolonBase where TRecord:class,IHolonBase
    {
        ArgumentNullException.ThrowIfNull(source);
        var result=new OASISResult<TInterface>();
        string acknowledged=null;
        try {
            var originalId=source.Id;
            var snapshot=OasisJson.DeserializeStored<TRecord>(SerializeCanonicalEmbeddedRecord(source).ToString(Newtonsoft.Json.Formatting.None));
            var committed=await UpsertStorageHistoryAsync(kind,snapshot,IsVersionControlEnabled);
            if(!StorageReceiptSucceeded(committed.Receipt))throw new InvalidOperationException("Ethereum profile checked write reverted.");
            acknowledged=committed.Receipt.TransactionHash;
            if(source.Id!=originalId)throw new InvalidOperationException("Caller profile identity changed during acknowledged Ethereum write.");
            CommitEthereumStorageIdentity(source);
            source.Version=committed.Record.Version;source.VersionId=committed.Record.VersionId;source.PreviousVersionId=committed.Record.PreviousVersionId;
            result.Result=source;result.IsSaved=true;
        }catch(Exception cause){
            if(acknowledged!=null)cause=new EthereumPartialStorageMutationException(new[]{acknowledged},cause);
            OASISErrorHandling.HandleError(ref result,"Ethereum profile save failed: "+cause.Message,cause);
        }
        return result;
    }
}
