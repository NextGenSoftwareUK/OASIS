import {
  carsURL,
  currentCarURL,
  carProximityURL,
  usersURL,
  verifyCarURL,
} from './../../base-url/base-url';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, of } from 'rxjs';
import {
  Car,
  CarProximityRequest,
  CarProximityResponse,
  CarRequest,
  ProxyCar,
  VerifyCar,
} from 'src/app/models/car';
import { Profile } from 'src/app/models/user.model';

@Injectable({
  providedIn: 'root',
})
export class CarService {
  private carsURL: string = carsURL;
  private currentCarURL: string = currentCarURL;
  private carProximityUrl = carProximityURL;

  // Declare variable to hold trip cost
  tripCost: string = '';

  constructor(private http: HttpClient) {}

  // Endpoint to get all cars from JSON file
  getCars(): Observable<Car[]> {
    return this.http.get(this.carsURL).pipe(map((res: any) => res));
  }

  // Endpoint to get selected cars for rebooking
  getSelectedRebookedCar(
    carId: string,
    bookingId?: string
  ): Observable<ProxyCar> {
    return this.http
      .get<ProxyCar>(
        `${this.carsURL}/${carId}${bookingId ? '?bookingId=' + bookingId : ''}`
      )
      .pipe(map((res: any) => res));
  }

  createCar(carPayload: CarRequest): Observable<Car> {
    return this.http.post<Car>(this.carsURL, carPayload);
  }

  getCarByUserId(id: string | undefined): Observable<Car[]> {
    if (id) {
      return this.http.get<Car[]>(`${this.currentCarURL}/${id}`);
    } else {
      return of([]);
    }
  }

  getDriverById(id: string | undefined): Observable<Profile | null> {
    if (id) {
      return this.http
        .get<{ data: Profile }>(`${usersURL}/${id}`)
        .pipe(map((data) => data.data));
    } else {
      return of(null);
    }
  }

  getCarsByProximity(
    payload: CarProximityRequest
  ): Observable<CarProximityResponse> {
    const newProximityURL = this.carProximityUrl
      .replace('<pageCount>', payload.page)
      .replace('<pageSize>', payload.pageSize)
      .replace('<state>', payload.state)
      .replace('<date>', payload.scheduledDate)
      .replace('<sourceLatitude>', payload.sourceLatitude)
      .replace('<sourceLongitude>', payload.sourceLongitude)
      .replace('<destinationLatitude>', payload.destinationLatitude)
      .replace('<destinationLongitude>', payload.destinationLongitude);
    //
    return this.http.get<CarProximityResponse>(newProximityURL);
  }

  updateCar(car: Car) {
    return this.http.put<Car>(`${this.carsURL}/${car.id}`, car);
  }

  verifyCar(carInfo: VerifyCar) {
    return this.http.put<Car>(`${verifyCarURL}`, carInfo);
  }
}
