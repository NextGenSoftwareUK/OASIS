using System;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using MinioProvider = NextGenSoftware.OASIS.API.Providers.MinIOOASIS.MinIOOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

/// <summary>Explicit query-only MinIO enrollment; construction performs no native I/O.</summary>
public static class CqlMinioReadSourceRegistration
{
    private static readonly string[] Names = { "ORIGIN", "BUCKET", "REGION", "PROBE_ID", "ACCESS_KEY", "SECRET_KEY" };

    public static void AddMinioReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var values = Names.Select(name => configuration["CQL_MINIO_QUERY_" + name]).ToArray();
        if (values.All(value => value == null)) return;
        if (values.Any(string.IsNullOrWhiteSpace))
            throw new InvalidOperationException("MinIO CQL requires all six explicit query-source settings.");
        if (!Uri.TryCreate(values[0], UriKind.Absolute, out var origin) || origin.Scheme != "https" || origin.IsLoopback ||
            origin.AbsolutePath != "/" || origin.UserInfo.Length != 0 || origin.Query.Length != 0 || origin.Fragment.Length != 0)
            throw new InvalidOperationException("MinIO CQL requires a credential-free HTTPS origin.");
        if (!Regex.IsMatch(values[1], "^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$"))
            throw new InvalidOperationException("MinIO CQL requires an explicit DNS-safe teaching bucket.");
        if (!Regex.IsMatch(values[2], "^[a-z0-9][a-z0-9-]{0,62}$"))
            throw new InvalidOperationException("MinIO CQL requires an explicit native signing region.");
        if (!Guid.TryParseExact(values[3], "D", out var probe) || probe == Guid.Empty)
            throw new InvalidOperationException("MinIO CQL requires an existing live teaching probe identity.");
        // Every activation uses this same transport policy, including after native SDK disposal.
        IAmazonS3 CreateClient() => new AmazonS3Client(
            new BasicAWSCredentials(values[4], values[5]), new AmazonS3Config {
                ServiceURL = origin.AbsoluteUri, AuthenticationRegion = values[2], ForcePathStyle = true,
                MaxErrorRetry = 0, HttpClientFactory = new GuardedClientFactory()
            });
        services.AddSingleton<CqlReadSourceProfile>(provider => new CqlReadSourceProfile(new MinioProvider(
            origin.AbsoluteUri, values[4], values[5], values[1], true, probe,
            clientFactory: CreateClient)));
    }

    private sealed class GuardedClientFactory : Amazon.Runtime.HttpClientFactory
    {
        public override HttpClient CreateHttpClient(IClientConfig clientConfig)
            => new(NativeConnectionGuard.CreateGuardedHttpHandler());
    }
}
