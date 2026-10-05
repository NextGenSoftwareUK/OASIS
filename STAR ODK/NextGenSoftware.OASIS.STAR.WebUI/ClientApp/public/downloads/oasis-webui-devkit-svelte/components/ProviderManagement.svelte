<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let providers = [];

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/providers');
    providers = response.data;
  });

  async function toggleProvider(id: string) {
    await $oasisStore.client.post(`/api/providers/${id}/toggle`);
    const response = await $oasisStore.client.get('/api/providers');
    providers = response.data;
  }
</script>

<div class="provider-management">
  {#each providers as provider}
    <div class="provider-card">
      <div>
        <h3>{provider.name}</h3>
        <span class="status {provider.isActive ? 'active' : 'inactive'}">{provider.isActive ? 'Active' : 'Inactive'}</span>
      </div>
      <button on:click={() => toggleProvider(provider.id)}>Toggle</button>
    </div>
  {/each}
</div>

<style>
  .provider-card { display: flex; justify-content: space-between; align-items: center; padding: 16px; background: white; margin-bottom: 12px; border-radius: 8px; }
  .status { padding: 4px 12px; border-radius: 12px; font-size: 12px; font-weight: 600; }
  .status.active { background: #d4edda; color: #155724; }
  .status.inactive { background: #f8d7da; color: #721c24; }
  button { padding: 8px 16px; border: none; border-radius: 4px; cursor: pointer; background: #4A90E2; color: white; }
</style>



