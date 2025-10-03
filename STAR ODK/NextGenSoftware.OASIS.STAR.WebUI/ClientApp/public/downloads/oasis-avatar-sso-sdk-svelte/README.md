# OASIS Avatar SSO SDK - Svelte

Complete Svelte integration for OASIS Avatar Single Sign-On authentication with stores.

## Installation

```bash
npm install @oasis/avatar-sso-svelte
```

## Quick Start

### 1. Initialize in App

```svelte
<!-- App.svelte -->
<script>
  import { onMount } from 'svelte';
  import { initOasisSSO } from '@oasis/avatar-sso-svelte';
  
  onMount(() => {
    initOasisSSO({
      apiUrl: 'https://api.oasis.network',
      provider: 'Auto'
    });
  });
</script>

<slot />
```

### 2. Use in Components

```svelte
<!-- Login.svelte -->
<script>
  import { oasisSSO, user, isAuthenticated, isLoading } from '@oasis/avatar-sso-svelte';
  
  let username = '';
  let password = '';
  let provider = 'Auto';
  
  async function handleLogin() {
    try {
      await oasisSSO.login(username, password, provider);
      // Redirect on success
      window.location.href = '/dashboard';
    } catch (error) {
      console.error('Login failed:', error);
      alert('Login failed: ' + error.message);
    }
  }
</script>

{#if $isLoading}
  <div>Loading...</div>
{:else if $isAuthenticated}
  <div>
    <h2>Welcome, {$user?.username}! 🌟</h2>
    <button on:click={() => oasisSSO.logout()}>Beam Out</button>
  </div>
{:else}
  <div class="login-form">
    <h2>OASIS Avatar Login</h2>
    <input bind:value={username} placeholder="Username" />
    <input bind:value={password} type="password" placeholder="Password" />
    <select bind:value={provider}>
      <option value="Auto">Auto</option>
      <option value="MongoDBOASIS">MongoDB</option>
      <option value="EthereumOASIS">Ethereum</option>
    </select>
    <button on:click={handleLogin}>Beam In 🚀</button>
  </div>
{/if}
```

### 3. Protected Routes

```svelte
<!-- +layout.svelte -->
<script>
  import { goto } from '$app/navigation';
  import { isAuthenticated } from '@oasis/avatar-sso-svelte';
  import { onMount } from 'svelte';
  
  export let data;
  
  onMount(() => {
    if (data.requiresAuth && !$isAuthenticated) {
      goto('/login');
    }
  });
</script>

<slot />
```

## Features

✨ **Svelte Stores** - Reactive stores for auth state
🎯 **Auto-subscriptions** - Automatic reactivity
🚀 **TypeScript** - Full type safety
🔄 **SSR Support** - Works with SvelteKit
🎨 **Components** - Pre-built Svelte components

## API Reference

### Stores

All stores are readable and auto-updating:

```typescript
import { user, isAuthenticated, isLoading } from '@oasis/avatar-sso-svelte';

// Use with $ prefix in components
$user // Current user object
$isAuthenticated // Boolean auth state
$isLoading // Boolean loading state
```

### Methods

```typescript
import { oasisSSO } from '@oasis/avatar-sso-svelte';

// Login
await oasisSSO.login(username, password, provider);

// Logout
await oasisSSO.logout();

// Refresh token
await oasisSSO.refreshToken();

// Check auth
const isAuth = await oasisSSO.isAuthenticated();

// Get user
const currentUser = await oasisSSO.getCurrentUser();
```

### Initialization

```typescript
import { initOasisSSO } from '@oasis/avatar-sso-svelte';

initOasisSSO({
  apiUrl: string;
  provider?: string;
  onAuthChange?: (isAuthenticated: boolean) => void;
});
```

## Pre-built Components

### OasisLoginForm

```svelte
<script>
  import { OasisLoginForm } from '@oasis/avatar-sso-svelte';
  
  function handleSuccess() {
    goto('/dashboard');
  }
  
  function handleError(error) {
    console.error(error);
  }
</script>

<OasisLoginForm
  on:success={handleSuccess}
  on:error={handleError}
/>
```

### OasisAvatarDisplay

```svelte
<script>
  import { OasisAvatarDisplay } from '@oasis/avatar-sso-svelte';
</script>

<OasisAvatarDisplay
  showName={true}
  showEmail={true}
  showLogout={true}
/>
```

## SvelteKit Integration

```typescript
// src/hooks.server.ts
import { oasisSSO } from '@oasis/avatar-sso-svelte';

export async function handle({ event, resolve }) {
  const token = event.cookies.get('oasis_token');
  
  if (token) {
    event.locals.user = await oasisSSO.getCurrentUser();
  }
  
  return resolve(event);
}
```

## License

MIT © OASIS Team


