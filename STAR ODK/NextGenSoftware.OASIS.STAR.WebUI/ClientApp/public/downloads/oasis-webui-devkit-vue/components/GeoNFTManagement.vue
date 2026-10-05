<template>
  <div class="oasis-geonft-management">
    <div class="map-container" ref="mapContainer"></div>
    <div class="geonft-sidebar">
      <h3>GeoNFTs Nearby</h3>
      <button @click="createGeoNFT" class="create-btn">Create GeoNFT</button>
      <div class="geonft-list">
        <div v-for="geonft in nearbyGeoNFTs" :key="geonft.id" class="geonft-item" @click="selectGeoNFT(geonft)">
          <div class="geonft-icon">📍</div>
          <div class="geonft-info">
            <h4>{{ geonft.name }}</h4>
            <p>{{ geonft.distance }}m away</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useOASIS } from '../composables/useOASIS';

const { client } = useOASIS();
const nearbyGeoNFTs = ref<any[]>([]);
const mapContainer = ref<HTMLElement | null>(null);

onMounted(async () => {
  await loadNearbyGeoNFTs();
  initMap();
});

async function loadNearbyGeoNFTs() {
  const response = await client.value.get('/geonft/nearby');
  nearbyGeoNFTs.value = response.data;
}

function initMap() {
  // Map initialization logic
}

function createGeoNFT() {
  console.log('Create GeoNFT');
}

function selectGeoNFT(geonft: any) {
  console.log('Select GeoNFT:', geonft);
}
</script>

<style scoped>
.oasis-geonft-management { display: flex; height: 600px; }
.map-container { flex: 1; background: #e0e0e0; }
.geonft-sidebar { width: 300px; padding: 20px; background: white; overflow-y: auto; }
.create-btn { width: 100%; margin-bottom: 20px; padding: 12px; background: #4A90E2; color: white; border: none; border-radius: 6px; cursor: pointer; }
.geonft-item { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
.geonft-item:hover { background: #f5f5f5; }
</style>



