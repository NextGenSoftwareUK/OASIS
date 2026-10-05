import { HttpErrorResponse } from '@angular/common/http';
import { createAction, props } from '@ngrx/store';

// withdraw from wallet
export const withdrawFromWallet = createAction(
  '[Wallet] Withdraw From Wallet',
  props<{ amount: number }>()
);

export const withdrawFromWalletSuccess = createAction(
  '[Wallet] Withdraw From Wallet Success',
  props<{ response: any }>()
);

export const withdrawFromWalletFailure = createAction(
  '[Wallet] Withdraw From Wallet Failure',
  props<{ error: HttpErrorResponse }>()
);
