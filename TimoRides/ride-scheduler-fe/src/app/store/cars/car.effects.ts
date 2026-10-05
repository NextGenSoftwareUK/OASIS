import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, mergeMap, switchMap, tap } from 'rxjs/operators';
import { of } from 'rxjs';
import * as CarActions from './car.actions';
import { CarService } from 'src/app/components/services/car.service';
import { Car } from 'src/app/models/car';
import { HttpErrorResponse } from '@angular/common/http';
import { Profile } from 'src/app/models/user.model';
import * as AdminActions from 'src/app/components/dashboards/admin/store/admin.actions';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';

@Injectable()
export class CarEffects {
  constructor(
    private actions$: Actions,
    private carService: CarService,
    private _snackBar: SnackbarUtilService
  ) {}

  loadCarsByProxy$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CarActions.loadCarsByProxy),
      mergeMap((action) =>
        this.carService.getCarsByProximity(action.proxyInfo).pipe(
          map((cars) => CarActions.loadCarsByProxySuccess({ cars: cars })),
          catchError((error) => {
            this._snackBar.displaySnackBar(
              'Failed to load cars by proximity' + error.message,
              '',
              'red-snackbar'
            );
            return of(CarActions.loadCarsByProxyFailure({ error }));
          })
        )
      )
    )
  );

  loadDriver$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CarActions.loadSelectedDriver),
      mergeMap((action) =>
        this.carService.getDriverById(action.id).pipe(
          map((profile) =>
            CarActions.loadSelectedDriverSuccess({
              profile: profile as Profile,
            })
          ),
          tap(() => {
            this._snackBar.displaySnackBar(
              'Driver loaded successfully',
              '',
              'green-snackbar'
            );
          }),
          catchError((error) => {
            this._snackBar.displaySnackBar(
              'Failed to load driver' + error.message,
              '',
              'red-snackbar'
            );
            return of(CarActions.loadSelectedDriverFailure({ error }));
          })
        )
      )
    )
  );

  createCar$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CarActions.createCar),
      mergeMap((action) =>
        this.carService.createCar(action.car).pipe(
          map((newCar) => {
            this._snackBar.displaySnackBar(
              'Car created successfully',
              '',
              'green-snackbar'
            );
            return CarActions.createCarSuccess({ car: newCar });
          }),
          catchError((error) => {
            this._snackBar.displaySnackBar(
              'Failed to create car' + error.message,
              '',
              'red-snackbar'
            );
            return of(CarActions.createCarFailure({ error }));
          })
        )
      )
    )
  );

  loadSelectedRebookedCar$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CarActions.loadSelectedRebookedCar),
      mergeMap((action) =>
        this.carService
          .getSelectedRebookedCar(action.carId, action.bookingId)
          .pipe(
            map((car) =>
              CarActions.loadSelectedRebookedCarSuccess({
                car: car,
              })
            ),
            tap(() => {
              this._snackBar.displaySnackBar(
                'Car details loaded successfully',
                '',
                'green-snackbar'
              );
            }),
            catchError((error) => {
              this._snackBar.displaySnackBar(
                'Failed to load car details: ' + error.message,
                '',
                'red-snackbar'
              );
              return of(CarActions.loadSelectedRebookedCarFailure({ error }));
            })
          )
      )
    )
  );

  loadDriverCar$ = createEffect(() =>
    this.actions$.pipe(
      ofType(CarActions.loadDriverCar),
      mergeMap((action) =>
        this.carService.getCarByUserId(action.driverId).pipe(
          map((cars: Car[]) => {
            const driverCar = cars.length > 0 ? cars[0] : null;

            if (driverCar) {
              return CarActions.loadDriverCarSuccess({ car: driverCar });
            } else {
              this._snackBar.displaySnackBar(
                'No car found for driver',
                '',
                'red-snackbar'
              );
              return CarActions.loadDriverCarFailure({
                error: new HttpErrorResponse({ error: 'No car found' }),
              });
            }
          }),
          catchError((error: HttpErrorResponse) => {
            const errorObject = JSON.parse(error.message);

            if ('error' in errorObject) {
              if ('message' in errorObject.error) {
                if (
                  typeof errorObject.error.message === 'string' &&
                  (errorObject.error.message as string).toLowerCase() ===
                    'not found'
                ) {
                }
              }
            } else {
              this._snackBar.displaySnackBar(
                'Failed to load driver car',
                '',
                'red-snackbar'
              );
            }
            return of(CarActions.loadDriverCarFailure({ error }));
          })
        )
      )
    )
  );

  verifyCar$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.verifyCar),
      mergeMap((action) =>
        this.carService.getCarByUserId(action.driverId).pipe(
          switchMap((car) => {
            const updatePayload = {
              carId: car[0].id,
              isVerify: action.isVerify,
              isActive: action.isActive,
            };

            return this.carService.verifyCar(updatePayload).pipe(
              map((verifiedCar) => {
                this._snackBar.displaySnackBar(
                  'Car verified successfully',
                  '',
                  'green-snackbar'
                );
                return AdminActions.verifyCarSuccess({ car: verifiedCar });
              }),
              catchError((error) => {
                this._snackBar.displaySnackBar(
                  'Failed to verify car' + error.message,
                  '',
                  'red-snackbar'
                );
                return of(AdminActions.verifyCarFailure({ error }));
              })
            );
          }),
          catchError((error) => {
            this._snackBar.displaySnackBar(
              'Failed to verify car' + error.message,
              '',
              'red-snackbar'
            );
            return of(AdminActions.verifyCarFailure({ error }));
          })
        )
      )
    )
  );
}
