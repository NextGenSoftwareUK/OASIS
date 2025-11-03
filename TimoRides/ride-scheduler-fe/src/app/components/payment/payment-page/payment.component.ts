import { Component, OnInit, ViewEncapsulation } from '@angular/core';
import { MatRadioChange } from '@angular/material/radio';
import { PaymentCard } from 'src/app/models/payment-card';
import { Location } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { AppState } from 'src/app/app.state';
import { Store } from '@ngrx/store';
import {
  choosePaymentOption,
  saveScheduledRide,
  updateRideSchedule,
} from 'src/app/store/ride-booking/state/actions/ride-schedule-page.action';
import { Observable } from 'rxjs';
import { Car, CarInfo, ProxyCar } from 'src/app/models/car';
import { selectCarInfo } from 'src/app/store/cars/car.selectors';
import { BookingPayload } from 'src/app/models/booking-form';
import { SnackbarUtilService } from '../../shared/snackbar/snackbar-util.service';

@Component({
  selector: 'app-payment',
  templateUrl: './payment.component.html',
  styleUrls: ['./payment.component.css'],
  encapsulation: ViewEncapsulation.None,
})
export class PaymentComponent implements OnInit {
  isClicked: boolean = false;
  paymentOption!: string;
  paymentCards: PaymentCard[] = [];
  hasPaid: boolean = false;
  carInfo$!: Observable<CarInfo | null>;
  bookingInfo: CarInfo | null = null;
  bookingID!: string;
  transactionRef!: string;
  showUserForm: boolean = false;

  constructor(
    private location: Location,
    private store: Store<AppState>,
    private activatedRoute: ActivatedRoute,
    private _snackBar: SnackbarUtilService
  ) {
    this.carInfo$ = this.store.select(selectCarInfo);
    this.carInfo$.subscribe(async (res) => {
      this.bookingInfo = res;
    });
  }

  async ngOnInit(): Promise<void> {
    const params = this.activatedRoute.snapshot.queryParams;
    if ('status' in params && params['status'] === 'successful') {
      if ('transaction_id' in params && 'tx_ref' in params) {
        this.bookingID = params['transaction_id'];
        this.transactionRef = params['tx_ref'];

        // update isCash, trxId, trxRef;
        this.store.dispatch(
          updateRideSchedule({
            trxId: this.bookingID,
            trxRef: this.transactionRef,
            isCash: false,
          })
        );
      } else {
        this._snackBar.displaySnackBar(
          'Invalid Keys, contact payment gateway for API issues.',
          'OK',
          'red-snackbar'
        );
      }

      if (this.bookingID && this.transactionRef) {
        await this.completePayment();
      }
    }
  }

  clickMe() {
    this.isClicked = true;
  }

  onRadioChange(event: MatRadioChange) {
    this.store.dispatch(
      choosePaymentOption({ isCash: event.value === 'optCash' })
    );
    this.paymentOption = event.value;
  }

  async saveCard(card: PaymentCard) {
    // Add the selected card to the list of saved cards
    this.paymentCards.push(card);
    await this.completePayment();
  }

  getAmount(): number {
    return +(this.bookingInfo?.car as ProxyCar).rideAmount;
  }

  isCarOrProxy(car: Car | ProxyCar | undefined): car is ProxyCar {
    if (!car) {
      return false;
    }
    return 'rideAmount' in car ? true : false;
  }

  goBack() {
    this.location.back();
  }

  displayUserForm() {
    this.showUserForm = true;
  }

  async completePayment() {
    if (
      this.bookingInfo?.car &&
      this.bookingInfo.driver &&
      this.bookingInfo.rideSchedule
    ) {
      const bookingPayload: BookingPayload = {
        car: this.bookingInfo?.car.id,
        fullName: this.bookingInfo.rideSchedule.fullName,
        phoneNumber: this.bookingInfo.rideSchedule.phoneNumber,
        bookingType: 'passengers', // hardcoded until goods transportation starts
        email: this.bookingInfo.rideSchedule.email,
        tripAmount: this.getAmount().toString(),
        tripDuration: (this.bookingInfo.car as ProxyCar).duration,
        tripDistance: (this.bookingInfo.car as ProxyCar).distance,
        state: (this.bookingInfo.car as ProxyCar).state,
        isCash: this.bookingInfo.rideSchedule.isCash,
        departureTime: this.bookingInfo.rideSchedule.departureTime,
        sourceLocation: {
          ...this.bookingInfo.rideSchedule.sourceLocation,
        },
        destinationLocation: {
          ...this.bookingInfo.rideSchedule.destinationLocation,
        },
        trxId: this.bookingInfo.rideSchedule.trxId,
        trxRef: this.bookingInfo.rideSchedule.trxRef,
        passengers: this.bookingInfo.rideSchedule.passengers,
      };

      this.store.dispatch(saveScheduledRide({ scheduledRide: bookingPayload }));
    }
  }
}
