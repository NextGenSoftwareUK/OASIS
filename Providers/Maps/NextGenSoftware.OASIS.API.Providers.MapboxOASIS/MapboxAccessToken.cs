using System;

namespace NextGenSoftware.OASIS.API.Providers.MapboxOASIS
{
    internal static class MapboxAccessToken
    {
        public const string EnvironmentVariable = "OASIS_MAPBOX_ACCESS_TOKEN";

        public static string FromEnvironment() => Environment.GetEnvironmentVariable(EnvironmentVariable);

        public static string Normalise(string token) => string.IsNullOrWhiteSpace(token) ? null : token.Trim();

        public static string MissingMessage(string service) =>
            service + " needs a Mapbox access token; pass one to the constructor or set " + EnvironmentVariable + ".";
    }
}
