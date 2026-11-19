namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

/// <summary>Neo4j V2 package entry point using the canonical Neo4j driver and record implementation.</summary>
public sealed class Neo4jOASIS2 : Neo4jOASIS
{
    /// <summary>Connects to the configured Neo4j server with the supplied credentials.</summary>
    public Neo4jOASIS2(string host, string username, string password)
        : base(host, username, password)
    {
    }
}
