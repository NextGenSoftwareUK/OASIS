<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let holons = [];
  let name = '';
  let holonType = '';

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/data/holons');
    holons = response.data;
  });

  async function createHolon() {
    await $oasisStore.client.post('/api/data/holons', { name, holonType });
    name = '';
    holonType = '';
    const response = await $oasisStore.client.get('/api/data/holons');
    holons = response.data;
  }
</script>

<div class="data-management">
  <div class="create-form">
    <input bind:value={name} placeholder="Holon Name" />
    <input bind:value={holonType} placeholder="Type" />
    <button on:click={createHolon}>Create</button>
  </div>
  {#each holons as holon}
    <div class="holon-card">
      <h3>{holon.name}</h3>
      <p>Type: {holon.holonType}</p>
    </div>
  {/each}
</div>

<style>
  .create-form { display: flex; gap: 8px; margin-bottom: 20px; }
  input { flex: 1; padding: 10px; border: 1px solid #ddd; border-radius: 4px; }
  button { padding: 10px 20px; background: #27ae60; color: white; border: none; border-radius: 4px; cursor: pointer; }
  .holon-card { background: white; padding: 16px; margin-bottom: 12px; border-radius: 8px; }
</style>



