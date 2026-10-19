using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.AzureCosmosDBOASIS.UnitTests;

[TestClass]
public class CosmosGraphWriteTests
{
    private static AzureCosmosDBOASIS Provider(IHolonRepository repository)
    {
        var provider=new AzureCosmosDBOASIS(new Uri("https://localhost:8081"),"unused","owned",new List<string>());
        provider.IsProviderActivated=true;
        typeof(AzureCosmosDBOASIS).GetField("holonRepository",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.SetValue(provider,repository);
        return provider;
    }

    [DataTestMethod]
    [DataRow(false,true,0,0,1)] [DataRow(true,false,0,0,2)]
    [DataRow(true,true,1,1,1)] [DataRow(true,true,0,0,3)]
    public async Task SelectedGraphHonoursChildrenRecursionAndDepth(bool children,bool recursive,int depth,int initial,int count)
    {
        var seen=new List<IHolon>();var repository=new Mock<IHolonRepository>();
        repository.Setup(r=>r.UpsertAsync(It.IsAny<IHolon>())).Returns((IHolon row)=>{seen.Add(row);return Task.FromResult(row);});
        var grandchild=new Holon();var child=new Holon {Children=new List<IHolon> {grandchild}};
        var root=new Holon {Children=new List<IHolon> {child}};
        var result=await Provider(repository.Object).SaveHolonsAsync(new[] {root},children,recursive,depth,initial);
        Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(count,seen.Count);
        Assert.AreNotEqual(Guid.Empty,root.Id);
        if(count>1) Assert.AreEqual(root.Id,child.ParentHolonId);
        if(count>2) Assert.AreEqual(child.Id,grandchild.ParentHolonId);
        if(count==1) Assert.AreEqual(Guid.Empty,child.Id);
    }

    [TestMethod]
    public async Task InvalidGraphDoesNotPublishIdsOrCallRepository()
    {
        var repository=new Mock<IHolonRepository>(MockBehavior.Strict);
        var root=new Holon();root.Children=new List<IHolon> {root};
        var result=await Provider(repository.Object).SaveHolonAsync(root);
        Assert.IsTrue(result.IsError);Assert.AreEqual(Guid.Empty,root.Id);repository.VerifyNoOtherCalls();
    }

    [TestMethod]
    public async Task UncertainSecondWriteStopsAndRetainsFirstAcknowledgement()
    {
        var calls=0;var cause=new CosmosMutationReconciliationException("owned",new IOException("lost response"));
        var repository=new Mock<IHolonRepository>();
        repository.Setup(r=>r.UpsertAsync(It.IsAny<IHolon>())).Returns((IHolon row)=>++calls==1 ? Task.FromResult(row):Task.FromException<IHolon>(cause));
        var rows=new[] {new Holon {Id=Guid.NewGuid()},new Holon {Id=Guid.NewGuid()},new Holon {Id=Guid.NewGuid()}};
        var result=await Provider(repository.Object).SaveHolonsAsync(rows,continueOnError:true);
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(2,calls);
        var partial=result.Exception as CosmosPartialMutationException;Assert.IsNotNull(partial);
        Assert.AreEqual(rows[0].Id,partial.AcknowledgedRecordIds.Single());Assert.AreSame(cause,partial.InnerException);
        Assert.IsTrue(NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(partial));
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task ChangedBatchCallerIdentityIsNotPublishedOrWrittenAgain(bool synchronous,bool later)
    {
        var first=new Holon();var second=new Holon();var replacement=Guid.NewGuid();var calls=0;
        var repository=new Mock<IHolonRepository>();
        repository.Setup(r=>r.UpsertAsync(It.IsAny<IHolon>())).Returns(async(IHolon row)=>{
            calls++;await Task.Yield();(later?second:first).Id=replacement;return row;
        });
        var provider=Provider(repository.Object);
        var result=synchronous ? provider.SaveHolons(new[]{first,second}):await provider.SaveHolonsAsync(new[]{first,second});
        Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,calls);
        Assert.AreEqual(replacement,(later?second:first).Id);
        var partial=result.Exception as CosmosPartialMutationException;Assert.IsNotNull(partial);
        Assert.AreEqual(1,partial.AcknowledgedRecordIds.Count);
        Assert.AreNotEqual(replacement,partial.AcknowledgedRecordIds.Single());
        Assert.AreNotEqual(Guid.Empty,partial.AcknowledgedRecordIds.Single());
        if(later)Assert.AreEqual(first.Id,partial.AcknowledgedRecordIds.Single());
    }
}
