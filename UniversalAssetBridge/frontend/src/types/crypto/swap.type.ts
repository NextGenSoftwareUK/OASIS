export type SwapFormData = {
  userId: string;
  fromToken: string;
  toToken: string;
  amount: number | null;
  fromNetwork: string;
  toNetwork: string;
  destinationAddress: string;
};

export type SwapResponse = {
  data: {
    orderId: string;
  };
};

export type SwapTarget = "from" | "to";
