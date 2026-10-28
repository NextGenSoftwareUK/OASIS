using System;

namespace NextGenSoftware.OASIS.API.Providers.NeonOASIS.UnitTests;

// Constructor-only metadata tests: valid syntax, no network dispatch.
internal static class NeonOASISTestFactory
{
    public static NeonOASIS Create() => new NeonOASIS(
        Environment.GetEnvironmentVariable("NEONOASIS_CONNECTIONSTRING")
        ?? "Host=127.0.0.1;Port=1;Database=synthetic;Username=synthetic");
}
