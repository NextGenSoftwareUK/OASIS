using System;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Contracts;
using NextGenSoftware.OASIS.API.Core.Interfaces;

[assembly:InternalsVisibleTo("NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS.UnitTests")]

namespace NextGenSoftware.OASIS.API.Providers.Web3CoreOASIS;

public sealed class Web3MutationReconciliationException : Exception, INativeMutationReconciliationRequired
{
    public string OriginalTransactionId {get;}
    public TransactionReceipt ObservedReceipt {get;}
    public Web3MutationReconciliationException(string hash,TransactionReceipt receipt,Exception cause)
        :base("EVM mutation did not complete successfully; inspect retained outcome before replay.",cause)
    {OriginalTransactionId=hash ?? string.Empty;ObservedReceipt=receipt;}
}

internal static class Web3TransactionOutcome
{
    internal static async Task<TransactionReceipt> SubmitAndObserveAsync(Func<Task<string>> submit,
        Func<string,Task<TransactionReceipt>> observe,TimeSpan window,TimeSpan interval,string preparedHash=null)
    {
        ArgumentNullException.ThrowIfNull(submit);ArgumentNullException.ThrowIfNull(observe);
        if(window<=TimeSpan.Zero || interval<=TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(window));
        string hash=preparedHash;TransactionReceipt receipt=null;
        try
        {
            var returnedHash=await submit();
            if(preparedHash!=null && !string.Equals(preparedHash,returnedHash,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("EVM submission identity differs from original signed transaction.");
            hash=returnedHash;
            if(string.IsNullOrWhiteSpace(hash)) throw new InvalidOperationException("EVM submission returned no transaction hash.");
            using var timeout=new CancellationTokenSource(window);
            while(true)
            {
                timeout.Token.ThrowIfCancellationRequested();
                receipt=await observe(hash);
                if(receipt!=null)
                {
                    if(!string.Equals(receipt.TransactionHash,hash,StringComparison.OrdinalIgnoreCase))
                        throw new InvalidOperationException("EVM receipt identity differs from submitted transaction.");
                    if(receipt.Status==null) throw new InvalidOperationException("EVM receipt lacks execution status.");
                    if(!receipt.Succeeded()) throw new InvalidOperationException($"EVM transaction '{hash}' reverted.");
                    return receipt;
                }
                await Task.Delay(interval,timeout.Token);
            }
        }
        catch(Exception cause) {throw new Web3MutationReconciliationException(hash,receipt,cause);}
    }
}
