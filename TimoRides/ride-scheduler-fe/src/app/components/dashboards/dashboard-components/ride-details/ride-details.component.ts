import { Component, EventEmitter, Input, Output } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { ButtonService } from 'src/app/components/button/button.service';
import { BookedRidesInfo } from 'src/app/models/booking-form';
import { TRIP_MODE_ENUMS } from 'src/app/models/trip';
import * as BookedRideActions from 'src/app/store/booked-rides/booked-rides.actions';

@Component({
  selector: 'app-ride-details',
  templateUrl: './ride-details.component.html',
  styleUrls: ['./ride-details.component.css'],
})
export class RideDetailsComponent {
  @Input('rideDetails') rideDetails: BookedRidesInfo | undefined;
  @Output() onDialogClick: EventEmitter<boolean | undefined> = new EventEmitter<
    boolean | undefined
  >();
  tripForm: FormGroup;

  constructor(
    private store: Store<AppState>,
    private buttonService: ButtonService,
    private formBuilder: FormBuilder
  ) {
    this.tripForm = this.formBuilder.group({
      tripControl: ['', [Validators.required]],
    });
  }

  closeDialog(isAccepted: boolean | undefined) {
    this.onDialogClick.emit(isAccepted);
  }

  trackByFn(index: number): number {
    return index;
  }

  acceptRide() {
    this.store.dispatch(
      BookedRideActions.acceptRide({
        bookingId: this.rideDetails?.id as string,
      })
    );
    this.buttonService.emitButtonEvent('accept ride');
    this.closeDialog(true);
  }

  cancelRide() {
    this.store.dispatch(
      BookedRideActions.cancelRide({
        bookingId: this.rideDetails?.id as string,
      })
    );
    this.buttonService.emitButtonEvent('cancel ride');
    this.closeDialog(false);
  }

  startTrip() {
    this.store.dispatch(
      BookedRideActions.startRide({
        tripInfo: {
          bookingId: this.rideDetails?.id as string,
          otpCode: this.tripForm.get('tripControl')?.value,
          tripMode: TRIP_MODE_ENUMS.START,
        },
      })
    );

    this.resetForm();
    this.closeDialog(true);
  }

  endTrip() {
    this.store.dispatch(
      BookedRideActions.endRide({
        tripInfo: {
          bookingId: this.rideDetails?.id as string,
          otpCode: this.tripForm.get('tripControl')?.value,
          tripMode: TRIP_MODE_ENUMS.END,
        },
      })
    );

    this.resetForm();
    this.closeDialog(true);
  }

  resetForm() {
    this.tripForm.reset();
  }
}
