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

namespace NextGenSoftware.OASIS.API.Providers.EspressoSystemsOASIS;

internal sealed class EspressoPublicHeaderClient : IDisposable
{
    private const int MaximumResponseBytes = 16 * 1024;
    private const string BlockOneL1Hash = "0x635350c09bb6827bdca8c4ff5a666fa055a4c712aa2c099517ed26e3531fcfa8";
    private readonly HttpClient _client;

    internal sealed record Header(uint Height, long Timestamp, long FinalizedL1Block, string FinalizedL1Hash);

    public EspressoPublicHeaderClient()
    {
        var handler = (SocketsHttpHandler)NativeConnectionGuard.CreateGuardedHttpHandler();
        handler.UseCookies = false;
        handler.UseProxy = false;
        _client = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://query.main.net.espresso.network/v1/availability/header/")
        };
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task<Header> ReadAsync(uint height, CancellationToken cancellationToken)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(12));
        using var response = await _client.GetAsync(height.ToString(CultureInfo.InvariantCulture),
            HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentType?.MediaType != "application/json")
            throw new InvalidDataException("Expected an Espresso JSON header.");

        await using var body = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
        var buffer = new byte[MaximumResponseBytes + 1];
        var size = await body.ReadAtLeastAsync(buffer, buffer.Length, false, deadline.Token).ConfigureAwait(false);
        if (size > MaximumResponseBytes)
            throw new InvalidDataException("Espresso header exceeds the response budget.");

        using var document = JsonDocument.Parse(buffer.AsMemory(0, size), new JsonDocumentOptions { MaxDepth = 16 });
        var fields = document.RootElement.GetProperty("fields");
        var actual = fields.GetProperty("height").GetUInt32();
        var timestamp = fields.GetProperty("timestamp").GetInt64();
        var chain = fields.GetProperty("chain_config").GetProperty("chain_config").GetProperty("Left");
        var finalized = fields.GetProperty("l1_finalized");
        var number = finalized.GetProperty("number").GetInt64();
        var hash = finalized.GetProperty("hash").GetString();

        if (actual != height || chain.GetProperty("chain_id").GetString() != "1" || timestamp <= 0 ||
            number <= 0 || hash == null || !Regex.IsMatch(hash, @"\A0x[0-9a-f]{64}\z") ||
            (height == 1 && hash != BlockOneL1Hash))
            throw new InvalidDataException($"Espresso returned an invalid mainnet header for height {height}.");

        return new Header(actual, timestamp, number, hash);
    }

    public void Dispose() => _client.Dispose();
}
