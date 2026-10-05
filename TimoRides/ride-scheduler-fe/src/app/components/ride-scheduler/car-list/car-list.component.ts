import { Component, OnDestroy, OnInit } from '@angular/core';
import { CarProximityRequest, ProxyCar } from 'src/app/models/car';
import { Observable, Subscription, filter, map, switchMap } from 'rxjs';
import { ActivatedRoute, Router } from '@angular/router';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import * as CarActions from '../../../store/cars/car.actions';
import {
  selectRideSchedule,
  selectRideState,
} from 'src/app/store/ride-booking/state/ride-schedule.selectors';
import { BookingRequest } from 'src/app/models/booking-form';
import * as RideScheduleActions from '../../../store/ride-booking/state/actions/ride-schedule-page.action';
import { RouteParams } from 'src/app/models/rebooking';

@Component({
  selector: 'app-car-list',
  templateUrl: './car-list.component.html',
  styleUrls: ['./car-list.component.css'],
})
export class CarListComponent implements OnInit, OnDestroy {
  subscription: Subscription = new Subscription();
  cars$: Observable<ProxyCar[]>;
  rideSchedule$: Observable<BookingRequest | null>;
  rideState$: Observable<string | null>;
  pageSize: string = '10';
  currentPage: string = '1';
  params: RouteParams | undefined;

  constructor(
    private store: Store<AppState>,
    private router: Router,
    private activatedRoute: ActivatedRoute
  ) {
    this.cars$ = this.store.select((state) => state.car.proxyCars);
    this.rideSchedule$ = this.store.select(selectRideSchedule);
    this.rideState$ = this.store.select(selectRideState);

    // set params if available
    if (
      'sourceLat' in this.activatedRoute.snapshot.queryParams &&
      'sourceLong' in this.activatedRoute.snapshot.queryParams &&
      'destLat' in this.activatedRoute.snapshot.queryParams &&
      'destLong' in this.activatedRoute.snapshot.queryParams &&
      'state' in this.activatedRoute.snapshot.queryParams &&
      'scheduledDate' in this.activatedRoute.snapshot.queryParams &&
      'bookingId' in this.activatedRoute.snapshot.queryParams
    ) {
      this.params = {
        sourceLat: this.activatedRoute.snapshot.queryParams['sourceLat'],
        sourceLong: this.activatedRoute.snapshot.queryParams['sourceLong'],
        destLat: this.activatedRoute.snapshot.queryParams['destLat'],
        destLong: this.activatedRoute.snapshot.queryParams['destLong'],
        state: this.activatedRoute.snapshot.queryParams['state'],
        scheduledDate:
          this.activatedRoute.snapshot.queryParams['scheduledDate'],
        bookingId: this.activatedRoute.snapshot.queryParams['bookingId'],
      };

      this.store.dispatch(
        RideScheduleActions.saveRebooking({
          rebooking: {
            ...this.params,
          },
        })
      );
    }
  }

  ngOnInit(): void {
    if (this.params) {
      this.getProxyCarsByParam();
    } else {
      this.getProxyCars();
    }
  }

  getProxyCarsByParam() {
    if (this.params) {
      const proxyPayload: CarProximityRequest = {
        page: this.currentPage,
        pageSize: this.pageSize,
        state: this.params.state,
        scheduledDate: this.params.scheduledDate,
        sourceLatitude: this.params.sourceLat,
        sourceLongitude: this.params.sourceLong,
        destinationLatitude: this.params.destLat,
        destinationLongitude: this.params.destLong.toString().trim(),
      };
      return this.store.dispatch(
        CarActions.loadCarsByProxy({ proxyInfo: proxyPayload })
      );
    }
  }

  getProxyCars() {
    this.subscription.add(
      this.rideState$
        .pipe(
          switchMap((state) =>
            this.rideSchedule$.pipe(
              filter((res) => !!res),
              map((res) => {
                if (res) {
                  const proxyPayload: CarProximityRequest = {
                    page: this.currentPage,
                    pageSize: this.pageSize,
                    state: state as string,
                    scheduledDate: res.departureTime as string,
                    sourceLatitude: res.sourceLocation.latitude
                      .toString()
                      .trim(),
                    sourceLongitude: res.sourceLocation.longitude
                      .toString()
                      .trim(),
                    destinationLatitude: res.destinationLocation.latitude
                      .toString()
                      .trim(),
                    destinationLongitude: res.destinationLocation.longitude
                      .toString()
                      .trim(),
                  };
                  return this.store.dispatch(
                    CarActions.loadCarsByProxy({ proxyInfo: proxyPayload })
                  );
                } else {
                  console.log('no-res');
                }
              })
            )
          )
        )
        .subscribe()
    );
  }

  viewDetails(event: string, car: ProxyCar) {
    this.store.dispatch(CarActions.selectProxyDriverCar({ car }));
    this.router.navigateByUrl(`/ride-schedule/cars/${car.driver.id}`);
  }

  goBack() {
    // reset ride schedule state
    this.store.dispatch(RideScheduleActions.resetBooking());

    // reset car state
    this.store.dispatch(CarActions.resetCar());

    this.router.navigateByUrl(`/ride-schedule`);
  }

  ngOnDestroy(): void {
    if (this.subscription) this.subscription.unsubscribe();
  }
}
