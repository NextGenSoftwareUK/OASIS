namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests
{
    /// <summary>Stands in for GO Map's GOLocationManager.</summary>
    public class FakeGOMapLocationManager
    {
        public FakeGOMapCoordinate currentLocation { get; set; } = new FakeGOMapCoordinate();
    }
}
