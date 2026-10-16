using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.HyperDrive.IntegrationTests;

public sealed partial class MongoSqliteRealProviderRoutingTests
{
    private static void AssertInventoryMedia(IInventoryItem actual,IInventoryItem expected)
    {
        actual.Id.Should().Be(expected.Id);
        actual.Quantity.Should().Be(expected.Quantity);
        actual.Image2D.Should().BeEquivalentTo(expected.Image2D,options=>options.WithStrictOrdering());
        actual.Object3D.Should().BeEquivalentTo(expected.Object3D,options=>options.WithStrictOrdering());
        (actual.Image2DURI?.OriginalString).Should().Be(expected.Image2DURI?.OriginalString);
        (actual.Object3DURI?.OriginalString).Should().Be(expected.Object3DURI?.OriginalString);
    }
}
