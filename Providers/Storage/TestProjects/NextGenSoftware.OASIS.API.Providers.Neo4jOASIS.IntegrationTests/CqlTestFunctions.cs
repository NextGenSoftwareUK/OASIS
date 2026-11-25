namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

/// <summary>Marker methods for the Q56 user-defined function journey; they run only as native translations.</summary>
public static class CqlTestFunctions
{
    public static string Shout(string value) => throw new InvalidOperationException("Shout is valid only inside a CQL expression.");
    public static string Prefix(string value, int length) => throw new InvalidOperationException("Prefix is valid only inside a CQL expression.");
}
