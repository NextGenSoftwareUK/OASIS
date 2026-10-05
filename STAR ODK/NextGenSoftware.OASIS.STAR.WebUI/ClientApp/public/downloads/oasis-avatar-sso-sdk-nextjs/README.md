# OASIS Avatar SSO SDK - Next.js

Complete Next.js integration for OASIS Avatar Single Sign-On with App Router and Pages Router support.

## Installation

```bash
npm install @oasis/avatar-sso-nextjs
```

## Quick Start (App Router)

### 1. Create Provider Component

```tsx
// app/providers.tsx
'use client';

import { OasisSSOProvider } from '@oasis/avatar-sso-nextjs';

export function Providers({ children }: { children: React.ReactNode }) {
  return (
    <OasisSSOProvider
      apiUrl={process.env.NEXT_PUBLIC_OASIS_API_URL || 'https://api.oasis.network'}
      provider="Auto"
    >
      {children}
    </OasisSSOProvider>
  );
}
```

### 2. Wrap Root Layout

```tsx
// app/layout.tsx
import { Providers } from './providers';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
```

### 3. Use in Components

```tsx
// app/login/page.tsx
'use client';

import { useState } from 'react';
import { useOasisSSO } from '@oasis/avatar-sso-nextjs';
import { useRouter } from 'next/navigation';

export default function LoginPage() {
  const { login, isAuthenticated, user } = useOasisSSO();
  const router = useRouter();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [provider, setProvider] = useState('Auto');

  const handleLogin = async () => {
    try {
      await login(username, password, provider);
      router.push('/dashboard');
    } catch (error) {
      console.error('Login failed:', error);
    }
  };

  if (isAuthenticated) {
    return (
      <div>
        <h2>Welcome, {user?.username}!</h2>
      </div>
    );
  }

  return (
    <div>
      <h1>Login to OASIS</h1>
      <input value={username} onChange={(e) => setUsername(e.target.value)} placeholder="Username" />
      <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" />
      <select value={provider} onChange={(e) => setProvider(e.target.value)}>
        <option value="Auto">Auto</option>
        <option value="MongoDBOASIS">MongoDB</option>
        <option value="EthereumOASIS">Ethereum</option>
      </select>
      <button onClick={handleLogin}>Beam In</button>
    </div>
  );
}
```

### 4. Protected Routes with Middleware

```typescript
// middleware.ts
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const token = request.cookies.get('oasis_token')?.value;
  
  if (!token && request.nextUrl.pathname.startsWith('/dashboard')) {
    return NextResponse.redirect(new URL('/login', request.url));
  }
  
  return NextResponse.next();
}

export const config = {
  matcher: ['/dashboard/:path*', '/profile/:path*']
};
```

## Server-Side Authentication

### API Route Handler

```typescript
// app/api/auth/login/route.ts
import { NextRequest, NextResponse } from 'next/server';

export async function POST(request: NextRequest) {
  const { username, password, provider } = await request.json();
  
  const response = await fetch('https://api.oasis.network/avatar/authenticate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password, provider })
  });
  
  const data = await response.json();
  
  if (data.success && data.token) {
    const res = NextResponse.json(data);
    res.cookies.set('oasis_token', data.token, {
      httpOnly: true,
      secure: process.env.NODE_ENV === 'production',
      sameSite: 'strict',
      maxAge: 60 * 60 * 24 * 7 // 7 days
    });
    return res;
  }
  
  return NextResponse.json(data, { status: 401 });
}
```

### Server Component with Auth

```tsx
// app/dashboard/page.tsx
import { cookies } from 'next/headers';
import { redirect } from 'next/navigation';

async function getUser() {
  const token = cookies().get('oasis_token')?.value;
  
  if (!token) {
    redirect('/login');
  }
  
  const response = await fetch('https://api.oasis.network/avatar/profile', {
    headers: { Authorization: `Bearer ${token}` }
  });
  
  return response.json();
}

export default async function DashboardPage() {
  const user = await getUser();
  
  return (
    <div>
      <h1>Dashboard</h1>
      <p>Welcome, {user.username}!</p>
    </div>
  );
}
```

## Pages Router Support

```tsx
// pages/_app.tsx
import { OasisSSOProvider } from '@oasis/avatar-sso-nextjs';
import type { AppProps } from 'next/app';

export default function App({ Component, pageProps }: AppProps) {
  return (
    <OasisSSOProvider apiUrl="https://api.oasis.network">
      <Component {...pageProps} />
    </OasisSSOProvider>
  );
}
```

```tsx
// pages/login.tsx
import { useOasisSSO } from '@oasis/avatar-sso-nextjs';

export default function Login() {
  const { login } = useOasisSSO();
  // ... same as App Router example
}
```

## Features

✨ **App Router & Pages Router** - Full support for both
🔐 **Server-Side Auth** - Secure cookie-based authentication
🚀 **TypeScript** - Complete type safety
🎯 **Middleware Support** - Easy route protection
📱 **SSR & SSG** - Server-side rendering compatible

## API Reference

### Client Components

```typescript
import { useOasisSSO } from '@oasis/avatar-sso-nextjs';

const {
  user,
  isAuthenticated,
  isLoading,
  login,
  logout,
  refreshToken
} = useOasisSSO();
```

### Server Actions (App Router)

```typescript
// app/actions/auth.ts
'use server';

import { cookies } from 'next/headers';

export async function serverLogin(username: string, password: string) {
  // Server-side authentication
  const response = await fetch('https://api.oasis.network/avatar/authenticate', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password, provider: 'Auto' })
  });
  
  const data = await response.json();
  
  if (data.success && data.token) {
    cookies().set('oasis_token', data.token, {
      httpOnly: true,
      secure: true,
      sameSite: 'strict'
    });
  }
  
  return data;
}
```

## Environment Variables

```env
NEXT_PUBLIC_OASIS_API_URL=https://api.oasis.network
OASIS_API_SECRET=your-secret-key
```

## License

MIT © OASIS Team


