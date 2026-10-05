import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

export interface OasisConfig {
  apiUrl: string;
  provider?: string;
}

export interface AuthResult {
  success: boolean;
  token?: string;
  avatar?: any;
  message?: string;
}

export interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  avatarUrl?: string;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class OasisAvatarSSOService {
  private apiUrl: string;
  private provider: string;
  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    user: null,
    token: null
  });

  public authState$: Observable<AuthState> = this.authStateSubject.asObservable();
  public user$: Observable<User | null> = this.authState$.pipe(map(state => state.user));

  constructor(private http: HttpClient) {
    this.apiUrl = 'https://api.oasis.network';
    this.provider = 'Auto';
    this.loadAuthState();
  }

  configure(config: OasisConfig): void {
    this.apiUrl = config.apiUrl;
    this.provider = config.provider || 'Auto';
  }

  async login(username: string, password: string, provider?: string): Promise<AuthResult> {
    const selectedProvider = provider || this.provider;
    
    try {
      const response = await this.http.post<AuthResult>(`${this.apiUrl}/avatar/authenticate`, {
        username,
        password,
        provider: selectedProvider
      }).toPromise();

      if (response && response.success && response.token) {
        this.setAuthState({
          isAuthenticated: true,
          user: this.mapAvatarToUser(response.avatar),
          token: response.token
        });
        localStorage.setItem('oasis_token', response.token);
        localStorage.setItem('oasis_user', JSON.stringify(response.avatar));
      }

      return response!;
    } catch (error) {
      console.error('Login error:', error);
      throw error;
    }
  }

  async logout(): Promise<void> {
    try {
      const token = this.authStateSubject.value.token;
      if (token) {
        await this.http.post(`${this.apiUrl}/avatar/logout`, {}, {
          headers: new HttpHeaders({ Authorization: `Bearer ${token}` })
        }).toPromise();
      }
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      this.clearAuthState();
    }
  }

  async isAuthenticated(): Promise<boolean> {
    const token = this.authStateSubject.value.token || localStorage.getItem('oasis_token');
    if (!token) return false;

    try {
      // Verify token is still valid
      await this.http.get(`${this.apiUrl}/avatar/verify`, {
        headers: new HttpHeaders({ Authorization: `Bearer ${token}` })
      }).toPromise();
      return true;
    } catch {
      this.clearAuthState();
      return false;
    }
  }

  async getCurrentUser(): Promise<User | null> {
    return this.authStateSubject.value.user;
  }

  async refreshToken(): Promise<void> {
    const token = this.authStateSubject.value.token;
    if (!token) throw new Error('No token available');

    try {
      const response = await this.http.post<{ token: string }>(`${this.apiUrl}/avatar/refresh`, {}, {
        headers: new HttpHeaders({ Authorization: `Bearer ${token}` })
      }).toPromise();

      if (response && response.token) {
        const currentState = this.authStateSubject.value;
        this.setAuthState({ ...currentState, token: response.token });
        localStorage.setItem('oasis_token', response.token);
      }
    } catch (error) {
      console.error('Token refresh error:', error);
      this.clearAuthState();
      throw error;
    }
  }

  private loadAuthState(): void {
    const token = localStorage.getItem('oasis_token');
    const userJson = localStorage.getItem('oasis_user');

    if (token && userJson) {
      try {
        const avatar = JSON.parse(userJson);
        this.setAuthState({
          isAuthenticated: true,
          user: this.mapAvatarToUser(avatar),
          token
        });
      } catch {
        this.clearAuthState();
      }
    }
  }

  private setAuthState(state: AuthState): void {
    this.authStateSubject.next(state);
  }

  private clearAuthState(): void {
    localStorage.removeItem('oasis_token');
    localStorage.removeItem('oasis_user');
    this.authStateSubject.next({
      isAuthenticated: false,
      user: null,
      token: null
    });
  }

  private mapAvatarToUser(avatar: any): User {
    return {
      id: avatar.id || avatar.avatarId,
      username: avatar.username,
      email: avatar.email,
      firstName: avatar.firstName || '',
      lastName: avatar.lastName || '',
      avatarUrl: avatar.image || avatar.avatarUrl
    };
  }
}

