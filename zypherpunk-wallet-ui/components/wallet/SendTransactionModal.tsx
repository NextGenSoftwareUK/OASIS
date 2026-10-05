import React, { useState } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select';
import { Card, CardContent } from '@/components/ui/card';
import { Send, ArrowRight, AlertCircle } from 'lucide-react';
import { formatAddress, validateAddress, validateAmount } from '@/lib/utils';
import type { Wallet, WalletTransactionRequest } from '@/lib/types';
import { ProviderType } from '@/lib/types';
import { useWalletStore } from '@/lib/store';

interface SendTransactionModalProps {
  wallet: Wallet;
  onClose?: () => void;
}

export const SendTransactionModal: React.FC<SendTransactionModalProps> = ({
  wallet,
  onClose,
}) => {
  const [isOpen, setIsOpen] = useState(false);
  const [formData, setFormData] = useState({
    toAddress: '',
    amount: '',
    memo: '',
    toProviderType: wallet.providerType,
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [isLoading, setIsLoading] = useState(false);
  
  const { sendTransaction } = useWalletStore();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    // Validate form
    const newErrors: Record<string, string> = {};
    
    if (!validateAddress(formData.toAddress)) {
      newErrors.toAddress = 'Invalid wallet address';
    }
    
    if (!validateAmount(formData.amount)) {
      newErrors.amount = 'Invalid amount';
    }
    
    if (parseFloat(formData.amount) > wallet.balance) {
      newErrors.amount = 'Insufficient balance';
    }
    
    if (Object.keys(newErrors).length > 0) {
      setErrors(newErrors);
      return;
    }

    setIsLoading(true);
    try {
      const request: WalletTransactionRequest = {
        fromWalletAddress: wallet.walletAddress,
        toWalletAddress: formData.toAddress,
        fromProviderType: wallet.providerType,
        toProviderType: formData.toProviderType,
        amount: parseFloat(formData.amount),
        memoText: formData.memo,
      };

      await sendTransaction(request);
      
      // Reset form
      setFormData({
        toAddress: '',
        amount: '',
        memo: '',
        toProviderType: wallet.providerType,
      });
      setErrors({});
      setIsOpen(false);
      onClose?.();
    } catch (error) {
      console.error('Failed to send transaction:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (field: string, value: string) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={setIsOpen}>
      <DialogTrigger asChild>
        <Button variant="wallet" size="sm">
          <Send className="h-4 w-4 mr-2" />
          Send
        </Button>
      </DialogTrigger>
      <DialogContent className="sm:max-w-md">
        <DialogHeader>
          <DialogTitle>Send Transaction</DialogTitle>
        </DialogHeader>
        
        <form onSubmit={handleSubmit} className="space-y-4">
          {/* From Wallet Info */}
          <Card>
            <CardContent className="p-4">
              <div className="flex items-center justify-between">
                <div>
                  <p className="text-sm text-muted-foreground">From</p>
                  <p className="font-medium">{wallet.providerType.replace('OASIS', '')} Wallet</p>
                  <p className="text-xs text-muted-foreground">
                    {formatAddress(wallet.walletAddress)}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-sm text-muted-foreground">Balance</p>
                  <p className="font-medium">{wallet.balance} {wallet.providerType.replace('OASIS', '')}</p>
                </div>
              </div>
            </CardContent>
          </Card>

          {/* Arrow */}
          <div className="flex justify-center">
            <ArrowRight className="h-6 w-6 text-muted-foreground" />
          </div>

          {/* To Address */}
          <div className="space-y-2">
            <label className="text-sm font-medium">To Address</label>
            <Input
              placeholder="Enter wallet address"
              value={formData.toAddress}
              onChange={(e) => handleInputChange('toAddress', e.target.value)}
              className={errors.toAddress ? 'border-red-500' : ''}
            />
            {errors.toAddress && (
              <p className="text-sm text-red-500 flex items-center">
                <AlertCircle className="h-4 w-4 mr-1" />
                {errors.toAddress}
              </p>
            )}
          </div>

          {/* Amount */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Amount</label>
            <Input
              type="number"
              step="0.000001"
              placeholder="0.00"
              value={formData.amount}
              onChange={(e) => handleInputChange('amount', e.target.value)}
              className={errors.amount ? 'border-red-500' : ''}
            />
            {errors.amount && (
              <p className="text-sm text-red-500 flex items-center">
                <AlertCircle className="h-4 w-4 mr-1" />
                {errors.amount}
              </p>
            )}
          </div>

          {/* To Provider Type */}
          <div className="space-y-2">
            <label className="text-sm font-medium">To Network</label>
            <Select
              value={formData.toProviderType}
              onValueChange={(value) => handleInputChange('toProviderType', value)}
            >
              <SelectTrigger>
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                <SelectItem value={ProviderType.EthereumOASIS}>Ethereum</SelectItem>
                <SelectItem value={ProviderType.SolanaOASIS}>Solana</SelectItem>
                <SelectItem value={ProviderType.PolygonOASIS}>Polygon</SelectItem>
                <SelectItem value={ProviderType.ArbitrumOASIS}>Arbitrum</SelectItem>
              </SelectContent>
            </Select>
          </div>

          {/* Memo */}
          <div className="space-y-2">
            <label className="text-sm font-medium">Memo (Optional)</label>
            <Input
              placeholder="Transaction memo"
              value={formData.memo}
              onChange={(e) => handleInputChange('memo', e.target.value)}
            />
          </div>

          {/* Actions */}
          <div className="flex space-x-2 pt-4">
            <Button
              type="button"
              variant="outline"
              onClick={() => setIsOpen(false)}
              className="flex-1"
            >
              Cancel
            </Button>
            <Button
              type="submit"
              disabled={isLoading}
              className="flex-1"
            >
              {isLoading ? 'Sending...' : 'Send Transaction'}
            </Button>
          </div>
        </form>
      </DialogContent>
    </Dialog>
  );
}; 