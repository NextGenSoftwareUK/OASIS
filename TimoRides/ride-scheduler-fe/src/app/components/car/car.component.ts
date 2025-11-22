import { Component, EventEmitter, Input, Output } from '@angular/core';
import { Car, ProxyCar } from 'src/app/models/car';
import { currency } from 'src/data/constants';

@Component({
  selector: 'app-car',
  templateUrl: './car.component.html',
  styleUrls: ['./car.component.css'],
})
export class CarComponent {
  @Input('car') car!: Car | ProxyCar;
  @Input('tripCost') tripCost!: string;

  @Output() click: EventEmitter<string> = new EventEmitter<string>();
  paymentCurrency = currency;

  isProxyCar(car: Car | ProxyCar): car is ProxyCar {
    return 'rideAmount' in car ? true : false;
  }

  emitCarId(event: Event, carId: string) {
    event.stopPropagation();
    this.click.emit(carId);
  }
}
