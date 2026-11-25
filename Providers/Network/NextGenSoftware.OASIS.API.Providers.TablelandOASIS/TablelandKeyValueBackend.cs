using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.TablelandOASIS
{
    /// <summary>Connection settings for one Tableland key-value table owned by one EVM wallet.</summary>
    public sealed class TablelandConnection
    {
        /// <param name="rpcUrl">EVM JSON-RPC endpoint of the chain hosting the TablelandTables registry.</param>
        /// <param name="validatorUrl">Tableland validator base URL (the host serving /api/v1).</param>
        /// <param name="registryAddress">TablelandTables registry contract address on that chain.</param>
        /// <param name="chainId">Chain id the RPC endpoint must report; transactions are signed for it.</param>
        /// <param name="privateKeyHex">Wallet secret key that owns the table and signs every write.</param>
        public TablelandConnection(Uri rpcUrl, Uri validatorUrl, string registryAddress, long chainId, string privateKeyHex)
        {
            RpcUrl = rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl));
            ValidatorUrl = validatorUrl ?? throw new ArgumentNullException(nameof(validatorUrl));
            if (string.IsNullOrWhiteSpace(registryAddress)) throw new ArgumentException("A registry contract address is required.", nameof(registryAddress));
            if (chainId <= 0) throw new ArgumentOutOfRangeException(nameof(chainId), "A positive chain id is required.");
            if (string.IsNullOrWhiteSpace(privateKeyHex)) throw new ArgumentException("A wallet private key is required.", nameof(privateKeyHex));
            RegistryAddress = registryAddress;
            ChainId = chainId;
            PrivateKeyHex = privateKeyHex;
        }

        public Uri RpcUrl { get; }
        public Uri ValidatorUrl { get; }
        public string RegistryAddress { get; }
        public long ChainId { get; }
        internal string PrivateKeyHex { get; }

        /// <summary>Maximum time to wait for the validator to materialise a mined write.</summary>
        public TimeSpan MaterializationTimeout { get; init; } = TimeSpan.FromMinutes(2);

        /// <summary>Validator write-statement size limit (Tableland default MaxWriteQuerySize).</summary>
        public int MaxStatementBytes { get; init; } = 35000;

        /// <summary>
        /// Validator request budget per second (Tableland validators answer HTTP 429 beyond HTTP.MaxRequestPerInterval,
        /// 10 per 1s by default). Every query and receipt lookup is paced to stay inside it.
        /// </summary>
        public int MaxValidatorRequestsPerSecond { get; init; } = 10;
    }

    /// <summary>
    /// Sliding-window pacing so no one-second window holds more than the validator's request budget. The budget belongs
    /// to the validator endpoint, so every backend in the process talking to the same validator shares one pacer.
    /// </summary>
    internal sealed class ValidatorRequestPacer
    {
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<(string, int), ValidatorRequestPacer> PerValidator = new();

        public static ValidatorRequestPacer For(TablelandConnection connection)
            => PerValidator.GetOrAdd((connection.ValidatorUrl.GetLeftPart(UriPartial.Authority).ToLowerInvariant(), connection.MaxValidatorRequestsPerSecond),
                key => new ValidatorRequestPacer(key.Item2));

        private static readonly TimeSpan Window = TimeSpan.FromSeconds(1);
        private readonly Queue<long> _sent = new();
        private readonly SemaphoreSlim _gate = new(1, 1);
        private readonly int _budget;

        private ValidatorRequestPacer(int budget)
        {
            if (budget <= 0) throw new ArgumentOutOfRangeException(nameof(budget), "A positive validator request budget is required.");
            _budget = budget;
        }

        public async Task<HttpResponseMessage> GetAsync(HttpClient http, string uri, CancellationToken cancellationToken)
        {
            await _gate.WaitAsync(cancellationToken);
            try
            {
                while (_sent.Count >= _budget)
                {
                    var elapsed = Stopwatch.GetElapsedTime(_sent.Peek());
                    if (elapsed >= Window) { _sent.Dequeue(); continue; }
                    await Task.Delay(Window - elapsed, cancellationToken);
                }
                _sent.Enqueue(Stopwatch.GetTimestamp());
            }
            finally { _gate.Release(); }
            return await http.GetAsync(uri, cancellationToken);
        }
    }

    /// <summary>
    /// <see cref="IKeyValueBackend"/> over one Tableland table <c>{prefix}_{chainId}_{tableId}(k TEXT PRIMARY KEY, v TEXT NOT NULL)</c>.
    /// Writes are signed <c>mutate</c> transactions to the TablelandTables registry; a write completes only after its
    /// transaction is mined successfully and the validator's receipt for it reports no execution error.
    /// Reads use the validator query API, which serves the materialised state.
    /// </summary>
    public sealed class TablelandKeyValueBackend : IKeyValueBackend, IDisposable
    {
        private const int ListPageSize = 1000;
        private static readonly TimeSpan ReceiptPollInterval = TimeSpan.FromMilliseconds(250);

        private readonly TablelandConnection _connection;
        private readonly Web3 _web3;
        private readonly HttpClient _http;
        private readonly SemaphoreSlim _writeGate = new(1, 1);
        private readonly ValidatorRequestPacer _pacer;

        public TablelandKeyValueBackend(TablelandConnection connection, string tableName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            (TableId, _) = ParseTableName(tableName, connection.ChainId);
            TableName = tableName;
            var account = new Account(connection.PrivateKeyHex, connection.ChainId);
            WalletAddress = account.Address;
            _web3 = new Web3(account, connection.RpcUrl.ToString());
            _http = new HttpClient { BaseAddress = connection.ValidatorUrl, Timeout = TimeSpan.FromSeconds(60) };
            _pacer = ValidatorRequestPacer.For(connection);
        }

        public string TableName { get; }
        public BigInteger TableId { get; }
        public string WalletAddress { get; }

        /// <summary>
        /// Creates a new key-value table owned by the connection's wallet and waits until the validator has materialised it.
        /// Returns the full table name (<c>{prefix}_{chainId}_{tableId}</c>) to configure the provider with.
        /// </summary>
        public static async Task<string> CreateTableAsync(TablelandConnection connection, string prefix = "oasis_kv", CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            if (string.IsNullOrEmpty(prefix) || !prefix.All(c => c == '_' || char.IsAsciiLetterOrDigit(c)) || !char.IsAsciiLetter(prefix[0]))
                throw new ArgumentException("A table prefix must start with a letter and contain only ASCII letters, digits and underscores.", nameof(prefix));
            var account = new Account(connection.PrivateKeyHex, connection.ChainId);
            var web3 = new Web3(account, connection.RpcUrl.ToString());
            using var http = new HttpClient { BaseAddress = connection.ValidatorUrl, Timeout = TimeSpan.FromSeconds(60) };
            await RequireChainAsync(web3, connection.ChainId);
            var statement = $"CREATE TABLE {prefix}_{connection.ChainId} (k TEXT PRIMARY KEY, v TEXT NOT NULL)";
            var receipt = await web3.Eth.GetContractTransactionHandler<CreateFunction>()
                .SendRequestAndWaitForReceiptAsync(connection.RegistryAddress, new CreateFunction { Owner = account.Address, Statement = statement }, cancellationToken);
            RequireMined(receipt, "create");
            var created = receipt.DecodeAllEvents<CreateTableEvent>()
                .SingleOrDefault(e => string.Equals(e.Log.Address, connection.RegistryAddress, StringComparison.OrdinalIgnoreCase))
                ?? throw new InvalidOperationException("Tableland create transaction emitted no CreateTable event from the registry.");
            await WaitForMaterializationAsync(http, ValidatorRequestPacer.For(connection), connection, receipt.TransactionHash, cancellationToken);
            return $"{prefix}_{connection.ChainId}_{created.Event.TableId}";
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            var rows = await QueryAsync($"SELECT v FROM {TableName} WHERE k = {Literal(key)}", cancellationToken);
            if (rows.Count == 0) return null;
            if (rows.Count > 1) throw new InvalidOperationException("Tableland returned more than one row for a primary key.");
            return RequireString(rows[0], "v");
        }

        public Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(value);
            return MutateAsync($"INSERT INTO {TableName} (k, v) VALUES ({Literal(key)}, {Literal(value)}) ON CONFLICT (k) DO UPDATE SET v = excluded.v", cancellationToken);
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
            => MutateAsync($"DELETE FROM {TableName} WHERE k = {Literal(key)}", cancellationToken);

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            var keys = new List<string>();
            var lowerBound = $"k >= {Literal(prefix)}";
            while (true)
            {
                var rows = await QueryAsync($"SELECT k FROM {TableName} WHERE {lowerBound} ORDER BY k LIMIT {ListPageSize}", cancellationToken);
                foreach (var row in rows)
                {
                    // Keys sharing a prefix are contiguous in SQLite BINARY order, so the first non-match ends the range.
                    var key = RequireString(row, "k");
                    if (!key.StartsWith(prefix, StringComparison.Ordinal)) return keys;
                    keys.Add(key);
                }
                if (rows.Count < ListPageSize) return keys;
                lowerBound = $"k > {Literal(keys[^1])}";
            }
        }

        /// <summary>Proves the RPC serves the configured chain, the wallet owns the table, and the validator serves it.</summary>
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            await RequireChainAsync(_web3, _connection.ChainId);
            var owner = await _web3.Eth.GetContractQueryHandler<OwnerOfFunction>()
                .QueryAsync<string>(_connection.RegistryAddress, new OwnerOfFunction { TokenId = TableId });
            if (!string.Equals(owner, WalletAddress, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Tableland table {TableName} is owned by {owner}, not the configured wallet {WalletAddress}.");
            await QueryAsync($"SELECT count(*) AS n FROM {TableName}", cancellationToken);
        }

        private async Task MutateAsync(string statement, CancellationToken cancellationToken)
        {
            var size = Encoding.UTF8.GetByteCount(statement);
            if (size > _connection.MaxStatementBytes)
                throw new InvalidOperationException($"Tableland write statement is {size} bytes; the validator limit is {_connection.MaxStatementBytes} bytes.");
            await _writeGate.WaitAsync(cancellationToken);
            try
            {
                var receipt = await _web3.Eth.GetContractTransactionHandler<MutateFunction>().SendRequestAndWaitForReceiptAsync(
                    _connection.RegistryAddress, new MutateFunction { Caller = WalletAddress, TableId = TableId, Statement = statement }, cancellationToken);
                RequireMined(receipt, "mutate");
                await WaitForMaterializationAsync(_http, _pacer, _connection, receipt.TransactionHash, cancellationToken);
            }
            finally { _writeGate.Release(); }
        }

        /// <summary>Polls the validator receipt endpoint (404 until the validator has processed the transaction's block).</summary>
        private static async Task WaitForMaterializationAsync(HttpClient http, ValidatorRequestPacer pacer, TablelandConnection connection, string txHash, CancellationToken cancellationToken)
        {
            var deadline = DateTime.UtcNow + connection.MaterializationTimeout;
            while (true)
            {
                using var response = await pacer.GetAsync(http, $"api/v1/receipt/{connection.ChainId}/{txHash}", cancellationToken);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                    if (doc.RootElement.TryGetProperty("error", out var error) && error.ValueKind == JsonValueKind.String && error.GetString().Length > 0)
                        throw new InvalidOperationException($"Tableland validator rejected transaction {txHash}: {error.GetString()}");
                    return;
                }
                if (response.StatusCode != HttpStatusCode.NotFound)
                    throw new HttpRequestException($"Tableland receipt lookup failed ({(int)response.StatusCode}): {await response.Content.ReadAsStringAsync(cancellationToken)}");
                if (DateTime.UtcNow >= deadline)
                    throw new TimeoutException($"Tableland validator did not materialise transaction {txHash} within {connection.MaterializationTimeout}.");
                await Task.Delay(ReceiptPollInterval, cancellationToken);
            }
        }

        private async Task<List<JsonElement>> QueryAsync(string statement, CancellationToken cancellationToken)
        {
            using var response = await _pacer.GetAsync(_http, $"api/v1/query?format=objects&statement={Uri.EscapeDataString(statement)}", cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"Tableland query failed ({(int)response.StatusCode}): {body}");
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("Tableland query response is not a row array.");
            return doc.RootElement.EnumerateArray().Select(e => e.Clone()).ToList();
        }

        private static async Task RequireChainAsync(Web3 web3, long chainId)
        {
            var actual = (await web3.Eth.ChainId.SendRequestAsync()).Value;
            if (actual != chainId)
                throw new InvalidOperationException($"Tableland RPC serves chain {actual}, not the configured chain {chainId}.");
        }

        private static void RequireMined(TransactionReceipt receipt, string operation)
        {
            if (receipt == null || receipt.Status == null || receipt.Status.Value != 1)
                throw new InvalidOperationException($"Tableland {operation} transaction {receipt?.TransactionHash} did not succeed on chain.");
        }

        private static string RequireString(JsonElement row, string column)
            => row.TryGetProperty(column, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : throw new InvalidOperationException($"Tableland row has no text column '{column}'.");

        /// <summary>
        /// SQL text expression for <paramref name="value"/>: single quotes are doubled, and because the Tableland statement parser
        /// rejects any text literal longer than <see cref="MaxTextLiteralBytes"/>, longer values are written as a <c>||</c>
        /// concatenation of literals that each fit (never splitting a quote pair or a surrogate pair). NUL is not representable.
        /// </summary>
        internal static string Literal(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (value.Contains('\0')) throw new ArgumentException("Tableland text values cannot contain NUL characters.", nameof(value));
            var expression = new StringBuilder(value.Length + 8);
            var literal = new StringBuilder();
            var literalBytes = 0;
            void Flush()
            {
                if (expression.Length > 0) expression.Append(" || ");
                expression.Append('\'').Append(literal).Append('\'');
                literal.Clear();
                literalBytes = 0;
            }
            for (var i = 0; i < value.Length; i++)
            {
                var unit = char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])
                    ? value.Substring(i++, 2)
                    : value[i] == '\'' ? "''" : value[i].ToString();
                var unitBytes = Encoding.UTF8.GetByteCount(unit);
                if (literalBytes + unitBytes > MaxTextLiteralBytes) Flush();
                literal.Append(unit);
                literalBytes += unitBytes;
            }
            if (literal.Length > 0 || expression.Length == 0) Flush();
            return expression.ToString();
        }

        /// <summary>Longest text literal the Tableland SQL parser accepts.</summary>
        internal const int MaxTextLiteralBytes = 1024;

        internal static (BigInteger TableId, string Prefix) ParseTableName(string tableName, long chainId)
        {
            if (string.IsNullOrWhiteSpace(tableName)) throw new ArgumentException("A Tableland table name is required.", nameof(tableName));
            var parts = tableName.Split('_');
            if (parts.Length < 3 || parts[^2] != chainId.ToString() || !BigInteger.TryParse(parts[^1], out var id) || id <= 0
                || !tableName.All(c => c == '_' || char.IsAsciiLetterOrDigit(c)) || !char.IsAsciiLetter(tableName[0]))
                throw new ArgumentException($"'{tableName}' is not a Tableland table name of the form prefix_{chainId}_tableId.", nameof(tableName));
            return (id, string.Join('_', parts[..^2]));
        }

        public void Dispose()
        {
            _http.Dispose();
            _writeGate.Dispose();
        }

        [Function("create", "uint256")]
        private sealed class CreateFunction : FunctionMessage
        {
            [Parameter("address", "owner", 1)] public string Owner { get; set; }
            [Parameter("string", "statement", 2)] public string Statement { get; set; }
        }

        [Function("mutate")]
        private sealed class MutateFunction : FunctionMessage
        {
            [Parameter("address", "caller", 1)] public string Caller { get; set; }
            [Parameter("uint256", "tableId", 2)] public BigInteger TableId { get; set; }
            [Parameter("string", "statement", 3)] public string Statement { get; set; }
        }

        [Function("ownerOf", "address")]
        private sealed class OwnerOfFunction : FunctionMessage
        {
            [Parameter("uint256", "tokenId", 1)] public BigInteger TokenId { get; set; }
        }

        [Event("CreateTable")]
        private sealed class CreateTableEvent : IEventDTO
        {
            [Parameter("address", "owner", 1, false)] public string Owner { get; set; }
            [Parameter("uint256", "tableId", 2, false)] public BigInteger TableId { get; set; }
            [Parameter("string", "statement", 3, false)] public string Statement { get; set; }
        }
    }
}
