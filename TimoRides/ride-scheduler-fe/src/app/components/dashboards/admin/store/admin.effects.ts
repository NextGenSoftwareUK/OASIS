import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { combineLatest, of, tap } from 'rxjs';
import { catchError, map, mergeMap } from 'rxjs/operators';
import { AdminService } from '../services/admin.service';
import * as AdminActions from './admin.actions';
import { CarService } from 'src/app/components/services/car.service';
import { UserWithCars } from './admin.model';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';

@Injectable()
export class AdminEffects {
  constructor(
    private actions$: Actions,
    private adminService: AdminService,
    private carService: CarService,
    private _snackBar: SnackbarUtilService
  ) {}

  loadUsers$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.loadUsers),
      mergeMap(() =>
        this.adminService.getUsers().pipe(
          mergeMap((users) =>
            combineLatest(
              users.map((user) =>
                this.carService.getCarByUserId(user.id).pipe(
                  map((cars) => ({
                    ...user,
                    cars,
                  })),
                  catchError(() =>
                    of({
                      ...user,
                      cars: null,
                    })
                  )
                )
              )
            )
          ),
          map((usersWithCars) =>
            AdminActions.loadUsersSuccess({
              users: usersWithCars as UserWithCars[],
            })
          ),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Users loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to load users',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin API] Users Loaded Error' });
          })
        )
      )
    )
  );

  loadUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.loadUser),
      mergeMap((action) =>
        this.adminService.getUser(action.id).pipe(
          mergeMap((user) =>
            this.carService.getCarByUserId(action.id).pipe(
              map((cars) => ({
                ...user,
                cars,
              })),
              catchError(() =>
                of({
                  ...user,
                  cars: null,
                })
              )
            )
          ),
          map((userWithCar) =>
            AdminActions.loadUserSuccess({ user: userWithCar as UserWithCars })
          ),
          tap(() =>
            this._snackBar.displaySnackBar(
              'User loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'An error occurred',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin API] User Loaded Error' });
          })
        )
      )
    )
  );

  loadTrips$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.loadTrips),
      mergeMap(() =>
        this.adminService.getTrips().pipe(
          map((trips) => AdminActions.loadTripsSuccess({ trips })),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Trips loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to load trips',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin API] Trips Loaded Error' });
          })
        )
      )
    )
  );

  updateTripAmount$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.updateTripAmount),
      mergeMap((action) =>
        this.adminService.updateTripAmount(action.tripId, action.amount).pipe(
          map((trip) => AdminActions.updateTripAmountSuccess({ trip })),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Trip amount updated successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to update trip amount',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin API] Trip Amount Update Error' });
          })
        )
      )
    )
  );

  loadCarByDriverId$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.loadCarByDriverId),
      mergeMap((action) =>
        this.adminService.getCarByDriverId(action.id).pipe(
          map((car) => AdminActions.loadCarByDriverIdSuccess({ car })),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Car loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to load car by driver Id',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin] Load Car By Driver Id Failed' });
          })
        )
      )
    )
  );

  loadConfig$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.loadConfig),
      mergeMap(() =>
        this.adminService.getAdminConfig().pipe(
          map((config) => AdminActions.loadConfigSuccess({ config })),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Config loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to load config',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin] Load Config Failed' });
          })
        )
      )
    )
  );

  updateConfig$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.updateConfig),
      mergeMap((action) =>
        this.adminService.updateAdminConfig(action.config).pipe(
          map((config) => AdminActions.loadConfigSuccess({ config })),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Config updated successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to update config',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin] Update Config Failed' });
          })
        )
      )
    )
  );

  updateBusinessCommission$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.updateBusinessCommission),
      mergeMap((action) =>
        this.adminService.updateBusinessCommission(action.config).pipe(
          map((config) =>
            AdminActions.updateBusinessCommissionSuccess({ config })
          ),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Commission updated successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to update commission',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin] Update Business Commission Failed' });
          })
        )
      )
    )
  );

  loadWithdrawals$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.loadWithdrawals),
      mergeMap(() =>
        this.adminService.getPendingWithdrawals().pipe(
          map((withdrawals) =>
            AdminActions.loadWithdrawalsSuccess({ withdrawals })
          ),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Withdrawals loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to load withdrawals',
              '',
              'red-snackbar'
            );
            return of({ type: '[Admin] Load Withdrawals Failed' });
          })
        )
      )
    )
  );

  payUser$ = createEffect(() =>
    this.actions$.pipe(
      ofType(AdminActions.payUser),
      mergeMap((action) =>
        this.adminService.payUser(action.requestId).pipe(
          map((withdrawal) => AdminActions.payUserSuccess({ withdrawal })),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Payment completed successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError(() => {
            this._snackBar.displaySnackBar(
              'Unable to complete payment',
              '',
              'red-snackbar'
            );
            return of({ type: '[Payment] Pay User Failure' });
          })
        )
      )
    )
  );
}
