import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { pricingURL } from 'src/app/base-url/base-url';
import {
  LocationInfo,
  RidePricePayload,
  RidePriceResponse,
} from 'src/app/models/pricing';
import { tripRate } from 'src/data/constants';

@Injectable({
  providedIn: 'root',
})
export class RideScheduleService {
  private tripCostURL: string = pricingURL;
  constructor(private http: HttpClient) {}

  getTripCost(coordinates: LocationInfo) {
    const tripCostPayload: RidePricePayload = {
      sourceCoordinate: {
        latitude: (<any>coordinates.pickupLocation)['latitude'],
        longitude: (<any>coordinates.pickupLocation)['latitude'],
      },
      destinationCoordinate: {
        latitude: (<any>coordinates.dropOffLocation)['latitude'],
        longitude: (<any>coordinates.dropOffLocation)['latitude'],
      },
      amountPerKilo: tripRate,
    };

    return this.http.post<RidePriceResponse>(this.tripCostURL, tripCostPayload);
  }
}
