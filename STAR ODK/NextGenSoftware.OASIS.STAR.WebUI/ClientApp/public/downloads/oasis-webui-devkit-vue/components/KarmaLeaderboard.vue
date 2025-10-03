<template>
  <div :class="['oasis-karma-leaderboard', `oasis-karma-leaderboard--${theme}`]">
    <div class="leaderboard-header">
      <h3>Karma Leaderboard</h3>
      <span class="time-range">{{ timeRange.charAt(0).toUpperCase() + timeRange.slice(1) }}</span>
    </div>

    <div class="leaderboard-list">
      <div v-if="loading">Loading leaderboard...</div>
      <div 
        v-for="(entry, index) in leaderboard" 
        :key="entry.avatarId"
        :class="['leaderboard-item', { 'current-user': highlightCurrentUser && entry.avatarId === currentAvatarId }]">
        <span class="rank">{{ getMedalEmoji(index + 1) }}</span>
        <img :src="entry.avatarImage || '/default-avatar.png'" :alt="entry.username" class="avatar" />
        <div class="user-info">
          <h4>{{ entry.username }}</h4>
          <span class="level">Level {{ entry.level }}</span>
        </div>
        <div class="karma-score">
          <span class="value">{{ entry.karma.toLocaleString() }}</span>
          <span class="label">karma</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  limit?: number;
  timeRange?: 'day' | 'week' | 'month' | 'all';
  highlightCurrentUser?: boolean;
  currentAvatarId?: string;
  theme?: 'light' | 'dark';
}

const props = withDefaults(defineProps<Props>(), {
  limit: 10,
  timeRange: 'week',
  highlightCurrentUser: true,
  theme: 'dark'
});

const leaderboard = ref<any[]>([]);
const loading = ref(false);
const client = new OASISClient();

onMounted(async () => {
  loading.value = true;
  leaderboard.value = await client.getKarmaLeaderboard(props.timeRange, props.limit);
  loading.value = false;
});

const getMedalEmoji = (rank: number): string => {
  if (rank === 1) return '🥇';
  if (rank === 2) return '🥈';
  if (rank === 3) return '🥉';
  return `#${rank}`;
};
</script>

<style scoped>
.leaderboard-item { display: flex; align-items: center; gap: 1rem; padding: 1rem; border: 1px solid #ddd; border-radius: 8px; margin-bottom: 0.5rem; }
.leaderboard-item.current-user { background-color: #e3f2fd; }
.avatar { width: 40px; height: 40px; border-radius: 50%; }
</style>

