using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.RavenDBOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlRavenDbReadSourceRegistration
{
    private const string CertificateKey = "cql-ravendb-reader";
    private const string CertificatePath = "/data/cql-secrets/ravendb/reader.pfx";

    public static void AddRavenDbReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        const string prefix = "CQL_RAVENDB_QUERY_";
        var endpoint = configuration[prefix + "ORIGIN"];
        var digest = configuration[prefix + "CERTIFICATE_SHA256"];
        var password = configuration[prefix + "CERTIFICATE_PASSWORD"];
        var probe = configuration[prefix + "PROBE_ID"];
        if (endpoint == null && digest == null && password == null && probe == null) return;

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
            origin.HostNameType != UriHostNameType.Dns || origin.Host.Length > 253 ||
            !origin.Host.EndsWith(".ravendb.cloud", StringComparison.Ordinal) ||
            origin.Host.Split('.').Any(label => label.Length is < 1 or > 63 || label[0] == '-' || label[^1] == '-' ||
                label.Any(c => !(char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '-'))) ||
            origin.AbsolutePath != "/" || !origin.IsDefaultPort || origin.UserInfo.Length != 0 ||
            origin.Query.Length != 0 || origin.Fragment.Length != 0 || endpoint != origin.GetLeftPart(UriPartial.Authority) ||
            digest is not { Length: 64 } || digest.Any(c => !char.IsAsciiHexDigitLower(c)) ||
            password is { Length: > 1024 } || !Guid.TryParseExact(probe, "D", out var id) ||
            id == Guid.Empty || probe != id.ToString("D"))
            throw new InvalidOperationException("RavenDB CQL requires a canonical cloud origin, approved certificate digest and probe.");

        services.AddKeyedSingleton<X509Certificate2>(CertificateKey,
            (_, _) => LoadCertificate(CertificatePath, digest, password));
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new RavenDbQuerySource(
            origin, id, provider.GetRequiredKeyedService<X509Certificate2>(CertificateKey))));
    }

    internal static X509Certificate2 LoadCertificate(string path, string digest, string? password)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var length = stream.Length;
        if (length is <= 0 or > 128 * 1024)
            throw new InvalidDataException("The mounted RavenDB certificate exceeds the accepted file size.");
        var bytes = new byte[(int)length];
        try
        {
            stream.ReadExactly(bytes);
            if (stream.ReadByte() != -1 || Convert.ToHexStringLower(SHA256.HashData(bytes)) != digest)
                throw new InvalidDataException("The mounted RavenDB certificate differs from the approved source.");
            var certificate = X509CertificateLoader.LoadPkcs12(bytes, password, X509KeyStorageFlags.EphemeralKeySet);
            if (certificate.HasPrivateKey) return certificate;
            certificate.Dispose();
            throw new InvalidDataException("The RavenDB reader certificate requires its private key.");
        }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }
}
