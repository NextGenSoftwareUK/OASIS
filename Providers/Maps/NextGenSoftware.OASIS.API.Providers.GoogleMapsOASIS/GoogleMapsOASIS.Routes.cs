using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS;

public partial class GoogleMapsOASIS
{
    private static readonly Uri ComputeRoutesEndpoint = new("https://routes.googleapis.com/directions/v2:computeRoutes");
    private static readonly HttpClient RoutesClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(30)
    };

    public sealed record DrivingRoute(int DistanceMeters, string EncodedPolyline);

    public async Task<OASISResult<DrivingRoute>> ComputeDrivingRouteAsync(
        double originLatitude, double originLongitude, double destinationLatitude, double destinationLongitude,
        string apiKey, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<DrivingRoute>();
        if (!ValidCoordinates(originLatitude, originLongitude) || !ValidCoordinates(destinationLatitude, destinationLongitude))
        {
            OASISErrorHandling.HandleError(ref result, "Valid WGS84 origin and destination coordinates are required.");
            return result;
        }

        if (string.IsNullOrWhiteSpace(apiKey) || apiKey.Contains('\r') || apiKey.Contains('\n'))
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
            travelMode = "DRIVE"
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
                OASISErrorHandling.HandleError(ref result, "Google Routes returned no driving route.");
                return result;
            }

            var route = routes[0];
            int distance = route.GetProperty("distanceMeters").GetInt32();
            string polyline = route.GetProperty("polyline").GetProperty("encodedPolyline").GetString();
            if (distance < 0 || string.IsNullOrWhiteSpace(polyline))
                throw new JsonException("Google Routes returned an invalid route.");

            result.Result = new DrivingRoute(distance, polyline);
            result.IsLoaded = true;
            result.LoadedCount = 1;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException or FormatException)
        {
            OASISErrorHandling.HandleError(ref result, "Google Routes request or response validation failed.");
        }

        return result;
    }

    private static bool ValidCoordinates(double latitude, double longitude)
        => double.IsFinite(latitude) && double.IsFinite(longitude) &&
            latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;

    private static object Waypoint(double latitude, double longitude)
        => new { location = new { latLng = new { latitude, longitude } } };
}
