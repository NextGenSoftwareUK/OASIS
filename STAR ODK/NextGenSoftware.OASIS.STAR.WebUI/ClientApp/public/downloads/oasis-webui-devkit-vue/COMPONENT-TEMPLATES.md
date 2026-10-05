# Vue 3 Component Templates

This guide provides templates for creating OASIS components in Vue 3.

## Component Template Pattern

```vue
<template>
  <div :class="['oasis-component-name', `oasis-component-name--${theme}`]">
    <!-- Your component UI here -->
    <div v-if="loading">Loading...</div>
    <div v-else>
      <!-- Main content -->
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  // Required props
  avatarId?: string;
  // Optional props with defaults
  theme?: 'light' | 'dark';
  // ... other props
}

const props = withDefaults(defineProps<Props>(), {
  theme: 'dark',
  // ... other defaults
});

const emit = defineEmits<{
  // Define your events
  update: [data: any];
  error: [error: Error];
}>();

// Reactive state
const data = ref<any>(null);
const loading = ref(false);
const client = new OASISClient();

// Lifecycle
onMounted(async () => {
  loading.value = true;
  try {
    data.value = await client.someMethod(props.avatarId);
  } catch (error) {
    emit('error', error as Error);
  } finally {
    loading.value = false;
  }
});

// Methods
const handleAction = async () => {
  // Your logic
};

// Computed
const computedValue = computed(() => {
  return data.value?.someProperty;
});
</script>

<style scoped>
.oasis-component-name {
  padding: 1.5rem;
}
.oasis-component-name--dark {
  background: #1a1a1a;
  color: white;
}
</style>
```

## Remaining Components to Build

### 1. NFTGallery.vue
```vue
<template>
  <div class="oasis-nft-gallery">
    <div class="nft-grid" :style="{ gridTemplateColumns: `repeat(${columns}, 1fr)` }">
      <div v-for="nft in nfts" :key="nft.id" @click="$emit('select', nft)">
        <img :src="nft.imageUrl" :alt="nft.name" />
        <h4>{{ nft.name }}</h4>
        <p>{{ nft.price }} OASIS</p>
      </div>
    </div>
  </div>
</template>
```

### 2. Messaging.vue
```vue
<template>
  <div class="oasis-messaging">
    <div class="messages" ref="messagesContainer">
      <div v-for="msg in messages" :key="msg.id">
        {{ msg.content }}
      </div>
    </div>
    <input v-model="newMessage" @keyup.enter="sendMessage" />
  </div>
</template>
```

### 3. DataManagement.vue
### 4. ProviderManagement.vue
### 5. OASISSettings.vue
### 6. Notifications.vue
### 7. SocialFeed.vue
### 8. FriendsList.vue
### 9. GroupManagement.vue
### 10. NFTManagement.vue
### 11. GeoNFTMap.vue
### 12. GeoNFTManagement.vue
### 13. ChatWidget.vue
### 14. AchievementsBadges.vue

## Using Composables

```typescript
// In your component
import { useOASIS, useKarma, useNFTs } from '../composables/useOASIS';

const { currentAvatar, isAuthenticated, login, logout } = useOASIS();
const { karma, loadKarma, addKarma } = useKarma(avatarId);
const { nfts, loadNFTs, mint, transfer } = useNFTs(avatarId);
```

## Quick Build Guide

1. Copy template above
2. Replace component name
3. Add your props interface
4. Implement onMounted logic
5. Add your template markup
6. Style as needed
7. Export in index.ts

