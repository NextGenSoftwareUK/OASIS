using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Nethereum.Contracts;
using Nethereum.RPC.Eth.DTOs;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.Common;
using Nethereum.Hex.HexConvertors.Extensions;
using NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS
{
    public partial class EthereumOASIS
    {
        private readonly System.Threading.SemaphoreSlim _storageMutationGate=new(1,1);
        private async Task<TransactionReceipt> SubmitStorageRequestAsync<TFunction>(TFunction request)
            where TFunction : FunctionMessage, new()
        {
            await _storageMutationGate.WaitAsync();
            string transactionId=null;
            try
            {
                var web3=Web3Client??throw new InvalidOperationException("Ethereum storage provider is not activated.");
                var contract=ContractAddress;var chain=ChainId;var sender=web3.TransactionManager.Account.Address;
                var data=new Nethereum.Contracts.MessageEncodingServices.FunctionMessageEncodingService<TFunction>().GetCallData(request);
                // Explicit pending nonce avoids consuming the SDK nonce cache on a failed preparation.
                request.Nonce=await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(sender,BlockParameter.CreatePending());
                // The SDK sign-only handler retains the same gas-estimation/fee-pricing path as send.
                var signed=await web3.Eth.GetContractTransactionHandler<TFunction>().SignTransactionAsync(contract,request);
                var bytes=signed.HexToByteArray();transactionId=Web3SignedPreparation.Hash(bytes);
                Web3SignedPreparation.Verify(bytes,transactionId,chain,sender,contract,data,System.Numerics.BigInteger.Zero);
                var returnedHash=await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(signed.EnsureHexPrefix());
                if(!string.Equals(transactionId,returnedHash,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Ethereum submission identity differs from original signed transaction.");
                var receipt=await PollStorageReceiptAsync(transactionId);
                if(receipt==null || !string.Equals(receipt.TransactionHash,transactionId,StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Ethereum storage receipt does not match the submitted transaction.");
                return receipt;
            }
            catch(Exception cause)
            {
                // A prepared hash identifies signed bytes, not acceptance. Preserve it on response loss.
                // Never replay an uncertain original or fabricate a returned native identity.
                throw new EthereumStorageReceiptReconciliationException(transactionId,cause);
            }
            finally {_storageMutationGate.Release();}
        }

        protected virtual Task<TransactionReceipt> PollStorageReceiptAsync(string transactionId)
            => Web3Client.TransactionManager.TransactionReceiptService.PollForReceiptAsync(transactionId);

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
        // Null means no signed-byte identity was derived. A hash does not prove submission/mining.
        // Retain the original identity and reconcile authoritative native state before replay.
        public string OriginalTransactionId { get; }
        public EthereumStorageReceiptReconciliationException(string identity,Exception cause=null)
            : base("Ethereum storage receipt lacks an authoritative outcome or transaction identity; reconcile before replay.",cause)
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
