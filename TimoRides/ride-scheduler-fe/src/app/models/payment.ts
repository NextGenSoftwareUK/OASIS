export interface FlutterwavePaymentResponse {
  status: string;
  customer: {
    name: string;
    email: string;
    phone_number: string;
  };
  transaction_id: number;
  tx_ref: string;
  flw_ref: string;
  currency: string;
  amount: number;
  charged_amount: number;
  charge_response_code: string;
  charge_response_message: string;
  created_at: string;
}

export interface WalletTopupResponse {
  trxId: string;
  trxRef: string;
  amount: number;
}
