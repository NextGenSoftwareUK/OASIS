using System;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class SendWeb3TokenRequest : ISendWeb3TokenRequest
    {
        public Guid Web3TokenId { get; set; }
        public string FromTokenAddress { get; set; }
        public string FromWalletAddress { get; set; }
        public string ToWalletAddress { get; set; }
        //public string FromToken { get; set; }
        //public string ToToken { get; set; }

       // public EnumValue<ProviderType> FromProviderType { get; set; } //Optional (if FromWalletAddress is not provided then it can be retreived from the logged in avatar using the default wallet for the specified provider type).

        public decimal Amount { get; set; }
        public string MemoText { get; set; }

        public string OwnerPublicKey { get; init; } //TODO: Not sure if we need these?
        public string OwnerPrivateKey { get; init; } //TODO: Not sure if we need these?
        public string OwnerSeedPhrase { get; init; } //TODO: Not sure if we need these?
    }
}