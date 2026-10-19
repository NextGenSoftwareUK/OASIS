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
        var (provider,address)=await CreateActivatedProviderAsync();
        try {
            if(kind=="avatar")await VerifyNativeHistory(provider,address,kind,new Avatar {Id=Guid.NewGuid(),Username="native-private-marker"});
            else if(kind=="avatar-detail")await VerifyNativeHistory(provider,address,kind,new AvatarDetail {Id=Guid.NewGuid(),Username="native-private-marker"});
            else await VerifyNativeHistory(provider,address,kind,new Core.Holons.Holon {Id=Guid.NewGuid(),Name="native-private-marker",Children=new List<IHolon>{new Core.Holons.Holon {Id=Guid.NewGuid(),Name="native-child"}}});
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
            Assert.AreEqual(2,latest.Version);Assert.AreEqual("native-second-marker",latest.Description);Assert.AreEqual(1,archived.Version);Assert.IsNull(archived.Description);
            if(kind=="holon"){var children=((IHolon)archived).Children;Assert.AreEqual(1,children.Count());Assert.AreEqual("native-child",children.Single().Name);}
        }finally{CryptographicOperations.ZeroMemory(key);}
    }
}
