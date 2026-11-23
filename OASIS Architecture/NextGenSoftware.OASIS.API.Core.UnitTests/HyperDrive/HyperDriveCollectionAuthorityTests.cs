using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData("LoadAllHolons","sync","SA")]
    [InlineData("LoadHolonsByMetaData","async","SLP")]
    [InlineData("LoadHolonsByMetaDataPairs","load-balance","student-service")]
    public async Task CollectionQueryNeverAcceptsPartialReplicaAndPreservesActorScope(string operation,string mode,string actorLabel)
    {
        var actor=Guid.NewGuid();
        var request=new StorageOperationRequest {
            Operation=operation,AvatarId=actor,IncludePublic=true,LoadChildren=false,Recursive=false,
            MetaKey="type",MetaValue="district",MetaKeyValuePairs=new(){{"type","district"}}};
        var visible=new IHolon[]{
            new Holon{Id=Guid.NewGuid(),Name=actorLabel+" owned",CreatedByAvatarId=actor},
            new Holon{Id=Guid.NewGuid(),Name="visible public",IsPublic=true}};
        var primary=CreateActiveProvider(ProviderType.MongoDBOASIS,"collection-authority");
        var replica=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"incomplete-collection-replica");
        SetupScopedCollection(primary,request,new OASISResult<IEnumerable<IHolon>>(visible));
        SetupScopedCollection(replica,request,new OASISResult<IEnumerable<IHolon>>(visible.Take(1).ToArray()));
        var router=CollectionAuthorityRouter(primary,replica);
        var result=mode=="sync"?router.RouteRequest<IEnumerable<IHolon>>(request):
            mode=="async"?await router.RouteRequestAsync<IEnumerable<IHolon>>(request):
            await router.LoadBalanceRequestAsync<IEnumerable<IHolon>>(request);

        result.IsError.Should().BeFalse(result.Message);
        result.Result.Select(row=>row.Id).Should().Equal(visible.Select(row=>row.Id));
        primary.Invocations.Count(call=>call.Method.Name.StartsWith("Load",StringComparison.Ordinal)).Should().Be(1,
            "the actor-aware authoritative overload must receive the exact scope once");
        replica.Invocations.Where(call=>call.Method.Name.StartsWith("Load",StringComparison.Ordinal)).Should().BeEmpty(
            "a nonempty subset is not proof of complete collection membership");
        request.AvatarId.Should().Be(actor);request.IncludePublic.Should().BeTrue();
    }

    [Fact]
    public async Task CollectionAuthorityFailureCannotFailOverToAPartialReplica()
    {
        var request=new StorageOperationRequest {Operation="LoadAllHolons",AvatarId=Guid.NewGuid(),
            IncludePublic=true,LoadChildren=false,Recursive=false};
        var primary=CreateActiveProvider(ProviderType.MongoDBOASIS,"unavailable-collection-authority");
        var replica=CreateActiveProvider(ProviderType.SQLLiteDBOASIS,"partial-failover-collection");
        SetupScopedCollection(primary,request,new OASISResult<IEnumerable<IHolon>>{
            IsError=true,ErrorCode="CONTROLLED_COLLECTION_OUTAGE",Message="Authority unavailable",
            Exception=new IOException("controlled isolated authority outage")});
        SetupScopedCollection(replica,request,new OASISResult<IEnumerable<IHolon>>(new IHolon[]{new Holon{Id=Guid.NewGuid()}}));
        var router=CollectionAuthorityRouter(primary,replica);
        var result=await router.FailoverRequestAsync<IEnumerable<IHolon>>(request);
        result.IsError.Should().BeTrue();
        result.Result.Should().BeNull();
        primary.Invocations.Count(call=>call.Method.Name.StartsWith("Load",StringComparison.Ordinal)).Should().Be(1);
        replica.Invocations.Where(call=>call.Method.Name.StartsWith("Load",StringComparison.Ordinal)).Should().BeEmpty();
    }

    private static OASISHyperDrive CollectionAuthorityRouter(Mock<IOASISStorageProvider> primary,Mock<IOASISStorageProvider> replica)
    {
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){
            IsAutoFailOverEnabled=true,IsAutoLoadBalanceEnabled=true,IsAutoReplicationEnabled=false};
        foreach(var provider in new[]{primary,replica}) {
            provider.Setup(value=>value.ActivateProvider()).Returns(new OASISResult<bool>(true));
            provider.Setup(value=>value.ActivateProviderAsync()).ReturnsAsync(new OASISResult<bool>(true));
            manager.RegisterProvider(provider.Object).Should().BeTrue();
        }
        manager.SetAndActivateCurrentStorageProvider(primary.Object).IsError.Should().BeFalse();
        manager.AddProvidersToAutoLoadBalanceList(new[]{ProviderType.SQLLiteDBOASIS});
        manager.SetAndReplaceAutoFailOverListForProviders(new[]{
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.SQLLiteDBOASIS)}).IsError.Should().BeFalse();
        return new OASISHyperDrive(manager);
    }

    private static void SetupScopedCollection(Mock<IOASISStorageProvider> provider,StorageOperationRequest request,
        OASISResult<IEnumerable<IHolon>> result)
    {
        var actor=request.AvatarId;var match=request.MetaKeyValuePairMatchMode;
        if(request.Operation=="LoadAllHolons") {
            provider.Setup(value=>value.LoadAllHolons(actor,true,HolonType.All,false,false,0,0,true,false,0)).Returns(result);
            provider.Setup(value=>value.LoadAllHolonsAsync(actor,true,HolonType.All,false,false,0,0,true,false,0)).ReturnsAsync(result);
        } else if(request.Operation=="LoadHolonsByMetaData") {
            provider.Setup(value=>value.LoadHolonsByMetaData("type","district",actor,true,HolonType.All,false,false,0,0,true,false,0)).Returns(result);
            provider.Setup(value=>value.LoadHolonsByMetaDataAsync("type","district",actor,true,HolonType.All,false,false,0,0,true,false,0)).ReturnsAsync(result);
        } else {
            provider.Setup(value=>value.LoadHolonsByMetaData(It.Is<Dictionary<string,string>>(pairs=>pairs.Count==1&&pairs["type"]=="district"),
                match,actor,true,HolonType.All,false,false,0,0,true,false,0)).Returns(result);
            provider.Setup(value=>value.LoadHolonsByMetaDataAsync(It.Is<Dictionary<string,string>>(pairs=>pairs.Count==1&&pairs["type"]=="district"),
                match,actor,true,HolonType.All,false,false,0,0,true,false,0)).ReturnsAsync(result);
        }
    }
}
