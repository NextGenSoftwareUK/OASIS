using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public interface ISolanaService
{
    Task<OASISResult<SendTransactionResult>> SendTransaction(SendTransactionRequest sendTransactionRequest, Account signerAccount = null);
    Task<OASISResult<MintNftResult>> MintNftAsync(MintWeb3NFTRequest mintNftRequest);
    Task<OASISResult<BurnNftResult>> BurnNftAsync(IBurnWeb3NFTRequest burnNftRequest);
    Task<OASISResult<SendTransactionResult>> SendNftAsync(SendWeb3NFTRequest mintNftRequest);
    Task<OASISResult<GetNftResult>> LoadNftAsync(string address);
    Task<OASISResult<SolanaAvatarDto>> GetAvatarByUsernameAsync(string username);
    Task<OASISResult<SolanaAvatarDto>> GetAvatarByIdAsync(Guid id);
    Task<OASISResult<SolanaAvatarDto>> GetAvatarByEmailAsync(string email);
    Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByIdAsync(Guid id);
    Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByUsernameAsync(string username);
    Task<OASISResult<SolanaAvatarDetailDto>> GetAvatarDetailByEmailAsync(string email);
}