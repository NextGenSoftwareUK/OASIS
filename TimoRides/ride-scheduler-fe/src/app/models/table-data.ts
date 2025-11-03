import { BookedRides } from './booking-form';

export interface TableData extends BookedRides {
  status:
    | RIDE_STATUS.ACCEPTED
    | RIDE_STATUS.COMPLETED
    | RIDE_STATUS.PENDING
    | RIDE_STATUS.CANCELLED
    | RIDE_STATUS.STARTED;
  viewDetails: (index: number) => void;
}

export enum RIDE_STATUS {
  ACCEPTED = 'accepted',
  COMPLETED = 'completed',
  PENDING = 'pending',
  CANCELLED = 'cancelled',
  STARTED = 'started',
}
