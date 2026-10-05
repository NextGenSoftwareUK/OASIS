export interface CopyIpfsButtonProps {
  cid: string;
}

export interface RwaPurchaseHistoryProps {
  type: "Buy" | "Sell";
  price: number;
  buyerPublicKey: string;
  transactionDate: string;
}
