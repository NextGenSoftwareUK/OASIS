using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT.Request
{
    public class UnlockWeb3TokenRequest : IUnlockWeb3TokenRequest
    {
        public Guid UnlockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
    }
}