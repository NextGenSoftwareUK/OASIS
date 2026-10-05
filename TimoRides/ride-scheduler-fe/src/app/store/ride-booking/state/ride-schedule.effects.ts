import { Injectable } from '@angular/core';
import { Actions, createEffect, ofType } from '@ngrx/effects';
import { catchError, map, mergeMap, switchMap } from 'rxjs/operators';
import { of } from 'rxjs';
import { RideScheduleService } from 'src/app/components/services/pricing/ride-schedule.service';
import * as RideScheduleAPIActions from './actions/ride-schedule-api.action';
import * as RideSchedulePageActions from './actions/ride-schedule-page.action';
import { Router } from '@angular/router';
import { BookingService } from 'src/app/components/services/booking/booking.service';

@Injectable()
export class RideScheduleEffects {
  constructor(
    private actions$: Actions,
    private rideScheduleService: RideScheduleService,
    private bookingService: BookingService,
    private router: Router
  ) {}

  fetchTripCost$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RideScheduleAPIActions.fetchTripCost),
      mergeMap((action) =>
        this.rideScheduleService.getTripCost(action.selectedLocations).pipe(
          map((res) =>
            RideScheduleAPIActions.fetchTripCostSuccess({
              tripInfo: res,
            })
          ),
          catchError((error) =>
            of(RideScheduleAPIActions.fetchTripCostFailure({ error }))
          )
        )
      )
    )
  );

  fetchTripCostSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(RideScheduleAPIActions.fetchTripCostSuccess),
        map(() => {
          this.router.navigateByUrl('/ride-schedule/cars');
        })
      ),
    { dispatch: false }
  );

  saveScheduledRide$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RideSchedulePageActions.saveScheduledRide),
      mergeMap((action) =>
        this.bookingService.bookRide(action.scheduledRide).pipe(
          map((res) =>
            RideSchedulePageActions.saveScheduledRideSuccess({
              response: res.id,
            })
          ),
          catchError((error) =>
            of(
              RideSchedulePageActions.saveScheduledRideFailure({
                error,
              })
            )
          )
        )
      )
    )
  );

  submitRebooking$ = createEffect(() =>
    this.actions$.pipe(
      ofType(RideSchedulePageActions.submitRebooking),
      mergeMap((action) =>
        this.bookingService.updateBookedRide(action.rebookingInfo).pipe(
          map((res) =>
            RideSchedulePageActions.saveScheduledRideSuccess({
              response: res.id,
            })
          ),
          catchError((error) =>
            of(
              RideSchedulePageActions.saveScheduledRideFailure({
                error,
              })
            )
          )
        )
      ),
      switchMap((action) => {
        if (
          action.type === RideSchedulePageActions.saveScheduledRideSuccess.name
        ) {
          this.router.navigate(['/ride-schedule/complete']);
        }
        return of(action);
      })
    )
  );

  saveScheduledRideSuccess$ = createEffect(
    () =>
      this.actions$.pipe(
        ofType(RideSchedulePageActions.saveScheduledRideSuccess),
        map((res) => {
          this.router.navigateByUrl(
            '/ride-schedule/complete?id=' + res.response
          );
        })
      ),
    { dispatch: false }
  );
}
