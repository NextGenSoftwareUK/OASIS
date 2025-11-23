'use client';

import React from 'react';
import { ArrowLeft, Shield } from 'lucide-react';
import { PrivacyBridgeForm } from './PrivacyBridgeForm';
import { privacyBridgeChains } from './PrivacyBridgeForm';

interface PrivacyBridgeScreenProps {
  onBack: () => void;
}

export const PrivacyBridgeScreen: React.FC<PrivacyBridgeScreenProps> = ({ onBack }) => {
  return (
    <div className="min-h-screen bg-black text-white flex flex-col">
      <header className="px-4 pt-12 pb-4 border-b border-gray-800 flex items-center space-x-4">
        <button onClick={onBack} className="p-2">
          <ArrowLeft className="w-6 h-6" />
        </button>
        <div>
          <p className="text-xs text-gray-400 uppercase tracking-wider">Privacy Bridge</p>
          <h1 className="text-xl font-semibold">Private Cross-Chain Transfer</h1>
        </div>
      </header>

      <main className="flex-1 px-4 py-6 space-y-6 overflow-y-auto">
        <PrivacyBridgeForm />

        <section>
          <p className="text-xs uppercase tracking-widest text-gray-400 mb-3">Supported Privacy Chains</p>
          <div className="grid grid-cols-1 gap-3">
            {privacyBridgeChains.map(chain => (
              <div key={chain.name} className="p-3 rounded-2xl border border-white/5 bg-white/5 flex items-center space-x-3">
                <div className="w-10 h-10 rounded-full bg-black/40 flex items-center justify-center">
                  <Shield className="w-5 h-5 text-zypherpunk-primary" />
                </div>
                <div>
                  <p className="font-semibold text-sm">{chain.name}</p>
                  <p className="text-xs text-gray-400">{chain.description}</p>
                </div>
              </div>
            ))}
          </div>
        </section>

        <div className="rounded-2xl border border-white/10 bg-black/40 p-4 space-y-2">
          <p className="text-sm font-semibold text-white flex items-center space-x-2">
            <Shield className="w-4 h-4 text-zypherpunk-primary" />
            <span>Privacy Features</span>
          </p>
          <ul className="text-xs text-gray-400 space-y-1 list-disc list-inside">
            <li>Shielded transactions on Zcash</li>
            <li>Private notes on Aztec and Miden</li>
            <li>Zero-knowledge proof verification</li>
            <li>Optional viewing keys for auditability</li>
            <li>Partial notes for enhanced privacy</li>
          </ul>
        </div>
      </main>
    </div>
  );
}

