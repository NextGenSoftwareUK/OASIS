using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers;
using Xunit;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests.Controllers;

public sealed class KarmaWeightingControllerTests
{
    [Fact]
    public void GetWeightingsReturnsTheAuthoritativeCoreValues()
    {
        var controller = new KarmaController();

        var positive = controller.GetPositiveKarmaWeighting(KarmaTypePositive.OurWorldHelpOtherPlayer);
        var negative = controller.GetNegativeKarmaWeighting(KarmaTypeNegative.OurWorldDropLitter);

        Assert.False(positive.IsError, positive.Message);
        Assert.False(negative.IsError, negative.Message);
        Assert.Equal(KarmaManager.GetKarmaForType(KarmaTypePositive.OurWorldHelpOtherPlayer), positive.Result);
        Assert.Equal(KarmaManager.GetKarmaForType(KarmaTypeNegative.OurWorldDropLitter), negative.Result);
    }

    [Fact]
    public void WeightingMutationsFailExplicitlyInsteadOfReturningEmptySuccess()
    {
        var controller = new KarmaController();

        var vote = controller.VoteForPositiveKarmaWeighting(KarmaTypePositive.OurWorldHelpOtherPlayer, 101);
        var set = controller.SetNegativeKarmaWeighting(KarmaTypeNegative.OurWorldDropLitter, 251);

        Assert.True(vote.IsError);
        Assert.True(set.IsError);
        Assert.Equal("KARMA_WEIGHTING_GOVERNANCE_NOT_AVAILABLE", vote.ErrorCode);
        Assert.Equal("KARMA_WEIGHTING_GOVERNANCE_NOT_AVAILABLE", set.ErrorCode);
    }
}
