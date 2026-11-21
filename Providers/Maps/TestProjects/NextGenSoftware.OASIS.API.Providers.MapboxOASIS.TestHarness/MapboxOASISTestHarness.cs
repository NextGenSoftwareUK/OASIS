using System;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.TestHarness;

public static class MapboxOASISTestHarness
{
    public static async Task<int> Main()
    {
        if (Environment.GetEnvironmentVariable("OASIS_ROUTING_LIVE_TESTS") != "1" ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_MAPBOX_ACCESS_TOKEN")))
        {
            Console.Error.WriteLine("Enable OASIS_ROUTING_LIVE_TESTS and configure OASIS_MAPBOX_ACCESS_TOKEN.");
            return 1;
        }

        IOASISRoutingProvider provider = new MapBoxOASIS();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            var route = await provider.ComputeRouteAsync(
                new RoutingCoordinate(52.517037, 13.388860),
                new RoutingCoordinate(52.529407, 13.397634), RoutingType.Driving, timeout.Token);
            Console.WriteLine($"Mapbox returned {route.DistanceMeters:F0} metres and {route.Points.Count} points.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine($"Mapbox route verification failed ({error.GetType().Name}).");
            return 1;
        }
    }
}
