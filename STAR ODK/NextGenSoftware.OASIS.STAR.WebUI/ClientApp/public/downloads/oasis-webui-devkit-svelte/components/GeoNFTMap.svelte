<script lang="ts">
  let showHeatmap = false;

  function centerOnUser() {
    console.log('Center on user');
  }

  function toggleLayer() {
    showHeatmap = !showHeatmap;
  }
</script>

<div class="geonft-map">
  <div class="map-controls">
    <button on:click={centerOnUser}>📍 My Location</button>
    <button on:click={toggleLayer}>{showHeatmap ? '🗺️ Normal' : '🔥 Heatmap'}</button>
  </div>
</div>

<style>
  .geonft-map { position: relative; width: 100%; height: 500px; background: #e0e0e0; border-radius: 12px; }
  .map-controls { position: absolute; top: 20px; right: 20px; display: flex; gap: 8px; }
  .map-controls button { padding: 8px 16px; background: white; border: none; border-radius: 4px; cursor: pointer; box-shadow: 0 2px 4px rgba(0,0,0,0.2); }
</style>



