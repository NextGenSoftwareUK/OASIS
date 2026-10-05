import { AdminState } from './components/dashboards/admin/store/admin.reducer';
import { BankState } from './store/banks/bank.reducer';
import { BookedRideState } from './store/booked-rides/booked-rides.reducer';
import { CarState } from './store/cars/car.reducer';
import { BookingState } from './store/ride-booking/state/ride-schedule.reducer';
import { TransactionState } from './store/transaction/transaction.reducer';
import { ProfileState } from './store/user/user.reducer';
import { WithdrawalState } from './store/withdrawal/withdrawal.reducer';

export interface AppState {
  profile: ProfileState;
  booking: BookingState;
  bookedRides: BookedRideState;
  car: CarState;
  admin: AdminState;
  withdrawal: WithdrawalState;
  transaction: TransactionState;
  bank: BankState;
}
