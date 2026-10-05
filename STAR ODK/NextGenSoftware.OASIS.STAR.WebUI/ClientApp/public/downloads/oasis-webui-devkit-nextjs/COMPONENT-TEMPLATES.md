# Next.js Component Templates

Build OASIS components for Next.js 13+ with App Router and Server Components.

## Server Component Template

```typescript
// app/components/ServerComponent.tsx
import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

interface Props {
  avatarId: string;
}

export default async function ServerComponent({ avatarId }: Props) {
  const client = new OASISServerClient();
  const data = await client.getData(avatarId);

  return (
    <div className="oasis-server-component">
      <h3>{data.title}</h3>
      <pre>{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
}
```

## Client Component Template

```typescript
// app/components/ClientComponent.tsx
'use client';

import { useState, useEffect } from 'react';
import { OASISClient } from '@oasis/api-client';

interface Props {
  avatarId: string;
  theme?: 'light' | 'dark';
}

export default function ClientComponent({ avatarId, theme = 'dark' }: Props) {
  const [data, setData] = useState<any>(null);
  const [loading, setLoading] = useState(false);
  const client = new OASISClient();

  useEffect(() => {
    const loadData = async () => {
      setLoading(true);
      try {
        const result = await client.getData(avatarId);
        setData(result);
      } finally {
        setLoading(false);
      }
    };
    loadData();
  }, [avatarId]);

  if (loading) return <div>Loading...</div>;

  return (
    <div className={`oasis-client-component oasis-client-component--${theme}`}>
      {/* Your UI */}
    </div>
  );
}
```

## Server Actions Template

```typescript
// app/actions/karma.ts
'use server';

import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';
import { revalidatePath } from 'next/cache';

export async function addKarmaAction(avatarId: string, amount: number, reason: string) {
  const client = new OASISServerClient();
  const result = await client.addKarma(avatarId, amount, reason);
  
  // Revalidate the karma page
  revalidatePath(`/avatar/${avatarId}/karma`);
  
  return result;
}

export async function mintNFTAction(avatarId: string, nftData: any) {
  const client = new OASISServerClient();
  const result = await client.mintNFT(avatarId, nftData);
  
  revalidatePath(`/avatar/${avatarId}/nfts`);
  
  return result;
}
```

## API Route Template

```typescript
// app/api/karma/route.ts
import { NextRequest, NextResponse } from 'next/server';
import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

export async function GET(request: NextRequest) {
  const avatarId = request.nextUrl.searchParams.get('avatarId');
  
  if (!avatarId) {
    return NextResponse.json({ error: 'Avatar ID required' }, { status: 400 });
  }

  const client = new OASISServerClient();
  const karma = await client.getKarma(avatarId);
  
  return NextResponse.json(karma);
}

export async function POST(request: NextRequest) {
  const { avatarId, amount, reason } = await request.json();
  
  const client = new OASISServerClient();
  const result = await client.addKarma(avatarId, amount, reason);
  
  return NextResponse.json(result);
}
```

## Middleware Template

```typescript
// middleware.ts
import { NextResponse } from 'next/server';
import type { NextRequest } from 'next/server';

export function middleware(request: NextRequest) {
  const avatarSession = request.cookies.get('oasis_session');
  
  // Protect routes
  if (!avatarSession && request.nextUrl.pathname.startsWith('/dashboard')) {
    return NextResponse.redirect(new URL('/login', request.url));
  }
  
  return NextResponse.next();
}

export const config = {
  matcher: ['/((?!api|_next/static|_next/image|favicon.ico).*)']
};
```

## Priority Components

### 1. Server: AvatarDetailServer
```typescript
// components/server/AvatarDetailServer.tsx
import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

export default async function AvatarDetailServer({ avatarId }: { avatarId: string }) {
  const client = new OASISServerClient();
  const avatar = await client.getAvatarDetail(avatarId);

  return (
    <div>
      <h2>{avatar.username}</h2>
      <p>Karma: {avatar.karma}</p>
      <p>Level: {avatar.level}</p>
    </div>
  );
}
```

### 2. Client: KarmaManagement
```typescript
'use client';

import { useState } from 'react';
import { addKarmaAction } from '@/app/actions/karma';

export default function KarmaManagement({ avatarId }: { avatarId: string }) {
  const [amount, setAmount] = useState(0);
  const [reason, setReason] = useState('');

  const handleAddKarma = async () => {
    await addKarmaAction(avatarId, amount, reason);
  };

  return (
    <div>
      <input type="number" value={amount} onChange={(e) => setAmount(+e.target.value)} />
      <input type="text" value={reason} onChange={(e) => setReason(e.target.value)} />
      <button onClick={handleAddKarma}>Add Karma</button>
    </div>
  );
}
```

### 3. Hybrid: NFTGallery
```typescript
// Server component for initial data
import NFTGalleryClient from './NFTGalleryClient';

export default async function NFTGallery({ avatarId }: { avatarId: string }) {
  const client = new OASISServerClient();
  const initialNFTs = await client.getNFTs(avatarId);

  return <NFTGalleryClient initialNFTs={initialNFTs} avatarId={avatarId} />;
}

// Client component for interactivity
'use client';

export default function NFTGalleryClient({ initialNFTs, avatarId }) {
  const [nfts, setNFTs] = useState(initialNFTs);
  // ... interactive logic
}
```

## Layout with Providers

```typescript
// app/layout.tsx
import { OASISProvider } from '@oasis/webui-devkit-nextjs';

export default function RootLayout({ children }: { children: React.ReactNode }) {
  return (
    <html lang="en">
      <body>
        <OASISProvider
          config={{
            apiEndpoint: process.env.NEXT_PUBLIC_OASIS_API!,
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

## Streaming and Suspense

```typescript
import { Suspense } from 'react';
import AvatarDetailServer from './AvatarDetailServer';

export default function Page() {
  return (
    <Suspense fallback={<div>Loading avatar...</div>}>
      <AvatarDetailServer avatarId="123" />
    </Suspense>
  );
}
```

## Remaining Components

**Server Components:**
- KarmaLeaderboardServer
- NFTGalleryServer  
- SocialFeedServer

**Client Components:**
- AvatarSSO (client)
- Messaging (client)
- DataManagement (client)
- ProviderManagement (client)
- OASISSettings (client)
- Notifications (client)
- ChatWidget (client)
- GeoNFTMap (client)

**Hybrid Components:**
- AchievementsDisplay
- FriendsList
- GroupManagement

