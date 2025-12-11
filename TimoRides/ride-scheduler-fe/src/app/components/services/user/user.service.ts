import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, map, switchMap, of, tap, catchError } from 'rxjs';
import { Store } from '@ngrx/store';
import { AppState } from 'src/app/app.state';
import { Router } from '@angular/router';
import { usersURL } from 'src/app/base-url/base-url';
import {
  Profile,
  UserProfileResponse,
  UserUpdateResponse,
} from 'src/app/models/user.model';
import {
  selectProfileId,
  selectUserProfile,
} from 'src/app/store/user/user.selectors';
import { updateUserProfile } from 'src/app/store/user/user.actions';

@Injectable({
  providedIn: 'root',
})
export class UserService {
  constructor(
    private http: HttpClient,
    private store: Store<AppState>,
    private router: Router
  ) {}

  // getUserProfile(): Observable<Profile | null> {
  //   return this.store.select(selectUserProfile)
  //   .pipe(
  //     switchMap((profile) => {
  //       if (profile) {
  //         return of(profile);
  //       } else {
  //         // Redirect to login if profile ID is not found
  //         // this.router.navigate(['/login'], { queryParams: { returnUrl } });
  //         return of(null);
  //       }
  //     })
  //   );
  // }

  getUserProfile(id: string): Observable<Profile | null> {
    return this.http.get<Profile>(`${usersURL}/${id}`).pipe(
      tap((updatedProfile) => {
        // Dispatch an action to update the store with the new profile
        this.store.dispatch(updateUserProfile({ profile: updatedProfile }));
      }),
      switchMap(() => this.store.select(selectUserProfile)),
      catchError((error) => {
        console.error('Error fetching user profile', error);
        return of(null);
      })
    );
  }

  getUserById(id: string) {
    const url = `${usersURL}/${id}`;
    return this.http
      .get<UserProfileResponse>(url)
      .pipe(map((data) => data.data));
  }

  updateUserProfile(payload: any): Observable<UserUpdateResponse | null> {
    return this.store.select(selectProfileId).pipe(
      switchMap((profileId) => {
        if (profileId) {
          const url = `${usersURL}/${profileId}`;
          return this.http
            .put<UserUpdateResponse>(url, payload)
            .pipe(map((data) => data));
        } else {
          const returnUrl = this.router.url;
          // Redirect to login if profile ID is not found
          this.router.navigate(['/login'], { queryParams: { returnUrl } });
          return of(null);
        }
      })
    );
  }
}
