using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicLoadedAdmissionTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void AdmissionAndBaselineUseTheSameSingleMembershipCapture(bool rejectChild)
    {
        var child = new Holon { Id = Guid.NewGuid(), Name = "child" };
        var root = new CosmicDeepLifecycleTests.ChangingChildren { Id = Guid.NewGuid(), First = child };
        var admitted = new List<IHolon>();
        var accepted = HolonChangeTracking.AcceptBatch(new[] { root }, beforeCapture: value => {
            admitted.Add(value); return !rejectChild || !ReferenceEquals(value, child); });
        root.Reads.Should().Be(1); admitted.Should().Equal(root, child); accepted.Should().Be(!rejectChild);
        if (rejectChild)
        {
            root.Original.Should().BeNull(); child.Original.Should().BeNull();
            root.IsNewHolon.Should().BeTrue(); child.IsNewHolon.Should().BeTrue();
        }
        else
        {
            root.Original.Should().NotBeNull(); child.Original.Should().NotBeNull();
            HolonChangeTracking.HasChanges(child, false).Should().BeFalse();
        }
    }
}
