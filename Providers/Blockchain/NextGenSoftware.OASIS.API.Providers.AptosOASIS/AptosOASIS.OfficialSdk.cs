using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Aptos;
using Newtonsoft.Json.Linq;

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

        private async Task ExecuteEntryFunctionAsync(string functionName, params object[] arguments)
        {
            EnsureTransactionAccount();
            var transaction = await _aptosClient.Transaction.Build(
                _account.Address,
                new GenerateEntryFunctionPayloadData(
                    $"{_contractAddress}::oasis::{functionName}",
                    new List<object>(arguments),
                    new List<object>()),
                withFeePayer: false,
                options: new TransactionBuilder.GenerateTransactionOptions(maxGasAmount: 100_000));
            var pending = await _aptosClient.Transaction.SignAndSubmitTransaction(_account, transaction);
            var committed = await _aptosClient.Transaction.WaitForTransaction(pending);
            if (!committed.Success)
                throw new InvalidOperationException($"Aptos transaction {committed.Hash} failed: {committed.VmStatus}");
        }

        private async Task UpsertRecordAsync(string recordType, string providerKey, object value)
        {
            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, value.GetType()));
            await ExecuteEntryFunctionAsync(
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
            return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes));
        }

        private Task DeleteRecordAsync(string recordType, string providerKey) =>
            ExecuteEntryFunctionAsync(
                "delete_record",
                Encoding.UTF8.GetBytes(recordType),
                Encoding.UTF8.GetBytes(providerKey));
    }
}
