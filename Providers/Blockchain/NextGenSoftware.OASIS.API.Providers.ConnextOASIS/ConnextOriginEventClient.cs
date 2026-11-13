using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace NextGenSoftware.OASIS.API.Providers.ConnextOASIS;

/// <summary>Exact Ethereum Connext origin events observed by Blockscout.</summary>
public sealed class ConnextOriginEventClient(HttpClient client)
{
    public const string ContractAddress = "0x8898b472c54c31894e3b9bb83cea802a5d0e63c6";
    private const string EventTopic = "0xed8e6ba697dd65259e5ce532ac08ff06d1a3607bcec58f8f0937fe36a5666c54";
    private const int MaximumResponseBytes = 512 * 1024;
    private static readonly Regex Hash = new(@"\A0x[0-9a-f]{64}\z", RegexOptions.CultureInvariant);

    public sealed record Identity(string TransferId, string TransactionHash, string BlockHash, long BlockNumber, int LogIndex);

    public async Task<Identity> ReadAsync(Identity expected, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expected);
        if (!Hash.IsMatch(expected.TransferId) || !Hash.IsMatch(expected.TransactionHash) ||
            !Hash.IsMatch(expected.BlockHash) || expected.BlockNumber <= 0 || expected.LogIndex < 0)
            throw new ArgumentException("A canonical Connext origin event identity is required.", nameof(expected));

        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://eth.blockscout.com/api/v2/transactions/" + expected.TransactionHash + "/logs");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.UserAgent.ParseAdd("OASIS-ConnextRead/1.0");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaximumResponseBytes)
            throw new InvalidDataException("Connext transaction logs exceed the response budget.");
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var payload = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = await stream.ReadAsync(buffer.AsMemory(), cancellationToken)) != 0)
        {
            if (payload.Length + count > MaximumResponseBytes)
                throw new InvalidDataException("Connext transaction logs exceed the response budget.");
            payload.Write(buffer, 0, count);
        }

        payload.Position = 0;
        using var document = JsonDocument.Parse(payload, new JsonDocumentOptions { MaxDepth = 32 });
        var root = document.RootElement;
        if (root.GetProperty("next_page_params").ValueKind != JsonValueKind.Null)
            throw new InvalidDataException("The exact transaction log page must be complete within the response budget.");
        int matches = 0;
        foreach (var row in root.GetProperty("items").EnumerateArray())
        {
            if (row.GetProperty("index").GetInt32() != expected.LogIndex)
                continue;
            var topics = row.GetProperty("topics");
            if (!string.Equals(row.GetProperty("address").GetProperty("hash").GetString(), ContractAddress, StringComparison.OrdinalIgnoreCase) ||
                row.GetProperty("transaction_hash").GetString() != expected.TransactionHash ||
                row.GetProperty("block_hash").GetString() != expected.BlockHash ||
                row.GetProperty("block_number").GetInt64() != expected.BlockNumber ||
                topics.GetArrayLength() != 4 || topics[0].GetString() != EventTopic || topics[1].GetString() != expected.TransferId)
                throw new InvalidDataException("The indexed Connext origin event differs from its configured identity.");
            matches++;
        }

        if (matches != 1)
            throw new InvalidDataException("One exact indexed Connext origin event is required.");
        cancellationToken.ThrowIfCancellationRequested();
        return expected;
    }
}
