
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class MintWeb3TokenRequest : MintTokenRequestBase, IMintWeb3TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
    }
}