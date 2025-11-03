import { Component, ViewChild } from '@angular/core';
import { GooglePlaceDirective } from 'ngx-google-places-autocomplete/ngx-google-places-autocomplete.directive';
import { Address } from 'ngx-google-places-autocomplete/objects/address';
import { Options } from 'ngx-google-places-autocomplete/objects/options/options';

@Component({
  selector: 'app-location-autocomplete',
  templateUrl: './location-autocomplete.component.html',
  styleUrls: ['./location-autocomplete.component.css'],
})
export class LocationAutocompleteComponent {
  @ViewChild('placesRef') placesRef?: GooglePlaceDirective;

  formattedAddress?: string;

  options: Options = new Options({
    bounds: undefined,
    fields: ['address_component'],
    strictBounds: false,
    types: ['geocode', 'route'],
    componentRestrictions: { country: 'US' },
  });

  public handleAddressChange(address: Address) {
    this.formattedAddress = address.formatted_address;
  }
}
