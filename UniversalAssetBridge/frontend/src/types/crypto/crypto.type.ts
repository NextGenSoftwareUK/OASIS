export type CryptoOption = {
  id: string;
  name: string;
  description: string;
  tokens: string[];
};

export type SelectedCrypto = {
  network: string;
  token: string;
};
