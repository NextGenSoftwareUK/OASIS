using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;
namespace NextGenSoftware.OASIS.API.Providers.NEAROASIS.UnitTests;

[TestClass]
public class NearObservedStageTests
{
    private static readonly Type Service=typeof(NEAROASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.NEAROASIS.NearSdkService")!;
    private static object Snapshot(string key,string? json)=>Activator.CreateInstance(Service.GetNestedType("StoredSnapshot",BindingFlags.NonPublic)!,BindingFlags.Instance|BindingFlags.NonPublic,null,new object?[]{null,key,json==null?null:"observed-opaque-ciphertext",json},null)!;
    private static async Task<KeyValuePair<string,string>?> Stage(IHolonBase proposed,string key,string? previous,bool history)
    {
        var method=typeof(NEAROASIS).GetMethod("StageObservedRecordVersionAsync",BindingFlags.Static|BindingFlags.NonPublic)!.MakeGenericMethod(proposed.GetType());
        return await (Task<KeyValuePair<string,string>?>)method.Invoke(null,new[]{proposed,Snapshot(key,previous),(object)history})!;
    }
    [DataTestMethod][DataRow(0,true)][DataRow(1,true)][DataRow(2,true)][DataRow(0,false)][DataRow(1,false)][DataRow(2,false)]
    public async Task StageUsesObservedPreviousAndDoesNotPerformNativeArchiveWrite(int kind,bool history)
    {
        IHolonBase proposed=kind==0?new Avatar():kind==1?new AvatarDetail():new Holon();proposed.Id=Guid.NewGuid();proposed.Version=2;
        var old=OasisJson.Deserialize(OasisJson.Serialize(proposed),proposed.GetType()) as IHolonBase;old!.VersionId=Guid.NewGuid();old.MetaData["marker"]="old";
        proposed.MetaData["marker"]="new";var originalJson=OasisJson.Serialize(old);
        var key=(kind==0?"avatar:":kind==1?"avatar-detail:":"holon:")+proposed.Id.ToString("D");
        var archive=await Stage(proposed,key,originalJson,history);
        Assert.AreEqual(3,proposed.Version);Assert.AreEqual(old.VersionId,proposed.PreviousVersionId);
        if(history){Assert.AreEqual("history:"+key+":0000000002",archive!.Value.Key);Assert.AreEqual(originalJson,archive.Value.Value);}
        else Assert.IsNull(archive);
        Assert.AreEqual(2,old.Version);Assert.AreEqual("old",old.MetaData["marker"]);
    }
    [DataTestMethod][DataRow(false)][DataRow(true)]
    public async Task NewRecordStagesWithoutArchive(bool history)
    {
        var row=new Holon{Id=Guid.NewGuid()};Assert.IsNull(await Stage(row,"holon:"+row.Id,null,history));Assert.AreEqual(1,row.Version);
    }
    [TestMethod]public async Task LegacyVersionZeroBeginsAddressableHistoryAtOne()
    {
        var row=new Holon{Id=Guid.NewGuid()};Assert.IsNull(await Stage(row,"holon:"+row.Id,OasisJson.Serialize(row),true));Assert.AreEqual(1,row.Version);
    }
    [TestMethod]public async Task WrongObservedIdentityCannotStage()
    {
        var row=new Holon{Id=Guid.NewGuid()};var old=new Holon{Id=Guid.NewGuid(),Version=1};
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>Stage(row,"holon:"+row.Id,OasisJson.Serialize(old),true));Assert.AreEqual(0,row.Version);
    }
    [DataTestMethod][DataRow("key")][DataRow("negative")][DataRow("exhausted")]
    public async Task InvalidObservedKeyOrVersionCannotProduceArchive(string mode)
    {
        var row=new Holon{Id=Guid.NewGuid()};var old=new Holon{Id=row.Id,Version=mode=="negative"?-1:mode=="exhausted"?int.MaxValue:1};
        var key=(mode=="key"?"avatar:":"holon:")+row.Id;
        if(mode=="exhausted")await Assert.ThrowsExceptionAsync<InvalidDataException>(()=>Stage(row,key,OasisJson.Serialize(old),true));
        else await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>Stage(row,key,OasisJson.Serialize(old),true));
        Assert.AreEqual(0,row.Version);
    }
}
