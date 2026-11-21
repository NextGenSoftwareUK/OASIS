using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.OASIS.API.Contracts.Interfaces;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS;

public partial class GoogleMapsOASIS : IOASISRoutingProvider
{
    private static readonly Uri ComputeRoutesEndpoint = new("https://routes.googleapis.com/directions/v2:computeRoutes");
    private static readonly HttpClient RoutesClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public readonly record struct RoutePoint(double Latitude, double Longitude);

    public sealed record DrivingRoute(int DistanceMeters, string EncodedPolyline, IReadOnlyList<RoutePoint> Points);

    public async Task<RoutingRoute> ComputeRouteAsync(RoutingCoordinate origin, RoutingCoordinate destination,
        RoutingType travelMode, CancellationToken cancellationToken = default)
    {
        var mode = travelMode switch
        {
            RoutingType.Driving => "DRIVE",
            RoutingType.Walking => "WALK",
            RoutingType.Cycling => "BICYCLE",
            _ => throw new ArgumentOutOfRangeException(nameof(travelMode))
        };

        var result = await ComputeGoogleRouteAsync(origin.Latitude, origin.Longitude, destination.Latitude,
            destination.Longitude, Environment.GetEnvironmentVariable("OASIS_GOOGLE_ROUTES_API_KEY"),
            mode, cancellationToken).ConfigureAwait(false);
        if (result.IsError)
            throw new InvalidOperationException(result.Message);

        return new RoutingRoute(result.Result.DistanceMeters, result.Result.Points.Select(
            point => new RoutingCoordinate(point.Latitude, point.Longitude)));
    }

    public Task<OASISResult<DrivingRoute>> ComputeDrivingRouteAsync(
        double originLatitude, double originLongitude, double destinationLatitude, double destinationLongitude,
        string apiKey, CancellationToken cancellationToken = default)
        => ComputeGoogleRouteAsync(originLatitude, originLongitude, destinationLatitude, destinationLongitude,
            apiKey, "DRIVE", cancellationToken);

    private static async Task<OASISResult<DrivingRoute>> ComputeGoogleRouteAsync(
        double originLatitude, double originLongitude, double destinationLatitude, double destinationLongitude,
        string apiKey, string travelMode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = new OASISResult<DrivingRoute>();
        if (!ValidCoordinates(originLatitude, originLongitude) || !ValidCoordinates(destinationLatitude, destinationLongitude))
        {
            OASISErrorHandling.HandleError(ref result, "Valid WGS84 origin and destination coordinates are required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Any(character => character < 33 || character > 126))
        {
            OASISErrorHandling.HandleError(ref result, "A Google Routes API credential is required.");
            return result;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, ComputeRoutesEndpoint);
        request.Headers.Add("X-Goog-Api-Key", apiKey);
        request.Headers.Add("X-Goog-FieldMask", "routes.distanceMeters,routes.polyline.encodedPolyline");
        request.Content = JsonContent.Create(new
        {
            origin = Waypoint(originLatitude, originLongitude),
            destination = Waypoint(destinationLatitude, destinationLongitude),
            travelMode
        });

        try
        {
            using var response = await RoutesClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                OASISErrorHandling.HandleError(ref result, $"Google Routes returned HTTP {(int)response.StatusCode}.");
                return result;
            }

            using var body = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken: cancellationToken).ConfigureAwait(false);
            if (body == null || !body.RootElement.TryGetProperty("routes", out var routes) ||
                routes.ValueKind != JsonValueKind.Array || routes.GetArrayLength() == 0)
            {
                OASISErrorHandling.HandleError(ref result, "Google Routes returned no route.");
                return result;
            }

            var route = routes[0];
            int distance = route.GetProperty("distanceMeters").GetInt32();
            string polyline = route.GetProperty("polyline").GetProperty("encodedPolyline").GetString();
            if (distance < 0 || string.IsNullOrWhiteSpace(polyline))
                throw new JsonException("Google Routes returned an invalid route.");

            result.Result = new DrivingRoute(distance, polyline, DecodePolyline(polyline));
            result.IsLoaded = true;
            result.LoadedCount = 1;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            OASISErrorHandling.HandleError(ref result, "Google Routes request timed out.");
        }
        catch (HttpRequestException)
        {
            OASISErrorHandling.HandleError(ref result, "Google Routes transport failed.");
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException or FormatException or NotSupportedException)
        {
            OASISErrorHandling.HandleError(ref result, "Google Routes returned an invalid response.");
        }

        return result;
    }

    private static IReadOnlyList<RoutePoint> DecodePolyline(string encoded)
    {
        var points = new List<RoutePoint>();
        long latitude = 0;
        long longitude = 0;
        int index = 0;
        while (index < encoded.Length)
        {
            latitude += ReadPolylineDelta(encoded, ref index);
            longitude += ReadPolylineDelta(encoded, ref index);
            var point = new RoutePoint(latitude / 100000d, longitude / 100000d);
            if (!ValidCoordinates(point.Latitude, point.Longitude))
                throw new JsonException("Google Routes returned an out-of-range polyline coordinate.");

            points.Add(point);
        }

        if (points.Count < 2)
            throw new JsonException("Google Routes returned insufficient route geometry.");

        return points.AsReadOnly();
    }

    private static long ReadPolylineDelta(string encoded, ref int index)
    {
        long value = 0;
        for (int shift = 0; shift <= 30 && index < encoded.Length; shift += 5)
        {
            int group = encoded[index++] - 63;
            if (group < 0 || group > 63)
                break;

            value |= (long)(group & 31) << shift;
            if ((group & 32) == 0 && value <= uint.MaxValue)
                return (value & 1) == 0 ? value >> 1 : ~(value >> 1);
        }

        throw new JsonException("Google Routes returned malformed polyline geometry.");
    }

    private static bool ValidCoordinates(double latitude, double longitude)
        => double.IsFinite(latitude) && double.IsFinite(longitude) &&
            latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;

    private static object Waypoint(double latitude, double longitude)
        => new { location = new { latLng = new { latitude, longitude } } };
}
