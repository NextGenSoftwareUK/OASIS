using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests;

public partial class Web3NativeStorageTests
{
    private static async Task VerifySqliteAvatarDeleteAbsenceAsync(PolygonOASIS.PolygonOASIS polygon,SQLLiteDBOASIS.SQLLiteDBOASIS sqlite,
        Core.Managers.ProviderManager manager,Core.Managers.OASISHyperDrive.OASISHyperDrive router,Web3CoreOASIS native,
        Web4UsageExecution executor,string user,UsageEndpointPolicy policy,MongoUsageOutboxStore store)
    {
        foreach(var asynchronous in new[] {false,true})
        foreach(var selector in new[] {"DeleteAvatar","DeleteAvatarByUsername","DeleteAvatarByEmail","DeleteAvatarByProviderKey"})
        foreach(var soft in new[] {false,true})
        foreach(var missingDetail in new[] {false,true})
        {
            if(missingDetail && !soft) continue; // Hard deletion permits an existing avatar without detail.
            var id=Guid.NewGuid();
            var avatar=new Avatar {Id=id,IsActive=true,Username="absence-"+id.ToString("N"),Email=id.ToString("N")+"@example.invalid",AvatarType=new EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)};
            Assert.IsFalse((await polygon.SaveAvatarAsync(avatar)).IsError);
            Assert.IsFalse((await polygon.SaveAvatarDetailAsync(new AvatarDetail {Id=id,IsActive=true,XP=95})).IsError);
            if(missingDetail) Assert.IsFalse((await sqlite.SaveAvatarAsync(avatar)).IsError);
            var handle=await executor.BeginAsync("local-fixture-bearer",user,$"sql-avatar-absence-{asynchronous}-{selector}-{soft}-{missingDetail}",new string('a',64),"local avatar deletion absence/integrity fixture",policy);
            var context=new PreparedStorageExecution(handle,"sql-avatar-absence",new Dictionary<Core.Enums.ProviderType,string> {{Core.Enums.ProviderType.PolygonOASIS,native.SigningAddress}},new[] {Core.Enums.ProviderType.SQLLiteDBOASIS});
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0;
            var request=new Core.StorageOperationRequest {Operation=selector,AvatarId=id,Username=avatar.Username,Email=avatar.Email,ProviderKey=id.ToString(),SoftDelete=soft,PreferredProvider=Core.Enums.ProviderType.SQLLiteDBOASIS,PreparedExecution=context};
            var result=asynchronous ? await router.RouteRequestAsync<bool>(request):router.RouteRequest<bool>(request);
            Assert.IsTrue(result.IsError,result.Message);Assert.IsFalse(result.Result);Assert.IsFalse(result.IsDeleted);Assert.IsNull(result.Exception);
            Assert.AreEqual(missingDetail ? Core.Helpers.StorageLifecycleConflictException.ErrorCode:OASISResultErrorCodes.RecordNotFound,result.ErrorCode);
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.PolygonOASIS)?.TotalRequests ?? 0);
            var untouched=await polygon.LoadAvatarAsync(id);Assert.IsFalse(untouched.IsError,untouched.Message);Assert.IsTrue(untouched.Result.IsActive);
            var detail=await polygon.LoadAvatarDetailAsync(id);Assert.IsFalse(detail.IsError,detail.Message);Assert.IsTrue(detail.Result.IsActive);Assert.AreEqual(95L,detail.Result.XP);
            if(missingDetail)
            {
                var preserved=await sqlite.LoadAvatarAsync(id);Assert.IsFalse(preserved.IsError,preserved.Message);
                Assert.IsTrue(preserved.Result.IsActive);Assert.AreEqual(DateTime.MinValue,preserved.Result.DeletedDate);
            }
            var attempt=(await store.GetAsync(handle.OperationId,default)).ProviderAttempts.Single();Assert.AreEqual("SQLLiteDBOASIS",attempt.Provider);Assert.IsNull(attempt.NativePreparation);
            Assert.IsFalse((await polygon.DeleteAvatarAsync(id,false)).IsError); // Explicit owned fixture teardown after assertions.
        }
    }
}
