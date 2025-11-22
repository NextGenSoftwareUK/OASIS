import { Component, HostListener, OnInit } from '@angular/core';
import { Store, select } from '@ngrx/store';
import { Router } from '@angular/router';
import { Observable } from 'rxjs';
import { AppState } from 'src/app/app.state';
import { Profile } from 'src/app/models/user.model';
import { map } from 'rxjs/operators';
import * as fromProfile from 'src/app/store/user/user.actions';
import * as fromBookingPage from 'src/app/store/ride-booking/state/actions/ride-schedule-page.action';
import * as fromCar from 'src/app/store/cars/car.actions';

@Component({
  selector: 'app-navbar',
  templateUrl: './navbar.component.html',
  styleUrls: ['./navbar.component.css'],
})
export class NavbarComponent implements OnInit {
  isLoggedIn$: Observable<boolean>;
  user$: Observable<Profile | null>;
  isNavbarOpen: boolean = false;
  exclusionRoutes: string[] = [
    'login',
    'register',
    'forgot-password',
    'reset-password',
    'verify',
  ];

  constructor(private store: Store<AppState>, private router: Router) {
    this.user$ = this.store.pipe(
      select('profile'),
      map((state) => state.profile)
    );
    this.isLoggedIn$ = this.user$.pipe(map((user) => !!user));
  }

  ngOnInit(): void {}

  @HostListener('window:scroll', [])
  onWindowScroll() {
    const navbar = document.getElementById('navbar');
    if (window.scrollY > 20) {
      navbar?.classList.add('scrolled');
    } else {
      navbar?.classList.remove('scrolled');
    }
  }

  isCurrentPage(route: string): boolean {
    return this.router.url.includes(route);
  }

  toggleNavbar() {
    this.isNavbarOpen = !this.isNavbarOpen;
  }

  viewProfile() {
    this.toggleNavbar();
    this.user$
      .subscribe((user) => {
        let url = '';
        if (user) {
          url =
            user.role === 'admin' || user.role === 'user'
              ? `/users/${user.id}`
              : `/drivers/${user.id}`;
        }
        this.router.navigate([url]);
      })
      .unsubscribe();
  }

  viewDashboard() {
    this.toggleNavbar();
    this.user$
      .subscribe((user) => {
        let url = 'dashboards/';
        switch (user?.role) {
          case 'user':
            url += 'user';
            break;
          case 'driver':
            url += 'driver';
            break;
          case 'admin':
            url += 'admin';
            break;
        }
        this.router.navigate([url]);
      })
      .unsubscribe();
  }

  logout() {
    this.toggleNavbar();
    sessionStorage.clear();
    this.store.dispatch(fromProfile.logout());
    this.store.dispatch(fromBookingPage.clearBookingState());
    this.store.dispatch(fromCar.clearCarState());
    // refresh page using window object
    window.location.reload();
    this.router.navigate(['/ride-schedule']);
  }
}
