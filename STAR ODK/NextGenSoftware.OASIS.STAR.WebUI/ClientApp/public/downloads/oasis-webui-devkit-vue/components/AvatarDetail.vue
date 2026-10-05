<template>
  <div :class="['oasis-avatar-detail', `oasis-avatar-detail--${theme}`]">
    <div class="avatar-header" v-if="avatar">
      <img :src="avatar.image || '/default-avatar.png'" :alt="avatar.username" class="avatar-image" />
      <div class="avatar-info">
        <h2>{{ avatar.username }}</h2>
        <p class="email">{{ avatar.email }}</p>
      </div>
      <button v-if="editable" @click="isEditing = !isEditing">
        {{ isEditing ? 'Cancel' : 'Edit' }}
      </button>
    </div>

    <div class="avatar-details" v-if="avatar">
      <div v-if="!isEditing">
        <div class="detail-row">
          <span class="label">Full Name:</span>
          <span class="value">{{ avatar.firstName }} {{ avatar.lastName }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Bio:</span>
          <span class="value">{{ avatar.bio || 'No bio provided' }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Member Since:</span>
          <span class="value">{{ new Date(avatar.createdDate).toLocaleDateString() }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Karma:</span>
          <span class="value karma">{{ avatar.karma || 0 }}</span>
        </div>
        <div class="detail-row">
          <span class="label">Level:</span>
          <span class="value">{{ avatar.level || 1 }}</span>
        </div>
      </div>

      <form v-if="isEditing" @submit.prevent="handleSave">
        <input type="text" placeholder="First Name" v-model="formData.firstName" />
        <input type="text" placeholder="Last Name" v-model="formData.lastName" />
        <textarea placeholder="Bio" v-model="formData.bio"></textarea>
        <button type="submit" :disabled="loading">
          {{ loading ? 'Saving...' : 'Save Changes' }}
        </button>
      </form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  avatarId: string;
  theme?: 'light' | 'dark';
  editable?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  theme: 'dark',
  editable: true
});

const emit = defineEmits<{
  update: [avatar: any];
}>();

const avatar = ref<any>(null);
const isEditing = ref(false);
const loading = ref(false);
const formData = ref<any>({});
const client = new OASISClient();

onMounted(async () => {
  avatar.value = await client.getAvatarDetail(props.avatarId);
});

const handleSave = async () => {
  loading.value = true;
  try {
    const updated = await client.updateAvatar(props.avatarId, formData.value);
    emit('update', updated);
    avatar.value = updated;
    isEditing.value = false;
  } finally {
    loading.value = false;
  }
};
</script>

<style scoped>
.oasis-avatar-detail { padding: 1.5rem; }
.avatar-header { display: flex; align-items: center; gap: 1rem; margin-bottom: 1.5rem; }
.avatar-image { width: 80px; height: 80px; border-radius: 50%; }
.detail-row { display: flex; justify-content: space-between; padding: 0.5rem 0; }
</style>

