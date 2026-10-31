using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.PostgreSQLOASIS.IntegrationTests;

public partial class PostgreSqlNativeAcceptanceTests
{
    [DataTestMethod][DataRow(false,false)][DataRow(false,true)][DataRow(true,false)][DataRow(true,true)]
    public async Task NativeImportUsesTheAtomicGraphWriterAndReportsItsOutcome(bool failure,bool synchronous)
    {
        var suffix=Guid.NewGuid().ToString()[9..];var id=Guid.Parse("00000000-"+suffix);var childId=Guid.Parse("ffffffff-"+suffix);
        await using var fixture=new GraphFixture($"native-import-{failure}-{synchronous}",new[]{id,childId});await fixture.Begin();
        var child=new Holon{Id=childId,ParentHolonId=id,Name="import-child",MetaData=new(){["owned-fixture"]=fixture.Marker}};
        var root=new Holon{Id=id,Name="import-root",Children=new List<IHolon>{child},MetaData=new(){["owned-fixture"]=fixture.Marker}};
        string? originalChild=null;
        if(failure){
            fixture.Provider.IsVersionControlEnabled=false;child.Version=int.MaxValue;fixture.BeforeMutation();var seeded=await fixture.Provider.SaveHolonAsync(child,saveChildren:false);Assert.IsFalse(seeded.IsError,seeded.Exception?.ToString());await fixture.Observe();originalChild=await fixture.Body(childId);fixture.Provider.IsVersionControlEnabled=true;
        }
        fixture.BeforeMutation();var result=synchronous?fixture.Provider.Import(new[]{root}):await fixture.Provider.ImportAsync(new[]{root});
        if(failure){Assert.IsTrue(result.IsError);Assert.IsInstanceOfType<InvalidDataException>(result.Exception);await fixture.Observe();Assert.IsFalse(result.Result);Assert.IsFalse(result.IsSaved);Assert.AreEqual(0,result.SavedCount);Assert.IsNull(await fixture.Body(id));Assert.AreEqual(originalChild,await fixture.Body(childId));Assert.AreEqual(0,root.Version);}
        else{Assert.IsFalse(result.IsError,result.Exception?.ToString());await fixture.Observe();Assert.IsTrue(result.Result);Assert.IsTrue(result.IsSaved);Assert.AreEqual(2,result.SavedCount);var read=await fixture.Provider.LoadHolonAsync(id,loadChildrenFromProvider:true);Assert.IsFalse(read.IsError,read.Message);Assert.AreEqual(childId,read.Result.Children.Single().Id);}
    }
}
