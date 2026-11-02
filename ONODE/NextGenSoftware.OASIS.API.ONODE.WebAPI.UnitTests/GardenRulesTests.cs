using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Gardens;

public sealed class GardenRulesTests
{
    [Fact] public void GrowthNeedsBothTimeAndCareAndNeverExceedsMaturity()
    {
        var flower=GardenRules.Get("wildflower");var planted=DateTime.UtcNow.AddDays(-30);
        Assert.Equal(0,GardenRules.Progress(flower,planted,DateTime.UtcNow,0,0));
        Assert.Equal(.5,GardenRules.Progress(flower,planted,DateTime.UtcNow,3,2));
        Assert.Equal(1,GardenRules.Progress(flower,planted,DateTime.UtcNow,20,20));
        Assert.Equal("Alive",GardenRules.Stage(1));
        Assert.Equal(0,GardenRules.Progress(flower,DateTime.UtcNow.AddDays(1),DateTime.UtcNow,20,20));
    }
    [Theory] [InlineData("claim")] [InlineData("Water")] [InlineData(null)]
    public void UnknownActionsCannotRequestRewards(string action)=>Assert.Throws<ArgumentException>(()=>GardenRules.ValidateAction(action));
    [Fact] public void LocationMustBeFreshAndUtc()
    {
        var now=DateTime.UtcNow;GardenRules.ValidateObservation(now,now);
        Assert.Throws<ArgumentException>(()=>GardenRules.ValidateObservation(now.AddMinutes(-3),now));
        Assert.Throws<ArgumentException>(()=>GardenRules.ValidateObservation(DateTime.SpecifyKind(now,DateTimeKind.Unspecified),now));
    }
}
