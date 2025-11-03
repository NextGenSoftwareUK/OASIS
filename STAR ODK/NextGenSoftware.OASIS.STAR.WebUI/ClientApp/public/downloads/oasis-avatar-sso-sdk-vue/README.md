# OASIS Avatar SSO SDK - Vue 3

Complete Vue 3 integration for OASIS Avatar Single Sign-On authentication with Composition API.

## Installation

```bash
npm install @oasis/avatar-sso-vue
```

## Quick Start

### 1. Install Plugin

```javascript
import { createApp } from 'vue';
import { OasisSSOPlugin } from '@oasis/avatar-sso-vue';
import App from './App.vue';

const app = createApp(App);

app.use(OasisSSOPlugin, {
  apiUrl: 'https://api.oasis.network',
  provider: 'Auto'
});

app.mount('#app');
```

### 2. Use Composable in Components

```vue
<template>
  <div v-if="isLoading">Loading...</div>
  
  <div v-else-if="isAuthenticated">
    <h2>Welcome, {{ user?.username }}! 🌟</h2>
    <button @click="logout">Beam Out</button>
  </div>
  
  <div v-else class="login-form">
    <h2>OASIS Avatar Login</h2>
    <input v-model="username" placeholder="Username" />
    <input v-model="password" type="password" placeholder="Password" />
    <select v-model="provider">
      <option value="Auto">Auto</option>
      <option value="MongoDBOASIS">MongoDB</option>
      <option value="EthereumOASIS">Ethereum</option>
    </select>
    <button @click="handleLogin">Beam In 🚀</button>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { useOasisSSO } from '@oasis/avatar-sso-vue';

const { user, isAuthenticated, isLoading, login, logout } = useOasisSSO();

const username = ref('');
const password = ref('');
const provider = ref('Auto');

const handleLogin = async () => {
  try {
    await login(username.value, password.value, provider.value);
  } catch (error) {
    console.error('Login failed:', error);
  }
};
</script>
```

### 3. Navigation Guards

```javascript
import { useOasisSSO } from '@oasis/avatar-sso-vue';

router.beforeEach(async (to, from, next) => {
  const { isAuthenticated } = useOasisSSO();
  
  if (to.meta.requiresAuth && !isAuthenticated.value) {
    next('/login');
  } else {
    next();
  }
});
```

## Features

✨ **Composition API** - Modern Vue 3 composables
🎯 **Reactive State** - Built on Vue's reactivity system
🚀 **TypeScript** - Full type definitions
🔄 **Auto Refresh** - Automatic token management
🎨 **Components** - Pre-built Vue components

## API Reference

### useOasisSSO Composable

Returns reactive refs and methods:

```typescript
{
  // Reactive State
  user: Ref<User | null>;
  isAuthenticated: Ref<boolean>;
  isLoading: Ref<boolean>;
  
  // Methods
  login: (username: string, password: string, provider?: string) => Promise<void>;
  logout: () => Promise<void>;
  refreshToken: () => Promise<void>;
}
```

### Plugin Options

```typescript
{
  apiUrl: string;
  provider?: string;
  onAuthChange?: (isAuthenticated: boolean) => void;
}
```

## Pre-built Components

### OasisLoginForm

```vue
<template>
  <OasisLoginForm
    @success="handleSuccess"
    @error="handleError"
  />
</template>

<script setup>
import { OasisLoginForm } from '@oasis/avatar-sso-vue';

const handleSuccess = () => {
  router.push('/dashboard');
};

const handleError = (error) => {
  console.error('Login error:', error);
};
</script>
```

### OasisAvatarCard

```vue
<template>
  <OasisAvatarCard
    :show-name="true"
    :show-email="true"
    :show-logout="true"
  />
</template>

<script setup>
import { OasisAvatarCard } from '@oasis/avatar-sso-vue';
</script>
```

## Options API Support

For Vue 2 or Options API users:

```vue
<script>
export default {
  inject: ['oasisSSO'],
  computed: {
    user() {
      return this.oasisSSO.user;
    },
    isAuthenticated() {
      return this.oasisSSO.isAuthenticated;
    }
  },
  methods: {
    async login() {
      await this.oasisSSO.login(this.username, this.password);
    }
  }
}
</script>
```

## License

MIT © OASIS Team

