using NextGenSoftware.OASIS.API.Providers.Neo4jOASIS;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

internal static class Neo4jOASISTestFactory
{
    public static Neo4jOASIS CreateFromEnvironment() => new(Require("NEO4JOASIS_HOST"), Require("NEO4JOASIS_USERNAME"), Require("NEO4JOASIS_PASSWORD"));

    private static string Require(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException($"{name} must identify a disposable loopback Neo4j instance.");
        return value;
    }
}
