'use client';

import React from 'react';
import { ArrowLeft, Shield, AlertCircle } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

interface ClaimDropScreenProps {
  onBack: () => void;
  onSuccess?: () => void;
}

export const ClaimDropScreen: React.FC<ClaimDropScreenProps> = ({
  onBack,
  onSuccess,
}) => {
  return (
    <div className="min-h-screen bg-zypherpunk-bg text-zypherpunk-text p-4">
      {/* Header */}
      <div className="flex items-center gap-4 mb-6">
        <Button
          variant="ghost"
          size="icon"
          onClick={onBack}
          className="text-zypherpunk-text hover:bg-zypherpunk-surface"
        >
          <ArrowLeft className="w-5 h-5" />
        </Button>
        <div className="flex items-center gap-2">
          <Shield className="w-6 h-6 text-zypherpunk-shielded" />
          <h1 className="text-2xl font-bold">Claim Privacy Drop</h1>
        </div>
      </div>

      <Card className="bg-zypherpunk-surface border-zypherpunk-border p-6">
        <div className="flex items-center gap-3 mb-4">
          <AlertCircle className="w-5 h-5 text-yellow-400" />
          <h2 className="text-xl font-semibold">Coming Soon</h2>
        </div>
        <p className="text-zypherpunk-text-muted mb-4">
          Privacy Drop claiming functionality is under development. This feature will allow you to
          claim privacy-preserving drops using a claim code, without revealing your identity to the sender.
        </p>
        <Button onClick={onBack} className="w-full">
          Go Back
        </Button>
      </Card>
    </div>
  );
};



