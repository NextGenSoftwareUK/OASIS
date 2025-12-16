using System;

using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class LockWeb3TokenRequest : ILockWeb3TokenRequest
    {
        public Guid LockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
        public string FromWalletPrivateKey { get; set; }
        public string FromWalletAddress { get; set; }
        public decimal Amount { get; set; }
    }
}