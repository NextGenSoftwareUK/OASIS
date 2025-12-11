import { SelectedCrypto } from "@/types/crypto/crypto.type";

export const defaultSelectedFrom: SelectedCrypto = {
  network: "Solana",
  token: "SOL",
};

export const defaultSelectedTo: SelectedCrypto = {
  network: "Ethereum",
  token: "ETH",
};

export const defaultSelectedNetwork = {
  name: "Solana",
  description: "Solana network",
};

export const networkIcons: Record<string, string> = {
  Solana: "/SOL.svg",
  Ethereum: "/ETH.svg",
  Polygon: "/MATIC.svg",
  Base: "/BASE.svg",
  Arbitrum: "/ARB.png",
  Optimism: "/OP.svg",
  "BNB Chain": "/BNB.svg",
  Avalanche: "/AVAX.svg",
  Fantom: "/FTM.svg",
  Radix: "/XRD.svg",
};

export const cryptoOptions = [
  {
    token: "SOL",
    name: "Solana",
    network: "Solana",
    icon: "/SOL.svg",
    description: "Fast and low-cost blockchain"
  },
  {
    token: "ETH",
    name: "Ethereum",
    network: "Ethereum",
    icon: "/ETH.svg",
    description: "Largest smart contract platform"
  },
  {
    token: "MATIC",
    name: "Polygon",
    network: "Polygon",
    icon: "/MATIC.svg",
    description: "Ethereum scaling solution"
  },
  {
    token: "BASE",
    name: "Base",
    network: "Base",
    icon: "/BASE.svg",
    description: "Coinbase Layer 2"
  },
  {
    token: "ARB",
    name: "Arbitrum",
    network: "Arbitrum",
    icon: "/ARB.png",
    description: "Ethereum Layer 2"
  },
  {
    token: "OP",
    name: "Optimism",
    network: "Optimism",
    icon: "/OP.svg",
    description: "Ethereum Layer 2"
  },
  {
    token: "BNB",
    name: "BNB Chain",
    network: "BNB Chain",
    icon: "/BNB.svg",
    description: "Binance Smart Chain"
  },
  {
    token: "AVAX",
    name: "Avalanche",
    network: "Avalanche",
    icon: "/AVAX.svg",
    description: "High-performance blockchain"
  },
  {
    token: "FTM",
    name: "Fantom",
    network: "Fantom",
    icon: "/FTM.svg",
    description: "Fast and scalable"
  },
  {
    token: "XRD",
    name: "Radix",
    network: "Radix",
    icon: "/XRD.svg",
    description: "DeFi-focused blockchain"
  },
];
