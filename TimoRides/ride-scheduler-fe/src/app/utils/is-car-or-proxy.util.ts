import { Car, ProxyCar } from '../models/car';

export const isProxyCar = (car: Car | ProxyCar): car is ProxyCar => {
  return 'rideAmount' in car ? true : false;
};
