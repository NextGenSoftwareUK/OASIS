# Svelte Component Templates

Build reactive OASIS components with Svelte's elegant syntax.

## Base Svelte Component Template

```svelte
<script lang="ts">
  import { onMount } from 'svelte';
  import { writable } from 'svelte/store';
  import { OASISClient } from '@oasis/api-client';

  // Props
  export let avatarId: string;
  export let theme: 'light' | 'dark' = 'dark';
  
  // State
  let data = writable<any>(null);
  let loading = writable<boolean>(false);
  let error = writable<string | null>(null);
  
  const client = new OASISClient();

  // Lifecycle
  onMount(async () => {
    await loadData();
  });

  // Methods
  async function loadData() {
    loading.set(true);
    try {
      const result = await client.someMethod(avatarId);
      data.set(result);
    } catch (err) {
      error.set(err.message);
    } finally {
      loading.set(false);
    }
  }

  function handleAction() {
    // Your logic
  }
</script>

<div class="oasis-component" class:dark={theme === 'dark'}>
  {#if $loading}
    <div class="loading">Loading...</div>
  {:else if $error}
    <div class="error">{$error}</div>
  {:else if $data}
    <div class="content">
      <!-- Your content here -->
    </div>
  {/if}
</div>

<style>
  .oasis-component {
    padding: 1.5rem;
    border-radius: 8px;
  }
  
  .oasis-component.dark {
    background: #1a1a1a;
    color: white;
  }
  
  .loading {
    text-align: center;
    padding: 2rem;
  }
</style>
```

## Using Svelte Stores

```typescript
// stores/oasis.ts
import { writable, derived } from 'svelte/store';

export const currentAvatar = writable<any>(null);
export const isAuthenticated = derived(currentAvatar, $avatar => !!$avatar);

export const karma = writable<number>(0);
export const nfts = writable<any[]>([]);
```

## Priority Components Templates

### 1. AvatarSSO.svelte
```svelte
<script lang="ts">
  export let providers: string[] = ['holochain', 'ethereum'];
  export let onSuccess: (avatar: any) => void = () => {};
  
  let isOpen = false;
  let loading = '';
  
  async function login(provider: string) {
    loading = provider;
    // ... auth logic
    onSuccess(result);
    isOpen = false;
  }
</script>

<button on:click={() => isOpen = true}>Sign In with OASIS</button>

{#if isOpen}
  <div class="modal">
    {#each providers as provider}
      <button on:click={() => login(provider)}>
        {loading === provider ? 'Connecting...' : `Connect ${provider}`}
      </button>
    {/each}
  </div>
{/if}
```

### 2. KarmaDisplay.svelte
```svelte
<script lang="ts">
  export let avatarId: string;
  export let showHistory = true;
  
  let karma = 0;
  let history: any[] = [];
</script>

<div class="karma-display">
  <h3>Karma Points</h3>
  <div class="value">{karma.toLocaleString()}</div>
  
  {#if showHistory}
    <ul>
      {#each history as entry}
        <li>{entry.amount} - {entry.reason}</li>
      {/each}
    </ul>
  {/if}
</div>
```

### 3. NFTCard.svelte
```svelte
<script lang="ts">
  export let nft: any;
  export let onClick: (nft: any) => void = () => {};
</script>

<div class="nft-card" on:click={() => onClick(nft)}>
  <img src={nft.imageUrl} alt={nft.name} />
  <h4>{nft.name}</h4>
  <p>{nft.price} OASIS</p>
</div>

<style>
  .nft-card {
    cursor: pointer;
    border: 1px solid #ddd;
    border-radius: 8px;
    overflow: hidden;
    transition: transform 0.2s;
  }
  .nft-card:hover {
    transform: translateY(-4px);
  }
</style>
```

## Reactive Patterns

```svelte
<script lang="ts">
  // Reactive declarations
  $: fullName = `${firstName} ${lastName}`;
  $: isValid = username.length > 3;
  
  // Reactive statements
  $: {
    if (avatarId) {
      loadData(avatarId);
    }
  }
  
  // Store subscriptions
  $: console.log('Karma changed:', $karma);
</script>
```

## Event Handling

```svelte
<script lang="ts">
  import { createEventDispatcher } from 'svelte';
  
  const dispatch = createEventDispatcher();
  
  function handleAction() {
    dispatch('action', { data: 'value' });
  }
</script>

<button on:click={handleAction}>Action</button>
```

## Remaining Components

- AvatarDetail.svelte
- NFTGallery.svelte  
- Messaging.svelte
- DataManager.svelte
- ProviderSelector.svelte
- SettingsPanel.svelte
- NotificationToast.svelte
- SocialFeed.svelte
- FriendsList.svelte
- GroupManager.svelte
- Leaderboard.svelte
- Achievements.svelte
- GeoNFTMap.svelte
- ChatWidget.svelte

