'use client';

import { useState } from 'react';
import { 
  Building2, 
  Users, 
  Layers, 
  Rocket,
  FileCode,
  Database,
  Activity,
  Code,
  Zap,
  ArrowRight,
  CheckCircle2,
  XCircle
} from 'lucide-react';
import { KlerosTeamView } from '@/components/kleros/kleros-team-view';
import { PartnerIntegrationView } from '@/components/kleros/partner-integration-view';
import { ArchitectureDiagram } from '@/components/kleros/architecture-diagram';
import { UserJourney } from '@/components/kleros/user-journey';
import { KlerosProductsSimple as KlerosProducts } from '@/components/kleros/kleros-products-simple';

export function KlerosArchitectureDemo() {
  const [activeView, setActiveView] = useState<'architecture' | 'products' | 'team' | 'partner' | 'journey'>('architecture');

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-950 via-purple-950 to-slate-900">
      {/* Animated background stars */}
      <div className="fixed inset-0 overflow-hidden pointer-events-none">
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_50%_50%,rgba(138,43,226,0.1),transparent_50%)]" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_80%_20%,rgba(0,255,255,0.05),transparent_50%)]" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_20%_80%,rgba(255,0,255,0.05),transparent_50%)]" />
      </div>

      {/* Header */}
      <header className="relative bg-slate-900/80 backdrop-blur-lg border-b border-cyan-500/20 shadow-lg shadow-purple-500/10">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-6">
          <div className="flex items-center justify-between">
          <div>
              <h1 className="text-3xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 via-purple-400 to-pink-400 flex items-center gap-3">
                <div className="w-10 h-10">
                  <img src="/logos/kleros.svg" alt="Kleros" className="w-full h-full drop-shadow-[0_0_10px_rgba(168,85,247,0.6)]" />
          </div>
                Kleros × OASIS
                <span className="text-sm font-normal text-cyan-300/60 ml-2">Multi-Chain Operations Platform</span>
              </h1>
              <p className="mt-2 text-cyan-100/70">
                Two-Layer Architecture: Internal Tooling + Partner Integration
              </p>
        </div>
            <div className="flex gap-2">
              <span className="inline-flex items-center px-3 py-1 rounded-full text-xs font-medium bg-emerald-500/20 text-emerald-400 border border-emerald-500/30">
                <CheckCircle2 className="w-3 h-3 mr-1" />
                Proof of Concept
              </span>
            </div>
          </div>
        </div>
      </header>

      {/* Navigation Tabs */}
      <div className="relative bg-slate-900/60 backdrop-blur-md border-b border-purple-500/20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex gap-1">
            <button
              onClick={() => setActiveView('architecture')}
              className={`px-6 py-4 font-medium text-sm border-b-2 transition-all ${
                activeView === 'architecture'
                  ? 'border-cyan-400 text-cyan-400 bg-cyan-500/10'
                  : 'border-transparent text-slate-400 hover:text-cyan-300 hover:border-cyan-500/30'
              }`}
            >
              <div className="flex items-center gap-2">
                <Layers className="w-4 h-4" />
                Architecture Overview
              </div>
            </button>
            <button
              onClick={() => setActiveView('products')}
              className={`px-6 py-4 font-medium text-sm border-b-2 transition-all ${
                activeView === 'products'
                  ? 'border-blue-400 text-blue-400 bg-blue-500/10'
                  : 'border-transparent text-slate-400 hover:text-blue-300 hover:border-blue-500/30'
              }`}
            >
              <div className="flex items-center gap-2">
                <Layers className="w-4 h-4" />
                5 Products
                <span className="text-xs text-slate-500">(Court, Oracle, Curate, Escrow, Governor)</span>
              </div>
            </button>
            <button
              onClick={() => setActiveView('team')}
              className={`px-6 py-4 font-medium text-sm border-b-2 transition-all ${
                activeView === 'team'
                  ? 'border-purple-400 text-purple-400 bg-purple-500/10'
                  : 'border-transparent text-slate-400 hover:text-purple-300 hover:border-purple-500/30'
              }`}
            >
              <div className="flex items-center gap-2">
                <Building2 className="w-4 h-4" />
                Deployment
                <span className="text-xs text-slate-500">(OASIS/AssetRail)</span>
              </div>
            </button>
            <button
              onClick={() => setActiveView('partner')}
              className={`px-6 py-4 font-medium text-sm border-b-2 transition-all ${
                activeView === 'partner'
                  ? 'border-pink-400 text-pink-400 bg-pink-500/10'
                  : 'border-transparent text-slate-400 hover:text-pink-300 hover:border-pink-500/30'
              }`}
            >
              <div className="flex items-center gap-2">
                <Users className="w-4 h-4" />
                Partner View
                <span className="text-xs text-slate-500">(External - Standard Web3)</span>
              </div>
            </button>
            <button
              onClick={() => setActiveView('journey')}
              className={`px-6 py-4 font-medium text-sm border-b-2 transition-all ${
                activeView === 'journey'
                  ? 'border-emerald-400 text-emerald-400 bg-emerald-500/10'
                  : 'border-transparent text-slate-400 hover:text-emerald-300 hover:border-emerald-500/30'
              }`}
            >
              <div className="flex items-center gap-2">
                <Zap className="w-4 h-4" />
                User Journey
                <span className="text-xs text-slate-500">(How disputes actually work)</span>
              </div>
            </button>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <main className="relative max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {activeView === 'architecture' && <ArchitectureDiagram />}
        {activeView === 'products' && <KlerosProducts />}
        {activeView === 'team' && <KlerosTeamView />}
        {activeView === 'partner' && <PartnerIntegrationView />}
        {activeView === 'journey' && <UserJourney />}
      </main>

      {/* Footer */}
      <footer className="relative bg-slate-900/80 backdrop-blur-lg border-t border-purple-500/20 mt-16">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              <div>
              <h3 className="font-semibold text-cyan-400 mb-3 flex items-center gap-2">
                <Zap className="w-4 h-4" />
                Key Insight
              </h3>
              <p className="text-sm text-slate-300">
                OASIS + AssetRail are <strong className="text-purple-400">Kleros's internal tools</strong>. 
                Partners integrate using <strong className="text-cyan-400">standard Web3</strong> (Ethers.js, Web3.js).
              </p>
              </div>
            <div>
              <h3 className="font-semibold text-purple-400 mb-3 flex items-center gap-2">
                <Rocket className="w-4 h-4" />
                Value Proposition
              </h3>
              <ul className="text-sm text-slate-300 space-y-1">
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-cyan-400 rounded-full" />
                  90% faster deployments (2-4 weeks → 1-2 days)
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-purple-400 rounded-full" />
                  $200k-400k/year engineering cost savings
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-pink-400 rounded-full" />
                  Deploy to 15+ chains from one template
                </li>
              </ul>
            </div>
            <div>
              <h3 className="font-semibold text-pink-400 mb-3 flex items-center gap-2">
                <Activity className="w-4 h-4" />
                Analogy
              </h3>
              <p className="text-sm text-slate-300">
                Like <strong className="text-cyan-400">Stripe's</strong> internal tools support 100+ countries,
                but merchants just use Stripe.js — OASIS helps Kleros support 15+ chains,
                but partners just use Kleros contracts.
              </p>
            </div>
          </div>
          <div className="mt-8 pt-8 border-t border-slate-700/50 text-center text-sm text-slate-500">
            Kleros × OASIS Proof of Concept — Built with Next.js 15 + React 19 + TypeScript
          </div>
        </div>
      </footer>
    </div>
  );
}
