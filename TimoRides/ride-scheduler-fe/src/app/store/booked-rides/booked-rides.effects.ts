import { Actions, createEffect, ofType } from '@ngrx/effects';
import { mergeMap, map, catchError, of, tap } from 'rxjs';
import { BookedRidesInfo, BookedRidesType } from 'src/app/models/booking-form';
import * as BookedRideActions from './booked-rides.actions';
import { Injectable } from '@angular/core';
import { BookingService } from 'src/app/components/services/booking/booking.service';
import { TripStatusResponse } from 'src/app/models/trip';
import { SnackbarUtilService } from 'src/app/components/shared/snackbar/snackbar-util.service';
import { Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';

@Injectable()
export class BookedRideEffects {
  constructor(
    private actions$: Actions,
    private bookingService: BookingService,
    private _snackBar: SnackbarUtilService,
    private router: Router
  ) {}

  loadBookings$ = createEffect(() =>
    this.actions$.pipe(
      ofType(BookedRideActions.loadBookings),
      mergeMap(() =>
        this.bookingService.getAllBookedRides().pipe(
          map((bookings: BookedRidesType) =>
            BookedRideActions.loadBookingsSuccess({ bookings })
          ),
          tap(() =>
            this._snackBar.displaySnackBar(
              'Bookings loaded successfully',
              '',
              'green-snackbar'
            )
          ),
          catchError((error: HttpErrorResponse) => {
            this._snackBar.displaySnackBar(
              'Unable to load bookings',
              '',
              'red-snackbar'
            );

            if (
              JSON.parse(error.message).error.error ===
              'Unauthorized - verify your email'
            ) {
              // redirect to email verification component
              this.router.navigate(['/verify'], {
                queryParams: { token: 'kol' },
              });
            }
            return of(BookedRideActions.loadBookingsFailure({ error }));
          })
        )
      )
    )
  );

  acceptRide$ = createEffect(() =>
    this.actions$.pipe(
      ofType(BookedRideActions.acceptRide),
      mergeMap(({ bookingId }) =>
        this.bookingService
          .acceptRideFromDashboard({ bookingId, isAccepted: true })
          .pipe(
            map((response: BookedRidesInfo) =>
              BookedRideActions.acceptRideSuccess({
                booking: { ...response },
              })
            ),
            tap(() =>
              this._snackBar.displaySnackBar(
                'Ride accepted successfully',
                '',
                'green-snackbar'
              )
            ),
            catchError((error) => {
              this._snackBar.displaySnackBar(
                'Unable to accept config',
                '',
                'red-snackbar'
              );
              return of(BookedRideActions.acceptRideFailure({ error }));
            })
          )
      )
    )
  );

  cancelRide$ = createEffect(() =>
    this.actions$.pipe(
      ofType(BookedRideActions.cancelRide),
      mergeMap(({ bookingId }) =>
        this.bookingService
          .acceptRideFromDashboard({ bookingId, isAccepted: false })
          .pipe(
            map((response: BookedRidesInfo) =>
              BookedRideActions.cancelRideSuccess({
                booking: { ...response },
              })
            ),
            tap(() =>
              this._snackBar.displaySnackBar(
                'Ride cancelled successfully',
                '',
                'green-snackbar'
              )
            ),
            catchError((error) => {
              this._snackBar.displaySnackBar(
                'Unable to cancel ride',
                '',
                'red-snackbar'
              );
              return of(BookedRideActions.cancelRideFailure({ error }));
            })
          )
      )
    )
  );

  startRide$ = createEffect(() =>
    this.actions$.pipe(
      ofType(BookedRideActions.startRide),
      mergeMap((action) =>
        this.bookingService
          .setTripStatus({
            bookingId: action.tripInfo.bookingId,
            otpCode: action.tripInfo.otpCode,
            tripMode: action.tripInfo.tripMode,
          })
          .pipe(
            map((response: TripStatusResponse) =>
              BookedRideActions.startRideSuccess({
                rideInfo: { ...response },
              })
            ),
            tap(() =>
              this._snackBar.displaySnackBar(
                'Ride started successfully',
                '',
                'green-snackbar'
              )
            ),
            catchError((error) => {
              this._snackBar.displaySnackBar(
                'Unable to start ride',
                '',
                'red-snackbar'
              );
              return of(BookedRideActions.startRideFailure({ error }));
            })
          )
      )
    )
  );

  endRide$ = createEffect(() =>
    this.actions$.pipe(
      ofType(BookedRideActions.endRide),
      mergeMap((action) =>
        this.bookingService
          .setTripStatus({
            bookingId: action.tripInfo.bookingId,
            otpCode: action.tripInfo.otpCode,
            tripMode: action.tripInfo.tripMode,
          })
          .pipe(
            map((response: TripStatusResponse) =>
              BookedRideActions.endRideSuccess({
                rideInfo: { ...response },
              })
            ),
            tap(() =>
              this._snackBar.displaySnackBar(
                'Ride ended successfully',
                '',
                'green-snackbar'
              )
            ),
            catchError((error) => {
              this._snackBar.displaySnackBar(
                'Unable to end ride',
                '',
                'red-snackbar'
              );
              return of(BookedRideActions.endRideFailure({ error }));
            })
          )
      )
    )
  );
}
