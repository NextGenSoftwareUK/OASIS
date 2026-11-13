using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.WorldIDOASIS;

internal sealed class WorldIdProtocolComponentReader : IDisposable
{
    private static readonly Uri Endpoint = new("https://worldchain-mainnet.g.alchemy.com/public");
    private const string Genesis = "0x70d316d2e0973b62332ba2e9768dd7854298d7ffe77f0409ffdb8d859f2d3fa3";
    private const string Registry = "0x0000000000ae079eb8a274cd51c0f44a9e4d67d4";
    private const string Verifier = "0x00000000009e00f9fe82cfeebb4556686da094d7";
    private const string IssuerRegistry = "0x941239840f4d9668da8be76b568e836b50685d2c";
    private const string OprfRegistry = "0x0d8b461799474207a3d223553d4d5e6609cb0c69";
    private static readonly Dictionary<string, string> Contracts = new(StringComparer.Ordinal)
    {
        ["registry"] = Registry,
        ["verifier"] = Verifier,
        ["issuer-registry"] = IssuerRegistry
    };

    internal static IEnumerable<string> Components => Contracts.Keys;
    internal static bool IsReviewedComponent(string? component) => component != null && Contracts.ContainsKey(component);

    internal sealed record Observation(string Component, IReadOnlyDictionary<string, object> Fields);

    private readonly ReviewedEvmReadClient client;

    public WorldIdProtocolComponentReader(HttpMessageHandler? handler = null) => client = new(handler);

    public Task VerifyChainAsync(CancellationToken token) =>
        client.VerifyChainAsync(Endpoint, "0x1e0", Genesis, token);

    public async Task<Observation[]> ReadAsync(IReadOnlyList<string> components, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        var block = await client.ReadFinalizedBlockAsync(Endpoint, deadline.Token);
        var rows = new List<Observation>(components.Count);
        foreach (var component in components)
        {
            var contract = Contracts[component];
            var fields = new Dictionary<string, object>
            {
                ["name"] = component,
                ["WorldIdComponent"] = component,
                ["WorldIdContract"] = contract,
                ["WorldIdChainId"] = 480,
                ["WorldIdBlockNumber"] = block.Number,
                ["WorldIdBlockHash"] = block.Hash
            };
            Task<string> Read(string selector) => client.ReadAbiWordAsync(Endpoint, contract, selector, block, deadline.Token);
            switch (component)
            {
                case "registry":
                {
                    var words = await Task.WhenAll(Read("0xfdab463d"), Read("0x8e5cdd50"), Read("0x65b64a72"));
                    if (!words[0].AsSpan(2).ContainsAnyExcept('0'))
                        throw new InvalidDataException("The World ID registry must expose a nonzero current root.");
                    fields["WorldIdCurrentRoot"] = words[0];
                    fields["WorldIdTreeDepth"] = ReadPositiveInteger(words[1]);
                    fields["WorldIdRootValiditySeconds"] = ReadPositiveInteger(words[2]);
                    break;
                }
                case "verifier":
                {
                    var words = await Task.WhenAll(Read("0xc6c15ae0"), Read("0x340eb98f"), Read("0x1dfc0cea"));
                    fields["WorldIdRegistry"] = RequireAddress(words[0], Registry);
                    fields["WorldIdIssuerRegistry"] = RequireAddress(words[1], IssuerRegistry);
                    fields["WorldIdOprfRegistry"] = RequireAddress(words[2], OprfRegistry);
                    break;
                }
                case "issuer-registry":
                    fields["WorldIdOprfRegistry"] = RequireAddress(await Read("0x1dfc0cea"), OprfRegistry);
                    break;
            }
            rows.Add(new Observation(component, fields));
        }
        return rows.ToArray();
    }

    private static long ReadPositiveInteger(string word)
    {
        if (word.AsSpan(2, 48).ContainsAnyExcept('0') ||
            !long.TryParse(word.AsSpan(50), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) ||
            value <= 0)
            throw new InvalidDataException("World ID registry configuration must contain a bounded positive integer.");
        return value;
    }

    private static string RequireAddress(string word, string expected)
    {
        if (word.AsSpan(2, 24).ContainsAnyExcept('0') ||
            !word.AsSpan(26).SequenceEqual(expected.AsSpan(2)))
            throw new InvalidDataException("The World ID v4 component points outside the reviewed deployment.");
        return expected;
    }

    public void Dispose() => client.Dispose();
}
