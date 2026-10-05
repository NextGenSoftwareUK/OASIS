<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  theme?: 'light' | 'dark';
}

const props = withDefaults(defineProps<Props>(), {
  theme: 'dark'
});

const providers = ref<any[]>([]);
const currentProvider = ref<string>('');
const loading = ref(false);
const client = new OASISClient();

onMounted(async () => {
  await loadProviders();
});

async function loadProviders() {
  loading.value = true;
  try {
    providers.value = await client.getAvailableProviders();
    currentProvider.value = await client.getCurrentProvider();
  } finally {
    loading.value = false;
  }
}

async function switchProvider(providerName: string) {
  try {
    await client.switchProvider(providerName);
    currentProvider.value = providerName;
  } catch (error) {
    console.error('Error switching provider:', error);
  }
}
</script>

<template>
  <div class="oasis-provider-mgmt" :class="[`oasis-provider-mgmt--${theme}`]">
    <h3>Provider Management</h3>
    
    <div v-if="loading" class="loading">Loading providers...</div>

    <div v-else class="providers">
      <div
        v-for="provider in providers"
        :key="provider.name"
        class="provider-item"
        :class="{ active: provider.name === currentProvider }"
        @click="switchProvider(provider.name)"
      >
        <div class="provider-icon">{{ provider.icon }}</div>
        <div class="provider-info">
          <h4>{{ provider.name }}</h4>
          <p>{{ provider.description }}</p>
        </div>
        <div v-if="provider.name === currentProvider" class="active-badge">Active</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.oasis-provider-mgmt {
  padding: 1.5rem;
  background: white;
  color: #333;
  border-radius: 12px;
}

.oasis-provider-mgmt--dark {
  background: #1a1a1a;
  color: white;
}

.providers {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.provider-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #f5f5f5;
  border: 2px solid transparent;
  border-radius: 8px;
  cursor: pointer;
  transition: all 0.2s;
}

.oasis-provider-mgmt--dark .provider-item {
  background: #2a2a2a;
}

.provider-item:hover {
  border-color: #00bcd4;
}

.provider-item.active {
  border-color: #00bcd4;
  background: rgba(0, 188, 212, 0.1);
}

.provider-icon {
  font-size: 2rem;
}

.provider-info {
  flex: 1;
}

.provider-info h4 {
  margin: 0 0 0.25rem 0;
}

.provider-info p {
  margin: 0;
  font-size: 0.875rem;
  opacity: 0.7;
}

.active-badge {
  padding: 0.25rem 0.75rem;
  background: #00bcd4;
  color: white;
  border-radius: 12px;
  font-size: 0.875rem;
}
</style>
