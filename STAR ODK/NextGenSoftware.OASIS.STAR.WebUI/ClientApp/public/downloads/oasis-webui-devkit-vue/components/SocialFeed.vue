<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  feedType?: 'global' | 'friends' | 'avatar';
  avatarId?: string;
  theme?: 'light' | 'dark';
}

const props = withDefaults(defineProps<Props>(), {
  feedType: 'global',
  theme: 'dark'
});

const posts = ref<any[]>([]);
const loading = ref(false);
const client = new OASISClient();

onMounted(async () => {
  await loadFeed();
});

async function loadFeed() {
  loading.value = true;
  try {
    posts.value = await client.getSocialFeed(props.feedType, props.avatarId);
  } finally {
    loading.value = false;
  }
}

async function likePost(postId: string) {
  try {
    await client.likePost(postId);
    await loadFeed();
  } catch (error) {
    console.error('Error liking post:', error);
  }
}
</script>

<template>
  <div class="oasis-social-feed" :class="[`oasis-social-feed--${theme}`]">
    <h3>Social Feed</h3>

    <div v-if="loading" class="loading">Loading feed...</div>

    <div v-else class="posts">
      <div v-for="post in posts" :key="post.id" class="post">
        <div class="post-header">
          <img :src="post.avatarImage" :alt="post.avatarName" class="avatar" />
          <div class="post-info">
            <h4>{{ post.avatarName }}</h4>
            <span class="post-time">{{ new Date(post.timestamp).toLocaleString() }}</span>
          </div>
        </div>

        <div class="post-content">{{ post.content }}</div>

        <div v-if="post.imageUrl" class="post-image">
          <img :src="post.imageUrl" :alt="post.content" />
        </div>

        <div class="post-actions">
          <button @click="likePost(post.id)">
            ❤️ {{ post.likes || 0 }}
          </button>
          <button>💬 {{ post.comments || 0 }}</button>
          <button>🔄 {{ post.shares || 0 }}</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.oasis-social-feed {
  padding: 1.5rem;
  background: white;
  color: #333;
  border-radius: 12px;
}

.oasis-social-feed--dark {
  background: #1a1a1a;
  color: white;
}

.posts {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}

.post {
  background: #f5f5f5;
  border-radius: 8px;
  padding: 1rem;
}

.oasis-social-feed--dark .post {
  background: #2a2a2a;
}

.post-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 1rem;
}

.avatar {
  width: 40px;
  height: 40px;
  border-radius: 50%;
}

.post-info h4 {
  margin: 0;
  font-size: 1rem;
}

.post-time {
  font-size: 0.875rem;
  opacity: 0.7;
}

.post-content {
  margin-bottom: 1rem;
  line-height: 1.5;
}

.post-image img {
  width: 100%;
  border-radius: 8px;
  margin-bottom: 1rem;
}

.post-actions {
  display: flex;
  gap: 1rem;
}

.post-actions button {
  background: none;
  border: none;
  cursor: pointer;
  padding: 0.5rem;
  opacity: 0.7;
  transition: opacity 0.2s;
}

.post-actions button:hover {
  opacity: 1;
}
</style>
