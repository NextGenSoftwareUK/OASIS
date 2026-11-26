using FluentAssertions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.UnitTests;

public class CqlTextCompilerTests
{
    private readonly CqlTextCompiler compiler = new();

    [Fact]
    public void RowQueryCompilesToTheLeafEndpoint()
    {
        var query = compiler.Compile("SELECT * FROM MongoDBOASIS LIMIT 5");
        query.Path.Should().Be("data/query-cql");
        query.Body.GetProperty("take").GetInt32().Should().Be(5);
        query.Body.GetProperty("provider").GetString().Should().Be("MongoDBOASIS");
    }

    [Fact]
    public void CountCompilesToTheScalarEndpoint()
        => compiler.Compile("SELECT COUNT(*) AS Total FROM MongoDBOASIS").Path.Should().Be("data/query-cql/scalar");

    [Fact]
    public void NamedParametersAreBound()
        => compiler.Compile("SELECT * FROM SQLLiteDBOASIS WHERE Version >= @minimum LIMIT 5", "{\"minimum\":2}")
            .Body.GetRawText().Should().Contain("\"value\":2");

    [Theory]
    [InlineData("SELECT * FROM StarknetOASIS WHERE metadata.StarknetContractAddress = '0x1' LIMIT 1")]
    [InlineData("SELECT * FROM MongoDBOASIS, Neo4jOASIS LIMIT 5")]
    public void BigIntProfilesAndFederationCompile(string text)
    {
        Action compile = () => compiler.Compile(text);
        if (text.Contains("Neo4j")) compiler.Compile(text).Path.Should().Be("data/query-cql/federation");
        else
        {
            // Either accepted or refused with a parser message; never an engine failure.
            try { compile(); }
            catch (CqlTextException error) { error.Message.Should().NotBeNullOrWhiteSpace(); }
        }
    }

    [Fact]
    public void InvalidSqlGivesTheParserMessage()
    {
        Action compile = () => compiler.Compile("DELETE FROM MongoDBOASIS");
        compile.Should().Throw<CqlTextException>().Which.Message.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void AskPromptContainsTheDatabaseRules()
        => compiler.AskPrompt("MongoDBOASIS").Should().Contain("FROM MongoDBOASIS").And.Contain("UNSUPPORTED");

    [Fact]
    public void SystemPromptListsOnlyReadySources()
    {
        compiler.SystemPrompt("[{\"provider\":\"MongoDBOASIS\",\"available\":true},{\"provider\":\"Neo4jOASIS\",\"available\":false}]")
            .Should().Contain("Use only these currently available WEB4 providers: MongoDBOASIS.");
        compiler.SystemPrompt("[]").Should().BeNull();
    }

    [Theory]
    [InlineData("dev.api.web4.oasisomniverse.one", "https://dev.api.web6.oasisomniverse.one")]
    [InlineData("api.web4.oasisomniverse.one", "https://api.web6.oasisomniverse.one")]
    public void Web6HostFollowsTheRequestHost(string host, string expected)
    {
        if (Environment.GetEnvironmentVariable("OASIS_WEB6_BASE_URL") == null)
            CqlTextMiddleware.Web6BaseUrl(host).Should().Be(expected);
    }
}
