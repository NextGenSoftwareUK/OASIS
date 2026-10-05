import { createReducer, on } from '@ngrx/store';
import * as WithdrawalActions from './withdrawal.actions';

export interface WithdrawalState {
  isWithdrawalInitiated: boolean | null;
  error: any;
}

export const initialState: WithdrawalState = {
  isWithdrawalInitiated: null,
  error: null,
};

export const withdrawalReducer = createReducer(
  initialState,
  on(WithdrawalActions.withdrawFromWalletSuccess, (state, { response }) => ({
    ...state,
    isWithdrawalInitiated:
      'message' in response &&
      response['message'] === 'Payment request is being peocessed',
    error: null,
  })),
  on(WithdrawalActions.withdrawFromWalletFailure, (state, { error }) => ({
    ...state,
    isWithdrawalInitiated: false,
    error,
  }))
);
