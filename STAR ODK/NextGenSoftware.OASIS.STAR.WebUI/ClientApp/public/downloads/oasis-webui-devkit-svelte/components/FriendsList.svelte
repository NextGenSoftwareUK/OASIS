<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let friends = [];

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/friends');
    friends = response.data;
  });
</script>

<div class="friends-list">
  {#each friends as friend}
    <div class="friend">
      <div class="avatar"></div>
      <div class="info"><strong>{friend.username}</strong><br/><small>Level {friend.level}</small></div>
      <div class="status {friend.online ? 'online' : 'offline'}"></div>
    </div>
  {/each}
</div>

<style>
  .friend { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; align-items: center; }
  .avatar { width: 48px; height: 48px; border-radius: 50%; background: #e3f2fd; }
  .info { flex: 1; }
  .status { width: 12px; height: 12px; border-radius: 50%; }
  .status.online { background: #27ae60; }
  .status.offline { background: #95a5a6; }
</style>



