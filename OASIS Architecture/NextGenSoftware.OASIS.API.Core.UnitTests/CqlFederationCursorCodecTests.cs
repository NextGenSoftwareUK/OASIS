using Microsoft.AspNetCore.DataProtection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;
using Xunit;

namespace NextGenSoftware.OASIS.API.Core.UnitTests;

public class CqlFederationCursorCodecTests
{
    private static readonly Guid Actor = Guid.NewGuid();
    private static readonly string Deployment = new('b', 64);
    private static readonly DateTimeOffset Issued = new(2026, 10, 26, 0, 0, 0, TimeSpan.Zero);
    private static CqlFederationCursorState State(int remaining = 100) => new(Actor, new string('a', 64), Deployment,
        Guid.NewGuid(), Guid.NewGuid(), Issued, Issued.AddMinutes(5), 25,
        new[] { ProviderType.MongoDBOASIS, ProviderType.SQLLiteDBOASIS }, remaining, new[] { remaining, remaining },
        remaining, remaining, remaining, Math.Min(64, remaining), Math.Min(30000, remaining));

    [Fact]
    public void ProtectedStateRoundTripsAllBindingsAndHasFixedLengthAcrossRemainingBudgets()
    {
        var codec = new CqlFederationCursorCodec(new EphemeralDataProtectionProvider());
        var state = State();
        var token = codec.Protect(state);
        Assert.False(token.IsError, token.Exception?.ToString());
        var decoded = codec.Unprotect(token.Result, Actor, Deployment, Issued.AddSeconds(1));
        Assert.False(decoded.IsError, decoded.Exception?.ToString());
        Assert.Equal(state.Intent, decoded.Result.Intent);
        Assert.Equal(state.Lineage, decoded.Result.Lineage);
        Assert.Equal(state.NextRequestKey, decoded.Result.NextRequestKey);
        Assert.Equal(state.Expires, decoded.Result.Expires);
        Assert.Equal(state.Offset, decoded.Result.Offset);
        Assert.Equal(state.Providers, decoded.Result.Providers);
        Assert.Equal(state.ProviderRows, decoded.Result.ProviderRows);
        Assert.Equal(state.Work, decoded.Result.Work);
        Assert.Equal(state.Attempts, decoded.Result.Attempts);
        Assert.Equal(state.InputBytes, decoded.Result.InputBytes);
        Assert.Equal(state.OutputBytes, decoded.Result.OutputBytes);
        Assert.Equal(state.DurationMilliseconds, decoded.Result.DurationMilliseconds);
        Assert.Equal(token.Result.Length, codec.Protect(State(0)).Result.Length);
        Assert.NotEqual(token.Result, codec.Protect(state).Result);
        Assert.DoesNotContain(state.Intent, token.Result);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)] [InlineData(5)]
    public void TamperedForeignExpiredOrNoncanonicalTokensFailWithoutState(int fault)
    {
        var codec = new CqlFederationCursorCodec(new EphemeralDataProtectionProvider());
        var token = codec.Protect(State()).Result;
        var actor = Actor; var deployment = Deployment; var now = Issued.AddSeconds(1);
        switch (fault)
        {
            case 0: token = (token[0] == 'A' ? "B" : "A") + token[1..]; break;
            case 1: actor = Guid.NewGuid(); break;
            case 2: deployment = new string('c', 64); break;
            case 3: now = Issued.AddMinutes(5); break;
            case 4: now = Issued.AddTicks(-1); break;
            case 5: token += "="; break;
        }
        var result = codec.Unprotect(token, actor, deployment, now);
        Assert.Equal("CQL_CURSOR_INVALID", result.ErrorCode);
        Assert.Null(result.Result);
    }

    [Fact]
    public void DifferentKeyOwnerCannotReadTheToken()
    {
        var writer = new CqlFederationCursorCodec(new EphemeralDataProtectionProvider());
        var reader = new CqlFederationCursorCodec(new EphemeralDataProtectionProvider());
        Assert.True(reader.Unprotect(writer.Protect(State()).Result, Actor, Deployment, Issued.AddSeconds(1)).IsError);
    }

    [Fact]
    public void ATokenFromAnotherPurposeIsRejectedEvenWithTheSameKeyOwner()
    {
        var provider = new EphemeralDataProtectionProvider();
        var codec = new CqlFederationCursorCodec(provider);
        var token = codec.Protect(State()).Result;
        var encoded = token.Replace('-', '+').Replace('_', '/');
        encoded += new string('=', (4 - encoded.Length % 4) % 4);
        var payload = provider.CreateProtector("OASIS.CQL.Federation.Cursor.v1").Unprotect(Convert.FromBase64String(encoded));
        var foreign = Convert.ToBase64String(provider.CreateProtector("OASIS.CQL.Federation.Cursor.v2").Protect(payload))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal("CQL_CURSOR_INVALID", codec.Unprotect(foreign, Actor, Deployment, Issued).ErrorCode);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)]
    public void AuthenticatedButUnknownTruncatedOrNoncanonicalLayoutIsRejected(int fault)
    {
        var provider = new EphemeralDataProtectionProvider();
        var codec = new CqlFederationCursorCodec(provider);
        var protector = provider.CreateProtector("OASIS.CQL.Federation.Cursor.v1");
        var token = codec.Protect(State()).Result;
        var encoded = token.Replace('-', '+').Replace('_', '/');
        encoded += new string('=', (4 - encoded.Length % 4) % 4);
        var payload = protector.Unprotect(Convert.FromBase64String(encoded));
        if (fault == 0) payload[0] = 2;
        else if (fault == 1) payload = payload[..^1];
        else payload[134 + 8] = 1; // unused third provider slot must be zero
        var modified = Convert.ToBase64String(protector.Protect(payload)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Assert.Equal("CQL_CURSOR_INVALID", codec.Unprotect(modified, Actor, Deployment, Issued.AddSeconds(1)).ErrorCode);
    }

    [Fact]
    public void MutableInputArraysCannotChangeCapturedState()
    {
        var providers = new[] { ProviderType.MongoDBOASIS };
        var rows = new[] { 1 };
        var state = new CqlFederationCursorState(Actor, new string('a', 64), Deployment, Guid.NewGuid(), Guid.NewGuid(),
            Issued, Issued.AddMinutes(1), 1, providers, 1, rows, 1, 1, 1, 1, 1);
        providers[0] = ProviderType.Neo4jOASIS; rows[0] = 999;
        Assert.Equal(ProviderType.MongoDBOASIS, state.Providers[0]);
        Assert.Equal(1, state.ProviderRows[0]);
    }

    [Theory]
    [InlineData("")] [InlineData("not a token")] [InlineData("A")]
    public void InvalidEncodingNeverProducesState(string token)
    {
        var codec = new CqlFederationCursorCodec(new EphemeralDataProtectionProvider());
        Assert.Equal("CQL_CURSOR_INVALID", codec.Unprotect(token, Actor, Deployment, Issued).ErrorCode);
        Assert.Equal("CQL_CURSOR_INVALID", codec.Unprotect(new string('A', 4097), Actor, Deployment, Issued).ErrorCode);
    }

    [Theory]
    [InlineData(0)] [InlineData(1)] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ServerStateRejectsUnboundedOrIncompleteBindings(int fault)
    {
        Assert.Throws<ArgumentException>(() => new CqlFederationCursorState(fault == 0 ? Guid.Empty : Actor,
            new string('a', 64), Deployment, Guid.NewGuid(), Guid.NewGuid(), Issued,
            Issued.AddMinutes(fault == 1 ? 6 : 1), 1,
            fault == 3 ? new[] { ProviderType.MongoDBOASIS, ProviderType.MongoDBOASIS } : new[] { ProviderType.MongoDBOASIS },
            fault == 2 ? -1 : 1, fault == 4 ? Array.Empty<int>() : new[] { 1 }, 1, 1, 1, 1, 1));
    }
}
