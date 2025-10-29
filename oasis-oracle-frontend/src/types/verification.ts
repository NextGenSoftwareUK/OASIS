import { ChainType } from "./chains";

export type VerificationStatus = 
  | "pending"
  | "verified"
  | "failed"
  | "rejected";

export type TransactionVerification = {
  transactionHash: string;
  chain: ChainType;
  status: VerificationStatus;
  confirmations: number;
  requiredConfirmations: number;
  isFinalized: boolean;
  hasValidSignature: boolean;
  noDoubleSpend: boolean;
  blockNumber: number;
  timestamp: Date;
  from: string;
  to: string;
  amount: number;
  token: string;
};

export type NFTTransferVerification = {
  nftId: string;
  sourceChain: ChainType;
  destinationChain: ChainType;
  sourceTxHash: string;
  destinationTxHash: string;
  isLocked: boolean;
  isMinted: boolean;
  metadataMatch: boolean;
  provenanceChain: ChainType[];
  verificationTimestamp: Date;
  status: VerificationStatus;
};

export type VerificationRequest = {
  chain: ChainType;
  transactionHash: string;
  requiredConfirmations: number;
};

