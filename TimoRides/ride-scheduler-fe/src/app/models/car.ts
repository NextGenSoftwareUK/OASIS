import { BookingRequest, SaveRebooking } from './booking-form';
import { Profile } from './user.model';

export type Car = {
  id: string;
  driver: string;
  vehicleRegNumber: string;
  vehicleModelYear: string;
  vehicleMake: string;
  vehicleModel: string;
  engineNumber: string;
  vehicleColor: string;
  insuranceBroker: string;
  insurancePolicyNumber: string;
  imagePath: string;
  altImagePath: string;
  interiorImagePath: string;
  vehicleAddress: string;
  state: string;
  location: {
    latitude: number;
    longitude: number;
  };
  rating: number;
  isVerify: boolean;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
};

export type CarById = {
  id: string;
  driver: string;
  state: string;
  rating: 1;
  plateNumber: string;
  imagePath: string;
  altImagePath: string;
  interiorImagePath: string;
  engineNumber: string;
  insuranceBroker: string;
  insurancePolicyNumber: string;
  vehicleRegNumber: string;
  isVerify: boolean;
  isActive: boolean;
  location: {
    latitude: number;
    longitude: number;
  };
  createdAt: string;
  updatedAt: string;
  vehicleModel: string;
  vehicleColor: string;
};

export type CarRequest = {
  vehicleRegNumber: string;
  vehicleModelYear: string;
  vehicleMake: string;
  vehicleModel: string;
  engineNumber: string;
  vehicleColor: string;
  insuranceBroker: string;
  insurancePolicyNumber: string;
  imagePath: string;
  altImagePath: string;
  interiorImagePath: string;
  vehicleAddress: string;
  state: string;
  location: {
    latitude: number;
    longitude: number;
  };
};

export type CarProximityRequest = {
  page: string;
  pageSize: string;
  state: string;
  scheduledDate: string;
  sourceLatitude: string;
  sourceLongitude: string;
  destinationLatitude: string;
  destinationLongitude: string;
};

export type CarProximityResponse = {
  cars: ProxyCar[];
  currentPage: number;
  totalPages: number;
};

export type ProxyCar = {
  id: string;
  driver: {
    wallet: number;
    type: string;
    id: string;
  };
  state: string;
  rating: number;
  plateNumber: string;
  imagePath: string;
  altImagePath: string;
  interiorImagePath: string;
  engineNumber: string;
  insuranceBroker: string;
  insurancePolicyNumber: string;
  vehicleRegNumber: string;
  isVerify: boolean;
  isActive: boolean;
  location: {
    latitude: number;
    longitude: number;
  };
  createdAt: string;
  updatedAt: string;
  vehicleModelYear: string;
  vehicleModel: string;
  vehicleColor: string;
  duration: string;
  distance: string;
  rideAmount: string;
  durationAway: string;
  distanceAway: string;
  vehicleMake?: string;
  vehicleAddress?: string;
  isOffline?: boolean;
};

export type CarInfo = {
  car: Car | ProxyCar;
  driver: Profile;
  rideSchedule?: BookingRequest;
  rebooking?: SaveRebooking;
};

export type VerifyCar = {
  isVerify?: boolean;
  isActive?: boolean;
  carId?: string;
};
