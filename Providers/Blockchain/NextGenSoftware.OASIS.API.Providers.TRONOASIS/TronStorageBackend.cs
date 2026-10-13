using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;
using System.Text;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.Model;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TRONOASIS;

/// <summary>SDK-backed OASISKeyValueStore adapter. Compose with PrivateKeyValueBackend before storing user data.</summary>
public sealed class TronStorageBackend : IKeyValueBackend
{
    private readonly TronNativeTransferClient _client;
    private readonly string _owner;
    private readonly string _contract;
    private readonly Func<string> _signingKey;
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public TronStorageBackend(TronNativeTransferClient client, string owner, string contract, Func<string> signingKey)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _owner = !string.IsNullOrWhiteSpace(owner) ? owner : throw new ArgumentException("Storage owner is required.");
        _contract = !string.IsNullOrWhiteSpace(contract) ? contract : throw new ArgumentException("Storage contract is required.");
        _signingKey = signingKey ?? throw new ArgumentNullException(nameof(signingKey));
    }

    internal static byte[] Encode(string signature, string[] types, params object[] values)
    {
        foreach (var value in values) if (value is string text) StrictUtf8.GetByteCount(text);
        var parameters = types.Select((type, index) => new Parameter(type, "arg" + index, index + 1)).ToArray();
        return new FunctionCallEncoder().EncodeRequest(new Nethereum.Util.Sha3Keccack().CalculateHash(signature)[..8], parameters, values).HexToByteArray();
    }

    private async Task<List<ParameterOutput>> Query(string signature, string[] inputTypes, object[] values,
        string[] outputTypes, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var bytes = await _client.QueryContractAsync(_owner, _contract, Encode(signature, inputTypes, values), token);
        return Decode(bytes, outputTypes);
    }

    internal static List<ParameterOutput> Decode(byte[] bytes, params string[] outputTypes)
    {
        if (bytes == null || bytes.Length == 0 || bytes.Length % 32 != 0)
            throw new InvalidOperationException("Storage query returned an empty or truncated ABI payload.");
        var outputs = outputTypes.Select((type, index) => new ParameterOutput
        {
            Parameter = new Parameter(type, "output" + index, index + 1)
            {
                DecodedType = type switch
                {
                    "bool" => typeof(bool), "string" => typeof(string),
                    "uint256" => typeof(BigInteger), "string[]" => typeof(List<string>),
                    _ => throw new ArgumentException("Unsupported storage ABI output type.")
                }
            }
        }).ToArray();
        var decoded = new FunctionCallDecoder().DecodeOutput("0x" + Convert.ToHexString(bytes), outputs);
        foreach (var output in decoded)
        {
            if (output.Result is string text) StrictUtf8.GetByteCount(text);
            if (output.Result is IEnumerable<string> strings)
                foreach (var entry in strings) StrictUtf8.GetByteCount(entry);
        }
        var canonical = Encode("result(" + string.Join(",", outputTypes) + ")", outputTypes,
            decoded.Select(output => output.Result).ToArray());
        if (!bytes.AsSpan().SequenceEqual(canonical.AsSpan(4)))
            throw new InvalidOperationException("Storage query returned noncanonical or lossy ABI data.");
        return decoded;
    }

    public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var result = await Query("get(string)", new[] { "string" }, new object[] { key }, new[] { "bool", "string" }, cancellationToken);
        return (bool)result[0].Result ? (string)result[1].Result : null;
    }

    private async Task Write(string signature, string[] types, object[] values, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var data = Encode(signature, types, values);
        var key = _signingKey();
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Storage signing credential is missing.");
        var transaction = await _client.ExecuteSignedContractAsync(_owner, _contract, data, key, token);
        if (string.IsNullOrWhiteSpace(transaction)) throw new InvalidOperationException("Storage mutation returned no confirmed transaction ID.");
    }

    public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(key)) throw new ArgumentException("Storage key must not be empty.");
        ArgumentNullException.ThrowIfNull(value);
        return Write("put(string,string)", new[] { "string", "string" }, new object[] { key, value }, cancellationToken);
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        return Write("remove(string)", new[] { "string" }, new object[] { key }, cancellationToken);
    }

    public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(prefix);
        StrictUtf8.GetByteCount(prefix);
        async Task<BigInteger> Revision() => (BigInteger)(await Query("revision()", Array.Empty<string>(), Array.Empty<object>(),
            new[] { "uint256" }, cancellationToken))[0].Result;
        var revision = await Revision();
        var cursor = BigInteger.Zero;
        BigInteger? total = null;
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        do
        {
            var page = await Query("keyPage(uint256,uint256,uint256)", new[] { "uint256", "uint256", "uint256" },
                new object[] { revision, cursor, new BigInteger(256) }, new[] { "string[]", "uint256", "uint256" }, cancellationToken);
            var entries = ((System.Collections.IEnumerable)page[0].Result).Cast<string>().ToArray();
            var next = (BigInteger)page[1].Result;
            var count = (BigInteger)page[2].Result;
            if (count < 0 || next < cursor || next > count || next - cursor != entries.Length
                || entries.Length > 256 || (total.HasValue && total != count) || (next == cursor && next != count))
                throw new InvalidOperationException("Storage pagination returned inconsistent cursor or total.");
            total = count;
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry) || !keys.Add(entry)) throw new InvalidOperationException("Storage pagination returned invalid or duplicate keys.");
            }
            cursor = next;
        } while (cursor < total);
        if (await Revision() != revision) throw new InvalidOperationException("Storage changed during enumeration.");
        return keys.Where(key => key.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
    }

    public async Task VerifyAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var ownerBytes=NBitcoin.DataEncoders.Encoders.Base58Check.DecodeData(_owner);
        if(ownerBytes.Length!=21 || ownerBytes[0]!=0x41)
            throw new InvalidOperationException("Storage owner must be a canonical TRON address.");
        var expected=new BigInteger(ownerBytes.AsSpan(1),isUnsigned:true,isBigEndian:true);
        var actual=(BigInteger)(await Query("owner()",Array.Empty<string>(),Array.Empty<object>(),
            new[]{"uint256"},cancellationToken))[0].Result;
        if(actual!=expected)throw new InvalidOperationException("Storage contract owner does not match the configured owner.");
        await ListKeysAsync(string.Empty,cancellationToken);
    }
}
