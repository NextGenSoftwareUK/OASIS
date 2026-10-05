using System.ComponentModel;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums
{
    public enum RoutingProfile
    {
        [Description("mapbox/driving-traffic")]
        MapboxDrivingTraffic,
        [Description("mapbox/driving")]
        MapboxDriving,
        [Description("mapbox/walking")]
        MapboxWalking,
        [Description("mapbox/cycling")]
        MapboxCycling
    }
}