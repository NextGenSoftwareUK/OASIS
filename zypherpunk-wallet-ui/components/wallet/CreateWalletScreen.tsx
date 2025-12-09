'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Loader2, Key, Eye, EyeOff, Copy, Check, AlertCircle } from 'lucide-react';
import { keysAPI } from '@/lib/keysApi';
import { oasisWalletAPI } from '@/lib/api';
import { ProviderType } from '@/lib/types';
import { useAvatarStore } from '@/lib/avatarStore';
import { useWalletStore } from '@/lib/store';
import { toastManager } from '@/lib/toast';
import { getProviderMetadata } from '@/lib/providerMeta';
import { createUnifiedWallet, generateMnemonic } from '@/lib/unifiedWallet';

interface CreateWalletScreenProps {
  onBack: () => void;
  onSuccess?: () => void;
}

type Step = 'method' | 'generate' | 'import' | 'unified';

export const CreateWalletScreen: React.FC<CreateWalletScreenProps> = ({
  onBack,
  onSuccess,
}) => {
  const { avatar, token } = useAvatarStore();
  const { loadWallets } = useWalletStore();
  const [step, setStep] = useState<Step>('method');
  const [providerType, setProviderType] = useState<ProviderType>(ProviderType.ZcashOASIS); // Default to Zcash for Zypherpunk
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  // Form fields
  const [privateKey, setPrivateKey] = useState('');
  const [publicKey, setPublicKey] = useState('');
  const [walletAddress, setWalletAddress] = useState('');
  const [showPrivateKey, setShowPrivateKey] = useState(false);
  const [copiedField, setCopiedField] = useState<string | null>(null);
  const [generatedMnemonic, setGeneratedMnemonic] = useState<string | null>(null);
  const [showMnemonic, setShowMnemonic] = useState(false);
  const [mnemonicConfirmed, setMnemonicConfirmed] = useState(false);

  const avatarId = avatar?.avatarId || avatar?.id;
  
  if (!avatarId) {
    return (
      <div className="min-h-screen bg-black text-white flex items-center justify-center px-4">
        <div className="text-center">
          <AlertCircle className="w-12 h-12 mx-auto mb-4 text-red-400" />
          <p className="text-lg mb-2">Avatar Required</p>
          <p className="text-sm text-gray-400 mb-4">Please authenticate your avatar first</p>
          <Button onClick={onBack} variant="outline">
            Go Back
          </Button>
        </div>
      </div>
    );
  }

  // Set auth token for keys API
  React.useEffect(() => {
    if (token) {
      keysAPI.setAuthToken(token);
    }
  }, [token]);

  const handleCopy = async (text: string, field: string) => {
    try {
      await navigator.clipboard.writeText(text);
      setCopiedField(field);
      setTimeout(() => setCopiedField(null), 2000);
      toastManager.success('Copied to clipboard');
    } catch (error) {
      toastManager.error('Failed to copy');
    }
  };

  const handleGenerateKeypair = async () => {
    if (!avatarId) return;

    setIsLoading(true);
    setError(null);

    try {
      const result = await keysAPI.generateKeypair(avatarId, providerType);
      
      if (result.isError) {
        setError(result.message || 'Failed to generate keypair');
        return;
      }

      if (result.result) {
        setPrivateKey(result.result.privateKey);
        setPublicKey(result.result.publicKey);
        setWalletAddress(result.result.walletAddress || '');
        setStep('import');
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to generate keypair');
    } finally {
      setIsLoading(false);
    }
  };

  const handleCreateUnifiedWallet = async () => {
    if (!avatarId) return;

    setIsLoading(true);
    setError(null);

    try {
      // Generate mnemonic if not already generated
      if (!generatedMnemonic) {
        const mnemonic = generateMnemonic(12);
        setGeneratedMnemonic(mnemonic);
        setIsLoading(false);
        return;
      }

      // If mnemonic not confirmed, wait for confirmation
      if (!mnemonicConfirmed) {
        setError('Please confirm you have saved your recovery phrase');
        setIsLoading(false);
        return;
      }

      // Create unified wallet
      const unifiedWallet = await createUnifiedWallet(avatarId, generatedMnemonic);

      const walletCount = Object.keys(unifiedWallet.wallets).length;
      const providerNames = Object.keys(unifiedWallet.wallets).join(', ');
      
      if (walletCount > 0) {
        toastManager.success(`Unified wallet created! ${walletCount} wallets generated: ${providerNames}`);
      } else {
        toastManager.warning('Wallet creation completed but no wallets were found. They may still be processing...');
      }
      
      // Wait a bit for wallets to be fully saved
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // Reload wallets
      if (avatarId) {
        await loadWallets(avatarId);
      }

      // Call success callback
      if (onSuccess) {
        onSuccess();
      } else {
        onBack();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create unified wallet');
    } finally {
      setIsLoading(false);
    }
  };

  const handleLinkKeys = async () => {
    if (!avatarId) return;

    if (!publicKey.trim()) {
      setError('Public key is required');
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const result = await keysAPI.linkKeys({
        avatarId,
        providerType,
        privateKey: privateKey.trim() || undefined,
        publicKey: publicKey.trim(),
        walletAddress: walletAddress.trim() || undefined,
      });

      if (result.isError) {
        setError(result.message || 'Failed to link keys and create wallet');
        return;
      }

      toastManager.success('Wallet created successfully!');
      
      // Reload wallets
      if (avatarId) {
        await loadWallets(avatarId);
      }

      // Call success callback
      if (onSuccess) {
        onSuccess();
      } else {
        onBack();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to create wallet');
    } finally {
      setIsLoading(false);
    }
  };

  const providerMeta = getProviderMetadata(providerType);

  const renderMethodSelection = () => (
    <div className="space-y-4">
      <div className="text-center mb-6">
        <h2 className="text-2xl font-semibold mb-2">Create Wallet</h2>
        <p className="text-gray-400 text-sm">
          Link your keys to create a wallet for {providerMeta.name}
        </p>
      </div>

      <div className="space-y-3">
        <button
          onClick={() => setStep('unified')}
          className="w-full p-4 bg-gradient-to-r from-purple-600/20 to-blue-600/20 border-2 border-purple-500/50 rounded-xl hover:bg-gradient-to-r hover:from-purple-600/30 hover:to-blue-600/30 transition-colors text-left"
        >
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-lg bg-gradient-to-br from-purple-500 to-blue-500 flex items-center justify-center">
              <Key className="w-5 h-5 text-white" />
            </div>
            <div className="flex-1">
              <div className="font-semibold flex items-center space-x-2">
                <span>Create Unified Wallet</span>
                <span className="text-xs bg-purple-500/30 text-purple-300 px-2 py-0.5 rounded-full">Recommended</span>
              </div>
              <div className="text-sm text-gray-400">One seed phrase for all blockchains</div>
            </div>
          </div>
        </button>

        <button
          onClick={() => setStep('generate')}
          className="w-full p-4 bg-gray-900 border border-gray-800 rounded-xl hover:bg-gray-800 transition-colors text-left"
        >
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-lg bg-purple-500/20 flex items-center justify-center">
              <Key className="w-5 h-5 text-purple-400" />
            </div>
            <div>
              <div className="font-semibold">Generate Single Chain Wallet</div>
              <div className="text-sm text-gray-400">Create wallet for one blockchain</div>
            </div>
          </div>
        </button>

        <button
          onClick={() => setStep('import')}
          className="w-full p-4 bg-gray-900 border border-gray-800 rounded-xl hover:bg-gray-800 transition-colors text-left"
        >
          <div className="flex items-center space-x-3">
            <div className="w-10 h-10 rounded-lg bg-blue-500/20 flex items-center justify-center">
              <Key className="w-5 h-5 text-blue-400" />
            </div>
            <div>
              <div className="font-semibold">Import Existing Keys</div>
              <div className="text-sm text-gray-400">Link your existing keys</div>
            </div>
          </div>
        </button>
      </div>

      <div className="pt-4">
        <label className="text-sm text-gray-400 mb-2 block">Select Blockchain</label>
        <select
          value={providerType}
          onChange={(e) => setProviderType(e.target.value as ProviderType)}
          className="w-full p-3 bg-gray-900 border border-gray-800 rounded-lg text-white"
        >
          <option value={ProviderType.ZcashOASIS}>Zcash (Privacy)</option>
          <option value={ProviderType.AztecOASIS}>Aztec (Privacy L2)</option>
          <option value={ProviderType.MidenOASIS}>Miden (Privacy zkVM)</option>
          <option value={ProviderType.StarknetOASIS}>Starknet (Privacy L2)</option>
          <option value={ProviderType.EthereumOASIS}>Ethereum</option>
          <option value={ProviderType.SolanaOASIS}>Solana</option>
          <option value={ProviderType.PolygonOASIS}>Polygon</option>
          <option value={ProviderType.ArbitrumOASIS}>Arbitrum</option>
        </select>
      </div>
    </div>
  );

  const renderGenerateStep = () => (
    <div className="space-y-4">
      <div className="text-center mb-6">
        <h2 className="text-2xl font-semibold mb-2">Generate Keypair</h2>
        <p className="text-gray-400 text-sm">
          Generate a new keypair for {providerMeta.name}
        </p>
      </div>

      <Button
        onClick={handleGenerateKeypair}
        disabled={isLoading}
        className="w-full bg-purple-600 hover:bg-purple-700"
      >
        {isLoading ? (
          <>
            <Loader2 className="w-4 h-4 mr-2 animate-spin" />
            Generating...
          </>
        ) : (
          <>
            <Key className="w-4 h-4 mr-2" />
            Generate Keypair
          </>
        )}
      </Button>
    </div>
  );

  const renderUnifiedStep = () => (
    <div className="space-y-4">
      <div className="text-center mb-6">
        <h2 className="text-2xl font-semibold mb-2">Create Unified Wallet</h2>
        <p className="text-gray-400 text-sm">
          One seed phrase generates wallets for all blockchains
        </p>
      </div>

      {!generatedMnemonic ? (
        <div className="space-y-4">
          <div className="p-4 bg-blue-500/10 border border-blue-500/30 rounded-lg">
            <p className="text-sm text-blue-300 mb-2">
              <strong>What is a Unified Wallet?</strong>
            </p>
            <ul className="text-xs text-blue-200/80 space-y-1 list-disc list-inside">
              <li>One recovery phrase for all blockchains</li>
              <li>Wallets for Solana, Ethereum, Polygon, Arbitrum, Zcash, Aztec, Miden, and Starknet</li>
              <li>Simplified management across all chains</li>
              <li>Secure and easy to backup</li>
            </ul>
          </div>

          <Button
            onClick={handleCreateUnifiedWallet}
            disabled={isLoading}
            className="w-full bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700"
          >
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                Generating...
              </>
            ) : (
              <>
                <Key className="w-4 h-4 mr-2" />
                Generate Recovery Phrase
              </>
            )}
          </Button>
        </div>
      ) : (
        <div className="space-y-4">
          <div className="p-4 bg-yellow-500/10 border-2 border-yellow-500/50 rounded-lg">
            <div className="flex items-start space-x-2 mb-2">
              <AlertCircle className="w-5 h-5 text-yellow-400 mt-0.5" />
              <div>
                <p className="text-sm font-semibold text-yellow-300 mb-1">
                  Save Your Recovery Phrase
                </p>
                <p className="text-xs text-yellow-200/80">
                  Write down these 12 words in order. Store them securely. You'll need this to recover your wallet.
                </p>
              </div>
            </div>
          </div>

          <div className="p-4 bg-gray-900 border border-gray-800 rounded-lg">
            <div className="flex items-center justify-between mb-3">
              <label className="text-sm font-semibold text-gray-300">Recovery Phrase</label>
              <button
                onClick={() => setShowMnemonic(!showMnemonic)}
                className="text-xs text-gray-400 hover:text-white flex items-center space-x-1"
              >
                {showMnemonic ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                <span>{showMnemonic ? 'Hide' : 'Show'}</span>
              </button>
            </div>
            
            {showMnemonic ? (
              <div className="grid grid-cols-3 gap-2">
                {generatedMnemonic.split(' ').map((word, index) => (
                  <div
                    key={index}
                    className="p-2 bg-gray-800 rounded text-sm text-center font-mono"
                  >
                    <span className="text-gray-500 text-xs mr-1">{index + 1}.</span>
                    {word}
                  </div>
                ))}
              </div>
            ) : (
              <div className="grid grid-cols-3 gap-2">
                {Array.from({ length: 12 }).map((_, index) => (
                  <div
                    key={index}
                    className="p-2 bg-gray-800 rounded text-sm text-center"
                  >
                    <span className="text-gray-500 text-xs mr-1">{index + 1}.</span>
                    ••••
                  </div>
                ))}
              </div>
            )}

            <button
              onClick={() => handleCopy(generatedMnemonic, 'mnemonic')}
              className="mt-3 text-xs text-gray-400 hover:text-white flex items-center space-x-1"
            >
              {copiedField === 'mnemonic' ? (
                <>
                  <Check className="w-3 h-3 text-green-400" />
                  <span className="text-green-400">Copied!</span>
                </>
              ) : (
                <>
                  <Copy className="w-3 h-3" />
                  <span>Copy to clipboard</span>
                </>
              )}
            </button>
          </div>

          <div className="flex items-start space-x-2">
            <input
              type="checkbox"
              id="confirm-mnemonic"
              checked={mnemonicConfirmed}
              onChange={(e) => setMnemonicConfirmed(e.target.checked)}
              className="mt-1"
            />
            <label htmlFor="confirm-mnemonic" className="text-sm text-gray-300">
              I have securely saved my recovery phrase and understand that losing it means losing access to my wallet
            </label>
          </div>

          {error && (
            <div className="p-3 bg-red-500/10 border border-red-500/30 rounded-lg text-sm text-red-400">
              {error}
            </div>
          )}

          <div className="flex space-x-3 pt-4">
            <Button
              onClick={() => {
                setStep('method');
                setGeneratedMnemonic(null);
                setMnemonicConfirmed(false);
                setError(null);
              }}
              variant="outline"
              className="flex-1"
              disabled={isLoading}
            >
              Back
            </Button>
            <Button
              onClick={handleCreateUnifiedWallet}
              disabled={isLoading || !mnemonicConfirmed}
              className="flex-1 bg-gradient-to-r from-purple-600 to-blue-600 hover:from-purple-700 hover:to-blue-700"
            >
              {isLoading ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Creating...
                </>
              ) : (
                'Create Unified Wallet'
              )}
            </Button>
          </div>
        </div>
      )}
    </div>
  );

  const renderImportStep = () => (
    <div className="space-y-4">
      <div className="text-center mb-6">
        <h2 className="text-2xl font-semibold mb-2">
          {privateKey && publicKey ? 'Review Keys' : 'Link Keys'}
        </h2>
        <p className="text-gray-400 text-sm">
          {privateKey && publicKey 
            ? 'Review the generated keys and create your wallet'
            : 'Enter your keys to create a wallet'}
        </p>
      </div>

      {privateKey && (
        <div>
          <label className="text-sm text-gray-400 mb-2 block flex items-center justify-between">
            <span>Private Key</span>
            <span className="text-xs text-red-400">Keep this secret!</span>
          </label>
          <div className="relative">
            <Input
              type={showPrivateKey ? 'text' : 'password'}
              value={privateKey}
              onChange={(e) => setPrivateKey(e.target.value)}
              placeholder="Enter private key"
              className="bg-gray-900 border-gray-800 text-white pr-20"
              disabled={Boolean(!!privateKey && publicKey)}
            />
            <div className="absolute right-2 top-1/2 -translate-y-1/2 flex items-center space-x-1">
              <button
                onClick={() => setShowPrivateKey(!showPrivateKey)}
                className="text-gray-400 hover:text-white"
              >
                {showPrivateKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
              </button>
              <button
                onClick={() => handleCopy(privateKey, 'private')}
                className="text-gray-400 hover:text-white"
              >
                {copiedField === 'private' ? (
                  <Check className="w-4 h-4 text-green-400" />
                ) : (
                  <Copy className="w-4 h-4" />
                )}
              </button>
            </div>
          </div>
        </div>
      )}

      <div>
        <label className="text-sm text-gray-400 mb-2 block">Public Key *</label>
        <div className="relative">
          <Input
            type="text"
            value={publicKey}
            onChange={(e) => setPublicKey(e.target.value)}
            placeholder="Enter public key"
            className="bg-gray-900 border-gray-800 text-white pr-10"
            required
          />
          <button
            onClick={() => handleCopy(publicKey, 'public')}
            className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
          >
            {copiedField === 'public' ? (
              <Check className="w-4 h-4 text-green-400" />
            ) : (
              <Copy className="w-4 h-4" />
            )}
          </button>
        </div>
      </div>

      <div>
        <label className="text-sm text-gray-400 mb-2 block">Wallet Address (Optional)</label>
        <div className="relative">
          <Input
            type="text"
            value={walletAddress}
            onChange={(e) => setWalletAddress(e.target.value)}
            placeholder="Enter wallet address (optional)"
            className="bg-gray-900 border-gray-800 text-white pr-10"
          />
          {walletAddress && (
            <button
              onClick={() => handleCopy(walletAddress, 'address')}
              className="absolute right-2 top-1/2 -translate-y-1/2 text-gray-400 hover:text-white"
            >
              {copiedField === 'address' ? (
                <Check className="w-4 h-4 text-green-400" />
              ) : (
                <Copy className="w-4 h-4" />
              )}
            </button>
          )}
        </div>
        <p className="text-xs text-gray-500 mt-1">
          The wallet address will be derived from the public key if not provided
        </p>
      </div>

      {error && (
        <div className="p-3 bg-red-500/10 border border-red-500/30 rounded-lg text-sm text-red-400">
          {error}
        </div>
      )}

      <div className="flex space-x-3 pt-4">
        <Button
          onClick={() => {
            setStep('method');
            setError(null);
          }}
          variant="outline"
          className="flex-1"
          disabled={isLoading}
        >
          Back
        </Button>
        <Button
          onClick={handleLinkKeys}
          disabled={isLoading || !publicKey.trim()}
          className="flex-1 bg-purple-600 hover:bg-purple-700"
        >
          {isLoading ? (
            <>
              <Loader2 className="w-4 h-4 mr-2 animate-spin" />
              Creating...
            </>
          ) : (
            'Create Wallet'
          )}
        </Button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-black text-white">
      <div className="px-4 py-6">
        <div className="mb-6">
          <button
            onClick={onBack}
            className="text-gray-400 hover:text-white transition-colors flex items-center space-x-2"
          >
            <span>←</span>
            <span>Back</span>
          </button>
        </div>

        <div className="max-w-md mx-auto">
          {step === 'method' && renderMethodSelection()}
          {step === 'unified' && renderUnifiedStep()}
          {step === 'generate' && (
            <>
              {renderGenerateStep()}
              {privateKey && publicKey && (
                <div className="mt-6">
                  {renderImportStep()}
                </div>
              )}
            </>
          )}
          {step === 'import' && renderImportStep()}
        </div>
      </div>
    </div>
  );
};

