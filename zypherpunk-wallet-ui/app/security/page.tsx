'use client';

import React from 'react';
import { ArrowLeft } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { SecurityFeaturesDisplay } from '@/components/security/SecurityFeaturesDisplay';
import { useRouter } from 'next/navigation';

export default function SecurityPage() {
  const router = useRouter();

  return (
    <div className="min-h-screen bg-zypherpunk-bg text-zypherpunk-text p-4">
      <div className="max-w-4xl mx-auto space-y-6">
        {/* Header */}
        <div className="flex items-center space-x-4">
          <Button
            variant="ghost"
            onClick={() => router.back()}
            className="text-zypherpunk-text-muted hover:text-zypherpunk-text"
          >
            <ArrowLeft className="w-5 h-5" />
          </Button>
          <div>
            <h1 className="text-3xl font-bold">Security Features</h1>
            <p className="text-sm text-zypherpunk-text-muted mt-1">
              OASIS Wallet API Security Status
            </p>
          </div>
        </div>

        {/* Security Features Display */}
        <SecurityFeaturesDisplay />
      </div>
    </div>
  );
}

