import { ProxyCar } from './../../../../models/car';
import { Component, Input } from '@angular/core';
import { Car } from 'src/app/models/car';
import { currency } from 'src/data/constants';

@Component({
  selector: 'app-car-details-summary',
  templateUrl: './car-details-summary.component.html',
  styleUrls: ['./car-details-summary.component.css'],
})
export class CarDetailsSummaryComponent {
  @Input('carDetails') carDetails!: Car | ProxyCar;
  paymentCurrency = currency;

  isProxyCar(car: Car | ProxyCar): car is ProxyCar {
    return 'rideAmount' in car ? true : false;
  }
}
