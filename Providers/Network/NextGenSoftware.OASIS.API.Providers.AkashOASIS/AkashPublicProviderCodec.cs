using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NextGenSoftware.OASIS.API.Providers.AkashOASIS;

/// <summary>Allowlisted indexed-provider observations; no cloud provisioning or node consensus claim.</summary>
public static class AkashPublicProviderCodec
{
    public sealed record Provider(string Owner, string? DisplayName, string HostUri, bool IsOnline);

    public static void ValidateOwner(string value)
    {
        const string prefix = "akash";
        const string alphabet = "qpzry9x8gf2tvdw0s3jn54khce6mua7l";
        //20bytes=32base32symbols, plus fixed HRP, separator and6checksum symbols.
        if (value == null || value.Length != 44 || !value.StartsWith(prefix + "1", StringComparison.Ordinal))
            throw new ArgumentException("Canonical lowercase20byte akash Bech32 owner required.");
        uint checksum = 1;
        void Step(int part)
        {
            var top = checksum >> 25;
            checksum = ((checksum & 0x1ffffff) << 5) ^ (uint)part;
            uint[] generators = { 0x3b6a57b2, 0x26508e6d, 0x1ea119fa, 0x3d4233dd, 0x2a1462b3 };
            for (var i = 0; i < 5; i++) if (((top >> i) & 1) != 0) checksum ^= generators[i];
        }
        foreach (var c in prefix) Step(c >> 5);
        Step(0);
        foreach (var c in prefix) Step(c & 31);
        foreach (var c in value.AsSpan(prefix.Length + 1))
        {
            var digit = alphabet.IndexOf(c);
            if (digit < 0) throw new ArgumentException("Invalid Bech32 owner alphabet.");
            Step(digit);
        }
        if (checksum != 1) throw new ArgumentException("Invalid Bech32 owner checksum.");
    }

    public static Provider Decode(JsonElement value, string expectedOwner)
    {
        ValidateOwner(expectedOwner);
        if (value.ValueKind != JsonValueKind.Object || value.TryGetProperty("error", out _))
            throw new InvalidDataException("Complete successful provider observation required.");
        var fields = new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in value.EnumerateObject())
            if (!fields.Add(field.Name)) throw new InvalidDataException("Duplicate provider field.");
        var owner = value.GetProperty("owner").GetString();
        if (owner != expectedOwner) throw new InvalidDataException("Provider owner differs from requested key.");
        var host = value.GetProperty("hostUri").GetString();
        if (host == null || host.Length > 512 || host.Any(char.IsControl) ||
            !Uri.TryCreate(host, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment))
            throw new InvalidDataException("Bounded credential-free HTTPS service URI required as descriptive text.");
        string? name = null;
        if (value.TryGetProperty("name", out var label) && label.ValueKind != JsonValueKind.Null)
        {
            name = label.GetString();
            if (name == null || Encoding.UTF8.GetByteCount(name) > 256 || name.Any(char.IsControl))
                throw new InvalidDataException("Bounded provider display label required.");
        }
        // Service URI is data only. Never connect to it or copy arbitrary nested
        // payloads, contact email, exact locations, capacity or audit claims.
        return new Provider(owner!, name, host, value.GetProperty("isOnline").GetBoolean());
    }
}
