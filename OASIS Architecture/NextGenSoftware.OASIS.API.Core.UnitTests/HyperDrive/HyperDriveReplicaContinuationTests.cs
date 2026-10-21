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
    [InlineData(false, false)] [InlineData(true, false)]
    [InlineData(false, true)] [InlineData(true, true)]
    public async Task OrdinaryReplicaCapturesDeepCommittedGraphWithoutSharingProviderMutations(bool synchronous, bool emptyDescendantIdentity)
    {
        const int count = 4096;
        var primary = CreateActiveProvider();
        var replica = CreateActiveProvider(ProviderType.IPFSOASIS, "deep-replica");
        var root = new Holon { Id = Guid.NewGuid(), Name = "committed" };
        IHolon tail = root;
        for (var index = 1; index < count; index++)
        {
            var child = new Holon { Id = Guid.NewGuid(), ParentHolonId = tail.Id };
            tail.Children.Add(child);
            tail = child;
        }
        if (emptyDescendantIdentity) tail.Id = Guid.Empty;
        primary.Setup(store => store.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns(new OASISResult<IHolon>(root) { IsSaved = true });
        primary.Setup(store => store.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .ReturnsAsync(new OASISResult<IHolon>(root) { IsSaved = true });
        var calls = 0;
        OASISResult<IHolon> Receive(IHolon record)
        {
            calls++;
            record.Should().NotBeSameAs(root);
            IHolon current = record;
            var seen = 1;
            while (current.Children.Count != 0)
            {
                var child = current.Children.Single();
                child.ParentHolonId.Should().Be(current.Id);
                current = child;
                seen++;
            }
            seen.Should().Be(count);
            current.Id.Should().Be(tail.Id);
            record.Name = "replica mutation";
            record.Children.Clear();
            return new OASISResult<IHolon>(record) { IsSaved = true };
        }
        replica.Setup(store => store.SaveHolon(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon row, bool a, bool b, int c, bool d, bool e) => Receive(row));
        replica.Setup(store => store.SaveHolonAsync(It.IsAny<IHolon>(), true, true, 0, true, false))
            .Returns((IHolon row, bool a, bool b, int c, bool d, bool e) => Task.FromResult(Receive(row)));
        var dna = CreateDna(HyperDriveModes.V2); dna.OASIS.SubscriptionConfig.PlanType = "Gold";
        var manager = new ProviderManager(null, dna) { IsAutoReplicationEnabled = true };
        manager.RegisterProvider(primary.Object); manager.RegisterProvider(replica.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[] {
            new EnumValue<ProviderType>(ProviderType.MongoDBOASIS), new EnumValue<ProviderType>(ProviderType.IPFSOASIS) });
        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest { Operation = "SaveHolon", Payload = root,
            SaveChildren = true, Recursive = true, PreferredProvider = ProviderType.MongoDBOASIS };
        var result = synchronous ? router.RouteRequest<IHolon>(request) : await router.RouteRequestAsync<IHolon>(request);
        if (emptyDescendantIdentity)
        {
            result.IsError.Should().BeTrue(); result.IsSaved.Should().BeFalse();
            NextGenSoftware.OASIS.API.Core.Helpers.NativeMutationReconciliation.IsRequired(result.Exception).Should().BeTrue();
            calls.Should().Be(0);
            tail.Id.Should().BeEmpty();
        }
        else
        {
            result.IsError.Should().BeFalse(); result.IsSaved.Should().BeTrue();
            calls.Should().Be(1);
        }
        root.Name.Should().Be("committed"); root.Children.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task OrdinaryReplicaFailureDoesNotPreventLaterReplicaSubmission(bool synchronous)
    {
        var primary=CreateActiveProvider();var failure=CreateActiveProvider(ProviderType.LocalFileOASIS,"ordinary-failure");
        var later=CreateActiveProvider(ProviderType.IPFSOASIS,"later");var holon=new Holon{Id=Guid.NewGuid()};
        foreach(var provider in new[]{primary,later})
        {
            provider.Setup(store=>store.SaveHolon(It.Is<IHolon>(h => h.Id == holon.Id),false,true,0,true,false)).Returns(new OASISResult<IHolon>(holon){IsSaved=true});
            provider.Setup(store=>store.SaveHolonAsync(It.Is<IHolon>(h => h.Id == holon.Id),false,true,0,true,false)).ReturnsAsync(new OASISResult<IHolon>(holon){IsSaved=true});
        }
        var refused=new OASISResult<IHolon>{IsError=true,Exception=new IOException("ordinary owned refusal"),Message="ordinary refusal"};
        failure.Setup(store=>store.SaveHolon(It.Is<IHolon>(h => h.Id == holon.Id),false,true,0,true,false)).Returns(refused);
        failure.Setup(store=>store.SaveHolonAsync(It.Is<IHolon>(h => h.Id == holon.Id),false,true,0,true,false)).ReturnsAsync(refused);
        var dna=CreateDna(HyperDriveModes.V2);dna.OASIS.SubscriptionConfig.PlanType="Gold";
        var manager=new ProviderManager(null,dna){IsAutoReplicationEnabled=true};
        foreach(var provider in new[]{primary,failure,later}) manager.RegisterProvider(provider.Object);
        manager.SetAndReplaceAutoReplicationListForProviders(new[]{new EnumValue<ProviderType>(ProviderType.MongoDBOASIS),new EnumValue<ProviderType>(ProviderType.LocalFileOASIS),new EnumValue<ProviderType>(ProviderType.IPFSOASIS)});
        var router=new OASISHyperDrive(manager);var request=new StorageOperationRequest{Operation="SaveHolon",Payload=holon,SaveChildren=false,PreferredProvider=ProviderType.MongoDBOASIS};
        var result=synchronous ? router.RouteRequest<IHolon>(request):await router.RouteRequestAsync<IHolon>(request);
        result.IsError.Should().BeFalse();result.IsSaved.Should().BeTrue();result.IsWarning.Should().BeTrue();
        router.LastReplicationDiagnostic.Attempts.Should().HaveCount(2);router.LastReplicationDiagnostic.Attempts.Last().Succeeded.Should().BeTrue();
        if(synchronous) later.Verify(store=>store.SaveHolon(It.Is<IHolon>(h => h.Id == holon.Id),false,true,0,true,false),Times.Once);
        else later.Verify(store=>store.SaveHolonAsync(It.Is<IHolon>(h => h.Id == holon.Id),false,true,0,true,false),Times.Once);
    }
}
