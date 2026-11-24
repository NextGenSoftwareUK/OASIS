using NextGenSoftware.OASIS.API.Contracts.Enums;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.Providers.Shared.Maps;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Http;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Models;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Routing;
using NextGenSoftware.OASIS.Providers.Shared.Maps.Search;

namespace NextGenSoftware.OASIS.API.Providers.MapPlatforms.UnitTests;

internal sealed class TestMapProvider : MapProviderBase
{
    public TestMapProvider(ScriptedRouting routing)
        : base(MapProviderType.GoogleMaps, "Test", "Test provider", routing, new NominatimPlaceSearchService())
    {
    }
}

/// <summary>Returns a three-point route per leg, or a configured error.</summary>
internal sealed class ScriptedRouting : MapRoutingServiceBase
{
    public string Error { get; set; }
    public List<(Geolocation From, Geolocation To, RoutingType Type)> Calls { get; } = new();

    public override string Name => "scripted";

    protected override Task<OASISResult<MapRoute>> RequestRouteAsync(Geolocation from, Geolocation to,
        RoutingType routingType, CancellationToken cancellationToken)
    {
        Calls.Add((from, to, routingType));
        if (Error != null) return Task.FromResult(MapResults.Error<MapRoute>(Error));

        var middle = new Geolocation((from.Latitude + to.Latitude) / 2, (from.Longitude + to.Longitude) / 2);
        return Task.FromResult(MapResults.Ok(new MapRoute(new List<Geolocation> { from, middle, to }, 100, 60, routingType, Name)));
    }
}

internal sealed class RecordingSink : IMapCommandSink
{
    public bool Accept { get; set; } = true;
    public List<MapCommand> Applied { get; } = new();

    public bool Apply(MapCommand command)
    {
        if (!Accept) return false;
        Applied.Add(command);
        return true;
    }
}

internal sealed class GeoEntity
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}
