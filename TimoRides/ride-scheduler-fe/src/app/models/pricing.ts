export type LocationInfo = {
  pickupLocation: boolean | LocationCoordinate;
  dropOffLocation: boolean | LocationCoordinate;
};

export type LocationCoordinate = {
  lat: number;
  lng: number;
};

export type RidePricePayload = {
  sourceCoordinate: {
    latitude: number;
    longitude: number;
  };
  destinationCoordinate: {
    latitude: number;
    longitude: number;
  };
  amountPerKilo: number;
};

export type RidePriceResponse = {
  totalKilo: string;
  price: string;
  amountPerKilo: number;
  durationInTraffic: string;
};
