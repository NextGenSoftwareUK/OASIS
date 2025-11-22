'use client';

import React, { useState, useEffect } from 'react';
import { Eye, Plus, Trash2, Download, Shield, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { PrivacyIndicator } from './PrivacyIndicator';
import { privacyAPI, type ViewingKey } from '@/lib/api/privacyApi';
import { maskViewingKey, formatViewingKeyPurpose, isViewingKeyExpired } from '@/lib/privacy/viewingKey';
import { useWalletStore } from '@/lib/store';
import { useAvatarStore } from '@/lib/avatarStore';

interface ViewingKeyManagerProps {
  walletId?: string;
  onClose?: () => void;
}

export const ViewingKeyManager: React.FC<ViewingKeyManagerProps> = ({
  walletId,
  onClose,
}) => {
  const { wallets } = useWalletStore();
  const { avatar } = useAvatarStore();
  const [viewingKeys, setViewingKeys] = useState<ViewingKey[]>([]);
  const [selectedWalletId, setSelectedWalletId] = useState(walletId || '');
  const [purpose, setPurpose] = useState<'audit' | 'compliance' | 'personal'>('audit');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const allWallets = Object.values(wallets).flat();

  useEffect(() => {
    if (selectedWalletId) {
      loadViewingKeys();
    }
  }, [selectedWalletId]);

  const loadViewingKeys = async () => {
    if (!selectedWalletId) return;

    setIsLoading(true);
    setError(null);
    try {
      const result = await privacyAPI.getViewingKeys(selectedWalletId);
      if (result.isError) {
        setError(result.message || 'Failed to load viewing keys');
      } else {
        setViewingKeys(result.result || []);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to load viewing keys');
    } finally {
      setIsLoading(false);
    }
  };

  const handleGenerate = async () => {
    if (!selectedWalletId) {
      setError('Please select a wallet');
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const result = await privacyAPI.generateViewingKey(selectedWalletId, purpose);
      if (result.isError) {
        setError(result.message || 'Failed to generate viewing key');
      } else {
        await loadViewingKeys(); // Reload keys
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate viewing key');
    } finally {
      setIsLoading(false);
    }
  };

  const handleRevoke = async (viewingKeyId: string) => {
    if (!confirm('Are you sure you want to revoke this viewing key?')) {
      return;
    }

    setIsLoading(true);
    setError(null);
    try {
      const result = await privacyAPI.revokeViewingKey(viewingKeyId);
      if (result.isError) {
        setError(result.message || 'Failed to revoke viewing key');
      } else {
        await loadViewingKeys(); // Reload keys
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to revoke viewing key');
    } finally {
      setIsLoading(false);
    }
  };

  const handleExport = (key: ViewingKey) => {
    // Export viewing key data (encrypted, never full key)
    const exportData = {
      id: key.id,
      address: key.address,
      keyHash: key.keyHash, // Only hash, never full key
      purpose: key.purpose,
      createdAt: key.createdAt,
      lastUsed: key.lastUsed,
      note: 'This is a viewing key hash. The full key is stored encrypted on the server.',
    };

    const blob = new Blob([JSON.stringify(exportData, null, 2)], { type: 'application/json' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `viewing-key-${key.id}.json`;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  };

  return (
    <div className="space-y-6 p-4 bg-zypherpunk-bg text-zypherpunk-text">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <Eye className="w-6 h-6 text-zypherpunk-accent" />
          <h2 className="text-2xl font-bold">Viewing Key Manager</h2>
        </div>
        {onClose && (
          <Button
            variant="ghost"
            onClick={onClose}
            className="text-zypherpunk-text-muted hover:text-zypherpunk-text"
          >
            Close
          </Button>
        )}
      </div>

      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
        <p className="text-sm text-zypherpunk-text-muted mb-4">
          Viewing keys allow auditors to verify transactions without revealing amounts. 
          Keys are stored encrypted and never displayed in full.
        </p>

        {/* Wallet Selection */}
        <div className="space-y-4">
          <div>
            <label className="text-sm font-medium text-zypherpunk-text-muted mb-2 block">
              Select Wallet
            </label>
            <Select value={selectedWalletId} onValueChange={setSelectedWalletId}>
              <SelectTrigger className="bg-zypherpunk-bg border-zypherpunk-border text-zypherpunk-text">
                <SelectValue placeholder="Select a wallet" />
              </SelectTrigger>
              <SelectContent className="bg-zypherpunk-surface border-zypherpunk-border">
                {allWallets.map((wallet) => (
                  <SelectItem key={wallet.walletId} value={wallet.walletId}>
                    {wallet.providerType.replace('OASIS', '')} - {wallet.walletAddress.substring(0, 8)}...
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>

          {/* Generate New Key */}
          {selectedWalletId && (
            <div className="space-y-3">
              <div>
                <label className="text-sm font-medium text-zypherpunk-text-muted mb-2 block">
                  Purpose
                </label>
                <Select
                  value={purpose}
                  onValueChange={(value) => setPurpose(value as typeof purpose)}
                >
                  <SelectTrigger className="bg-zypherpunk-bg border-zypherpunk-border text-zypherpunk-text">
                    <SelectValue />
                  </SelectTrigger>
                  <SelectContent className="bg-zypherpunk-surface border-zypherpunk-border">
                    <SelectItem value="audit">Audit</SelectItem>
                    <SelectItem value="compliance">Compliance</SelectItem>
                    <SelectItem value="personal">Personal</SelectItem>
                  </SelectContent>
                </Select>
              </div>

              <Button
                onClick={handleGenerate}
                disabled={isLoading}
                className="w-full bg-zypherpunk-primary/20 hover:bg-zypherpunk-primary/30 border border-zypherpunk-primary/40 text-zypherpunk-primary font-semibold"
              >
                <Plus className="w-4 h-4 mr-2" />
                Generate Viewing Key
              </Button>
            </div>
          )}
        </div>
      </Card>

      {/* Error Display */}
      {error && (
        <Card className="bg-red-500/10 border-red-500/50 p-4">
          <div className="flex items-center gap-2 text-red-400">
            <AlertCircle className="w-5 h-5" />
            <span>{error}</span>
          </div>
        </Card>
      )}

      {/* Viewing Keys List */}
      {selectedWalletId && (
        <Card className="bg-zypherpunk-surface border-zypherpunk-border p-4">
          <h3 className="text-lg font-semibold mb-4">Viewing Keys</h3>

          {isLoading ? (
            <div className="flex items-center justify-center p-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-zypherpunk-primary"></div>
            </div>
          ) : viewingKeys.length === 0 ? (
            <div className="text-center py-8 text-zypherpunk-text-muted">
              <Eye className="w-12 h-12 mx-auto mb-4 opacity-50" />
              <p>No viewing keys found</p>
              <p className="text-sm mt-2">Generate a viewing key to get started</p>
            </div>
          ) : (
            <div className="space-y-3">
              {viewingKeys.map((key) => (
                <div
                  key={key.id}
                  className="flex items-center justify-between p-4 bg-zypherpunk-bg rounded-lg border border-zypherpunk-border"
                >
                  <div className="flex-1">
                    <div className="flex items-center gap-2 mb-2">
                      <PrivacyIndicator type="viewing-key" level="high" size="sm" />
                      <span className="font-medium">{formatViewingKeyPurpose(key.purpose)}</span>
                      {isViewingKeyExpired(key.createdAt) && (
                        <span className="text-xs text-zypherpunk-warning">(Expired)</span>
                      )}
                    </div>
                    <div className="text-sm text-zypherpunk-text-muted space-y-1">
                      <p>Address: {key.address.substring(0, 12)}...</p>
                      <p className="font-mono text-xs">
                        Key Hash: {maskViewingKey(key.keyHash)}
                      </p>
                      <p className="text-xs">
                        Created: {new Date(key.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleExport(key)}
                      className="text-zypherpunk-accent hover:bg-zypherpunk-surface"
                    >
                      <Download className="w-4 h-4" />
                    </Button>
                    <Button
                      variant="ghost"
                      size="icon"
                      onClick={() => handleRevoke(key.id)}
                      className="text-red-400 hover:bg-red-500/10"
                    >
                      <Trash2 className="w-4 h-4" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}
    </div>
  );
};

