using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;
using NextGenSoftware.OASIS.API.Core.Services.Subscriptions;
using NextGenSoftware.OASIS.SubscriptionLedger.OutboxAdmin;
using Xunit;

namespace SubscriptionLedger.OutboxAdmin.Tests;

public sealed class OutboxAdminTests : IDisposable
{
    private const string Secret = "test-only-operator-signing-secret-123456789012345678901234567890";
    private const string Issuer = "oasis-private-operator-test";
    private readonly string _actor = Guid.NewGuid().ToString("D");
    private readonly string _dnaPath = Path.Combine(Path.GetTempPath(), "oasis-outbox-admin-test-" + Guid.NewGuid().ToString("N") + ".json");
    private readonly string _operation = Guid.NewGuid().ToString("D");
    private readonly string _action = Guid.NewGuid().ToString("D");
    public OutboxAdminTests() => File.WriteAllText(_dnaPath, JsonSerializer.Serialize(new { OASIS = new { Security = new { SecretKey = Secret, Oidc = new { Issuer } } } }));
    public void Dispose() => File.Delete(_dnaPath);

    private IConfiguration Config(string token = null, string allowed = null, string mongo = null, string database = null) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string> {
        ["OASIS_DNA_PATH"] = _dnaPath, ["SUBSCRIPTION_ADMIN_BEARER_TOKEN"] = token ?? Token(),
        ["SUBSCRIPTION_ADMIN_AVATAR_IDS"] = allowed ?? _actor, ["SUBSCRIPTION_OUTBOX_MONGO_CONNECTION_STRING"] = mongo,
        ["SUBSCRIPTION_OUTBOX_DATABASE"] = database }).Build();
    private string Token(string secret = Secret, string issuer = Issuer, string audience = Issuer, bool admin = true,
        string subject = null, string id = null, DateTime? expires = null, DateTime? notBefore = null, string algorithm = SecurityAlgorithms.HmacSha256, bool duplicateSubject = false, string identityAlias = null)
    {
        var claims = new List<Claim> { new("sub", subject ?? _actor), new("id", id ?? subject ?? _actor) };
        if (admin) claims.Add(new Claim("oasis.subscription.admin", "true"));
        if (duplicateSubject) claims.Add(new Claim("sub", Guid.NewGuid().ToString("D")));
        if (identityAlias != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, identityAlias));
        var jwt = new JwtSecurityToken(issuer, audience, claims, notBefore ?? DateTime.UtcNow.AddMinutes(-5), expires ?? DateTime.UtcNow.AddMinutes(5),
            new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secret)), algorithm));
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    [Fact]
    public async Task ActorIsDerivedFromExactlyValidatedAndAllowlistedJwt()
    {
        Assert.Equal(_actor, await OutboxAdminCommand.AuthenticateAdministratorAsync(Config()));
    }

    [Theory]
    [InlineData("signature")]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("not-yet-valid")]
    [InlineData("identity-mismatch")]
    [InlineData("duplicate-subject")]
    [InlineData("algorithm")]
    [InlineData("identity-alias")]
    public async Task InvalidSignedTokenNeverReachesStorage(string defect)
    {
        string token = defect switch {
            "signature" => Token(secret: new string('x', 64)),
            "issuer" => Token(issuer: "different-issuer"),
            "audience" => Token(audience: "different-audience"),
            "expired" => Token(expires: DateTime.UtcNow.AddMinutes(-1)),
            "not-yet-valid" => Token(notBefore: DateTime.UtcNow.AddMinutes(1)),
            "identity-mismatch" => Token(id: Guid.NewGuid().ToString("D")),
            "duplicate-subject" => Token(duplicateSubject: true),
            "algorithm" => Token(algorithm: SecurityAlgorithms.HmacSha384),
            "identity-alias" => Token(subject: Guid.NewGuid().ToString("D"), identityAlias: _actor),
            _ => throw new InvalidOperationException()
        };
        using var output = new StringWriter(); using var errors = new StringWriter();
        int code = await OutboxAdminCommand.RunAsync(new[] { "requeue", _operation, _action, "approved repair" }, Config(token), output, errors);
        Assert.Equal(3, code); Assert.Contains("ADMIN_AUTHENTICATION_FAILED", errors.ToString()); Assert.Empty(output.ToString());
        Assert.DoesNotContain(token, errors.ToString()); Assert.DoesNotContain(Secret, errors.ToString());
    }

    [Theory]
    [InlineData("claim")]
    [InlineData("allowlist")]
    public async Task SignedOrdinaryOrUnapprovedAvatarCannotRequeue(string defect)
    {
        using var output = new StringWriter(); using var errors = new StringWriter();
        int code = await OutboxAdminCommand.RunAsync(new[] { "requeue", _operation, _action, "approved repair" },
            Config(Token(admin: defect != "claim"), allowed: defect == "allowlist" ? Guid.NewGuid().ToString("D") : _actor), output, errors);
        Assert.Equal(3, code); Assert.Contains("ADMIN_AUTHORIZATION_FAILED", errors.ToString());
    }

    [Fact]
    public async Task ActorCannotBeInjectedThroughArguments()
    {
        using var output = new StringWriter(); using var errors = new StringWriter();
        int code = await OutboxAdminCommand.RunAsync(new[] { "requeue", _operation, _action, "reason", "--actor", Guid.NewGuid().ToString("D") }, Config(), output, errors);
        Assert.Equal(2, code); Assert.Contains("INVALID_ARGUMENTS", errors.ToString());
    }

    [Fact]
    public async Task MissingDeployedDnaDoesNotGenerateSigningKeys()
    {
        File.WriteAllText(_dnaPath, "{}");
        using var output = new StringWriter(); using var errors = new StringWriter();
        int code = await OutboxAdminCommand.RunAsync(new[] { "requeue", _operation, _action, "reason" }, Config(), output, errors);
        Assert.Equal(2, code); Assert.Contains("OPERATOR_CONFIGURATION_INVALID", errors.ToString()); Assert.Equal("{}", File.ReadAllText(_dnaPath));
    }

    [Fact]
    public async Task RelativeDnaPathIsRejectedEvenWhenItResolvesToValidDeployedDna()
    {
        var configuration = Config();
        configuration["OASIS_DNA_PATH"] = Path.GetRelativePath(Environment.CurrentDirectory, _dnaPath);
        Assert.False(Path.IsPathFullyQualified(configuration["OASIS_DNA_PATH"]));
        Assert.True(File.Exists(configuration["OASIS_DNA_PATH"]));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => OutboxAdminCommand.AuthenticateAdministratorAsync(configuration));
        Assert.Contains("absolute path", error.Message);
    }

    [Fact]
    public async Task HelpDoesNotRequireSecretsOrStorage()
    {
        using var output = new StringWriter(); using var errors = new StringWriter();
        Assert.Equal(0, await OutboxAdminCommand.RunAsync(new[] { "--help" }, new ConfigurationBuilder().Build(), output, errors));
        Assert.Contains("requeue", output.ToString()); Assert.Empty(errors.ToString());
    }

    [Fact]
    public async Task RealMongoRequeueUsesAuthenticatedActorAndPreservesSettlement()
    {
        string connection = Environment.GetEnvironmentVariable("SUBSCRIPTION_TEST_MONGODB_URI")
            ?? throw new InvalidOperationException("SUBSCRIPTION_TEST_MONGODB_URI must explicitly identify a disposable MongoDB replica set.");
        string database = "usage_operator_test_" + Guid.NewGuid().ToString("N");
        try
        {
            var store = new MongoUsageOutboxStore(connection, database); await store.InitializeAsync(default);
            var settlement = new UsageSettlementRequest { OperationId = _operation, UserId = Guid.NewGuid().ToString("D"), ConsumingService = "WEB6", Outcome = "succeeded", ActualCostUsd = 0.25m };
            var operation = new UsageOutboxOperation { Id = _operation, UserId = settlement.UserId, Service = "WEB6", Fingerprint = new string('a', 64),
                State = "deadletter", Settlement = settlement, ExecutionOwner = Guid.NewGuid().ToString("D"), CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow, LastError = "401 credential rotated" };
            Assert.True(await store.TryBeginAsync(operation, default));
            using var output = new StringWriter(); using var errors = new StringWriter();
            var configuration = Config(mongo: connection, database: database);
            string[] command = { "requeue", _operation, _action, "Service credential repaired; approved ticket 123" };
            Assert.Equal(0, await OutboxAdminCommand.RunAsync(command, configuration, output, errors));
            Assert.Equal(0, await OutboxAdminCommand.RunAsync(command, configuration, output, errors));
            var after = await store.GetAsync(_operation, default);
            Assert.Equal("pending", after.State); Assert.Single(after.AdministrativeActions); Assert.Equal(_actor, after.AdministrativeActions[0].Actor);
            Assert.Equal(JsonSerializer.Serialize(settlement), JsonSerializer.Serialize(after.Settlement)); Assert.Equal(operation.Fingerprint, after.Fingerprint);
            Assert.Empty(errors.ToString()); Assert.DoesNotContain(Secret, output.ToString());
            string token = configuration["SUBSCRIPTION_ADMIN_BEARER_TOKEN"]; Assert.DoesNotContain(token, output.ToString());
        }
        finally { await new MongoClient(connection).DropDatabaseAsync(database); }
    }
}
