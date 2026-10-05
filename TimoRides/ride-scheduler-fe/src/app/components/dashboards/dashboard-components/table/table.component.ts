import { Component, Input } from '@angular/core';
import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';
import { AppState } from 'src/app/app.state';
import { BookedRidesInfo } from 'src/app/models/booking-form';
import { UserRole } from 'src/app/models/user.model';
import { selectUserRole } from 'src/app/store/auth/auth.selectors';
import * as AdminActions from '../../admin/store/admin.actions';

export type ITableData = {
  type:
    | 'booked rides'
    | 'admin cars'
    | 'admin drivers'
    | 'customers'
    | 'admin users'
    | 'admin rides'
    | 'admin withdrawals';
  value: any[];
};

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.css'],
})
export class TableComponent {
  @Input('tableData') tableData!: ITableData;
  showRideDetails: boolean = false;
  currentDetails: BookedRidesInfo | undefined;
  userRole$: Observable<UserRole | undefined>;
  defaultImage: string = 'assets/images/profile-default.svg';

  constructor(private store: Store<AppState>) {
    this.userRole$ = this.store.select(selectUserRole);
  }

  setCurrentDetails = (index: number): void => {
    this.currentDetails = this.tableData.value[index];
    this.toggleRideDetailsContainer();
  };

  activeCar(driver: any) {
    // if both are true, deactivate car.
    // if isActive && !isVerify
    if (driver.cars[0].isActive === false && driver.cars[0].isVerify === true) {
      this.store.dispatch(
        AdminActions.verifyCar({
          driverId: driver.id,
          isVerify: true,
          isActive: true,
        })
      );
    } else if (
      driver.cars[0].isActive === true &&
      driver.cars[0].isVerify === true
    ) {
      // deactivate car
      this.store.dispatch(
        AdminActions.verifyCar({
          driverId: driver.id,
          isVerify: true,
          isActive: false,
        })
      );
    }
  }

  verifyCar(driver: any) {
    if (
      driver.cars[0].isActive === false &&
      driver.cars[0].isVerify === false
    ) {
      // verify car
      this.store.dispatch(
        AdminActions.verifyCar({
          driverId: driver.id,
          isVerify: true,
          isActive: false,
        })
      );
    } else if (
      driver.cars[0].isActive === false &&
      driver.cars[0].isVerify === true
    ) {
      // disable car
      this.store.dispatch(
        AdminActions.verifyCar({
          driverId: driver.id,
          isVerify: false,
          isActive: false,
        })
      );
    }
  }

  payUser(value: any) {
    // dispatch payment action
    this.store.dispatch(
      AdminActions.payUser({
        requestId: value.id,
      })
    );
  }

  removeCurrentDetails() {
    this.currentDetails = undefined;
    this.toggleRideDetailsContainer();
  }

  toggleRideDetailsContainer(): void {
    this.showRideDetails = this.currentDetails !== undefined;
  }

  trackByFn(index: number): number {
    return index;
  }
}
