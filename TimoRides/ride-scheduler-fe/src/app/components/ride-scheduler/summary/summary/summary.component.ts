import { Component, Input, OnDestroy } from '@angular/core';
import { Profile } from 'src/app/models/user.model';
import { Car, CarInfo } from 'src/app/models/car';
import { Store } from '@ngrx/store';
import { Observable, Subscription } from 'rxjs';
import { AppState } from 'src/app/app.state';
import {
  selectSelectedCar,
  selectCarInfo,
  selectSelectedDriver,
} from 'src/app/store/cars/car.selectors';
import { selectRideSchedule } from 'src/app/store/ride-booking/state/ride-schedule.selectors';
import { selectUserProfile } from 'src/app/store/user/user.selectors';

@Component({
  selector: 'app-summary',
  templateUrl: './summary.component.html',
  styleUrls: ['./summary.component.css'],
})
export class SummaryComponent implements OnDestroy {
  @Input('hasPaid') hasPaid: boolean = false;
  mergeSubscription: Subscription | undefined;
  selectedCar$: Observable<Car | null>;
  carInfo$: Observable<CarInfo | null>;
  car$: Observable<Car | null>;
  driver$: Observable<Profile | null>;
  profile$: Observable<Profile | null>;
  rideSchedule$: Observable<any>;

  constructor(private store: Store<AppState>) {
    this.selectedCar$ = this.store.select(selectSelectedCar);
    this.carInfo$ = this.store.select(selectCarInfo);

    this.car$ = this.store.select(selectSelectedCar);
    this.driver$ = this.store.select(selectSelectedDriver);

    this.profile$ = this.store.select(selectUserProfile);
    this.rideSchedule$ = this.store.select(selectRideSchedule);
  }

  ngOnDestroy(): void {
    this.mergeSubscription?.unsubscribe();
  }
}
