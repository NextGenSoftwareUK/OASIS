import {
  ChangeDetectorRef,
  Component,
  OnChanges,
  OnDestroy,
  OnInit,
  SimpleChanges,
} from '@angular/core';
import { DashBoardCardInfo } from 'src/app/models/dashboard-card';
import { BookedRidesType } from 'src/app/models/booking-form';
import { Observable, of, Subscription } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import * as BookedRideSelectors from 'src/app/store/booked-rides/booked-rides.selectors';
import * as BookedRideActions from 'src/app/store/booked-rides/booked-rides.actions';
import { ButtonService } from '../../button/button.service';
import { ActivatedRoute } from '@angular/router';
import { DriverProfile } from 'src/app/models/user.model';
import { selectDriverProfile } from 'src/app/store/user/user.selectors';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { withdrawFromWallet } from 'src/app/store/withdrawal/withdrawal.actions';
import { FlutterWaveService } from '../../services/flutterwave.service';
import { defaultCurrency } from 'src/app/constants/currency';
import { PaymentSuccessResponse } from 'flutterwave-angular-v3';
import { topupWallet } from 'src/app/store/transaction/transaction.actions';

@Component({
  selector: 'app-driver',
  templateUrl: './driver.component.html',
  styleUrls: ['./driver.component.css'],
})
export class DriverComponent implements OnInit, OnChanges, OnDestroy {
  bookings$!: Observable<BookedRidesType | null>;
  profile$!: Observable<DriverProfile | null>;
  profile!: DriverProfile | null;
  totalRides$!: Observable<number | undefined>;
  totalAcceptedRides$!: Observable<number | undefined>;
  totalCancelledRides$!: Observable<number | undefined>;
  totalPendingRides$!: Observable<number | undefined>;
  totalCompletedRides$!: Observable<number | undefined>;
  userRole: string = '';
  showWidget: boolean = false;
  withdrawForm: FormGroup = this.formBuilder.group({
    amountControl: [0, [Validators.required]],
  });
  transactionType: string = '';

  subscriptions: Subscription[] = [];
  driverSummary: DashBoardCardInfo[] = [
    {
      title: 'Total Rides',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/total.png',
    },
    {
      title: 'Pending',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/pending.png',
    },
    {
      title: 'Accepted',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/accepted.png',
    },
    {
      title: 'Cancelled',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/cancelled.png',
    },
    {
      title: 'Completed',
      value: '0',
      imagePath: 'assets/images/dashboard-cards/completed.png',
    },
  ];
  defaultCurrency = defaultCurrency;
  referenceId: string = '';

  constructor(
    private store: Store<AppState>,
    private buttonService: ButtonService,
    private activatedRoute: ActivatedRoute,
    private formBuilder: FormBuilder,
    private flutterwaveService: FlutterWaveService,
    private changeDetectorRef: ChangeDetectorRef
  ) {
    this.bookings$ = this.store.select(BookedRideSelectors.selectAllBookings);
    this.profile$ = this.store.select(selectDriverProfile);
    this.totalRides$ = this.store.select(BookedRideSelectors.selectTotalRides);
    this.totalAcceptedRides$ = this.store.select(
      BookedRideSelectors.selectTotalAcceptedRides
    );
    this.totalCancelledRides$ = this.store.select(
      BookedRideSelectors.selectTotalCancelledRides
    );
    this.totalPendingRides$ = this.store.select(
      BookedRideSelectors.selectTotalPendingRides
    );
    this.totalCompletedRides$ = this.store.select(
      BookedRideSelectors.selectTotalCompletedRides
    );

    // subscribe to profile
    this.profile$.subscribe((profile) => {
      if (profile) {
        this.profile = profile;
      }
    });
  }

  ngOnInit(): void {
    // Dispatch the action to load bookings
    this.store.dispatch(BookedRideActions.loadBookings());

    this.subscriptions.push(
      this.buttonService.buttonEventListener().subscribe((value) => {
        this.bookings$ = this.store.select(
          BookedRideSelectors.selectAllBookings
        );
      })
    );

    // get userRole
    this.userRole = this.activatedRoute.snapshot.data['role'];

    this.subscriptions.push(
      this.withdrawForm
        .get('amountControl')
        ?.valueChanges.subscribe((value) => {
          if (value < 0 || value === '') {
            this.withdrawForm.get('amountControl')?.setValue(0);
          }
          this.handleAmountControlChange(value);

          if (this.profile) {
            this.referenceId = this.getReference();
          }
        }) as Subscription
    );
  }

  ngOnChanges(changes: SimpleChanges): void {
    this.changeDetectorRef.detectChanges();
  }

  handleAmountControlChange(value: string) {
    const amountControlControl = this.withdrawForm.get('amountControl');

    if (amountControlControl) {
      // Remove non-numeric characters

      if (value) {
        const numericValue = value.replace(/[^0-9]/g, '');

        // Prevent users from deleting all numbers (set to 0 if empty)
        if (numericValue === '') {
          amountControlControl.setValue('0', { emitEvent: false });
          return;
        }

        // Remove leading zero if there's any other digit
        if (numericValue.length > 1 && numericValue.startsWith('0')) {
          amountControlControl.setValue(numericValue.slice(1), {
            emitEvent: false,
          });
          return;
        }

        // Set the cleaned numeric value
        if (value !== numericValue) {
          amountControlControl.setValue(numericValue, { emitEvent: false });
        }
      }
    }
  }

  trackByFn(index: number) {
    return index;
  }

  getObservableByIndex(index: number): Observable<number | undefined> {
    switch (index) {
      case 0:
        return this.totalRides$;
      case 1:
        return this.totalPendingRides$;
      case 2:
        return this.totalAcceptedRides$;
      case 3:
        return this.totalCancelledRides$;
      case 4:
        return this.totalCompletedRides$;
      default:
        return of(0);
    }
  }

  showControl(show: boolean, type: string) {
    this.showWidget = show;
    this.transactionType = type;
  }

  withdraw() {
    if (this.withdrawForm.valid) {
      if (this.transactionType === 'withdraw') {
        this.store.dispatch(
          withdrawFromWallet({
            amount: +this.withdrawForm.get('amountControl')?.value,
          })
        );
      } else {
        // show flutterwave payment page
      }

      this.withdrawForm.reset();

      this.showControl(false, '');
    }
  }

  getControl(control: string) {
    return this.withdrawForm.get(control) as FormControl;
  }

  /** FLUTTERWAVE */

  getKey(): string {
    return this.flutterwaveService.getPublicKey();
  }

  getPaymentOption() {
    return this.flutterwaveService.getPaymentOptionMax();
  }

  getCustomerDetails() {
    return {
      driverId: this.profile?.id,
      full_name: this.profile?.fullName,
      role: this.profile?.role || this.profile?.type,
      amount: defaultCurrency + this.getControl('amountControl').value,
      name: this.profile?.fullName,
      phone_number: this.profile?.phone,
      email: this.profile?.email,
    };
  }

  getCustomizations() {
    if (this.profile) {
      return this.flutterwaveService.setupCustomizations(
        'Wallet Topup',
        `Topup for ${this.profile.fullName} [${defaultCurrency}${
          this.getControl('amountControl').value
        }]`
      );
    } else {
      alert('Customizations not found');
    }
    return {};
  }

  getMeta() {
    return {
      customer_email: this.profile?.email,
      phone_number: this.profile?.phone,
    };
  }

  getReference() {
    let transactionTime = this.flutterwaveService.generateReference();
    return transactionTime;
  }

  detectChange() {
    this.changeDetectorRef.detectChanges();
  }

  makePaymentCallback(event: PaymentSuccessResponse) {
    this.flutterwaveService.closeModal();
    this.withdrawForm.reset();
    this.showControl(false, '');

    // this.withdrawForm.reset();

    if (event.status === 'successful') {
      // dispatch topup action
      this.store.dispatch(
        topupWallet({
          data: {
            trxId: (event.transaction_id as number)?.toString(),
            trxRef: event.tx_ref as string,
            amount: event.amount as number,
          },
        })
      );
    }
  }

  closedPaymentModal(event: any): void {
    // this.changeDetectorRef.detectChanges();
    console.log('closed modal event');
  }

  /** FLUTTERWAVE */

  ngOnDestroy(): void {
    this.subscriptions.forEach((sub) => sub.unsubscribe());
  }
}
