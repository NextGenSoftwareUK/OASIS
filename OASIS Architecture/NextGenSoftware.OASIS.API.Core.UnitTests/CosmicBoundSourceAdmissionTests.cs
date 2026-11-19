using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task BoundSourceCannotDowngradeToAnAtomicProviderWithoutNativeBindingCapability(bool asynchronous)
    {
        await Task.Run(async ()=>{
            var fixture=Fixture(true,conditional:true,atomicReplication:true);
            var root=new Holon{Name="still pending"};var actor=Guid.NewGuid();
            var expected=new CosmicStorageIdentity(ProviderType.SQLLiteDBOASIS,new string('A',64),Guid.NewGuid());
            var options=new CosmicWriteOptions(Guid.NewGuid(),expectedRevisions:null,replicationEnrollment:
                new HolonReplicationEnrollment("bound-source",new[]{"target"},expected));
            var result=asynchronous
                ? await fixture.Manager.SaveHolonAsync(root,actor,options,saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS)
                : fixture.Manager.SaveHolon(root,actor,options,saveChildren:false,providerType:ProviderType.SQLLiteDBOASIS);
            result.IsError.Should().BeTrue();result.ErrorCode.Should().Be("COSMIC_ATOMIC_REPLICATION_UNSUPPORTED");
            root.Id.Should().BeEmpty();root.Name.Should().Be("still pending");
            fixture.Provider.Invocations.Should().NotContain(call=>call.Method.Name.StartsWith("ExecuteReplicating")
                || call.Method.Name.StartsWith("SaveHolon") || call.Method.Name.StartsWith("ExecuteConditional"));
        });
    }
}
