using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifyPreparationScopeAsync(PolygonOASIS.PolygonOASIS provider,Web3CoreOASIS native)
    {
        var sdk=(Web3CoreOASIS)typeof(Web3CoreOASISBaseProvider).GetField("_web3CoreOASIS",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(provider)!;
        var id=Guid.NewGuid();var numeric=(uint)Core.Utilities.HashUtility.GetNumericHash(id.ToString());var callbacks=0;
        var cause=new IOException("owned preparation persistence failure");
        var failed=await sdk.WithNativePreparationAsync(()=>provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=241}),async evidence=>
        {
            callbacks++;
            Assert.AreEqual("31337",evidence.GetProperty("chainId").GetString());
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(numeric)));
            throw cause;
        });
        Assert.IsTrue(failed.IsError);Assert.AreSame(cause,failed.Exception);Assert.IsFalse(failed.IsSaved);
        Assert.AreEqual(1,callbacks);Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(numeric)));
        System.Text.Json.JsonElement retained=default;
        var saved=await sdk.WithNativePreparationAsync(()=>provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=242}),async evidence=>
        {
            callbacks++;retained=evidence.Clone();
            Assert.IsTrue(Web3CoreOASISBaseProvider.IsPrivateRecordAbsent(await native.GetAvatarDetailByIdAsync(numeric)));
        });
        Assert.IsFalse(saved.IsError,saved.Message);Assert.AreEqual(2,callbacks);
        Assert.AreEqual(242L,(await provider.LoadAvatarDetailAsync(id)).Result.XP);
        Assert.IsTrue(retained.GetProperty("transactionHash").GetString()!.StartsWith("0x"));
        Assert.IsTrue(Convert.FromBase64String(retained.GetProperty("signedTransactionBase64").GetString()!).Length>0);
        Assert.IsFalse((await provider.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=243})).IsError);
        Assert.AreEqual(2,callbacks,"Preparation scope must not leak into later direct operations.");
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(()=>sdk.WithNativePreparationAsync(
            ()=>sdk.WithNativePreparationAsync(()=>Task.FromResult(true),_=>Task.CompletedTask),_=>Task.CompletedTask));
        await native.DeleteAvatarDetailAsync(numeric);
    }
}
