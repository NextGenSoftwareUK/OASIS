using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public class SuiStoragePreparationScopeTests
{
    [TestMethod]
    public async Task ExplicitPreparationCannotReplaceCurrentOperationOwner()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(SuiOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiSdkClient")!;
        using var client=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{"http://127.0.0.1:1","localnet","chain","key","package","store","currency"},null)!;
        using var explicitOwner=new PreparedSubmissionServer((_,_)=>throw new AssertFailedException("Conflicting owner must not persist."));
        Func<Task<int>> invoke=()=>{
            var failure=Assert.ThrowsException<System.Reflection.TargetInvocationException>(()=>type.GetMethod("InvokePreparedAsync",flags)!
                .Invoke(client,new object[]{"nftMint",new{},explicitOwner,CancellationToken.None}));
            Assert.IsInstanceOfType<InvalidOperationException>(failure.InnerException);return Task.FromResult(1);
        };
        Func<PreparedSubmissionServer> owner=()=>throw new AssertFailedException("No SDK process may start.");
        var scoped=(Task<int>)type.GetMethod("WithStoragePreparationAndCancellationAsync",flags)!.MakeGenericMethod(typeof(int))
            .Invoke(client,new object[]{invoke,owner,CancellationToken.None})!;
        Assert.AreEqual(1,await scoped);
    }

    [TestMethod]
    public async Task EveryPublicSdkMutationUsesCurrentOwnerBeforeProcessLaunch()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(SuiOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiSdkClient")!;
        using var client=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{"http://127.0.0.1:1","localnet","chain","key","package","store","currency"},null)!;
        foreach(var operation in new[]{"put","delete","putMany","deleteMany","publish","publicationResume","transfer",
            "nftMint","nftSend","nftBurn","nftLock","nftUnlock","tokenMint","tokenSend","tokenBurn","tokenLock","tokenUnlock",
            "tokenEscrowLock","tokenEscrowRelease"}){
            var cause=new InvalidOperationException("Owned preparation rejection before SDK launch.");var calls=0;
            Func<Task<int>> invoke=async()=>{
                await (Task<System.Text.Json.JsonElement>)type.GetMethod("InvokeAsync",flags)!
                    .Invoke(client,new object[]{operation,null!,CancellationToken.None})!;return 1;
            };
            Func<PreparedSubmissionServer> owner=()=>{calls++;throw cause;};
            var scoped=(Task<int>)type.GetMethod("WithStoragePreparationAndCancellationAsync",flags)!.MakeGenericMethod(typeof(int))
                .Invoke(client,new object[]{invoke,owner,CancellationToken.None})!;
            var failure=await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>scoped);
            Assert.AreSame(cause,failure);Assert.AreEqual(1,calls,operation);
        }
    }

    [TestMethod]
    public async Task AdmittedCancellationReachesDefaultTokenSdkRead()
    {
        var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var type=typeof(SuiOASIS).Assembly.GetType("NextGenSoftware.OASIS.API.Providers.SuiOASIS.SuiSdkClient")!;
        using var client=(IDisposable)Activator.CreateInstance(type,flags,null,new object[]{"http://127.0.0.1:1","localnet","chain","key","package","store","currency"},null)!;
        using var cancellation=new CancellationTokenSource();
        Func<Task<int>> read=async()=>{
            cancellation.Cancel();
            await (Task<System.Text.Json.JsonElement>)type.GetMethod("InvokeAsync",flags)!
                .Invoke(client,new object[]{"get",new {key="owned"},CancellationToken.None})!;
            return 1;
        };
        Func<PreparedSubmissionServer> factory=()=>throw new AssertFailedException("Read must not create a mutation gate.");
        var scoped=(Task<int>)type.GetMethod("WithStoragePreparationAndCancellationAsync",flags)!.MakeGenericMethod(typeof(int))
            .Invoke(client,new object[]{read,factory,cancellation.Token})!;
        await Assert.ThrowsExceptionAsync<OperationCanceledException>(()=>scoped);
    }

    [TestMethod]
    public async Task ClosingOwnerWaitsForInProgressGateFactory()
    {
        var scope=new OwnedPreparationScope();
        var factoryEntered=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var bodyReturned=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release=new ManualResetEventSlim();Task? write=null;
        var owner=Task.Run(()=>scope.RunAsync(async()=>{
            write=Task.Run(()=>{using var gate=scope.CreateCurrent();Assert.IsNotNull(gate);});
            await factoryEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));bodyReturned.SetResult(true);return true;
        },()=>{
            factoryEntered.SetResult(true);
            if(!release.Wait(TimeSpan.FromSeconds(5)))throw new TimeoutException("Controlled gate factory release missing.");
            return new PreparedSubmissionServer((_,_)=>Task.CompletedTask);
        }));
        try{
            await bodyReturned.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.AreNotSame(owner,await Task.WhenAny(owner,Task.Delay(100)),"Owner closure must not finish while its gate factory is in progress.");
        }finally{release.Set();await owner.WaitAsync(TimeSpan.FromSeconds(5));if(write!=null)await write.WaitAsync(TimeSpan.FromSeconds(5));}
        Assert.IsNull(scope.CreateCurrent());
    }
    [TestMethod]
    public async Task ConcurrentIndependentOwnersKeepSeparateFactories()
    {
        var scope=new OwnedPreparationScope();var ready=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var arrived=0;var observed=new System.Collections.Concurrent.ConcurrentBag<string>();
        Task Run(string owner)=>Task.Run(()=>scope.RunAsync(async()=>{
            if(Interlocked.Increment(ref arrived)==2)ready.SetResult(true);
            await ready.Task.WaitAsync(TimeSpan.FromSeconds(5));
            using var gate=scope.CreateCurrent("put",owner);Assert.IsNotNull(gate);return true;
        },(operation,key)=>{
            Assert.AreEqual("put",operation);Assert.AreEqual(owner,key);observed.Add(owner);
            return new PreparedSubmissionServer((_,_)=>Task.CompletedTask);
        }));
        await Task.WhenAll(Run("owner-a"),Run("owner-b"));
        CollectionAssert.AreEquivalent(new[]{"owner-a","owner-b"},observed.ToArray());
        Assert.IsNull(scope.CreateCurrent());
    }
    [TestMethod]
    public async Task OwnershipRejectsNestedAndInheritedLateWrites()
    {
        var scope=new OwnedPreparationScope();var released=new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Task? late=null;var created=0;
        PreparedSubmissionServer Factory(){created++;return new PreparedSubmissionServer((_,_)=>Task.CompletedTask);}
        await scope.RunAsync(async()=>{
            using var current=scope.CreateCurrent();Assert.IsNotNull(current);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>scope.RunAsync(()=>Task.FromResult(1),Factory));
            late=Task.Run(async()=>{await released.Task;using var invalid=scope.CreateCurrent();});
            return 1;
        },Factory);
        released.SetResult(true);
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>late!);
        Assert.AreEqual(1,created);Assert.IsNull(scope.CreateCurrent());
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>scope.RunAsync(()=>{
            using var invalid=scope.CreateCurrent();return Task.FromResult(1);
        },()=>null!));
    }
}
