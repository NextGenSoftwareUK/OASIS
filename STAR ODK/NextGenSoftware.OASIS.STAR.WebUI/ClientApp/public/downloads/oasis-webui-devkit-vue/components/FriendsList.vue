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

const emit = defineEmits<{
  select: [friend: any];
}>();

const friends = ref<any[]>([]);
const loading = ref(false);
const client = new OASISClient();

onMounted(async () => {
  await loadFriends();
});

async function loadFriends() {
  loading.value = true;
  try {
    friends.value = await client.getFriends(props.avatarId);
  } finally {
    loading.value = false;
  }
}

function selectFriend(friend: any) {
  emit('select', friend);
}
</script>

<template>
  <div class="oasis-friends-list" :class="[`oasis-friends-list--${theme}`]">
    <h3>Friends</h3>

    <div v-if="loading" class="loading">Loading friends...</div>

    <div v-else class="friends">
      <div
        v-for="friend in friends"
        :key="friend.id"
        class="friend-item"
        @click="selectFriend(friend)"
      >
        <img :src="friend.image" :alt="friend.username" class="friend-avatar" />
        <div class="friend-info">
          <h4>{{ friend.username }}</h4>
          <p>{{ friend.status || 'Online' }}</p>
        </div>
        <div class="friend-karma">{{ friend.karma }} 🏆</div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.oasis-friends-list {
  padding: 1.5rem;
  background: white;
  color: #333;
  border-radius: 12px;
}

.oasis-friends-list--dark {
  background: #1a1a1a;
  color: white;
}

.friends {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.friend-item {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 0.75rem;
  background: #f5f5f5;
  border-radius: 8px;
  cursor: pointer;
  transition: background 0.2s;
}

.oasis-friends-list--dark .friend-item {
  background: #2a2a2a;
}

.friend-item:hover {
  background: rgba(0, 188, 212, 0.1);
}

.friend-avatar {
  width: 50px;
  height: 50px;
  border-radius: 50%;
}

.friend-info {
  flex: 1;
}

.friend-info h4 {
  margin: 0 0 0.25rem 0;
}

.friend-info p {
  margin: 0;
  font-size: 0.875rem;
  opacity: 0.7;
}

.friend-karma {
  font-weight: bold;
  color: #00bcd4;
}
</style>
