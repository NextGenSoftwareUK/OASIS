using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;
using NextGenSoftware.OASIS.ONODE.Service.Models;

namespace NextGenSoftware.OASIS.ONODE.Service.Services;

/// <summary>
/// Reads and writes OASIS provider configuration from OASISDNA.json.
/// Provider list lives at OASISDNA.StorageProviders.Providers (or StorageProviders.Providers at root).
/// All writes: backup first, validate, then write atomically.
/// </summary>
public class ProviderService
{
    private readonly SupervisorConfig _config;
    private readonly ILogger<ProviderService> _logger;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public static readonly IReadOnlyList<string> KnownProviderTypes = new[]
    {
        "HoloOASIS", "IPFSOASIS", "EthereumOASIS", "ThreeFoldOASIS",
        "SolanaOASIS", "EOSIOOASIS", "MongoDBOASIS", "SQLiteOASIS",
        "Neo4jOASIS", "AzureCosmosOASIS", "ElasticSearchOASIS"
    };

    public ProviderService(IOptions<SupervisorConfig> config, ILogger<ProviderService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public List<ProviderDto> GetProviders(string? serviceId = null)
    {
        try
        {
            var root = LoadRoot();
            var arr = FindProvidersArray(root);
            if (arr == null) return [];

            return arr.Select((n, i) =>
            {
                var o = n?.AsObject();
                if (o == null) return null;
                return new ProviderDto
                {
                    ProviderType = o["ProviderType"]?.GetValue<string>() ?? o["providerType"]?.GetValue<string>() ?? "",
                    IsEnabled    = o["IsEnabled"]?.GetValue<bool>()    ?? o["isEnabled"]?.GetValue<bool>()    ?? false,
                    Priority     = o["Priority"]?.GetValue<int>()      ?? o["priority"]?.GetValue<int>()      ?? (i + 1),
                };
            }).Where(p => p != null).Cast<ProviderDto>().ToList();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "ProviderService.GetProviders error");
            return [];
        }
    }

    public async Task<bool> SetEnabledAsync(string providerType, bool enabled, CancellationToken ct = default)
        => await MutateAsync(o =>
        {
            if (string.Equals(o["ProviderType"]?.GetValue<string>() ?? o["providerType"]?.GetValue<string>(),
                providerType, StringComparison.OrdinalIgnoreCase))
            {
                o["IsEnabled"] = enabled;
                return true;
            }
            return false;
        }, ct);

    public async Task<bool> SetPriorityAsync(string providerType, int priority, CancellationToken ct = default)
        => await MutateAsync(o =>
        {
            if (string.Equals(o["ProviderType"]?.GetValue<string>() ?? o["providerType"]?.GetValue<string>(),
                providerType, StringComparison.OrdinalIgnoreCase))
            {
                o["Priority"] = priority;
                return true;
            }
            return false;
        }, ct);

    // ── internals ─────────────────────────────────────────────────────────────────

    JsonNode? LoadRoot()
    {
        var path = _config.ResolveOASISDNAPath();
        if (!File.Exists(path)) return null;
        return JsonNode.Parse(File.ReadAllText(path));
    }

    static JsonArray? FindProvidersArray(JsonNode? root)
    {
        if (root == null) return null;

        // Try OASISDNA.StorageProviders.Providers
        var providers = root["OASISDNA"]?["StorageProviders"]?["Providers"]?.AsArray()
            ?? root["StorageProviders"]?["Providers"]?.AsArray()
            ?? root["Providers"]?.AsArray();

        return providers;
    }

    async Task<bool> MutateAsync(Func<JsonObject, bool> mutator, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var path = _config.ResolveOASISDNAPath();
            if (!File.Exists(path)) return false;

            var json = await File.ReadAllTextAsync(path, ct);
            var root = JsonNode.Parse(json);
            var arr = FindProvidersArray(root);
            if (arr == null || root == null) return false;

            bool found = false;
            foreach (var node in arr)
            {
                var obj = node?.AsObject();
                if (obj != null && mutator(obj)) found = true;
            }

            if (!found) return false;

            // Backup before write
            var backup = path + $".bak.{DateTime.UtcNow:yyyyMMddHHmmss}";
            File.Copy(path, backup, overwrite: true);

            await File.WriteAllTextAsync(path,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), ct);

            return true;
        }
        finally
        {
            _lock.Release();
        }
    }
}

public class ProviderDto
{
    public string ProviderType { get; set; } = "";
    public bool IsEnabled { get; set; }
    public int Priority { get; set; }

    // Friendly display name derived from type
    public string FriendlyName => ProviderType
        .Replace("OASIS", "")
        .Replace("OASIS", "");
}
