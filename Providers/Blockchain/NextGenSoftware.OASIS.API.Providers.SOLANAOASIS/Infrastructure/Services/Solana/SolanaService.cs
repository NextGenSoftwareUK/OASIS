using NextGenSoftware.OASIS.API.Core.Interfaces.Avatar;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Objects.NFT.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Entities.DTOs.Requests;
using NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Requests;
using Solnet.Metaplex.Utilities;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

public sealed partial class SolanaService(Account oasisAccount, IRpcClient rpcClient) : ISolanaService
{
    private const uint SellerFeeBasisPoints = 500;
    private const byte CreatorShare = 100;
    private const string Solana = "Solana";
    private const decimal Lamports = 1_000_000_000m;

    private readonly List<Creator> _creators =
    [
        new(oasisAccount.PublicKey, share: CreatorShare, verified: true)
    ];

}
