using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using NBitcoin;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.Common;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Providers.BitcoinOASIS;

public partial class BitcoinOASIS
{
    private async Task<OASISResult<ITransactionResponse>> BurnNativeBitcoinAsync(IBurnWeb3TokenRequest request)
    {
        var result = new OASISResult<ITransactionResponse>();
        try
        {
            if (request == null ||
                (!string.IsNullOrWhiteSpace(request.TokenAddress) && !string.Equals(request.TokenAddress, "BTC", StringComparison.OrdinalIgnoreCase)))
                throw new ArgumentException("The Bitcoin burn requires a native BTC request.");

            var satoshis = request.Amount * 100_000_000m;
            if (satoshis <= 0 || satoshis > 2_100_000_000_000_000m || satoshis != decimal.Truncate(satoshis))
                throw new ArgumentException("The burn amount must be a positive whole number of satoshis within the Bitcoin supply limit.");
            var amount = Money.Satoshis((long)satoshis);

            if (!_isActivated)
            {
                var activation = await ActivateProviderAsync();
                if (activation.IsError)
                    throw new InvalidOperationException("Could not activate the Bitcoin provider for the burn.");
            }

            var signingKey = request.OwnerPrivateKey;
            var walletAddress = (string)null;
            if (request.BurntByAvatarId != Guid.Empty)
            {
                var needsSigner = string.IsNullOrWhiteSpace(signingKey) && string.IsNullOrWhiteSpace(request.OwnerSeedPhrase);
                var wallet = await WalletManager.GetAvatarDefaultWalletByIdAsync(request.BurntByAvatarId, ProviderType.Value,
                    showPrivateKeys: needsSigner);
                if (wallet.IsError || string.IsNullOrWhiteSpace(wallet.Result?.WalletAddress))
                    throw new InvalidOperationException("The burning avatar does not have a Bitcoin wallet.");
                walletAddress = wallet.Result.WalletAddress;
                if (needsSigner)
                    signingKey = wallet.Result.PrivateKey;
            }
            if (string.IsNullOrWhiteSpace(signingKey) && string.IsNullOrWhiteSpace(request.OwnerSeedPhrase))
                throw new ArgumentException("A Bitcoin owner signing key is required to burn BTC.");

            var network = SigningNetwork;
            var signer = new BitcoinSecret(ResolveWalletSigningKey(signingKey, request.OwnerSeedPhrase), network);
            var supported = new[]
            {
                signer.GetAddress(ScriptPubKeyType.Segwit),
                signer.GetAddress(ScriptPubKeyType.TaprootBIP86),
                signer.GetAddress(ScriptPubKeyType.SegwitP2SH),
                signer.GetAddress(ScriptPubKeyType.Legacy)
            };
            var addresses = supported;
            if (walletAddress != null)
            {
                var expected = BitcoinAddress.Create(walletAddress, network);
                addresses = supported.Where(address => address.Equals(expected)).ToArray();
                if (addresses.Length == 0)
                    throw new ArgumentException("The Bitcoin wallet does not belong to the burn signing key.");
            }

            using var ord = CreateOrdClient();
            var tip = await ReadSharedOrdinalTipAsync(ord);
            if (!uint256.TryParse((await _httpClient.GetStringAsync("block-height/0")).Trim(), out var genesis) ||
                genesis != network.GetGenesis().GetHash())
                throw new InvalidOperationException("Esplora is serving a different Bitcoin network.");

            var feeRate = await ReadBitcoinPaymentFeeRateAsync();
            foreach (var address in addresses)
            {
                var wrappedSegwit = address.Equals(supported[2]);
                var builder = network.CreateTransactionBuilder().AddKeys(signer.PrivateKey);
                var transaction = network.CreateTransaction();
                transaction.Outputs.Add(amount, BitcoinBurnScript);
                var change = transaction.Outputs.Add(Money.Zero, address.ScriptPubKey);
                var available = Money.Zero;
                using var utxos = JsonDocument.Parse(await _httpClient.GetStringAsync($"address/{address}/utxo"));
                foreach (var utxo in utxos.RootElement.EnumerateArray())
                {
                    if (!utxo.GetProperty("status").GetProperty("confirmed").GetBoolean() ||
                        !uint256.TryParse(utxo.GetProperty("txid").GetString(), out var previousId))
                        continue;

                    var outpoint = new OutPoint(previousId, utxo.GetProperty("vout").GetUInt32());
                    var coin = await ReadOrdinalCoinAsync(ord, outpoint, address, signer, wrappedSegwit, null);
                    if (coin == null)
                        continue;

                    builder.AddCoins(coin);
                    transaction.Inputs.Add(outpoint);
                    available += coin.Amount;
                    var fee = builder.EstimateFees(transaction, feeRate);
                    if (available <= amount + fee)
                        continue;
                    change.Value = available - amount - fee;
                    if (change.IsDust())
                        continue;

                    builder.SignTransactionInPlace(transaction);
                    if (!builder.Verify(transaction))
                        throw new InvalidOperationException("The signed Bitcoin burn failed local transaction policy.");
                    if (await ReadSharedOrdinalTipAsync(ord) != tip)
                        throw new InvalidOperationException("The indexed Bitcoin chain changed while preparing the burn.");

                    return await BroadcastBitcoinBurnAsync(transaction, amount, tip);
                }
            }

            throw new InvalidOperationException("The signing wallet has insufficient confirmed inscription-free and Rune-free BTC.");
        }
        catch (Exception ex)
        {
            OASISErrorHandling.HandleError(ref result, ex is ArgumentException or InvalidOperationException
                ? ex.Message : $"Native Bitcoin burn failed ({ex.GetType().Name}).");
        }
        return result;
    }
}
