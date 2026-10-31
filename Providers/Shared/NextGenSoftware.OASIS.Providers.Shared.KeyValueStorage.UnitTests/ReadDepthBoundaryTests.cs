using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class ReadDepthBoundaryTests
{
    private sealed class NoAccessBackend : IKeyValueBackend
    {
        public int Calls;
        private Exception Access() { Calls++; return new InvalidOperationException("Invalid depth must not access storage."); }
        public Task<string> GetAsync(string key,CancellationToken cancellationToken=default)=>throw Access();
        public Task PutAsync(string key,string value,CancellationToken cancellationToken=default)=>throw Access();
        public Task DeleteAsync(string key,CancellationToken cancellationToken=default)=>throw Access();
        public Task<IReadOnlyList<string>> ListKeysAsync(string prefix,CancellationToken cancellationToken=default)=>throw Access();
        public Task VerifyAsync(CancellationToken cancellationToken=default)=>throw Access();
    }
    [DataTestMethod]
    [DataRow(false)][DataRow(true)]
    public async Task MaximumInitialDepthDoesNotOverflowIntoGrandchildren(bool synchronous)
    {
        var provider=new TestProvider(new InMemoryBackend());
        var parent=Guid.NewGuid();var child=new Holon{Id=Guid.NewGuid(),ParentHolonId=parent};
        var grandchild=new Holon{Id=Guid.NewGuid(),ParentHolonId=child.Id};
        Assert.IsFalse((await provider.SaveHolonAsync(child,saveChildren:false)).IsError);
        Assert.IsFalse((await provider.SaveHolonAsync(grandchild,saveChildren:false)).IsError);
        var result=synchronous?provider.LoadHolonsForParent(parent,maxChildDepth:int.MaxValue,curentChildDepth:int.MaxValue)
            :await provider.LoadHolonsForParentAsync(parent,maxChildDepth:int.MaxValue,curentChildDepth:int.MaxValue);
        Assert.IsFalse(result.IsError,result.Message);
        Assert.AreEqual(0,result.Result.Single().Children.Count());
    }

    [DataTestMethod]
    [DataRow(0)][DataRow(1)][DataRow(2)][DataRow(3)]
    public async Task InvalidDepthIsRejectedEvenForEmptyStorage(int route)
    {
        var backend=new NoAccessBackend();var provider=new TestProvider(backend);
        if(route==0)Assert.IsTrue((await provider.LoadHolonAsync(Guid.NewGuid(),maxChildDepth:-1)).IsError);
        else if(route==1)Assert.IsTrue((await provider.LoadAllHolonsAsync(maxChildDepth:-1)).IsError);
        else if(route==2)Assert.IsTrue((await provider.LoadHolonsForParentAsync(Guid.NewGuid(),curentChildDepth:-1)).IsError);
        else Assert.IsTrue((await provider.LoadAllHolonsAsync(curentChildDepth:-1)).IsError);
        Assert.AreEqual(0,backend.Calls,"Admission must precede backend reads, not merely return an unrelated storage error.");
    }
}
