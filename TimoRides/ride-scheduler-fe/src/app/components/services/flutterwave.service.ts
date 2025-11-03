import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
} from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Flutterwave } from 'flutterwave-angular-v3';
import { catchError, Observable, of, tap } from 'rxjs';
import { bankListURL } from 'src/app/base-url/base-url';
import { BankResponse } from 'src/app/store/banks/bank.model';
import { environment } from 'src/environments/environment';

@Injectable({
  providedIn: 'root',
})
export class FlutterWaveService {
  private publicKey = environment.flutterKey;
  private paymentOptionsMax: string =
    'card,account,1voucher,googlepay``applepay';
  private paymentOptionsMin: string = 'card';
  private logo: string = '../../../assets/images/logo.png';

  constructor(private flutterwave: Flutterwave, private http: HttpClient) {}

  getPublicKey() {
    return this.publicKey;
  }

  getPaymentOptionMin() {
    return this.paymentOptionsMin;
  }

  getPaymentOptionMax() {
    return this.paymentOptionsMax;
  }

  closeModal(): void {
    this.flutterwave.closePaymentModal(10);
  }

  configureCustomer(...args: any) {
    const { email, phone, ...others } = args;
    return {
      customer_email: email,
      customer_phone: phone,
      ...others,
    };
  }

  setupCustomizations(title: string, description: string) {
    return {
      title,
      description,
      logo: this.logo,
    };
  }

  setupMetaConfiguration() {}

  generateReference(): string {
    let date = new Date();
    return date.getTime().toString();
  }

  getBanks(countryCode: string): Observable<BankResponse> {
    const options = {
      headers: new HttpHeaders({
        Authorization: `Bearer ${environment.secretKey}`,
      }),
    };

    return this.http
      .get<BankResponse>(`${bankListURL}/${countryCode}`, { ...options })
      .pipe(
        tap((res) => console.log('banks', res)),
        catchError((error: HttpErrorResponse) => {
          // TODO: temporarily return south afrian banks until a
          // world bank list api is integrated
          return of({
            status: 'success',
            message: 'Banks fetched successfully',
            data: [
              'South African Reserve Bank',
              'African Bank Limited',
              'Bidvest Bank Limited',
              'Capitec Bank Limited',
              'FirstRand Bank Limited',
              'Grindrod Bank Limited',
              'Imperial Bank South Africa',
              'Investec Bank Limited',
              'Nedbank',
              'Sasfin Bank Limited',
              'Teba Bank Limited',
              'Standard Bank of South Africa',
              'Absa Bank Limited',
              'Albaraka Bank Limited',
              'Habib Bank Overseas Bank Limited',
              'Habib Bank AG Zurich',
              'Mercantile Bank Limited',
              'South African Bank of Athens Limited',
              'Bank of Baroda',
              'Bank of China',
              'Bank of Taiwan',
              'Calyon Corporate and Investment Bank',
              'China Construction Bank Corporation',
              'Citibank N.A.',
              'Deutsche Bank AG',
              'JPMorgan Chase Bank',
              'Société Générale',
              'Standard Chartered Bank',
              'State Bank of India',
              'Hongkong and Shanghai Banking Corporation',
              'Royal Bank of Scotland',
              'GBS Mutual Bank',
              'VBS Mutual Bank',
              'Development Bank of Southern Africa',
              'Land and Agricultural Development Bank of South Africa',
              'Postbank',
            ].map((item: string, index) => ({
              id: index + 1,
              name: item,
              code: (index + 1).toString(),
            })),
          });
        })
      );
  }
}
