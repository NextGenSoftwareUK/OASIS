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
    [InlineData(false,false)] [InlineData(true,false)] [InlineData(false,true)] [InlineData(true,true)]
    public async Task ReusedImportRequestRefreshesCompletedDataButCannotReplayUncertainMutation(bool synchronous,bool uncertain)
    {
        var primary=CreateActiveProvider();var names=new List<string>();var calls=0;
        OASISResult<bool> Import(IEnumerable<IHolon> rows)
        {calls++;names.Add(rows.Single().Name);return new OASISResult<bool>{Result=!uncertain};}
        primary.Setup(store=>store.Import(It.IsAny<IEnumerable<IHolon>>())).Returns((IEnumerable<IHolon> rows)=>Import(rows));
        primary.Setup(store=>store.ImportAsync(It.IsAny<IEnumerable<IHolon>>())).Returns((IEnumerable<IHolon> rows)=>Task.FromResult(Import(rows)));
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna);manager.RegisterProvider(primary.Object);var router=new OASISHyperDrive(manager);
        var source=new Holon{Name="first"};var request=new StorageOperationRequest {Operation="Import",Payload=new IHolon[]{source},PreferredProvider=ProviderType.MongoDBOASIS};
        var first=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
        source.Name="second";
        var second=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
        if(uncertain)
        {
            first.IsError.Should().BeTrue();second.IsError.Should().BeTrue();calls.Should().Be(1);source.Id.Should().BeEmpty();
            NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(second.Exception).Should().BeTrue();
        }
        else {first.Result.Should().BeTrue();second.Result.Should().BeTrue();calls.Should().Be(2);names.Should().Equal("first","second");}
    }

    [Fact]
    public async Task ExplicitImportReplicationStopsAtUnacknowledgedMutationAndRetainsCause()
    {
        var primary=CreateActiveProvider();var secondary=CreateActiveProvider(ProviderType.LocalFileOASIS,"secondary");
        primary.Setup(store=>store.ImportAsync(It.IsAny<IEnumerable<IHolon>>())).ReturnsAsync(new OASISResult<bool>{Result=false});
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna);manager.RegisterProvider(primary.Object);manager.RegisterProvider(secondary.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)});
        var source=new Holon();var router=new OASISHyperDrive(manager);
        var result=await router.ReplicateRequestAsync<bool>(new StorageOperationRequest {Operation="Import",Payload=new IHolon[]{source}});
        result.IsError.Should().BeTrue();NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeTrue();
        source.Id.Should().BeEmpty();secondary.Invocations.Should().NotContain(call=>call.Method.Name=="ImportAsync");
        router.LastReplicationDiagnostic.Attempts.Should().ContainSingle();
    }

    public static IEnumerable<object[]> ImportPublicationModes()
    {
        foreach(var mode in new[]{"success","failure","false","changed-id","caller-edit"})
        foreach(var sync in new[]{false,true}) yield return new object[]{mode,sync};
    }

    [Theory]
    [MemberData(nameof(ImportPublicationModes))]
    public async Task ImportPublishesOnlyAcknowledgedStableDetachedIdentities(string mode,bool synchronous)
    {
        var root=new Holon {Name="owned import"};var child=new Holon {Name="owned child"};root.Children.Add(child);
        var primary=CreateActiveProvider();var secondary=CreateActiveProvider(ProviderType.LocalFileOASIS,"secondary");
        var primaryCalls=0;var secondaryCalls=0;var sourceWasEmpty=false;var primaryWasDetached=false;
        List<Guid>? primaryIds=null;List<Guid>? secondaryIds=null;string? secondaryName=null;
        OASISResult<bool> Import(IEnumerable<IHolon> input,bool first)
        {
            var rows=input.ToList();
            if(first)
            {
                primaryCalls++;
                if(primaryCalls==1)
                {
                    sourceWasEmpty=root.Id==Guid.Empty && child.Id==Guid.Empty;
                    primaryWasDetached=rows.Count==2 && !ReferenceEquals(rows[0],root) && rows.All(row=>row.Children.Count==0) && rows[1].ParentHolonId==rows[0].Id;
                    primaryIds=rows.Select(row=>row.Id).ToList();
                }
                rows[0].Name="provider mutation";
                if(mode=="failure") return new OASISResult<bool>{IsError=true,Exception=new System.IO.IOException("owned precommit refusal"),Message="owned precommit refusal"};
                if(mode=="false") return new OASISResult<bool>{Result=false};
                if(mode=="changed-id") rows[1].Id=Guid.NewGuid();
                if(mode=="caller-edit") child.Id=Guid.NewGuid();
            }
            else {secondaryCalls++;secondaryIds=rows.Select(row=>row.Id).ToList();secondaryName=rows[0].Name;}
            return new OASISResult<bool>{Result=true,IsSaved=true};
        }
        primary.Setup(store=>store.Import(It.IsAny<IEnumerable<IHolon>>())).Returns((IEnumerable<IHolon> rows)=>Import(rows,true));
        primary.Setup(store=>store.ImportAsync(It.IsAny<IEnumerable<IHolon>>())).Returns((IEnumerable<IHolon> rows)=>Task.FromResult(Import(rows,true)));
        secondary.Setup(store=>store.Import(It.IsAny<IEnumerable<IHolon>>())).Returns((IEnumerable<IHolon> rows)=>Import(rows,false));
        secondary.Setup(store=>store.ImportAsync(It.IsAny<IEnumerable<IHolon>>())).Returns((IEnumerable<IHolon> rows)=>Task.FromResult(Import(rows,false)));
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoFailOverEnabled=true,IsAutoReplicationEnabled=true};
        manager.RegisterProvider(primary.Object);manager.RegisterProvider(secondary.Object);
        var types=new[]{new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS)};
        manager.SetAndReplaceAutoFailOverListForProviders(types);manager.SetAndReplaceAutoReplicationListForProviders(types);
        var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest {Operation="Import",Payload=new IHolon[]{root},PreferredProvider=ProviderType.MongoDBOASIS};
        var result=synchronous ? router.RouteRequest<bool>(request):await router.RouteRequestAsync<bool>(request);
        sourceWasEmpty.Should().BeTrue();primaryWasDetached.Should().BeTrue();primaryCalls.Should().Be(mode=="failure" ? 2:1);
        if(mode=="success" || mode=="failure")
        {
            result.IsError.Should().BeFalse(result.Message);result.Result.Should().BeTrue();secondaryCalls.Should().Be(1);
            result.IsWarning.Should().Be(mode=="failure","existing replication retries the original failed store after successful failover and reports its refusal as a warning");
            secondaryIds.Should().Equal(primaryIds!);secondaryName.Should().Be("owned import");
            root.Id.Should().Be(primaryIds![0]);child.Id.Should().Be(primaryIds[1]);child.ParentHolonId.Should().Be(root.Id);
        }
        else
        {
            result.IsError.Should().BeTrue();result.Result.Should().BeFalse();secondaryCalls.Should().Be(0);
            NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeTrue();root.Id.Should().BeEmpty();
            var evidence=Newtonsoft.Json.Linq.JObject.Parse(result.MetaData["HyperDrive.ImportReconciliation"]);
            evidence["Provider"]!.ToObject<string>().Should().Be(nameof(ProviderType.MongoDBOASIS));
            evidence["HolonIds"]!.ToObject<List<Guid>>().Should().Equal(primaryIds!);
            if(mode!="caller-edit") child.Id.Should().BeEmpty();
        }
        root.Name.Should().Be("owned import");root.Children.Single().Should().BeSameAs(child);
    }
}
