using System.Collections.Generic;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Configuration
{
    public class TimoRidesOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:4205";
        public string ApiPrefix { get; set; } = "/api";
        public string DefaultState { get; set; } = "KwaZuluNatal";
        public int HttpTimeoutSeconds { get; set; } = 20;
        public bool UseInsecureSsl { get; set; } = true;
        public DemoRiderOptions DemoRider { get; set; } = new();
        public ServiceAccountOptions ServiceAccount { get; set; } = new();
        public GoogleMapsOptions GoogleMaps { get; set; } = new();
        public IDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }

    public class DemoRiderOptions
    {
        public string FullName { get; set; } = "Timo Telegram Rider";
        public string Email { get; set; } = "telegram.demo@timorides.test";
        public string PhoneNumber { get; set; } = "+27700000000";
        public int DefaultPassengers { get; set; } = 1;
    }

    public class ServiceAccountOptions
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int TokenRefreshMinutes { get; set; } = 45;
    }

    public class GoogleMapsOptions
    {
        public string ApiKey { get; set; }
        public bool UseMocks { get; set; } = true;
        public string DefaultCity { get; set; } = "Durban, South Africa";
    }
}

