'use client';

import React, { useMemo, useState } from 'react';
import Image from 'next/image';
import { universalBridgeChains, type BridgeChain } from '@/lib/providerMeta';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';

interface TokenSelectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (chain: BridgeChain) => void;
  target: 'from' | 'to';
}

export function TokenSelectModal({ isOpen, onClose, onSelect, target }: TokenSelectModalProps) {
  const [search, setSearch] = useState('');

  const filteredChains = useMemo(() => {
    return universalBridgeChains.filter(chain => {
      const haystack = `${chain.name} ${chain.symbol} ${chain.network}`.toLowerCase();
      return haystack.includes(search.toLowerCase());
    });
  }, [search]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 px-4">
      <div className="w-full max-w-md rounded-2xl bg-zinc-950 border border-white/10 shadow-2xl p-5 space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="text-xs uppercase tracking-widest text-gray-400">Select token</p>
            <h2 className="text-lg font-semibold text-white">Choose {target === 'from' ? 'source' : 'destination'} asset</h2>
          </div>
          <Button variant="ghost" size="sm" onClick={onClose} className="text-gray-400 hover:text-white">
            Close
          </Button>
        </div>

        <Input
          placeholder="Search chain or symbol"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="bg-white/5 border-white/10 text-white placeholder:text-gray-500"
        />

        <div className="max-h-[320px] overflow-y-auto pr-1 space-y-2">
          {filteredChains.map(chain => (
            <button
              key={`${chain.name}-${chain.symbol}`}
              onClick={() => {
                onSelect(chain);
                onClose();
                setSearch('');
              }}
              className="w-full flex items-center justify-between rounded-xl border border-white/5 bg-white/5 px-3 py-3 hover:border-white/20 transition-colors"
            >
              <div className="flex items-center space-x-3">
                <div className="w-10 h-10 rounded-full bg-black/50 flex items-center justify-center">
                  <Image
                    src={chain.logoUrl}
                    alt={chain.symbol}
                    width={28}
                    height={28}
                    className="object-contain"
                    loading="lazy"
                  />
                </div>
                <div className="text-left">
                  <p className="font-semibold text-sm text-white">{chain.name}</p>
                  <p className="text-xs text-gray-400">{chain.description}</p>
                </div>
              </div>
              <span className="text-sm font-semibold text-gray-200">{chain.symbol}</span>
            </button>
          ))}

          {filteredChains.length === 0 && (
            <div className="text-center py-6 text-gray-500 text-sm">
              Nothing matches "{search}"
            </div>
          )}
        </div>
      </div>
    </div>
  );
}

