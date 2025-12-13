using System;
using System.Threading.Tasks;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Requests;
using NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Responses;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.OASIS.API.Core.Managers.Bridge.DTOs;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IOASISNFTProvider : IOASISProvider
    {
        //TODO: More to come soon... ;-)
        public OASISResult<IWeb3NFTTransactionResponse> SendNFT(ISendWeb3NFTRequest transation);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> SendNFTAsync(ISendWeb3NFTRequest transation);
        public OASISResult<IWeb3NFTTransactionResponse> MintNFT(IMintWeb3NFTRequest transation);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> MintNFTAsync(IMintWeb3NFTRequest transation);
        public OASISResult<IWeb3NFTTransactionResponse> BurnNFT(IBurnWeb3NFTRequest request);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> BurnNFTAsync(IBurnWeb3NFTRequest request);
        
        // NFT-specific lock/unlock methods
        public OASISResult<IWeb3NFTTransactionResponse> LockNFT(ILockWeb3NFTRequest request);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> LockNFTAsync(ILockWeb3NFTRequest request);
        public OASISResult<IWeb3NFTTransactionResponse> UnlockNFT(IUnlockWeb3NFTRequest request);
        public Task<OASISResult<IWeb3NFTTransactionResponse>> UnlockNFTAsync(IUnlockWeb3NFTRequest request);
        
        // NFT Bridge Methods for cross-chain transfers
        
        /// <summary>
        /// Withdraws an NFT from the source chain (locks it for cross-chain transfer).
        /// </summary>
        /// <param name="nftTokenAddress">The address of the NFT token contract.</param>
        /// <param name="tokenId">The unique token ID of the NFT.</param>
        /// <param name="senderAccountAddress">The address of the account owning the NFT.</param>
        /// <param name="senderPrivateKey">The private key of the account.</param>
        /// <returns>A response containing the transaction details of the NFT withdrawal/lock.</returns>
        Task<OASISResult<BridgeTransactionResponse>> WithdrawNFTAsync(string nftTokenAddress, string tokenId, string senderAccountAddress, string senderPrivateKey);

        /// <summary>
        /// Deposits an NFT to the destination chain (mints wrapped NFT or unlocks original).
        /// </summary>
        /// <param name="nftTokenAddress">The address of the NFT token contract on destination chain.</param>
        /// <param name="tokenId">The unique token ID (may be different on destination if wrapped).</param>
        /// <param name="receiverAccountAddress">The address of the account to receive the NFT.</param>
        /// <param name="sourceTransactionHash">The transaction hash from the source chain withdrawal (for verification).</param>
        /// <returns>A response containing the transaction details of the NFT deposit/mint.</returns>
        Task<OASISResult<BridgeTransactionResponse>> DepositNFTAsync(string nftTokenAddress, string tokenId, string receiverAccountAddress, string sourceTransactionHash = null);
        public OASISResult<IWeb3NFT> LoadOnChainNFTData(string nftTokenAddress);
        public Task<OASISResult<IWeb3NFT>> LoadOnChainNFTDataAsync(string nftTokenAddress);
    }
}