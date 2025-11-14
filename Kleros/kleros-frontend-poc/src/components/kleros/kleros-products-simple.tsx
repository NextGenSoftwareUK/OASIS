'use client';

import { useState } from 'react';
import { 
  Gavel, 
  Eye,
  List,
  Shield,
  Building,
  CheckCircle2,
  ArrowRight,
  Zap,
  TrendingUp,
  DollarSign
} from 'lucide-react';

type ProductId = 'court' | 'oracle' | 'curate' | 'escrow' | 'governor';

interface Product {
  id: ProductId;
  name: string;
  tagline: string;
  icon: any;
  description: string;
  useCase: string;
  currentState: {
    chains: string;
    cost: string;
    deployment: string;
    partners: string;
  };
  withOASIS: {
    chains: string;
    cost: string;
    deployment: string;
    partners: string;
  };
  targets: string[];
  integrationPattern: string;
  enhancements: string[];
}

export function KlerosProductsSimple() {
  const [selectedProduct, setSelectedProduct] = useState<ProductId>('court');

  const products: Product[] = [
    {
      id: 'court',
      name: 'Kleros Court',
      tagline: 'Arbitration-as-a-Service',
      icon: Gavel,
      description: 'Decentralized dispute resolution via crowdsourced jurors',
      useCase: 'NFT disputes, escrow conflicts, work quality, DAO governance',
      currentState: {
        chains: '3-5 EVM chains (Ethereum, Polygon, Gnosis)',
        cost: '$50 per dispute (Ethereum)',
        deployment: '2-4 weeks per chain',
        partners: 'Manual integration, 1-2 week setup'
      },
      withOASIS: {
        chains: '15+ chains (EVM + Solana)',
        cost: '$2-50 (auto-optimized per dispute value)',
        deployment: '1-2 days for ALL chains',
        partners: 'Auto-generated SDKs, 2-3 hour setup'
      },
      targets: ['OpenSea (NFT)', 'Uniswap (OTC)', 'Gitcoin (Bounties)'],
      integrationPattern: 'ERC-792 (IArbitrable interface)',
      enhancements: [
        'Multi-chain deployment: Deploy arbitrator contracts to Solana, Avalanche, Cosmos (not just EVM)',
        'Cost optimization: Route small disputes to Polygon ($2), large to Ethereum ($50)',
        'Cross-chain arbitration: Escrow on Chain A, arbitration on Chain B (96% cost savings)',
        'Evidence management: Guaranteed IPFS pinning via PinataOASIS, MongoDB backup'
      ]
    },
    {
      id: 'oracle',
      name: 'Kleros Oracle',
      tagline: 'Truth-as-a-Service',
      icon: Eye,
      description: 'Get decentralized answers to subjective questions via Reality.eth + Kleros',
      useCase: 'Prediction markets, event verification, content validation',
      currentState: {
        chains: '3 chains (Ethereum, Gnosis, Polygon)',
        cost: 'Bond escalation + arbitration fee',
        deployment: 'Requires Reality.eth deployment',
        partners: 'Complex bonding mechanism'
      },
      withOASIS: {
        chains: '15+ chains (deploy Reality.eth anywhere)',
        cost: 'Query on cheapest chain, use answer anywhere',
        deployment: '1 day (Reality.eth + Kleros together)',
        partners: 'Simplified SDK, auto-bonding'
      },
      targets: ['Polymarket (Predictions)', 'Omen (Markets)', 'Snapshot (DAO)'],
      integrationPattern: 'Reality.eth + Kleros Proxy',
      enhancements: [
        'Reality.eth everywhere: Deploy to Base, Arbitrum, Avalanche (5x market reach)',
        'Cross-chain queries: Ask on Polygon (cheap), use answer on Ethereum',
        'Unified API: Query oracles across all chains from one interface'
      ]
    },
    {
      id: 'curate',
      name: 'Kleros Curate',
      tagline: 'Data-Curation-as-a-Service',
      icon: List,
      description: 'Community-curated lists with dispute resolution for submissions',
      useCase: 'Token lists, address registries, verified content',
      currentState: {
        chains: 'Primarily Ethereum, some Polygon',
        cost: 'Deposit per submission',
        deployment: 'Per-chain lists (fragmented)',
        partners: 'Separate lists per chain'
      },
      withOASIS: {
        chains: 'Synchronized lists across 15+ chains',
        cost: 'Dispute on cheapest chain',
        deployment: '1 day to all chains',
        partners: 'Universal list API (query all chains)'
      },
      targets: ['Uniswap (Token Lists)', 'DEXes (Registries)', '1inch (Aggregation)'],
      integrationPattern: 'Kleros Curate API',
      enhancements: [
        'Synchronized lists: One token list, synced across 15+ chains automatically',
        'Dispute once, apply everywhere: Challenge on Polygon, applies to all chains',
        'Aggregated queries: Get combined list from all chains via OASIS'
      ]
    },
    {
      id: 'escrow',
      name: 'Kleros Escrow',
      tagline: 'Escrow-as-a-Service',
      icon: Shield,
      description: 'Pre-built escrow service with integrated Kleros arbitration',
      useCase: 'Freelancing, sales, service payments, bounties',
      currentState: {
        chains: 'Ethereum (high gas costs)',
        cost: '$50+ gas per transaction',
        deployment: 'Standalone product',
        partners: 'White-label or iframe embed'
      },
      withOASIS: {
        chains: 'Auto-route to cheapest (Polygon, Base)',
        cost: '$2-50 (smart routing: $50 → $2 for small)',
        deployment: 'Available everywhere',
        partners: 'Embed anywhere, costs 96% less'
      },
      targets: ['Braintrust (Freelance)', 'LaborX (Gig)', 'Request Network (Invoices)'],
      integrationPattern: 'Iframe or custom ERC-792',
      enhancements: [
        'Intelligent routing: $50 transaction → Polygon, $10k transaction → Ethereum',
        '96% cost savings: Enable micro-escrow ($5-50 transactions viable)',
        'Universal availability: Escrow on every chain OASIS supports'
      ]
    },
    {
      id: 'governor',
      name: 'Kleros Governor',
      tagline: 'Supreme-Court-as-a-Service',
      icon: Building,
      description: 'Enforce DAO governance proposals via decentralized arbitration',
      useCase: 'DAO proposal disputes, governance validation, execution enforcement',
      currentState: {
        chains: 'Ethereum, Gnosis (limited DAO support)',
        cost: 'High on Ethereum',
        deployment: 'Complex setup',
        partners: 'Few DAOs integrated'
      },
      withOASIS: {
        chains: 'Any DAO on any chain (even Solana DAOs)',
        cost: 'Arbitrate on cheap chain, execute on DAO chain',
        deployment: 'SafeSnap-style modules auto-generated',
        partners: 'One-click DAO integration'
      },
      targets: ['Arbitrum DAO', 'MakerDAO', 'Compound Gov', 'Solana Realms'],
      integrationPattern: 'Kleros Reality Module + Governor',
      enhancements: [
        'Any DAO, any chain: Solana DAOs can use Kleros governance via OASIS',
        'Cost optimization: DAO on Ethereum, arbitration on Polygon (cheaper)',
        'Auto-generated modules: SafeSnap-style Reality modules for any DAO'
      ]
    }
  ];

  const product = products.find(p => p.id === selectedProduct)!;
  const Icon = product.icon;

  return (
    <div className="space-y-8 relative">
      {/* Title */}
      <div className="text-center">
        <h2 className="text-4xl font-bold text-transparent bg-clip-text bg-gradient-to-r from-cyan-400 via-purple-400 to-pink-400 mb-3">
          5 Kleros Products Enhanced by OASIS
        </h2>
        <p className="text-lg text-cyan-100/80 max-w-3xl mx-auto">
          Kleros isn't just arbitration - it's a <strong className="text-purple-400">platform</strong> with 5 distinct products.  
          OASIS + AssetRail enhance <strong className="text-cyan-400">all of them</strong>.
        </p>
      </div>

      {/* Product Selector */}
      <div className="grid grid-cols-5 gap-2">
        {products.map((p) => {
          const PIcon = p.icon;
          const isSelected = selectedProduct === p.id;
          
          return (
            <button
              key={p.id}
              onClick={() => setSelectedProduct(p.id)}
              className={`p-4 rounded-lg border-2 transition-all ${
                isSelected
                  ? 'border-purple-500 bg-purple-500/20 shadow-[0_0_20px_rgba(138,43,226,0.4)]'
                  : 'border-slate-700 bg-slate-900/40 hover:border-purple-500/40'
              }`}
            >
              <PIcon className={`w-6 h-6 mx-auto mb-2 ${
                isSelected 
                  ? 'text-purple-400 drop-shadow-[0_0_10px_rgba(138,43,226,0.8)]' 
                  : 'text-slate-400'
              }`} />
              <div className={`text-xs font-medium ${
                isSelected ? 'text-purple-300' : 'text-slate-400'
              }`}>
                {p.name}
              </div>
            </button>
          );
        })}
      </div>

      {/* Product Details */}
      <div className="relative bg-gradient-to-br from-purple-900/40 to-purple-800/20 rounded-xl border-2 border-purple-500/40 p-8 backdrop-blur-sm shadow-[0_0_30px_rgba(138,43,226,0.3)]">
        <div className="absolute inset-0 bg-gradient-to-r from-purple-500/5 to-pink-500/5 rounded-xl blur-xl" />
        
        <div className="relative">
          {/* Header */}
          <div className="flex items-start gap-4 mb-6">
            <div className="p-4 bg-purple-500/20 rounded-lg border border-purple-500/40">
              <Icon className="w-10 h-10 text-purple-400 drop-shadow-[0_0_15px_rgba(138,43,226,0.8)]" />
            </div>
            <div className="flex-1">
              <h3 className="text-3xl font-bold text-purple-300 mb-2">
                {product.name}
              </h3>
              <p className="text-lg text-purple-200/80 mb-2">
                {product.tagline}
              </p>
              <p className="text-slate-300">
                {product.description}
              </p>
            </div>
          </div>

          {/* Use Cases */}
          <div className="mb-6 p-4 bg-slate-900/40 backdrop-blur-sm rounded-lg border border-cyan-500/20">
            <div className="text-sm font-medium text-cyan-300 mb-2 flex items-center gap-2">
              <Zap className="w-4 h-4" />
              Use Cases:
            </div>
            <p className="text-sm text-slate-300">{product.useCase}</p>
          </div>

          {/* Comparison */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {/* Current State */}
            <div className="bg-slate-900/60 backdrop-blur-sm rounded-lg p-5 border border-slate-600/50">
              <h4 className="font-semibold text-slate-300 mb-4">Current State</h4>
              <div className="space-y-3 text-sm">
                <div>
                  <div className="text-slate-400 mb-1">Chains:</div>
                  <div className="text-slate-200">{product.currentState.chains}</div>
                </div>
                <div>
                  <div className="text-slate-400 mb-1">Cost:</div>
                  <div className="text-slate-200">{product.currentState.cost}</div>
                </div>
                <div>
                  <div className="text-slate-400 mb-1">Deployment:</div>
                  <div className="text-slate-200">{product.currentState.deployment}</div>
                </div>
                <div>
                  <div className="text-slate-400 mb-1">Partner Integration:</div>
                  <div className="text-slate-200">{product.currentState.partners}</div>
                </div>
              </div>
            </div>

            {/* With OASIS */}
            <div className="bg-gradient-to-br from-emerald-900/40 to-emerald-800/20 rounded-lg p-5 border-2 border-emerald-500/50 shadow-[0_0_20px_rgba(16,185,129,0.3)]">
              <h4 className="font-semibold text-emerald-300 mb-4 flex items-center gap-2">
                <TrendingUp className="w-4 h-4" />
                With OASIS + AssetRail
              </h4>
              <div className="space-y-3 text-sm">
                <div>
                  <div className="text-slate-400 mb-1">Chains:</div>
                  <div className="text-emerald-300 font-medium">
                    {product.withOASIS.chains}
                  </div>
                </div>
                <div>
                  <div className="text-slate-400 mb-1">Cost:</div>
                  <div className="text-emerald-300 font-medium">
                    {product.withOASIS.cost}
                  </div>
                </div>
                <div>
                  <div className="text-slate-400 mb-1">Deployment:</div>
                  <div className="text-emerald-300 font-medium">
                    {product.withOASIS.deployment}
                  </div>
                </div>
                <div>
                  <div className="text-slate-400 mb-1">Partner Integration:</div>
                  <div className="text-emerald-300 font-medium">
                    {product.withOASIS.partners}
                  </div>
                </div>
              </div>
            </div>
          </div>

          {/* Integration Pattern */}
          <div className="mt-6 p-4 bg-slate-900/40 backdrop-blur-sm rounded-lg border border-purple-500/20">
            <div className="text-sm font-medium text-purple-300 mb-2 flex items-center gap-2">
              <CheckCircle2 className="w-4 h-4" />
              Integration Pattern:
            </div>
            <code className="text-sm text-cyan-400 font-mono">
              {product.integrationPattern}
            </code>
          </div>

          {/* Target Partners */}
          <div className="mt-6">
            <div className="text-sm font-medium text-pink-300 mb-3 flex items-center gap-2">
              <Building className="w-4 h-4" />
              Target Partners for Integration:
            </div>
            <div className="flex flex-wrap gap-2">
              {product.targets.map((target) => (
                <span
                  key={target}
                  className="px-3 py-1.5 bg-slate-800/60 border border-purple-500/30 rounded-lg text-sm text-slate-200"
                >
                  {target}
                </span>
              ))}
            </div>
          </div>

          {/* OASIS Enhancements */}
          <div className="mt-6 p-5 bg-slate-900/40 backdrop-blur-sm rounded-lg border border-purple-500/20">
            <h4 className="font-semibold text-purple-300 mb-4 flex items-center gap-2">
              <Zap className="w-5 h-5" />
              How OASIS Specifically Enhances {product.name}
            </h4>
            <ul className="space-y-2 text-sm text-slate-300">
              {product.enhancements.map((enhancement, idx) => (
                <li key={idx} className="flex items-start gap-2">
                  <ArrowRight className="w-4 h-4 text-cyan-400 flex-shrink-0 mt-0.5" />
                  <span dangerouslySetInnerHTML={{ __html: enhancement.replace(/\*\*(.*?)\*\*/g, '<strong class="text-purple-300">$1</strong>') }} />
                </li>
              ))}
            </ul>
          </div>
        </div>
      </div>

      {/* Key Benefits Summary */}
      <div className="bg-slate-800/60 backdrop-blur-sm rounded-lg shadow-[0_0_30px_rgba(16,185,129,0.2)] border border-emerald-500/30 p-6">
        <h3 className="font-semibold text-emerald-300 mb-4 flex items-center gap-2">
          <DollarSign className="w-5 h-5" />
          Impact Across All 5 Products
        </h3>
        
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-gradient-to-br from-purple-900/30 to-purple-800/20 rounded-lg p-4 border border-purple-500/30">
            <div className="text-2xl font-bold text-purple-300 mb-1">200-300%</div>
            <div className="text-sm text-slate-300">Market Expansion</div>
            <div className="text-xs text-slate-400 mt-1">3-5 chains → 15+ chains</div>
          </div>
          
          <div className="bg-gradient-to-br from-cyan-900/30 to-cyan-800/20 rounded-lg p-4 border border-cyan-500/30">
            <div className="text-2xl font-bold text-cyan-300 mb-1">90%</div>
            <div className="text-sm text-slate-300">Deployment Time Savings</div>
            <div className="text-xs text-slate-400 mt-1">2-4 weeks → 1-2 days</div>
          </div>
          
          <div className="bg-gradient-to-br from-emerald-900/30 to-emerald-800/20 rounded-lg p-4 border border-emerald-500/30">
            <div className="text-2xl font-bold text-emerald-300 mb-1">$500k-1M</div>
            <div className="text-sm text-slate-300">Annual Cost Savings</div>
            <div className="text-xs text-slate-400 mt-1">Engineering + infrastructure</div>
          </div>
        </div>
      </div>
    </div>
  );
}

