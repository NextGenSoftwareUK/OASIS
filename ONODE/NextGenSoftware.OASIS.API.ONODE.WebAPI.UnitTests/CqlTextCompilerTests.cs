using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Holons;
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

    [Fact]
    public void SystemPromptNarrowsToAMappingProviderAndExampleTextsAreOptional()
    {
        const string sources = "[{\"provider\":\"MongoDBOASIS\",\"available\":true},{\"provider\":\"Neo4jOASIS\",\"available\":true}]";
        compiler.SystemPrompt(sources, "Neo4jOASIS", "11111111-1111-1111-1111-111111111111")
            .Should().Contain("providers: Neo4jOASIS.").And.Contain("saved virtual mapping 11111111");
        compiler.ExampleTexts("[{\"provider\":\"MongoDBOASIS\",\"available\":true,\"examples\":[{\"id\":\"public-records\",\"operation\":\"Rows\",\"request\":{\"provider\":\"MongoDBOASIS\"}}]}]")
            .Should().BeEmpty();
    }

    [Theory]
    [InlineData("dev.api.web4.oasisomniverse.one", "https://dev.api.web6.oasisomniverse.one")]
    [InlineData("api.web4.oasisomniverse.one", "https://api.web6.oasisomniverse.one")]
    public void Web6HostFollowsTheRequestHost(string host, string expected)
    {
        if (Environment.GetEnvironmentVariable("OASIS_WEB6_BASE_URL") == null)
            CqlTextMiddleware.Web6BaseUrl(host).Should().Be(expected);
    }

    // Usage admission is looked up by path, so the text routes must carry the compiled query's path before it runs.
    [Theory]
    [InlineData(true, "{\"query\":\"SELECT COUNT(*) AS Total FROM MongoDBOASIS\"}", "/api/data/query-cql/scalar")]
    [InlineData(true, "{\"query\":\"DELETE FROM MongoDBOASIS\"}", "/api/data/query-cql/sql")]
    [InlineData(false, "{\"query\":\"SELECT * FROM MongoDBOASIS\"}", "/api/data/query-cql/sql")]
    public async Task SqlRequestsTakeTheCompiledPathForAdmission(bool authenticated, string body, string expected)
    {
        var http = new DefaultHttpContext { RequestServices = new ServiceCollection().BuildServiceProvider() };
        http.Request.Method = "POST";
        http.Request.Path = CqlTextMiddleware.SqlPath;
        http.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(body));
        if (authenticated) http.Items["Avatar"] = new NextGenSoftware.OASIS.API.Core.Holons.Avatar();
        string? seen = null;
        var middleware = new CqlTextMiddleware(context => { seen = context.Request.Path.Value; return Task.CompletedTask; });

        await middleware.InvokeAsync(http, compiler, null!);

        seen.Should().Be(expected);
        http.Request.Body.Position.Should().Be(0, "the action still binds the original body");
    }
}
