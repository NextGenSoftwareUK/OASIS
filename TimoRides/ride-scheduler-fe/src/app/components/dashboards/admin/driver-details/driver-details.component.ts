import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { Profile } from 'src/app/models/user.model';
import * as AdminSelectors from '../store/admin.selectors';
import * as AdminActions from '../store/admin.actions';
import { CarById } from 'src/app/models/car';

@Component({
  selector: 'app-driver-details',
  templateUrl: './driver-details.component.html',
  styleUrls: ['./driver-details.component.css'],
})
export class DriverDetailsComponent implements OnInit {
  driver$!: Observable<Profile | undefined>;
  car$!: Observable<CarById | null>;

  constructor(private store: Store<AppState>, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    if (userId) {
      this.store.dispatch(AdminActions.loadUser({ id: userId }));
      this.driver$ = this.store.select(AdminSelectors.selectUserById(userId));

      this.driver$.subscribe((res) => {
        if (res) {
          this.store.dispatch(AdminActions.loadUser({ id: userId }));

          this.car$ = this.store.select(
            AdminSelectors.selectCarByDriverId(res.id)
          );
        }
      });
    }
  }

  verifyDriver(driver: Profile): void {
    this.store.dispatch(AdminActions.verifyDriver({ driver }));
  }

  invalidateDriver(driver: Profile): void {
    this.store.dispatch(AdminActions.invalidateDriver({ driver }));
  }
}
