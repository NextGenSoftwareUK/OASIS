export type ChainId = "solana";

export type ProviderMapping = {
  onChain: { value: number; name: string };
  offChain: { value: number; name: string };
  nftOffChainMetaType: { value: number; name: string };
  nftStandardType: { value: number; name: string };
};

export type ChainOption = {
  id: ChainId;
  name: string;
  description: string;
  logo?: string;
  protocol: string;
  wallet: string;
  providerMapping: ProviderMapping;
  configurationVariants?: string[];
};

export const SOLANA_CHAIN: ChainOption = {
  id: "solana",
  name: "Solana",
  description: "SPL NFTs via SolanaOASIS + MongoDBOASIS",
  logo: "/logos/solana.png",
  protocol: "SPL",
  wallet: "Phantom",
  providerMapping: {
    onChain: { value: 3, name: "SolanaOASIS" },
    offChain: { value: 23, name: "MongoDBOASIS" },
    nftOffChainMetaType: { value: 3, name: "ExternalJsonURL" },
    nftStandardType: { value: 2, name: "SPL" },
  },
  configurationVariants: [
    "Metaplex Standard",
    "Collection with Verified Creator",
    "Editioned Series",
    "Compressed NFT (Bubblegum)"
  ],
};
