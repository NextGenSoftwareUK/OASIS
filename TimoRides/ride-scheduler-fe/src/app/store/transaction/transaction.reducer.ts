import { createReducer, on } from '@ngrx/store';
import * as transactionActions from './transaction.actions';
import { TopupRequest } from './transaction.model';

export interface TransactionState {
  topup: TopupRequest[] | null;
  error: any;
}

export const initialState: TransactionState = {
  topup: null,
  error: null,
};

export const transactionReducer = createReducer(
  initialState,
  on(transactionActions.topupWalletSuccess, (state, { response }) => ({
    ...state,
    topup:
      state.topup && state.topup?.length > 0
        ? [...state.topup, response]
        : [response],
    error: null,
  })),
  on(transactionActions.topupWalletFailure, (state, { error }) => ({
    ...state,
    topup: null,
    error,
  }))
);
