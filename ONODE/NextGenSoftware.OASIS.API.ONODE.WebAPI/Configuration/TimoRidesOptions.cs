namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Configuration
{
    /// <summary>
    /// Configuration options for the TimoRides ride-sharing integration, loaded from appsettings.json
    /// under the "TimoRides" section.
    /// </summary>
    public class TimoRidesOptions
    {
        public string BaseUrl { get; set; } = "http://localhost:4205";
        public string ApiPrefix { get; set; } = "/api";
        public int HttpTimeoutSeconds { get; set; } = 30;
        public bool UseInsecureSsl { get; set; } = false;
        public string DefaultState { get; set; } = "KwaZuluNatal";
        public string TelegramBotToken { get; set; }
        public string OpenAiApiKey { get; set; }
        public string OasisApiBaseUrl { get; set; } = "http://localhost:7011";
        public string OasisBotAvatarUsername { get; set; }
        public string OasisBotAvatarPassword { get; set; }
        public string OasisBotAvatarId { get; set; }
        public string PinataJwt { get; set; }

        public GoogleMapsSection GoogleMaps { get; set; } = new();
        public ServiceAccountSection ServiceAccount { get; set; } = new();
        public DemoRiderSection DemoRider { get; set; } = new();
    }

    public class GoogleMapsSection
    {
        public string ApiKey { get; set; }
        public string DefaultCity { get; set; } = "Durban";
        public bool UseMocks { get; set; } = false;
    }

    public class ServiceAccountSection
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int TokenRefreshMinutes { get; set; } = 30;
    }

    public class DemoRiderSection
    {
        public string Email { get; set; }
        public string Name { get; set; } = "Demo Rider";
        public string FullName { get; set; } = "Demo Rider";
        public string Phone { get; set; } = "+27000000000";
        public string PhoneNumber { get; set; } = "+27000000000";
        public int DefaultPassengers { get; set; } = 1;
    }
}
