import { Component, ViewChild, AfterViewInit, OnInit } from '@angular/core';
import { LoadingComponent } from './components/shared/loading/loading.component';
import { LoadingService } from './components/shared/loading/loading.service';
import { Store } from '@ngrx/store';
import { BankState } from './store/banks/bank.reducer';
import * as BankActions from './store/banks/bank.actions';
import { GeolocationService } from './components/services/geolocation.service';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css'],
})
export class AppComponent implements OnInit, AfterViewInit {
  @ViewChild(LoadingComponent) loadingComponent!: LoadingComponent;
  title = 'Timo Rides';

  constructor(
    private loadingService: LoadingService,
    private store: Store<{ bank: BankState }>,
    private geolocationService: GeolocationService
  ) {}

  ngOnInit(): void {
    this.geolocationService.getGeolocation().subscribe({
      next: (data) => {
        const countryCode = data.country;
        this.store.dispatch(BankActions.loadBanks({ countryCode }));
      },
      error: (error) => {
        console.error('Error fetching geolocation:', error);
      },
    });
  }

  ngAfterViewInit() {
    this.loadingService.registerLoadingComponent(this.loadingComponent);
  }
}
