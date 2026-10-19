using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public sealed class WalkingKarmaTests
{
    [Fact]
    public void OneHundredWalkingStepsAwardsTenKarma()
    {
        Assert.Equal(10, KarmaManager.GetKarmaForType(KarmaTypePositive.Walking100Steps));
    }

    [Fact]
    public void WalkingTypeIsAppendedAndNatureRewardsAreUnchanged()
    {
        Assert.Equal((int)KarmaTypePositive.VisitingNewNatureLocation + 1, (int)KarmaTypePositive.Walking100Steps);
        Assert.Equal(11, KarmaManager.GetKarmaForType(KarmaTypePositive.SpendingTimeInNature));
        Assert.Equal(333, KarmaManager.GetKarmaForType(KarmaTypePositive.VisitingNewNatureLocation));
    }
}
