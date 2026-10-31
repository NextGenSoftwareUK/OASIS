using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Response;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;
using static NextGenSoftware.Utilities.KeyHelper;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public partial class HashgraphOASIS
{
    public async Task<OASISResult<IKeyPairAndWallet>> RestoreKeyPairAsync(string privateKey)
    {
        var result = new OASISResult<IKeyPairAndWallet>();
        try
        {
            var restored = await HederaSdkClient.InvokeAsync("restoreKey", new { privateKey, keyType = "der" });
            result.Result = new KeyPairAndWallet
            {
                PrivateKey = restored.GetProperty("privateKey").GetString(),
                PublicKey = restored.GetProperty("publicKey").GetString(), WalletAddressLegacy = null
            };
            result.Message = "Hedera SDK key restored. Link the actual ledger account separately.";
        }
        catch (Exception error) { OASISErrorHandling.HandleError(ref result, error.Message, error); }
        return result;
    }

    public OASISResult<IKeyPairAndWallet> RestoreKeyPair(string privateKey)
        => RestoreKeyPairAsync(privateKey).GetAwaiter().GetResult();
}
