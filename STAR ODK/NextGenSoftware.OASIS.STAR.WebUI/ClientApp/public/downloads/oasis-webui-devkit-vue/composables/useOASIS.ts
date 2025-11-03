import { ref, computed } from 'vue';
import { OASISClient } from '@oasis/api-client';

const client = new OASISClient();
const currentAvatar = ref<any>(null);
const isAuthenticated = ref(false);

export function useOASIS() {
  const login = async (provider: string) => {
    const result = await client.authenticateWithProvider(provider);
    currentAvatar.value = result;
    isAuthenticated.value = true;
    return result;
  };

  const logout = () => {
    currentAvatar.value = null;
    isAuthenticated.value = false;
  };

  return {
    client,
    currentAvatar: computed(() => currentAvatar.value),
    isAuthenticated: computed(() => isAuthenticated.value),
    login,
    logout
  };
}

export function useKarma(avatarId: string) {
  const karma = ref<any>(null);
  const history = ref<any[]>([]);
  const loading = ref(false);

  const loadKarma = async () => {
    loading.value = true;
    karma.value = await client.getAvatarKarma(avatarId);
    loading.value = false;
  };

  const loadHistory = async () => {
    history.value = await client.getKarmaHistory(avatarId);
  };

  const addKarma = async (amount: number, reason: string) => {
    await client.addKarma(avatarId, amount, reason);
    await loadKarma();
  };

  return {
    karma: computed(() => karma.value),
    history: computed(() => history.value),
    loading: computed(() => loading.value),
    loadKarma,
    loadHistory,
    addKarma
  };
}

export function useNFTs(avatarId: string) {
  const nfts = ref<any[]>([]);
  const loading = ref(false);

  const loadNFTs = async () => {
    loading.value = true;
    nfts.value = await client.getNFTs(avatarId);
    loading.value = false;
  };

  const mint = async (nftData: any) => {
    await client.mintNFT(avatarId, nftData);
    await loadNFTs();
  };

  const transfer = async (nftId: string, toAddress: string) => {
    await client.transferNFT(nftId, toAddress);
    await loadNFTs();
  };

  return {
    nfts: computed(() => nfts.value),
    loading: computed(() => loading.value),
    loadNFTs,
    mint,
    transfer
  };
}

