namespace NextGenSoftware.OASIS.API.Providers.ConvexOASIS.UnitTests;

/// <summary>Metadata-only construction with explicit synthetic configuration; never opens a connection.</summary>
internal static class ConvexOASISTestFactory
{
    public static ConvexOASIS Create() => new("https://synthetic.invalid", "synthetic-admin-key");
}
