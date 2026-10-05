import { ProviderType, Wallet } from './types';
import { oasisWalletAPI } from './api';
import { useWalletStore } from './store';
import { normalizeProviderType } from './providerTypeMapper';

/**
 * Remove wallets of specific provider types (e.g., Default, LocalFileOASIS)
 * by saving an empty array for those provider types
 */
export async function removeWalletsByProviderType(
  avatarId: string,
  providerTypesToRemove: ProviderType[]
): Promise<{ success: boolean; message: string }> {
  try {
    // Load current wallets
    const currentWalletsResult = await oasisWalletAPI.loadWalletsById(avatarId);
    
    if (currentWalletsResult.isError) {
      return {
        success: false,
        message: currentWalletsResult.message || 'Failed to load wallets',
      };
    }

    const currentWallets = currentWalletsResult.result || {};
    const updatedWallets: Partial<Record<ProviderType, Wallet[]>> = { ...currentWallets };

    // Remove wallets for specified provider types
    for (const providerType of providerTypesToRemove) {
      updatedWallets[providerType] = [];
    }

    // Save the updated wallets (empty arrays for removed types)
    const saveResult = await oasisWalletAPI.saveWalletsById(avatarId, updatedWallets as Record<ProviderType, Wallet[]>);
    
    if (saveResult.isError) {
      return {
        success: false,
        message: saveResult.message || 'Failed to remove wallets',
      };
    }

    // Reload wallets to update the store
    const walletStore = useWalletStore.getState();
    await walletStore.loadWallets(avatarId);

    return {
      success: true,
      message: `Successfully removed ${providerTypesToRemove.length} wallet type(s)`,
    };
  } catch (error) {
    return {
      success: false,
      message: error instanceof Error ? error.message : 'Unknown error occurred',
    };
  }
}

/**
 * Filter out wallets with provider types that should be hidden (e.g., Default, LocalFileOASIS)
 */
export function filterUniversalWallets(
  wallets: Partial<Record<ProviderType, Wallet[]>>
): Partial<Record<ProviderType, Wallet[]>> {
  const filtered: Partial<Record<ProviderType, Wallet[]>> = {};
  const hiddenTypes = [
    ProviderType.Default, 
    ProviderType.LocalFileOASIS, 
    ProviderType.MongoDBOASIS,
    'Default' as ProviderType,
    'LocalFileOASIS' as ProviderType,
    'MongoDBOASIS' as ProviderType,
  ];
  
  for (const [providerType, walletList] of Object.entries(wallets)) {
    if (walletList && walletList.length > 0) {
      const type = normalizeProviderType(providerType as ProviderType);
      // Also check individual wallets - they might have different providerTypes
      const filteredWallets = walletList.filter(wallet => {
        const walletProviderType = normalizeProviderType(wallet.providerType || type);
        const isHidden = hiddenTypes.includes(walletProviderType as ProviderType) || 
                        hiddenTypes.includes(type);
        if (isHidden) {
          console.log('Filtering out wallet:', walletProviderType, wallet.walletId);
        }
        return !isHidden;
      });
      
      if (filteredWallets.length > 0) {
        filtered[type] = filteredWallets;
      }
    }
  }
  
  return filtered;
}

