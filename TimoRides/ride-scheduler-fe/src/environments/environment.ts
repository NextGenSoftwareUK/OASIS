export const environment = {
  production: false,
  googleMapsApiKey: (process.env as any)['GOOGLE_MAPS_API_KEY'],
  baseURL: (process.env as any)['BASE_URL'],
  flutterKey: (process.env as any)['FLUTTER_KEY'],
  secretKey: (process.env as any)['SECRET_KEY'],
  mapsURL: (process.env as any)['MAPS_URL'],
};
