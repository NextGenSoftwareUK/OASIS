using System.ComponentModel;

namespace Enums
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