# OASIS Avatar SSO SDK - Angular

Complete Angular integration for OASIS Avatar Single Sign-On authentication.

## Installation

```bash
npm install @oasis/avatar-sso-angular
```

## Quick Start

### 1. Import the Module

```typescript
import { NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { OasisAvatarSSOModule } from '@oasis/avatar-sso-angular';
import { AppComponent } from './app.component';

@NgModule({
  declarations: [AppComponent],
  imports: [
    BrowserModule,
    OasisAvatarSSOModule.forRoot({
      apiUrl: 'https://api.oasis.network',
      provider: 'Auto'
    })
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule { }
```

### 2. Use in Components

```typescript
import { Component } from '@angular/core';
import { OasisAvatarSSOService } from '@oasis/avatar-sso-angular';

@Component({
  selector: 'app-login',
  template: `
    <div class="login-container">
      <h2>OASIS Avatar Login</h2>
      <input [(ngModel)]="username" placeholder="Username" />
      <input [(ngModel)]="password" type="password" placeholder="Password" />
      <select [(ngModel)]="provider">
        <option value="Auto">Auto</option>
        <option value="MongoDBOASIS">MongoDB</option>
        <option value="EthereumOASIS">Ethereum</option>
      </select>
      <button (click)="login()">Beam In</button>
    </div>
  `
})
export class LoginComponent {
  username = '';
  password = '';
  provider = 'Auto';

  constructor(private ssoService: OasisAvatarSSOService) {}

  async login() {
    try {
      const result = await this.ssoService.login(
        this.username,
        this.password,
        this.provider
      );
      console.log('Login successful:', result);
    } catch (error) {
      console.error('Login failed:', error);
    }
  }
}
```

### 3. Use Auth Guard

```typescript
import { Injectable } from '@angular/core';
import { CanActivate, Router } from '@angular/router';
import { OasisAvatarSSOService } from '@oasis/avatar-sso-angular';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private ssoService: OasisAvatarSSOService,
    private router: Router
  ) {}

  async canActivate(): Promise<boolean> {
    const isAuthenticated = await this.ssoService.isAuthenticated();
    if (!isAuthenticated) {
      this.router.navigate(['/login']);
      return false;
    }
    return true;
  }
}
```

## Features

✨ **Full Angular Integration** - Built specifically for Angular 15+
🔐 **Type-Safe** - Complete TypeScript support
🚀 **Reactive** - RxJS observables for auth state
🛡️ **Route Guards** - Built-in authentication guards
🎨 **Customizable** - Flexible configuration options

## API Reference

### OasisAvatarSSOService

#### Methods

- `login(username: string, password: string, provider?: string): Promise<AuthResult>`
- `logout(): Promise<void>`
- `isAuthenticated(): Promise<boolean>`
- `getCurrentUser(): Promise<User | null>`
- `refreshToken(): Promise<void>`

#### Observables

- `authState$: Observable<AuthState>` - Current authentication state
- `user$: Observable<User | null>` - Current user data

## License

MIT © OASIS Team

