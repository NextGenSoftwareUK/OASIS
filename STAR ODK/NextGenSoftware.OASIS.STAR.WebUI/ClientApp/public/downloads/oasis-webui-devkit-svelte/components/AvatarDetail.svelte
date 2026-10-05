<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  export let avatarId: string;
  let avatar = null;

  onMount(async () => {
    const response = await $oasisStore.client.get(`/api/avatar/${avatarId}`);
    avatar = response.data;
  });
</script>

{#if avatar}
  <div class="avatar-detail">
    <div class="header">
      <div class="avatar"></div>
      <div>
        <h2>{avatar.username}</h2>
        <p>{avatar.email}</p>
        <p>Level {avatar.level}</p>
      </div>
    </div>
    <div class="stats">
      <div class="stat"><div class="value">{avatar.karma}</div><div>Karma</div></div>
      <div class="stat"><div class="value">{avatar.nftCount || 0}</div><div>NFTs</div></div>
    </div>
  </div>
{/if}

<style>
  .avatar-detail { background: white; padding: 24px; border-radius: 12px; }
  .header { display: flex; gap: 20px; margin-bottom: 20px; }
  .avatar { width: 100px; height: 100px; border-radius: 50%; background: linear-gradient(135deg, #667eea, #764ba2); }
  .stats { display: grid; grid-template-columns: repeat(2, 1fr); gap: 16px; }
  .stat { text-align: center; padding: 16px; background: #f5f5f5; border-radius: 8px; }
  .value { font-size: 24px; font-weight: 600; color: #667eea; }
</style>



