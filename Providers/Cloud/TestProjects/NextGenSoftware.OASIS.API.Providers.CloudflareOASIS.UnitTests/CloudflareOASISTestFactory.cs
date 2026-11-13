namespace NextGenSoftware.OASIS.API.Providers.CloudflareOASIS.UnitTests;

internal static class CloudflareOASISTestFactory
{
    internal static CloudflareOASIS Create() => new("syntheticaccount", "syntheticnamespace", "synthetic-token");
}
