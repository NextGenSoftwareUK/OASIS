using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nethereum.ABI;
using Nethereum.ABI.FunctionEncoding;
using Nethereum.ABI.FunctionEncoding.Attributes;
using Nethereum.Contracts;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage;

namespace NextGenSoftware.OASIS.API.Providers.SignProtocolOASIS
{
    /// <summary>Connection settings for one Sign Protocol storage schema owned by one EVM wallet.</summary>
    public sealed class SignProtocolConnection
    {
        /// <param name="rpcUrl">EVM JSON-RPC endpoint of the chain hosting the Sign Protocol (SP) contract.</param>
        /// <param name="chainId">Chain id the RPC endpoint must report; transactions are signed for it.</param>
        /// <param name="spAddress">Address of the SP contract (the proxy, for upgradeable deployments).</param>
        /// <param name="privateKeyHex">Wallet secret key that owns the schema and is the only accepted attester.</param>
        /// <param name="schemaId">Storage schema registered by the wallet (see <see cref="SignProtocolKeyValueBackend.RegisterSchemaAsync"/>).</param>
        /// <param name="fromBlock">Block in which the schema was registered; attestations are read from this block onward.</param>
        public SignProtocolConnection(Uri rpcUrl, long chainId, string spAddress, string privateKeyHex, ulong schemaId, ulong fromBlock)
        {
            RpcUrl = rpcUrl ?? throw new ArgumentNullException(nameof(rpcUrl));
            if (chainId <= 0) throw new ArgumentOutOfRangeException(nameof(chainId), "A positive chain id is required.");
            if (string.IsNullOrWhiteSpace(spAddress) || !AddressUtil.Current.IsValidEthereumAddressHexFormat(spAddress))
                throw new ArgumentException("A Sign Protocol contract address is required.", nameof(spAddress));
            if (string.IsNullOrWhiteSpace(privateKeyHex)) throw new ArgumentException("A wallet private key is required.", nameof(privateKeyHex));
            ChainId = chainId;
            SpAddress = spAddress;
            PrivateKeyHex = privateKeyHex;
            SchemaId = schemaId;
            FromBlock = fromBlock;
        }

        public Uri RpcUrl { get; }
        public long ChainId { get; }
        public string SpAddress { get; }
        public ulong SchemaId { get; }
        public ulong FromBlock { get; }
        internal string PrivateKeyHex { get; }

        /// <summary>Largest block span requested in one eth_getLogs call.</summary>
        public ulong MaxLogBlockRange { get; init; } = 2000;

        internal SignProtocolConnection WithSchema(ulong schemaId, ulong fromBlock)
            => new(RpcUrl, ChainId, SpAddress, PrivateKeyHex, schemaId, fromBlock) { MaxLogBlockRange = MaxLogBlockRange };
    }

    /// <summary>A storage schema registered on a Sign Protocol contract.</summary>
    public sealed record SignProtocolStorageSchema(ulong SchemaId, ulong RegisteredBlock);

    /// <summary>
    /// <see cref="IKeyValueBackend"/> over Sign Protocol attestations. One schema, registered by the wallet, holds the store:
    /// Put attests <c>abi.encode(string key, uint64 seq, string value)</c> with indexing key <c>oasis-kv:v1:keccak256(key)</c>;
    /// the attestation with the highest seq for a key is its current value. Delete revokes that attestation, which is the
    /// protocol's native removal. Reads derive the store from the contract's AttestationMade/AttestationRevoked events and
    /// <c>getAttestation</c>, accepting only attestations of the configured schema made by the configured wallet, so other
    /// accounts cannot inject records. Writes complete only once their transaction is mined with status 1 and the
    /// resulting attestation (or revocation) has been read back from the chain.
    /// </summary>
    public sealed class SignProtocolKeyValueBackend : IKeyValueBackend, IDisposable
    {
        /// <summary>Sign Protocol contract version this backend implements (SP.sol <c>version()</c>).</summary>
        public const string SupportedProtocolVersion = "1.1.4";

        /// <summary>The schema text registered for OASIS storage (Schema.data).</summary>
        public const string SchemaDefinition =
            "{\"name\":\"OASIS key-value record v1\",\"data\":[{\"name\":\"key\",\"type\":\"string\"},{\"name\":\"seq\",\"type\":\"uint64\"},{\"name\":\"value\",\"type\":\"string\"}]}";

        private const string IndexPrefix = "oasis-kv:v1:";
        private const string RevokeReason = "oasis-kv delete";
        private const byte OnChain = 0; // DataLocation.ONCHAIN
        private const string ZeroAddress = "0x0000000000000000000000000000000000000000";

        private readonly SignProtocolConnection _connection;
        private readonly Web3 _web3;
        private readonly SemaphoreSlim _writeGate = new(1, 1);
        private readonly SemaphoreSlim _indexGate = new(1, 1);

        // Chain-derived index of this wallet's records, advanced block range by block range.
        private readonly Dictionary<string, Record> _current = new(StringComparer.Ordinal);
        private readonly Dictionary<ulong, string> _ownAttestations = new();
        private ulong _nextBlock;

        public SignProtocolKeyValueBackend(SignProtocolConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            var account = new Account(connection.PrivateKeyHex, connection.ChainId);
            WalletAddress = account.Address;
            _web3 = new Web3(account, connection.RpcUrl.ToString());
            _nextBlock = connection.FromBlock;
        }

        /// <summary>Address of the wallet that owns the schema and signs every attestation.</summary>
        public string WalletAddress { get; }

        public ulong SchemaId => _connection.SchemaId;

        /// <summary>
        /// Registers a new revocable, hook-less, on-chain storage schema owned by the connection's wallet and returns its id
        /// and registration block, to configure the provider with. The connection's own schema settings are ignored.
        /// </summary>
        public static async Task<SignProtocolStorageSchema> RegisterSchemaAsync(SignProtocolConnection connection, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(connection);
            var account = new Account(connection.PrivateKeyHex, connection.ChainId);
            var web3 = new Web3(account, connection.RpcUrl.ToString());
            await RequireProtocolAsync(web3, connection);
            var receipt = await web3.Eth.GetContractTransactionHandler<RegisterFunction>().SendRequestAndWaitForReceiptAsync(connection.SpAddress,
                new RegisterFunction
                {
                    Schema = new SchemaStruct
                    {
                        Registrant = account.Address, Revocable = true, DataLocation = OnChain, MaxValidFor = 0,
                        Hook = ZeroAddress, Timestamp = 0, Data = SchemaDefinition
                    },
                    DelegateSignature = Array.Empty<byte>()
                }, cancellationToken);
            RequireMined(receipt, "register");
            var registered = receipt.DecodeAllEvents<SchemaRegisteredEvent>()
                .Where(e => string.Equals(e.Log.Address, connection.SpAddress, StringComparison.OrdinalIgnoreCase)).ToList();
            if (registered.Count != 1)
                throw new InvalidOperationException("Sign Protocol register transaction did not emit exactly one SchemaRegistered event.");
            var schema = new SignProtocolStorageSchema(registered[0].Event.SchemaId, (ulong)receipt.BlockNumber.Value);
            var backend = new SignProtocolKeyValueBackend(connection.WithSchema(schema.SchemaId, schema.RegisteredBlock));
            await backend.RequireSchemaAsync();
            return schema;
        }

        public async Task<string> GetAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            await SyncAsync(cancellationToken);
            await _indexGate.WaitAsync(cancellationToken);
            try { return _current.TryGetValue(key, out var record) && !record.Revoked ? record.Value : null; }
            finally { _indexGate.Release(); }
        }

        public async Task<IReadOnlyList<string>> ListKeysAsync(string prefix, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(prefix);
            await SyncAsync(cancellationToken);
            await _indexGate.WaitAsync(cancellationToken);
            try
            {
                return _current.Where(entry => !entry.Value.Revoked && entry.Key.StartsWith(prefix, StringComparison.Ordinal))
                    .Select(entry => entry.Key).OrderBy(k => k, StringComparer.Ordinal).ToList();
            }
            finally { _indexGate.Release(); }
        }

        public async Task PutAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            await _writeGate.WaitAsync(cancellationToken);
            try
            {
                var previous = await CurrentAsync(key, cancellationToken);
                var seq = (previous?.Seq ?? 0) + 1;
                var receipt = await _web3.Eth.GetContractTransactionHandler<AttestFunction>().SendRequestAndWaitForReceiptAsync(_connection.SpAddress,
                    new AttestFunction
                    {
                        Attestation = new AttestationStruct
                        {
                            SchemaId = _connection.SchemaId, LinkedAttestationId = 0, AttestTimestamp = 0, RevokeTimestamp = 0,
                            Attester = WalletAddress, ValidUntil = 0, DataLocation = OnChain, Revoked = false,
                            Recipients = new List<byte[]>(), Data = EncodeRecord(key, seq, value)
                        },
                        IndexingKey = IndexingKey(key),
                        DelegateSignature = Array.Empty<byte>(),
                        ExtraData = Array.Empty<byte>()
                    }, cancellationToken);
                RequireMined(receipt, "attest");
                var made = receipt.DecodeAllEvents<AttestationMadeEvent>()
                    .Where(e => string.Equals(e.Log.Address, _connection.SpAddress, StringComparison.OrdinalIgnoreCase)).ToList();
                if (made.Count != 1)
                    throw new InvalidOperationException("Sign Protocol attest transaction did not emit exactly one AttestationMade event.");
                var current = await CurrentAsync(key, cancellationToken);
                if (current == null || current.AttestationId != made[0].Event.AttestationId || current.Revoked || current.Value != value)
                    throw new InvalidOperationException($"Sign Protocol attestation {made[0].Event.AttestationId} is not the current record after it was mined.");
            }
            finally { _writeGate.Release(); }
        }

        public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(key);
            await _writeGate.WaitAsync(cancellationToken);
            try
            {
                var current = await CurrentAsync(key, cancellationToken);
                if (current == null || current.Revoked) return;
                var receipt = await _web3.Eth.GetContractTransactionHandler<RevokeFunction>().SendRequestAndWaitForReceiptAsync(_connection.SpAddress,
                    new RevokeFunction
                    {
                        AttestationId = current.AttestationId, Reason = RevokeReason,
                        DelegateSignature = Array.Empty<byte>(), ExtraData = Array.Empty<byte>()
                    }, cancellationToken);
                RequireMined(receipt, "revoke");
                var after = await CurrentAsync(key, cancellationToken);
                if (after == null || after.AttestationId != current.AttestationId || !after.Revoked)
                    throw new InvalidOperationException($"Sign Protocol revocation of attestation {current.AttestationId} was not observed after it was mined.");
            }
            finally { _writeGate.Release(); }
        }

        /// <summary>Proves the RPC serves the configured chain and SP version and that the wallet owns the storage schema, then indexes it.</summary>
        public async Task VerifyAsync(CancellationToken cancellationToken = default)
        {
            await RequireProtocolAsync(_web3, _connection);
            await RequireSchemaAsync();
            await SyncAsync(cancellationToken);
        }

        private async Task<Record> CurrentAsync(string key, CancellationToken cancellationToken)
        {
            await SyncAsync(cancellationToken);
            await _indexGate.WaitAsync(cancellationToken);
            try { return _current.TryGetValue(key, out var record) ? record : null; }
            finally { _indexGate.Release(); }
        }

        /// <summary>Advances the index from the next unread block to the chain head.</summary>
        private async Task SyncAsync(CancellationToken cancellationToken)
        {
            await _indexGate.WaitAsync(cancellationToken);
            try
            {
                var head = (ulong)(await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync()).Value;
                while (_nextBlock <= head)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var to = Math.Min(head, _nextBlock + _connection.MaxLogBlockRange - 1);
                    var made = _web3.Eth.GetEvent<AttestationMadeEvent>(_connection.SpAddress);
                    var madeLogs = await made.GetAllChangesAsync(made.CreateFilterInput(new BlockParameter(_nextBlock), new BlockParameter(to)));
                    foreach (var log in madeLogs.OrderBy(l => l.Log.BlockNumber.Value).ThenBy(l => l.Log.LogIndex.Value))
                        await IndexAttestationAsync(log.Event);
                    var revoked = _web3.Eth.GetEvent<AttestationRevokedEvent>(_connection.SpAddress);
                    var revokedLogs = await revoked.GetAllChangesAsync(revoked.CreateFilterInput(new BlockParameter(_nextBlock), new BlockParameter(to)));
                    foreach (var log in revokedLogs)
                    {
                        if (!_ownAttestations.TryGetValue(log.Event.AttestationId, out var key)) continue;
                        if (_current.TryGetValue(key, out var record) && record.AttestationId == log.Event.AttestationId) record.Revoked = true;
                    }
                    _nextBlock = to + 1;
                }
            }
            finally { _indexGate.Release(); }
        }

        private async Task IndexAttestationAsync(AttestationMadeEvent made)
        {
            if (made.IndexingKey == null || !made.IndexingKey.StartsWith(IndexPrefix, StringComparison.Ordinal)) return;
            var attestation = (await _web3.Eth.GetContractQueryHandler<GetAttestationFunction>()
                .QueryDeserializingToObjectAsync<GetAttestationOutput>(new GetAttestationFunction { AttestationId = made.AttestationId }, _connection.SpAddress)).Attestation;
            // Only this wallet's attestations of this schema are records; everyone can attest with any indexing key.
            if (attestation.SchemaId != _connection.SchemaId || !string.Equals(attestation.Attester, WalletAddress, StringComparison.OrdinalIgnoreCase)) return;
            if (attestation.DataLocation != OnChain || attestation.LinkedAttestationId != 0 || attestation.Recipients is { Count: > 0 })
                throw new InvalidDataException($"Sign Protocol attestation {made.AttestationId} does not have the OASIS record shape.");
            var (key, seq, value) = DecodeRecord(attestation.Data, made.AttestationId);
            if (!string.Equals(IndexingKey(key), made.IndexingKey, StringComparison.Ordinal))
                throw new InvalidDataException($"Sign Protocol attestation {made.AttestationId} indexing key does not match its record key.");
            _ownAttestations.Add(made.AttestationId, key);
            if (_current.TryGetValue(key, out var existing))
            {
                if (seq == existing.Seq)
                    throw new InvalidDataException($"Sign Protocol attestations {existing.AttestationId} and {made.AttestationId} share sequence {seq} for one key.");
                if (seq < existing.Seq) return;
            }
            _current[key] = new Record(made.AttestationId, seq, value) { Revoked = attestation.Revoked };
        }

        private async Task RequireSchemaAsync()
        {
            var schema = (await _web3.Eth.GetContractQueryHandler<GetSchemaFunction>()
                .QueryDeserializingToObjectAsync<GetSchemaOutput>(new GetSchemaFunction { SchemaId = _connection.SchemaId }, _connection.SpAddress)).Schema;
            if (!string.Equals(schema.Registrant, WalletAddress, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Sign Protocol schema {_connection.SchemaId} is registered by {schema.Registrant}, not the configured wallet {WalletAddress}.");
            if (!schema.Revocable || schema.DataLocation != OnChain || schema.MaxValidFor != 0
                || !string.Equals(schema.Hook, ZeroAddress, StringComparison.OrdinalIgnoreCase) || schema.Data != SchemaDefinition)
                throw new InvalidOperationException($"Sign Protocol schema {_connection.SchemaId} is not the OASIS key-value storage schema.");
        }

        private static async Task RequireProtocolAsync(Web3 web3, SignProtocolConnection connection)
        {
            var actual = (await web3.Eth.ChainId.SendRequestAsync()).Value;
            if (actual != connection.ChainId)
                throw new InvalidOperationException($"Sign Protocol RPC serves chain {actual}, not the configured chain {connection.ChainId}.");
            var version = await web3.Eth.GetContractQueryHandler<VersionFunction>().QueryAsync<string>(connection.SpAddress, new VersionFunction());
            if (version != SupportedProtocolVersion)
                throw new InvalidOperationException($"Sign Protocol contract {connection.SpAddress} reports version {version}; {SupportedProtocolVersion} is required.");
        }

        private static void RequireMined(TransactionReceipt receipt, string operation)
        {
            if (receipt == null || receipt.Status == null || receipt.Status.Value != 1)
                throw new InvalidOperationException($"Sign Protocol {operation} transaction {receipt?.TransactionHash} did not succeed on chain.");
        }

        internal static string IndexingKey(string key)
            => IndexPrefix + Sha3Keccack.Current.CalculateHash(Encoding.UTF8.GetBytes(key)).ToHex();

        internal static byte[] EncodeRecord(string key, ulong seq, string value)
            => new ABIEncode().GetABIEncoded(new ABIValue("string", key), new ABIValue("uint64", seq), new ABIValue("string", value));

        internal static (string Key, ulong Seq, string Value) DecodeRecord(byte[] data, ulong attestationId)
        {
            RecordData record;
            try { record = new FunctionCallDecoder().DecodeFunctionOutput<RecordData>(data.ToHex(true)); }
            catch (Exception cause) { throw new InvalidDataException($"Sign Protocol attestation {attestationId} data is not an OASIS record.", cause); }
            if (record?.Key == null || record.Value == null || record.Seq == 0
                || !data.AsSpan().SequenceEqual(EncodeRecord(record.Key, record.Seq, record.Value)))
                throw new InvalidDataException($"Sign Protocol attestation {attestationId} data is not a canonical OASIS record.");
            return (record.Key, record.Seq, record.Value);
        }

        public void Dispose()
        {
            _writeGate.Dispose();
            _indexGate.Dispose();
        }

        private sealed class Record(ulong attestationId, ulong seq, string value)
        {
            public ulong AttestationId { get; } = attestationId;
            public ulong Seq { get; } = seq;
            public string Value { get; } = value;
            public bool Revoked { get; set; }
        }

        [FunctionOutput]
        internal sealed class RecordData : IFunctionOutputDTO
        {
            [Parameter("string", "key", 1)] public string Key { get; set; }
            [Parameter("uint64", "seq", 2)] public ulong Seq { get; set; }
            [Parameter("string", "value", 3)] public string Value { get; set; }
        }

        internal sealed class SchemaStruct
        {
            [Parameter("address", "registrant", 1)] public string Registrant { get; set; }
            [Parameter("bool", "revocable", 2)] public bool Revocable { get; set; }
            [Parameter("uint8", "dataLocation", 3)] public byte DataLocation { get; set; }
            [Parameter("uint64", "maxValidFor", 4)] public ulong MaxValidFor { get; set; }
            [Parameter("address", "hook", 5)] public string Hook { get; set; }
            [Parameter("uint64", "timestamp", 6)] public ulong Timestamp { get; set; }
            [Parameter("string", "data", 7)] public string Data { get; set; }
        }

        internal sealed class AttestationStruct
        {
            [Parameter("uint64", "schemaId", 1)] public ulong SchemaId { get; set; }
            [Parameter("uint64", "linkedAttestationId", 2)] public ulong LinkedAttestationId { get; set; }
            [Parameter("uint64", "attestTimestamp", 3)] public ulong AttestTimestamp { get; set; }
            [Parameter("uint64", "revokeTimestamp", 4)] public ulong RevokeTimestamp { get; set; }
            [Parameter("address", "attester", 5)] public string Attester { get; set; }
            [Parameter("uint64", "validUntil", 6)] public ulong ValidUntil { get; set; }
            [Parameter("uint8", "dataLocation", 7)] public byte DataLocation { get; set; }
            [Parameter("bool", "revoked", 8)] public bool Revoked { get; set; }
            [Parameter("bytes[]", "recipients", 9)] public List<byte[]> Recipients { get; set; }
            [Parameter("bytes", "data", 10)] public byte[] Data { get; set; }
        }

        [Function("register", "uint64")]
        private sealed class RegisterFunction : FunctionMessage
        {
            [Parameter("tuple", "schema", 1)] public SchemaStruct Schema { get; set; }
            [Parameter("bytes", "delegateSignature", 2)] public byte[] DelegateSignature { get; set; }
        }

        [Function("attest", "uint64")]
        private sealed class AttestFunction : FunctionMessage
        {
            [Parameter("tuple", "attestation", 1)] public AttestationStruct Attestation { get; set; }
            [Parameter("string", "indexingKey", 2)] public string IndexingKey { get; set; }
            [Parameter("bytes", "delegateSignature", 3)] public byte[] DelegateSignature { get; set; }
            [Parameter("bytes", "extraData", 4)] public byte[] ExtraData { get; set; }
        }

        [Function("revoke")]
        private sealed class RevokeFunction : FunctionMessage
        {
            [Parameter("uint64", "attestationId", 1)] public ulong AttestationId { get; set; }
            [Parameter("string", "reason", 2)] public string Reason { get; set; }
            [Parameter("bytes", "delegateSignature", 3)] public byte[] DelegateSignature { get; set; }
            [Parameter("bytes", "extraData", 4)] public byte[] ExtraData { get; set; }
        }

        [Function("getSchema", typeof(GetSchemaOutput))]
        private sealed class GetSchemaFunction : FunctionMessage
        {
            [Parameter("uint64", "schemaId", 1)] public ulong SchemaId { get; set; }
        }

        [FunctionOutput]
        internal sealed class GetSchemaOutput : IFunctionOutputDTO
        {
            [Parameter("tuple", "", 1)] public SchemaStruct Schema { get; set; }
        }

        [Function("getAttestation", typeof(GetAttestationOutput))]
        private sealed class GetAttestationFunction : FunctionMessage
        {
            [Parameter("uint64", "attestationId", 1)] public ulong AttestationId { get; set; }
        }

        [FunctionOutput]
        internal sealed class GetAttestationOutput : IFunctionOutputDTO
        {
            [Parameter("tuple", "", 1)] public AttestationStruct Attestation { get; set; }
        }

        [Function("version", "string")]
        private sealed class VersionFunction : FunctionMessage { }

        [Event("SchemaRegistered")]
        private sealed class SchemaRegisteredEvent : IEventDTO
        {
            [Parameter("uint64", "schemaId", 1, false)] public ulong SchemaId { get; set; }
        }

        [Event("AttestationMade")]
        private sealed class AttestationMadeEvent : IEventDTO
        {
            [Parameter("uint64", "attestationId", 1, false)] public ulong AttestationId { get; set; }
            [Parameter("string", "indexingKey", 2, false)] public string IndexingKey { get; set; }
        }

        [Event("AttestationRevoked")]
        private sealed class AttestationRevokedEvent : IEventDTO
        {
            [Parameter("uint64", "attestationId", 1, false)] public ulong AttestationId { get; set; }
            [Parameter("string", "reason", 2, false)] public string Reason { get; set; }
        }
    }
}
