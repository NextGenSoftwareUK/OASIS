<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let groups = [];
  let showCreateModal = false;
  let newGroup = { name: '', description: '', emoji: '' };

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/groups');
    groups = response.data;
  });

  async function createGroup() {
    await $oasisStore.client.post('/api/groups', newGroup);
    showCreateModal = false;
    newGroup = { name: '', description: '', emoji: '' };
    const response = await $oasisStore.client.get('/api/groups');
    groups = response.data;
  }
</script>

<div class="oasis-group-management">
  <div class="header">
    <h2>My Groups</h2>
    <button on:click={() => showCreateModal = true}>Create Group</button>
  </div>
  
  <div class="groups-grid">
    {#each groups as group}
      <div class="group-card">
        <div class="emoji">{group.emoji || '👥'}</div>
        <h3>{group.name}</h3>
        <p>{group.description}</p>
        <small>{group.memberCount} members</small>
      </div>
    {/each}
  </div>

  {#if showCreateModal}
    <div class="modal" on:click={() => showCreateModal = false}>
      <div class="modal-content" on:click|stopPropagation>
        <h3>Create New Group</h3>
        <input bind:value={newGroup.name} placeholder="Group Name" />
        <textarea bind:value={newGroup.description} placeholder="Description"></textarea>
        <input bind:value={newGroup.emoji} placeholder="Emoji" maxlength="2" />
        <div class="modal-actions">
          <button on:click={createGroup}>Create</button>
          <button on:click={() => showCreateModal = false}>Cancel</button>
        </div>
      </div>
    </div>
  {/if}
</div>

<style>
  .header { display: flex; justify-content: space-between; margin-bottom: 24px; }
  .groups-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 20px; }
  .group-card { background: white; padding: 20px; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
  .emoji { font-size: 48px; }
  .modal { position: fixed; inset: 0; background: rgba(0,0,0,0.5); display: flex; align-items: center; justify-content: center; z-index: 1000; }
  .modal-content { background: white; padding: 24px; border-radius: 12px; max-width: 500px; width: 90%; }
  .modal-content input, .modal-content textarea { width: 100%; padding: 10px; margin-bottom: 12px; border: 1px solid #ddd; border-radius: 4px; }
  .modal-actions { display: flex; gap: 12px; margin-top: 20px; }
  .modal-actions button { flex: 1; padding: 10px; border: none; border-radius: 4px; cursor: pointer; background: #4A90E2; color: white; }
</style>



