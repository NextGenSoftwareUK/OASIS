export interface TripStatusRequest {
  bookingId: string;
  otpCode: string;
  tripMode: TRIP_MODE_ENUMS.START | TRIP_MODE_ENUMS.END;
}

export enum TRIP_MODE_ENUMS {
  START = 'start',
  END = 'end',
}

export interface TripStatusResponse {
  id: string;
  bookingId: string;
  driverId: string;
  startTrip: {
    code: string;
    isActived: true;
    codeDate: string;
  };
  endTrip: {
    code: string;
    isActived: false;
    codeDate: string;
  };
  createdAt: string;
  updatedAt: string;
}
