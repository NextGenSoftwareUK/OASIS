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

const data = ref<any[]>([]);
const loading = ref(false);
const newKey = ref('');
const newValue = ref('');
const client = new OASISClient();

onMounted(async () => {
  await loadData();
});

async function loadData() {
  loading.value = true;
  try {
    const result = await client.getData(props.avatarId);
    data.value = Object.entries(result).map(([key, value]) => ({ key, value }));
  } finally {
    loading.value = false;
  }
}

async function saveData() {
  if (!newKey.value || !newValue.value) return;

  try {
    await client.saveData(props.avatarId, newKey.value, newValue.value);
    newKey.value = '';
    newValue.value = '';
    await loadData();
  } catch (error) {
    console.error('Error saving data:', error);
  }
}

async function deleteData(key: string) {
  try {
    await client.deleteData(props.avatarId, key);
    await loadData();
  } catch (error) {
    console.error('Error deleting data:', error);
  }
}
</script>

<template>
  <div class="oasis-data-mgmt" :class="[`oasis-data-mgmt--${theme}`]">
    <h3>Data Management</h3>

    <div class="add-data">
      <input v-model="newKey" type="text" placeholder="Key" />
      <input v-model="newValue" type="text" placeholder="Value" />
      <button @click="saveData">Add</button>
    </div>

    <div v-if="loading" class="loading">Loading data...</div>

    <div v-else class="data-list">
      <div v-for="item in data" :key="item.key" class="data-item">
        <div class="data-key">{{ item.key }}</div>
        <div class="data-value">{{ item.value }}</div>
        <button class="delete-btn" @click="deleteData(item.key)">×</button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.oasis-data-mgmt {
  padding: 1.5rem;
  background: white;
  color: #333;
  border-radius: 12px;
}

.oasis-data-mgmt--dark {
  background: #1a1a1a;
  color: white;
}

.add-data {
  display: flex;
  gap: 0.5rem;
  margin-bottom: 1.5rem;
}

.add-data input {
  flex: 1;
  padding: 0.5rem;
  border: 1px solid #ddd;
  border-radius: 4px;
}

.oasis-data-mgmt--dark .add-data input {
  background: #2a2a2a;
  border-color: #3a3a3a;
  color: white;
}

.add-data button {
  padding: 0.5rem 1rem;
  background: #00bcd4;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}

.data-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.data-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  background: #f5f5f5;
  border-radius: 8px;
}

.oasis-data-mgmt--dark .data-item {
  background: #2a2a2a;
}

.data-key {
  font-weight: bold;
  min-width: 120px;
}

.data-value {
  flex: 1;
  opacity: 0.8;
}

.delete-btn {
  background: #f44336;
  color: white;
  border: none;
  border-radius: 50%;
  width: 24px;
  height: 24px;
  cursor: pointer;
  font-size: 1.2rem;
  line-height: 1;
}
</style>
