using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public sealed class OrdinarySaveAcknowledgementTests
{
    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task ImportPreservesCompletedCountsAndSaveOutcome(bool synchronous,bool fail)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var first=new Holon {Id=Guid.NewGuid()};var second=new Holon {Id=Guid.NewGuid()};
        if(fail)backend.FailPutKey="oasis/holon/"+second.Id.ToString("N");
        var result=synchronous?provider.Import(new[] {first,second}):await provider.ImportAsync(new[] {first,second});
        Assert.AreEqual(fail,result.IsError);Assert.AreEqual(!fail,result.Result);
        Assert.AreEqual(!fail,result.IsSaved);Assert.AreEqual(fail?1:2,result.SavedCount);
        Assert.IsTrue(backend.Items.ContainsKey("oasis/holon/"+first.Id.ToString("N")));
        Assert.AreEqual(!fail,backend.Items.ContainsKey("oasis/holon/"+second.Id.ToString("N")));
        if(fail)Assert.IsNotNull(result.Exception);
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task FailedGraphOrBatchCountsOnlyCompletedRecords(bool synchronous,bool batch)
    {
        var backend=new InMemoryBackend();var provider=new TestProvider(backend);
        var root=new Holon {Id=Guid.NewGuid()};var failing=new Holon {Id=Guid.NewGuid()};
        backend.FailPutKey="oasis/holon/"+failing.Id.ToString("N");
        if(batch){
            var result=synchronous?provider.SaveHolons(new[] {root,failing},saveChildren:false)
                :await provider.SaveHolonsAsync(new[] {root,failing},saveChildren:false);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);
        }else{
            root.Children.Add(failing);
            var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,result.SavedCount);
        }
        Assert.IsTrue(backend.Items.ContainsKey("oasis/holon/"+root.Id.ToString("N")));
        Assert.IsFalse(backend.Items.ContainsKey("oasis/holon/"+failing.Id.ToString("N")));
    }

    [DataTestMethod]
    [DataRow(0,false)] [DataRow(0,true)]
    [DataRow(1,false)] [DataRow(1,true)]
    [DataRow(2,false)] [DataRow(2,true)]
    [DataRow(3,false)] [DataRow(3,true)]
    [DataRow(4,false)] [DataRow(4,true)]
    public async Task SuccessfulOrdinarySavesCountAcknowledgedRecords(int shape,bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend());
        if(shape==0){
            var row=new Avatar {Id=Guid.NewGuid(),Username="owned-count"};
            var result=synchronous?provider.SaveAvatar(row):await provider.SaveAvatarAsync(row);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,result.SavedCount);
        }else if(shape==1){
            var row=new AvatarDetail {Id=Guid.NewGuid()};
            var result=synchronous?provider.SaveAvatarDetail(row):await provider.SaveAvatarDetailAsync(row);
            Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(1,result.SavedCount);
        }else{
            Holon Graph(bool children){
                var root=new Holon {Id=Guid.NewGuid()};
                if(children){var child=new Holon {Id=Guid.NewGuid()};child.Children.Add(new Holon {Id=Guid.NewGuid()});root.Children.Add(child);}
                return root;
            }
            if(shape==4){
                var roots=new[] {Graph(true),Graph(false)};
                var result=synchronous?provider.SaveHolons(roots):await provider.SaveHolonsAsync(roots);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(4,result.SavedCount);
            }else{
                var root=Graph(shape==3);
                var result=synchronous?provider.SaveHolon(root):await provider.SaveHolonAsync(root);
                Assert.IsFalse(result.IsError,result.Message);Assert.IsTrue(result.IsSaved);Assert.AreEqual(shape==3?3:1,result.SavedCount);
            }
        }
    }
}
