
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IMintWeb3TokenRequest : IMintTokenRequestBase
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
    }
}