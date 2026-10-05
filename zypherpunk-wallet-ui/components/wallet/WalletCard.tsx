import React from 'react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Badge } from '@/components/ui/badge';
import { Copy, Send, Settings, Eye, EyeOff } from 'lucide-react';
import { formatAddress, formatBalance, getProviderColor, getProviderIcon, copyToClipboard } from '@/lib/utils';
import type { Wallet, ProviderType } from '@/lib/types';
import { cn } from '@/lib/utils';

interface WalletCardProps {
  wallet: Wallet;
  onSend?: (wallet: Wallet) => void;
  onSetDefault?: (walletId: string, providerType: ProviderType) => void;
  onCopyAddress?: (address: string) => void;
  showPrivateKey?: boolean;
  onTogglePrivateKey?: () => void;
}

export const WalletCard: React.FC<WalletCardProps> = ({
  wallet,
  onSend,
  onSetDefault,
  onCopyAddress,
  showPrivateKey = false,
  onTogglePrivateKey,
}) => {
  const handleCopyAddress = async () => {
    try {
      await copyToClipboard(wallet.walletAddress);
      onCopyAddress?.(wallet.walletAddress);
    } catch (error) {
      console.error('Failed to copy address:', error);
    }
  };

  const providerColor = getProviderColor(wallet.providerType);
  const providerIcon = getProviderIcon(wallet.providerType);

  return (
    <Card className="wallet-card hover:shadow-lg transition-all duration-200">
      <CardHeader className="wallet-card-header">
        <div className="flex items-center space-x-3">
          <div className={cn(
            "w-12 h-12 rounded-full flex items-center justify-center text-white font-bold text-lg cosmic-pulse",
            providerColor
          )}>
            <span className="cosmic-float">{providerIcon}</span>
          </div>
          <div>
            <CardTitle className="text-lg font-semibold">
              {wallet.providerType.replace('OASIS', '')} Wallet
            </CardTitle>
            <div className="flex items-center space-x-2 mt-1">
              <Badge variant={wallet.isDefaultWallet ? "default" : "secondary"}>
                {wallet.isDefaultWallet ? "Default" : "Secondary"}
              </Badge>
              <span className="provider-badge ethereum">
                {wallet.providerType}
              </span>
            </div>
          </div>
        </div>
        <div className="flex items-center space-x-2">
          <Button
            variant="ghost"
            size="sm"
            onClick={handleCopyAddress}
            className="h-8 w-8 p-0"
          >
            <Copy className="h-4 w-4" />
          </Button>
          {onTogglePrivateKey && (
            <Button
              variant="ghost"
              size="sm"
              onClick={onTogglePrivateKey}
              className="h-8 w-8 p-0"
            >
              {showPrivateKey ? <EyeOff className="h-4 w-4" /> : <Eye className="h-4 w-4" />}
            </Button>
          )}
          <Button
            variant="ghost"
            size="sm"
            className="h-8 w-8 p-0"
          >
            <Settings className="h-4 w-4" />
          </Button>
        </div>
      </CardHeader>
      
      <CardContent className="wallet-card-content">
        <div className="space-y-4">
          {/* Balance */}
          <div className="text-center">
            <div className="wallet-balance">
              {formatBalance(wallet.balance)} {wallet.providerType.replace('OASIS', '')}
            </div>
            <p className="text-sm text-muted-foreground mt-1">
              ≈ {formatBalance(wallet.balance * 1800)} USD
            </p>
          </div>

          {/* Address */}
          <div className="space-y-2">
            <label className="text-sm font-medium text-muted-foreground">
              Wallet Address
            </label>
            <div className="wallet-address bg-muted p-2 rounded-md">
              {formatAddress(wallet.walletAddress, 12)}
            </div>
          </div>

          {/* Private Key (if shown) */}
          {showPrivateKey && wallet.privateKey && (
            <div className="space-y-2">
              <label className="text-sm font-medium text-muted-foreground">
                Private Key
              </label>
              <div className="wallet-address bg-muted p-2 rounded-md text-red-600">
                {formatAddress(wallet.privateKey, 12)}
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex space-x-2 pt-2">
            <Button
              variant="wallet"
              size="sm"
              onClick={() => onSend?.(wallet)}
              className="flex-1"
            >
              <Send className="h-4 w-4 mr-2" />
              Send
            </Button>
            {!wallet.isDefaultWallet && onSetDefault && (
              <Button
                variant="outline"
                size="sm"
                onClick={() => onSetDefault(wallet.walletId, wallet.providerType)}
              >
                Set Default
              </Button>
            )}
          </div>

          {/* Transaction Count */}
          <div className="text-center text-sm text-muted-foreground">
            {wallet.transactions.length} transactions
          </div>
        </div>
      </CardContent>
    </Card>
  );
}; 