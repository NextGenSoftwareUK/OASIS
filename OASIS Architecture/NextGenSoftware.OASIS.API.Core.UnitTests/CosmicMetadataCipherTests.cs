using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NextGenSoftware.OASIS.API.Core.Security;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CosmicMetadataCipherTests
{
    private sealed class Resolver : IHolonMetadataKeyResolver
    {
        public Dictionary<string, byte[]> Keys = new() { ["key-a"] = Enumerable.Repeat((byte)17,32).ToArray(), ["key-b"] = Enumerable.Repeat((byte)34,32).ToArray() };
        public readonly List<HolonMetadataKeyRequest> Requests = new();
        public readonly List<HolonMetadataKeyMaterial> Leases = new();
        public bool Denied;
        public OASISResult<HolonMetadataKeyMaterial> ResolveKey(HolonMetadataKeyRequest request)
        {
            Requests.Add(request);
            if (Denied || !Keys.TryGetValue(request.KeyId, out var bytes))
                return new() { IsError = true, ErrorCode = "KEY_NOT_AUTHORIZED", Message = "resolver-private-details" };
            var lease = new HolonMetadataKeyMaterial(bytes); Leases.Add(lease);
            return new(lease);
        }
    }
    private static HolonMetadataEncryptionPolicy Policy(Resolver resolver, string key = "key-a", string context = "tenant-scope")
        => new(context, key, resolver);
    private static string Edit(string envelope, Action<JObject> edit)
    {
        var json = JObject.Parse(envelope.Substring(HolonMetadataCipher.Prefix.Length)); edit(json);
        return HolonMetadataCipher.Prefix + json.ToString(Newtonsoft.Json.Formatting.None);
    }
    private static string Flip(string value)
    { var bytes = Convert.FromBase64String(value); bytes[0] ^= 1; return Convert.ToBase64String(bytes); }

    [Fact]
    public void AuthenticatedEnvelopeRoundTripsExactJsonAndDisposesOwnedKeys()
    {
        var resolver = new Resolver(); var policy = Policy(resolver); var id = Guid.NewGuid();
        const string json = "{\"classified\":\"☕\",\"amount\":1.00000000000000000000000000000000000000001}";
        var first = HolonMetadataCipher.Encrypt(id, json, policy);
        var second = HolonMetadataCipher.Encrypt(id, json, policy);
        Assert.NotEqual(first, second); Assert.DoesNotContain("classified", first);
        Assert.Equal(json, HolonMetadataCipher.Decrypt(id, first, policy));
        Assert.Equal(3, resolver.Requests.Count);
        Assert.Equal(HolonMetadataKeyPurpose.Encrypt, resolver.Requests[0].Purpose);
        Assert.Equal(HolonMetadataKeyPurpose.Decrypt, resolver.Requests[2].Purpose);
        Assert.All(resolver.Requests, request => { Assert.Equal(id, request.RecordId); Assert.Equal("tenant-scope", request.ContextId); Assert.Equal("key-a", request.KeyId); });
        Assert.All(resolver.Leases, lease => Assert.True(lease.IsDisposed));
        Assert.All(resolver.Keys["key-a"], value => Assert.Equal((byte)17, value));
    }

    [Fact]
    public void FreshResolverCanReadRetainedKeyAfterWriteKeyRotation()
    {
        var id = Guid.NewGuid(); var writer = new Resolver();
        var old = HolonMetadataCipher.Encrypt(id, "{\"value\":1}", Policy(writer));
        var restarted = new Resolver(); var currentPolicy = Policy(restarted, "key-b");
        Assert.Equal("{\"value\":1}", HolonMetadataCipher.Decrypt(id, old, currentPolicy));
        Assert.Equal("key-a", restarted.Requests.Single().KeyId);
        var current = HolonMetadataCipher.Encrypt(id, "{\"value\":2}", currentPolicy);
        Assert.Equal("key-b", restarted.Requests.Last().KeyId);
        Assert.Equal("{\"value\":2}", HolonMetadataCipher.Decrypt(id, current, currentPolicy));
    }

    [Theory]
    [InlineData("record")] [InlineData("context")] [InlineData("key")]
    [InlineData("nonce")] [InlineData("tag")] [InlineData("ciphertext")]
    public void AuthenticatedBindingRejectsHeaderOrCiphertextTampering(string field)
    {
        var resolver = new Resolver(); resolver.Keys["key-b"] = resolver.Keys["key-a"];
        var id = Guid.NewGuid(); var policy = Policy(resolver);
        var envelope = HolonMetadataCipher.Encrypt(id, "{\"value\":1}", policy);
        var readId = field == "record" ? Guid.NewGuid() : id;
        var readPolicy = field == "context" ? Policy(resolver, context:"other-scope") : policy;
        var changed = Edit(envelope, json => {
            if (field == "record") json["recordId"] = readId.ToString("D");
            else if (field == "context") json["contextId"] = readPolicy.ContextId;
            else if (field == "key") json["keyId"] = "key-b";
            else json[field] = Flip(json[field]!.Value<string>()!);
        });
        Assert.ThrowsAny<CryptographicException>(() => HolonMetadataCipher.Decrypt(readId, changed, readPolicy));
        Assert.All(resolver.Leases, lease => Assert.True(lease.IsDisposed));
    }

    [Theory]
    [InlineData("owner")] [InlineData("scope")] [InlineData("version")]
    [InlineData("algorithm")] [InlineData("unknown")] [InlineData("duplicate")]
    [InlineData("nonce-length")] [InlineData("tag-length")] [InlineData("base64")]
    public void InvalidEnvelopeIsRejectedBeforeCallingTheResolver(string fault)
    {
        var resolver = new Resolver(); var id = Guid.NewGuid(); var policy = Policy(resolver);
        var envelope = HolonMetadataCipher.Encrypt(id,"{}",policy); resolver.Requests.Clear();
        if (fault == "duplicate") envelope = envelope.Insert(envelope.IndexOf('{') + 1,"\"version\":2,");
        else envelope = Edit(envelope, json => {
            if (fault == "version") json["version"] = 2.0m;
            else if (fault == "algorithm") json["algorithm"] = "unknown";
            else if (fault == "unknown") json["extra"] = true;
            else if (fault == "nonce-length") json["nonce"] = "";
            else if (fault == "tag-length") json["tag"] = "";
            else if (fault == "base64") json["nonce"] = " " + json["nonce"]!.Value<string>();
        });
        var readId = fault == "owner" ? Guid.NewGuid() : id;
        var readPolicy = fault == "scope" ? Policy(resolver,context:"wrong-scope") : policy;
        Assert.ThrowsAny<System.IO.InvalidDataException>(() => HolonMetadataCipher.Decrypt(readId,envelope,readPolicy));
        Assert.Empty(resolver.Requests);
    }

    [Fact]
    public void AuthorizationFailureIsExplicitAndDoesNotExposeResolverDetails()
    {
        var resolver = new Resolver(); var id = Guid.NewGuid(); var policy = Policy(resolver);
        var envelope = HolonMetadataCipher.Encrypt(id,"{}",policy); resolver.Denied = true;
        var error = Assert.Throws<HolonMetadataKeyResolutionException>(() => HolonMetadataCipher.Decrypt(id,envelope,policy));
        Assert.Equal("KEY_NOT_AUTHORIZED",error.ErrorCode); Assert.DoesNotContain("resolver-private-details",error.ToString());
        Assert.All(resolver.Leases,lease=>Assert.True(lease.IsDisposed));
    }

    [Fact]
    public void KeyMaterialOwnsItsBufferAndErasesItOnDispose()
    {
        var input = Enumerable.Repeat((byte)19,32).ToArray(); var lease = new HolonMetadataKeyMaterial(input); var owned = lease.Bytes;
        Assert.NotSame(input,owned); lease.Dispose(); lease.Dispose();
        Assert.All(owned,value=>Assert.Equal((byte)0,value)); Assert.All(input,value=>Assert.Equal((byte)19,value));
        Assert.Throws<ObjectDisposedException>(()=>lease.Bytes);
    }

    [Fact]
    public void BoundsAndInvalidUnicodeFailBeforeResolvingKeys()
    {
        var resolver = new Resolver(); var policy = Policy(resolver); var id = Guid.NewGuid();
        Assert.Throws<System.IO.InvalidDataException>(()=>HolonMetadataCipher.Encrypt(id,new string('x',HolonMetadataCipher.MaximumPlaintextBytes+1),policy));
        Assert.Throws<EncoderFallbackException>(()=>HolonMetadataCipher.Encrypt(id,"\ud800",policy));
        Assert.Throws<System.IO.InvalidDataException>(()=>HolonMetadataCipher.Decrypt(id,HolonMetadataCipher.Prefix+new string('x',2*1024*1024),policy));
        Assert.Throws<ArgumentException>(()=>HolonMetadataCipher.Encrypt(Guid.Empty,"{}",policy)); Assert.Empty(resolver.Requests);
    }

    [Fact]
    public void PolicyCapturesQueryableKeysAndRejectsInvalidReferences()
    {
        var resolver = new Resolver(); var keys = new List<string>{"Status"};
        var policy = new HolonMetadataEncryptionPolicy("scope","key-a",resolver,keys); keys[0]="changed";
        Assert.Equal("Status",Assert.Single(policy.AdditionalQueryableKeys));
        Assert.Throws<ArgumentException>(()=>new HolonMetadataEncryptionPolicy("\ud800","key-a",resolver));
        Assert.Throws<ArgumentException>(()=>new HolonMetadataEncryptionPolicy("scope"," ",resolver));
        Assert.Throws<ArgumentException>(()=>new HolonMetadataEncryptionPolicy("scope","key-a",resolver,new[]{"Status","Status"}));
    }
}
