
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces.Wallet.Requests;
using NextGenSoftware.Utilities;

namespace NextGenSoftware.OASIS.API.Core.Objects.Wallet.Requests
{
    public class BurnWeb4TokenRequest : BurnWeb3TokenRequest, IBurnWeb4TokenRequest
    {
        public EnumValue<ProviderType> ProviderType { get; set; }
        public bool WaitTillTokenBurnt { get; set; } = true;
        public int WaitForTokenToBurnInSeconds { get; set; } = 60;
        public int AttemptToBurnEveryXSeconds { get; set; } = 5;
    }
}