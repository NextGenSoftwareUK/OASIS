
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests
{
    public interface IBurnWeb4TokenRequest : IBurnWeb3TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTokenBurnt { get; set; }
        public int WaitForTokenToBurnInSeconds { get; set; }
        public int AttemptToBurnEveryXSeconds { get; set; }
    }
}