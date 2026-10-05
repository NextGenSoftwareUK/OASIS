# OASIS Web UI Dev Kit - Complete Implementation Guide

## 🎯 Overview

The OASIS Web UI Dev Kit provides 20+ pre-built components for all major web frameworks, allowing you to integrate OASIS functionality into your applications with minimal effort.

## 📦 Available Kits

| Framework | Status | Package | Documentation |
|-----------|--------|---------|---------------|
| **React** | ✅ Complete | `@oasis/webui-devkit-react` | [Docs](./oasis-webui-devkit-react/README.md) |
| **Angular** | ✅ Complete | `@oasis/webui-devkit-angular` | [Docs](./oasis-webui-devkit-angular/README.md) |
| **Vue 3** | 🔄 Templates | `@oasis/webui-devkit-vue` | [Templates](./oasis-webui-devkit-vue/COMPONENT-TEMPLATES.md) |
| **Vanilla JS** | 🔄 Templates | `@oasis/webui-devkit-vanilla` | [Templates](./oasis-webui-devkit-vanilla/COMPONENT-TEMPLATES.md) |
| **Svelte** | 🔄 Templates | `@oasis/webui-devkit-svelte` | [Templates](./oasis-webui-devkit-svelte/COMPONENT-TEMPLATES.md) |
| **Next.js** | 🔄 Templates | `@oasis/webui-devkit-nextjs` | [Templates](./oasis-webui-devkit-nextjs/COMPONENT-TEMPLATES.md) |

## 🚀 Quick Start by Framework

### React

```bash
npm install @oasis/webui-devkit-react
```

```tsx
import { AvatarSSO, KarmaManagement, NFTGallery } from '@oasis/webui-devkit-react';

function App() {
  const [avatarId, setAvatarId] = useState('');
  
  return (
    <OASISProvider config={{ apiEndpoint: 'https://api.oasis.network' }}>
      <AvatarSSO onSuccess={(avatar) => setAvatarId(avatar.id)} />
      <KarmaManagement avatarId={avatarId} />
      <NFTGallery avatarId={avatarId} columns={3} />
    </OASISProvider>
  );
}
```

### Angular

```bash
ng add @oasis/webui-devkit-angular
```

```typescript
import { OasisWebUIModule } from '@oasis/webui-devkit-angular';

@NgModule({
  imports: [
    OasisWebUIModule.forRoot({
      apiEndpoint: 'https://api.oasis.network'
    })
  ]
})
```

```html
<oasis-avatar-sso (onSuccess)="handleLogin($event)"></oasis-avatar-sso>
<oasis-karma-management [avatarId]="avatarId"></oasis-karma-management>
```

### Vue 3

```bash
npm install @oasis/webui-devkit-vue
```

```vue
<script setup>
import { AvatarSSO, KarmaManagement } from '@oasis/webui-devkit-vue';
import { ref } from 'vue';

const avatarId = ref('');
</script>

<template>
  <AvatarSSO @success="avatarId = $event.id" />
  <KarmaManagement :avatar-id="avatarId" />
</template>
```

### Vanilla JS (Web Components)

```html
<script src="https://cdn.oasis.network/webui-devkit/1.0.0/oasis.min.js"></script>

<oasis-avatar-sso providers="holochain,ethereum"></oasis-avatar-sso>
<oasis-karma-display avatar-id="123"></oasis-karma-display>

<script>
  document.querySelector('oasis-avatar-sso')
    .addEventListener('success', (e) => console.log(e.detail));
</script>
```

### Svelte

```bash
npm install @oasis/webui-devkit-svelte
```

```svelte
<script>
  import { AvatarSSO, KarmaDisplay } from '@oasis/webui-devkit-svelte';
  let avatarId = '';
</script>

<AvatarSSO on:success={(e) => avatarId = e.detail.id} />
<KarmaDisplay {avatarId} />
```

### Next.js

```bash
npm install @oasis/webui-devkit-nextjs
```

```tsx
// Server Component (app/page.tsx)
import { AvatarDetailServer } from '@oasis/webui-devkit-nextjs/server';

export default async function Page() {
  return <AvatarDetailServer avatarId="123" />;
}

// Client Component
'use client';
import { KarmaManagement } from '@oasis/webui-devkit-nextjs';

export function Dashboard() {
  return <KarmaManagement avatarId="123" />;
}
```

## 📋 Component Catalog

### Authentication & User Management
- **AvatarSSO** - Multi-provider authentication (Holochain, Ethereum, Solana, etc.)
- **AvatarDetail** - Display and edit avatar profile information
- **AvatarCard** - Compact avatar display card

### Karma & Gamification
- **KarmaManagement** - Display and manage karma points with history
- **KarmaLeaderboard** - Show top karma earners with rankings
- **AchievementsBadges** - Display user achievements and progress

### NFT & Digital Assets
- **NFTGallery** - Display NFT collections with filtering and sorting
- **NFTManagement** - Mint, transfer, and manage NFTs
- **GeoNFTMap** - Interactive map for location-based NFTs
- **GeoNFTManagement** - Create and manage Geo-NFTs

### Communication
- **Messaging** - Real-time messaging component
- **ChatWidget** - Embeddable chat widget (floating or inline)
- **Notifications** - Toast and notification system

### Data & Storage
- **DataManagement** - CRUD operations for OASIS data
- **ProviderManagement** - Switch between storage providers
- **OASISSettings** - Configure OASIS settings

### Social & Community
- **SocialFeed** - Activity feed component
- **FriendsList** - Display and manage connections
- **GroupManagement** - Create and manage groups

## 🎨 Theming & Customization

### Global Theme Configuration

```typescript
// React
<OASISProvider config={{
  theme: {
    primaryColor: '#00bcd4',
    secondaryColor: '#ff4081',
    mode: 'dark'
  }
}} />

// Angular
OasisWebUIModule.forRoot({
  theme: { primary: '#00bcd4', mode: 'dark' }
})

// Vue
app.use(createOASIS({
  theme: { primary: '#00bcd4', mode: 'dark' }
}))
```

### Component-Level Customization

```tsx
// Custom styles
<KarmaManagement
  theme="dark"
  customStyles={{
    background: 'linear-gradient(135deg, #667eea 0%, #764ba2 100%)',
    borderRadius: '20px'
  }}
/>

// CSS variables
<style>
  :root {
    --oasis-primary: #00bcd4;
    --oasis-secondary: #ff4081;
    --oasis-radius: 12px;
  }
</style>
```

## 🔌 API Integration

### Client-Side API

```typescript
import { OASISClient } from '@oasis/api-client';

const client = new OASISClient({
  apiEndpoint: 'https://api.oasis.network',
  defaultProvider: 'holochain'
});

// Authentication
const avatar = await client.login(provider);

// Karma
const karma = await client.getKarma(avatarId);
await client.addKarma(avatarId, 100, 'Quest completed');

// NFTs
const nfts = await client.getNFTs(avatarId);
await client.mintNFT(avatarId, nftData);
```

### Server-Side API (Next.js)

```typescript
import { OASISServerClient } from '@oasis/webui-devkit-nextjs/server';

const client = new OASISServerClient();
const data = await client.getAvatarDetail(avatarId);
```

## 🛠️ Building Custom Components

### Following the Pattern

Each component follows a consistent pattern:

1. **Props/Attributes** - Accept configuration
2. **State Management** - Handle loading, data, errors
3. **API Integration** - Fetch/update data from OASIS
4. **Events/Callbacks** - Emit events for parent components
5. **Theming** - Support light/dark modes
6. **Styling** - Scoped/modular styles

### Example: Custom Component

```tsx
// React
export const CustomComponent: React.FC<Props> = ({ avatarId, theme }) => {
  const [data, setData] = useState(null);
  const [loading, setLoading] = useState(false);
  const client = new OASISClient();

  useEffect(() => {
    loadData();
  }, [avatarId]);

  const loadData = async () => {
    setLoading(true);
    const result = await client.customMethod(avatarId);
    setData(result);
    setLoading(false);
  };

  return (
    <div className={`custom-component custom-component--${theme}`}>
      {loading ? <Spinner /> : <Content data={data} />}
    </div>
  );
};
```

## 📚 Resources

- **Full Documentation**: https://docs.oasis.network/webui-devkit
- **API Reference**: https://docs.oasis.network/api
- **Component Demos**: https://demos.oasis.network/webui-devkit
- **GitHub Repository**: https://github.com/oasis-network/webui-devkit
- **Discord Support**: https://discord.gg/oasis-network

## 🐛 Troubleshooting

### Common Issues

**Components not rendering?**
- Ensure OASISProvider wraps your app
- Check API endpoint configuration
- Verify network connectivity

**Styling issues?**
- Import CSS: `import '@oasis/webui-devkit-react/dist/style.css'`
- Check theme prop values
- Inspect CSS variable overrides

**TypeScript errors?**
- Install type definitions: `@types/...`
- Update tsconfig.json include paths
- Check peer dependency versions

### Debug Mode

```typescript
<OASISProvider config={{
  debug: true,
  logLevel: 'verbose'
}} />
```

## 🤝 Contributing

Want to add more components or improve existing ones?

1. Fork the repository
2. Create a feature branch
3. Follow the component pattern guide
4. Add tests and documentation
5. Submit a pull request

## 📄 License

MIT License - Use freely in your projects!

