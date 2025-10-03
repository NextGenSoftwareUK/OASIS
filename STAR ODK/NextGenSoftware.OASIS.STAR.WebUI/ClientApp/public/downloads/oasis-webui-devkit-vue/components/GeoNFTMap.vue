<template>
  <div class="geonft-map" ref="mapElement">
    <div class="map-controls">
      <button @click="centerOnUser">📍 My Location</button>
      <button @click="toggleLayer">{{ showHeatmap ? '🗺️ Normal' : '🔥 Heatmap' }}</button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';

const mapElement = ref<HTMLElement | null>(null);
const showHeatmap = ref(false);

onMounted(() => {
  // Initialize map
});

function centerOnUser() {
  console.log('Center on user');
}

function toggleLayer() {
  showHeatmap.value = !showHeatmap.value;
}
</script>

<style scoped>
.geonft-map { position: relative; width: 100%; height: 500px; background: #e0e0e0; }
.map-controls { position: absolute; top: 20px; right: 20px; display: flex; gap: 8px; }
.map-controls button { padding: 8px 16px; background: white; border: none; border-radius: 4px; cursor: pointer; box-shadow: 0 2px 4px rgba(0,0,0,0.2); }
</style>



