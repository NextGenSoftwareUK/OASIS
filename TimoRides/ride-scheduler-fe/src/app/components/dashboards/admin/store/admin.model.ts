import { Car } from 'src/app/models/car';
import { Profile } from 'src/app/models/user.model';

export type AdminConfig = {
  id: string;
  businessCommission: number;
  createdAt: string;
  driverWalletPercentage: number;
  pricePerKm: number;
  updatedAt: string;
  userPernalizeRate: number;
};

export type UserWithCars = Profile & {
  cars: Car[] | null;
};

export type UpdateAdminConfigRequest = Partial<AdminConfig>;

export interface WithdrawalPaymentModel {
  id: string;
  userId: string;
  wallet: number;
  amountRequested: number;
  status: string;
  role: string;
  createdAt: string;
  updatedAt: string;
}
