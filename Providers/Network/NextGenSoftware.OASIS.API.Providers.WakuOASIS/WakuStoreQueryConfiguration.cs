#nullable enable
using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.WakuOASIS;

/// <summary>Operator configuration for the fixed loopback Waku teaching store.</summary>
public static class WakuStoreQueryConfiguration
{
    public static WakuStoreQuerySource CreateSource(string json) => CreateSource(json, handler: null);

    internal static WakuStoreQuerySource CreateSource(string json, HttpMessageHandler? handler)
    {
        if (json is null || json.Length > 1024)
            throw new ArgumentException("Waku CQL requires a bounded teaching source descriptor.");

        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 3 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object ||
            !root.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal)
                .SequenceEqual(new[] { "messageHashes", "sourceOwner" }) ||
            root.GetProperty("sourceOwner").ValueKind != JsonValueKind.String ||
            !Guid.TryParseExact(root.GetProperty("sourceOwner").GetString(), "D", out var owner) ||
            owner == Guid.Empty || owner.ToString("D") != root.GetProperty("sourceOwner").GetString())
            throw new ArgumentException("Waku CQL requires a canonical source owner and reviewed message hashes.");

        var hashes = root.GetProperty("messageHashes");
        if (hashes.ValueKind != JsonValueKind.Array || hashes.GetArrayLength() != 3 ||
            hashes.EnumerateArray().Any(hash => hash.ValueKind != JsonValueKind.String))
            throw new ArgumentException("Waku CQL requires exactly three reviewed message hashes.");

        var reviewed = hashes.EnumerateArray().Select(hash => hash.GetString()!).ToArray();
        if (reviewed.Distinct(StringComparer.Ordinal).Count() != reviewed.Length)
            throw new ArgumentException("Waku CQL requires distinct reviewed message hashes.");

        return new WakuStoreQuerySource(owner, reviewed, handler, verifyCatalogue: true);
    }
}
