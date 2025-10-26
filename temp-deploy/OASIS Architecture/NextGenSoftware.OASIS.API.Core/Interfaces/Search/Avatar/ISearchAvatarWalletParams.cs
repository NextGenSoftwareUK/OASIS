
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Helpers;
using NextGenSoftware.OASIS.API.Core.Objects;
using System.Collections.Generic;
using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Search.Avatar
{
    public interface ISearchAvatarWalletParams
    {
        public bool WalletId { get; set; }
        public bool PublicKey { get; set; }
        public bool WalletAddress { get; set; }
        public bool ProviderType { get; set; }
        public bool Balance { get; set; }
        public bool IsDefaultWallet { get; set; }

        public ISearchAvatarWalletTransactionParams SearchAvatarWalletTransactionParams { get; set; }
    }
}