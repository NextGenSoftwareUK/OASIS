import React, { useState } from 'react';
import { WalletCard } from './WalletCard';
import { Button } from '@/components/ui/button';
import { Plus, Download, Upload } from 'lucide-react';
import type { Wallet, ProviderType } from '@/lib/types';
import { useWalletStore } from '@/lib/store';

interface WalletGridProps {
  wallets: Partial<Record<ProviderType, Wallet[]>>;
  onSendTransaction?: (wallet: Wallet) => void;
  onImportWallet?: () => void;
  onExportWallet?: () => void;
  onCreateWallet?: () => void;
}

export const WalletGrid: React.FC<WalletGridProps> = ({
  wallets,
  onSendTransaction,
  onImportWallet,
  onExportWallet,
  onCreateWallet,
}) => {
  const [showPrivateKeys, setShowPrivateKeys] = useState<Record<string, boolean>>({});
  const { setDefaultWallet } = useWalletStore();

  const handleTogglePrivateKey = (walletId: string) => {
    setShowPrivateKeys(prev => ({
      ...prev,
      [walletId]: !prev[walletId]
    }));
  };

  const handleSetDefault = async (walletId: string, providerType: ProviderType) => {
    try {
      await setDefaultWallet(walletId, providerType);
    } catch (error) {
      console.error('Failed to set default wallet:', error);
    }
  };

  const handleCopyAddress = (address: string) => {
    // Could show a toast notification here
    console.log('Address copied:', address);
  };

  // Flatten all wallets into a single array
  const allWallets = Object.entries(wallets).flatMap(([providerType, walletList]) =>
    (walletList || []).map(wallet => ({ ...wallet, providerType: providerType as ProviderType }))
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-3xl font-bold cosmic-text">My Wallets</h2>
          <p className="text-gray-300">
            Manage your multi-chain wallets
          </p>
        </div>
        <div className="flex items-center space-x-2">
          <Button
            variant="outline"
            size="sm"
            onClick={onImportWallet}
            className="border-white/20 text-white hover:bg-white/10"
          >
            <Upload className="h-4 w-4 mr-2" />
            Import
          </Button>
          <Button
            variant="outline"
            size="sm"
            onClick={onExportWallet}
            className="border-white/20 text-white hover:bg-white/10"
          >
            <Download className="h-4 w-4 mr-2" />
            Export
          </Button>
          <Button
            onClick={onCreateWallet}
            className="cosmic-button"
          >
            <Plus className="h-4 w-4 mr-2" />
            Create Wallet
          </Button>
        </div>
      </div>

      {/* Wallet Grid */}
      {allWallets.length === 0 ? (
        <div className="text-center py-12">
          <div className="w-20 h-20 bg-gradient-to-r from-primary to-primary/60 rounded-full flex items-center justify-center mx-auto mb-6 cosmic-glow">
            <Plus className="h-10 w-10 text-white" />
          </div>
          <h3 className="text-xl font-semibold mb-3 cosmic-text">No wallets yet</h3>
          <p className="text-gray-300 mb-6">
            Create your first wallet to get started
          </p>
          <Button onClick={onCreateWallet} className="cosmic-button">
            <Plus className="h-4 w-4 mr-2" />
            Create Your First Wallet
          </Button>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {allWallets.map((wallet) => (
            <WalletCard
              key={wallet.walletId}
              wallet={wallet}
              onSend={onSendTransaction}
              onSetDefault={handleSetDefault}
              onCopyAddress={handleCopyAddress}
              showPrivateKey={showPrivateKeys[wallet.walletId] || false}
              onTogglePrivateKey={() => handleTogglePrivateKey(wallet.walletId)}
            />
          ))}
        </div>
      )}

      {/* Summary */}
      {allWallets.length > 0 && (
        <div className="cosmic-card p-6">
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6 text-center">
            <div>
              <div className="text-3xl font-bold cosmic-text">{allWallets.length}</div>
              <div className="text-sm text-gray-300">Total Wallets</div>
            </div>
            <div>
              <div className="text-3xl font-bold cosmic-text">
                {Object.keys(wallets).length}
              </div>
              <div className="text-sm text-gray-300">Blockchains</div>
            </div>
            <div>
              <div className="text-3xl font-bold cosmic-text">
                {allWallets.filter(w => w.isDefaultWallet).length}
              </div>
              <div className="text-sm text-gray-300">Default Wallets</div>
            </div>
            <div>
              <div className="text-3xl font-bold cosmic-text">
                {allWallets.reduce((sum, w) => sum + w.transactions.length, 0)}
              </div>
              <div className="text-sm text-gray-300">Total Transactions</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}; 