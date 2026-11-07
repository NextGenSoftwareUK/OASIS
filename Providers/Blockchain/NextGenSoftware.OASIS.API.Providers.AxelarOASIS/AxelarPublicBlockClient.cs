using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;

namespace NextGenSoftware.OASIS.API.Providers.AxelarOASIS;

internal sealed class AxelarPublicBlockClient : IDisposable
{
    private const int MaximumResponseBytes = 256 * 1024;
    private readonly HttpClient _client;

    internal sealed record Header(long Height, string Timestamp, string BlockHash);

    public AxelarPublicBlockClient()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        handler.UseProxy = false;
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://axelar-rest.publicnode.com/cosmos/base/tendermint/v1beta1/blocks/")
        };
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<Header> ReadAsync(long height, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));
        using var response = await _client.GetAsync(height.ToString(CultureInfo.InvariantCulture),
            HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentType?.MediaType != "application/json")
            throw new InvalidDataException("Expected an Axelar JSON header.");

        await using var body = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
        var buffer = new byte[MaximumResponseBytes + 1];
        var size = await body.ReadAtLeastAsync(buffer, buffer.Length, false, deadline.Token).ConfigureAwait(false);
        if (size > MaximumResponseBytes)
            throw new InvalidDataException("Axelar header exceeds the response budget.");

        using var document = JsonDocument.Parse(buffer.AsMemory(0, size), new JsonDocumentOptions { MaxDepth = 16 });
        var root = document.RootElement;
        var header = root.GetProperty("block").GetProperty("header");
        var expected = height.ToString(CultureInfo.InvariantCulture);
        var timestamp = header.GetProperty("time").GetString();
        var encodedHash = root.GetProperty("block_id").GetProperty("hash").GetString();
        Span<byte> hash = stackalloc byte[32];
        if (header.GetProperty("chain_id").GetString() != "axelar-dojo-1" ||
            header.GetProperty("height").GetString() != expected ||
            timestamp == null || !Regex.IsMatch(timestamp, @"\A[0-9]{4}-[0-9]{2}-[0-9]{2}T[0-9]{2}:[0-9]{2}:[0-9]{2}(?:\.[0-9]{1,9})?Z\z") ||
            !DateTimeOffset.TryParse(timestamp, CultureInfo.InvariantCulture, DateTimeStyles.None, out _) ||
            encodedHash == null || !Convert.TryFromBase64String(encodedHash, hash, out var written) || written != hash.Length ||
            Convert.ToBase64String(hash) != encodedHash)
            throw new InvalidDataException($"Axelar returned an invalid mainnet block for height {height}.");

        return new Header(height, timestamp, Convert.ToHexStringLower(hash));
    }

    public void Dispose() => _client.Dispose();
}
