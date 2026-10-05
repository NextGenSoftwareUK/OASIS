import { Component, OnInit } from '@angular/core';
import { BehaviorSubject, combineLatest, map, Observable, of } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import * as AdminSelectors from '../store/admin.selectors';
import * as AdminActions from '../store/admin.actions';
import * as BookingActions from 'src/app/store/booked-rides/booked-rides.actions';
import * as BookingSelectors from 'src/app/store/booked-rides/booked-rides.selectors';
import { Profile } from 'src/app/models/user.model';
import { BookedRidesType } from 'src/app/models/booking-form';
import { Router } from '@angular/router';
import { FormBuilder, FormGroup } from '@angular/forms';
import { AdminConfig, WithdrawalPaymentModel } from '../store/admin.model';
import { DashBoardCardInfo } from 'src/app/models/dashboard-card';

@Component({
  selector: 'app-admin-dashboard',
  templateUrl: './admin-dashboard.component.html',
  styleUrls: ['./admin-dashboard.component.css'],
})
export class AdminDashboardComponent implements OnInit {
  users$!: Observable<Profile[]>;
  bookings$!: Observable<BookedRidesType | null>;
  totalCustomers$!: Observable<number>;
  totalDrivers$!: Observable<number>;
  totalAdmins$!: Observable<number>;
  totalUsers$!: Observable<number>;
  selectedUser: Profile | null = null;
  configForm!: FormGroup;
  config$!: Observable<AdminConfig | null>;
  withdrawals$!: Observable<WithdrawalPaymentModel[] | null>;
  filteredWithdrawals$!: Observable<WithdrawalPaymentModel[] | null>;
  loading = true;

  private filterSubject = new BehaviorSubject<string>('all');

  adminSummary: DashBoardCardInfo[] = [
    {
      title: 'Total Customers',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/total.png',
    },
    {
      title: 'Total Drivers',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/pending.png',
    },
    {
      title: 'Total Admins',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/accepted.png',
    },
    {
      title: 'Total Users',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/cancelled.png',
    },
  ];

  constructor(
    private store: Store<AppState>,
    private router: Router,
    private formBuilder: FormBuilder
  ) {
    this.store.dispatch(BookingActions.loadBookings());
    this.store.dispatch(AdminActions.loadUsers());
    this.store.dispatch(AdminActions.loadConfig());
    this.store.dispatch(AdminActions.loadWithdrawals());

    this.configForm = this.formBuilder.group({
      tripRate: [''],
      businessCommission: [''],
      minWalletBalance: [''],
    });
  }

  ngOnInit(): void {
    this.users$ = this.store.select(AdminSelectors.selectAllUsers);
    this.bookings$ = this.store.select(BookingSelectors.selectAllBookings);
    this.totalCustomers$ = this.store.select(
      AdminSelectors.selectTotalCustomers
    );
    this.totalDrivers$ = this.store.select(AdminSelectors.selectTotalDrivers);
    this.totalAdmins$ = this.store.select(AdminSelectors.selectTotalAdmins);
    this.totalUsers$ = this.store.select(AdminSelectors.selectTotalUsers);
    this.config$ = this.store.select(AdminSelectors.selectConfigs);
    this.withdrawals$ = this.store.select(AdminSelectors.selectWithdrawals);

    this.filteredWithdrawals$ = combineLatest([
      this.withdrawals$,
      this.filterSubject,
    ]).pipe(
      map(([withdrawals, filter]) => {
        const validWithdrawals = withdrawals ?? [];
        switch (filter) {
          case 'pending':
            return validWithdrawals.filter((w) => w.status === 'pending');
          case 'completed':
            return validWithdrawals.filter((w) => w.status === 'completed');
          default:
            return validWithdrawals;
        }
      })
    );

    this.config$.subscribe({
      next: () => {
        this.loading = false;
      },
      error: () => {
        this.loading = false;
      },
    });
  }

  trackByFn(index: number) {
    return index;
  }

  getObservableByIndex(index: number): Observable<number | undefined> {
    switch (index) {
      case 0:
        return this.totalCustomers$;
      case 1:
        return this.totalDrivers$;
      case 2:
        return this.totalAdmins$;
      case 3:
        return this.totalUsers$;
      default:
        return of(0);
    }
  }

  openUserDetails(user: Profile) {
    this.selectedUser = user;
  }

  closeUserDetails() {
    this.selectedUser = null;
  }

  viewUserDetails(user: Profile): void {
    if (user.role === 'driver') {
      this.router.navigate(['/admin/driver-details', user.id]);
    } else {
      this.router.navigate(['/admin/customer-details', user.id]);
    }
  }

  openBookingDetails(booking: BookedRidesType) {
    // Implement booking details view logic
  }

  setWalletBalance() {
    const newValue = this.configForm.get('minWalletBalance')?.value;

    if (newValue) {
      this.store.dispatch(
        AdminActions.updateConfig({
          config: { driverWalletPercentage: +newValue / 100 },
        })
      );
      this.configForm.get('minWalletBalance')?.reset();
    }
  }

  setCommission() {
    const newValue = this.configForm.get('businessCommission')?.value;

    if (newValue) {
      this.store.dispatch(
        AdminActions.updateBusinessCommission({
          config: { businessCommission: +newValue / 100 },
        })
      );
      this.configForm.get('businessCommission')?.reset();
    }
  }

  setTripRate() {
    const newValue = this.configForm.get('tripRate')?.value;

    if (newValue) {
      this.store.dispatch(
        AdminActions.updateConfig({
          config: { pricePerKm: +newValue },
        })
      );

      this.configForm.get('tripRate')?.reset();
    }
  }

  filterAll(): void {
    this.filterSubject.next('all');
  }

  filterPending(): void {
    this.filterSubject.next('pending');
  }

  filterCompleted(): void {
    this.filterSubject.next('completed');
  }
}
