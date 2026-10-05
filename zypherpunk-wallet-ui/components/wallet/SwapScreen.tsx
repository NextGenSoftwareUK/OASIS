'use client';

import React from 'react';
import Image from 'next/image';
import { ArrowLeft } from 'lucide-react';
import { SwapForm } from '@/components/swap/SwapForm';
import { universalBridgeChains } from '@/lib/providerMeta';

interface SwapScreenProps {
  onBack: () => void;
}

export const SwapScreen: React.FC<SwapScreenProps> = ({ onBack }) => {
  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      <header className="px-4 pt-12 pb-4 border-b border-gray-800 flex items-center space-x-4">
        <button onClick={onBack} className="p-2">
          <ArrowLeft className="w-6 h-6" />
        </button>
        <div>
          <p className="text-xs text-gray-400 uppercase tracking-wider">Universal Bridge</p>
          <h1 className="text-xl font-semibold">Swap Assets</h1>
        </div>
      </header>

      <main className="flex-1 px-4 py-6 space-y-6 overflow-y-auto">
        <SwapForm />

        <section>
          <p className="text-xs uppercase tracking-widest text-gray-400 mb-3">Supported chains</p>
          <div className="grid grid-cols-2 gap-3">
            {universalBridgeChains.map(chain => (
              <div key={chain.name} className="p-3 rounded-2xl border border-white/5 bg-white/5 flex items-center space-x-3">
                <div className="w-10 h-10 rounded-full bg-black/40 overflow-hidden relative">
                  <Image src={chain.logoUrl} alt={chain.name} fill className="object-cover" />
                </div>
                <div>
                  <p className="font-semibold text-sm">{chain.name}</p>
                  <p className="text-xs text-gray-400">{chain.description}</p>
                </div>
              </div>
            ))}
          </div>
        </section>
      </main>
    </div>
  );
};

