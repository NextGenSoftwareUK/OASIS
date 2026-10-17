namespace NextGenSoftware.OASIS.API.Providers.GOMapOASIS.UnitTests
{
    /// <summary>Stands in for GoShared.GOCoordinate, which uses lower-case fields.</summary>
    public class FakeGOMapCoordinate
    {
        public double latitude { get; set; } = 51.5138;
        public double longitude { get; set; } = -0.0984;
        public double altitude { get; set; }
    }
}
