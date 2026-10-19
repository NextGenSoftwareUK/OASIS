using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
using Newtonsoft.Json.Linq;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.IntegrationTests;

[TestClass,DoNotParallelize]
public class EthereumRecordHistoryTests
{
    [DataTestMethod]
    [DataRow("avatar")][DataRow("avatar-detail")][DataRow("holon")]
    public async Task DetachedHistoryPreservesArchiveAndEmbeddedChildren(string kind)
    {
        const string name="OASIS_ETHEREUM_STORAGE_ENCRYPTION_KEY";
        var previous=Environment.GetEnvironmentVariable(name);var key=RandomNumberGenerator.GetBytes(32);
        Environment.SetEnvironmentVariable(name,Convert.ToBase64String(key));
        try {
            if(kind=="avatar")await Verify(kind,new Avatar {Id=Guid.NewGuid(),Username="private-marker"},key);
            else if(kind=="avatar-detail")await Verify(kind,new AvatarDetail {Id=Guid.NewGuid(),Username="private-marker"},key);
            else await Verify(kind,new Holon {Id=Guid.NewGuid(),Name="private-marker",Children=new List<IHolon>{new Holon {Id=Guid.NewGuid(),Name="embedded-child"}}},key);
        }finally{Environment.SetEnvironmentVariable(name,previous);CryptographicOperations.ZeroMemory(key);}
    }

    private static async Task Verify<T>(string kind,T source,byte[] key) where T:class,IHolonBase
    {
        var method=typeof(EthereumOASIS).GetMethod("StageStorageHistoryAsync",BindingFlags.NonPublic|BindingFlags.Static)!.MakeGenericMethod(typeof(T));
        Task<(T Record,string Ciphertext)> Stage(BigInteger id,string identity,string ciphertext,bool enabled)
            =>(Task<(T,string)>)method.Invoke(null,new object?[]{kind,source,id,identity,ciphertext,enabled})!;
        var id=new BigInteger(HashUtility.GetNumericHash(source.Id.ToString()));
        var first=await Stage(0,null!,null!,true);
        Assert.AreEqual(0,source.Version);Assert.AreEqual(Guid.Empty,source.VersionId);
        Assert.AreEqual(1,first.Record.Version);Assert.IsFalse(first.Ciphertext.Contains("private-marker"));
        Assert.IsFalse(source.ProviderUniqueStorageKey.ContainsKey(API.Core.Enums.ProviderType.EthereumOASIS));
        source.Description="second-marker";
        var second=await Stage(id,source.Id.ToString(),first.Ciphertext,false);
        Assert.AreEqual(2,second.Record.Version);Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);
        Assert.AreEqual(0,source.Version);
        using var codec=new EncryptedRecordCodec(key,compress:true);
        var context="oasis/ethereum/storage/v1/"+kind+"/"+id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var document=JObject.Parse(codec.Decrypt(context,second.Ciphertext));
        var history=new EmbeddedRecordHistory();
        var archived=await history.ReadAsync(source.Id,document,1,OasisJson.DeserializeStored<T>);
        Assert.AreEqual(1,archived.Version);Assert.IsNull(archived.Description);
        if(source is IHolon original && original.Children?.Any()==true && archived is IHolon holon){Assert.AreEqual(1,holon.Children.Count());Assert.AreEqual("embedded-child",holon.Children.Single().Name);}
        Assert.IsNull(await history.ReadAsync(source.Id,document,99,OasisJson.DeserializeStored<T>));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>{await Stage(id,Guid.NewGuid().ToString(),first.Ciphertext,true);});
        document[EmbeddedRecordHistory.PropertyName]!["schema"]=99;
        var malformed=codec.Encrypt(context,document.ToString(Newtonsoft.Json.Formatting.None));
        await Assert.ThrowsExceptionAsync<InvalidDataException>(async()=>{await Stage(id,source.Id.ToString(),malformed,false);});
        var legacy=await Stage(0,null!,null!,false);Assert.AreEqual(0,legacy.Record.Version);
    }
}
