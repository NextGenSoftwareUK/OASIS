using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public interface IUnlockWeb3TokenRequest
    {
        Guid UnlockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
    }
}