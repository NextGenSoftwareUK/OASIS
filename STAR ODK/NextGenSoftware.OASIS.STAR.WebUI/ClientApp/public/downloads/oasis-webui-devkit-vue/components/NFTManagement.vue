<template>
  <div class="oasis-nft-management">
    <div class="nft-header">
      <h2>My NFTs</h2>
      <div class="header-actions">
        <button @click="showMintModal = true" class="mint-btn">Mint NFT</button>
        <select v-model="filterChain" class="chain-filter">
          <option value="">All Chains</option>
          <option value="ethereum">Ethereum</option>
          <option value="polygon">Polygon</option>
          <option value="solana">Solana</option>
        </select>
      </div>
    </div>

    <div v-if="loading" class="loading">Loading NFTs...</div>

    <div v-else class="nft-grid">
      <div v-for="nft in filteredNFTs" :key="nft.id" class="nft-card">
        <div class="nft-image">
          <img :src="nft.image" :alt="nft.name" />
        </div>
        <div class="nft-info">
          <h3>{{ nft.name }}</h3>
          <p class="nft-collection">{{ nft.collection }}</p>
          <div class="nft-meta">
            <span class="chain-badge">{{ nft.chain }}</span>
            <span class="token-id">#{{ nft.tokenId }}</span>
          </div>
          <div class="nft-actions">
            <button @click="viewNFT(nft)">View</button>
            <button @click="transferNFT(nft)">Transfer</button>
            <button @click="listNFT(nft)" class="list-btn">List for Sale</button>
          </div>
        </div>
      </div>
    </div>

    <!-- Mint Modal -->
    <div v-if="showMintModal" class="modal" @click.self="showMintModal = false">
      <div class="modal-content">
        <h3>Mint New NFT</h3>
        <input v-model="newNFT.name" placeholder="NFT Name" />
        <textarea v-model="newNFT.description" placeholder="Description"></textarea>
        <input v-model="newNFT.imageUrl" placeholder="Image URL" />
        <select v-model="newNFT.chain">
          <option value="ethereum">Ethereum</option>
          <option value="polygon">Polygon</option>
          <option value="solana">Solana</option>
        </select>
        <div class="modal-actions">
          <button @click="mintNFT" class="primary-btn">Mint NFT</button>
          <button @click="showMintModal = false">Cancel</button>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useOASIS } from '../composables/useOASIS';

interface NFT {
  id: string;
  name: string;
  collection: string;
  image: string;
  chain: string;
  tokenId: string;
}

const { client } = useOASIS();
const nfts = ref<NFT[]>([]);
const loading = ref(true);
const filterChain = ref('');
const showMintModal = ref(false);
const newNFT = ref({ name: '', description: '', imageUrl: '', chain: 'ethereum' });

const filteredNFTs = computed(() => {
  if (!filterChain.value) return nfts.value;
  return nfts.value.filter(nft => nft.chain === filterChain.value);
});

onMounted(async () => {
  await loadNFTs();
});

async function loadNFTs() {
  try {
    loading.value = true;
    const response = await client.value.get('/avatar/nfts');
    nfts.value = response.data;
  } catch (error) {
    console.error('Failed to load NFTs:', error);
  } finally {
    loading.value = false;
  }
}

async function mintNFT() {
  try {
    await client.value.post('/nft/mint', newNFT.value);
    showMintModal.value = false;
    newNFT.value = { name: '', description: '', imageUrl: '', chain: 'ethereum' };
    await loadNFTs();
  } catch (error) {
    console.error('Failed to mint NFT:', error);
  }
}

function viewNFT(nft: NFT) {
  console.log('View NFT:', nft);
}

function transferNFT(nft: NFT) {
  console.log('Transfer NFT:', nft);
}

function listNFT(nft: NFT) {
  console.log('List NFT for sale:', nft);
}
</script>

<style scoped>
.nft-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.header-actions {
  display: flex;
  gap: 12px;
}

.mint-btn {
  background: #8b5cf6;
  color: white;
  border: none;
  padding: 10px 20px;
  border-radius: 6px;
  cursor: pointer;
  font-weight: 600;
}

.chain-filter {
  padding: 10px;
  border: 1px solid #ddd;
  border-radius: 6px;
}

.nft-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 24px;
}

.nft-card {
  background: white;
  border-radius: 12px;
  overflow: hidden;
  box-shadow: 0 2px 8px rgba(0,0,0,0.1);
  transition: transform 0.2s;
}

.nft-card:hover {
  transform: translateY(-4px);
}

.nft-image {
  aspect-ratio: 1;
  overflow: hidden;
  background: #f5f5f5;
}

.nft-image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.nft-info {
  padding: 16px;
}

.nft-info h3 {
  margin: 0 0 4px 0;
  font-size: 18px;
}

.nft-collection {
  color: #666;
  font-size: 14px;
  margin: 0 0 12px 0;
}

.nft-meta {
  display: flex;
  gap: 8px;
  margin-bottom: 12px;
}

.chain-badge {
  background: #e3f2fd;
  color: #1976d2;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 12px;
}

.token-id {
  color: #999;
  font-size: 12px;
}

.nft-actions {
  display: flex;
  gap: 8px;
}

.nft-actions button {
  flex: 1;
  padding: 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  cursor: pointer;
  background: white;
  font-size: 13px;
}

.list-btn {
  background: #f39c12 !important;
  color: white !important;
  border: none !important;
}
</style>



