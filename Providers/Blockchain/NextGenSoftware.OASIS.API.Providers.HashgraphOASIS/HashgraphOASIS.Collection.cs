using System;
using System.Globalization;
using System.Linq;
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
    public async Task<OASISResult<HederaNftCollectionResponse>> UpdateNftMetadataAsync(
        string senderAccountId, string privateKeyDer, string tokenId, long[] serials, string metadata,
        long maxFeeTinybars, CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<HederaNftCollectionResponse>();
        try
        {
            foreach (var id in new[] { senderAccountId, tokenId })
                if (!Regex.IsMatch(id ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                    throw new ArgumentException("Explicit nonzero numeric payer and collection token IDs are required.");
            if (serials == null || serials.Length < 1 || serials.Length > 10 || serials.Any(value => value <= 0)
                || serials.Distinct().Count() != serials.Length)
                throw new ArgumentException("One to ten unique positive int64 NFT serials are required.");
            if (metadata == null || Encoding.UTF8.GetByteCount(metadata) > 100)
                throw new ArgumentException("Explicit NFT metadata of at most 100 UTF-8 bytes is required.");
            if (maxFeeTinybars <= 0 || string.IsNullOrWhiteSpace(privateKeyDer))
                throw new ArgumentException("An explicit DER metadata signing key and positive fee ceiling are required.");
            cancellationToken.ThrowIfCancellationRequested();
            var transactionId = HederaSdkClient.CreateTransactionId(senderAccountId);
            result.Result = new HederaNftCollectionResponse { TransactionId = transactionId, TokenId = tokenId };
            var receipt = await HederaSdkClient.InvokeAsync("nftMetadataUpdate", new
            {
                network = _hederaNetwork, nodes = _consensusNodes, sender = senderAccountId,
                privateKey = privateKeyDer, keyType = "der", tokenId, metadata,
                serials = serials.Select(value => value.ToString(CultureInfo.InvariantCulture)).ToArray(),
                maxFeeTinybars = maxFeeTinybars.ToString(CultureInfo.InvariantCulture), transactionId
            }, cancellationToken);
            if (receipt.GetProperty("status").GetString() != "SUCCESS"
                || receipt.GetProperty("transactionId").GetString() != transactionId)
                throw new InvalidOperationException("NFT metadata update receipt identity mismatch; reconcile the original transaction.");
            result.Result.ConsensusVerified = true;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    public async Task<OASISResult<HederaNftCollectionResponse>> DeleteTokenCollectionAsync(
        string senderAccountId, string privateKeyDer, string tokenId, long maxFeeTinybars,
        CancellationToken cancellationToken = default)
    {
        var result = new OASISResult<HederaNftCollectionResponse>();
        try
        {
            foreach (var id in new[] { senderAccountId, tokenId })
                if (!Regex.IsMatch(id ?? "", @"\A[0-9]+\.[0-9]+\.[1-9][0-9]*\z"))
                    throw new ArgumentException("Explicit nonzero numeric payer and collection token IDs are required.");
            if (maxFeeTinybars <= 0 || string.IsNullOrWhiteSpace(privateKeyDer))
                throw new ArgumentException("An explicit DER admin signing key and positive fee ceiling are required.");
            cancellationToken.ThrowIfCancellationRequested();
            var transactionId = HederaSdkClient.CreateTransactionId(senderAccountId);
            result.Result = new HederaNftCollectionResponse { TransactionId = transactionId, TokenId = tokenId };
            var receipt = await HederaSdkClient.InvokeAsync("tokenCollectionDelete", new
            {
                network = _hederaNetwork, nodes = _consensusNodes, sender = senderAccountId,
                privateKey = privateKeyDer, keyType = "der", tokenId,
                maxFeeTinybars = maxFeeTinybars.ToString(CultureInfo.InvariantCulture), transactionId
            }, cancellationToken);
            if (receipt.GetProperty("status").GetString() != "SUCCESS"
                || receipt.GetProperty("transactionId").GetString() != transactionId)
                throw new InvalidOperationException("Collection deletion receipt identity mismatch; reconcile the original transaction.");
            result.Result.ConsensusVerified = true;
        }
        catch (Exception ex) { OASISErrorHandling.HandleError(ref result, ex.Message, ex); }
        return result;
    }

    // Explicit native operation; credentials go to the SDK through stdin only.
    public async Task<OASISResult<HederaNftCollectionResponse>> CreateNftCollectionAsync(
        string senderAccountId, string privateKeyDer, string name, string symbol,
        long maxSupply, long maxFeeTinybars, CancellationToken cancellationToken = default, int royaltyPercentage = 0)
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
            if (royaltyPercentage < 0 || royaltyPercentage > 100)
                throw new ArgumentException("Collection royalty percentage must be between 0 and 100.");
            if (string.IsNullOrWhiteSpace(privateKeyDer))
                throw new ArgumentException("An explicit DER collection signing key is required.");
            cancellationToken.ThrowIfCancellationRequested();
            var id = HederaSdkClient.CreateTransactionId(senderAccountId);
            result.Result = new HederaNftCollectionResponse { TransactionId = id };
            var receipt = await HederaSdkClient.InvokeAsync("nftCollectionCreate", new
            {
                network = _hederaNetwork, nodes = _consensusNodes, sender = senderAccountId,
                privateKey = privateKeyDer, keyType = "der", name, symbol, royaltyPercentage,
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
