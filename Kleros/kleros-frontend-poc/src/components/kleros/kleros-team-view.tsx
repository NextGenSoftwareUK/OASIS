'use client';

import { useState } from 'react';
import { 
  FileCode, 
  Database,
  Activity,
  Rocket,
  CheckCircle2,
  Loader2,
  Terminal,
  Building2,
  Zap,
  ArrowRight
} from 'lucide-react';

type DeploymentStatus = 'idle' | 'generating' | 'compiling' | 'deploying' | 'complete';
type Chain = 'ethereum' | 'polygon' | 'arbitrum' | 'base' | 'solana';

export function KlerosTeamView() {
  const [deploymentStatus, setDeploymentStatus] = useState<DeploymentStatus>('idle');
  const [selectedChains, setSelectedChains] = useState<Chain[]>([]);
  const [generatedCode, setGeneratedCode] = useState('');
  const [deployedAddresses, setDeployedAddresses] = useState<Record<Chain, string>>({} as any);

  const chains: { id: Chain; name: string; logo: string; color: string }[] = [
    { id: 'ethereum', name: 'Ethereum', logo: '/logos/ethereum.png', color: 'cyan' },
    { id: 'polygon', name: 'Polygon', logo: '/logos/polygon.png', color: 'purple' },
    { id: 'arbitrum', name: 'Arbitrum', logo: '/logos/arbitrum.png', color: 'blue' },
    { id: 'base', name: 'Base', logo: '/logos/base.png', color: 'indigo' },
    { id: 'solana', name: 'Solana', logo: '/logos/solana.png', color: 'pink' },
  ];

  const toggleChain = (chainId: Chain) => {
    setSelectedChains(prev =>
      prev.includes(chainId)
        ? prev.filter(id => id !== chainId)
        : [...prev, chainId]
    );
  };

  const startDeployment = async () => {
    if (selectedChains.length === 0) return;

    // Step 1: Generate
    setDeploymentStatus('generating');
    await new Promise(resolve => setTimeout(resolve, 1500));
    setGeneratedCode(generateContractCode(selectedChains));

    // Step 2: Compile
    setDeploymentStatus('compiling');
    await new Promise(resolve => setTimeout(resolve, 1500));

    // Step 3: Deploy
    setDeploymentStatus('deploying');
    const addresses: Partial<Record<Chain, string>> = {};
    for (const chain of selectedChains) {
      addresses[chain] = `0x${Math.random().toString(16).slice(2, 10)}...`;
      await new Promise(resolve => setTimeout(resolve, 1000));
    }
    setDeployedAddresses(addresses as any);

    // Step 4: Complete
    setDeploymentStatus('complete');
  };

  const generateContractCode = (chains: Chain[]) => {
    return `// Generated Kleros Arbitrator Contracts
// Chains: ${chains.join(', ')}
// Template: kleros-arbitrator.sol.hbs

pragma solidity ^0.8.20;

contract KlerosArbitrator {
    uint256 public arbitrationCost;
    uint256 public constant MIN_JURORS = 3;
    
    event DisputeCreation(uint256 indexed disputeID, address indexed creator);
    
    function createDispute(
        uint256 _numJurors,
        string memory _metadataURI
    ) external payable returns (uint256) {
        require(msg.value >= arbitrationCost, "Insufficient payment");
        require(_numJurors >= MIN_JURORS, "Too few jurors");
        
        // Create dispute logic...
        return block.timestamp; // Mock dispute ID
    }
}`;
  };

  return (
    <div className="space-y-8 relative">
      {/* Title */}
      <div>
        <h2 className="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-purple-400 via-pink-400 to-cyan-400 mb-3">
          Kleros Team: Internal Operations
        </h2>
        <p className="text-lg text-purple-200/70">
          Deploy Kleros arbitrator contracts to multiple chains using OASIS + AssetRail SC-Gen
        </p>
      </div>

      {/* Step 1: Select Chains */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(138,43,226,0.3)] border border-purple-500/30 p-6">
        <div className="flex items-center gap-2 mb-4">
          <Database className="w-5 h-5 text-purple-400 drop-shadow-[0_0_10px_rgba(168,85,247,0.8)]" />
          <h3 className="text-xl font-semibold text-purple-300">
            Step 1: Select Target Chains
          </h3>
        </div>
        <p className="text-sm text-slate-400 mb-4">
          Choose which blockchains to deploy Kleros arbitrator contracts to
        </p>
        <div className="grid grid-cols-2 md:grid-cols-5 gap-3">
          {chains.map((chain) => (
            <button
              key={chain.id}
              onClick={() => toggleChain(chain.id)}
              disabled={deploymentStatus !== 'idle'}
              className={`p-4 rounded-lg border-2 transition-all ${
                selectedChains.includes(chain.id)
                  ? `border-${chain.color}-500 bg-${chain.color}-500/20 shadow-[0_0_15px_rgba(0,255,255,0.4)]`
                  : 'border-slate-700 hover:border-purple-500/50 bg-slate-900/40'
              } ${deploymentStatus !== 'idle' ? 'opacity-50 cursor-not-allowed' : 'cursor-pointer hover:shadow-[0_0_15px_rgba(168,85,247,0.2)]'}`}
            >
              <div className="w-12 h-12 mx-auto mb-2 flex items-center justify-center">
                <img 
                  src={chain.logo} 
                  alt={chain.name} 
                  className={`w-full h-full ${selectedChains.includes(chain.id) ? 'opacity-100' : 'opacity-70'}`}
                />
              </div>
              <div className={`font-medium text-sm ${selectedChains.includes(chain.id) ? `text-${chain.color}-300` : 'text-slate-300'}`}>
                {chain.name}
              </div>
              {selectedChains.includes(chain.id) && (
                <CheckCircle2 className={`w-4 h-4 text-${chain.color}-400 mx-auto mt-2 drop-shadow-[0_0_8px_rgba(0,255,255,0.6)]`} />
              )}
            </button>
          ))}
        </div>
        <div className="mt-4 text-sm text-cyan-400/70">
          {selectedChains.length} chain{selectedChains.length !== 1 ? 's' : ''} selected
        </div>
      </div>

      {/* Step 2: Generate & Deploy */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(0,255,255,0.2)] border border-cyan-500/30 p-6">
        <div className="flex items-center gap-2 mb-4">
          <FileCode className="w-5 h-5 text-cyan-400 drop-shadow-[0_0_10px_rgba(0,255,255,0.8)]" />
          <h3 className="text-xl font-semibold text-cyan-300">
            Step 2: Generate, Compile & Deploy
          </h3>
        </div>
        
        <button
          onClick={startDeployment}
          disabled={selectedChains.length === 0 || deploymentStatus !== 'idle'}
          className={`px-6 py-3 rounded-lg font-medium transition-all ${
            selectedChains.length > 0 && deploymentStatus === 'idle'
              ? 'bg-gradient-to-r from-purple-600 to-pink-600 text-white hover:from-purple-500 hover:to-pink-500 shadow-[0_0_20px_rgba(168,85,247,0.5)] hover:shadow-[0_0_30px_rgba(168,85,247,0.7)]'
              : 'bg-slate-700/50 text-slate-500 cursor-not-allowed border border-slate-600/50'
          }`}
        >
          {deploymentStatus === 'idle' && (
            <span className="flex items-center gap-2">
              <Rocket className="w-4 h-4" />
              Start Deployment
            </span>
          )}
          {deploymentStatus === 'generating' && (
            <span className="flex items-center gap-2">
              <Loader2 className="w-4 h-4 animate-spin" />
              Generating Contracts from Templates...
            </span>
          )}
          {deploymentStatus === 'compiling' && (
            <span className="flex items-center gap-2">
              <Loader2 className="w-4 h-4 animate-spin" />
              Compiling for {selectedChains.length} Chain(s)...
            </span>
          )}
          {deploymentStatus === 'deploying' && (
            <span className="flex items-center gap-2">
              <Loader2 className="w-4 h-4 animate-spin" />
              Deploying via OASIS...
            </span>
          )}
          {deploymentStatus === 'complete' && (
            <span className="flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4" />
              Deployment Complete!
            </span>
          )}
        </button>

        {/* Deployment Progress */}
        {deploymentStatus !== 'idle' && (
          <div className="mt-6 space-y-4">
            {/* Generated Code */}
            {generatedCode && (
              <div className="border border-cyan-500/30 rounded-lg p-4 bg-slate-900/60 backdrop-blur-sm shadow-[0_0_20px_rgba(0,255,255,0.2)]">
                <div className="flex items-center gap-2 mb-2">
                  <Terminal className="w-4 h-4 text-cyan-400" />
                  <span className="text-sm font-medium text-cyan-300">Generated Contract Code:</span>
                </div>
                <pre className="text-xs bg-slate-950 text-emerald-400 p-4 rounded overflow-x-auto font-mono border border-emerald-500/20 shadow-[inset_0_0_20px_rgba(16,185,129,0.1)]">
                  {generatedCode}
                </pre>
              </div>
            )}

            {/* Deployed Addresses */}
            {Object.keys(deployedAddresses).length > 0 && (
              <div className="border border-emerald-500/30 rounded-lg p-4 bg-emerald-900/20 backdrop-blur-sm shadow-[0_0_20px_rgba(16,185,129,0.3)]">
                <div className="flex items-center gap-2 mb-3">
                  <CheckCircle2 className="w-4 h-4 text-emerald-400 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]" />
                  <span className="text-sm font-medium text-emerald-300">Deployed Contracts:</span>
                </div>
                <div className="space-y-2">
                  {Object.entries(deployedAddresses).map(([chain, address]) => (
                    <div key={chain} className="flex justify-between items-center text-sm p-2 bg-slate-800/40 rounded border border-cyan-500/20">
                      <span className="font-medium text-cyan-300 capitalize">{chain}:</span>
                      <code className="bg-slate-950/60 px-3 py-1 rounded border border-cyan-500/30 font-mono text-xs text-emerald-400 shadow-[0_0_10px_rgba(0,255,255,0.2)]">
                        {address}
                      </code>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>

      {/* Step 3: Monitoring Dashboard */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(236,72,153,0.2)] border border-pink-500/30 p-6">
        <div className="flex items-center gap-2 mb-4">
          <Activity className="w-5 h-5 text-pink-400 drop-shadow-[0_0_10px_rgba(236,72,153,0.8)]" />
          <h3 className="text-xl font-semibold text-pink-300">
            Step 3: Multi-Chain Monitoring Dashboard
          </h3>
        </div>
        <p className="text-sm text-slate-400 mb-4">
          Unified view of all disputes across all chains
        </p>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-gradient-to-br from-blue-900/40 to-blue-800/20 rounded-lg p-4 border border-blue-500/40 shadow-[0_0_20px_rgba(59,130,246,0.3)]">
            <div className="text-3xl font-bold text-blue-300 mb-1 drop-shadow-[0_0_10px_rgba(59,130,246,0.8)]">247</div>
            <div className="text-sm text-blue-200">Active Disputes</div>
            <div className="text-xs text-blue-400/60 mt-1">Across all chains</div>
          </div>
          <div className="bg-gradient-to-br from-emerald-900/40 to-emerald-800/20 rounded-lg p-4 border border-emerald-500/40 shadow-[0_0_20px_rgba(16,185,129,0.3)]">
            <div className="text-3xl font-bold text-emerald-300 mb-1 drop-shadow-[0_0_10px_rgba(16,185,129,0.8)]">1,523</div>
            <div className="text-sm text-emerald-200">Total Resolved</div>
            <div className="text-xs text-emerald-400/60 mt-1">Last 30 days</div>
          </div>
          <div className="bg-gradient-to-br from-purple-900/40 to-purple-800/20 rounded-lg p-4 border border-purple-500/40 shadow-[0_0_20px_rgba(168,85,247,0.3)]">
            <div className="text-3xl font-bold text-purple-300 mb-1 drop-shadow-[0_0_10px_rgba(168,85,247,0.8)]">$48.2k</div>
            <div className="text-sm text-purple-200">Fees Collected</div>
            <div className="text-xs text-purple-400/60 mt-1">This month</div>
          </div>
        </div>

        {/* Chain-specific stats */}
        {deploymentStatus === 'complete' && (
          <div className="mt-6">
            <div className="text-sm font-medium text-pink-300 mb-3 flex items-center gap-2">
              <Zap className="w-4 h-4" />
              Disputes by Chain:
            </div>
            <div className="space-y-2">
              {selectedChains.map((chain, idx) => {
                const chainData = chains.find(c => c.id === chain);
                const disputes = Math.floor(Math.random() * 100);
                const percentage = Math.random() * 100;
                
                return (
                  <div key={chain} className="flex items-center justify-between p-3 bg-slate-900/40 backdrop-blur-sm rounded-lg border border-purple-500/20">
                    <div className="flex items-center gap-3">
                      <div className="w-8 h-8">
                        <img src={chainData?.logo} alt={chain} className="w-full h-full" />
                      </div>
                      <span className="capitalize font-medium text-slate-200">{chain}</span>
                    </div>
                    <div className="flex items-center gap-4">
                      <div className="text-sm text-cyan-300">
                        {disputes} active
                      </div>
                      <div className="w-24 bg-slate-700/50 rounded-full h-2 overflow-hidden">
                        <div
                          className={`bg-gradient-to-r from-cyan-500 to-purple-500 h-2 rounded-full shadow-[0_0_10px_rgba(0,255,255,0.5)]`}
                          style={{ width: `${percentage}%` }}
                        />
                      </div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        )}
      </div>

      {/* Key Benefits */}
      <div className="relative bg-gradient-to-br from-purple-900/40 to-pink-900/30 rounded-lg border border-purple-500/40 p-6 backdrop-blur-sm shadow-[0_0_30px_rgba(168,85,247,0.2)]">
        <div className="absolute inset-0 bg-gradient-to-r from-purple-500/5 to-pink-500/5 rounded-lg blur-xl" />
        
        <div className="relative">
          <h3 className="font-semibold text-purple-300 mb-4 flex items-center gap-2">
            <Rocket className="w-5 h-5 drop-shadow-[0_0_10px_rgba(168,85,247,0.8)]" />
            Time Savings with OASIS + AssetRail
          </h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-4 border border-slate-600/50">
              <div className="text-sm text-slate-400 mb-2">Without OASIS/AssetRail:</div>
              <div className="space-y-1 text-sm text-slate-300">
                <div className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  2-4 weeks per chain
                </div>
                <div className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  Manual deployment each time
                </div>
                <div className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  Separate monitoring dashboards
                </div>
                <div className="flex items-center gap-2">
                  <span className="w-1 h-1 bg-red-400 rounded-full" />
                  Inconsistent implementations
                </div>
              </div>
            </div>
            <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg p-4 border-2 border-emerald-500/50 shadow-[0_0_20px_rgba(16,185,129,0.3)]">
              <div className="text-sm text-emerald-400 font-medium mb-2 flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4" />
                With OASIS/AssetRail:
              </div>
              <div className="space-y-1 text-sm text-slate-300">
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-cyan-400" />
                  <span>1-2 days for ALL chains</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-purple-400" />
                  <span>One-command deployment</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-pink-400" />
                  <span>Unified monitoring dashboard</span>
                </div>
                <div className="flex items-center gap-2">
                  <CheckCircle2 className="w-4 h-4 text-emerald-400" />
                  <span>100% consistency guaranteed</span>
                </div>
              </div>
            </div>
          </div>
          <div className="mt-4 p-4 bg-gradient-to-r from-purple-900/60 to-pink-900/60 rounded-lg border border-purple-500/40 backdrop-blur-sm shadow-[0_0_20px_rgba(236,72,153,0.3)]">
            <div className="text-2xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 to-pink-400 mb-1">
              90% Time Savings
            </div>
            <div className="text-sm text-purple-300">$200k-400k/year in engineering costs</div>
          </div>
        </div>
      </div>
    </div>
  );
}
