using System.Threading;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Objects;

namespace NextGenSoftware.OASIS.API.Providers.AWSOASIS.IntegrationTests;

public partial class AWSOASISIntegrationTests
{
    private sealed class CommitThenFailClient : AmazonDynamoDBClient
    {
        public required AmazonDynamoDBClient Healthy;
        public readonly IOException Cause=new("owned SDK acknowledgement boundary failure after real transaction commit");
        public int Commits;
        public CommitThenFailClient():base(new BasicAWSCredentials("fakeMyKeyId","fakeSecretAccessKey"),
            new AmazonDynamoDBConfig {ServiceURL="http://127.0.0.1:18000",AuthenticationRegion="us-east-1",UseHttp=true}) { }
        public override Task<GetItemResponse> GetItemAsync(GetItemRequest request,CancellationToken cancellationToken=default)
            =>Healthy.GetItemAsync(request,cancellationToken);
        public override async Task<TransactWriteItemsResponse> TransactWriteItemsAsync(TransactWriteItemsRequest request,CancellationToken cancellationToken=default)
        {
            await Healthy.TransactWriteItemsAsync(request,cancellationToken);Commits++;
            throw Cause;
        }
    }

    [DataTestMethod]
    [DataRow(false,false)] [DataRow(true,false)] [DataRow(false,true)] [DataRow(true,true)]
    public async Task CommittedLifecycleWithoutAckCannotFailOverOrReplicate(bool synchronous,bool softDelete)
    {
        var path=Path.GetFullPath("TestResults/AWSRoutingFiles/uncertain-routing.db");
        if(File.Exists(path)) Assert.Fail("Retained acknowledgement evidence exists; refusing overwrite.");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var sqlite=new NextGenSoftware.OASIS.API.Providers.SQLLiteDBOASIS.SQLLiteDBOASIS($"Data Source={path};Pooling=False");
        var verified=false;var healthy=Client;var id=Guid.NewGuid();
        try
        {
            Assert.IsFalse((await sqlite.ActivateProviderAsync()).IsError);
            await SeedPair(id);
            Assert.IsFalse((await sqlite.SaveAvatarAsync(new Avatar {Id=id,Username="owned-uncertain",IsActive=true,
                AvatarType=new NextGenSoftware.Utilities.EnumValue<Core.Enums.AvatarType>(Core.Enums.AvatarType.User)})).IsError);
            Assert.IsFalse((await sqlite.SaveAvatarDetailAsync(new AvatarDetail {Id=id,Username="owned-uncertain",IsActive=true,XP=777})).IsError);
            var dna=new NextGenSoftware.OASIS.API.DNA.OASISDNA {OASIS=new NextGenSoftware.OASIS.API.DNA.OASIS {
                HyperDriveMode=NextGenSoftware.OASIS.API.DNA.HyperDriveModes.V2,StorageProviders=new(),SubscriptionConfig=new() {PlanType="Gold"}}};
            var manager=new Core.Managers.ProviderManager(null,dna) {IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
            manager.RegisterProvider(_provider);manager.RegisterProvider(sqlite);
            var types=new[] {new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.AWSOASIS),
                new NextGenSoftware.Utilities.EnumValue<Core.Enums.ProviderType>(Core.Enums.ProviderType.SQLLiteDBOASIS)};
            Assert.IsFalse(manager.SetAndReplaceAutoFailOverListForProviders(types).IsError);
            Assert.IsFalse(manager.SetAndReplaceAutoReplicationListForProviders(types).IsError);
            var router=new Core.Managers.OASISHyperDrive.OASISHyperDrive(manager);
            var before=manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0;
            using var fault=new CommitThenFailClient {Healthy=healthy};ClientField.SetValue(_provider,fault);
            NextGenSoftware.OASIS.Common.OASISResult<bool> result;
            try
            {
                var request=new Core.StorageOperationRequest {Operation="DeleteAvatar",AvatarId=id,SoftDelete=softDelete,PreferredProvider=Core.Enums.ProviderType.AWSOASIS};
                result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
            }
            finally {ClientField.SetValue(_provider,healthy);}
            Assert.IsTrue(result.IsError);Assert.IsFalse(result.IsSaved);Assert.AreEqual(1,fault.Commits);
            Assert.IsInstanceOfType(result.Exception,typeof(INativeMutationReconciliationRequired));
            Assert.AreSame(fault.Cause,result.Exception.GetBaseException());
            Assert.IsTrue(string.IsNullOrEmpty(((INativeMutationReconciliationRequired)result.Exception).OriginalTransactionId));
            Assert.IsFalse(AvatarTombstoneEvidence.TryGet(result,out _));
            Assert.AreEqual(before,manager.PerformanceMonitor.GetMetrics(Core.Enums.ProviderType.SQLLiteDBOASIS)?.TotalRequests ?? 0);
            Assert.IsTrue((await sqlite.LoadAvatarAsync(id)).Result.IsActive);
            var untouched=(await sqlite.LoadAvatarDetailAsync(id)).Result;Assert.IsTrue(untouched.IsActive);Assert.AreEqual(777L,untouched.XP);
            if(softDelete)
            {
                var pair=await ReadPair(id);Assert.IsFalse(pair.Profile.IsActive);Assert.IsFalse(pair.Detail.IsActive);
                Assert.AreEqual(pair.Profile.DeletedDate,pair.Detail.DeletedDate);Assert.AreEqual(321L,pair.Detail.XP);
                Assert.IsFalse((await _provider.DeleteAvatarAsync(id,false)).IsError);
            }
            else foreach(var table in new[] {"OasisAvatars","OasisAvatarDetails"})
                Assert.AreEqual(0,(await healthy.GetItemAsync(new GetItemRequest {TableName=table,Key=Key(id),ConsistentRead=true})).Item.Count);
            Assert.IsFalse((await sqlite.DeleteAvatarAsync(id,false)).IsError);
            verified=true;
        }
        finally
        {
            ClientField.SetValue(_provider,healthy);await sqlite.DeActivateProviderAsync();
            if(verified) foreach(var file in new[] {path,path+"-wal",path+"-shm"}) if(File.Exists(file)) File.Delete(file);
        }
    }
}
