import { SelectedCrypto } from "@/types/crypto/crypto.type";

export const defaultSelectedFrom: SelectedCrypto = {
  network: "Solana",
  token: "SOL",
};

export const defaultSelectedTo: SelectedCrypto = {
  network: "Radix",
  token: "XRD",
};

export const defaultSelectedNetwork = {
  name: "Solana",
  description: "Solana network",
};

export const networkIcons: Record<string, string> = {
  Solana: "/SOL.svg",
  Radix: "/XRD.svg",
};
