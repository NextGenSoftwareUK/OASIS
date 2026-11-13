using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CosmicCqlDemo;

/// <summary>Non-secret operator admission for a bounded hosted read demonstration.</summary>
public sealed class HostedCosmosReadSettings
{
    [JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
    private sealed class WireSettings
    {
        public WireSettings() { }
        public string Endpoint { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public Guid Actor { get; set; }
        public Guid[] ReadableIds { get; set; } = Array.Empty<Guid>();
        public bool FreeAccountSpendingProtectionConfirmed { get; set; }
    }

    public static HostedCosmosReadSettings FromJson(string json)
    {
        var wire = JsonSerializer.Deserialize<WireSettings>(json)
            ?? throw new ArgumentException("A non-secret hosted configuration object is required.");
        return new HostedCosmosReadSettings(wire.Endpoint, wire.DatabaseName, wire.Actor,
            wire.ReadableIds, wire.FreeAccountSpendingProtectionConfirmed);
    }

    public Uri Endpoint { get; }
    public string DatabaseName { get; }
    public Guid Actor { get; }
    public ReadOnlyCollection<Guid> ReadableIds { get; }

    public HostedCosmosReadSettings(string endpoint, string databaseName, Guid actor,
        IEnumerable<Guid> readableIds, bool freeAccountSpendingProtectionConfirmed)
    {
        // This confirmation records an operator prerequisite; it does not inspect
        // Azure billing or make the subscription free. No default opt-in exists.
        if (!freeAccountSpendingProtectionConfirmed)
            throw new ArgumentException("Confirm the free account's active spending protection before hosted requests.");
        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https" ||
            !uri.IsDefaultPort || uri.IsLoopback || !string.IsNullOrEmpty(uri.UserInfo) ||
            !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) || uri.AbsolutePath != "/")
            throw new ArgumentException("A canonical hosted HTTPS endpoint is required.");

        // This first acceptance profile explicitly targets commercial Azure's
        // standard account endpoint. Sovereign/custom/private endpoints need their
        // own enrollment; never treat a localhost emulator as hosted evidence.
        const string suffix = ".documents.azure.com";
        var host = uri.IdnHost;
        var account = host.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)
            ? host[..^suffix.Length] : "";
        if (account.Length < 3 || account.Length > 44 || account[0] == '-' || account[^1] == '-' ||
            account.Any(character => !(character is >= 'a' and <= 'z' or >= '0' and <= '9' or '-')))
            throw new ArgumentException("Use the enrolled commercial Azure Cosmos account endpoint.");
        if (string.IsNullOrWhiteSpace(databaseName) || databaseName != databaseName.Trim() ||
            databaseName.Length > 255 || databaseName.Any(character => char.IsControl(character) || "/\\?#".Contains(character)))
            throw new ArgumentException("Supply the existing owned database name.");
        if (actor == Guid.Empty) throw new ArgumentException("A trusted actor identity is required.");
        var ids = readableIds?.Take(9).ToArray() ?? throw new ArgumentNullException(nameof(readableIds));
        if (ids.Length < 3 || ids.Length > 8 || ids.Any(id => id == Guid.Empty) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("Supply three to eight distinct owned synthetic record IDs.");

        Endpoint = uri;
        DatabaseName = databaseName;
        Actor = actor;
        ReadableIds = Array.AsReadOnly(ids);
    }
}
