import { createAction, props } from '@ngrx/store';
import { RidePriceResponse } from 'src/app/models/pricing';

export const fetchTripCost = createAction(
  '[Ride Schedule] Fetch Trip Cost',
  props<{ selectedLocations: any }>()
);

export const fetchTripCostSuccess = createAction(
  '[Ride Schedule] Fetch Trip Cost Success',
  props<{ tripInfo: RidePriceResponse }>()
);

export const fetchTripCostFailure = createAction(
  '[Ride Schedule] Fetch Trip Cost Failure',
  props<{ error: any }>()
);
