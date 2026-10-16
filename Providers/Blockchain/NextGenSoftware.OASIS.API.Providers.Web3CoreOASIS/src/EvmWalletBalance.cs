using System;
using System.IO;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Web3;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.Providers.Shared.Evm;

// Native EVM coin only; the wallet-balance request has no ERC20 contract field.
internal static class EvmWalletBalance
{
    internal static async Task<OASISResult<double>> ReadAsync(IGetWeb3WalletBalanceRequest request,
        Func<Task<OASISResult<Web3>>> initialize)
    {
        var result=new OASISResult<double>();
        try
        {
            if(request==null || !Nethereum.Util.AddressUtil.Current.IsValidEthereumAddressHexFormat(request.WalletAddress))
                throw new ArgumentException("A valid EVM wallet address is required.",nameof(request));
            var ready=await initialize();
            if(ready.IsError)
            {
                OASISErrorHandling.HandleError(ref result,ready.Message,ready.Exception);result.ErrorCode=ready.ErrorCode;return result;
            }
            var client=ready.Result??throw new InvalidOperationException("EVM balance client was not initialized.");
            var balance=await client.Eth.GetBalance.SendRequestAsync(request.WalletAddress);
            if(balance==null || balance.Value<0 || balance.Value>=(BigInteger.One<<256))
                throw new InvalidDataException("Native EVM balance is missing or outside uint256.");
            // The public interface returns double. Avoid an overflowing decimal intermediate;
            // this is an approximate display amount, not an exact amount for signing/payments.
            result.Result=(double)balance.Value/1_000_000_000_000_000_000d;
            result.IsLoaded=true;result.LoadedCount=1;
            result.Message="Native EVM coin balance (18 decimal places); approximate double display amount.";
        }
        catch(Exception cause){OASISErrorHandling.HandleError(ref result,"EVM balance read failed: "+cause.Message,cause);}
        return result;
    }
}
