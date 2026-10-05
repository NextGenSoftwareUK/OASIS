import { RidePriceResponse } from './pricing';

export type BookingPayload = {
  car: string;
  fullName: string;
  phoneNumber: string;
  bookingType: 'passengers' | 'goods'; //'Timo Go (passengers)' | 'Timo Load (goods)';
  email: string;
  tripAmount: string;
  tripDuration: string;
  tripDistance: string;
  state: string;
  isCash: boolean;
  departureTime: string;
  sourceLocation: MapLocation;
  destinationLocation: MapLocation;
  trxId?: string | null;
  trxRef?: string | null;
  passengers: string;
};

export enum RideType {
  TimoGo = 'Timo Go (passengers)',
  TimoLoad = 'Timo Load (goods)',
}

export type BookingRequest = {
  car: string;
  fullName: string;
  phoneNumber: string;
  bookingType: string;
  email: string;
  passengers: string;
  tripInfo: RidePriceResponse;
  isCash: boolean;
  departureTime: string;
  sourceLocation: MapLocation;
  destinationLocation: MapLocation;
  trxId?: string | null;
  trxRef?: string | null;
};

export type MapLocation = {
  address: string;
  latitude: number;
  longitude: number;
};

export type BookingResponse = {
  id: string;
};

export type AcceptRideRequest = {
  token: string;
  isAccepted: boolean;
  trxId: string | null;
};

export type AcceptRideResponse = {
  message: string;
};

export type AcceptRideFromDashboardRequest = {
  bookingId: string;
  isAccepted: boolean;
};

export type BookedRides = {
  id: string;
  user: string;
  car: string;
  departureTime: string;
  tripInfo: RidePriceResponse;
  isCash: boolean;
  isDriverAccept: boolean;
  trxId: string;
  destinationLocation: {
    address: string;
    latitude: {
      $numberDecimal: string;
    };
    longitude: {
      $numberDecimal: string;
    };
  };
  sourceLocation: {
    address: string;
    latitude: {
      $numberDecimal: string;
    };
    longitude: {
      $numberDecimal: string;
    };
  };
  createdAt: string;
  updatedAt: string;
  // __v: number;
};

export type BookedRidesType = {
  bookings: BookedRidesInfo[];
};

export type BookedRidesInfo = {
  numberOfPassengers?: number;
  user: string;
  car: {
    id: string;
    driver: {
      fullName: string;
      profileImg: string;
      wallet: number;
      email: string;
      phone: string;
      password: string;
      homeAddress: string;
      isVerified: boolean;
      role: string;
      isDisable: boolean;
      nextOfKinFullName: string;
      nextOfKinEmailAddress: string;
      nextOfKinRelationship: string;
      nextOfKinPhoneCell: string;
      nextOfKinPhoneHome: string;
      nextOfKinHomeAddress: string;
      type: string;
      workPhone: string;
      homePhone: string;
      identityType: string;
      identityNumber: string;
      nextOfKinFullNameAlt: string;
      nextOfKinEmailAddressAlt: string;
      nextOfKinRelationshipAlt: string;
      nextOfKinPhoneCellAlt: string;
      nextOfKinPhoneHomeAlt: string;
      nextOfKinHomeAddressAlt: string;
      nameOfAccountHolder: string;
      nameOfAccountContactPerson: string;
      bankName: string;
      bankBranchName: string;
      bankBranchCode: string;
      bankAccountNumber: string;
      bankAccountType: string;
      bankAccountContactPerson: string;
      bankAuthorizedSignature: string;
      bankDate: string;
      bankNameInPrint: string;
      driverLicenseImg: string;
      driverIdentityCardImg: string;
      comprehensiveInsuranceImg: string;
      operatorCardImg: string;
      inspectionCertImg: string;
      professionalDriverPermitImg: string;
      commissionConsent: boolean;
      commissionConsentAuthorizedSignatureImg: string;
      commissionConsentDate: string;
      commissionConsentNameInPrint: string;
      tcConsent: boolean;
      tcFullName: string;
      tcAuthorizedSignatureImg: string;
      tcDate: string;
      tcPlace: string;
      location: {
        latitude: number;
        longitude: number;
      };
      createdAt: string;
      updatedAt: string;
      completedRides: number;
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
    vehicleModel: string;
    vehicleColor: string;
  };
  departureTime: string;
  tripAmount: string;
  bookingType: string;
  passengers?: number;
  fullName: string;
  phoneNumber: string;
  isCash: boolean;
  email: string;
  isDriverAccept: boolean;
  trxId: null;
  trxRef?: string;
  status: string;
  destinationLocation: {
    address: string;
    latitude: number;
    longitude: number;
  };
  sourceLocation: {
    address: string;
    latitude: number;
    longitude: number;
  };
  createdAt: string;
  updatedAt: string;
  state?: string;
  tripDistance?: string;
  tripDuration?: string;
  id: string;
};

export interface TripResponse {
  message: string;
}

export interface SaveRebooking {
  sourceLat: string;
  sourceLong: string;
  destLat: string;
  destLong: string;
  state: string;
  scheduledDate: string;
  bookingId: string;
}

export interface UpdateBookedRideRequest {
  car: string;
  bookingId: string;
}

export interface UpdateBookedRideResponse {
  message: string;
}
