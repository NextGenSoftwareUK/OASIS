# OASIS Web UI Dev Kit - Next.js

Optimized React Server Components and Client Components for Next.js 13+ with OASIS integration.

## 🚀 Quick Start

```bash
npm install @oasis/webui-devkit-nextjs
# or
yarn add @oasis/webui-devkit-nextjs
```

## 📦 Components Included

### Server Components (RSC)
- `AvatarDetailServer` - Server-side avatar data
- `KarmaLeaderboardServer` - SSR leaderboard
- `NFTGalleryServer` - Server-rendered NFTs

### Client Components
- `AvatarSSO` - Interactive auth
- `KarmaManagement` - Client karma
- `NFTManagement` - Interactive NFTs
- `Messaging` - Real-time chat
- `DataManagement` - Data ops
- `ProviderManagement` - Provider control
- `GeoNFTMap` - Interactive map
- And more...

## 📖 App Router Usage (Next.js 13+)

```tsx
// app/page.tsx (Server Component)
import { AvatarDetailServer, KarmaLeaderboardServer } from '@oasis/webui-devkit-nextjs/server';

export default async function HomePage() {
  return (
    <main>
      <AvatarDetailServer avatarId="user-123" />
      <KarmaLeaderboardServer limit={10} />
    </main>
  );
}

// app/dashboard/page.tsx (Client Component)
'use client';

import { 
  AvatarSSO, 
  KarmaManagement, 
  NFTGallery 
} from '@oasis/webui-devkit-nextjs';
import { useState } from 'react';

export default function DashboardPage() {
  const [avatarId, setAvatarId] = useState('');

  return (
    <div>
      <AvatarSSO
        providers={['holochain', 'ethereum', 'solana']}
        onSuccess={(avatar) => setAvatarId(avatar.id)}
      />
      
      {avatarId && (
        <>
          <KarmaManagement avatarId={avatarId} />
          <NFTGallery avatarId={avatarId} columns={3} />
        </>
      )}
    </div>
  );
}
```

## 🔧 Provider Setup

```tsx
// app/layout.tsx
import { OASISProvider } from '@oasis/webui-devkit-nextjs';

export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        <OASISProvider
          config={{
            apiEndpoint: process.env.NEXT_PUBLIC_OASIS_API,
            defaultProvider: 'holochain'
          }}
        >
          {children}
        </OASISProvider>
      </body>
    </html>
  );
}
```

## 🎨 API Routes

```typescript
// app/api/karma/route.ts
import { NextRequest } from 'next/server';
import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

export async function GET(request: NextRequest) {
  const avatarId = request.nextUrl.searchParams.get('avatarId');
  const client = new OASISServerClient();
  
  const karma = await client.getKarma(avatarId);
  
  return Response.json(karma);
}

export async function POST(request: NextRequest) {
  const { avatarId, amount, reason } = await request.json();
  const client = new OASISServerClient();
  
  await client.addKarma(avatarId, amount, reason);
  
  return Response.json({ success: true });
}
```

## 🔐 Server Actions

```typescript
// app/actions/karma.ts
'use server';

import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

export async function addKarmaAction(avatarId: string, amount: number, reason: string) {
  const client = new OASISServerClient();
  return await client.addKarma(avatarId, amount, reason);
}

// In your component
'use client';

import { addKarmaAction } from './actions/karma';

export function KarmaButton({ avatarId }: { avatarId: string }) {
  return (
    <button onClick={() => addKarmaAction(avatarId, 10, 'Button clicked')}>
      Add Karma
    </button>
  );
}
```

## 🚀 Middleware

```typescript
// middleware.ts
import { OASISMiddleware } from '@oasis/webui-devkit-nextjs/middleware';

export const middleware = OASISMiddleware({
  protectedRoutes: ['/dashboard', '/profile'],
  publicRoutes: ['/login', '/signup']
});

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico).*)']
};
```

## 📚 Documentation

https://docs.oasis.network/webui-devkit/nextjs

## 🛠️ Requirements

- Next.js 13.4+
- React 18+
- TypeScript 5.0+

## 📄 License

MIT License

