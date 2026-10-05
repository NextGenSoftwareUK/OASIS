<template>
  <div class="oasis-karma-management">
    <div class="karma-header">
      <div class="karma-total">
        <h2>{{ totalKarma.toLocaleString() }}</h2>
        <p>Total Karma</p>
      </div>
      <div class="karma-level">
        <span class="level-badge">Level {{ karmaLevel }}</span>
        <div class="progress-bar">
          <div class="progress-fill" :style="{ width: levelProgress + '%' }"></div>
        </div>
        <p>{{ karmaToNextLevel }} to Level {{ karmaLevel + 1 }}</p>
      </div>
    </div>

    <div class="karma-breakdown">
      <h3>Karma Breakdown</h3>
      <div class="karma-types">
        <div class="karma-type positive">
          <span class="icon">⬆️</span>
          <span class="label">Positive</span>
          <span class="value">{{ positiveKarma }}</span>
        </div>
        <div class="karma-type negative">
          <span class="icon">⬇️</span>
          <span class="label">Negative</span>
          <span class="value">{{ negativeKarma }}</span>
        </div>
      </div>
    </div>

    <div class="karma-history">
      <h3>Recent Karma Activity</h3>
      <div class="history-list">
        <div v-for="record in karmaHistory" :key="record.id" class="history-item">
          <div :class="['karma-change', record.type]">
            {{ record.type === 'positive' ? '+' : '-' }}{{ Math.abs(record.amount) }}
          </div>
          <div class="karma-details">
            <p class="karma-source">{{ record.source }}</p>
            <span class="karma-time">{{ formatDate(record.date) }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useOASIS } from '../composables/useOASIS';

const { client } = useOASIS();
const totalKarma = ref(0);
const positiveKarma = ref(0);
const negativeKarma = ref(0);
const karmaHistory = ref<any[]>([]);

const karmaLevel = computed(() => Math.floor(totalKarma.value / 1000) + 1);
const karmaToNextLevel = computed(() => ((karmaLevel.value * 1000) - totalKarma.value));
const levelProgress = computed(() => ((totalKarma.value % 1000) / 1000) * 100);

onMounted(async () => {
  await loadKarmaData();
});

async function loadKarmaData() {
  try {
    const response = await client.value.get('/avatar/karma');
    totalKarma.value = response.data.total;
    positiveKarma.value = response.data.positive;
    negativeKarma.value = response.data.negative;
    karmaHistory.value = response.data.history || [];
  } catch (error) {
    console.error('Failed to load karma:', error);
  }
}

function formatDate(date: string) {
  return new Date(date).toLocaleDateString();
}
</script>

<style scoped>
.karma-header {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  color: white;
  padding: 32px;
  border-radius: 12px;
  margin-bottom: 24px;
}

.karma-total h2 {
  font-size: 48px;
  margin: 0;
}

.level-badge {
  background: rgba(255,255,255,0.2);
  padding: 4px 12px;
  border-radius: 16px;
  font-size: 14px;
  font-weight: 600;
}

.progress-bar {
  height: 8px;
  background: rgba(255,255,255,0.2);
  border-radius: 4px;
  margin: 12px 0;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: white;
  transition: width 0.3s;
}

.karma-types {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 16px;
  margin-top: 16px;
}

.karma-type {
  padding: 20px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.karma-type.positive { background: #e8f5e9; }
.karma-type.negative { background: #ffebee; }

.history-item {
  display: flex;
  gap: 16px;
  padding: 16px;
  border-bottom: 1px solid #eee;
}

.karma-change {
  font-weight: 600;
  padding: 4px 8px;
  border-radius: 4px;
}

.karma-change.positive { color: #27ae60; background: #e8f5e9; }
.karma-change.negative { color: #e74c3c; background: #ffebee; }
</style>



