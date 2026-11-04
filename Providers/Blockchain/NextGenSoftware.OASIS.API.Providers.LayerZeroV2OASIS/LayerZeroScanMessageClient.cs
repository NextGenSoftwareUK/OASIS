using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.LayerZeroV2OASIS;

/// <summary>Exact public mainnet message observations from LayerZero Scan.</summary>
public sealed class LayerZeroScanMessageClient(HttpClient client)
{
    private const int MaximumResponseBytes = 256 * 1024;
    private static readonly Regex MessageGuid = new(@"\A0x[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    public sealed record Observation(string Guid, uint SourceEndpoint, uint DestinationEndpoint, ulong Nonce, string Status);

    public async Task<Observation> ReadAsync(string guid, CancellationToken cancellationToken)
    {
        if (guid == null || !MessageGuid.IsMatch(guid))
            throw new ArgumentException("A canonical LayerZero message GUID is required.", nameof(guid));

        using var request = new HttpRequestMessage(HttpMethod.Get,
            new Uri("https://scan.layerzero-api.com/v1/messages/guid/" + guid));
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("OASIS-LayerZeroScan/1.0");
        using var response = await client.SendAsync(request,
            HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            throw new InvalidDataException("LayerZero Scan response exceeds its byte budget.");

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var count = await stream.ReadAsync(buffer.AsMemory(), cancellationToken);
            if (count == 0)
                break;
            if (payload.Length + count > MaximumResponseBytes)
                throw new InvalidDataException("LayerZero Scan response exceeds its byte budget.");
            payload.Write(buffer, 0, count);
        }

        payload.Position = 0;
        using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });
        var rows = document.RootElement.GetProperty("data");
        if (rows.ValueKind != JsonValueKind.Array || rows.GetArrayLength() != 1)
            throw new InvalidDataException("LayerZero Scan must return one exact message.");

        var row = rows[0];
        if (row.GetProperty("guid").GetString() != guid)
            throw new InvalidDataException("LayerZero Scan returned a different message identity.");

        var pathway = row.GetProperty("pathway");
        var source = pathway.GetProperty("srcEid").GetUInt32();
        var destination = pathway.GetProperty("dstEid").GetUInt32();
        var nonce = pathway.GetProperty("nonce").GetUInt64();
        var status = row.GetProperty("status").GetProperty("name").GetString();
        if (source == 0 || destination == 0 || status == null || status.Length is < 1 or > 64
            || !Regex.IsMatch(status, @"\A[A-Z][A-Z0-9_]*\z", RegexOptions.CultureInvariant))
            throw new InvalidDataException("LayerZero Scan returned invalid message metadata.");
        return new(guid, source, destination, nonce, status);
    }
}
