using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.DataProtection;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

// Server-only state: never bind this object from request JSON. Offsets describe
// an explicitly moving complete-source reread, not a distributed snapshot.
internal sealed class CqlFederationCursorState
{
    internal Guid Actor { get; }
    internal string Intent { get; }
    internal string Deployment { get; }
    internal Guid Lineage { get; }
    internal Guid NextRequestKey { get; }
    internal DateTimeOffset Issued { get; }
    internal DateTimeOffset Expires { get; }
    internal int Offset { get; }
    internal ReadOnlyCollection<ProviderType> Providers { get; }
    internal int InputRows { get; }
    internal ReadOnlyCollection<int> ProviderRows { get; }
    internal int InputBytes { get; }
    internal int OutputBytes { get; }
    internal int Work { get; }
    internal int Attempts { get; }
    internal int DurationMilliseconds { get; }

    internal CqlFederationCursorState(Guid actor, string intent, string deployment, Guid lineage, Guid nextRequestKey,
        DateTimeOffset issued, DateTimeOffset expires, int offset, ProviderType[] providers,
        int inputRows, int[] providerRows, int inputBytes, int outputBytes, int work, int attempts, int durationMilliseconds)
    {
        bool Digest(string value) => value != null && value.Length == 64 && value.All(character =>
            character is >= '0' and <= '9' or >= 'a' and <= 'f');
        if (actor == Guid.Empty || lineage == Guid.Empty || nextRequestKey == Guid.Empty || !Digest(intent) || !Digest(deployment)
            || issued.Offset != TimeSpan.Zero || expires.Offset != TimeSpan.Zero || expires <= issued || expires - issued > TimeSpan.FromMinutes(5)
            || offset < 0 || offset > 1000000 || providers == null || providers.Length is < 1 or > 3
            || providers.Distinct().Count() != providers.Length || providers.Any(provider => provider is not
                (ProviderType.MongoDBOASIS or ProviderType.SQLLiteDBOASIS or ProviderType.Neo4jOASIS))
            || providerRows == null || providerRows.Length != providers.Length || providerRows.Any(rows => rows is < 0 or > 10000)
            || inputRows is < 0 or > 10000 || inputBytes is < 0 or > 16777216 || outputBytes is < 0 or > 16777216
            || work is < 0 or > 10000000 || attempts is < 0 or > 64 || durationMilliseconds is < 0 or > 30000)
            throw new ArgumentException("Continuation state requires complete bounded server-owned bindings.");
        Actor = actor; Intent = intent; Deployment = deployment; Lineage = lineage; NextRequestKey = nextRequestKey;
        Issued = issued; Expires = expires; Offset = offset; Providers = Array.AsReadOnly((ProviderType[])providers.Clone());
        InputRows = inputRows; ProviderRows = Array.AsReadOnly((int[])providerRows.Clone()); InputBytes = inputBytes;
        OutputBytes = outputBytes; Work = work; Attempts = attempts; DurationMilliseconds = durationMilliseconds;
    }
}

/// <summary>Host-supplied Data Protection owns encryption, authentication and key rotation.
/// No keys, filesystem provider or HTTP cursor enrollment are created here.</summary>
internal sealed class CqlFederationCursorCodec
{
    private const int PayloadBytes = 182;
    private const int MaximumTokenCharacters = 4096;
    private readonly IDataProtector protector;
    internal CqlFederationCursorCodec(IDataProtectionProvider provider)
        => protector = (provider ?? throw new ArgumentNullException(nameof(provider))).CreateProtector("OASIS.CQL.Federation.Cursor.v1");

    internal OASISResult<string> Protect(CqlFederationCursorState state)
    {
        try
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true))
            {
                writer.Write((byte)1);
                writer.Write(state.Actor.ToByteArray()); writer.Write(Convert.FromHexString(state.Intent));
                writer.Write(Convert.FromHexString(state.Deployment)); writer.Write(state.Lineage.ToByteArray());
                writer.Write(state.NextRequestKey.ToByteArray()); writer.Write(state.Issued.UtcTicks); writer.Write(state.Expires.UtcTicks);
                writer.Write(state.Offset); writer.Write((byte)state.Providers.Count);
                for (var index = 0; index < 3; index++) writer.Write(index < state.Providers.Count ? (int)state.Providers[index] : 0);
                writer.Write(state.InputRows);
                for (var index = 0; index < 3; index++) writer.Write(index < state.ProviderRows.Count ? state.ProviderRows[index] : 0);
                writer.Write(state.InputBytes); writer.Write(state.OutputBytes); writer.Write(state.Work);
                writer.Write(state.Attempts); writer.Write(state.DurationMilliseconds);
            }
            if (stream.Length != PayloadBytes) throw new InvalidOperationException("Cursor wire layout changed without a version change.");
            var token = Encode(protector.Protect(stream.ToArray()));
            if (token.Length > MaximumTokenCharacters) throw new InvalidOperationException("Protected cursor exceeds its wire bound.");
            return new OASISResult<string>(token);
        }
        catch (Exception cause) { return Error<string>("CQL_CURSOR_PROTECTION_FAILED", cause); }
    }

    internal OASISResult<CqlFederationCursorState> Unprotect(string token, Guid actor, string deployment, DateTimeOffset now)
    {
        try
        {
            if (actor == Guid.Empty || now.Offset != TimeSpan.Zero || string.IsNullOrEmpty(token) || token.Length > MaximumTokenCharacters
                || token.Any(character => !(character is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_')))
                throw new ArgumentException("Malformed cursor encoding.");
            var encoded = token.Replace('-', '+').Replace('_', '/');
            encoded += new string('=', (4 - encoded.Length % 4) % 4);
            var protectedBytes = Convert.FromBase64String(encoded);
            if (Encode(protectedBytes) != token) throw new ArgumentException("Noncanonical cursor encoding.");
            var bytes = protector.Unprotect(protectedBytes);
            if (bytes.Length != PayloadBytes) throw new ArgumentException("Unexpected protected cursor layout.");
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new BinaryReader(stream);
            if (reader.ReadByte() != 1) throw new ArgumentException("Unknown cursor version.");
            var principal = new Guid(reader.ReadBytes(16));
            var intent = Convert.ToHexString(reader.ReadBytes(32)).ToLowerInvariant();
            var version = Convert.ToHexString(reader.ReadBytes(32)).ToLowerInvariant();
            var lineage = new Guid(reader.ReadBytes(16)); var key = new Guid(reader.ReadBytes(16));
            var issued = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero); var expires = new DateTimeOffset(reader.ReadInt64(), TimeSpan.Zero);
            var offset = reader.ReadInt32(); var count = reader.ReadByte();
            if (count is < 1 or > 3) throw new ArgumentException("Invalid cursor target count.");
            var providers = new ProviderType[count]; var providerRows = new int[count];
            for (var index = 0; index < 3; index++)
            {
                var provider = reader.ReadInt32();
                if (index < count) providers[index] = (ProviderType)provider;
                else if (provider != 0) throw new ArgumentException("Noncanonical unused provider slot.");
            }
            var inputRows = reader.ReadInt32();
            for (var index = 0; index < 3; index++)
            {
                var rows = reader.ReadInt32();
                if (index < count) providerRows[index] = rows;
                else if (rows != 0) throw new ArgumentException("Noncanonical unused row slot.");
            }
            var state = new CqlFederationCursorState(principal, intent, version, lineage, key, issued, expires, offset,
                providers, inputRows, providerRows, reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());
            if (stream.Position != stream.Length || state.Actor != actor || state.Deployment != deployment
                || now < state.Issued || now >= state.Expires)
                throw new ArgumentException("Cursor principal, deployment or lifetime no longer matches.");
            return new OASISResult<CqlFederationCursorState>(state);
        }
        catch (Exception cause) { return Error<CqlFederationCursorState>("CQL_CURSOR_INVALID", cause); }
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static OASISResult<T> Error<T>(string code, Exception cause)
        => new() { IsError = true, ErrorCount = 1, ErrorCode = code, Message = "Protected continuation state was rejected.", Exception = cause };
}
