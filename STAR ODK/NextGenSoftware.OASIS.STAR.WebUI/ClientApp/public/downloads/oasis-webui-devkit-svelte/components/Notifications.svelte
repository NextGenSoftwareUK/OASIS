<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let notifications = [];

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/notifications');
    notifications = response.data;
  });

  async function markAsRead(id: string) {
    await $oasisStore.client.post(`/api/notifications/${id}/read`);
    notifications = notifications.map(n => n.id === id ? { ...n, read: true } : n);
  }
</script>

<div class="oasis-notifications">
  <h3>Notifications</h3>
  {#each notifications as notification}
    <div class="notification {notification.read ? '' : 'unread'}" on:click={() => markAsRead(notification.id)}>
      <div class="icon">{notification.type === 'karma' ? '⭐' : '📢'}</div>
      <div class="content">
        <h4>{notification.title}</h4>
        <p>{notification.message}</p>
      </div>
    </div>
  {/each}
</div>

<style>
  .notification { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
  .notification:hover { background: #f5f5f5; }
  .notification.unread { background: #e3f2fd; }
  .icon { font-size: 24px; }
  .content h4 { margin: 0 0 4px 0; font-size: 14px; }
  .content p { margin: 0; font-size: 13px; color: #666; }
</style>



