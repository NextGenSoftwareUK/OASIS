using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class TokenHelper
    {
        public static string GetTokenForProvider(ProviderType provderType)
        {
            //TODO: Finish implementing ASAP!
            switch (provderType)
            {
                case ProviderType.EthereumOASIS:
                    return "ETH";

                case ProviderType.SolanaOASIS:
                    return "SOL";

                case ProviderType.PolygonOASIS:
                    return "POLY";

                case ProviderType.EOSIOOASIS:
                    return "EOS";

                case ProviderType.CosmosBlockChainOASIS:
                    return "COSMOS";
            }

            return "";
        }
    }
}