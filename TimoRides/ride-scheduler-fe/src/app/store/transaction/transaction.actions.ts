import { HttpErrorResponse } from '@angular/common/http';
import { createAction, props } from '@ngrx/store';
import { TopupRequest } from './transaction.model';

// withdraw from wallet
export const topupWallet = createAction(
  '[Wallet] Topup Wallet',
  props<{ data: TopupRequest }>()
);

export const topupWalletSuccess = createAction(
  '[Wallet] Topup Wallet Success',
  props<{ response: TopupRequest }>()
);

export const topupWalletFailure = createAction(
  '[Wallet] Topup Wallet Failure',
  props<{ error: HttpErrorResponse }>()
);
