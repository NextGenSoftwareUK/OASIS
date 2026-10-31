using FluentAssertions;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicDeepLifecycleTests
{
    public class ChangingChildren : Holon,IHolon
    {
        [JsonIgnore] public int Reads;
        [JsonIgnore] public IHolon First;
        private IList<IHolon> _stored;
        public new IList<IHolon> Children
        {get=>_stored ?? (++Reads==1 ? new[]{First}:Array.Empty<IHolon>());set=>_stored=value;}
    }
    public class ChangingName : Holon,IHolon
    {
        [JsonIgnore] public int Reads;
        private bool _assigned;private string _value;
        public new string Name
        {get=>_assigned ? _value:++Reads==1 ? "captured once":"read twice";set{_assigned=true;_value=value;}}
    }
    [Fact]
    public void SaveStagingUsesOneCanonicalSourceCapture()
    {
        var source=new ChangingName{Id=Guid.NewGuid()};
        using var plan=HolonSavePlan.Create(new[]{source},false,true,0);
        source.Reads.Should().Be(1,"initial values and staged values must come from the same serialization");
        plan.Roots[0].Name.Should().Be("captured once");
    }
    private static Holon[] Chain(int count,bool identities=true)
    {
        var rows=Enumerable.Range(0,count).Select(index=>new Holon{Id=identities ? Guid.NewGuid():Guid.Empty,Name="node-"+index}).ToArray();
        for(var index=1;index<count;index++)rows[index-1].Children.Add(rows[index]);
        return rows;
    }
    [Fact]
    public void AcceptCapturesEachSelectedChildCollectionOnce()
    {
        var child=new Holon{Id=Guid.NewGuid(),Name="accepted child"};
        var root=new ChangingChildren{Id=Guid.NewGuid(),First=child};
        HolonChangeTracking.AcceptChanges(root);
        root.Reads.Should().Be(1,"baseline membership and recursive acceptance must use the same capture");
        child.IsNewHolon.Should().BeFalse();HolonChangeTracking.HasChanges(child,false).Should().BeFalse();
    }
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void DeepAcceptDetectAndRejectPreserveTheRequestedScope(bool children)
    {
        var rows=Chain(4096);HolonChangeTracking.AcceptChanges(rows[0]);
        HolonChangeTracking.HasChanges(rows[0]).Should().BeFalse();
        rows[0].Name="pending-root";rows[^1].Name="pending-leaf";
        HolonChangeTracking.HasChanges(rows[0]).Should().BeTrue();
        HolonChangeTracking.RejectChanges(rows[0],children).IsError.Should().BeFalse();
        rows[0].Name.Should().Be("node-0");rows[^1].Name.Should().Be(children ? "node-4095":"pending-leaf");
        HolonChangeTracking.HasChanges(rows[0],false).Should().BeFalse();
        if(!children)HolonChangeTracking.RejectChanges(rows[0]).IsError.Should().BeFalse();
        rows[2048].Children.Clear();rows[^1].Name="detached pending leaf";
        HolonChangeTracking.RejectChanges(rows[0]).IsError.Should().BeFalse();
        rows[2048].Children.Single().Should().BeSameAs(rows[2049]);rows[^1].Name.Should().Be("node-4095");
        HolonChangeTracking.HasChanges(rows[0]).Should().BeFalse();
    }
    [Fact]
    public void DeepRejectPlansEveryBaselineBeforePublishingAnyRestore()
    {
        var rows=Chain(2048);
        foreach(var row in rows.Take(rows.Length-1))HolonChangeTracking.AcceptChanges(row,false);
        rows[0].Name="must remain pending";
        var rejected=HolonChangeTracking.RejectChanges(rows[0]);
        rejected.IsError.Should().BeTrue();rejected.ErrorCode.Should().Be("HOLON_REJECT_CHANGES_FAILED");
        rows[0].Name.Should().Be("must remain pending");rows[^1].IsNewHolon.Should().BeTrue();
    }
    [Fact]
    public void DeepSaveStagingAndAcknowledgementPreserveIdentityOrderAndCallerOwnership()
    {
        var rows=Chain(2048,false);var operation=Guid.NewGuid();var identities=new List<Guid>();
        using(var first=HolonSavePlan.Create(new[]{rows[0]},true,true,0,operation))
        {
            var current=first.Roots[0];
            for(var index=0;index<rows.Length;index++)
            {current.Name.Should().Be(rows[index].Name);identities.Add(current.Id);if(index+1<rows.Length)current=current.Children.Single();}
        }
        identities.Distinct().Count().Should().Be(rows.Length);rows.All(row=>row.Id==Guid.Empty).Should().BeTrue();
        using(var retry=HolonSavePlan.Create(new[]{rows[0]},true,true,0,operation))
        {
            var current=retry.Roots[0];
            for(var index=0;index<rows.Length;index++)
            {current.Id.Should().Be(identities[index]);if(index>0)current.ParentHolonId.Should().Be(identities[index-1]);if(index+1<rows.Length)current=current.Children.Single();}
            retry.Publish(retry.Roots,_=>new OASISResult<bool>(true));
        }
        rows.Select(row=>row.Id).Should().Equal(identities);
        HolonChangeTracking.HasChanges(rows[0]).Should().BeFalse();
        rows[0].Children.Single().Should().BeSameAs(rows[1]);
    }
    [Fact]
    public void FailedGraphStagingReleasesEveryPreviouslyAcquiredLease()
    {
        var rows=Chain(128);rows[0].Children.Add(rows[^1]);
        Action invalid=()=>HolonSavePlan.Create(new[]{rows[0]},true,true,0);
        invalid.Should().Throw<ArgumentException>();rows[0].Children.RemoveAt(1);
        using var recovered=HolonSavePlan.Create(new[]{rows[0]},true,true,0);
        recovered.Roots.Should().ContainSingle();
    }
}
