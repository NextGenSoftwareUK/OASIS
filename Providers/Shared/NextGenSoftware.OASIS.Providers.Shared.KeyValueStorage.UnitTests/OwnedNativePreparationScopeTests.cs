using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class OwnedNativePreparationScopeTests
{
    private static JsonElement Evidence()=>JsonSerializer.SerializeToElement(new{transactionHash="owned"});
    [TestMethod]
    public async Task OrdinaryUnscopedInvocationDoesNotBuildUnusedEvidence()
    {
        var scope=new OwnedNativePreparationScope();var built=0;
        var permit=await scope.PrepareCurrentAsync(()=>{built++;return Evidence();});
        Assert.AreEqual(9,await permit.SubmitAsync(()=>Task.FromResult(9)));Assert.AreEqual(0,built);
    }
    [TestMethod]
    public async Task EachPhysicalMutationRequiresItsOwnPreparationAndPermission()
    {
        var scope=new OwnedNativePreparationScope();var persisted=0;var sends=0;
        await scope.RunAsync(async()=>{
            for(var index=0;index<2;index++){
                var permit=await scope.PrepareCurrentAsync(JsonSerializer.SerializeToElement(new{transactionHash="owned-"+index}));
                Assert.AreEqual(index+1,persisted);await permit.SubmitAsync(()=>Task.FromResult(++sends));
            }
            return true;
        },(_,_)=>{persisted++;return Task.CompletedTask;});
        Assert.AreEqual(2,sends);Assert.AreEqual(2,persisted);
    }
    [TestMethod]
    public async Task PersistencePrecedesSingleUseNativeInvocation()
    {
        var scope=new OwnedNativePreparationScope();var persisted=0;var sends=0;
        await scope.RunAsync(async()=>{
            var permit=await scope.PrepareCurrentAsync(Evidence());Assert.AreEqual(1,persisted);
            Assert.AreEqual(7,await permit.SubmitAsync(()=>{sends++;return Task.FromResult(7);}));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>permit.SubmitAsync(()=>{sends++;return Task.FromResult(8);}));
            return true;
        },(_,_)=>{persisted++;return Task.CompletedTask;});
        Assert.AreEqual(1,sends);Assert.IsFalse(scope.HasCurrent);
    }
    [TestMethod]
    public async Task FailedPersistenceNeverProducesSubmissionPermission()
    {
        var scope=new OwnedNativePreparationScope();var cause=new IOException("owned persistence failure");var sends=0;
        var error=await Assert.ThrowsExceptionAsync<IOException>(()=>scope.RunAsync(async()=>{
            var permit=await scope.PrepareCurrentAsync(Evidence());return await permit.SubmitAsync(()=>{sends++;return Task.FromResult(1);});
        },(_,_)=>Task.FromException(cause)));
        Assert.AreSame(cause,error);Assert.AreEqual(0,sends);Assert.IsFalse(scope.HasCurrent);
    }
    [TestMethod]
    public async Task CancellationAfterPersistencePreventsSubmission()
    {
        var scope=new OwnedNativePreparationScope();using var cancellation=new CancellationTokenSource();var sends=0;
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>scope.RunAsync(async()=>{
            var permit=await scope.PrepareCurrentAsync(Evidence());return await permit.SubmitAsync(()=>{sends++;return Task.FromResult(1);});
        },(_,_)=>{cancellation.Cancel();return Task.CompletedTask;},cancellation.Token));
        Assert.AreEqual(0,sends);
    }
    [TestMethod]
    public async Task NestedOwnersAreRejectedWithoutLeakingScope()
    {
        var scope=new OwnedNativePreparationScope();
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>scope.RunAsync(()=>scope.RunAsync(()=>Task.FromResult(true),(_,_)=>Task.CompletedTask),(_,_)=>Task.CompletedTask));
        Assert.IsFalse(scope.HasCurrent);
    }
    [TestMethod]
    public async Task EscapedPermissionCannotSubmitAfterOwnerCompletion()
    {
        var scope=new OwnedNativePreparationScope();OwnedNativePreparationScope.PreparedInvocation retained=null;var sends=0;
        await scope.RunAsync(async()=>{retained=await scope.PrepareCurrentAsync(Evidence());return true;},(_,_)=>Task.CompletedTask);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>retained.SubmitAsync(()=>{sends++;return Task.FromResult(1);}));
        Assert.AreEqual(0,sends);
    }
    [TestMethod]
    public async Task DetachedInheritedOwnerClosesWhilePersistenceIsAwaited()
    {
        var scope=new OwnedNativePreparationScope();var entered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var release=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);Task<int> detached=null;var sends=0;
        await scope.RunAsync(async()=>{
            detached=Task.Run(async()=>{var permit=await scope.PrepareCurrentAsync(Evidence());return await permit.SubmitAsync(()=>{sends++;return Task.FromResult(1);});});
            await entered.Task;return true;
        },async(_,_)=>{entered.SetResult(true);await release.Task;});
        release.SetResult(true);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(async()=>{await detached;});Assert.AreEqual(0,sends);
    }
    [TestMethod]
    public async Task PermissionCannotTransferIntoAnotherOwner()
    {
        var scope=new OwnedNativePreparationScope();var outside=await scope.PrepareCurrentAsync(Evidence());var sends=0;
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>scope.RunAsync(()=>outside.SubmitAsync(()=>{sends++;return Task.FromResult(1);}),(_,_)=>Task.CompletedTask));
        Assert.AreEqual(0,sends);
    }
}
