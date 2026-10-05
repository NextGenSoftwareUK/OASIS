<template>
  <div class="oasis-group-management">
    <div class="group-header">
      <h2>My Groups</h2>
      <button @click="showCreateModal = true" class="create-btn">Create Group</button>
    </div>

    <div v-if="loading" class="loading">Loading groups...</div>

    <div v-else class="groups-grid">
      <div v-for="group in groups" :key="group.id" class="group-card">
        <div class="group-icon">{{ group.emoji || '👥' }}</div>
        <h3>{{ group.name }}</h3>
        <p class="group-description">{{ group.description }}</p>
        <div class="group-stats">
          <span>{{ group.memberCount }} members</span>
          <span v-if="group.isAdmin" class="admin-badge">Admin</span>
        </div>
        <div class="group-actions">
          <button @click="viewGroup(group.id)">View</button>
          <button v-if="group.isAdmin" @click="manageGroup(group.id)">Manage</button>
          <button v-else @click="leaveGroup(group.id)" class="leave-btn">Leave</button>
        </div>
      </div>
    </div>

    <!-- Create Group Modal -->
    <div v-if="showCreateModal" class="modal" @click.self="showCreateModal = false">
      <div class="modal-content">
        <h3>Create New Group</h3>
        <input v-model="newGroup.name" placeholder="Group Name" />
        <textarea v-model="newGroup.description" placeholder="Description"></textarea>
        <input v-model="newGroup.emoji" placeholder="Emoji (optional)" maxlength="2" />
        <div class="modal-actions">
          <button @click="createGroup" class="primary-btn">Create</button>
          <button @click="showCreateModal = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useOASIS } from '../composables/useOASIS';

interface Group {
  id: string;
  name: string;
  description: string;
  emoji?: string;
  memberCount: number;
  isAdmin: boolean;
}

const { client } = useOASIS();
const groups = ref<Group[]>([]);
const loading = ref(true);
const showCreateModal = ref(false);
const newGroup = ref({ name: '', description: '', emoji: '' });

onMounted(async () => {
  await loadGroups();
});

async function loadGroups() {
  try {
    loading.value = true;
    const response = await client.value.get('/avatar/groups');
    groups.value = response.data;
  } catch (error) {
    console.error('Failed to load groups:', error);
  } finally {
    loading.value = false;
  }
}

async function createGroup() {
  try {
    await client.value.post('/avatar/groups', newGroup.value);
    showCreateModal.value = false;
    newGroup.value = { name: '', description: '', emoji: '' };
    await loadGroups();
  } catch (error) {
    console.error('Failed to create group:', error);
  }
}

async function leaveGroup(groupId: string) {
  if (confirm('Are you sure you want to leave this group?')) {
    try {
      await client.value.post(`/avatar/groups/${groupId}/leave`);
      await loadGroups();
    } catch (error) {
      console.error('Failed to leave group:', error);
    }
  }
}

function viewGroup(groupId: string) {
  // Navigate to group detail view
  console.log('View group:', groupId);
}

function manageGroup(groupId: string) {
  // Navigate to group management
  console.log('Manage group:', groupId);
}
</script>

<style scoped>
.oasis-group-management {
  padding: 20px;
}

.group-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.create-btn {
  background: #4A90E2;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
}

.groups-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 20px;
}

.group-card {
  background: white;
  border-radius: 12px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  transition: transform 0.2s;
}

.group-card:hover {
  transform: translateY(-4px);
}

.group-icon {
  font-size: 48px;
  text-align: center;
  margin-bottom: 12px;
}

.group-card h3 {
  margin: 0 0 8px 0;
  font-size: 18px;
  font-weight: 600;
}

.group-description {
  color: #666;
  font-size: 14px;
  margin: 0 0 12px 0;
  min-height: 40px;
}

.group-stats {
  display: flex;
  gap: 12px;
  margin-bottom: 12px;
  font-size: 13px;
  color: #888;
}

.admin-badge {
  background: #f39c12;
  color: white;
  padding: 2px 8px;
  border-radius: 12px;
  font-size: 11px;
}

.group-actions {
  display: flex;
  gap: 8px;
}

.group-actions button {
  flex: 1;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  cursor: pointer;
  background: white;
}

.leave-btn {
  color: #e74c3c;
  border-color: #e74c3c !important;
}

.modal {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  background: rgba(0,0,0,0.5);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}

.modal-content {
  background: white;
  border-radius: 12px;
  padding: 24px;
  max-width: 500px;
  width: 90%;
}

.modal-content h3 {
  margin: 0 0 20px 0;
}

.modal-content input,
.modal-content textarea {
  width: 100%;
  padding: 10px;
  margin-bottom: 12px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-family: inherit;
}

.modal-content textarea {
  min-height: 80px;
  resize: vertical;
}

.modal-actions {
  display: flex;
  gap: 12px;
  margin-top: 20px;
}

.modal-actions button {
  flex: 1;
  padding: 10px;
  border: none;
  border-radius: 4px;
  cursor: pointer;
  font-weight: 600;
}

.primary-btn {
  background: #4A90E2;
  color: white;
}
</style>



