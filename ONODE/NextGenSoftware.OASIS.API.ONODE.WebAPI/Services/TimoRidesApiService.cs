//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using NextGenSoftware.OASIS.API.ONODE.WebAPI.Configuration;
//using NextGenSoftware.OASIS.Common;

//namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services
//{
//    public class TimoRidesApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly ILogger<TimoRidesApiService> _logger;
//        private readonly TimoRidesOptions _options;
//        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);

//        private string _cachedToken;
//        private DateTime _tokenExpiryUtc = DateTime.MinValue;

//        public TimoRidesApiService(
//            IHttpClientFactory httpClientFactory,
//            IOptions<TimoRidesOptions> options,
//            IConfiguration configuration,
//            ILogger<TimoRidesApiService> logger)
//        {
//            _logger = logger;
//            _options = options?.Value ?? new TimoRidesOptions();

//            _httpClient = httpClientFactory?.CreateClient(nameof(TimoRidesApiService)) ?? new HttpClient();
//            var baseUrl = _options.BaseUrl?.TrimEnd('/');
//            if (string.IsNullOrEmpty(baseUrl))
//            {
//                baseUrl = configuration["TimoRides:BaseUrl"] ?? "http://localhost:4205";
//            }

//            _httpClient.BaseAddress = new Uri(baseUrl);
//            _httpClient.Timeout = TimeSpan.FromSeconds(Math.Max(5, _options.HttpTimeoutSeconds));

//            if (_options.UseInsecureSsl)
//            {
//                _logger.LogWarning("TimoRidesApiService is configured to bypass SSL validation. Use only in development.");
//            }
//        }

//        #region Public API

//        public async Task<OASISResult<IReadOnlyList<TimoDriverSummary>>> GetNearbyDriversAsync(
//            RideLocation pickup,
//            RideLocation destination,
//            DateTime? scheduledTime,
//            CancellationToken cancellationToken = default)
//        {
//            var result = new OASISResult<IReadOnlyList<TimoDriverSummary>>();

//            try
//            {
//                var query = BuildQueryString(new Dictionary<string, string>
//                {
//                    ["sourceLatitude"] = pickup.Latitude.ToString(CultureInfo.InvariantCulture),
//                    ["sourceLongitude"] = pickup.Longitude.ToString(CultureInfo.InvariantCulture),
//                    ["destinationLatitude"] = destination.Latitude.ToString(CultureInfo.InvariantCulture),
//                    ["destinationLongitude"] = destination.Longitude.ToString(CultureInfo.InvariantCulture),
//                    ["state"] = (_options.DefaultState ?? "KwaZuluNatal").ToLowerInvariant(),
//                    ["scheduledDate"] = scheduledTime?.ToString("o"),
//                    ["page"] = "1",
//                    ["pageSize"] = "5"
//                });

//                var response = await _httpClient.GetAsync($"{_options.ApiPrefix?.TrimEnd('/') ?? "/api"}/cars/proximity{query}", cancellationToken);
//                if (!response.IsSuccessStatusCode)
//                {
//                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
//                    return HandleError(result, $"Driver lookup failed: {response.StatusCode} {body}");
//                }

//                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
//                var payload = await JsonSerializer.DeserializeAsync<DriverSearchResponse>(stream, _serializerOptions, cancellationToken);

//                if (payload?.Cars == null)
//                {
//                    return HandleError(result, "Driver payload was empty.");
//                }

//                var summaries = new List<TimoDriverSummary>();
//                foreach (var car in payload.Cars)
//                {
//                    summaries.Add(new TimoDriverSummary
//                    {
//                        CarId = car.Id,
//                        DriverName = car.Driver?.FullName ?? "Pending driver",
//                        VehicleMake = car.VehicleMake,
//                        VehicleModel = car.VehicleModel,
//                        VehicleColor = car.VehicleColor,
//                        VehicleRegNumber = car.VehicleRegNumber,
//                        Rating = car.Rating,
//                        Distance = car.Distance,
//                        Duration = car.Duration,
//                        DistanceAway = car.DistanceAway,
//                        DurationAway = car.DurationAway,
//                        RideAmount = car.RideAmount,
//                        PhotoUrl = car.Driver?.AvatarUrl
//                    });
//                }

//                result.Result = summaries;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to fetch nearby drivers.");
//                HandleError(result, ex.Message);
//            }

//            return result;
//        }

//        public async Task<OASISResult<TimoBookingResponse>> CreateBookingAsync(
//            CreateBookingRequest bookingRequest,
//            CancellationToken cancellationToken = default)
//        {
//            var result = new OASISResult<TimoBookingResponse>();

//            try
//            {
//                var token = await EnsureAccessTokenAsync(cancellationToken);
//                if (string.IsNullOrEmpty(token))
//                {
//                    return HandleError(result, "Unable to acquire TimoRides access token.");
//                }

//                var payload = JsonSerializer.Serialize(bookingRequest, _serializerOptions);
//                using var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiPrefix?.TrimEnd('/') ?? "/api"}/bookings")
//                {
//                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
//                };

//                httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
//                var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Create booking failed: {Status} {Body}", response.StatusCode, rawBody);
//                    return HandleError(result, $"Booking failed: {response.StatusCode}");
//                }

//                var bookingResponse = JsonSerializer.Deserialize<TimoBookingResponse>(rawBody, _serializerOptions);
//                result.Result = bookingResponse;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Booking creation failed");
//                HandleError(result, ex.Message);
//            }

//            return result;
//        }

//        public async Task<OASISResult<TimoBooking>> GetBookingAsync(string bookingId, CancellationToken cancellationToken = default)
//        {
//            var result = new OASISResult<TimoBooking>();

//            try
//            {
//                var token = await EnsureAccessTokenAsync(cancellationToken);
//                if (string.IsNullOrWhiteSpace(token))
//                {
//                    return HandleError(result, "Missing access token.");
//                }

//                var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.ApiPrefix?.TrimEnd('/') ?? "/api"}/bookings/{bookingId}");
//                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                var response = await _httpClient.SendAsync(request, cancellationToken);
//                var rawBody = await response.Content.ReadAsStringAsync(cancellationToken);

//                if (!response.IsSuccessStatusCode)
//                {
//                    return HandleError(result, $"Fetch booking failed: {response.StatusCode}");
//                }

//                var payload = JsonSerializer.Deserialize<BookingEnvelope>(rawBody, _serializerOptions);
//                result.Result = payload?.Booking;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to fetch booking {BookingId}", bookingId);
//                HandleError(result, ex.Message);
//            }

//            return result;
//        }

//        public async Task<OASISResult<bool>> CancelBookingAsync(string bookingId, string reason, CancellationToken cancellationToken = default)
//        {
//            var result = new OASISResult<bool>();

//            try
//            {
//                var token = await EnsureAccessTokenAsync(cancellationToken);
//                if (string.IsNullOrEmpty(token))
//                {
//                    return HandleError(result, "Unable to acquire access token.");
//                }

//                var payload = JsonSerializer.Serialize(new
//                {
//                    bookingId,
//                    reason
//                }, _serializerOptions);

//                var request = new HttpRequestMessage(HttpMethod.Post, $"{_options.ApiPrefix?.TrimEnd('/') ?? "/api"}/bookings/cancel-acceptance")
//                {
//                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
//                };

//                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                var response = await _httpClient.SendAsync(request, cancellationToken);
//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogWarning("Cancel booking failed with {Status}", response.StatusCode);
//                    return HandleError(result, $"Cancel booking failed: {response.StatusCode}");
//                }

//                result.Result = true;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to cancel booking {BookingId}", bookingId);
//                HandleError(result, ex.Message);
//            }

//            return result;
//        }

//        public async Task<OASISResult<IReadOnlyList<TimoBooking>>> GetRiderBookingsAsync(CancellationToken cancellationToken = default)
//        {
//            var result = new OASISResult<IReadOnlyList<TimoBooking>>();

//            try
//            {
//                var token = await EnsureAccessTokenAsync(cancellationToken);
//                if (string.IsNullOrEmpty(token))
//                {
//                    return HandleError(result, "Unable to acquire token.");
//                }

//                var request = new HttpRequestMessage(HttpMethod.Get, $"{_options.ApiPrefix?.TrimEnd('/') ?? "/api"}/bookings");
//                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

//                var response = await _httpClient.SendAsync(request, cancellationToken);
//                var body = await response.Content.ReadAsStringAsync(cancellationToken);

//                if (!response.IsSuccessStatusCode)
//                {
//                    return HandleError(result, $"Bookings lookup failed: {response.StatusCode}");
//                }

//                var envelope = JsonSerializer.Deserialize<BookingListEnvelope>(body, _serializerOptions);
//                result.Result = envelope?.Bookings ?? Array.Empty<TimoBooking>();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to fetch rider bookings");
//                HandleError(result, ex.Message);
//            }

//            return result;
//        }

//        #endregion

//        #region Helpers

//        private async Task<string> EnsureAccessTokenAsync(CancellationToken cancellationToken)
//        {
//            if (!string.IsNullOrEmpty(_cachedToken) && DateTime.UtcNow < _tokenExpiryUtc)
//            {
//                return _cachedToken;
//            }

//            var serviceEmail = _options.ServiceAccount?.Email ?? _options.DemoRider?.Email;
//            var servicePassword = _options.ServiceAccount?.Password;

//            if (string.IsNullOrEmpty(serviceEmail) || string.IsNullOrEmpty(servicePassword))
//            {
//                _logger.LogWarning("TimoRides service credentials are not configured.");
//                return null;
//            }

//            var payload = JsonSerializer.Serialize(new
//            {
//                email = serviceEmail,
//                password = servicePassword
//            }, _serializerOptions);

//            var response = await _httpClient.PostAsync(
//                $"{_options.ApiPrefix?.TrimEnd('/') ?? "/api"}/auth/login",
//                new StringContent(payload, Encoding.UTF8, "application/json"),
//                cancellationToken);

//            if (!response.IsSuccessStatusCode)
//            {
//                _logger.LogWarning("Login failed for TimoRides service user {Email}", serviceEmail);
//                return null;
//            }

//            var body = await response.Content.ReadAsStringAsync(cancellationToken);
//            var tokenPayload = JsonSerializer.Deserialize<LoginResponse>(body, _serializerOptions);

//            _cachedToken = tokenPayload?.AccessToken;
//            _tokenExpiryUtc = DateTime.UtcNow.AddMinutes(Math.Max(5, _options.ServiceAccount?.TokenRefreshMinutes ?? 30));

//            return _cachedToken;
//        }

//        private static string BuildQueryString(IDictionary<string, string> parameters)
//        {
//            var sb = new StringBuilder("?");
//            var first = true;
//            foreach (var kv in parameters)
//            {
//                if (string.IsNullOrWhiteSpace(kv.Value))
//                    continue;

//                if (!first)
//                    sb.Append('&');

//                sb.Append(Uri.EscapeDataString(kv.Key));
//                sb.Append('=');
//                sb.Append(Uri.EscapeDataString(kv.Value));
//                first = false;
//            }

//            return first ? string.Empty : sb.ToString();
//        }

//        private static OASISResult<T> HandleError<T>(OASISResult<T> result, string message)
//        {
//            result.IsError = true;
//            result.Message = message;
//            return result;
//        }

//        #endregion
//    }

//    #region DTOs

//    public class DriverSearchResponse
//    {
//        public IReadOnlyList<CarDriverDto> Cars { get; set; }
//    }

//    public class CarDriverDto
//    {
//        public string Id { get; set; }
//        public DriverDto Driver { get; set; }
//        public string VehicleMake { get; set; }
//        public string VehicleModel { get; set; }
//        public string VehicleColor { get; set; }
//        public string VehicleRegNumber { get; set; }
//        public double Rating { get; set; }
//        public string Duration { get; set; }
//        public string Distance { get; set; }
//        public string DurationAway { get; set; }
//        public string DistanceAway { get; set; }
//        public decimal RideAmount { get; set; }
//    }

//    public class DriverDto
//    {
//        public string FullName { get; set; }
//        public string AvatarUrl { get; set; }
//    }

//    public class CreateBookingRequest
//    {
//        public string Car { get; set; }
//        public decimal TripAmount { get; set; }
//        public bool IsCash { get; set; } = true;
//        public DateTime DepartureTime { get; set; } = DateTime.UtcNow;
//        public string PhoneNumber { get; set; }
//        public string Email { get; set; }
//        public string FullName { get; set; }
//        public string BookingType { get; set; } = "passengers";
//        public LocationPayload SourceLocation { get; set; }
//        public LocationPayload DestinationLocation { get; set; }
//        public int Passengers { get; set; } = 1;
//        public string State { get; set; }
//        public object Currency { get; set; } = new { symbol = "R", code = "ZAR" };
//    }

//    public class LocationPayload
//    {
//        public string Address { get; set; }
//        public double Latitude { get; set; }
//        public double Longitude { get; set; }
//    }

//    public class TimoBookingResponse
//    {
//        public string Id { get; set; }
//    }

//    public class BookingEnvelope
//    {
//        public TimoBooking Booking { get; set; }
//    }

//    public class BookingListEnvelope
//    {
//        public IReadOnlyList<TimoBooking> Bookings { get; set; }
//    }

//    public class TimoBooking
//    {
//        public string Id { get; set; }
//        public string Status { get; set; }
//        public string TripAmount { get; set; }
//        public string BookingType { get; set; }
//        public string FullName { get; set; }
//        public string PhoneNumber { get; set; }
//        public string Email { get; set; }
//        public LocationPayload SourceLocation { get; set; }
//        public LocationPayload DestinationLocation { get; set; }
//        public DateTime DepartureTime { get; set; }
//    }

//    public class LoginResponse
//    {
//        public string AccessToken { get; set; }
//        public string RefreshToken { get; set; }
//    }

//    #endregion
//}

