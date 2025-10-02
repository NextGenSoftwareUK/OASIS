<template>
  <div class="oasis-notifications">
    <div class="notification-header">
      <h3>Notifications</h3>
      <button @click="markAllAsRead" class="mark-read-btn">Mark All Read</button>
    </div>
    
    <div v-if="loading" class="loading">Loading notifications...</div>
    
    <div v-else-if="notifications.length === 0" class="empty-state">
      <p>No notifications</p>
    </div>
    
    <div v-else class="notification-list">
      <div
        v-for="notification in notifications"
        :key="notification.id"
        :class="['notification-item', { unread: !notification.read }]"
        @click="markAsRead(notification.id)"
      >
        <div class="notification-icon">
          <span>{{ getIcon(notification.type) }}</span>
        </div>
        <div class="notification-content">
          <h4>{{ notification.title }}</h4>
          <p>{{ notification.message }}</p>
          <span class="notification-time">{{ formatTime(notification.timestamp) }}</span>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useOASIS } from '../composables/useOASIS';

interface Notification {
  id: string;
  type: 'karma' | 'message' | 'quest' | 'nft' | 'system';
  title: string;
  message: string;
  timestamp: Date;
  read: boolean;
}

const { client } = useOASIS();
const notifications = ref<Notification[]>([]);
const loading = ref(true);

onMounted(async () => {
  await loadNotifications();
});

async function loadNotifications() {
  try {
    loading.value = true;
    const response = await client.value.get('/avatar/notifications');
    notifications.value = response.data;
  } catch (error) {
    console.error('Failed to load notifications:', error);
  } finally {
    loading.value = false;
  }
}

async function markAsRead(id: string) {
  try {
    await client.value.post(`/avatar/notifications/${id}/read`);
    const notification = notifications.value.find(n => n.id === id);
    if (notification) notification.read = true;
  } catch (error) {
    console.error('Failed to mark notification as read:', error);
  }
}

async function markAllAsRead() {
  try {
    await client.value.post('/avatar/notifications/read-all');
    notifications.value.forEach(n => n.read = true);
  } catch (error) {
    console.error('Failed to mark all as read:', error);
  }
}

function getIcon(type: Notification['type']): string {
  const icons = {
    karma: '⭐',
    message: '💬',
    quest: '🎯',
    nft: '🎨',
    system: 'ℹ️'
  };
  return icons[type] || '📢';
}

function formatTime(timestamp: Date): string {
  const now = new Date();
  const diff = now.getTime() - new Date(timestamp).getTime();
  const minutes = Math.floor(diff / 60000);
  const hours = Math.floor(diff / 3600000);
  const days = Math.floor(diff / 86400000);
  
  if (minutes < 60) return `${minutes}m ago`;
  if (hours < 24) return `${hours}h ago`;
  return `${days}d ago`;
}
</script>

<style scoped>
.oasis-notifications {
  background: white;
  border-radius: 8px;
  padding: 20px;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
}

.notification-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 20px;
}

.mark-read-btn {
  background: #4A90E2;
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 4px;
  cursor: pointer;
}

.notification-list {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.notification-item {
  display: flex;
  gap: 12px;
  padding: 12px;
  border-radius: 6px;
  cursor: pointer;
  transition: background 0.2s;
}

.notification-item:hover {
  background: #f5f5f5;
}

.notification-item.unread {
  background: #e3f2fd;
}

.notification-icon {
  font-size: 24px;
}

.notification-content {
  flex: 1;
}

.notification-content h4 {
  margin: 0 0 4px 0;
  font-size: 14px;
  font-weight: 600;
}

.notification-content p {
  margin: 0 0 4px 0;
  font-size: 13px;
  color: #666;
}

.notification-time {
  font-size: 12px;
  color: #999;
}

.empty-state, .loading {
  text-align: center;
  padding: 40px;
  color: #999;
}
</style>



