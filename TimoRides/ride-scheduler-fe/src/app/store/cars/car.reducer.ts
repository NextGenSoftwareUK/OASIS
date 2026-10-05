import { createReducer, on } from '@ngrx/store';
import * as CarActions from './car.actions';
import { Car, ProxyCar } from 'src/app/models/car';
import { Profile } from 'src/app/models/user.model';
import * as AdminActions from 'src/app/components/dashboards/admin/store/admin.actions';

export interface CarState {
  cars: Car[];
  proxyCars: ProxyCar[];
  selectedCar: Car | null;
  createdCar: Car | null;
  selectedProxyCar: ProxyCar | null;
  selectedDriver: Profile | null;
  selectedDriverCar: Car | null;
  error: any | null;
}

export const initialState: CarState = {
  cars: [],
  proxyCars: [],
  createdCar: null,
  selectedCar: null,
  selectedProxyCar: null,
  selectedDriver: null,
  selectedDriverCar: null,
  error: null,
};

export const carReducer = createReducer(
  initialState,
  on(CarActions.loadCarsByProxySuccess, (state, { cars }) => ({
    ...state,
    proxyCars: cars.cars,
    error: null,
  })),
  on(CarActions.loadCarsByProxyFailure, (state, { error }) => ({
    ...state,
    error,
  })),
  on(
    CarActions.selectProxyDriverCar,
    CarActions.loadSelectedRebookedCarSuccess,
    (state, { car }) => ({
      ...state,
      selectedProxyCar: car,
    })
  ),
  on(CarActions.selectCar, (state, { car }) => ({
    ...state,
    selectedCar: car,
  })),
  on(CarActions.selectDriver, (state, { driver }) => ({
    ...state,
    selectedDriver: driver,
  })),
  on(CarActions.clearSelection, (state) => ({
    ...state,
    selectedCar: null,
    selectedDriver: null,
  })),
  on(CarActions.clearCarState, (state) => ({
    ...state,
    selectedCar: null,
    selectedDriver: null,
  })),
  on(CarActions.createCarSuccess, (state, { car }) => ({
    ...state,
    createdCar: car,
    error: null,
  })),
  on(CarActions.createCarFailure, (state, { error }) => ({
    ...state,
    error,
  })),
  on(CarActions.updateCarSuccess, (state, { car }) => ({
    ...state,
    cars: state.cars.map((c) => (c.id === car.id ? car : c)),
    error: null,
  })),
  on(CarActions.updateCarFailure, (state, { error }) => ({
    ...state,
    error,
  })),
  on(CarActions.loadDriverCarSuccess, (state, { car }) => ({
    ...state,
    selectedCar: car,
    selectedDriverCar: car,
    error: null,
  })),
  on(CarActions.loadDriverCarFailure, (state, { error }) => ({
    ...state,
    error: error.message,
  })),
  on(CarActions.loadSelectedDriverSuccess, (state, { profile }) => ({
    ...state,
    selectedDriver: profile,
    error: null,
  })),
  on(CarActions.loadSelectedDriverFailure, (state, { error }) => ({
    ...state,
    selectedDriver: null,
    error,
  })),
  on(CarActions.loadSelectedRebookedCarFailure, (state, { error }) => ({
    ...state,
    error,
  })),
  on(AdminActions.activateCarSuccess, (state, { car }) => ({
    ...state,
    cars: state.cars.map((c) => (c.id === car.id ? car : c)),
    error: null,
  })),
  on(AdminActions.activateCarFailure, (state, { error }) => ({
    ...state,
    error,
  })),
  on(CarActions.resetCar, () => initialState)
);
