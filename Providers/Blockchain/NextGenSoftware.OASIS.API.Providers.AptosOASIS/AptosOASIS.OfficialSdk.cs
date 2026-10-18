using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aptos;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace NextGenSoftware.OASIS.API.Providers.AptosOASIS
{
    public partial class AptosOASIS
    {
        private const string HolonRecordType = "holon";

        private void EnsureTransactionAccount()
        {
            if (_account == null)
                throw new InvalidOperationException("Aptos PrivateKey must be configured for write operations.");
        }

        private Task<string> ExecuteEntryFunctionAsync(string functionName, params object[] arguments) =>
            ExecuteEntryFunctionAsync($"{_contractAddress}::oasis::{functionName}", Array.Empty<object>(), arguments);

        private async Task<string> ExecuteEntryFunctionAsync(string functionId, IReadOnlyList<object> typeArguments, params object[] arguments)
        {
            EnsureTransactionAccount();
            var transaction = await _aptosClient.Transaction.Build(
                _account.Address,
                new GenerateEntryFunctionPayloadData(
                    functionId,
                    new List<object>(arguments),
                    new List<object>(typeArguments)),
                withFeePayer: false,
                options: new TransactionBuilder.GenerateTransactionOptions(maxGasAmount: 100_000));
            var pending = await _aptosClient.Transaction.SignAndSubmitTransaction(_account, transaction);
            var committed = await _aptosClient.Transaction.WaitForTransaction(pending);
            if (!committed.Success)
                throw new InvalidOperationException($"Aptos transaction {committed.Hash} failed: {committed.VmStatus}");
            return committed.Hash.ToString();
        }

        private async Task UpsertRecordAsync(string recordType, string providerKey, object value)
        {
            await UpsertRecordWithHashAsync(recordType, providerKey, value);
        }

        private Task<string> UpsertRecordWithHashAsync(string recordType, string providerKey, object value)
        {
            var payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
            return ExecuteEntryFunctionAsync(
                "upsert_record",
                Encoding.UTF8.GetBytes(recordType),
                Encoding.UTF8.GetBytes(providerKey),
                payload);
        }

        private async Task<T> GetRecordAsync<T>(string recordType, string providerKey)
        {
            EnsureTransactionAccount();
            var values = await _aptosClient.Contract.View(
                new GenerateViewFunctionPayloadData(
                    $"{_contractAddress}::oasis::get_record",
                    new List<object>
                    {
                        _account.Address.ToString(),
                        Encoding.UTF8.GetBytes(recordType),
                        Encoding.UTF8.GetBytes(providerKey)
                    },
                    new List<object>()));
            if (values == null || values.Count == 0)
                return default;

            byte[] bytes = values[0] switch
            {
                JArray array => array.ToObject<byte[]>(),
                byte[] raw => raw,
                string hex => Hex.FromHexInput(hex).ToByteArray(),
                _ => throw new InvalidOperationException($"Unexpected Aptos record response: {values[0]?.GetType().FullName}")
            };
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(bytes));
        }

        private async Task<bool> HasRecordAsync(string recordType, string providerKey)
        {
            EnsureTransactionAccount();
            var values = await _aptosClient.Contract.View(
                new GenerateViewFunctionPayloadData(
                    $"{_contractAddress}::oasis::has_record",
                    new List<object>
                    {
                        _account.Address.ToString(),
                        Encoding.UTF8.GetBytes(recordType),
                        Encoding.UTF8.GetBytes(providerKey)
                    },
                    new List<object>()));
            return values != null && values.Count > 0 && values[0] switch
            {
                bool value => value,
                JValue value when value.Type == JTokenType.Boolean => value.Value<bool>(),
                string value when bool.TryParse(value, out var parsed) => parsed,
                _ => false
            };
        }

        private async Task<IReadOnlyList<string>> GetRecordKeysAsync(string recordType)
        {
            EnsureTransactionAccount();
            var values = await _aptosClient.Contract.View(
                new GenerateViewFunctionPayloadData(
                    $"{_contractAddress}::oasis::get_record_keys",
                    new List<object> { _account.Address.ToString() },
                    new List<object>()));
            if (values == null || values.Count == 0)
                return Array.Empty<string>();

            var keys = values[0] switch
            {
                JArray array => array.Select(DecodeMoveBytes).ToList(),
                IEnumerable<byte[]> raw => raw.ToList(),
                _ => throw new InvalidOperationException($"Unexpected Aptos keys response: {values[0]?.GetType().FullName}")
            };
            var prefix = Encoding.UTF8.GetBytes(recordType + "\0");
            return (keys ?? new List<byte[]>())
                .Where(key => key.Length >= prefix.Length && key.AsSpan(0, prefix.Length).SequenceEqual(prefix))
                .Select(key => Encoding.UTF8.GetString(key, prefix.Length, key.Length - prefix.Length))
                .ToList();
        }

        private static byte[] DecodeMoveBytes(JToken token) => token switch
        {
            JArray array => array.Select(value => value.Value<byte>()).ToArray(),
            JValue value when value.Type == JTokenType.String => Hex.FromHexInput(value.Value<string>()).ToByteArray(),
            _ => throw new InvalidOperationException($"Unexpected Move byte-vector value: {token.Type}")
        };

        private Task DeleteRecordAsync(string recordType, string providerKey) =>
            DeleteRecordWithHashAsync(recordType, providerKey);

        private Task<string> DeleteRecordWithHashAsync(string recordType, string providerKey) =>
            ExecuteEntryFunctionAsync(
                "delete_record",
                Encoding.UTF8.GetBytes(recordType),
                Encoding.UTF8.GetBytes(providerKey));

        private async Task<ulong> GetNativeAptBalanceOctasAsync(string address)
        {
            var values = await _aptosClient.Contract.View(new GenerateViewFunctionPayloadData(
                "0x1::coin::balance",
                new List<object> { address },
                new List<object> { "0x1::aptos_coin::AptosCoin" }));
            if (values == null || values.Count == 0) return 0;
            return values[0] switch
            {
                JValue value when ulong.TryParse(value.ToString(), out var parsed) => parsed,
                string value when ulong.TryParse(value, out var parsed) => parsed,
                ulong value => value,
                long value when value >= 0 => (ulong)value,
                _ => throw new InvalidOperationException($"Unexpected Aptos balance response: {values[0]}")
            };
        }
    }
}
