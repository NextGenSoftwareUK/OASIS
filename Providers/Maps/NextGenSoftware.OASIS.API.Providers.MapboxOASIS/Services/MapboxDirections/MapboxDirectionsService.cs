using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Enums;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Extensions;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Direction;
using NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Models.Requests;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS.Services.MapboxDirections;

public class MapboxDirectionsService : IMapboxDirectionsService, IOASISRoutingProvider
{
    private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public async Task<Response<Direction>> RetrieveDirections(GetDirectionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var response = new Response<Direction>();
        try
        {
            var body = await SendDirectionsAsync(request.RoutingProfile.GetDescription(),
                string.Join(";", request.Coordinates), "", CancellationToken.None).ConfigureAwait(false);
            response.Payload = JsonConvert.DeserializeObject<Direction>(body);
        }
        catch (Exception error)
        {
            response.StatusCode = ResponseStatusCode.Fail;
            response.Message = error is InvalidOperationException ? error.Message : "Mapbox Directions request failed.";
        }

        return response;
    }

    public async Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
        RoutingType travelMode, CancellationToken cancellationToken = default)
    {
        var profile = travelMode switch
        {
            RoutingType.Driving => RoutingProfile.MapboxDriving,
            RoutingType.Walking => RoutingProfile.MapboxWalking,
            RoutingType.Cycling => RoutingProfile.MapboxCycling,
            _ => throw new ArgumentOutOfRangeException(nameof(travelMode))
        };
        var coordinates = FormattableString.Invariant(
            $"{origin.Longitude:R},{origin.Latitude:R};{destination.Longitude:R},{destination.Latitude:R}");
        var body = await SendDirectionsAsync(profile.GetDescription(), coordinates,
            "&overview=full&geometries=geojson&steps=false&alternatives=false", cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        if (root.GetProperty("code").GetString() != "Ok" || root.GetProperty("routes").GetArrayLength() == 0)
            throw new InvalidOperationException("Mapbox Directions returned no route.");

        var route = root.GetProperty("routes")[0];
        var geometry = route.GetProperty("geometry");
        if (geometry.GetProperty("type").GetString() != "LineString")
            throw new InvalidOperationException("Mapbox Directions returned invalid route geometry.");

        return new RoutingRoute(route.GetProperty("distance").GetDouble(),
            geometry.GetProperty("coordinates").EnumerateArray().Select(point =>
                new RoutingCoordinate(point[1].GetDouble(), point[0].GetDouble())));
    }

    private static async Task<string> SendDirectionsAsync(string profile, string coordinates, string options,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var token = Environment.GetEnvironmentVariable("OASIS_MAPBOX_ACCESS_TOKEN");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Configure OASIS_MAPBOX_ACCESS_TOKEN for Mapbox Directions.");

        var url = $"https://api.mapbox.com/directions/v5/{profile}/{coordinates}?access_token={Uri.EscapeDataString(token)}{options}";
        try
        {
            using var response = await Client.GetAsync(url, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Mapbox Directions returned HTTP {(int)response.StatusCode}.");

            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            throw new InvalidOperationException("Mapbox Directions transport failed.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException("Mapbox Directions request timed out.");
        }
    }
}
