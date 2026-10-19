using System;
using System.Linq;
using System.Threading.Tasks;
using Solnet.Programs;
using Solnet.Wallet;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Responses;
using NextGenSoftware.OASIS.API.Core.Objects.Wallet.Responses;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Repositories;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS;

public partial class SolanaOASIS : IOASISTokenIssuerProvider
{
    public OASISResult<ICreateTokenResponse> CreateToken(ICreateWeb3TokenRequest request) => CreateTokenAsync(request).GetAwaiter().GetResult();

    /// <summary>
    /// Issues a new legacy SPL token. The creating avatar's Solana wallet named in
    /// <see cref="ICreateWeb3TokenRequest.AuthorityWalletAddress"/> becomes the mint authority and pays the fees;
    /// the resulting mint is read back from the chain and verified before success is reported.
    /// </summary>
    public async Task<OASISResult<ICreateTokenResponse>> CreateTokenAsync(ICreateWeb3TokenRequest request)
    {
        var result = new OASISResult<ICreateTokenResponse>();
        string signature = null;
        try
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.CreatedByAvatarId == Guid.Empty || string.IsNullOrWhiteSpace(request.AuthorityWalletAddress))
                throw new ArgumentException("The creating avatar and its authority wallet address are required.");
            if (request.Decimals < 0 || request.Decimals > 9)
                throw new ArgumentOutOfRangeException(nameof(request.Decimals), "SPL token decimals must be between 0 and 9.");
            var authorityAddress = new PublicKey(request.AuthorityWalletAddress);

            var keys = ResolveTokenSigningKeys(request.CreatedByAvatarId);
            if (keys.IsError)
            {
                OASISErrorHandling.HandleError(ref result, keys.Message, keys.Exception);
                result.ErrorCode = keys.ErrorCode;
                return result;
            }
            // Only a key that validates against the named authority address may sign; other avatar keys are not tried as substitutes.
            var authority = (keys.Result ?? new()).Where(key => !string.IsNullOrWhiteSpace(key)).Distinct(StringComparer.Ordinal)
                .Select(key =>
                {
                    try { return ExplicitTokenSigner(key, authorityAddress.Key); }
                    catch (Exception cause) when (cause is ArgumentException or FormatException) { return null; }
                })
                .FirstOrDefault(account => account != null)
                ?? throw new UnauthorizedAccessException("The authority wallet is not one of the avatar's Solana wallets.");

            var created = await new SolanaLegacyTokenClient(_rpcClient).CreateLegacyMintAsync(authority, request.Decimals, request.InitialSupply, request.MemoText);
            signature = created.Signature;

            var mint = await ReadLegacyMintAsync(created.Mint);
            if (mint.Decimals != request.Decimals || mint.MintAuthority?.Key != authority.PublicKey.Key || mint.FreezeAuthority != null || mint.Supply != created.RawSupply)
                throw new System.IO.InvalidDataException("The finalized SPL mint does not match the requested decimals, authority or supply.");
            if (created.RawSupply > 0)
            {
                var holder = AssociatedTokenAccountProgram.DeriveAssociatedTokenAccount(authority.PublicKey, created.Mint);
                var account = await ReadLegacyTokenAccountAsync(holder, created.Mint, authority.PublicKey, false);
                if (account.Amount != created.RawSupply)
                    throw new System.IO.InvalidDataException("The finalized initial supply was not credited to the authority wallet.");
            }

            result.Result = new CreateTokenResponse
            {
                TransactionResult = signature,
                TokenAddress = created.Mint.Key,
                Decimals = request.Decimals,
                InitialSupply = request.InitialSupply
            };
            result.IsSaved = true;
        }
        catch (Exception cause)
        {
            if (signature != null && cause is not SolanaTransactionReconciliationException)
                cause = new SolanaTransactionReconciliationException(signature, cause);
            OASISErrorHandling.HandleError(ref result, "Solana token creation failed.", cause);
        }
        return result;
    }
}
