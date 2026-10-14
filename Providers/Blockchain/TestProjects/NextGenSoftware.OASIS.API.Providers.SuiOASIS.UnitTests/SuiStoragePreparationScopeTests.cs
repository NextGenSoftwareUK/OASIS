using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SuiOASIS.UnitTests;

[TestClass]
public class SuiStoragePreparationScopeTests
{
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
