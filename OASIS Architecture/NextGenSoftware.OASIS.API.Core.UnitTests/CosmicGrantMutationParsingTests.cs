using System.Text.Json;
using FluentAssertions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public partial class CosmicTrackingTests
{
    public static IEnumerable<object[]> InvalidGrantMutations()
    {
        foreach (var values in CosmicGrantParsingTests.InvalidGrants())
            if (values[0] is string or JsonElement or Guid[])
                foreach (var revoke in new[] { false, true })
                    yield return new[] { (object)revoke, values[0] };
    }

    [Theory]
    [MemberData(nameof(InvalidGrantMutations))]
    public async Task SharingDoesNotSilentlyRepairMalformedExistingGrants(bool revoke, object raw)
    {
        var fixture = Fixture(true, conditional: true);
        var owner = Guid.NewGuid();
        var holon = new Holon { Id = Guid.NewGuid(), CreatedByAvatarId = owner };
        holon.MetaData[HolonReadGrants.MetadataKey] = raw;
        fixture.Provider.Setup(provider => provider.LoadHolonAsync(holon.Id, false, true, 0, true, false, 0))
            .ReturnsAsync(new OASISResult<IHolon>(holon) { IsLoaded = true });
        var actor = new Mock<IAvatar>();
        actor.SetupGet(avatar => avatar.Id).Returns(owner);

        var result = revoke
            ? await HolonAccess.RevokeAsync(fixture.Manager, holon.Id, new[] { Guid.NewGuid() }, actor.Object)
            : await HolonAccess.ShareAsync(fixture.Manager, holon.Id, new[] { Guid.NewGuid() }, actor.Object);

        result.ErrorCode.Should().Be(HolonReadGrants.InvalidCode);
        fixture.Provider.Invocations.Where(call => call.Method.Name.Contains("Save")
            || call.Method.Name.Contains("Conditional")).Should().BeEmpty();
        holon.MetaData[HolonReadGrants.MetadataKey].Should().BeSameAs(raw);
    }
}
