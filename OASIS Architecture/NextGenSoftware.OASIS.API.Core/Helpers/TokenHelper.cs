using NextGenSoftware.OASIS.API.Core.Enums;

namespace NextGenSoftware.OASIS.API.Core.Helpers
{
    public static class TokenHelper
    {
        public static string GetTokenForProvider(ProviderType provderType)
        {
            return provderType switch
            {
                ProviderType.SolanaOASIS          => "SOL",
                ProviderType.ArbitrumOASIS         => "ARB",
                ProviderType.AvalancheOASIS        => "AVAX",
                ProviderType.BaseOASIS             => "ETH",
                ProviderType.EthereumOASIS         => "ETH",
                ProviderType.PolygonOASIS          => "MATIC",
                ProviderType.EOSIOOASIS            => "EOS",
                ProviderType.TelosOASIS            => "TLOS",
                ProviderType.SEEDSOASIS            => "SEEDS",
                ProviderType.LoomOASIS             => "LOOM",
                ProviderType.TONOASIS              => "TON",
                ProviderType.StellarOASIS          => "XLM",
                ProviderType.BlockStackOASIS       => "STX",
                ProviderType.HashgraphOASIS        => "HBAR",
                ProviderType.ElrondOASIS           => "EGLD",
                ProviderType.TRONOASIS             => "TRX",
                ProviderType.CosmosBlockChainOASIS => "ATOM",
                ProviderType.RootstockOASIS        => "RBTC",
                ProviderType.ChainLinkOASIS        => "LINK",
                ProviderType.CardanoOASIS          => "ADA",
                ProviderType.PolkadotOASIS         => "DOT",
                ProviderType.BitcoinOASIS          => "BTC",
                ProviderType.NEAROASIS             => "NEAR",
                ProviderType.SuiOASIS              => "SUI",
                ProviderType.AptosOASIS            => "APT",
                ProviderType.OptimismOASIS         => "OP",
                ProviderType.BNBChainOASIS         => "BNB",
                ProviderType.FantomOASIS           => "FTM",
                ProviderType.StarknetOASIS         => "STRK",
                ProviderType.AztecOASIS            => "ETH",
                ProviderType.MidenOASIS            => "MIDEN",
                ProviderType.ZcashOASIS            => "ZEC",
                ProviderType.RadixOASIS            => "XRD",
                ProviderType.TelegramOASIS         => "TON",
                ProviderType.XRPLOASIS             => "XRP",
                ProviderType.MonadOASIS            => "MON",
                ProviderType.LineaOASIS            => "ETH",
                ProviderType.ScrollOASIS           => "ETH",
                ProviderType.ZkSyncOASIS           => "ETH",
                _                                  => ""
            };
        }
    }
}
