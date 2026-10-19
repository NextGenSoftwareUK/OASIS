using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

public partial class EthereumOASISIntegrationTests
{
    [DataTestMethod]
    [DataRow("avatar")][DataRow("avatar-detail")][DataRow("holon")]
    public async Task NativeCheckedHistoryPersistsEncryptedArchiveWithoutPublishingCaller(string kind)
    {
        var (provider,address)=await CreateActivatedProviderAsync(history:true);
        Assert.IsTrue(provider.IsVersionControlEnabled);
        try {
            if(kind=="avatar")await VerifyNativeHistory(provider,address,kind,new Avatar {Id=Guid.NewGuid(),Username="native-private-marker"});
            else if(kind=="avatar-detail")await VerifyNativeHistory(provider,address,kind,new AvatarDetail {Id=Guid.NewGuid(),Username="native-private-marker"});
            else await VerifyNativeHistory(provider,address,kind,new Core.Holons.Holon {Id=Guid.NewGuid(),CreatedByAvatarId=Guid.NewGuid(),Name="native-private-marker",MetaData=new Dictionary<string,object>{{"HistoryTag","original-root"},{"CustomKey","history-root"}},Children=new List<IHolon>{new Core.Holons.Holon {Id=Guid.NewGuid(),Name="native-child"}}});
        }finally{await provider.DeActivateProviderAsync();}
    }

    private static async Task VerifyNativeHistory<T>(EthereumOASIS provider,string address,string kind,T source) where T:class,IHolonBase
    {
        var method=typeof(EthereumOASIS).GetMethod("UpsertStorageHistoryAsync",BindingFlags.NonPublic|BindingFlags.Instance)!.MakeGenericMethod(typeof(T));
        Task<(TransactionReceipt Receipt,T Record)> Save(bool enabled)=>(Task<(TransactionReceipt,T)>)method.Invoke(provider,new object[]{kind,source,enabled})!;
        var first=await Save(true);Assert.AreEqual(BigInteger.One,first.Receipt.Status.Value);Assert.AreEqual(1,first.Record.Version);
        source.Description="native-second-marker";
        var second=await Save(false);Assert.AreEqual(BigInteger.One,second.Receipt.Status.Value);Assert.AreEqual(2,second.Record.Version);
        Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
        if(kind=="avatar"){
            provider.IsVersionControlEnabled=true;
            var saved=await provider.SaveAvatarAsync((IAvatar)source);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(3,source.Version);
            provider.IsVersionControlEnabled=false;
            var synced=provider.SaveAvatar((IAvatar)source);Assert.IsFalse(synced.IsError,synced.Message);Assert.AreEqual(4,source.Version);
        }else if(kind=="avatar-detail"){
            provider.IsVersionControlEnabled=true;
            var saved=await provider.SaveAvatarDetailAsync((IAvatarDetail)source);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(3,source.Version);
            provider.IsVersionControlEnabled=false;
            var synced=provider.SaveAvatarDetail((IAvatarDetail)source);Assert.IsFalse(synced.IsError,synced.Message);Assert.AreEqual(4,source.Version);
        }else{
            provider.IsVersionControlEnabled=true;
            var saved=await provider.SaveHolonAsync((IHolon)source);Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(3,source.Version);
            provider.IsVersionControlEnabled=false;
            source.MetaData["HistoryTag"]="current-root";
            var synced=provider.SaveHolon((IHolon)source);Assert.IsFalse(synced.IsError,synced.Message);Assert.AreEqual(4,source.Version);
            Assert.AreEqual(2,((IHolon)source).Children.Single().Version);
        }
        var service=new NextGenSoftwareOASISService(new Web3(new Account(PrivateKey,ChainId),Host),address);
        var id=new BigInteger(HashUtility.GetNumericHash(source.Id.ToString()));
        var ciphertext=kind switch {
            "avatar"=>(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1.Info,
            "avatar-detail"=>(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1.Info,
            _=>(await service.GetHolonByIdQueryAsync(id)).ReturnValue1.Info
        };
        Assert.IsFalse(ciphertext.Contains("native-private-marker"));Assert.IsFalse(ciphertext.Contains("native-second-marker"));
        var key=PersistedPrivateKeyValueBackend.ResolveEnvironmentKey("OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY");
        try {
            using var codec=new EncryptedRecordCodec(key,compress:true);
            var document=JObject.Parse(codec.Decrypt("oasis/ethereum/storage/v1/"+kind+"/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture),ciphertext));
            var history=new EmbeddedRecordHistory();
            var latest=await history.ReadAsync(source.Id,document,0,OasisJson.DeserializeStored<T>);
            var archived=await history.ReadAsync(source.Id,document,1,OasisJson.DeserializeStored<T>);
            Assert.AreEqual(4,latest.Version);Assert.AreEqual("native-second-marker",latest.Description);Assert.AreEqual(1,archived.Version);Assert.IsNull(archived.Description);
            if(kind=="holon"){
                var children=((IHolon)archived).Children;Assert.AreEqual(1,children.Count());Assert.AreEqual("native-child",children.Single().Name);
                var child=((IHolon)source).Children.Single();child.Description="independent-child";
                Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);Assert.AreEqual(3,child.Version);
                foreach(var selected in new[]{provider.LoadHolon(source.Id,loadChildrenFromProvider:true,version:1),await provider.LoadHolonAsync(source.Id.ToString(),loadChildrenFromProvider:true,version:1)}){
                    Assert.IsFalse(selected.IsError,selected.Message);Assert.AreEqual(1,selected.Result.Version);Assert.AreEqual(3,selected.Result.Children.Single().Version);Assert.AreEqual("independent-child",selected.Result.Children.Single().Description);
                }
                var embedded=await provider.LoadHolonAsync(source.Id,loadChildrenFromProvider:false,version:1);Assert.IsFalse(embedded.IsError,embedded.Message);Assert.AreEqual(0,embedded.Result.Children.Single().Version);
                var collection=await provider.LoadAllHolonsAsync(loadChildrenFromProvider:true,version:1);Assert.IsFalse(collection.IsError,collection.Message);Assert.AreEqual(1,collection.Result.Single(row=>row.Id==source.Id).Version);Assert.AreEqual(3,collection.Result.Single(row=>row.Id==source.Id).Children.Single().Version);
                var parent=await provider.LoadHolonsForParentAsync(source.Id.ToString(),version:1);Assert.IsFalse(parent.IsError,parent.Message);Assert.AreEqual(1,parent.Result.Single().Version);
                var missing=await provider.LoadAllHolonsAsync(version:99);Assert.IsFalse(missing.IsError,missing.Message);Assert.AreEqual(0,missing.Result.Count());
                var metadata=await provider.LoadHolonsByMetaDataAsync("HistoryTag","original-root",loadChildrenFromProvider:true,version:1);Assert.IsFalse(metadata.IsError,metadata.Message);Assert.AreEqual(1,metadata.Result.Single().Version);Assert.AreEqual(3,metadata.Result.Single().Children.Single().Version);
                var custom=await provider.LoadHolonByCustomKeyAsync("history-root",version:1);Assert.IsFalse(custom.IsError,custom.Message);Assert.AreEqual(1,custom.Result.Version);
                var latestMetadata=await provider.LoadHolonsByMetaDataAsync("HistoryTag","original-root");Assert.IsFalse(latestMetadata.IsError,latestMetadata.Message);Assert.AreEqual(0,latestMetadata.Result.Count());
                provider.IsVersionControlEnabled=true;
                var owner=new Avatar{Id=source.CreatedByAvatarId,Username="history-export-owner",Email="history-export@example.invalid"};Assert.IsFalse((await provider.SaveAvatarAsync(owner)).IsError);
                provider.IsVersionControlEnabled=false;
                foreach(var exported in new[]{await provider.ExportAllDataForAvatarByIdAsync(owner.Id,1),provider.ExportAllDataForAvatarByUsername(owner.Username,1),await provider.ExportAllDataForAvatarByEmailAsync(owner.Email,1)}){
                    Assert.IsFalse(exported.IsError,exported.Message);Assert.IsTrue(exported.IsLoaded);Assert.AreEqual(1,exported.Result.Single().Version);Assert.AreEqual(3,exported.Result.Single().Children.Single().Version);
                }
                var search=new Core.Objects.Search.SearchParams{SearchOnlyForCurrentAvatar=false,FilterByMetaData=new Dictionary<string,string>{{"HistoryTag","original-root"}},SearchGroups=new List<Core.Objects.Search.ISearchGroupBase>{new Core.Objects.Search.SearchTextGroup{SearchQuery="native-private-marker",SearchHolons=true,HolonType=Core.Enums.HolonType.All}}};
                var searched=await provider.SearchAsync(search,version:1);Assert.IsFalse(searched.IsError,searched.Message);Assert.AreEqual(1,searched.Result.NumberOfResults);Assert.AreEqual(1,searched.Result.SearchResultHolons.Single().Version);Assert.AreEqual(3,searched.Result.SearchResultHolons.Single().Children.Single().Version);
            }
            else {
                provider.IsVersionControlEnabled=true;
                if(kind=="avatar")Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail{Id=source.Id})).IsError);
                else Assert.IsFalse((await provider.SaveAvatarAsync(new Avatar{Id=source.Id})).IsError);
                provider.IsVersionControlEnabled=false;
                var deleted=await provider.DeleteAvatarAsync(source.Id,softDelete:true);Assert.IsFalse(deleted.IsError,deleted.Message);Assert.IsTrue(deleted.Result);
                var avatarRow=(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1;
                var detailRow=(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1;
                var avatarDocument=JObject.Parse(codec.Decrypt("oasis/ethereum/storage/v1/avatar/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture),avatarRow.Info));
                var detailDocument=JObject.Parse(codec.Decrypt("oasis/ethereum/storage/v1/avatar-detail/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture),detailRow.Info));
                var avatar=await history.ReadAsync(source.Id,avatarDocument,0,OasisJson.DeserializeStored<Avatar>);
                var detail=await history.ReadAsync(source.Id,detailDocument,0,OasisJson.DeserializeStored<AvatarDetail>);
                Assert.AreEqual(kind=="avatar"?5:2,avatar.Version);Assert.AreEqual(kind=="avatar-detail"?5:2,detail.Version);
                Assert.IsFalse(avatar.IsActive);Assert.IsFalse(detail.IsActive);Assert.AreEqual(avatar.DeletedDate,detail.DeletedDate);
                Assert.IsTrue((await history.ReadAsync(source.Id,avatarDocument,1,OasisJson.DeserializeStored<Avatar>)).IsActive);
                Assert.IsTrue((await history.ReadAsync(source.Id,detailDocument,1,OasisJson.DeserializeStored<AvatarDetail>)).IsActive);
                foreach(var historical in new[]{provider.LoadAvatar(source.Id,1),await provider.LoadAvatarAsync(source.Id,1)}){
                    Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.IsTrue(historical.Result.IsActive);
                }
                foreach(var historical in new[]{provider.LoadAvatarDetail(source.Id,1),await provider.LoadAvatarDetailAsync(source.Id,1)}){
                    Assert.IsFalse(historical.IsError,historical.Message);Assert.AreEqual(1,historical.Result.Version);Assert.IsTrue(historical.Result.IsActive);
                }
                var missingAvatar=await provider.LoadAvatarAsync(source.Id,99);Assert.IsTrue(missingAvatar.IsError);Assert.IsNull(missingAvatar.Result);Assert.IsFalse(missingAvatar.IsLoaded);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingAvatar.ErrorCode);
                var missingDetail=provider.LoadAvatarDetail(source.Id,99);Assert.IsTrue(missingDetail.IsError);Assert.IsNull(missingDetail.Result);Assert.IsFalse(missingDetail.IsLoaded);Assert.AreEqual(NextGenSoftware.OASIS.Common.OASISResultErrorCodes.RecordNotFound,missingDetail.ErrorCode);
                var avatars=await provider.LoadAllAvatarsAsync(1);Assert.IsFalse(avatars.IsError,avatars.Message);Assert.AreEqual(1,avatars.Result.Single().Version);
                var details=provider.LoadAllAvatarDetails(1);Assert.IsFalse(details.IsError,details.Message);Assert.AreEqual(1,details.Result.Single().Version);
                var keyed=await provider.LoadAvatarByProviderKeyAsync(source.Id.ToString(),1);Assert.IsFalse(keyed.IsError,keyed.Message);Assert.AreEqual(1,keyed.Result.Version);
                if(kind=="avatar"){var named=await provider.LoadAvatarByUsernameAsync("native-private-marker",1);Assert.IsFalse(named.IsError,named.Message);Assert.AreEqual(1,named.Result.Version);}
                else {var named=provider.LoadAvatarDetailByUsername("native-private-marker",1);Assert.IsFalse(named.IsError,named.Message);Assert.AreEqual(1,named.Result.Version);}
                var absent=await provider.LoadAllAvatarsAsync(99);Assert.IsFalse(absent.IsError,absent.Message);Assert.AreEqual(0,absent.Result.Count());
                var hard=await provider.DeleteAvatarAsync(source.Id,softDelete:false);Assert.IsFalse(hard.IsError,hard.Message);Assert.IsTrue(hard.Result);
                Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarByIdQueryAsync(id)).ReturnValue1.EntityId);
                Assert.AreEqual(BigInteger.Zero,(await service.GetAvatarDetailByIdQueryAsync(id)).ReturnValue1.EntityId);
            }
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
