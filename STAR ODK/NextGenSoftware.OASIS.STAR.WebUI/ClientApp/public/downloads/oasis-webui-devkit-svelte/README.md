# OASIS Web UI Dev Kit - Svelte

Lightweight and reactive Svelte component library for OASIS integration.

## 🚀 Quick Start

```bash
npm install @oasis/webui-devkit-svelte
```

## 📦 Components Included

### Authentication & User
- `AvatarSSO` - Multi-provider auth
- `AvatarDetail` - Profile management
- `AvatarCard` - Avatar display

### Data & Storage
- `DataManagement` - CRUD ops
- `ProviderManagement` - Provider control
- `OASISSettings` - Settings

### Karma & Gaming
- `KarmaManagement` - Karma system
- `KarmaLeaderboard` - Rankings
- `AchievementsBadges` - Achievements

### NFT & Assets
- `NFTGallery` - NFT display
- `NFTManagement` - NFT ops
- `GeoNFTMap` - Location NFTs
- `GeoNFTManagement` - Geo-NFT ops

### Communication
- `Messaging` - Chat
- `ChatWidget` - Widget
- `Notifications` - Alerts

### Social
- `SocialFeed` - Activity
- `FriendsList` - Friends
- `GroupManagement` - Groups

## 📖 Basic Usage

```svelte
<script>
  import { 
    AvatarSSO, 
    KarmaManagement, 
    NFTGallery,
    Messaging 
  } from '@oasis/webui-devkit-svelte';
  
  let currentAvatarId = '';
  
  function handleLogin(event) {
    currentAvatarId = event.detail.id;
    console.log('Logged in:', event.detail);
  }
</script>

<AvatarSSO
  providers={['holochain', 'ethereum', 'solana']}
  on:success={handleLogin}
  theme="dark"
/>

<KarmaManagement 
  avatarId={currentAvatarId}
  theme="dark"
/>

<NFTGallery 
  collections={['my-nfts']}
  columns={3}
/>

<Messaging
  chatId="global-chat"
  position="bottom-right"
/>
```

## 🎨 Theming

```svelte
<script>
  import { setOASISTheme } from '@oasis/webui-devkit-svelte';
  
  setOASISTheme({
    primary: '#00bcd4',
    secondary: '#ff4081',
    mode: 'dark'
  });
</script>
```

## 🔧 Stores

```svelte
<script>
  import { oasisStore, karmaStore, nftStore } from '@oasis/webui-devkit-svelte';
  
  $: avatar = $oasisStore.avatar;
  $: karma = $karmaStore.karma;
  $: nfts = $nftStore.nfts;
</script>

<div>
  {#if avatar}
    <p>Welcome {avatar.username}!</p>
    <p>Karma: {karma}</p>
    <p>NFTs: {nfts.length}</p>
  {/if}
</div>
```

## 📚 Documentation

https://docs.oasis.network/webui-devkit/svelte

## 🛠️ Requirements

- Svelte 4.0+
- Vite or SvelteKit

## 📄 License

MIT License

