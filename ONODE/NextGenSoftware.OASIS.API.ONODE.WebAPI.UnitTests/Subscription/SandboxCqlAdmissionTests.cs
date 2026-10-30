using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;

public class SandboxCqlAdmissionTests
{
    [Theory]
    [InlineData("POST", "/api/data/query-cql", 1)]
    [InlineData("POST", "/api/data/query-cql/scalar", 1)]
    [InlineData("POST", "/api/data/query-cql/federation", 3)]
    [InlineData("POST", "/api/data/save-holon", 512)]
    [InlineData("POST", "/api/data/save-holons", 512)]
    [InlineData("DELETE", "/api/data/delete-holon", 512)]
    [InlineData("POST", "/api/data/recover-conditional-operation", 2)]
    public async Task ShippedFreeProfilePassesStartupAndRealAuthorityMeterValidation(string method, string path, int attempts)
    {
        // Read the deployable file rather than a separately maintained test copy.
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "cql-free-admission.json"));
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["PREPARED_STORAGE_ADMISSION_JSON"] = json
        }).Build();
        var services = new ServiceCollection();
        var select = services.AddConfiguredStorageAdmission(configuration, true, _ => null);
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;
        var policy = select(context);
        Assert.NotNull(policy);
        Assert.Equal(attempts, policy.MaximumProviderAttempts);
        Assert.True(policy.RequiresProviderMeasurement);
        Assert.Equal(0m, policy.EstimatedCostUsd);
        Assert.Equal(0m, policy.MaximumNativeFeeCostUsd);

        var repository = new Mock<ISubscriptionUsageRepository>(MockBehavior.Strict);
        repository.Setup(store => store.AuthorizeAsync(It.Is<SubscriptionUsageEvent>(entry =>
            entry.MeterCategory == "api.request" && entry.ReservedCostUsd == 0 && entry.RequestedUnits == 1),
            0, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SubscriptionUsageLimitException("QUOTA_CHECK_REACHED", "Controlled repository denial."));
        var authority = new SubscriptionService(NullLogger<SubscriptionService>.Instance, repository.Object,
            new Mock<ISubscriptionBillingRepository>(MockBehavior.Strict).Object);
        var decision = await authority.AuthorizeUsageAsync(Guid.NewGuid().ToString("D"), 0,
            new UsageAuthorizationRequest {
                OperationId = Guid.NewGuid().ToString("D"), ConsumingService = "WEB4",
                Endpoint = method + " " + context.Request.Path, MeterCategory = policy.MeterCategory,
                RequestedUnits = policy.ReservedUnits, EstimatedCostUsd = policy.EstimatedCostUsd,
                RequestFingerprint = new string('a', 64)
            }, CancellationToken.None);
        Assert.Equal("QUOTA_CHECK_REACHED", decision.Code);
        repository.VerifyAll(); // A free price must still go through quota authorization.
        context.Request.Path = "/api/data/query-cql/projection";
        Assert.Null(select(context)); // This profile does not silently enroll other routes.
    }
    [Theory]
    [InlineData("MongoDBOASIS")]
    [InlineData("SQLLiteDBOASIS")]
    [InlineData("Neo4jOASIS")]
    public void MissingWriteTariffFailsStartupRatherThanLeavingAnUnmeteredWrite(string provider)
    {
        var profile = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(
            Path.Combine(AppContext.BaseDirectory, "cql-free-admission.json")))!;
        var tariffs = profile["storageTariffs"]!.AsArray();
        var missing = tariffs.Single(row => row!["provider"]!.GetValue<string>() == provider &&
            row["operation"]!.GetValue<string>() == "SaveHolon");
        tariffs.Remove(missing);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
            ["PREPARED_STORAGE_ADMISSION_JSON"] = profile.ToJsonString()
        }).Build();
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection()
            .AddConfiguredStorageAdmission(configuration, true, _ => null));
    }

}
