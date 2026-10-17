using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Middleware;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Subscription;
using Xunit;

namespace SubscriptionLedger.ProtocolTests;

public sealed class JwtAndMigrationSignatureTests
{
    private const string Key = "test-only-signing-key-minimum-32-bytes-long";
    private static string Token(string subject, string id, string issuer, DateTime expires, string key = Key) =>
        new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(issuer, issuer,
            new[] { new Claim("sub", subject), new Claim("id", id), new Claim("oasis.subscription.admin", "true") },
            DateTime.UtcNow.AddHours(-2), expires, new SigningCredentials(new SymmetricSecurityKey(Encoding.ASCII.GetBytes(key)), SecurityAlgorithms.HmacSha256)));

    [Theory]
    [InlineData("OASIS")][InlineData("https://identity.oasis.example")]
    public void RealIssuerContractPreservesAuthenticatedAdminAndAvatarClaims(string issuer)
    {
        string id = Guid.NewGuid().ToString();
        var principal = OasisJwtValidation.Validate(Token(id, id, issuer, DateTime.UtcNow.AddMinutes(10)), Key, issuer);
        Assert.True(principal.Identity.IsAuthenticated); Assert.Equal(id, principal.FindFirst("sub").Value);
        var context = new DefaultHttpContext { User = principal };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> { ["SUBSCRIPTION_ADMIN_AVATAR_IDS"] = id }).Build();
        Assert.Equal(id, SubscriptionServiceIdentity.RequireAdministrator(context, config));
    }
    [Theory]
    [InlineData("expired")][InlineData("issuer")][InlineData("signature")][InlineData("subject")]
    public void UntrustedTokensAreRejectedBeforeIdentityBinding(string attack)
    {
        string id = Guid.NewGuid().ToString();
        string token = Token(id, attack == "subject" ? Guid.NewGuid().ToString() : id,
            attack == "issuer" ? "different-issuer" : "OASIS", attack == "expired" ? DateTime.UtcNow.AddMinutes(-1) : DateTime.UtcNow.AddMinutes(10),
            attack == "signature" ? "other-secret-signing-key-minimum-32-bytes" : Key);
        Assert.ThrowsAny<SecurityTokenException>(() => OasisJwtValidation.Validate(token, Key, "OASIS"));
    }
    [Fact]
    public void OpeningBalanceApprovalBindsBatchSourceBalancesAndActor()
    {
        string actor = Guid.NewGuid().ToString();
        var manifest = new UsageOpeningBalanceManifest { MigrationId = Guid.NewGuid().ToString(), ApprovedBy = actor,
            SourceDigest = new string('a', 64), EvidenceSha256 = new string('b', 64), BatchCount = 2, BatchIndex = 0,
            Balances = new() { new() { UserId = Guid.NewGuid().ToString(), Day = "2026-09-22", SettledCostUsd = .123m } } };
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string> { ["SUBSCRIPTION_MIGRATION_APPROVAL_KEY"] = Key }).Build();
        manifest.Signature = Convert.ToHexStringLower(HMACSHA256.HashData(Encoding.UTF8.GetBytes(Key), Encoding.UTF8.GetBytes(UsageMigrationSignature.CanonicalJson(manifest))));
        UsageMigrationSignature.RequireApproval(manifest, actor, config);
        manifest.BatchIndex = 1;
        Assert.Throws<UnauthorizedAccessException>(() => UsageMigrationSignature.RequireApproval(manifest, actor, config));
    }
}
