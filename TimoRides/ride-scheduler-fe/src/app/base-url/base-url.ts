import { environment } from 'src/environments/environment';

const baseURL: string = environment.baseURL;

export const emailNotficationURL: string = `${baseURL}/notification/email`;
export const pricingURL: string = `${baseURL}/distance/calculate-distance-amount`;
export const signUpURL: string = `${baseURL}/auth/signup`;
export const loginURL: string = `${baseURL}/auth/login`;
export const verifyEmailURL: string = `${baseURL}/auth/verify-token`;
export const refreshTokenURL: string = `${baseURL}/auth/refresh-token`;
export const bookingURL: string = `${baseURL}/bookings`;
export const carsURL: string = `${baseURL}/cars`;
export const currentCarURL: string = `${baseURL}/cars/current-car`;
export const emailVerificationURL: string = `${baseURL}/auth/send-email-verify-token`;
export const bookedRidesURL: string = `${baseURL}/bookings`;
export const fileUploadURL: string = `${baseURL}/upload-image`;
export const usersURL: string = `${baseURL}/users`;
export const userByRoleURL: string = `${baseURL}/users?role=`;
export const carProximityURL: string = `${baseURL}/cars/proximity?page=<pageCount>&pageSize=<pageSize>&state=<state>&
scheduledDate=<date>&sourceLatitude=<sourceLatitude>&sourceLongitude=<sourceLongitude>&destinationLatitude=<destinationLatitude>&destinationLongitude=<destinationLongitude>`;
export const acceptRideFromDashBoardURL: string = `${baseURL}/bookings/confirm-acceptance-status`;
export const acceptRideFromEmailURL: string = `${baseURL}/bookings/verify-acceptance`;
export const verifyCarURL: string = `${baseURL}/admin/car-status`;
export const adminSettingsURL: string = `${baseURL}/admin/settings`;
export const startEndTripURL: string = `${baseURL}/trips/confirm-otp`;
export const updateBookedRideURL: string = `${baseURL}/bookings/re-book`;
export const withdrawAmountURL: string = `${baseURL}/users/request-payment`;
export const topupWalletURL: string = `${baseURL}/users/wallet-topup`;
export const getWithdrawalsURL: string = `${baseURL}/admin/request-payments`;
export const confirmPaymentURL: string = `${baseURL}/admin/confirm-payment`;
export const bankListURL: string = `/v3/banks`;
// export const bankListURL: string = `https://api.flutterwave.com/v3/banks`;
export const ipInfoUrl: string = 'https://ipinfo.io/json';
