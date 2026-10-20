namespace NextGenSoftware.OASIS.API.DNA
{
    public class SolidOASISProviderSettings : ProviderSettingsBase
    {
        public bool IsVersionControlEnabled { get; set; }
        public string PodRoot { get; set; }
        // Explicit selection; never infer anonymous access from missing credentials.
        public string AuthenticationMode { get; set; } = "DpopClientCredentials";
        public string TokenEndpoint { get; set; }
        public string ClientId { get; set; }
        public string StorageKeyEnvironmentVariable { get; set; } = "OASIS_SOLID_STORAGE_KEY";
        public string ClientSecretEnvironmentVariable { get; set; } = "OASIS_SOLID_CLIENT_SECRET";
        public string BearerTokenEnvironmentVariable { get; set; } = "OASIS_SOLID_BEARER_TOKEN";
    }
}
