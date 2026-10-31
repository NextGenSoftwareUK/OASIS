using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass]
public class EthereumProfilePublicationControlledTests
{
    private const string Transaction="0x1111111111111111111111111111111111111111111111111111111111111111";
    private sealed class AcknowledgedProvider(Action edit):EthereumOASIS("http://127.0.0.1:1",new string('1',64),31337,"0x0000000000000000000000000000000000000001")
    {
        public int Writes;
        protected override async Task<(TransactionReceipt Receipt,T Record)> UpsertStorageHistoryAsync<T>(string kind,T proposed,bool history)
        {
            Writes++;await Task.Yield();edit();
            proposed.Version=9;proposed.VersionId=Guid.NewGuid();
            return(new(){TransactionHash=Transaction,Status=new HexBigInteger(1)},proposed);
        }
    }

    [DataTestMethod]
    [DataRow(false,false,true)][DataRow(false,true,true)][DataRow(true,false,true)][DataRow(true,true,true)]
    [DataRow(false,false,false)][DataRow(false,true,false)][DataRow(true,false,false)][DataRow(true,true,false)]
    public async Task PublicProfileSaveRejectsNonIdentityCallerEdits(bool detail,bool synchronous,bool edit)
    {
        IHolonBase source=detail?new AvatarDetail{Id=Guid.NewGuid(),IsNewHolon=true}:new Avatar{Id=Guid.NewGuid(),IsNewHolon=true};
        var original=source.Id;
        var provider=new AcknowledgedProvider(()=>{if(edit)source.MetaData["later edit"]="preserve";});
        OASISResult<IAvatar>? avatar=null;OASISResult<IAvatarDetail>? avatarDetail=null;
        if(detail)avatarDetail=synchronous?provider.SaveAvatarDetail((IAvatarDetail)source):await provider.SaveAvatarDetailAsync((IAvatarDetail)source);
        else avatar=synchronous?provider.SaveAvatar((IAvatar)source):await provider.SaveAvatarAsync((IAvatar)source);
        if(!edit){
            Assert.IsFalse(detail?avatarDetail!.IsError:avatar!.IsError);
            Assert.IsTrue(detail?avatarDetail!.IsSaved:avatar!.IsSaved);
            Assert.AreEqual(1,provider.Writes);Assert.AreEqual(original,source.Id);Assert.AreEqual(9,source.Version);
            Assert.AreEqual(original.ToString(),source.ProviderUniqueStorageKey[Core.Enums.ProviderType.EthereumOASIS]);return;
        }
        Assert.IsTrue(detail?avatarDetail!.IsError:avatar!.IsError,"Caller edits after acknowledgement must prevent saved-stamp publication.");
        Assert.IsFalse(detail?avatarDetail!.IsSaved:avatar!.IsSaved);
        var error=detail?avatarDetail!.Exception:avatar!.Exception;
        Assert.IsInstanceOfType(error,typeof(EthereumPartialStorageMutationException));
        Assert.IsTrue(Core.Helpers.NativeMutationReconciliation.IsRequired(error));
        Assert.AreEqual(Transaction,((INativeMutationReconciliationRequired)error!).OriginalTransactionId);
        Assert.AreEqual(1,provider.Writes);Assert.AreEqual(original,source.Id);Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
        Assert.IsTrue(source.IsNewHolon);Assert.AreEqual("preserve",source.MetaData["later edit"]);
        Assert.IsFalse(source.ProviderUniqueStorageKey?.ContainsKey(Core.Enums.ProviderType.EthereumOASIS)==true);
    }
}
