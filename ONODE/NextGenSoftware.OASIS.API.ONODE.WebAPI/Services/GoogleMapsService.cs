//using System;
//using System.Globalization;
//using System.Net.Http;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Configuration;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
//{
//    public class GoogleMapsService
//    {
//        private readonly ILogger<GoogleMapsService> _logger;
//        private readonly TimoRidesOptions _options;
//        private readonly HttpClient _httpClient;

//        public GoogleMapsService(
//            IOptions<TimoRidesOptions> options,
//            IHttpClientFactory httpClientFactory,
//            ILogger<GoogleMapsService> logger)
//        {
//            _logger = logger;
//            _options = options?.Value ?? new TimoRidesOptions();
//            _httpClient = httpClientFactory?.CreateClient(nameof(GoogleMapsService)) ?? new HttpClient
//            {
//                Timeout = TimeSpan.FromSeconds(20)
//            };
//        }

//        public async Task<LocationLookupResult> GeocodeAsync(string rawAddress, CancellationToken cancellationToken = default)
//        {
//            if (string.IsNullOrWhiteSpace(rawAddress))
//            {
//                return LocationLookupResult.Error("Address is required.");
//            }

//            if (UseMocking())
//            {
//                return LocationLookupResult.FromMock(rawAddress, _options.GoogleMaps?.DefaultCity);
//            }

//            try
//            {
//                var encodedAddress = Uri.EscapeDataString(rawAddress);
//                var requestUri = $"https://maps.googleapis.com/maps/api/geocode/json?address={encodedAddress}&key={_options.GoogleMaps.ApiKey}";
//                var response = await _httpClient.GetAsync(requestUri, cancellationToken);

//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Geocode failed for {Address} with status {StatusCode}", rawAddress, response.StatusCode);
//                    return LocationLookupResult.Error($"Google Maps geocoding failed: {response.StatusCode}");
//                }

//                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
//                var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

//                var status = payload.RootElement.GetProperty("status").GetString();
//                if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
//                {
//                    _logger.LogWarning("Geocode returned status {Status} for {Address}", status, rawAddress);
//                    return LocationLookupResult.Error($"Geocoding status: {status}");
//                }

//                var firstResult = payload.RootElement.GetProperty("results")[0];
//                var formatted = firstResult.GetProperty("formatted_address").GetString();
//                var locationElement = firstResult.GetProperty("geometry").GetProperty("location");

//                var lat = locationElement.GetProperty("lat").GetDouble();
//                var lng = locationElement.GetProperty("lng").GetDouble();

//                return LocationLookupResult.Success(new RideLocation
//                {
//                    Address = formatted ?? rawAddress,
//                    Latitude = lat,
//                    Longitude = lng
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Geocoding failed for {Address}", rawAddress);
//                return LocationLookupResult.Error(ex.Message);
//            }
//        }

//        public async Task<DistanceMatrixResult> CalculateDistanceAsync(
//            RideLocation origin,
//            RideLocation destination,
//            CancellationToken cancellationToken = default)
//        {
//            if (origin == null || destination == null)
//            {
//                return DistanceMatrixResult.Error("Origin and destination are required.");
//            }

//            if (UseMocking())
//            {
//                var distanceKm = HaversineDistance(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude);
//                var durationMinutes = Math.Max(5, distanceKm * 2.5);
                
//                return DistanceMatrixResult.Success(distanceKm, $"{distanceKm:F1} km", TimeSpan.FromMinutes(durationMinutes));
//            }

//            try
//            {
//                var origins = $"{origin.Latitude.ToString(CultureInfo.InvariantCulture)},{origin.Longitude.ToString(CultureInfo.InvariantCulture)}";
//                var destinations = $"{destination.Latitude.ToString(CultureInfo.InvariantCulture)},{destination.Longitude.ToString(CultureInfo.InvariantCulture)}";

//                var requestUri =
//                    $"https://maps.googleapis.com/maps/api/distancematrix/json?origins={origins}&destinations={destinations}&key={_options.GoogleMaps.ApiKey}";

//                var response = await _httpClient.GetAsync(requestUri, cancellationToken);

//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Distance matrix failed: {StatusCode}", response.StatusCode);
//                    return DistanceMatrixResult.Error($"Distance matrix call failed: {response.StatusCode}");
//                }

//                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
//                var payload = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

//                var status = payload.RootElement.GetProperty("status").GetString();
//                if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
//                {
//                    return DistanceMatrixResult.Error($"Distance matrix status: {status}");
//                }

//                var element = payload.RootElement.GetProperty("rows")[0]
//                    .GetProperty("elements")[0];

//                var elementStatus = element.GetProperty("status").GetString();
//                if (!string.Equals(elementStatus, "OK", StringComparison.OrdinalIgnoreCase))
//                {
//                    return DistanceMatrixResult.Error($"Distance matrix element status: {elementStatus}");
//                }

//                var distance = element.GetProperty("distance");
//                var duration = element.GetProperty("duration");

//                var meters = distance.GetProperty("value").GetDouble();
//                var seconds = duration.GetProperty("value").GetDouble();

//                return DistanceMatrixResult.Success(
//                    meters / 1000,
//                    distance.GetProperty("text").GetString(),
//                    TimeSpan.FromSeconds(seconds));
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Distance matrix failed");
//                return DistanceMatrixResult.Error(ex.Message);
//            }
//        }

//        private bool UseMocking()
//        {
//            return string.IsNullOrWhiteSpace(_options.GoogleMaps?.ApiKey) || _options.GoogleMaps.UseMocks;
//        }

//        private static double HaversineDistance(double lat1, double lon1, double lat2, double lon2)
//        {
//            const double R = 6371; // km
//            var dLat = DegreesToRadians(lat2 - lat1);
//            var dLon = DegreesToRadians(lon2 - lon1);

//            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
//                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
//                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

//            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
//            return R * c;
//        }

//        private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180;
//    }

//    public class LocationLookupResult
//    {
//        private LocationLookupResult(bool success, RideLocation location, string error, bool isMock)
//        {
//            IsSuccess = success;
//            Location = location;
//            ErrorMessage = error;
//            IsMock = isMock;
//        }

//        public bool IsSuccess { get; }
//        public RideLocation Location { get; }
//        public string ErrorMessage { get; }
//        public bool IsMock { get; }

//        public static LocationLookupResult Success(RideLocation location) =>
//            new(true, location, null, false);

//        public static LocationLookupResult Error(string message) =>
//            new(false, null, message, false);

//        public static LocationLookupResult FromMock(string rawAddress, string defaultCity)
//        {
//            var hash = rawAddress?.GetHashCode() ?? 0;
//            var lat = -29.8 + (hash % 1000) / 10000.0;
//            var lon = 31.0 + (hash % 500) / 10000.0;

//            return new LocationLookupResult(
//                true,
//                new RideLocation
//                {
//                    Address = string.IsNullOrWhiteSpace(defaultCity) ? rawAddress : $"{rawAddress}, {defaultCity}",
//                    Latitude = lat,
//                    Longitude = lon
//                },
//                null,
//                true);
//        }
//    }

//    public class DistanceMatrixResult
//    {
//        private DistanceMatrixResult(bool success, double kilometers, string readableDistance, TimeSpan duration, string error)
//        {
//            IsSuccess = success;
//            Kilometers = kilometers;
//            DisplayDistance = readableDistance;
//            Duration = duration;
//            ErrorMessage = error;
//        }

//        public bool IsSuccess { get; }
//        public double Kilometers { get; }
//        public string DisplayDistance { get; }
//        public TimeSpan Duration { get; }
//        public string ErrorMessage { get; }

//        public static DistanceMatrixResult Success(double kilometers, string textDistance, TimeSpan duration) =>
//            new(true, kilometers, textDistance, duration, null);

//        public static DistanceMatrixResult Error(string message) =>
//            new(false, 0, null, TimeSpan.Zero, message);
//    }
//}

