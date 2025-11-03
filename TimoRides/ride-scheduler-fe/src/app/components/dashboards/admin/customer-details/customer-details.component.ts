import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { Profile } from 'src/app/models/user.model';
import * as AdminSelectors from '../store/admin.selectors';
import * as AdminActions from '../store/admin.actions';

@Component({
  selector: 'app-customer-details',
  templateUrl: './customer-details.component.html',
  styleUrls: ['./customer-details.component.css'],
})
export class CustomerDetailsComponent implements OnInit {
  customer$!: Observable<Profile | undefined>;

  constructor(private store: Store<AppState>, private route: ActivatedRoute) {}

  ngOnInit(): void {
    const userId = this.route.snapshot.paramMap.get('id');
    if (userId) {
      this.store.dispatch(AdminActions.loadUser({ id: userId }));
      this.customer$ = this.store.select(AdminSelectors.selectUserById(userId));
    }
  }
}
