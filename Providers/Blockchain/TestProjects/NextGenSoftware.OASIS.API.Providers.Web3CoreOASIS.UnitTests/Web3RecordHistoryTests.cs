using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Utilities;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

[TestClass][DoNotParallelize]
public class Web3RecordHistoryTests
{
    [DataTestMethod][DataRow(0)][DataRow(1)][DataRow(2)]
    public async Task EncryptedHistoryStagesDetachedAndRetainsArchiveWhenDisabled(int kind)
    {
        if(kind==0)await Check("avatar",new Avatar{Id=Guid.NewGuid()});
        else if(kind==1)await Check("detail",new AvatarDetail{Id=Guid.NewGuid()});
        else await Check("holon",new Holon{Id=Guid.NewGuid()});
    }
    private static async Task Check<T>(string kind,T proposal) where T:class,IHolonBase
    {
        var previousKey=Environment.GetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY");
        var key=RandomNumberGenerator.GetBytes(32);
        try{
            Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",Convert.ToBase64String(key));
            var provider=new PolygonOASIS.PolygonOASIS("http://127.0.0.1:18545","unused","0xowned");
            var numeric=(uint)HashUtility.GetNumericHash(proposal.Id.ToString());
            var external=Web3CoreOASISBaseProvider.PrivateExternalId(proposal.Id);
            proposal.MetaData=new(){{"marker","private-first"}};
            var legacy=await provider.StagePrivateHistoryAsync(kind,proposal,null,false);
            Assert.AreEqual(0,legacy.Record.Version);Assert.AreEqual(0,proposal.Version);
            var first=await provider.StagePrivateHistoryAsync(kind,proposal,null,true);
            Assert.AreEqual(1,first.Record.Version);Assert.AreEqual(0,proposal.Version);
            Assert.IsFalse(Encoding.UTF8.GetString(first.Info).Contains("private-first"));
            var stored=new EntityOASIS{EntityId=numeric,ExternalId=external,Info=first.Info};
            var original=(byte[])first.Info.Clone();proposal.MetaData["marker"]="private-second";
            var second=await provider.StagePrivateHistoryAsync(kind,proposal,stored,false);
            Assert.AreEqual(2,second.Record.Version);Assert.AreEqual(first.Record.VersionId,second.Record.PreviousVersionId);
            Assert.AreEqual(0,proposal.Version);CollectionAssert.AreEqual(original,stored.Info);
            var context=(string)typeof(Web3CoreOASISBaseProvider).GetMethod("PrivateContext",BindingFlags.Instance|BindingFlags.NonPublic)!
                .Invoke(provider,new object[]{kind,new System.Numerics.BigInteger(numeric),external})!;
            using var codec=new EncryptedRecordCodec(key,compress:true);
            var document=JObject.Parse(codec.Decrypt(context,Encoding.UTF8.GetString(second.Info)));
            var history=new EmbeddedRecordHistory();
            var past=await history.ReadAsync(proposal.Id,document,1,OasisJson.Deserialize<T>);
            Assert.AreEqual("private-first",past.MetaData["marker"]);
            Assert.IsNull(await history.ReadAsync(proposal.Id,document,99,OasisJson.Deserialize<T>));
            document[EmbeddedRecordHistory.PropertyName]!["schema"]=42;
            var corrupt=new EntityOASIS{EntityId=numeric,ExternalId=external,Info=Encoding.UTF8.GetBytes(codec.Encrypt(context,document.ToString()))};
            Assert.ThrowsException<InvalidDataException>(()=>provider.DecodePrivateRecord<T>(kind,proposal.Id,corrupt));
            await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>provider.StagePrivateHistoryAsync(kind,proposal,corrupt,false));
            var foreign=new EntityOASIS{EntityId=numeric,ExternalId=new byte[32],Info=original};
            await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>provider.StagePrivateHistoryAsync(kind,proposal,foreign,true));
        }finally{Environment.SetEnvironmentVariable("OASIS_WEB3CORE_STORAGE_ENCRYPTION_KEY",previousKey);CryptographicOperations.ZeroMemory(key);}
    }
}
