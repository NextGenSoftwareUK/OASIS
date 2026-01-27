"use client";

import React, { useState } from 'react';
import Link from 'next/link';
import BabelFishDiagram from "@/components/BabelFishDiagram";

export default function HowItWorksClient() {
  const [demoStatus, setDemoStatus] = useState<'idle' | 'processing' | 'complete'>('idle');

  const runDemo = () => {
    setDemoStatus('processing');
    setTimeout(() => {
      setDemoStatus('complete');
      setTimeout(() => {
        setDemoStatus('idle');
      }, 3000);
    }, 5000);
  };

  return (
    <div className="min-h-screen">
      <div className="relative overflow-hidden">
        <div className="absolute inset-x-0 top-0 h-72 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.35),transparent_60%)] blur-3xl" />
        <header className="relative z-10 border-b backdrop-blur-xl" style={{
          borderColor: 'rgba(56, 189, 248, 0.2)',
          background: 'rgba(5,5,16,0.85)'
        }}>
          <div className="max-w-[1300px] mx-auto md:px-5 py-4">
            <Link href="/" className="text-cyan-400 hover:text-cyan-300 transition-colors">
              ← Back to Babel Fish Protocol
            </Link>
          </div>
        </header>
      </div>

      <div className="max-w-7xl mx-auto px-4 py-12">
        {/* Hero Section */}
        <div className="text-center mb-12">
          <h1 className="text-5xl font-bold mb-4" style={{ color: '#00FF66' }}>
            The Babel Fish Protocol
          </h1>
          <p className="text-xl text-gray-300 mb-2">
            Universal Translation for Blockchain Languages
          </p>
          <p className="text-sm text-gray-400 italic">
            "The Babel Fish is small, yellow, leech-like, and probably the oddest thing in the universe."
          </p>
          <p className="text-sm text-gray-500 mt-2">
            — Douglas Adams, The Hitchhiker's Guide to the Galaxy
          </p>
        </div>

        {/* Main Diagram */}
        <div className="mb-12">
          <BabelFishDiagram 
            isActive={demoStatus !== 'idle'} 
            transactionStatus={demoStatus}
          />
        </div>

        {/* Demo Button */}
        <div className="text-center mb-12">
          <button
            onClick={runDemo}
            disabled={demoStatus !== 'idle'}
            className="px-8 py-4 text-lg font-bold rounded-lg transition-all transform hover:scale-105 disabled:opacity-50 disabled:cursor-not-allowed"
            style={{
              background: demoStatus === 'idle' ? '#00FF66' : '#666',
              color: '#000'
            }}
          >
            {demoStatus === 'idle' && "DON'T PANIC - RUN DEMO"}
            {demoStatus === 'processing' && "TRANSLATING..."}
            {demoStatus === 'complete' && "TRANSLATION COMPLETE!"}
          </button>
        </div>

        {/* Explanation Sections */}
        <div className="grid md:grid-cols-2 gap-8 mb-12">
          {/* Left Column */}
          <div className="space-y-6">
            <div className="p-6 rounded-lg" style={{ background: 'rgba(0, 255, 102, 0.05)', border: '1px solid rgba(0, 255, 102, 0.2)' }}>
              <h3 className="text-2xl font-bold mb-4" style={{ color: '#00FF66' }}>
                What Is The Babel Fish?
              </h3>
              <p className="text-gray-300 mb-4">
                In The Hitchhiker's Guide to the Galaxy, the Babel Fish is a small creature that, 
                when placed in your ear, allows you to instantly understand any language in the universe.
              </p>
              <p className="text-gray-300">
                It feeds on brain wave energy, absorbing unconscious frequencies and converting them 
                into conscious thought patterns in your native language.
              </p>
            </div>

            <div className="p-6 rounded-lg" style={{ background: 'rgba(255, 215, 0, 0.05)', border: '1px solid rgba(255, 215, 0, 0.2)' }}>
              <h3 className="text-2xl font-bold mb-4" style={{ color: '#FFD700' }}>
                The Blockchain Problem
              </h3>
              <p className="text-gray-300 mb-4">
                Today's blockchains are like different languages - Solana, Ethereum, Polygon, Radix. 
                They can't understand each other without "bridges."
              </p>
              <p className="text-gray-300 mb-2">
                <span className="text-red-400 font-bold">The Problem:</span> Traditional bridges are risky. 
                Over $2 billion lost to bridge hacks in recent years.
              </p>
              <p className="text-gray-300">
                <span className="text-green-400 font-bold">The Solution:</span> The Babel Fish Protocol - 
                no bridges needed, just universal translation.
              </p>
            </div>
          </div>

          {/* Right Column */}
          <div className="space-y-6">
            <div className="p-6 rounded-lg" style={{ background: 'rgba(0, 255, 255, 0.05)', border: '1px solid rgba(0, 255, 255, 0.2)' }}>
              <h3 className="text-2xl font-bold mb-4" style={{ color: '#00FFFF' }}>
                How It Works
              </h3>
              <div className="space-y-3">
                <div className="flex gap-3">
                  <div className="text-2xl">1.</div>
                  <div>
                    <p className="font-bold text-cyan-400">Telepathic Excretor</p>
                    <p className="text-sm text-gray-400">Detects your swap intent</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <div className="text-2xl">2.</div>
                  <div>
                    <p className="font-bold text-cyan-400">Quantum Brain</p>
                    <p className="text-sm text-gray-400">Calculates optimal route</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <div className="text-2xl">3.</div>
                  <div>
                    <p className="font-bold text-cyan-400">Energy Filter</p>
                    <p className="text-sm text-gray-400">Optimizes gas fees</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <div className="text-2xl">4.</div>
                  <div>
                    <p className="font-bold text-cyan-400">Atomic Heart</p>
                    <p className="text-sm text-gray-400">Executes swap (all or nothing)</p>
                  </div>
                </div>
                <div className="flex gap-3">
                  <div className="text-2xl">5.</div>
                  <div>
                    <p className="font-bold text-cyan-400">Frequency Sensors</p>
                    <p className="text-sm text-gray-400">Verify across all chains</p>
                  </div>
                </div>
              </div>
            </div>

            <div className="p-6 rounded-lg" style={{ background: 'rgba(255, 68, 68, 0.05)', border: '1px solid rgba(255, 68, 68, 0.2)' }}>
              <h3 className="text-2xl font-bold mb-4" style={{ color: '#FF4444' }}>
                Why It's "Mostly Harmless"
              </h3>
              <div className="space-y-2 text-gray-300">
                <div className="flex items-start gap-2">
                  <span className="text-green-400">✓</span>
                  <p><span className="font-bold">No Bridges</span> - Can't have bridge hacks without bridges</p>
                </div>
                <div className="flex items-start gap-2">
                  <span className="text-green-400">✓</span>
                  <p><span className="font-bold">Atomic Swaps</span> - All or nothing, never lose funds</p>
                </div>
                <div className="flex items-start gap-2">
                  <span className="text-green-400">✓</span>
                  <p><span className="font-bold">Auto-Failover</span> - 50+ providers, 100% uptime</p>
                </div>
                <div className="flex items-start gap-2">
                  <span className="text-green-400">✓</span>
                  <p><span className="font-bold">Real-time Verification</span> - Sensors on all chains</p>
                </div>
                <div className="flex items-start gap-2">
                  <span className="text-green-400">✓</span>
                  <p><span className="font-bold">Gas Optimized</span> - Save 60-90% on fees</p>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* Quote Section */}
        <div className="text-center p-8 rounded-lg mb-12" style={{ 
          background: 'rgba(0, 255, 102, 0.1)', 
          border: '2px solid #00FF66' 
        }}>
          <p className="text-2xl text-gray-300 italic mb-4">
            "Now it is such a bizarrely improbable coincidence that anything so mind-bogglingly 
            useful could have evolved purely by chance that some thinkers have chosen to see it 
            as a final and clinching proof of the non-existence of God."
          </p>
          <p className="text-gray-400">
            — Douglas Adams, on the Babel Fish
          </p>
          <p className="text-sm text-gray-500 mt-4">
            The same could be said of a bridge-less cross-chain protocol. But here we are.
          </p>
        </div>

        {/* CTA Section */}
        <div className="text-center">
          <h2 className="text-3xl font-bold mb-4" style={{ color: '#00FF66' }}>
            Ready to Translate Some Tokens?
          </h2>
          <p className="text-gray-300 mb-6">
            Don't panic. The Babel Fish Protocol makes cross-chain swaps as easy as 
            understanding any language in the universe.
          </p>
          <Link
            href="/"
            className="inline-block px-8 py-4 text-lg font-bold rounded-lg transition-all transform hover:scale-105"
            style={{
              background: '#00FF66',
              color: '#000'
            }}
          >
            START TRANSLATING NOW
          </Link>
        </div>

        {/* Footer Note */}
        <div className="mt-12 text-center text-sm text-gray-500">
          <p>
            The Babel Fish Protocol: Making blockchain translation as improbable as it is useful.
          </p>
          <p className="mt-2">
            Powered by OASIS Web4 Technology • Version 4.2.0 (obviously)
          </p>
        </div>
      </div>
    </div>
  );
}

