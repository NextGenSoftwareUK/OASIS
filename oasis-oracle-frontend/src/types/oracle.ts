import { ChainType } from "./chains";

export type OracleStatus = {
  totalDataSources: number;
  activeDataSources: number;
  totalChains: number;
  healthyChains: number;
  totalVerifications: number;
  consensusLevel: number;
  uptime: number;
  lastUpdate: Date;
};

export type ArbitrageOpportunity = {
  token: string;
  buyChain: ChainType;
  sellChain: ChainType;
  buyPrice: number;
  sellPrice: number;
  profitPercentage: number;
  estimatedProfit: number;
  recommendedAmount: number;
  riskScore: "low" | "medium" | "high";
  liquidityCheck: boolean;
  timeWindow: number;
};

export type DAOVote = {
  proposalId: string;
  chain: ChainType;
  voterAddress: string;
  voteChoice: "yes" | "no" | "abstain";
  tokenWeight: number;
  transactionHash: string;
  timestamp: Date;
};

export type DAOVoteAggregation = {
  proposalId: string;
  title: string;
  totalVotes: number;
  yesVotes: number;
  noVotes: number;
  abstainVotes: number;
  quorumReached: boolean;
  result: "passed" | "failed" | "pending";
  endTime: Date;
  votesByChain: {
    chain: ChainType;
    votes: number;
    yesPercentage: number;
  }[];
};

export type YieldPosition = {
  chain: ChainType;
  protocol: string;
  poolAddress: string;
  tokenAddress: string;
  positionSize: number;
  apy: number;
  apr: number;
  dailyYield: number;
  totalYield: number;
  riskLevel: "low" | "medium" | "high";
  isVerified: boolean;
};





