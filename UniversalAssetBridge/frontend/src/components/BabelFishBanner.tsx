"use client";

import React from 'react';
import Link from 'next/link';

export default function BabelFishBanner() {
  return (
    <div className="max-w-7xl mx-auto px-4 py-6 mb-6">
      <div className="relative overflow-hidden rounded-xl p-6" style={{
        background: 'linear-gradient(135deg, rgba(0, 255, 102, 0.1) 0%, rgba(0, 255, 255, 0.1) 100%)',
        border: '2px solid rgba(0, 255, 102, 0.3)'
      }}>
        {/* Background Pattern */}
        <div className="absolute inset-0 opacity-5" style={{
          backgroundImage: `repeating-linear-gradient(45deg, transparent, transparent 10px, #00FF66 10px, #00FF66 11px)`
        }} />
        
        <div className="relative z-10">
          <div className="flex items-center justify-between flex-wrap gap-4">
            <div className="flex-1 min-w-[250px]">
              <div className="flex items-center gap-3 mb-2">
                <span className="text-3xl">🐟</span>
                <h2 className="text-2xl font-bold" style={{ color: '#00FF66' }}>
                  The Babel Fish Protocol
                </h2>
              </div>
              <p className="text-gray-300 text-sm leading-relaxed">
                Universal translation for blockchain languages. No bridges. No hacks. No panic.
              </p>
              <p className="text-xs text-gray-500 mt-2 italic">
                "The Babel Fish is probably the oddest thing in the universe" — Douglas Adams
              </p>
            </div>
            
            <div className="flex gap-3">
              <Link 
                href="/how-it-works"
                className="px-6 py-3 rounded-lg font-semibold transition-all hover:scale-105 border"
                style={{
                  background: 'rgba(0, 255, 102, 0.1)',
                  borderColor: '#00FF66',
                  color: '#00FF66'
                }}
              >
                How It Works
              </Link>
              <button
                className="px-6 py-3 rounded-lg font-bold transition-all hover:scale-105"
                style={{
                  background: '#00FF66',
                  color: '#000'
                }}
              >
                DON'T PANIC
              </button>
            </div>
          </div>

          {/* Quick Stats */}
          <div className="mt-4 pt-4 border-t border-gray-700/50 grid grid-cols-2 md:grid-cols-4 gap-4">
            <div>
              <p className="text-xs text-gray-500">Blockchains Connected</p>
              <p className="text-lg font-bold" style={{ color: '#00FFFF' }}>2+</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Bridge Hacks</p>
              <p className="text-lg font-bold" style={{ color: '#00FF66' }}>0</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">Success Rate</p>
              <p className="text-lg font-bold" style={{ color: '#00FF66' }}>100%</p>
            </div>
            <div>
              <p className="text-xs text-gray-500">The Answer</p>
              <p className="text-lg font-bold" style={{ color: '#FFD700' }}>42</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

