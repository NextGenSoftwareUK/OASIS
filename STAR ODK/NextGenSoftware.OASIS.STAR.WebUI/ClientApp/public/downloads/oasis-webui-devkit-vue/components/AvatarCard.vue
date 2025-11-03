<template>
  <div 
    :class="['oasis-avatar-card', `oasis-avatar-card--${theme}`]"
    @click="emit('click', avatar)">
    <div class="avatar-card__image">
      <img :src="avatar.image || '/default-avatar.png'" :alt="avatar.username" />
    </div>
    <div class="avatar-card__info">
      <h4 class="username">{{ avatar.username }}</h4>
      <div v-if="showKarma" class="stat">
        <span class="label">Karma:</span>
        <span class="value">{{ avatar.karma || 0 }}</span>
      </div>
      <div v-if="showLevel" class="stat">
        <span class="label">Level:</span>
        <span class="value">{{ avatar.level || 1 }}</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
interface Props {
  avatar: any;
  theme?: 'light' | 'dark';
  showKarma?: boolean;
  showLevel?: boolean;
}

withDefaults(defineProps<Props>(), {
  theme: 'dark',
  showKarma: true,
  showLevel: true
});

const emit = defineEmits<{
  click: [avatar: any];
}>();
</script>

<style scoped>
.oasis-avatar-card {
  padding: 1rem;
  border: 1px solid #ddd;
  border-radius: 8px;
  cursor: pointer;
  transition: transform 0.2s;
}
.oasis-avatar-card:hover { transform: translateY(-2px); }
.avatar-card__image img { width: 60px; height: 60px; border-radius: 50%; }
</style>

