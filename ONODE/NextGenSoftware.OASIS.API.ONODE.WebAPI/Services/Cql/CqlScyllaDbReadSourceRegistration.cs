using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.Providers.ScyllaDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlScyllaDbReadSourceRegistration
{
    private const string CaKey = "cql-scylladb-ca";

    public static void AddScyllaDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_SCYLLADB_QUERY_";
        var ip = configuration[prefix + "IP"];
        var host = configuration[prefix + "HOST"];
        var encoded = configuration[prefix + "CA_DER_BASE64"];
        var digest = configuration[prefix + "CA_SHA256"];
        var password = configuration[prefix + "READER_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (ip == null && host == null && encoded == null && digest == null && password == null && probe == null) return;

        if (!IPAddress.TryParse(ip, out var address) || ip != address.ToString() ||
            !NativeConnectionGuard.IsPublicAddress(address) ||
            host is not { Length: > 0 and <= 253 } || !host.EndsWith(".clusters.scylla.cloud", StringComparison.Ordinal) ||
            host.Split('.').Any(label => label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))) ||
            encoded is not { Length: > 0 and <= 32768 } || encoded.Any(char.IsWhiteSpace) ||
            digest is not { Length: 64 } || digest.Any(c => !char.IsAsciiHexDigitLower(c)) ||
            string.IsNullOrWhiteSpace(password) || password.Length > 1024 || password.Any(char.IsControl) ||
            !Guid.TryParseExact(probe, "D", out var id) || id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("ScyllaDB CQL requires one reviewed public node, cluster CA, reader password and probe.");

        services.AddKeyedSingleton<X509Certificate2>(CaKey, (_, _) => LoadCa(encoded, digest));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new ScyllaDbQuerySource(
            address, host, password, provider.GetRequiredKeyedService<X509Certificate2>(CaKey), id)));
    }

    internal static X509Certificate2 LoadCa(string encoded, string digest)
    {
        var bytes = Convert.FromBase64String(encoded);
        if (Convert.ToBase64String(bytes) != encoded || Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
            throw new InvalidDataException("The configured ScyllaDB cluster CA differs from the approved source.");
        return X509CertificateLoader.LoadCertificate(bytes);
    }
}
