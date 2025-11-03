import { NgModule, isDevMode } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { HTTP_INTERCEPTORS, HttpClientModule } from '@angular/common/http';
import { AppComponent } from './app.component';
import { StarComponent } from './components/rating-tool/star/star.component';
import { RatingWidgetComponent } from './components/rating-tool/rating-widget/rating-widget.component';
import { CarComponent } from './components/car/car.component';
import { CarListComponent } from './components/ride-scheduler/car-list/car-list.component';
import { CarDetailsComponent } from './components/ride-scheduler/car-details/car-details.component';
import { DateSelectionInputComponent } from './components/date-selection-input/date-selection-input.component';
import { TextInputComponent } from './components/text-input/text-input.component';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { DropdownComponent } from './components/dropdown/dropdown.component';
import { RideFormComponent } from './components/ride-scheduler/ride-form/ride-form.component';
import { LocationAutocompleteComponent } from './components/location-autocomplete/location-autocomplete.component';
import { GooglePlaceModule } from 'ngx-google-places-autocomplete';
import { EntryComponent } from './components/entry/entry.component';
import { NicePipe } from './pipes/nice.pipe';
import { CheckboxComponent } from './components/checkbox/checkbox.component';
import { NewCardFormComponent } from './components/payment/new-card-form/new-card-form.component';
import { ReactiveFormsModule } from '@angular/forms';
import { CardTypeListComponent } from './components/payment/card-type-list/card-type-list.component';
import { AngularMaterialModule } from './components/shared/angular-material.module';
import { CardTypeComponent } from './components/payment/card-type/card-type.component';
import { PaymentComponent } from './components/payment/payment-page/payment.component';
import { SavedCardComponent } from './components/payment/saved-card/saved-card.component';
import { RideSummaryComponent } from './components/ride-scheduler/summary/ride-summary/ride-summary.component';
import { SummaryComponent } from './components/ride-scheduler/summary/summary/summary.component';
import { CarDetailsSummaryComponent } from './components/ride-scheduler/summary/car-details-summary/car-details-summary.component';
import { DriverDetailsSummaryComponent } from './components/ride-scheduler/summary/driver-details-summary/driver-details-summary.component';
import { HiddenPipe } from './pipes/hidden.pipe';
import { ActionReducerMap, MetaReducer, StoreModule } from '@ngrx/store';
import { EffectsModule } from '@ngrx/effects';
import { StoreRouterConnectingModule } from '@ngrx/router-store';
import { StoreDevtoolsModule } from '@ngrx/store-devtools';
import { UserEffects } from './store/user/user.effects';
import { UserService } from './components/services/user/user.service';
import { DatePipe } from '@angular/common';
import { PaymentCompleteComponent } from './components/payment/payment-complete/payment-complete.component';
import { DashboardsModule } from './components/dashboards/dashboards.module';
import { DashboardsComponent } from './components/dashboards/dashboards.component';
import { DriverComponent } from './components/dashboards/driver/driver.component';
import { SignUpComponent } from './components/sign-up/sign-up.component';
import { LoginComponent } from './components/login/login.component';
import { EmailVerificationComponent } from './components/email-verification/email-verification.component';
import { UserProfileComponent } from './components/dashboards/user/profile/user-profile.component';
import { AuthService } from './components/services/auth/auth.service';
import { ErrorInterceptor } from './interceptors/error.interceptor';
import { TokenInterceptor } from './interceptors/token.interceptor';
import { DriverProfileComponent } from './components/dashboards/driver/profile/driver-profile.component';
import { AuthGuard } from './guards/auth.guard';
import {
  SESSION_STORAGE_SYNC_REDUCER,
  sessionStorageSyncReducerFactory,
  rehydrateState,
} from './session-storage-sync.reducer';
import { AppState } from './app.state';
import { RideScheduleReducer } from './store/ride-booking/state/ride-schedule.reducer';
import { RideScheduleEffects } from './store/ride-booking/state/ride-schedule.effects';
import { carReducer } from './store/cars/car.reducer';
import { CarEffects } from './store/cars/car.effects';
import { NavbarComponent } from './components/shared/navbar/navbar.component';
import { bookedRideReducer } from './store/booked-rides/booked-rides.reducer';
import { BookedRideEffects } from './store/booked-rides/booked-rides.effects';
import { adminReducer } from './components/dashboards/admin/store/admin.reducer';
import { AdminEffects } from './components/dashboards/admin/store/admin.effects';
import { FlutterwaveModule } from 'flutterwave-angular-v3';
import { EncryptionService } from './components/services/encryption/encryption.service';
import { SharedModule } from './components/shared/shared.module';
import { LoadingInterceptor } from './interceptors/loading.interceptor';
import { UserRideFormComponent } from './components/ride-scheduler/user-form/user-form.component';
import { FaqComponent } from './components/faq/faq.component';
import { SvgComponent } from './components/svg/svg.component';
import { withdrawalReducer } from './store/withdrawal/withdrawal.reducer';
import { WithdrawalEffects } from './store/withdrawal/withdrawal.effects';
import { transactionReducer } from './store/transaction/transaction.reducer';
import { TransactionEffects } from './store/transaction/transaction.effects';
import { userReducer } from './store/user/user.reducer';
import { bankReducer } from './store/banks/bank.reducer';
import { BankEffects } from './store/banks/bank.effect';

export const reducers: ActionReducerMap<AppState> = {
  profile: userReducer,
  booking: RideScheduleReducer,
  bookedRides: bookedRideReducer,
  car: carReducer,
  admin: adminReducer,
  withdrawal: withdrawalReducer,
  transaction: transactionReducer,
  bank: bankReducer,
};

export function metaReducerFactory(
  encryptionService: EncryptionService
): MetaReducer<AppState>[] {
  return [sessionStorageSyncReducerFactory(encryptionService)];
}

@NgModule({
  declarations: [
    AppComponent,
    StarComponent,
    RatingWidgetComponent,
    CarComponent,
    CarListComponent,
    CarDetailsComponent,
    DateSelectionInputComponent,
    TextInputComponent,
    DropdownComponent,
    SvgComponent,
    EntryComponent,
    RideFormComponent,
    UserRideFormComponent,
    LocationAutocompleteComponent,
    NicePipe,
    CheckboxComponent,
    NewCardFormComponent,
    CardTypeListComponent,
    CardTypeComponent,
    PaymentComponent,
    SavedCardComponent,
    RideSummaryComponent,
    SummaryComponent,
    CarDetailsSummaryComponent,
    DriverDetailsSummaryComponent,
    HiddenPipe,
    PaymentCompleteComponent,
    DashboardsComponent,
    DriverComponent,
    SignUpComponent,
    LoginComponent,
    EmailVerificationComponent,
    UserProfileComponent,
    DriverProfileComponent,
    NavbarComponent,
    FaqComponent,
  ],
  imports: [
    BrowserModule,
    SharedModule,
    AppRoutingModule,
    HttpClientModule,
    BrowserAnimationsModule,
    GooglePlaceModule,
    ReactiveFormsModule,
    AngularMaterialModule,
    DashboardsModule,
    FlutterwaveModule,
    StoreModule.forRoot(reducers, {
      metaReducers: metaReducerFactory(new EncryptionService()),
      initialState: rehydrateState(new EncryptionService()),
    }),
    EffectsModule.forRoot([
      UserEffects,
      RideScheduleEffects,
      BookedRideEffects,
      CarEffects,
      AdminEffects,
      WithdrawalEffects,
      TransactionEffects,
      BankEffects,
    ]),
    StoreRouterConnectingModule.forRoot(),
    StoreDevtoolsModule.instrument({ maxAge: 25, logOnly: !isDevMode() }),
  ],
  providers: [
    DatePipe,
    AuthService,
    UserService,
    { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: LoadingInterceptor, multi: true },
    AuthGuard,
    EncryptionService,
    {
      provide: SESSION_STORAGE_SYNC_REDUCER,
      useFactory: sessionStorageSyncReducerFactory,
      deps: [EncryptionService],
    },
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
