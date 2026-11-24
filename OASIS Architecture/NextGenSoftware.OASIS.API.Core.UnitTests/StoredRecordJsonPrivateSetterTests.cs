using NextGenSoftware.OASIS.API.Core.Holons;
using NextGenSoftware.OASIS.API.Core.Serialization;
using Xunit;
namespace NextGenSoftware.OASIS.API.Core.UnitTests;
public sealed class StoredRecordJsonPrivateSetterTests
{
    public sealed class PrivateSlotHolon:Holon
    {
        public int Score {get;private set;}
        public string Label {get;private set;}="constructor default";
        public int GetterOnly=>42;
        public void Change(int score,string label){Score=score;Label=label;}
    }
    [Fact]
    public void CanonicalSnapshotRestoresPrivateCustomSlotsAndExplicitNull()
    {
        var input=new PrivateSlotHolon();input.Change(17,null);
        var json=StoredRecordJson.SerializeInput(input);
        var restored=StoredRecordJson.Deserialize<PrivateSlotHolon>(json);
        Assert.NotSame(input,restored);
        Assert.Equal(17,restored.Score);
        Assert.Null(restored.Label);
        Assert.Equal(42,restored.GetterOnly);
        Assert.Equal(17,input.Score);Assert.Null(input.Label);
    }
}
