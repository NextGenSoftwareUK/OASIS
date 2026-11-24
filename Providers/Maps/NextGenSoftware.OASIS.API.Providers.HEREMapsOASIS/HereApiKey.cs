using System;

namespace NextGenSoftware.OASIS.API.Providers.HEREMapsOASIS
{
    internal static class HereApiKey
    {
        public const string EnvironmentVariable = "OASIS_HERE_ROUTING_API_KEY";

        public static string FromEnvironment() => Environment.GetEnvironmentVariable(EnvironmentVariable);

        public static string Normalise(string apiKey) => string.IsNullOrWhiteSpace(apiKey) ? null : apiKey.Trim();

        public static string MissingMessage(string service) =>
            service + " needs a HERE platform API key; pass one to the constructor or set " + EnvironmentVariable + ".";
    }
}
