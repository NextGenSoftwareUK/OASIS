import { useState } from 'react';

type Chain = {
  name: string;
  symbol: string;
  balance: number;
  color: string;
  logo: string;
};

type Proposal = {
  id: number;
  title: string;
  description: string;
  votesFor: number;
  votesAgainst: number;
  status: 'active' | 'passed' | 'rejected';
  endsIn: string;
};

const chains: Chain[] = [
  { name: 'Ethereum', symbol: 'ETH', balance: 50000, color: '#627EEA', logo: '⟠' },
  { name: 'Solana', symbol: 'SOL', balance: 75000, color: '#14F195', logo: '◎' },
  { name: 'Bitcoin', symbol: 'BTC', balance: 30000, color: '#F7931A', logo: '₿' },
  { name: 'Polygon', symbol: 'MATIC', balance: 45000, color: '#8247E5', logo: '⬡' },
  { name: 'Arbitrum', symbol: 'ARB', balance: 40000, color: '#28A0F0', logo: '🔷' },
  { name: 'Base', symbol: 'BASE', balance: 35000, color: '#0052FF', logo: '🔵' },
  { name: 'Radix', symbol: 'XRD', balance: 25000, color: '#00C389', logo: 'ⓧ' },
  { name: 'Kadena', symbol: 'KDA', balance: 20000, color: '#ED098F', logo: '🔗' },
];

const proposals: Proposal[] = [
  {
    id: 1,
    title: 'Recognize Bitcoin as Legal Tender in UK',
    description: 'Make Bitcoin officially accepted for tax payments and government services',
    votesFor: 485000,
    votesAgainst: 125000,
    status: 'active',
    endsIn: '3 days',
  },
  {
    id: 2,
    title: 'Zero Capital Gains Tax on Crypto (Under £20k)',
    description: 'Align with Reform UK\'s £20k income tax threshold for crypto gains',
    votesFor: 520000,
    votesAgainst: 90000,
    status: 'active',
    endsIn: '5 days',
  },
  {
    id: 3,
    title: 'Ban All CBDCs in UK',
    description: 'Legislate against central bank digital currencies permanently',
    votesFor: 680000,
    votesAgainst: 45000,
    status: 'passed',
    endsIn: 'Passed',
  },
];

export default function ReformTokenDemo() {
  const [selectedChain, setSelectedChain] = useState<Chain | null>(null);
  const totalBalance = chains.reduce((sum, chain) => sum + chain.balance, 0);
  const marketCap = totalBalance * 0.15; // £0.15 per token
  const holders = 125000;

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 text-white p-8 shadow-2xl">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center justify-between">
            <div>
              <div className="flex items-center gap-4 mb-2">
                <div className="text-5xl">🇬🇧</div>
                <div>
                  <h1 className="text-4xl font-bold">ReformToken ($REFORM)</h1>
                  <p className="text-blue-200 text-xl">Great British Bitcoin Strategy</p>
                </div>
              </div>
              <p className="text-blue-300 mt-2">Multi-Chain British Sovereign Token • CBDC Alternative</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-blue-300">Market Cap</p>
              <p className="text-4xl font-bold text-white">£{(marketCap / 1000000).toFixed(1)}M</p>
              <p className="text-sm text-green-400 mt-1">+23.5% (24h)</p>
            </div>
          </div>
        </div>
      </div>

      {/* Stats Bar */}
      <div className="bg-slate-800 border-b border-slate-700 py-4">
        <div className="max-w-7xl mx-auto px-8">
          <div className="grid grid-cols-2 md:grid-cols-5 gap-4">
            <StatCard label="Price" value="£0.15" color="text-white" subtitle="Per token" />
            <StatCard label="Total Supply" value="1B" color="text-cyan-400" subtitle="Fixed (no inflation)" />
            <StatCard label="Holders" value={holders.toLocaleString()} color="text-green-400" subtitle="UK & Global" />
            <StatCard label="Blockchains" value="15" color="text-purple-400" subtitle="Simultaneous" />
            <StatCard label="APY (Staking)" value="7-12%" color="text-yellow-400" subtitle="Earn yield" />
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto p-8 space-y-8">
        
        {/* Multi-Chain Distribution */}
        <div className="rounded-2xl bg-slate-800/50 border border-cyan-500/30 p-8">
          <h2 className="text-2xl font-bold text-white mb-6">
            Multi-Chain Distribution (OASIS Web4 Technology)
          </h2>
          <p className="text-slate-300 mb-6">
            $REFORM exists natively on 15+ blockchains simultaneously. No bridges, no wrapping, instant cross-chain transfers.
          </p>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
            {chains.map((chain, i) => (
              <button
                key={i}
                onClick={() => setSelectedChain(chain)}
                className={`p-4 rounded-xl border-2 transition-all ${
                  selectedChain?.name === chain.name
                    ? 'border-cyan-500 bg-cyan-900/30 shadow-lg shadow-cyan-500/20'
                    : 'border-slate-700 bg-slate-900/30 hover:border-cyan-500/50'
                }`}
              >
                <div className="text-3xl mb-2" style={{ color: chain.color }}>
                  {chain.logo}
                </div>
                <p className="text-sm font-semibold text-white">{chain.name}</p>
                <p className="text-xs text-slate-400 mt-1">{chain.balance.toLocaleString()} $REFORM</p>
              </button>
            ))}
          </div>

          {selectedChain && (
            <div className="mt-6 p-6 bg-gradient-to-br from-cyan-900/30 to-blue-900/30 rounded-xl border border-cyan-500/30">
              <div className="flex items-center justify-between mb-4">
                <div className="flex items-center gap-3">
                  <div className="text-4xl" style={{ color: selectedChain.color }}>
                    {selectedChain.logo}
                  </div>
                  <div>
                    <h3 className="text-xl font-bold text-white">{selectedChain.name}</h3>
                    <p className="text-cyan-400">Native $REFORM Implementation</p>
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-3xl font-bold text-white">{selectedChain.balance.toLocaleString()}</p>
                  <p className="text-sm text-slate-400">$REFORM on {selectedChain.name}</p>
                </div>
              </div>
              <div className="grid grid-cols-3 gap-4 mt-4">
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-xs text-slate-400">% of Total</p>
                  <p className="text-lg font-bold text-cyan-400">
                    {((selectedChain.balance / totalBalance) * 100).toFixed(1)}%
                  </p>
                </div>
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-xs text-slate-400">Value (£)</p>
                  <p className="text-lg font-bold text-green-400">
                    £{((selectedChain.balance * 0.15) / 1000).toFixed(1)}k
                  </p>
                </div>
                <div className="bg-slate-900/50 rounded-lg p-3">
                  <p className="text-xs text-slate-400">Sync Status</p>
                  <p className="text-lg font-bold text-green-400">✓ Live</p>
                </div>
              </div>
            </div>
          )}

          <div className="mt-6 p-4 bg-blue-900/20 rounded-lg border border-blue-500/30">
            <p className="text-sm text-blue-200">
              💡 <strong>How it works:</strong> OASIS HyperDrive auto-replicates $REFORM across all chains. 
              Buy on Solana? Instantly available on Ethereum. Transfer on Bitcoin? Reflected on Polygon immediately. 
              <strong> No bridges needed.</strong>
            </p>
          </div>
        </div>

        {/* Governance Dashboard */}
        <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-8">
          <h2 className="text-2xl font-bold text-white mb-6">Governance: Vote on UK Crypto Policy</h2>
          <p className="text-slate-300 mb-6">
            $REFORM holders vote on crypto policy proposals. Results influence Reform UK's platform.
          </p>

          <div className="space-y-4">
            {proposals.map((proposal) => (
              <div key={proposal.id} className="bg-slate-900/50 border border-slate-700 rounded-xl p-6 hover:border-cyan-500/30 transition-colors">
                <div className="flex items-start justify-between mb-4">
                  <div className="flex-1">
                    <div className="flex items-center gap-3 mb-2">
                      <h3 className="text-lg font-bold text-white">{proposal.title}</h3>
                      <span className={`text-xs px-3 py-1 rounded-full ${
                        proposal.status === 'active' ? 'bg-cyan-900/50 text-cyan-400 border border-cyan-500/30' :
                        proposal.status === 'passed' ? 'bg-green-900/50 text-green-400 border border-green-500/30' :
                        'bg-red-900/50 text-red-400 border border-red-500/30'
                      }`}>
                        {proposal.status === 'active' ? `⏱️ ${proposal.endsIn}` : 
                         proposal.status === 'passed' ? '✓ Passed' : '✗ Rejected'}
                      </span>
                    </div>
                    <p className="text-sm text-slate-400">{proposal.description}</p>
                  </div>
                </div>

                <div className="space-y-3">
                  <VoteBar 
                    label="For" 
                    votes={proposal.votesFor} 
                    total={proposal.votesFor + proposal.votesAgainst}
                    color="green"
                  />
                  <VoteBar 
                    label="Against" 
                    votes={proposal.votesAgainst} 
                    total={proposal.votesFor + proposal.votesAgainst}
                    color="red"
                  />
                </div>

                {proposal.status === 'active' && (
                  <div className="mt-4 flex gap-3">
                    <button className="flex-1 px-4 py-2 bg-green-600 hover:bg-green-500 text-white rounded-lg font-semibold transition-colors">
                      Vote For ✓
                    </button>
                    <button className="flex-1 px-4 py-2 bg-red-600 hover:bg-red-500 text-white rounded-lg font-semibold transition-colors">
                      Vote Against ✗
                    </button>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Staking */}
        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div className="rounded-2xl bg-gradient-to-br from-green-900/30 to-blue-900/30 border border-green-500/30 p-8">
            <h3 className="text-2xl font-bold text-white mb-4">Stake $REFORM, Earn Yield</h3>
            <p className="text-slate-300 mb-6">Lock your tokens to earn rewards from UK blockchain ecosystem</p>

            <div className="space-y-4">
              <StakingOption period="30 days" apy="7%" amount="10,000" rewards="58.33" />
              <StakingOption period="90 days" apy="10%" amount="10,000" rewards="250" />
              <StakingOption period="365 days" apy="12%" amount="10,000" rewards="1,200" />
            </div>

            <div className="mt-6 p-4 bg-green-900/20 rounded-lg border border-green-500/30">
              <p className="text-sm text-green-200">
                💰 <strong>Yield Sources:</strong> Government blockchain contracts, DEX trading fees, ecosystem growth.
                <strong> Not Ponzi-nomics.</strong>
              </p>
            </div>
          </div>

          <div className="rounded-2xl bg-gradient-to-br from-purple-900/30 to-blue-900/30 border border-purple-500/30 p-8">
            <h3 className="text-2xl font-bold text-white mb-4">ReformStable (GBP-Pegged)</h3>
            <p className="text-slate-300 mb-6">Privacy-preserving stablecoin alternative to CBDCs</p>

            <div className="space-y-4">
              <div className="p-4 bg-slate-900/50 rounded-lg border border-purple-500/30">
                <div className="flex justify-between items-center mb-3">
                  <p className="text-slate-400">1 $REFORMSTABLE =</p>
                  <p className="text-2xl font-bold text-white">£1.00</p>
                </div>
                <div className="flex justify-between items-center text-sm">
                  <p className="text-slate-400">Backed by:</p>
                  <p className="text-purple-400">UK Treasury Bonds</p>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-3">
                <FeatureBox icon="🔐" label="Self-Custody" />
                <FeatureBox icon="🔒" label="Private (ZK)" />
                <FeatureBox icon="🇬🇧" label="British Reserves" />
                <FeatureBox icon="⚡" label="Multi-Chain" />
              </div>
            </div>

            <div className="mt-6 p-4 bg-purple-900/20 rounded-lg border border-purple-500/30">
              <p className="text-sm text-purple-200">
                🛡️ <strong>CBDC Alternative:</strong> Same stability as government currency, none of the surveillance.
              </p>
            </div>
          </div>
        </div>

        {/* Use Cases */}
        <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-8">
          <h2 className="text-2xl font-bold text-white mb-6">Real-World Use Cases</h2>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <UseCaseCard
              icon="🗳️"
              title="Political Engagement"
              description="Vote on crypto policy, earn rewards for activism, token-gated Reform UK communities"
            />
            <UseCaseCard
              icon="🏪"
              title="Merchant Acceptance"
              description="1,000+ UK businesses accept $REFORM. Pubs, shops, services. 0% fees for merchants."
              stat="Target: 1k businesses by Year 1"
            />
            <UseCaseCard
              icon="🌍"
              title="Global UK Support"
              description="Diaspora Brits and international supporters can own $REFORM. Represent UK values globally."
              stat="Accessible to 70M+ Brits abroad"
            />
          </div>
        </div>

        {/* Why ReformToken Matters */}
        <div className="rounded-2xl bg-gradient-to-br from-blue-900 to-blue-800 p-8">
          <h2 className="text-2xl font-bold text-white mb-6 text-center">
            Why $REFORM Changes British Politics
          </h2>
          
          <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div className="space-y-4">
              <h3 className="text-xl font-semibold text-cyan-400">For Reform UK</h3>
              <ul className="space-y-2 text-sm text-slate-200">
                <li className="flex gap-2"><span className="text-green-400">✓</span> Attract crypto-native voters (18-35)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Innovative fundraising (transparent, on-chain)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> CBDC opposition credibility (offer alternative)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Position as UK's blockchain-first party</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Global reach (international supporters)</li>
              </ul>
            </div>

            <div className="space-y-4">
              <h3 className="text-xl font-semibold text-cyan-400">For UK Citizens</h3>
              <ul className="space-y-2 text-sm text-slate-200">
                <li className="flex gap-2"><span className="text-green-400">✓</span> CBDC alternative (preserve financial privacy)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Direct democracy (vote on crypto policy)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Earn yield (7-12% APY staking)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Multi-chain freedom (15+ blockchains)</li>
                <li className="flex gap-2"><span className="text-green-400">✓</span> Support British sovereignty (digital + national)</li>
              </ul>
            </div>
          </div>

          <div className="mt-8 pt-6 border-t border-blue-700">
            <p className="text-center text-lg text-blue-200 italic">
              "Make UK the global crypto capital. ReformToken is the first step."
            </p>
          </div>
        </div>

        {/* Token Allocation */}
        <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-8">
          <h2 className="text-2xl font-bold text-white mb-6">Token Allocation</h2>
          
          <div className="space-y-3">
            <AllocationBar label="Public Sale (UK Priority)" percentage={30} color="cyan" />
            <AllocationBar label="Reform UK Treasury" percentage={20} color="blue" />
            <AllocationBar label="Development & Tech" percentage={15} color="purple" />
            <AllocationBar label="Ecosystem Incentives" percentage={15} color="green" />
            <AllocationBar label="Strategic Partners" percentage={10} color="yellow" />
            <AllocationBar label="Team & Advisors (4yr vest)" percentage={5} color="orange" />
            <AllocationBar label="Liquidity Provision" percentage={5} color="pink" />
          </div>
        </div>

        {/* Call to Action */}
        <div className="rounded-2xl bg-gradient-to-r from-green-900/50 to-blue-900/50 border border-green-500/30 p-8 text-center">
          <h3 className="text-3xl font-bold text-white mb-3">
            British Sovereignty • Blockchain Freedom
          </h3>
          <p className="text-green-200 text-lg max-w-3xl mx-auto mb-6">
            ReformToken combines Reform UK's political vision with OASIS Web4 technology to create 
            the UK's first multi-chain sovereign token—an alternative to CBDCs and a foundation for 
            the Great British Bitcoin Strategy.
          </p>
          <div className="flex gap-4 justify-center">
            <button className="px-8 py-4 bg-green-600 hover:bg-green-500 text-white rounded-lg font-bold text-lg transition-colors">
              Buy $REFORM (Coming Soon)
            </button>
            <button className="px-8 py-4 bg-blue-600 hover:bg-blue-500 text-white rounded-lg font-bold text-lg transition-colors">
              Stake & Earn 12% APY
            </button>
          </div>
        </div>

      </div>
    </div>
  );
}

function StatCard({ label, value, color, subtitle }: { label: string; value: string; color: string; subtitle?: string }) {
  return (
    <div className="bg-slate-800/50 rounded-lg p-4 border border-slate-700">
      <p className="text-xs uppercase tracking-wide text-slate-400 mb-1">{label}</p>
      <p className={`text-2xl font-bold ${color}`}>{value}</p>
      {subtitle && <p className="text-xs text-slate-500 mt-1">{subtitle}</p>}
    </div>
  );
}

function StakingOption({ period, apy, amount, rewards }: { period: string; apy: string; amount: string; rewards: string }) {
  return (
    <div className="bg-slate-900/50 border border-green-500/30 rounded-lg p-4 hover:border-green-500/50 transition-colors cursor-pointer">
      <div className="flex justify-between items-center mb-3">
        <div>
          <p className="text-white font-semibold">{period} Lock</p>
          <p className="text-xs text-slate-400">Stake {amount} $REFORM</p>
        </div>
        <div className="text-right">
          <p className="text-2xl font-bold text-green-400">{apy}</p>
          <p className="text-xs text-slate-400">APY</p>
        </div>
      </div>
      <div className="pt-3 border-t border-slate-700">
        <p className="text-xs text-slate-400">Est. Rewards</p>
        <p className="text-lg font-bold text-white">+{rewards} $REFORM</p>
      </div>
    </div>
  );
}

function VoteBar({ label, votes, total, color }: { label: string; votes: number; total: number; color: string }) {
  const percentage = (votes / total) * 100;
  const colorClasses = {
    green: 'bg-green-500',
    red: 'bg-red-500',
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-2">
        <span className="text-sm text-slate-400">{label}</span>
        <span className="text-sm font-semibold text-white">{votes.toLocaleString()} votes ({percentage.toFixed(1)}%)</span>
      </div>
      <div className="h-3 bg-slate-700 rounded-full overflow-hidden">
        <div 
          className={`h-full ${colorClasses[color as keyof typeof colorClasses]} transition-all`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}

function FeatureBox({ icon, label }: { icon: string; label: string }) {
  return (
    <div className="bg-slate-900/50 rounded-lg p-3 border border-purple-500/30 text-center">
      <div className="text-2xl mb-1">{icon}</div>
      <p className="text-xs text-purple-300 font-medium">{label}</p>
    </div>
  );
}

function UseCaseCard({ icon, title, description, stat }: { icon: string; title: string; description: string; stat?: string }) {
  return (
    <div className="bg-slate-900/50 border border-cyan-500/30 rounded-xl p-6">
      <div className="text-4xl mb-3">{icon}</div>
      <h4 className="text-lg font-bold text-white mb-2">{title}</h4>
      <p className="text-sm text-slate-300 mb-4">{description}</p>
      {stat && (
        <p className="text-xs text-cyan-400 font-semibold">{stat}</p>
      )}
    </div>
  );
}

function AllocationBar({ label, percentage, color }: { label: string; percentage: number; color: string }) {
  const colorClasses = {
    cyan: 'bg-cyan-500',
    blue: 'bg-blue-500',
    purple: 'bg-purple-500',
    green: 'bg-green-500',
    yellow: 'bg-yellow-500',
    orange: 'bg-orange-500',
    pink: 'bg-pink-500',
  };

  return (
    <div className="flex items-center gap-4">
      <div className="flex-1">
        <div className="flex justify-between items-center mb-2">
          <span className="text-sm text-slate-300">{label}</span>
          <span className="text-sm font-semibold text-white">{percentage}%</span>
        </div>
        <div className="h-3 bg-slate-700 rounded-full overflow-hidden">
          <div 
            className={`h-full ${colorClasses[color as keyof typeof colorClasses]} transition-all`}
            style={{ width: `${percentage}%` }}
          />
        </div>
      </div>
    </div>
  );
}

