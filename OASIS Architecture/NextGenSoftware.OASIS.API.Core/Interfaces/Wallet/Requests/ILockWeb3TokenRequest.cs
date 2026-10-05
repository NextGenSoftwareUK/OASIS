using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface ILockWeb3TokenRequest
    {
        Guid LockedByAvatarId { get; set; }
        public Guid Web3TokenId { get; set; }
        //public string MintWalletAddress { get; set; }
        public string TokenAddress { get; set; }
    }
}