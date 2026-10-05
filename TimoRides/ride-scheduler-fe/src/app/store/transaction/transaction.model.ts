export interface TopupRequest {
  trxId: string;
  trxRef: string;
  amount: number;
}

export interface TopupResponse {
  id: string;
  wallet: number;
  message: string;
}
