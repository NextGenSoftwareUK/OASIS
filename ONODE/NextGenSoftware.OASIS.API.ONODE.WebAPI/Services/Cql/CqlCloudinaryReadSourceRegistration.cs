using System;
using System.Linq;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NextGenSoftware.OASIS.API.Providers.CloudinaryOASIS;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

public static class CqlCloudinaryReadSourceRegistration
{
    public static void AddCloudinaryReadSource(this IServiceCollection services, IConfiguration configuration)
    {
        var source = configuration["CQL_CLOUDINARY_QUERY_SOURCE_JSON"];
        if (source == null) return;
        if (Encoding.UTF8.GetByteCount(source) > 4096)
            throw new InvalidOperationException("A bounded Cloudinary image catalogue is required.");
        try
        {
            using var document = JsonDocument.Parse(source, new JsonDocumentOptions { MaxDepth = 3 });
            var root = document.RootElement;
            var fields = new[] { "owner", "assets", "probe" };
            var keys = root.EnumerateObject().Select(property => property.Name).ToArray();
            if (keys.Length != fields.Length || !keys.ToHashSet(StringComparer.Ordinal).SetEquals(fields))
                throw new ArgumentException("Unexpected Cloudinary image catalogue fields.");
            var identity = root.GetProperty("owner").GetString();
            if (!Guid.TryParseExact(identity, "D", out var owner) || owner == Guid.Empty || owner.ToString("D") != identity)
                throw new ArgumentException("A canonical Cloudinary image catalogue owner is required.");
            var assets = root.GetProperty("assets").EnumerateArray().Select(value => value.GetString()).ToArray();
            var probe = root.GetProperty("probe").GetString();
            if (assets.Length is < 1 or > 8 || !assets.Contains(probe, StringComparer.Ordinal) ||
                assets.Select(asset => CloudinaryPublicImageQuerySource.LogicalId(owner, asset)).Distinct().Count() != assets.Length)
                throw new ArgumentException("Unique canonical assets and an enrolled probe are required.");
            services.AddSingleton<CqlReadSourceProfile>(_ =>
                new CqlReadSourceProfile(new CloudinaryPublicImageQuerySource(owner, assets, probe)));
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException)
        {
            throw new InvalidOperationException("Use the reviewed public Cloudinary image catalogue.", error);
        }
    }
}
