using System;

namespace NextGenSoftware.OASIS.API.Providers.GoogleMapsOASIS
{
    internal static class GoogleApiKey
    {
        public const string EnvironmentVariable = "OASIS_GOOGLE_ROUTES_API_KEY";

        public static string FromEnvironment() => Environment.GetEnvironmentVariable(EnvironmentVariable);

        public static string Normalise(string apiKey) => string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();

        public static string MissingMessage(string service) =>
            service + " needs a Google Maps Platform API key; pass one to the constructor or set " + EnvironmentVariable + ".";
    }
}
