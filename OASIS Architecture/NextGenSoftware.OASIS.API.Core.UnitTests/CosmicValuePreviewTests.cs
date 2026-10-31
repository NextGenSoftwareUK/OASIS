using FluentAssertions;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicValuePreviewTests
{
    [Fact]
    public void PreviewUsesPrivateBaselineAndRemainsDetachedFromLaterEdits()
    {
        var source=new Holon{Name="accepted"};source.AcceptChanges();
        source.Original.Name="tampered compatibility view";source.Name="pending";
        var result=HolonChangeTracking.PreviewValueChanges(source);
        result.IsError.Should().BeFalse();result.IsSaved.Should().BeFalse();
        var delta=result.Result.Single(change=>change.Path=="/Name");
        delta.PreviousJson.Should().Be("\"accepted\"");delta.CurrentJson.Should().Be("\"pending\"");
        source.Name="later";delta.CurrentJson.Should().Be("\"pending\"");
    }

    [Fact]
    public void PreviewAfterAcknowledgementIncludesOnlyTheRemainingPendingValue()
    {
        var source=new Holon{Name="submitted"};source.AcceptChanges();
        using var plan=HolonSavePlan.Create(new[]{source},false,false,0);
        source.Name="pending during IO";
        plan.Publish(plan.Roots,_=>new OASISResult<bool>(true));
        var preview=HolonChangeTracking.PreviewValueChanges(source);
        preview.IsError.Should().BeFalse();
        var delta=preview.Result.Single(change=>change.Path=="/Name");
        delta.PreviousJson.Should().Be("\"submitted\"");delta.CurrentJson.Should().Be("\"pending during IO\"");
    }

    [Fact]
    public void MissingBaselineFailsRatherThanInventingCommittedBeforeValues()
    {
        var source=new Holon{Name="detached"};
        var preview=HolonChangeTracking.PreviewValueChanges(source);
        preview.IsError.Should().BeTrue();preview.ErrorCode.Should().Be("HOLON_TRACKING_BASELINE_MISSING");
        source.IsNewHolon.Should().BeTrue();source.Original.Should().BeNull();
        source.AcceptChanges();HolonChangeTracking.PreviewValueChanges(source).Result.Should().BeEmpty();
    }
}
