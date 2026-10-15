using System;
using System.Collections.Generic;
using System.Linq;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private static bool StorageReceiptSucceeded(TransactionReceipt receipt)
        {
            if (receipt?.Status == null || !IsStorageTransactionIdentity(receipt.TransactionHash)
                || (receipt.Status.Value != 0 && receipt.Status.Value != 1))
                throw new EthereumStorageReceiptReconciliationException(receipt?.TransactionHash);
            return receipt.Status.Value == 1;
        }

        internal static bool IsStorageTransactionIdentity(string identity)
            => identity != null && identity.Length == 66 && identity.StartsWith("0x",StringComparison.Ordinal)
                && identity.Skip(2).All(Uri.IsHexDigit);

        private static void FencePartialStorageWrite<T>(OASISResult<T> result, List<string> committed)
        {
            if (result.IsError && committed.Count != 0)
                result.Exception = new EthereumPartialStorageMutationException(committed,
                    result.Exception ?? new InvalidOperationException(result.Message));
        }
    }

    public sealed class EthereumStorageReceiptReconciliationException : Exception, INativeMutationReconciliationRequired
    {
        // Null means the SDK supplied no native identity: never fabricate one or replay.
        // This must remain fenced until authoritative sender/nonce evidence is recovered.
        public string OriginalTransactionId { get; }
        public EthereumStorageReceiptReconciliationException(string identity)
            : base("Ethereum storage receipt lacks an authoritative outcome or transaction identity; reconcile before replay.")
            => OriginalTransactionId = identity;
    }

    public sealed class EthereumPartialStorageMutationException : Exception, INativeMutationReconciliationRequired
    {
        public string OriginalTransactionId { get; }
        public IReadOnlyList<string> CommittedTransactions { get; }
        public EthereumPartialStorageMutationException(IEnumerable<string> committed, Exception cause)
            : base("Ethereum bulk storage partially committed; reconcile before replay.", cause)
        {
            var identities = committed.ToArray();
            if (identities.Length == 0 || identities.Any(identity => !EthereumOASIS.IsStorageTransactionIdentity(identity)))
                throw new ArgumentException("Partial-write evidence requires actual committed transaction identities.", nameof(committed));
            CommittedTransactions = Array.AsReadOnly(identities);
            OriginalTransactionId = identities[0];
        }
    }
}
