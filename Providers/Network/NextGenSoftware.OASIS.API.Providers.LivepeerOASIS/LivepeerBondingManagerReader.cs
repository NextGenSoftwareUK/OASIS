using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.LivepeerOASIS;

internal sealed class LivepeerBondingManagerReader : IDisposable
{
    private static readonly Uri Endpoint = new("https://arb1.arbitrum.io/rpc");
    private const string Contract = "0x35bcf3c30594191d53231e4ff333e8a770453e40";
    private const string Genesis = "0x7ee576b35482195fc49205cec9af72ce14f003b9ae69f6ba0faef4514be8b442";
    internal sealed record Observation(string Address, bool IsActive, string BlockNumber, string BlockHash);

    private readonly ReviewedEvmReadClient client;

    public LivepeerBondingManagerReader(HttpMessageHandler? handler = null) => client = new(handler);

    public Task VerifyChainAsync(CancellationToken token) =>
        client.VerifyChainAsync(Endpoint, "0xa4b1", Genesis, token);

    public async Task<Observation[]> ReadAsync(IReadOnlyList<string> addresses, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        var block = await client.ReadFinalizedBlockAsync(Endpoint, deadline.Token);
        var rows = new List<Observation>(addresses.Count);
        foreach (var address in addresses)
        {
            var data = "0x08802374" + address[2..].PadLeft(64, '0');
            var word = await client.ReadAbiWordAsync(Endpoint, Contract, data, block, deadline.Token);
            if (word.AsSpan(2, 63).ContainsAnyExcept('0') || word[^1] is not ('0' or '1'))
                throw new InvalidDataException("Livepeer BondingManager must return one ABI Boolean.");

            rows.Add(new Observation(address, word[^1] == '1', block.Number, block.Hash));
        }
        return rows.ToArray();
    }

    public void Dispose() => client.Dispose();
}
