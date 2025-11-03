import { Injectable } from '@angular/core';
// declare var google: any;
/**
 * WARNING: The import below fixes this error in the terminal:
 * error TS2304: Cannot find name 'google'.
 */

import { google } from 'google-maps';
import { MapLocation } from 'src/app/models/booking-form';

/**
 * WARNING: The import above fixes this error in the terminal:
 * error TS2304: Cannot find name 'google'.
 */

@Injectable({
  providedIn: 'root',
})
export class GoogleAddressService {
  constructor() {}

  setupGoogleAddress(ids: string[]): string {
    ids.forEach((id) => {
      const locationField = document.getElementById(id) as HTMLInputElement;

      if (locationField instanceof HTMLInputElement) {
        const autocomplete = new google.maps.places.Autocomplete(locationField);

        autocomplete.addListener('place_changed', () => {
          const place = autocomplete.getPlace();
          const formattedAddress = `${
            place.formatted_address?.startsWith(place.name as string)
              ? place.formatted_address
              : place.name + ', ' + place.formatted_address
          }`;
          locationField.value = formattedAddress;
          return formattedAddress;
        });
      } else {
        console.error(
          `Element with ID ${id} is not an instance of HTMLInputElement or was not found.`
        );
      }
    });
    return '';
  }

  GetLocationCoordinates(
    location: google.maps.places.PlaceResult
  ): MapLocation | false {
    // Get coordinate of respective location
    const address = location.formatted_address;
    const longitude = location.geometry?.location?.lng();
    const latitude = location.geometry?.location?.lat();

    return address && longitude && latitude
      ? {
          address,
          latitude,
          longitude,
        }
      : false;
  }
}
