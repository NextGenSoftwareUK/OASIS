#nullable enable

using System;
using System.Linq;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.ThreeFoldOASIS;

/// <summary>
/// Persists OASIS data through a ThreeFold QSS S3-compatible gateway using the official AWS S3 SDK.
/// Connection string format:
/// https://qss.example?accessKey=...&amp;secretKey=...&amp;bucket=oasis&amp;useSSL=true
/// </summary>
public sealed class ThreeFoldOASIS : NextGenSoftware.OASIS.API.Providers.MinIOOASIS.MinIOOASIS
{
    private sealed record Settings(string Endpoint, string AccessKey, string SecretKey, string Bucket, bool UseSsl);

    protected override NextGenSoftware.OASIS.API.Core.Enums.ProviderType StorageProviderType
        => NextGenSoftware.OASIS.API.Core.Enums.ProviderType.ThreeFoldOASIS;

    public ThreeFoldOASIS(string connectionString) : this(Parse(connectionString))
    {
    }

    public ThreeFoldOASIS(string endpoint, string accessKey, string secretKey, string bucketName = "oasis", bool useSSL = true)
        : base(endpoint, accessKey, secretKey, bucketName, useSSL)
    {
        ProviderName = "ThreeFoldOASIS";
        ProviderDescription = "ThreeFold QSS provider (S3-compatible object storage via AWSSDK.S3)";
        ProviderType = new EnumValue<ProviderType>(Core.Enums.ProviderType.ThreeFoldOASIS);
        ProviderCategory = new EnumValue<ProviderCategory>(Core.Enums.ProviderCategory.StorageAndNetwork);
    }

    private ThreeFoldOASIS(Settings settings)
        : this(settings.Endpoint, settings.AccessKey, settings.SecretKey, settings.Bucket, settings.UseSsl)
    {
    }

    private static Settings Parse(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("ThreeFold QSS connection string is required.", nameof(connectionString));

        var uri = new Uri(connectionString);
        var values = uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => Uri.UnescapeDataString(pair[0]), pair => Uri.UnescapeDataString(pair[1]), StringComparer.OrdinalIgnoreCase);
        values.TryGetValue("accessKey", out string? accessKey);
        values.TryGetValue("secretKey", out string? secretKey);
        values.TryGetValue("bucket", out string? bucket);
        values.TryGetValue("useSSL", out string? useSslText);
        if (string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            throw new ArgumentException("ThreeFold QSS accessKey and secretKey are required.", nameof(connectionString));

        return new Settings(uri.GetLeftPart(UriPartial.Authority), accessKey, secretKey,
            string.IsNullOrWhiteSpace(bucket) ? "oasis" : bucket,
            !bool.TryParse(useSslText, out bool useSsl) || useSsl);
    }
}
