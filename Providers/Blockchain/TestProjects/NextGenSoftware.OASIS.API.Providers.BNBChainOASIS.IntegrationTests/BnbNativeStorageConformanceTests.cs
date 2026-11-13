using System.Net.Http.Json;
using System.Text.Json;
using NextGenSoftware.OASIS.Providers.TestSupport;
using Xunit;

namespace NextGenSoftware.OASIS.API.Providers.BNBChainOASIS.IntegrationTests;

public sealed class BnbNativeStorageConformanceTests
{
    private const string Rpc = "http://127.0.0.1:19845";
    private const int ChainId = 714;

    [BnbNativeFixtureTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SharedStorageAgainstBnbSmartChain(bool synchronous)
    {
        Assert.Equal("true", Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));
        var fixture = Path.GetFullPath(Environment.GetEnvironmentVariable("OASIS_BNB_CI_FIXTURE")!);
        Assert.Equal(Path.GetFullPath(Environment.GetEnvironmentVariable("RUNNER_TEMP")!), Path.GetDirectoryName(fixture));
        Assert.StartsWith("oasis-bnb.", Path.GetFileName(fixture));

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        using var response = await http.PostAsJsonAsync(Rpc, new
        {
            jsonrpc = "2.0",
            id = 1,
            method = "parlia_getValidators",
            @params = new[] { "latest" }
        });
        response.EnsureSuccessStatusCode();
        using var validators = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(validators.RootElement.GetProperty("result").EnumerateArray());

        var mode = synchronous ? "sync" : "async";
        var key = Environment.GetEnvironmentVariable("OASIS_BNB_CI_KEY");
        var contract = Environment.GetEnvironmentVariable($"OASIS_BNB_CI_CONTRACT_{mode.ToUpperInvariant()}");
        Assert.False(string.IsNullOrWhiteSpace(key));
        Assert.False(string.IsNullOrWhiteSpace(contract));

        var provider = new BNBChainOASIS(Rpc, key!, contract!, ChainId);
        var reportPath = Path.Combine(fixture, $"{mode}.json");
        Assert.False(File.Exists(reportPath) || File.Exists(reportPath + ".next"));
        var report = await CoreStorageConformance.RunAsync(provider, nameof(BNBChainOASIS), "native", synchronous,
            async checkpoint =>
            {
                await File.WriteAllTextAsync(reportPath + ".next", JsonSerializer.Serialize(checkpoint));
                File.Move(reportPath + ".next", reportPath, overwrite: true);
            });
        Assert.True(report.Succeeded,
            $"{report.CurrentCase}: {report.FailureType} {report.FailureContract}; {report.Passed.Count} checks passed.");
        Assert.True(report.FixtureCleanupComplete);
        Assert.Equal(CoreStorageConformance.ChecksPerMode, report.Passed.Count);
    }
}

public sealed class BnbNativeFixtureTheoryAttribute : TheoryAttribute
{
    public BnbNativeFixtureTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("OASIS_BNB_CI_FIXTURE")))
            Skip = "Requires the disposable native BNB Smart Chain CI fixture.";
    }
}
