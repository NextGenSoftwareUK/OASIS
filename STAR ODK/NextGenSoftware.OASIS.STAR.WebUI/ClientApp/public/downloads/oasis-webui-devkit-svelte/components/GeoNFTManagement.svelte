<script lang="ts">
  import { onMount } from 'svelte';
  import { oasisStore } from '../stores/oasis';

  let geoNFTs = [];

  onMount(async () => {
    const response = await $oasisStore.client.get('/api/geonft/nearby');
    geoNFTs = response.data;
  });
</script>

<div class="geonft-management">
  <div class="map"></div>
  <div class="sidebar">
    <h3>GeoNFTs Nearby</h3>
    <button>Create GeoNFT</button>
    {#each geoNFTs as geonft}
      <div class="geonft">
        <div>📍</div>
        <div><strong>{geonft.name}</strong><br/><small>{geonft.distance}m away</small></div>
      </div>
    {/each}
  </div>
</div>

<style>
  .geonft-management { display: flex; height: 600px; }
  .map { flex: 1; background: #e0e0e0; }
  .sidebar { width: 300px; padding: 20px; background: white; overflow-y: auto; }
  button { width: 100%; padding: 12px; background: #4A90E2; color: white; border: none; border-radius: 6px; cursor: pointer; margin-bottom: 20px; }
  .geonft { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
</style>



