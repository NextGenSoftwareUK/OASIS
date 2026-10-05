import { createFeatureSelector, createSelector } from '@ngrx/store';
import * as fromBank from './bank.reducer';

export const selectBankState =
  createFeatureSelector<fromBank.BankState>('bank');

export const selectBanks = createSelector(
  selectBankState,
  (state: fromBank.BankState) => state.banks
);
