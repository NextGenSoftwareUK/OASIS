export class TradeResponseDto {
  id: string;
  tradeId: string;
  buyer: {
    id: string;
    email: string;
    username?: string;
  };
  seller: {
    id: string;
    email: string;
    username?: string;
  };
  assetId: string;
  buyOrderId?: string;
  sellOrderId?: string;
  quantity: string; // BigInt as string
  pricePerTokenUsd: string; // Decimal as string
  totalValueUsd: string; // Decimal as string
  platformFeeUsd: string;
  platformFeePercentage: string;
  blockchain: string;
  transactionHash: string;
  blockNumber?: string;
  executedAt: Date;
  confirmedAt?: Date;
  status: string;
  settlementStatus: string;
}

export class TradeListResponseDto {
  items: TradeResponseDto[];
  total: number;
  page: number;
  limit: number;
  totalPages: number;
}

export class TradeStatisticsDto {
  totalTrades: number;
  totalVolume: string; // Decimal as string
  averageTradeSize: string; // Decimal as string
  minPrice: string; // Decimal as string
  maxPrice: string; // Decimal as string
  averagePrice: string; // Decimal as string
}












