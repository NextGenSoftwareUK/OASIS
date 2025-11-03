'use client';

import { 
  Building2, 
  Users, 
  Database,
  Layers,
  FileCode,
  Activity,
  ArrowDown,
  ArrowRight,
  Zap,
  CheckCircle2
} from 'lucide-react';

export function ArchitectureDiagram() {
  return (
    <div className="space-y-8 relative">
      {/* Title */}
      <div className="text-center">
        <h2 className="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 via-purple-400 to-pink-400 mb-3">
          Two-Layer Architecture
        </h2>
        <p className="text-lg text-cyan-100/80 max-w-3xl mx-auto">
          OASIS + AssetRail are <strong className="text-purple-400">Kleros's internal tools</strong> for multi-chain operations.
          Partners integrate using <strong className="text-cyan-400">standard Web3</strong> tools they already know.
        </p>
      </div>

      {/* Layer 1: Kleros Team */}
      <div className="relative bg-gradient-to-br from-purple-900/40 to-purple-800/30 rounded-xl border-2 border-purple-500/40 p-8 backdrop-blur-sm shadow-[0_0_30px_rgba(138,43,226,0.3)]">
        {/* Glow effect */}
        <div className="absolute inset-0 bg-gradient-to-r from-purple-500/10 to-pink-500/10 rounded-xl blur-xl" />
        
        <div className="relative">
          <div className="flex items-center gap-3 mb-6">
            <Building2 className="w-8 h-8 text-purple-400 drop-shadow-[0_0_10px_rgba(168,85,247,0.8)]" />
            <div>
              <h3 className="text-2xl font-bold text-purple-300">
                Layer 1: Kleros Team (Internal)
              </h3>
              <p className="text-purple-200/70">Uses OASIS + AssetRail SC-Gen for multi-chain operations</p>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* AssetRail SC-Gen */}
            <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-6 shadow-lg border border-cyan-500/30 hover:border-cyan-400/50 transition-all hover:shadow-[0_0_20px_rgba(0,255,255,0.3)]">
              <FileCode className="w-10 h-10 text-cyan-400 mb-4 drop-shadow-[0_0_10px_rgba(0,255,255,0.6)]" />
              <h4 className="font-semibold text-cyan-300 mb-2">AssetRail SC-Gen</h4>
              <p className="text-sm text-slate-300 mb-4">
                Cross-chain smart contract generator
              </p>
              <ul className="text-xs text-slate-400 space-y-1">
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-cyan-400 rounded-full" />
                  Template-based generation
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-cyan-400 rounded-full" />
                  Solidity (EVM chains)
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-cyan-400 rounded-full" />
                  Anchor/Rust (Solana)
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-cyan-400 rounded-full" />
                  Auto-compile & deploy
                </li>
              </ul>
            </div>

            {/* OASIS Platform */}
            <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-6 shadow-lg border border-purple-500/30 hover:border-purple-400/50 transition-all hover:shadow-[0_0_20px_rgba(168,85,247,0.3)]">
              <Database className="w-10 h-10 text-purple-400 mb-4 drop-shadow-[0_0_10px_rgba(168,85,247,0.6)]" />
              <h4 className="font-semibold text-purple-300 mb-2">OASIS Platform</h4>
              <p className="text-sm text-slate-300 mb-4">
                Multi-chain deployment & monitoring
              </p>
              <ul className="text-xs text-slate-400 space-y-1">
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-purple-400 rounded-full" />
                  15+ blockchain providers
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-purple-400 rounded-full" />
                  Unified dashboard
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-purple-400 rounded-full" />
                  Auto-failover
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-purple-400 rounded-full" />
                  Cost optimization
                </li>
              </ul>
            </div>

            {/* SDK Generator */}
            <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-6 shadow-lg border border-pink-500/30 hover:border-pink-400/50 transition-all hover:shadow-[0_0_20px_rgba(236,72,153,0.3)]">
              <Layers className="w-10 h-10 text-pink-400 mb-4 drop-shadow-[0_0_10px_rgba(236,72,153,0.6)]" />
              <h4 className="font-semibold text-pink-300 mb-2">SDK Generator</h4>
              <p className="text-sm text-slate-300 mb-4">
                Auto-generate partner libraries
              </p>
              <ul className="text-xs text-slate-400 space-y-1">
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-pink-400 rounded-full" />
                  Chain-specific SDKs
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-pink-400 rounded-full" />
                  npm packages
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-pink-400 rounded-full" />
                  Auto-documentation
                </li>
                <li className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-pink-400 rounded-full" />
                  No OASIS dependency
                </li>
              </ul>
            </div>
          </div>

          {/* Workflow */}
          <div className="mt-6 bg-slate-800/40 backdrop-blur-sm rounded-lg p-4 border border-purple-500/20">
            <div className="flex items-center gap-2 text-sm text-purple-300 font-medium mb-2">
              <Activity className="w-4 h-4" />
              Internal Workflow:
            </div>
            <div className="flex flex-wrap items-center gap-2 text-xs text-slate-300">
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-cyan-500/30 shadow-[0_0_10px_rgba(0,255,255,0.2)]">
                1. Create template
              </span>
              <ArrowRight className="w-4 h-4 text-cyan-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-cyan-500/30 shadow-[0_0_10px_rgba(0,255,255,0.2)]">
                2. Generate contracts
              </span>
              <ArrowRight className="w-4 h-4 text-purple-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-purple-500/30 shadow-[0_0_10px_rgba(168,85,247,0.2)]">
                3. Deploy to 15+ chains
              </span>
              <ArrowRight className="w-4 h-4 text-pink-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-pink-500/30 shadow-[0_0_10px_rgba(236,72,153,0.2)]">
                4. Generate SDKs
              </span>
              <ArrowRight className="w-4 h-4 text-cyan-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-cyan-500/30 shadow-[0_0_10px_rgba(0,255,255,0.2)]">
                5. Monitor all chains
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Arrow Down */}
      <div className="flex flex-col items-center">
        <ArrowDown className="w-12 h-12 text-cyan-400/60 animate-bounce drop-shadow-[0_0_10px_rgba(0,255,255,0.4)]" />
        <p className="text-sm text-cyan-300/60 mt-2 font-medium">
          Kleros contracts deployed to multiple chains
        </p>
      </div>

      {/* Blockchain Layer */}
      <div className="relative bg-gradient-to-br from-blue-900/40 to-indigo-900/30 rounded-xl border-2 border-blue-500/40 p-6 backdrop-blur-sm shadow-[0_0_30px_rgba(59,130,246,0.2)]">
        <div className="absolute inset-0 bg-gradient-to-r from-blue-500/5 to-cyan-500/5 rounded-xl blur-xl" />
        
        <div className="relative">
          <h4 className="font-semibold text-blue-300 mb-4 text-center flex items-center justify-center gap-2">
            <Database className="w-5 h-5" />
            Kleros Arbitrator Contracts (Deployed via OASIS)
          </h4>
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            {[
              { name: 'Ethereum', address: '0x988b3a5...', color: 'cyan' },
              { name: 'Polygon', address: '0x9C1dA9A...', color: 'purple' },
              { name: 'Arbitrum', address: '0xArbitr...', color: 'blue' },
              { name: 'Base', address: '0xBase12...', color: 'indigo' },
              { name: 'Solana', address: 'KLEROS...', color: 'pink' },
            ].map((chain) => (
              <div key={chain.name} className={`bg-slate-800/60 backdrop-blur-sm rounded-lg p-3 shadow-md border border-${chain.color}-500/30 hover:border-${chain.color}-400/60 transition-all hover:shadow-[0_0_15px_rgba(0,255,255,0.3)]`}>
                <div className={`font-medium text-${chain.color}-300 text-sm`}>{chain.name}</div>
                <div className="text-xs text-slate-400 font-mono mt-1">{chain.address}</div>
              </div>
            ))}
          </div>
        </div>
      </div>

      {/* Arrow Down */}
      <div className="flex flex-col items-center">
        <ArrowDown className="w-12 h-12 text-pink-400/60 animate-bounce drop-shadow-[0_0_10px_rgba(236,72,153,0.4)]" />
        <p className="text-sm text-pink-300/60 mt-2 font-medium">
          Partners use standard Web3 tools (no OASIS knowledge required)
        </p>
      </div>

      {/* Layer 2: Partner Integration */}
      <div className="relative bg-gradient-to-br from-emerald-900/40 to-green-800/30 rounded-xl border-2 border-emerald-500/40 p-8 backdrop-blur-sm shadow-[0_0_30px_rgba(16,185,129,0.2)]">
        <div className="absolute inset-0 bg-gradient-to-r from-emerald-500/10 to-cyan-500/10 rounded-xl blur-xl" />
        
        <div className="relative">
          <div className="flex items-center gap-3 mb-6">
            <Users className="w-8 h-8 text-emerald-400 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]" />
            <div>
              <h3 className="text-2xl font-bold text-emerald-300">
                Layer 2: Partner Integration (External)
              </h3>
              <p className="text-emerald-200/70">Standard Web3 integration - no OASIS required</p>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {/* Example Partners */}
            {[
              {
                name: 'Uniswap',
                chain: 'Ethereum',
                useCase: 'OTC escrow',
                tool: 'Ethers.js',
                icon: '🦄'
              },
              {
                name: 'OpenSea',
                chain: 'Polygon',
                useCase: 'NFT disputes',
                tool: 'Web3.js',
                icon: '🌊'
              },
              {
                name: 'Magic Eden',
                chain: 'Solana',
                useCase: 'Marketplace',
                tool: 'Anchor SDK',
                icon: '✨'
              }
            ].map((partner) => (
              <div key={partner.name} className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-6 shadow-lg border border-emerald-500/30 hover:border-emerald-400/60 transition-all hover:shadow-[0_0_20px_rgba(16,185,129,0.3)]">
                <div className="text-3xl mb-3">{partner.icon}</div>
                <h4 className="font-semibold text-emerald-300 mb-3">{partner.name}</h4>
                <div className="space-y-2 text-sm">
                  <div className="flex justify-between">
                    <span className="text-slate-400">Chain:</span>
                    <span className="font-medium text-cyan-300">{partner.chain}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-400">Use Case:</span>
                    <span className="font-medium text-purple-300">{partner.useCase}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-slate-400">Tool:</span>
                    <span className="font-medium text-pink-300">{partner.tool}</span>
                  </div>
                </div>
                <div className="mt-4 pt-4 border-t border-emerald-700/30">
                  <div className="flex items-center gap-2 text-xs text-emerald-400">
                    <CheckCircle2 className="w-3 h-3" />
                    No OASIS knowledge needed
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* Partner Experience */}
          <div className="mt-6 bg-slate-800/40 backdrop-blur-sm rounded-lg p-4 border border-emerald-500/20">
            <div className="flex items-center gap-2 text-sm text-emerald-300 font-medium mb-2">
              <Zap className="w-4 h-4" />
              Partner Integration Steps:
            </div>
            <div className="flex flex-wrap items-center gap-2 text-xs text-slate-300">
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-emerald-500/30 shadow-[0_0_10px_rgba(16,185,129,0.2)]">
                1. Read Kleros docs
              </span>
              <ArrowRight className="w-4 h-4 text-emerald-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-emerald-500/30 shadow-[0_0_10px_rgba(16,185,129,0.2)]">
                2. Install SDK (npm)
              </span>
              <ArrowRight className="w-4 h-4 text-cyan-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-cyan-500/30 shadow-[0_0_10px_rgba(0,255,255,0.2)]">
                3. Use standard Web3
              </span>
              <ArrowRight className="w-4 h-4 text-purple-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-purple-500/30 shadow-[0_0_10px_rgba(168,85,247,0.2)]">
                4. Create dispute
              </span>
              <ArrowRight className="w-4 h-4 text-pink-400/50" />
              <span className="bg-slate-700/60 px-3 py-1 rounded-full border border-pink-500/30 shadow-[0_0_10px_rgba(236,72,153,0.2)]">
                5. Get ruling
              </span>
            </div>
          </div>
        </div>
      </div>

      {/* Key Benefits */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mt-8">
        <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-6 shadow-lg border border-purple-500/30">
          <h3 className="font-semibold text-purple-300 mb-4 flex items-center gap-2">
            <Building2 className="w-5 h-5" />
            For Kleros Team
          </h3>
          <ul className="space-y-2 text-sm text-slate-300">
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-cyan-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(0,255,255,0.5)]" />
              <span>Deploy to new chain in 1-2 days (vs 2-4 weeks)</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-purple-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(168,85,247,0.5)]" />
              <span>Monitor all chains in one dashboard</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-pink-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(236,72,153,0.5)]" />
              <span>Save $200k-400k/year in engineering costs</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-cyan-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(0,255,255,0.5)]" />
              <span>100% consistency across all chains</span>
            </li>
          </ul>
        </div>

        <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-6 shadow-lg border border-emerald-500/30">
          <h3 className="font-semibold text-emerald-300 mb-4 flex items-center gap-2">
            <Users className="w-5 h-5" />
            For Partners
          </h3>
          <ul className="space-y-2 text-sm text-slate-300">
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-emerald-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(16,185,129,0.5)]" />
              <span>Use standard Web3 tools (Ethers.js, Web3.js)</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-cyan-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(0,255,255,0.5)]" />
              <span>No learning curve - works like any smart contract</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-purple-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(168,85,247,0.5)]" />
              <span>Simple npm packages per chain</span>
            </li>
            <li className="flex items-start gap-2">
              <CheckCircle2 className="w-5 h-5 text-pink-400 flex-shrink-0 mt-0.5 drop-shadow-[0_0_8px_rgba(236,72,153,0.5)]" />
              <span>Never need to know OASIS exists</span>
            </li>
          </ul>
        </div>
      </div>
    </div>
  );
}
