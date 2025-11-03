<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  avatarId: string;
  theme?: 'light' | 'dark';
}

const props = withDefaults(defineProps<Props>(), {
  theme: 'dark'
});

const settings = ref({
  defaultProvider: 'holochain',
  autoSync: true,
  notifications: true,
  privacy: 'public',
  theme: 'dark'
});

const loading = ref(false);
const client = new OASISClient();

onMounted(async () => {
  await loadSettings();
});

async function loadSettings() {
  loading.value = true;
  try {
    const result = await client.getSettings(props.avatarId);
    settings.value = { ...settings.value, ...result };
  } finally {
    loading.value = false;
  }
}

async function saveSettings() {
  try {
    await client.saveSettings(props.avatarId, settings.value);
    alert('Settings saved successfully!');
  } catch (error) {
    console.error('Error saving settings:', error);
  }
}
</script>

<template>
  <div class="oasis-settings" :class="[`oasis-settings--${theme}`]">
    <h3>OASIS Settings</h3>

    <div v-if="loading" class="loading">Loading settings...</div>

    <div v-else class="settings-form">
      <div class="setting-group">
        <label>Default Provider</label>
        <select v-model="settings.defaultProvider">
          <option value="holochain">Holochain</option>
          <option value="ethereum">Ethereum</option>
          <option value="solana">Solana</option>
          <option value="ipfs">IPFS</option>
        </select>
      </div>

      <div class="setting-group">
        <label>
          <input v-model="settings.autoSync" type="checkbox" />
          Auto-sync data across providers
        </label>
      </div>

      <div class="setting-group">
        <label>
          <input v-model="settings.notifications" type="checkbox" />
          Enable notifications
        </label>
      </div>

      <div class="setting-group">
        <label>Privacy Level</label>
        <select v-model="settings.privacy">
          <option value="public">Public</option>
          <option value="friends">Friends Only</option>
          <option value="private">Private</option>
        </select>
      </div>

      <div class="setting-group">
        <label>Theme</label>
        <select v-model="settings.theme">
          <option value="light">Light</option>
          <option value="dark">Dark</option>
          <option value="auto">Auto</option>
        </select>
      </div>

      <button class="save-btn" @click="saveSettings">Save Settings</button>
    </div>
  </div>
</template>

<style scoped>
.oasis-settings {
  padding: 1.5rem;
  background: white;
  color: #333;
  border-radius: 12px;
}

.oasis-settings--dark {
  background: #1a1a1a;
  color: white;
}

.settings-form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.setting-group {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.setting-group label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-weight: 500;
}

.setting-group select,
.setting-group input[type="text"] {
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: white;
}

.oasis-settings--dark .setting-group select,
.oasis-settings--dark .setting-group input[type="text"] {
  background: #2a2a2a;
  border-color: #3a3a3a;
  color: white;
}

.save-btn {
  margin-top: 1rem;
  padding: 0.75rem 1.5rem;
  background: #00bcd4;
  color: white;
  border: none;
  border-radius: 8px;
  cursor: pointer;
  font-size: 1rem;
}

.save-btn:hover {
  background: #0097a7;
}
</style>
