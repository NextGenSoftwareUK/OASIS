/**
 * WARNING: The import below fixes this error in the terminal:
 * error TS2304: Cannot find name 'google'.
 */

// import { google } from 'google-maps';

/**
 * WARNING: The import above fixes this error in the terminal:
 * error TS2304: Cannot find name 'google'.
 */

import { AfterViewInit, Component, OnDestroy, OnInit } from '@angular/core';
import { SelectConfiguration } from '../../../models/ride-form';
import { Router } from '@angular/router';
import { Store } from '@ngrx/store';
import * as RideScheduleActions from '../../../store/ride-booking/state/actions/ride-schedule-page.action';
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from '@angular/forms';
import { LocationInfo, RidePriceResponse } from 'src/app/models/pricing';
import { AppState } from 'src/app/app.state';
import { BookingRequest, MapLocation } from 'src/app/models/booking-form';
import {
  enterDropOffLocation,
  enterPickupLocation,
} from '../../../store/ride-booking/state/actions/ride-schedule-page.action';
import { selectRideSchedule } from 'src/app/store/ride-booking/state/ride-schedule.selectors';
import { Observable, Subscription } from 'rxjs';
import { DatePipe } from '@angular/common';
import { rideOptions } from 'src/app/constants/ride-schedule.constants';
import { SnackbarUtilService } from '../../shared/snackbar/snackbar-util.service';
import { environment } from 'src/environments/environment';

@Component({
  selector: 'app-ride-form',
  templateUrl: './ride-form.component.html',
  styleUrls: ['./ride-form.component.css'],
})
export class RideFormComponent implements OnInit, AfterViewInit, OnDestroy {
  currentDate = new Date().toISOString().substring(0, 10);
  currentTime: string = '';
  minDate = new Date().getTime();
  rideSchedule$: Observable<BookingRequest | null>;
  subscription: Subscription = new Subscription();
  selectCarOptions: SelectConfiguration[] = rideOptions;
  bookingForm!: FormGroup;
  currrentRideType: SelectConfiguration = {
    id: 0,
    name: 'Select ride type',
    value: '',
  };

  // Declare location holder to keep track of
  // pickup & dropoff coordinates
  selectedLocations: LocationInfo = {
    pickupLocation: false,
    dropOffLocation: false,
  };

  constructor(
    private router: Router,
    private store: Store<AppState>,
    private formBuilder: FormBuilder,
    private datePipe: DatePipe,
    private _snackBar: SnackbarUtilService
  ) {
    this.rideSchedule$ = this.store.select(selectRideSchedule);
  }

  // Form Variables
  isSelectCar: boolean = false;

  ngOnInit(): void {
    // initialize datePipe
    this.datePipe = new DatePipe('en-GB');

    // get current time
    this.currentTime = this.getCurrentTime();

    this.bookingForm = this.formBuilder.group({
      rideDate: [
        this.currentDate,
        [Validators.required, this.minDateValidator],
      ],
      rideTime: [this.currentTime, [Validators.required]],
      passengers: [1, [Validators.required, Validators.min(1)]],
      pickupLocation: ['', [Validators.required]],
      dropOffLocation: ['', [Validators.required]],
    });
  }

  ngAfterViewInit(): void {
    // dispatch page entry action
    this.store.dispatch(RideScheduleActions.enter());

    // Load the Google Maps API script and initialize autocomplete once loaded
    this.loadGoogleMapsScript().then(() => {
      this.initializeAutocomplete();
    });
  }

  loadGoogleMapsScript(): Promise<void> {
    return new Promise((resolve, reject) => {
      if (typeof google !== 'undefined' && google.maps) {
        resolve();
        return;
      }
      const script = document.createElement('script');
      script.src = environment.mapsURL;
      script.id = 'google-maps-script';
      script.async = true;
      script.defer = true;
      script.setAttribute('loading', 'async');
      script.onload = () => resolve();
      script.onerror = (error) => reject(error);
      document.body.appendChild(script);
    });
  }

  initializeAutocomplete(): void {
    const locationFields: NodeListOf<Element> =
      document.querySelectorAll('[id*=Location]');

    locationFields.forEach((locationField) => {
      /**
       * Subscribe to valueChanges for each location field
       * Set value in selectedLocations to false if field is empty
       */
      this.subscription.add(
        this.bookingForm
          .get(locationField.id)
          ?.valueChanges.subscribe((value) => {
            if (!value) {
              this.setCoordinate(locationField.id, false);
            }
          })
      );

      // Configure autocomplete for each location field
      let autocomplete = new google.maps.places.Autocomplete(
        locationField as HTMLInputElement
      );

      // Add event listener to each location field
      autocomplete.addListener('place_changed', () => {
        const place = autocomplete?.getPlace();

        // Update the formcontrol value
        this.bookingForm
          .get(locationField.id)
          ?.setValue(
            (<HTMLInputElement>document.getElementById(locationField.id)).value
          );

        if (!place) {
          this._snackBar.displaySnackBar(
            'location not found',
            '',
            'red-snackbar'
          );
          return;
        } else {
          // Set location coordinates
          this.setCoordinate(
            locationField.id,
            this.GetLocationCoordinates(place)
          );
        }
      });
    });
  }

  getCurrentTime(): string {
    const now = new Date();
    const hours = now.getHours().toString().padStart(2, '0');
    const minutes = now.getMinutes().toString().padStart(2, '0');
    return `${hours}:${minutes}`;
  }

  GetLocationCoordinates(
    location: google.maps.places.PlaceResult
  ): MapLocation | false {
    // Get coordinate of respective location
    const address = location.formatted_address;
    const countryState = location.address_components
      ?.find((component) =>
        component.types.includes('administrative_area_level_1')
      )
      ?.long_name.replace(' state', '')
      .replace(' State', '');
    const longitude = location.geometry?.location?.lng();
    const latitude = location.geometry?.location?.lat();

    // dispatch request state
    if (countryState)
      this.store.dispatch(RideScheduleActions.saveState({ countryState }));

    return address && longitude && latitude
      ? {
          address,
          latitude,
          longitude,
        }
      : false;
  }

  setCoordinate(locationID: string, location: MapLocation | false) {
    //  Update selectedLocations variable accordingly
    this.selectedLocations = {
      ...this.selectedLocations,
      [locationID]: location
        ? {
            address: location.address,
            latitude: location.latitude,
            longitude: location.longitude,
          }
        : false,
    };

    if (location) {
      if (locationID.toLowerCase().includes('pickup')) {
        this.store.dispatch(enterPickupLocation({ pickUp: location }));
      } else {
        this.store.dispatch(enterDropOffLocation({ dropOff: location }));
      }
    }
  }

  minDateValidator(control: FormControl): { [key: string]: boolean } | null {
    const selectedDate = new Date(control.value).getTime();
    const currentDate = new Date().setHours(0, 0, 0, 0);

    // Compare selected date with current date
    if (selectedDate < currentDate) {
      return { minDate: true };
    }
    return null;
  }

  getControl(controlName: string) {
    return this.bookingForm.get(controlName) as FormControl;
  }

  // Toggle open/close
  toggleIsCar() {
    this.isSelectCar = !this.isSelectCar;
  }

  // // --__set Car type
  setCurrrentSelection(value: SelectConfiguration) {
    this.getControl('rideType').setValue(value.value);
  }

  // Handle form submit
  handleSubmit(event: Event) {
    if (Object.values(this.selectedLocations).includes(false)) {
      this._snackBar.displaySnackBar(
        'Missing pickup / dropoff location coordinate. Try searching again.',
        '',
        'red-snackbar'
      );
      return;
    }

    event.preventDefault();

    const bookingData: BookingRequest = {
      car: '',
      fullName: '',
      phoneNumber: '',
      passengers: this.getControl('passengers').value,
      bookingType: 'passengers',
      email: '',
      tripInfo: {} as RidePriceResponse,
      isCash: false,
      departureTime: this.convertDateToISO(
        this.getControl('rideDate').value,
        this.getControl('rideTime').value
      ),
      sourceLocation: {
        address: '',
        latitude: 0,
        longitude: 0,
      },
      destinationLocation: {
        address: '',
        latitude: 0,
        longitude: 0,
      },
    };

    if (this.bookingForm.valid) {
      // dispatch save action
      this.store.dispatch(
        RideScheduleActions.submitForm({ formData: bookingData })
      );

      // this._snackBar.displaySnackBar('Form created', 'OK', 'green-snackbar');

      // navigate to cars list page
      this.router.navigateByUrl('/ride-schedule/cars');
    } else {
      this._snackBar.displaySnackBar('Form invalid', 'OK', 'red-snackbar');
    }
  }

  convertDateToISO(dateStr: string, timeStr: string) {
    // Parse the date and time strings
    const [year, month, day] = dateStr.split('-').map(Number);
    const [hours, minutes] = timeStr.split(':').map(Number);

    // Create a new Date object with the parsed values
    // Note: Month is 0-indexed in JavaScript Date object
    const combinedDate = new Date(year, month - 1, day, hours, minutes);

    // Convert the Date object to ISO 8601 string
    // return combinedDate.toISOString();
    return dateStr + 'T' + timeStr + ':00.000Z';
  }

  ngOnDestroy(): void {
    if (this.subscription) {
      this.subscription.unsubscribe();
    }
  }
}
