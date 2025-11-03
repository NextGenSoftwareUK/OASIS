import { Component, OnInit } from '@angular/core';
import { Observable } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import * as AdminSelectors from '../store/admin.selectors';
import * as AdminActions from '../store/admin.actions';
import { BookedRidesInfo } from 'src/app/models/booking-form';

@Component({
  selector: 'app-booked-trips',
  templateUrl: './booked-trips.component.html',
  styleUrls: ['./booked-trips.component.css'],
})
export class BookedTripsComponent implements OnInit {
  trips$!: Observable<BookedRidesInfo[]>;

  constructor(private store: Store<AppState>) {}

  ngOnInit(): void {
    this.store.dispatch(AdminActions.loadTrips());
    this.trips$ = this.store.select(AdminSelectors.selectAllTrips);
  }

  editTripAmount(trip: BookedRidesInfo): void {
    const newAmount = prompt('Enter new amount:', trip.tripAmount.toString());
    if (newAmount) {
      this.store.dispatch(
        AdminActions.updateTripAmount({
          tripId: trip.id,
          amount: parseFloat(newAmount),
        })
      );
    }
  }
}
