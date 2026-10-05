<template>
  <div class="oasis-achievements">
    <div class="achievements-header">
      <h2>Achievements & Badges</h2>
      <div class="stats">
        <span>{{ unlockedCount }}/{{ totalCount }} Unlocked</span>
        <div class="progress-bar">
          <div class="progress" :style="{ width: progressPercent + '%' }"></div>
        </div>
      </div>
    </div>

    <div class="filter-tabs">
      <button 
        v-for="tab in tabs" 
        :key="tab.id"
        @click="activeTab = tab.id"
        :class="{ active: activeTab === tab.id }"
      >
        {{ tab.label }}
      </button>
    </div>

    <div class="achievements-grid">
      <div 
        v-for="achievement in filteredAchievements" 
        :key="achievement.id"
        :class="['achievement-card', { locked: !achievement.unlocked }]"
      >
        <div class="achievement-icon">
          {{ achievement.icon }}
          <div v-if="achievement.unlocked" class="unlock-badge">✓</div>
        </div>
        <div class="achievement-info">
          <h3>{{ achievement.name }}</h3>
          <p>{{ achievement.description }}</p>
          <div v-if="achievement.unlocked" class="unlock-date">
            Unlocked: {{ formatDate(achievement.unlockedAt) }}
          </div>
          <div v-else class="achievement-progress">
            <div class="progress-text">
              {{ achievement.progress }}/{{ achievement.requirement }}
            </div>
            <div class="progress-bar small">
              <div class="progress" :style="{ width: (achievement.progress / achievement.requirement * 100) + '%' }"></div>
            </div>
          </div>
          <div class="achievement-reward">
            <span class="karma-reward">+{{ achievement.karmaReward }} Karma</span>
            <span v-if="achievement.rarity" :class="['rarity', achievement.rarity]">
              {{ achievement.rarity }}
            </span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useOASIS } from '../composables/useOASIS';

interface Achievement {
  id: string;
  name: string;
  description: string;
  icon: string;
  unlocked: boolean;
  unlockedAt?: Date;
  progress?: number;
  requirement?: number;
  karmaReward: number;
  rarity?: 'common' | 'rare' | 'epic' | 'legendary';
  category: string;
}

const { client } = useOASIS();
const achievements = ref<Achievement[]>([]);
const activeTab = ref('all');

const tabs = [
  { id: 'all', label: 'All' },
  { id: 'unlocked', label: 'Unlocked' },
  { id: 'locked', label: 'Locked' },
  { id: 'social', label: 'Social' },
  { id: 'quest', label: 'Quests' },
  { id: 'nft', label: 'NFTs' }
];

const filteredAchievements = computed(() => {
  if (activeTab.value === 'all') return achievements.value;
  if (activeTab.value === 'unlocked') return achievements.value.filter(a => a.unlocked);
  if (activeTab.value === 'locked') return achievements.value.filter(a => !a.unlocked);
  return achievements.value.filter(a => a.category === activeTab.value);
});

const unlockedCount = computed(() => achievements.value.filter(a => a.unlocked).length);
const totalCount = computed(() => achievements.value.length);
const progressPercent = computed(() => (unlockedCount.value / totalCount.value) * 100);

onMounted(async () => {
  await loadAchievements();
});

async function loadAchievements() {
  try {
    const response = await client.value.get('/avatar/achievements');
    achievements.value = response.data;
  } catch (error) {
    console.error('Failed to load achievements:', error);
  }
}

function formatDate(date: Date) {
  return new Date(date).toLocaleDateString();
}
</script>

<style scoped>
.achievements-header {
  margin-bottom: 24px;
}

.stats {
  display: flex;
  align-items: center;
  gap: 16px;
  margin-top: 12px;
}

.progress-bar {
  flex: 1;
  height: 12px;
  background: #eee;
  border-radius: 6px;
  overflow: hidden;
}

.progress {
  height: 100%;
  background: linear-gradient(90deg, #667eea, #764ba2);
  transition: width 0.3s;
}

.filter-tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 24px;
  flex-wrap: wrap;
}

.filter-tabs button {
  padding: 8px 16px;
  border: 1px solid #ddd;
  background: white;
  border-radius: 20px;
  cursor: pointer;
  transition: all 0.2s;
}

.filter-tabs button.active {
  background: #4A90E2;
  color: white;
  border-color: #4A90E2;
}

.achievements-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(320px, 1fr));
  gap: 20px;
}

.achievement-card {
  background: white;
  border-radius: 12px;
  padding: 20px;
  display: flex;
  gap: 16px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  transition: transform 0.2s;
}

.achievement-card:hover {
  transform: translateY(-2px);
}

.achievement-card.locked {
  opacity: 0.6;
}

.achievement-icon {
  font-size: 48px;
  position: relative;
}

.unlock-badge {
  position: absolute;
  top: -4px;
  right: -4px;
  width: 20px;
  height: 20px;
  background: #27ae60;
  color: white;
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 12px;
}

.achievement-info {
  flex: 1;
}

.achievement-info h3 {
  margin: 0 0 8px 0;
  font-size: 16px;
}

.achievement-info p {
  margin: 0 0 12px 0;
  font-size: 14px;
  color: #666;
}

.unlock-date {
  font-size: 12px;
  color: #27ae60;
  margin-bottom: 8px;
}

.achievement-progress {
  margin-bottom: 8px;
}

.progress-text {
  font-size: 12px;
  color: #666;
  margin-bottom: 4px;
}

.progress-bar.small {
  height: 6px;
}

.achievement-reward {
  display: flex;
  gap: 8px;
  align-items: center;
}

.karma-reward {
  background: #fff3cd;
  color: #856404;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
  font-weight: 600;
}

.rarity {
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  text-transform: uppercase;
}

.rarity.common { background: #95a5a6; color: white; }
.rarity.rare { background: #3498db; color: white; }
.rarity.epic { background: #9b59b6; color: white; }
.rarity.legendary { background: #f39c12; color: white; }
</style>



