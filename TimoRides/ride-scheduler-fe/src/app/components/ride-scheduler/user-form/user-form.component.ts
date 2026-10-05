import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Store } from '@ngrx/store';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { AppState } from 'src/app/app.state';
import { Observable, Subscription } from 'rxjs';
import { PaymentSuccessResponse } from 'flutterwave-angular-v3';
import { CarInfo, ProxyCar } from 'src/app/models/car';
import { selectCarInfo } from 'src/app/store/cars/car.selectors';
import { environment } from 'src/environments/environment';
import { updateBiodata } from 'src/app/store/ride-booking/state/actions/ride-schedule-page.action';
import { defaultCurrency } from 'src/app/constants/currency';
import { FlutterWaveService } from '../../services/flutterwave.service';

@Component({
  selector: 'app-user-form',
  templateUrl: './user-form.component.html',
  styleUrls: ['./user-form.component.css'],
})
export class UserRideFormComponent implements OnInit, OnDestroy {
  subscription: Subscription = new Subscription();
  carInfo$!: Observable<CarInfo | null>;
  bookingInfo: CarInfo | null = null;
  defaultCurrency = defaultCurrency;
  bookingForm!: FormGroup;
  referenceId: string = '';

  constructor(
    private store: Store<AppState>,
    private formBuilder: FormBuilder,
    private flutterwaveService: FlutterWaveService
  ) {
    this.carInfo$ = this.store.select(selectCarInfo);
    this.carInfo$.subscribe(async (res) => {
      this.bookingInfo = res;
    });
  }

  ngOnInit(): void {
    this.bookingForm = this.formBuilder.group({
      fullName: ['', [Validators.required]],
      email: ['', [Validators.required]],
      phoneNumber: ['', [Validators.required, Validators.email]],
    });

    this.subscription.add(
      this.bookingForm.valueChanges.subscribe(() => {
        this.saveBiodata();

        if (this.bookingForm.valid) {
          // style flutterwave component
          const btn = document.getElementsByClassName('flutterwave-pay-button');
          if (btn && btn.length > 0)
            (btn[0] as HTMLButtonElement).style.width = '100%';

          this.referenceId = this.generateReference();

          this.meta = {
            consumer_phone: this.bookingForm?.value?.['phoneNumber'], // this.bookingInfo?.rideSchedule?.phoneNumber,
            consumer_email: this.bookingForm?.value?.['email'], //this.bookingInfo?.rideSchedule?.email,
          };
        }
      })
    );
  }

  getControl(controlName: string) {
    return this.bookingForm.get(controlName) as FormControl;
  }

  saveBiodata() {
    // dispatch action to save biodata
    this.store.dispatch(
      updateBiodata({
        fullName: this.getControl('fullName').value,
        email: this.getControl('email').value,
        phoneNumber: this.getControl('phoneNumber').value,
      })
    );
  }

  /** FLUTTERWAVE IMPLEMENTATION */

  publicKey = environment.flutterKey;

  customizations = {
    title: 'Timorides Payment',
    description: `Booking for ${
      this.bookingInfo?.rideSchedule?.fullName || 'Customer'
    }`,
    logo: 'assets/images/logo.png',
  };

  meta = {
    consumer_phone: this.bookingForm?.value?.['phoneNumber'],
    consumer_email: this.bookingForm?.value?.['email'],
  };

  getAmount(): number {
    return +(this.bookingInfo?.car as ProxyCar).rideAmount;
  }

  getCustomerDetails() {
    return {
      ...this.bookingForm.value,
    };
  }

  makePaymentCallback(response: PaymentSuccessResponse): void {
    this.flutterwaveService.closeModal();
  }

  closedPaymentModal(): void {
    // this.changeDetectorRef.detectChanges();
  }

  generateReference(): string {
    let transactionTime = this.flutterwaveService.generateReference();
    return transactionTime;
  }

  /** FLUTTERWAVE IMPLEMENTATION */

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
