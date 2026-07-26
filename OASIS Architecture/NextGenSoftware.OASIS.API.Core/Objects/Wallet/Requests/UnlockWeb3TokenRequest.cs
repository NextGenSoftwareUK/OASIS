using System;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class UnlockWeb3TokenRequest : IUnlockWeb3TokenRequest
    {
        public Guid UnlockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
    }
}