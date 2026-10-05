import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ipInfoUrl } from 'src/app/base-url/base-url';

@Injectable({
  providedIn: 'root',
})
export class GeolocationService {
  constructor(private http: HttpClient) {}

  getGeolocation(): Observable<any> {
    return this.http.get<any>(ipInfoUrl);
  }
}
