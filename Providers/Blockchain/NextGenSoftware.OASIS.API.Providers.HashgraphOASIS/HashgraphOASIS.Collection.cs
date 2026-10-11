using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.HashgraphOASIS;

public sealed class HederaNftCollectionResponse
{
    public string TransactionId { get; set; }
    public string TokenId { get; set; }
    public bool ConsensusVerified { get; set; }
}

public partial class HashgraphOASIS
{
    // Explicit native operation; credentials go to the SDK through stdin only.
    public async Task<OASISResult<HederaNftCollectionResponse>> CreateNftCollectionAsync(
        string senderAccountId, string privateKeyDer, string name, string symbol,
        long maxSupply, long maxFeeTinybars, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<HederaNftCollectionResponse>();
        try
        {
            if (!Regex.IsMatch(senderAccountId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                throw new ArgumentException("An explicit nonzero numeric collection payer is required.");
            foreach (var value in new[] { name, symbol })
                if (string.IsNullOrWhiteSpace(value) || Encoding.UTF8.GetByteCount(value) > 100)
                    throw new ArgumentException("Collection name and symbol require 1 to 100 UTF-8 bytes.");
            if (maxSupply <= 0 || maxFeeTinybars <= 0)
                throw new ArgumentException("Collection supply and fee ceiling must be positive int64 values.");
            if (string.IsNullOrWhiteSpace(privateKeyDer))
                throw new ArgumentException("An explicit DER collection signing key is required.");
            cancellationToken.ThrowIfCancellationRequested();
            var id = HederaSdkClient.CreateTransactionId(senderAccountId);
            result.Result = new HederaNftCollectionResponse { TransactionId = id };
            var receipt = await HederaSdkClient.InvokeAsync("nftCollectionCreate", new
            {
                network = _hederaNetwork, nodes = _consensusNodes, sender = senderAccountId,
                privateKey = privateKeyDer, keyType = "der", name, symbol,
                maxSupply = maxSupply.ToString(CultureInfo.InvariantCulture),
                maxFeeTinybars = maxFeeTinybars.ToString(CultureInfo.InvariantCulture), transactionId = id
            }, cancellationToken);
            if (receipt.GetProperty("status").GetString() != "SUCCESS"
                || receipt.GetProperty("transactionId").GetString() != id)
                throw new InvalidOperationException("Collection consensus receipt identity mismatch; reconcile the original transaction.");
            var tokenId = receipt.GetProperty("tokenId").GetString();
            if (!Regex.IsMatch(tokenId ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                throw new InvalidOperationException("Collection receipt has no valid created token ID; reconcile the original transaction.");
            result.Result.TokenId = tokenId;
            result.Result.ConsensusVerified = true;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }
}
