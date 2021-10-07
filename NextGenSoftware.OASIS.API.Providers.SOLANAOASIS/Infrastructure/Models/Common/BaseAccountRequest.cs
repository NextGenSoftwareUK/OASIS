using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Models.Common
{
    public class BaseAccountRequest
    {
        public string PublicKey { get; set; }
        public string PrivateKey { get; set; }

        public Account GetAccountWallet()
        {
            if (string.IsNullOrEmpty(PublicKey) || string.IsNullOrEmpty(PrivateKey))
                return new Account();
            return new Account(PrivateKey, PublicKey);
        }
    }
}