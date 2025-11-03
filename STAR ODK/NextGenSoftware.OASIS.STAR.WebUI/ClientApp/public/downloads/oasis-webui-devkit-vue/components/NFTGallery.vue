<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { OASISClient } from '@oasis/api-client';

interface Props {
  avatarId: string;
  columns?: number;
  theme?: 'light' | 'dark';
}

const props = withDefaults(defineProps<Props>(), {
  columns: 3,
  theme: 'dark'
});

const emit = defineEmits<{
  select: [nft: any];
}>();

const nfts = ref<any[]>([]);
const loading = ref(false);
const client = new OASISClient();

onMounted(async () => {
  await loadNFTs();
});

async function loadNFTs() {
  loading.value = true;
  try {
    nfts.value = await client.getNFTs(props.avatarId);
  } finally {
    loading.value = false;
  }
}

function selectNFT(nft: any) {
  emit('select', nft);
}
</script>

<template>
  <div class="oasis-nft-gallery" :class="[`oasis-nft-gallery--${theme}`]">
    <div v-if="loading" class="loading">Loading NFTs...</div>
    
    <div
      v-else
      class="nft-grid"
      :style="{ gridTemplateColumns: `repeat(${columns}, 1fr)` }"
    >
      <div
        v-for="nft in nfts"
        :key="nft.id"
        class="nft-item"
        @click="selectNFT(nft)"
      >
        <img :src="nft.imageUrl" :alt="nft.name" class="nft-image" />
        <div class="nft-info">
          <h4 class="nft-name">{{ nft.name }}</h4>
          <p v-if="nft.collection" class="nft-collection">{{ nft.collection }}</p>
          <p class="nft-price">{{ nft.price || 0 }} OASIS</p>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.oasis-nft-gallery {
  padding: 1rem;
}

.nft-grid {
  display: grid;
  gap: 1rem;
}

.nft-item {
  background: white;
  border: 1px solid #ddd;
  border-radius: 8px;
  overflow: hidden;
  cursor: pointer;
  transition: transform 0.2s;
}

.oasis-nft-gallery--dark .nft-item {
  background: #2a2a2a;
  border-color: #3a3a3a;
  color: white;
}

.nft-item:hover {
  transform: translateY(-4px);
}

.nft-image {
  width: 100%;
  height: 200px;
  object-fit: cover;
}

.nft-info {
  padding: 1rem;
}

.nft-name {
  font-weight: bold;
  margin: 0 0 0.5rem 0;
}

.nft-collection {
  font-size: 0.875rem;
  opacity: 0.7;
  margin: 0 0 0.5rem 0;
}

.nft-price {
  color: #00bcd4;
  font-weight: bold;
  margin: 0;
}

.loading {
  text-align: center;
  padding: 2rem;
}
</style>
