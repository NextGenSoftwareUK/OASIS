using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicPreparationCipherTests
{
    private sealed class Resolver : IHolonMetadataKeyResolver
    {
        public bool Denied;
        public List<HolonMetadataKeyRequest> Requests { get; } = new();
        public List<HolonMetadataKeyMaterial> Leases { get; } = new();
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Requests.Add(request);
            if (Denied) return new() { IsError = true, ErrorCode = "PREPARATION_DENIED" };
            var lease = new HolonMetadataKeyMaterial(Enumerable.Repeat((byte)23, 32).ToArray());
            Leases.Add(lease); return new(lease);
        }
    }
    private static HolonMetadataEncryptionPolicy Policy(Resolver resolver, string key = "old-key") => new("tenant-a", key, resolver);

    [Fact]
    public void PreparationIsRandomizedAndUsesDistinctResolverPurposesAndStoredKeyOnRestart()
    {
        var record = Guid.NewGuid(); var operation = Guid.NewGuid(); var actor = Guid.NewGuid();
        const string exact = "{\"amount\":1.000000000000000000000000001,\"text\":\"001.00\"}";
        var resolver = new Resolver(); var policy = Policy(resolver);
        var first = HolonPreparationCipher.Encrypt(record, operation, actor, ProviderType.SQLLiteDBOASIS, exact, policy);
        var second = HolonPreparationCipher.Encrypt(record, operation, actor, ProviderType.SQLLiteDBOASIS, exact, policy);
        Assert.NotEqual(first, second); Assert.DoesNotContain("amount", first);
        var fresh = new Resolver();
        Assert.Equal(exact, HolonPreparationCipher.Decrypt(record, operation, actor, ProviderType.SQLLiteDBOASIS, first, Policy(fresh, "new-key")));
        Assert.All(resolver.Requests, r => Assert.Equal(HolonMetadataKeyPurpose.EncryptPreparation, r.Purpose));
        var request = Assert.Single(fresh.Requests);
        Assert.Equal(HolonMetadataKeyPurpose.DecryptPreparation, request.Purpose);
        Assert.Equal("old-key", request.KeyId); Assert.Equal(record, request.RecordId); Assert.Equal("tenant-a", request.ContextId);
        Assert.All(resolver.Leases.Concat(fresh.Leases), lease => Assert.True(lease.IsDisposed));
    }

    [Theory]
    [InlineData("operationId")] [InlineData("actorId")] [InlineData("authority")] [InlineData("purpose")]
    public void UnmatchedPreparationBindingRejectsBeforeResolvingAnyKey(string field)
    {
        var record = Guid.NewGuid(); var op = Guid.NewGuid(); var actor = Guid.NewGuid(); var resolver = new Resolver();
        var envelope = HolonPreparationCipher.Encrypt(record, op, actor, ProviderType.SQLLiteDBOASIS, "{}", Policy(resolver));
        var json = JObject.Parse(envelope.Substring(HolonPreparationCipher.Prefix.Length));
        json[field] = field == "authority" ? new JValue((int)ProviderType.MongoDBOASIS) : new JValue("wrong");
        var reader = new Resolver();
        Assert.Throws<InvalidDataException>(() => HolonPreparationCipher.Decrypt(record, op, actor, ProviderType.SQLLiteDBOASIS,
            HolonPreparationCipher.Prefix + json.ToString(), Policy(reader)));
        Assert.Empty(reader.Requests);
    }

    [Theory]
    [InlineData("operationId")] [InlineData("actorId")] [InlineData("authority")]
    public void RewritingHeaderToAnotherExpectedBindingFailsAuthentication(string field)
    {
        var record = Guid.NewGuid(); var op = Guid.NewGuid(); var actor = Guid.NewGuid(); var authority = ProviderType.SQLLiteDBOASIS;
        var resolver = new Resolver(); var envelope = HolonPreparationCipher.Encrypt(record, op, actor, authority, "{}", Policy(resolver));
        if (field == "operationId") op = Guid.NewGuid();
        if (field == "actorId") actor = Guid.NewGuid();
        if (field == "authority") authority = ProviderType.MongoDBOASIS;
        var json = JObject.Parse(envelope.Substring(HolonPreparationCipher.Prefix.Length));
        json[field] = field == "authority" ? new JValue((int)authority) : new JValue((field == "actorId" ? actor : op).ToString("D"));
        Assert.ThrowsAny<CryptographicException>(() => HolonPreparationCipher.Decrypt(record, op, actor, authority,
            HolonPreparationCipher.Prefix + json.ToString(), Policy(resolver)));
        Assert.All(resolver.Leases, lease => Assert.True(lease.IsDisposed));
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public void MetadataAndPreparationCannotBeSubstitutedEvenAfterRewritingProtocolHeader(bool preparationToMetadata)
    {
        var record = Guid.NewGuid(); var op = Guid.NewGuid(); var actor = Guid.NewGuid(); var resolver = new Resolver(); var policy = Policy(resolver);
        if (preparationToMetadata)
        {
            var envelope = HolonPreparationCipher.Encrypt(record, op, actor, ProviderType.SQLLiteDBOASIS, "{}", policy);
            var json = JObject.Parse(envelope.Substring(HolonPreparationCipher.Prefix.Length));
            foreach (var name in new[] { "purpose", "operationId", "actorId", "authority" }) json.Remove(name);
            json["version"] = 2;
            Assert.ThrowsAny<CryptographicException>(() => HolonMetadataCipher.Decrypt(record, HolonMetadataCipher.Prefix + json.ToString(), policy));
        }
        else
        {
            var envelope = HolonMetadataCipher.Encrypt(record, "{}", policy);
            var json = JObject.Parse(envelope.Substring(HolonMetadataCipher.Prefix.Length));
            json["version"] = 1; json["purpose"] = "conditional-preparation"; json["operationId"] = op.ToString("D");
            json["actorId"] = actor.ToString("D"); json["authority"] = (int)ProviderType.SQLLiteDBOASIS;
            Assert.ThrowsAny<CryptographicException>(() => HolonPreparationCipher.Decrypt(record, op, actor, ProviderType.SQLLiteDBOASIS,
                HolonPreparationCipher.Prefix + json.ToString(), policy));
        }
    }

    [Fact]
    public void PreparationKeyDenialRemainsAnExplicitError()
    {
        var resolver = new Resolver { Denied = true };
        var error = Assert.Throws<HolonMetadataKeyResolutionException>(() => HolonPreparationCipher.Encrypt(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ProviderType.SQLLiteDBOASIS, "{}", Policy(resolver)));
        Assert.Equal("PREPARATION_DENIED", error.ErrorCode); Assert.Empty(resolver.Leases);
    }

    [Theory]
    [InlineData(ProviderType.Default)] [InlineData(ProviderType.None)] [InlineData(ProviderType.All)]
    public void PreparationRequiresConcreteAuthorityBeforeKeyResolution(ProviderType authority)
    {
        var resolver = new Resolver();
        Assert.Throws<ArgumentException>(() => HolonPreparationCipher.Encrypt(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), authority, "{}", Policy(resolver)));
        Assert.Empty(resolver.Requests);
    }
}
