import { createAction, props } from '@ngrx/store';
import { BankInfo } from './bank.model';

export const loadBanks = createAction(
  '[Bank] Load Banks',
  props<{ countryCode: string }>()
);

export const loadBanksSuccess = createAction(
  '[Bank] Load Banks Success',
  props<{ banks: BankInfo[] }>()
);

export const loadBanksFailure = createAction(
  '[Bank] Load Banks Failure',
  props<{ error: any }>()
);
