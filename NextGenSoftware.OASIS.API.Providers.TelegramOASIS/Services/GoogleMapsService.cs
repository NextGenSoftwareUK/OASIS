using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NextGenSoftware.OASIS.API.Providers.TelegramOASIS.Services
{
    public class GoogleMapsService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GoogleMapsService> _logger;
        private readonly string _apiKey;
        
        public GoogleMapsService(
            IConfiguration configuration,
            ILogger<GoogleMapsService> logger)
        {
            _logger = logger;
            _apiKey = configuration["OASIS:StorageProviders:TelegramOASIS:TimoRides:GoogleMapsApiKey"];
            
            if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_GOOGLE_MAPS_API_KEY")
            {
                _logger.LogWarning("Google Maps API key not configured - will return coordinates instead of addresses");
            }
            
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://maps.googleapis.com/maps/api/"),
                Timeout = TimeSpan.FromSeconds(10)
            };
            
            _logger.LogInformation("GoogleMapsService initialized");
        }
        
        /// <summary>
        /// Reverse geocode coordinates to address
        /// </summary>
        public async Task<string> ReverseGeocodeAsync(double latitude, double longitude)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_GOOGLE_MAPS_API_KEY")
                {
                    _logger.LogWarning("Google Maps API key not configured");
                    return $"Location: {latitude:F4}, {longitude:F4}";
                }
                
                var url = $"geocode/json?latlng={latitude},{longitude}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<GeocodeResponse>();
                
                if (result?.Status == "OK" && result.Results?.Length > 0)
                {
                    var address = result.Results[0].FormattedAddress;
                    _logger.LogInformation($"Geocoded {latitude},{longitude} to: {address}");
                    return address;
                }
                
                _logger.LogWarning($"Geocoding failed with status: {result?.Status}");
                return $"Location: {latitude:F4}, {longitude:F4}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error geocoding location {latitude},{longitude}");
                return $"Location: {latitude:F4}, {longitude:F4}";
            }
        }
        
        /// <summary>
        /// Calculate distance between two points
        /// </summary>
        public async Task<DistanceResult> CalculateDistanceAsync(
            double originLat,
            double originLng,
            double destLat,
            double destLng)
        {
            try
            {
                if (string.IsNullOrEmpty(_apiKey) || _apiKey == "YOUR_GOOGLE_MAPS_API_KEY")
                {
                    // Fallback to Haversine formula
                    var distance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
                    var estimatedTime = (int)(distance / 40 * 60); // Assume 40km/h average speed
                    
                    return new DistanceResult
                    {
                        DistanceKm = distance,
                        DurationMinutes = estimatedTime
                    };
                }
                
                var url = $"distancematrix/json?origins={originLat},{originLng}" +
                          $"&destinations={destLat},{destLng}&key={_apiKey}";
                
                var response = await _httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                
                var result = await response.Content.ReadFromJsonAsync<DistanceMatrixResponse>();
                
                if (result?.Status == "OK" && 
                    result.Rows?.Length > 0 && 
                    result.Rows[0].Elements?.Length > 0)
                {
                    var element = result.Rows[0].Elements[0];
                    
                    if (element.Status == "OK")
                    {
                        return new DistanceResult
                        {
                            DistanceKm = element.Distance.Value / 1000.0,
                            DurationMinutes = element.Duration.Value / 60
                        };
                    }
                }
                
                // Fallback
                var fallbackDistance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
                return new DistanceResult
                {
                    DistanceKm = fallbackDistance,
                    DurationMinutes = (int)(fallbackDistance / 40 * 60)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating distance");
                var distance = CalculateHaversineDistance(originLat, originLng, destLat, destLng);
                return new DistanceResult
                {
                    DistanceKm = distance,
                    DurationMinutes = (int)(distance / 40 * 60)
                };
            }
        }
        
        /// <summary>
        /// Calculate distance using Haversine formula (fallback)
        /// </summary>
        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Earth's radius in kilometers
            
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            
            return R * c;
        }
        
        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
    }
    
    public class DistanceResult
    {
        public double DistanceKm { get; set; }
        public int DurationMinutes { get; set; }
    }
    
    #region Google Maps API Response Models
    
    public class GeocodeResponse
    {
        public string Status { get; set; }
        public GeocodeResult[] Results { get; set; }
    }
    
    public class GeocodeResult
    {
        public string FormattedAddress { get; set; }
    }
    
    public class DistanceMatrixResponse
    {
        public string Status { get; set; }
        public DistanceMatrixRow[] Rows { get; set; }
    }
    
    public class DistanceMatrixRow
    {
        public DistanceMatrixElement[] Elements { get; set; }
    }
    
    public class DistanceMatrixElement
    {
        public string Status { get; set; }
        public DistanceValue Distance { get; set; }
        public DurationValue Duration { get; set; }
    }
    
    public class DistanceValue
    {
        public int Value { get; set; } // meters
        public string Text { get; set; }
    }
    
    public class DurationValue
    {
        public int Value { get; set; } // seconds
        public string Text { get; set; }
    }
    
    #endregion
}





