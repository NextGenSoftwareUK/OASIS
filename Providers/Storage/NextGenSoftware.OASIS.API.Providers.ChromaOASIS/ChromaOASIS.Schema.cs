using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ChromaOASIS;

public partial class ChromaOASIS
{
    private async Task<string> ResolveCollectionIdAsync(string name)
    {
        if (_collectionIds.TryGetValue(name, out var cached)) return cached;

        // Provisioning is explicit: native errors must never create a replacement collection.
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{CollectionsUrl}/{Uri.EscapeDataString(name)}");
        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
        response.EnsureSuccessStatusCode();
        using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
        using var body = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer, deadline.Token)) != 0)
        {
            if (body.Length + count > 256 * 1024) throw new InvalidDataException("Chroma collection description exceeds256KiB.");
            body.Write(buffer, 0, count);
        }
        using var document = JsonDocument.Parse(body.ToArray());
        var schema = document.RootElement;
        var id = schema.GetProperty("id").GetString();
        if (!Guid.TryParseExact(id, "D", out var uuid) || uuid == Guid.Empty || id != uuid.ToString("D") ||
            schema.GetProperty("name").GetString() != name || schema.GetProperty("tenant").GetString() != _tenant ||
            schema.GetProperty("database").GetString() != _database)
            throw new InvalidDataException("Chroma collection differs from the configured storage authority.");
        var metadata = schema.GetProperty("metadata");
        if (metadata.GetProperty("cosmic_storage_schema").GetString() != "chroma-doc-v1" ||
            metadata.GetProperty("cosmic_embedding_dimension").GetInt32() != 1)
            throw new InvalidDataException("Chroma collection lacks the explicitly provisioned COSMIC storage schema.");
        var dimension = schema.GetProperty("dimension");
        // Native Chroma leaves dimension null until the first record fixes it.
        if (dimension.ValueKind != JsonValueKind.Null && dimension.GetInt32() != 1)
            throw new InvalidDataException("Chroma collection embedding dimension differs from the scalar document contract.");
        _collectionIds[name] = id!;
        return id!;
    }
}
