import { createReducer, on } from '@ngrx/store';
import * as BankActions from './bank.actions';
import { BankInfo } from './bank.model';

export interface BankState {
  banks: BankInfo[];
  error: any;
}

export const initialState: BankState = {
  banks: [],
  error: null,
};

export const bankReducer = createReducer(
  initialState,
  on(BankActions.loadBanksSuccess, (state, { banks }) => ({
    ...state,
    banks: banks,
    error: null,
  })),
  on(BankActions.loadBanksFailure, (state, { error }) => ({
    ...state,
    banks: [],
    error: error,
  }))
);
