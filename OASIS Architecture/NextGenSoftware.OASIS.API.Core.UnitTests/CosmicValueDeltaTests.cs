using FluentAssertions;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Holons;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicValueDeltaTests
{
    [Fact]
    public void MissingNullAndNestedValuesHaveDistinctEvidence()
    {
        var before=JObject.Parse("{\"MetaData\":{\"removed\":null,\"changed\":1}}");
        var after=JObject.Parse("{\"MetaData\":{\"added\":null,\"changed\":2}}");
        var changes=HolonValueDelta.Compare(before,after);
        changes.Select(change=>change.Path).Should().Equal("/MetaData/added","/MetaData/changed","/MetaData/removed");
        var added=changes[0];added.PreviousExists.Should().BeFalse();added.CurrentExists.Should().BeTrue();added.CurrentJson.Should().Be("null");
        changes[1].Change.Should().Be(HolonValueChange.Modified);
        changes[2].PreviousExists.Should().BeTrue();changes[2].PreviousJson.Should().Be("null");changes[2].CurrentExists.Should().BeFalse();
    }

    [Fact]
    public void OrderedArraysAreReplacedAndComparisonValuesDoNotAliasInputs()
    {
        var before=JObject.Parse("{\"items\":[1,2]}");var after=JObject.Parse("{\"items\":[2,1]}");
        var delta=HolonValueDelta.Compare(before,after).Single();
        before["items"]=new JArray();after["items"]=new JArray();
        delta.Path.Should().Be("/items");delta.PreviousJson.Should().Be("[1,2]");delta.CurrentJson.Should().Be("[2,1]");
    }

    [Fact]
    public void PointerEscapingAndEmptyObjectChangesArePreserved()
    {
        var before=JObject.Parse("{\"a/b~c\":{}}");var after=JObject.Parse("{\"a/b~c\":null}");
        var delta=HolonValueDelta.Compare(before,after).Single();
        delta.Path.Should().Be("/a~1b~0c");delta.PreviousJson.Should().Be("{}");delta.CurrentJson.Should().Be("null");
    }

    [Fact]
    public void EquivalentPropertyOrderCreatesNoDelta()
    {
        HolonValueDelta.Compare(JObject.Parse("{\"a\":1,\"b\":2}"),JObject.Parse("{\"b\":2,\"a\":1}")).Should().BeEmpty();
    }
}
