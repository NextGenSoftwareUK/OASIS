using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallets.Requests
{
    public interface IWalletTransactionRequest
    {
        //Need at least one of these to identify the sender.
        public string FromWalletAddress { get; set; }
        public Guid FromAvatarId { get; set; }
        public string FromAvatarUsername { get; set; }
        public string FromAvatarEmail { get; set; }

        //Need at least one of these to identify the receiver.
        public string ToWalletAddress { get; set; }
        public Guid ToAvatarId { get; set; }
        public string ToAvatarUsername { get; set; }
        public string ToAvatarEmail { get; set; }
        //public string FromToken { get; set; }
        //public string ToToken { get; set; }
        public EnumValue<ProviderType> FromProvider { get; set; }
        public EnumValue<ProviderType> ToProvider { get; set; }
        public decimal Amount { get; set; }
        public string MemoText { get; set; }
    }
}