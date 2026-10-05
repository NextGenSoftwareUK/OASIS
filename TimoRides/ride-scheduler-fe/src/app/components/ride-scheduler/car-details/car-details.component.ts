import { Component, OnDestroy, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { Observable, Subscription } from 'rxjs';
import { currency } from 'src/data/constants';
import { Store } from '@ngrx/store';
import {
  clearSelection,
  loadSelectedDriver,
} from 'src/app/store/cars/car.actions';
import { AppState } from 'src/app/app.state';
import {
  selectCarInfo,
  selectSelectedDriver,
  selectSelectedProxyCar,
} from 'src/app/store/cars/car.selectors';
import { Car, CarInfo, ProxyCar } from 'src/app/models/car';
import { DriverProfile, Profile, UserProfile } from 'src/app/models/user.model';
import * as RideScheduleActions from '../../../store/ride-booking/state/actions/ride-schedule-page.action';
import {
  selectRebooking,
  selectRideSchedule,
} from 'src/app/store/ride-booking/state/ride-schedule.selectors';
import { SnackbarUtilService } from '../../shared/snackbar/snackbar-util.service';
import { BookingRequest, SaveRebooking } from 'src/app/models/booking-form';
import { RouteParams } from 'src/app/models/rebooking';
import * as CarActions from '../../../store/cars/car.actions';
import { selectUserProfile } from 'src/app/store/user/user.selectors';

@Component({
  selector: 'app-car-details',
  templateUrl: './car-details.component.html',
  styleUrls: ['./car-details.component.css'],
})
export class CarDetailsComponent implements OnInit, OnDestroy {
  carInfo!: CarInfo;
  mergeSubscription: Subscription = new Subscription();
  paymentCurrency = currency;
  carInfo$: Observable<CarInfo | null>;
  car$: Observable<ProxyCar | null>;
  driver$: Observable<Profile | null>;
  profile$: Observable<Profile | null>;
  rebooking$: Observable<SaveRebooking | null>;
  rideSchedule$: Observable<any>;
  isProxyCar: boolean = false;
  car: Car | ProxyCar | null = null;
  driver: Profile | null = null;
  profile: Profile | null = null;
  rebooking: SaveRebooking | null = null;
  rideSchedule: BookingRequest | null = null;
  params: RouteParams | undefined;
  routeWithParams: string = '';
  private carId: string;

  constructor(
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private store: Store<AppState>,
    private _snackBar: SnackbarUtilService
  ) {
    this.carInfo$ = this.store.select(selectCarInfo);
    this.car$ = this.store.select(selectSelectedProxyCar);
    this.driver$ = this.store.select(selectSelectedDriver);
    this.profile$ = this.store.select(selectUserProfile); //remove later
    this.rebooking$ = this.store.select(selectRebooking);
    this.rideSchedule$ = this.store.select(selectRideSchedule);

    // set carId for rebooking cases
    this.carId =
      this.activatedRoute.snapshot.url[
        this.activatedRoute.snapshot.url.length - 1
      ].path;

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
      // save route param string for backward nav
      this.routeWithParams = this.router.url;

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
      // call endpoint to return car details
      this.store.dispatch(
        CarActions.loadSelectedRebookedCar({
          carId: this.carId,
          bookingId: this.params.bookingId,
        })
      );
    }

    this.mergeSubscription.add(
      this.car$.subscribe((value) => {
        this.car = value;

        if (typeof value?.driver === 'string') {
          this.store.dispatch(loadSelectedDriver({ id: value.driver }));
        } else if (
          value?.driver &&
          'id' in value?.driver &&
          !('profileImg' in value?.driver) &&
          !('fullName' in value?.driver)
        ) {
          this.store.dispatch(loadSelectedDriver({ id: (value?.driver).id }));
        }
      })
    );

    this.mergeSubscription.add(
      this.driver$.subscribe((value) => {
        this.driver = value;
      })
    );

    this.mergeSubscription.add(
      this.profile$.subscribe((value) => (this.profile = value))
    );

    this.mergeSubscription.add(
      this.rideSchedule$.subscribe((value) => (this.rideSchedule = value))
    );

    this.mergeSubscription.add(
      this.rebooking$.subscribe((value) => (this.rebooking = value))
    );
  }

  isCarOrProxy(car: Car | ProxyCar): car is ProxyCar {
    return 'rideAmount' in car ? true : false;
  }

  isDriverOrUser(
    key: string,
    person: UserProfile | DriverProfile
  ): person is DriverProfile {
    return key in person ? true : false;
  }

  goBack() {
    this.store.dispatch(clearSelection());
    this.removeSubscription();
    if (this.params) {
      this.router.navigateByUrl(
        'ride-schedule/cars?' + this.routeWithParams.split('?')[1]
      );
    } else {
      this.router.navigateByUrl('ride-schedule/cars');
    }
  }

  navigateToPayment() {
    if (
      (this.rebooking && !this.rideSchedule) ||
      (this.rebooking &&
        this.rideSchedule &&
        +this.rebooking.sourceLat ===
          this.rideSchedule?.sourceLocation.latitude &&
        +this.rebooking.sourceLong ===
          this.rideSchedule?.sourceLocation.longitude &&
        +this.rebooking.destLat ===
          this.rideSchedule?.destinationLocation.latitude &&
        +this.rebooking.destLong ===
          this.rideSchedule?.sourceLocation.longitude)
    ) {
      this.store.dispatch(
        RideScheduleActions.submitRebooking({
          rebookingInfo: {
            car: this.car?.id as string,
            bookingId: this.rebooking.bookingId,
          },
        })
      );
    } else {
      if (this.car && this.driver && this.rideSchedule) {
        this.router.navigateByUrl('/ride-schedule/pay');
      } else {
        this._snackBar.displaySnackBar(
          'Please make sure all required information is available before proceeding to payment.',
          '',
          'red-snackbar'
        );
      }
    }
  }

  ngOnDestroy(): void {
    this.removeSubscription();
  }

  removeSubscription() {
    this.mergeSubscription.unsubscribe();
  }
}
