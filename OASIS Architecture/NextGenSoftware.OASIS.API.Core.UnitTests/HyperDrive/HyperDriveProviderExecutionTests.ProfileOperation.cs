using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Managers.OASISHyperDrive;
using NextGenSoftware.OASIS.API.DNA;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests.HyperDrive;

public sealed partial class HyperDriveProviderExecutionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ApplyAvatarProfileWithoutAnImmutableCommandIsRejectedBeforeAnyProviderIsTouched(bool synchronous)
    {
        var dna = CreateDna(HyperDriveModes.V2);
        var manager = new ProviderManager(null, dna);
        var provider = CreateActiveProvider();
        manager.RegisterProvider(provider.Object);
        var router = new OASISHyperDrive(manager);
        var request = new StorageOperationRequest
        {
            Operation = "ApplyAvatarProfile", AvatarId = Guid.NewGuid(), Payload = null,
            PreferredProvider = ProviderType.MongoDBOASIS
        };

        var result = synchronous ? router.RouteRequest<bool>(request) : await router.RouteRequestAsync<bool>(request);

        result.IsError.Should().BeTrue();
        result.IsSaved.Should().BeFalse();
        provider.Invocations.Where(call => call.Method.Name.StartsWith("Save", StringComparison.Ordinal)
            || call.Method.Name.StartsWith("Delete", StringComparison.Ordinal)
            || call.Method.Name.StartsWith("Apply", StringComparison.Ordinal)).Should().BeEmpty();
    }
}
