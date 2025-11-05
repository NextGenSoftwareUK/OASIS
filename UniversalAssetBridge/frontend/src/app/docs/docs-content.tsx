"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import { Book, Rocket, ArrowRightLeft, Wallet, Zap, DollarSign, Shield, Droplets } from "lucide-react";

const sections = [
  { id: "overview", title: "What is Web4?", icon: Book },
  { id: "create", title: "Create Tokens", icon: Rocket },
  { id: "migrate", title: "Migrate Existing Tokens", icon: ArrowRightLeft },
  { id: "liquidity", title: "Unified Liquidity Pools", icon: Droplets },
  { id: "use", title: "Using Web4 Tokens", icon: Wallet },
  { id: "hyperdrive", title: "How HyperDrive Works", icon: Zap },
  { id: "economics", title: "Financial Benefits", icon: DollarSign },
  { id: "security", title: "Security & Safety", icon: Shield },
];

export default function DocsContent() {
  const [activeSection, setActiveSection] = useState("overview");

  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-6">
        <div>
          <p className="text-sm uppercase tracking-[0.4em]" style={{color: 'var(--oasis-muted)'}}>Documentation</p>
          <div className="flex flex-col gap-4">
            <h2 className="mt-2 text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              Web4 Token Platform Guide
            </h2>
          </div>
          <p className="mt-3 max-w-3xl text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
            Complete guide to creating, migrating, and using Web4 tokens across multiple blockchains.
          </p>
        </div>

        {/* Documentation Layout */}
        <div className="glass-card gradient-ring relative overflow-hidden rounded-3xl border" style={{borderColor: 'var(--oasis-card-border)'}}>
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.2),transparent_70%)]" />
          <div className="relative grid gap-6 p-6 lg:grid-cols-[280px_1fr] lg:gap-10 lg:p-8">
            {/* Sidebar Navigation */}
            <aside className="space-y-6">
              <h3 className="text-lg font-semibold" style={{color: 'var(--oasis-foreground)'}}>Table of Contents</h3>
              <nav className="space-y-2">
                {sections.map((section) => {
                  const Icon = section.icon;
                  const isActive = activeSection === section.id;
                  return (
                    <button
                      key={section.id}
                      onClick={() => setActiveSection(section.id)}
                      className={cn(
                        "flex w-full items-center gap-3 rounded-xl border px-4 py-3 text-left transition",
                        isActive
                          ? "border-[var(--oasis-accent)]/70 bg-[rgba(34,211,238,0.12)]"
                          : "border-transparent bg-[rgba(8,11,26,0.6)] hover:border-[var(--oasis-accent)]/30"
                      )}
                    >
                      <Icon size={18} color={isActive ? 'var(--oasis-accent)' : 'var(--oasis-muted)'} />
                      <span className="font-semibold" style={{color: isActive ? 'var(--oasis-foreground)' : 'var(--oasis-muted)'}}>
                        {section.title}
                      </span>
                    </button>
                  );
                })}
              </nav>
            </aside>

            {/* Content Area */}
            <article className="min-h-[600px] rounded-2xl border p-8 shadow-inner prose prose-invert max-w-none" style={{
              borderColor: 'var(--oasis-card-border)',
              background: 'rgba(3,7,18,0.85)'
            }}>
              {activeSection === "overview" && <OverviewSection />}
              {activeSection === "create" && <CreateSection />}
              {activeSection === "migrate" && <MigrateSection />}
              {activeSection === "liquidity" && <LiquiditySection />}
              {activeSection === "use" && <UseSection />}
              {activeSection === "hyperdrive" && <HyperDriveSection />}
              {activeSection === "economics" && <EconomicsSection />}
              {activeSection === "security" && <SecuritySection />}
            </article>
          </div>
        </div>
      </section>
    </main>
  );
}

function OverviewSection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>What is Web4?</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          Web4 tokens are the next evolution of cryptocurrency. Unlike traditional tokens that exist on one blockchain, 
          Web4 tokens exist <strong style={{color: 'var(--oasis-foreground)'}}>natively on multiple blockchains simultaneously</strong>.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>The Problem with Current Tokens</h3>
        <ul className="space-y-2 ml-4">
          <li>Traditional tokens exist on ONE blockchain only</li>
          <li>To use them on another chain, you need bridges</li>
          <li>Bridges create wrapped versions (not the real token)</li>
          <li>Bridges get hacked: <strong style={{color: '#ef4444'}}>$2 billion+ lost annually</strong></li>
          <li>70% of users refuse to bridge due to fear</li>
        </ul>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>The Web4 Solution</h3>
        <div className="rounded-xl border p-6 my-4" style={{
          borderColor: 'var(--oasis-accent)',
          background: 'rgba(15,118,110,0.15)'
        }}>
          <p className="text-base" style={{color: 'var(--oasis-foreground)'}}>
            Web4 tokens exist natively on 10-42 blockchains at the same time. When you spend 1 token on Solana, 
            your balance updates on Ethereum, Polygon, Base, and all other chains <strong>instantly</strong>.
          </p>
          <p className="mt-3 text-sm" style={{color: 'var(--oasis-accent)'}}>
            No bridges. No wrapped tokens. No hacks. Just one token, everywhere.
          </p>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Key Benefits</h3>
        <div className="grid grid-cols-2 gap-4 mt-4">
          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ No Bridge Risk</h4>
            <p className="text-sm">Mathematically impossible to have bridge hacks because no bridges exist</p>
          </div>
          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ One Token</h4>
            <p className="text-sm">Same balance on all chains. No wrapped versions to manage</p>
          </div>
          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ Instant Sync</h4>
            <p className="text-sm">Updates across all chains in under 2 seconds</p>
          </div>
          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ 100% Uptime</h4>
            <p className="text-sm">Auto-failover across 50+ providers ensures availability</p>
          </div>
        </div>
      </div>
    </div>
  );
}

function CreateSection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>Create Web4 Tokens</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          Launch your token natively on 10-42 blockchains simultaneously using our <a href="/mint-token" className="font-bold underline" style={{color: 'var(--oasis-accent)'}}>Token Minting Platform</a>.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>The Process (5 Steps)</h3>
        
        <div className="space-y-4">
          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>Step 1: Configure & Deploy</h4>
            <p>Enter your token details (name, symbol, supply, image) and select which chains to deploy on. Your token configuration sits visually "on top" of all selected chains.</p>
          </div>

          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>Step 2: Token Economics</h4>
            <p>Define distribution percentages (Team, Public Sale, Treasury, Rewards). Must total 100%. Use sliders to adjust allocation.</p>
          </div>

          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>Step 3: Smart Contract Template</h4>
            <p>Choose from 6 templates: Basic, Governance, Staking, Gaming, Revenue Share, or Security Token.</p>
          </div>

          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>Step 4: Compliance & Rules</h4>
            <p>Configure optional access controls and regulatory requirements (coming soon).</p>
          </div>

          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>Step 5: Review & Deploy</h4>
            <p>Review configuration and total cost. Click "Deploy Token" to launch across all chains simultaneously (2-5 minutes).</p>
          </div>
        </div>

        <div className="rounded-xl border p-5 mt-6" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.15)'}}>
          <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>Cost: $300 average (10 chains)</h4>
          <p>Includes: HyperDrive activation ($100) + gas fees per chain ($5-$150 depending on network)</p>
          <p className="mt-2 text-sm">Compare to traditional multi-chain deployment: $800K+ (separate launches + bridges + security)</p>
        </div>
      </div>
    </div>
  );
}

function MigrateSection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>Migrate Existing Tokens to Web4</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          Already have a token on Ethereum, Polygon, or Solana? Upgrade it to Web4 using our <a href="/migrate-token" className="font-bold underline" style={{color: 'var(--oasis-accent)'}}>Token Migration Portal</a>.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>How Migration Works</h3>
        
        <div className="space-y-4">
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>1</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Lock Original Tokens</h4>
              <p>Your existing tokens are locked in OASIS migration contract. Held in escrow with cryptographic proof.</p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>2</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Deploy Web4 Version</h4>
              <p>Web4 version minted natively on all selected chains. 1:1 ratio maintained. All deployments happen in parallel.</p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>3</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Cross-Chain Synchronization</h4>
              <p>HyperDrive keeps all chains synchronized. Spend on any chain, balance updates everywhere instantly.</p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>4</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Reversible Anytime</h4>
              <p>Burn Web4 tokens to unlock originals. Always 1:1 backed. You keep full control.</p>
            </div>
          </div>
        </div>

        <div className="rounded-xl border p-5 mt-6" style={{borderColor: 'var(--oasis-warning)', background: 'rgba(250,204,21,0.1)'}}>
          <h4 className="font-bold mb-2" style={{color: 'var(--oasis-foreground)'}}>Use Cases</h4>
          <ul className="space-y-2 ml-4">
            <li><strong>Stablecoins (USDC, USDT):</strong> Eliminate bridge risks, save $2M+/year in security costs</li>
            <li><strong>DAO Tokens:</strong> Enable voting from any chain, increase participation 10x</li>
            <li><strong>DeFi Tokens:</strong> Instant liquidity on 10+ chains, no fragmentation</li>
          </ul>
        </div>
      </div>
    </div>
  );
}

function LiquiditySection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>Unified Liquidity Pools</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          HyperDrive Liquidity Pools let you provide liquidity ONCE and earn fees from ALL chains simultaneously. Visit the <a href="/liquidity" className="font-bold underline" style={{color: 'var(--oasis-accent)'}}>Liquidity Platform</a> to get started.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>The Liquidity Fragmentation Problem</h3>
        <div className="rounded-xl border p-5" style={{borderColor: '#ef4444', background: 'rgba(239,68,68,0.1)'}}>
          <p className="font-bold mb-2" style={{color: '#ef4444'}}>Traditional Multi-Chain Liquidity:</p>
          <ul className="space-y-2 text-sm ml-4">
            <li>Separate pools on each chain (Ethereum, Polygon, Solana...)</li>
            <li>Your liquidity ONLY earns from the chain you deployed on</li>
            <li>$1M liquidity on Ethereum = earn from Ethereum trades only</li>
            <li>To earn from other chains, you must deploy separate liquidity on each</li>
            <li>Total capital required: 10 chains × $1M = <strong>$10M</strong></li>
          </ul>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>The Unified Pool Solution</h3>
        <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-accent)', background: 'rgba(15,118,110,0.15)'}}>
          <p className="font-bold mb-2" style={{color: 'var(--oasis-accent)'}}>HyperDrive Unified Pool:</p>
          <ul className="space-y-2 text-sm ml-4">
            <li>ONE logical pool existing simultaneously on all chains</li>
            <li>Deploy $1M on Ethereum, earn fees from <strong>all 10 chains</strong></li>
            <li>Trades on Solana use your Ethereum liquidity</li>
            <li>Trades on Polygon use your Ethereum liquidity</li>
            <li>Total capital required: <strong>$1M</strong> (not $10M)</li>
          </ul>
          <p className="mt-3 font-bold" style={{color: 'var(--oasis-positive)'}}>
            10x fee income with SAME capital deployment
          </p>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>How It Works</h3>
        
        <div className="space-y-4">
          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>1</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Provide Liquidity</h4>
              <p>Choose token pair (e.g., DPT/USDC) and amounts. Select ONE chain to deploy on (where LP tokens are custodied).</p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>2</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>HyperDrive Synchronization</h4>
              <p>Your liquidity is registered on ALL chains simultaneously. Pool state syncs across all chains in real-time (&lt;2s).</p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>3</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Cross-Chain Fee Earning</h4>
              <p>When anyone swaps on ANY chain, you earn a proportional fee. Fees aggregate from all 10 chains into your position.</p>
            </div>
          </div>

          <div className="flex items-start gap-4">
            <div className="flex-shrink-0 w-8 h-8 rounded-full flex items-center justify-center font-bold" style={{background: 'var(--oasis-accent)', color: '#041321'}}>4</div>
            <div>
              <h4 className="font-bold" style={{color: 'var(--oasis-foreground)'}}>Withdraw Anytime</h4>
              <p>Remove liquidity from the chain you deployed on. Receive your original tokens + ALL fees earned from ALL chains.</p>
            </div>
          </div>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Real Example: DPT/USDC Pool</h3>
        <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
          <p className="mb-3">You provide: <strong>$10,000 liquidity</strong> (deployed on Ethereum)</p>
          
          <div className="space-y-2 text-sm">
            <p className="font-semibold" style={{color: 'var(--oasis-accent)'}}>Daily trading volume breakdown:</p>
            <ul className="ml-4 space-y-1">
              <li>Ethereum: $850K volume → <strong>$850 fees</strong></li>
              <li>Solana: $420K volume → <strong>$420 fees</strong></li>
              <li>Polygon: $320K volume → <strong>$320 fees</strong></li>
              <li>Base: $180K volume → <strong>$180 fees</strong></li>
              <li>6 other chains: $230K volume → <strong>$230 fees</strong></li>
            </ul>
            <p className="pt-2 border-t font-bold" style={{borderColor: 'var(--oasis-card-border)', color: 'var(--oasis-positive)'}}>
              Total daily fees: $2,000 (20% daily return!)
            </p>
            <p className="text-xs mt-2">
              * 0.3% fee on $2M total cross-chain volume
            </p>
          </div>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Benefits vs Traditional Pools</h3>

        <div className="overflow-x-auto">
          <table className="w-full mt-4 text-sm">
            <thead>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <th className="text-left p-3" style={{color: 'var(--oasis-foreground)'}}>Feature</th>
                <th className="text-left p-3" style={{color: 'var(--oasis-foreground)'}}>Uniswap / Traditional</th>
                <th className="text-left p-3" style={{color: 'var(--oasis-accent)'}}>HyperDrive Unified</th>
              </tr>
            </thead>
            <tbody>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Chains covered</td>
                <td className="p-3">1 chain per pool</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>10 chains per pool</td>
              </tr>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Capital required</td>
                <td className="p-3">10x (separate pools)</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>1x (unified pool)</td>
              </tr>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Fee income</td>
                <td className="p-3">1 chain only</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>All 10 chains</td>
              </tr>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Impermanent loss</td>
                <td className="p-3" style={{color: '#ef4444'}}>High (shallow pools)</td>
                <td className="p-3" style={{color: 'var(--oasis-positive)'}}>Lower (deep unified pool)</td>
              </tr>
              <tr>
                <td className="p-3 font-semibold">APY</td>
                <td className="p-3">15-25%</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>30-50%+ (10x volume)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <div className="rounded-xl border p-5 mt-6" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.15)'}}>
          <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>Key Advantage: Capital Efficiency</h4>
          <p className="text-sm">
            Traditional approach requires $10M to provide $1M liquidity on 10 chains. HyperDrive requires just $1M 
            to provide $1M effective liquidity on ALL 10 chains. That's <strong>10x capital efficiency</strong> while 
            earning <strong>10x fee income</strong>.
          </p>
        </div>
      </div>
    </div>
  );
}

function UseSection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>Using Web4 Tokens</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          Web4 tokens work differently than traditional tokens. Visit the <a href="/token-portal" className="font-bold underline" style={{color: 'var(--oasis-accent)'}}>Token Portal</a> to manage your Web4 tokens.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Key Differences</h3>
        
        <div className="overflow-x-auto">
          <table className="w-full mt-4 text-sm">
            <thead>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <th className="text-left p-3" style={{color: 'var(--oasis-foreground)'}}>Feature</th>
                <th className="text-left p-3" style={{color: 'var(--oasis-foreground)'}}>Traditional Token</th>
                <th className="text-left p-3" style={{color: 'var(--oasis-accent)'}}>Web4 Token</th>
              </tr>
            </thead>
            <tbody>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Exists on</td>
                <td className="p-3">1 blockchain</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>10-42 blockchains</td>
              </tr>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">To use elsewhere</td>
                <td className="p-3">Must bridge (risky)</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>Already there (native)</td>
              </tr>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Balance sync</td>
                <td className="p-3">Manual bridging</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>Automatic (&lt;2s)</td>
              </tr>
              <tr style={{borderBottom: '1px solid var(--oasis-card-border)'}}>
                <td className="p-3 font-semibold">Security risk</td>
                <td className="p-3" style={{color: '#ef4444'}}>Bridge hacks ($2B/year)</td>
                <td className="p-3" style={{color: 'var(--oasis-positive)'}}>No bridges (zero risk)</td>
              </tr>
              <tr>
                <td className="p-3 font-semibold">Uptime</td>
                <td className="p-3">Single chain (can go down)</td>
                <td className="p-3" style={{color: 'var(--oasis-accent)'}}>50+ provider failover (100%)</td>
              </tr>
            </tbody>
          </table>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Common Actions</h3>
        <ul className="space-y-2 ml-4">
          <li><strong>Send:</strong> Choose which chain to execute from, recipient gets tokens on ALL chains</li>
          <li><strong>Receive:</strong> Share your address from any chain, tokens appear everywhere</li>
          <li><strong>Stake:</strong> Stake on one chain, rewards distributed across all chains</li>
          <li><strong>Swap:</strong> Use Universal Asset Bridge to swap between different Web4 tokens</li>
        </ul>
      </div>
    </div>
  );
}

function HyperDriveSection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>How HyperDrive Works</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          HyperDrive is OASIS's revolutionary auto-failover data replication system that makes Web4 tokens possible.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Multi-Provider Architecture</h3>
        <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
          <p>OASIS uses <strong style={{color: 'var(--oasis-accent)'}}>50+ data providers</strong> simultaneously:</p>
          <ul className="mt-3 space-y-1 ml-4">
            <li>10 blockchains (Ethereum, Solana, Polygon, etc.)</li>
            <li>5 databases (MongoDB, PostgreSQL, Neo4j, etc.)</li>
            <li>3 distributed storage (IPFS, Filecoin, Arweave)</li>
            <li>Cloud providers (AWS, Azure, GCP)</li>
          </ul>
          <p className="mt-3 text-sm" style={{color: 'var(--oasis-accent)'}}>
            When you transfer a token, HyperDrive writes to ALL providers automatically.
          </p>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Automatic Failover</h3>
        <p>If any provider fails, HyperDrive automatically switches to the next:</p>
        <div className="rounded-xl border p-5 mt-3" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
          <p className="font-mono text-sm">
            Ethereum down? → Use Polygon<br/>
            Polygon down? → Use Solana<br/>
            Solana down? → Use Base<br/>
            ...continues through all 50+ providers
          </p>
          <p className="mt-3" style={{color: 'var(--oasis-positive)'}}>
            <strong>Result:</strong> Mathematically impossible to shut down (would need ALL 50+ providers to fail simultaneously)
          </p>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Consensus & Conflict Resolution</h3>
        <ul className="space-y-2 ml-4">
          <li>Every chain knows token state on every other chain</li>
          <li>If balances diverge (double-spend), majority vote determines truth</li>
          <li>Conflicts resolved in &lt;2 seconds automatically</li>
          <li>Byzantine fault tolerance across all providers</li>
        </ul>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Why This is Different from Bridges</h3>
        <div className="grid grid-cols-2 gap-4 mt-4">
          <div className="rounded-lg border p-4" style={{borderColor: '#ef4444', background: 'rgba(239,68,68,0.1)'}}>
            <h4 className="font-bold mb-2" style={{color: '#ef4444'}}>Traditional Bridges</h4>
            <ul className="space-y-1 text-sm">
              <li>Lock on Chain A</li>
              <li>Mint wrapped version on Chain B</li>
              <li>Two different tokens</li>
              <li>Bridge can get hacked</li>
              <li>Single point of failure</li>
            </ul>
          </div>
          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.15)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>HyperDrive Web4</h4>
            <ul className="space-y-1 text-sm">
              <li>Write to ALL chains simultaneously</li>
              <li>Same token, native everywhere</li>
              <li>One unified token</li>
              <li>No bridges to hack</li>
              <li>50+ provider redundancy</li>
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}

function EconomicsSection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>Financial Benefits</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          Web4 tokens provide massive cost savings and value creation for projects, foundations, and users.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>For Token Creators</h3>
        <div className="grid grid-cols-2 gap-4 mt-4">
          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold mb-3" style={{color: '#ef4444'}}>Traditional Launch (1 Chain)</h4>
            <div className="space-y-2 text-sm">
              <p>Cost: $50,000</p>
              <p>Potential users: 5M</p>
              <p>Market cap potential: $10M</p>
              <p className="pt-2 border-t" style={{borderColor: 'var(--oasis-card-border)'}}>
                To expand to 10 chains: <strong>+$800K</strong>
              </p>
            </div>
          </div>
          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-accent)', background: 'rgba(15,118,110,0.15)'}}>
            <h4 className="font-bold mb-3" style={{color: 'var(--oasis-accent)'}}>Web4 Launch (10 Chains)</h4>
            <div className="space-y-2 text-sm">
              <p>Cost: $50,300 (+$300 only!)</p>
              <p>Potential users: 50M (10x)</p>
              <p>Market cap potential: $100M (10x)</p>
              <p className="pt-2 border-t" style={{borderColor: 'var(--oasis-card-border)'}}>
                Savings vs traditional: <strong style={{color: 'var(--oasis-positive)'}}>$750K</strong>
              </p>
            </div>
          </div>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Real-World Examples</h3>

        <div className="space-y-4">
          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>USDC Migration</h4>
            <p><strong>Cost:</strong> $400 migration fee</p>
            <p><strong>Savings:</strong> $2M+/year (eliminate bridge security costs)</p>
            <p><strong>Value created:</strong> $100M+ in market cap (increased accessibility)</p>
            <p className="mt-2 text-sm font-bold" style={{color: 'var(--oasis-positive)'}}>ROI: 250,000x</p>
          </div>

          <div className="rounded-xl border p-5" style={{borderColor: 'var(--oasis-card-border)', background: 'rgba(6,11,26,0.6)'}}>
            <h4 className="font-bold text-lg mb-2" style={{color: 'var(--oasis-accent)'}}>Uniswap DAO Token</h4>
            <p><strong>Cost:</strong> $400 migration fee</p>
            <p><strong>Impact:</strong> Voter participation 5% → 50% (10x increase)</p>
            <p><strong>Value created:</strong> $800M-$2B (better governance + token premium)</p>
            <p className="mt-2 text-sm font-bold" style={{color: 'var(--oasis-positive)'}}>ROI: 2,000,000x</p>
          </div>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Network Effects</h3>
        <p>Each new Web4 token makes ALL existing Web4 tokens more valuable:</p>
        <ul className="space-y-2 ml-4 mt-3">
          <li>Token #1: Can swap with 0 others</li>
          <li>Token #100: Can swap with 99 others = 990 trading pairs</li>
          <li>Token #1,000: Can swap with 999 others = 999,000 trading pairs</li>
        </ul>
        <p className="mt-3" style={{color: 'var(--oasis-accent)'}}>
          <strong>Metcalfe's Law:</strong> Network value grows exponentially (n² where n = number of tokens)
        </p>
      </div>
    </div>
  );
}

function SecuritySection() {
  return (
    <div className="space-y-6" style={{color: 'var(--oasis-foreground)'}}>
      <h2 className="text-3xl font-bold" style={{color: 'var(--oasis-accent)'}}>Security & Safety</h2>
      
      <div className="space-y-4 text-base" style={{color: 'var(--oasis-muted)'}}>
        <p className="text-lg leading-relaxed">
          Web4 tokens are fundamentally more secure than bridged tokens because there are no bridges to hack.
        </p>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Bridge Hack Problem</h3>
        <div className="rounded-xl border p-5" style={{borderColor: '#ef4444', background: 'rgba(239,68,68,0.1)'}}>
          <p className="font-bold mb-3" style={{color: '#ef4444'}}>$2 Billion+ Lost to Bridge Hacks Annually</p>
          <ul className="space-y-2 text-sm ml-4">
            <li>Wormhole: $320M stolen (2022)</li>
            <li>Ronin Bridge: $625M stolen (2022)</li>
            <li>Nomad Bridge: $190M stolen (2022)</li>
            <li>Multichain: $126M stolen (2023)</li>
          </ul>
          <p className="mt-3 text-sm">
            Every bridge is a honeypot. Hackers target them because they hold millions in locked tokens.
          </p>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Web4 Security Model</h3>
        <div className="space-y-3">
          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.1)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ No Bridges = No Bridge Hacks</h4>
            <p className="text-sm">Tokens exist natively on each chain. Nothing to bridge, nothing to hack.</p>
          </div>

          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.1)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ 50+ Provider Redundancy</h4>
            <p className="text-sm">Even if 10 chains go down, token still exists on 40+ other providers.</p>
          </div>

          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.1)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ Byzantine Fault Tolerance</h4>
            <p className="text-sm">Consensus across all providers. Would need to compromise 26+ providers to attack (impossible).</p>
          </div>

          <div className="rounded-lg border p-4" style={{borderColor: 'var(--oasis-positive)', background: 'rgba(20,118,96,0.1)'}}>
            <h4 className="font-bold mb-2" style={{color: 'var(--oasis-positive)'}}>✓ Migration Reversibility</h4>
            <p className="text-sm">Migrated tokens can always be downgraded back to originals. 1:1 guarantee via smart contract.</p>
          </div>
        </div>

        <h3 className="text-xl font-bold mt-6" style={{color: 'var(--oasis-foreground)'}}>Audit Status</h3>
        <p>All Web4 token contracts and migration contracts are:</p>
        <ul className="space-y-2 ml-4 mt-3">
          <li>Open source (GitHub)</li>
          <li>Security audited (in progress)</li>
          <li>Bug bounty program active</li>
          <li>Tested on testnets across all chains</li>
        </ul>
      </div>
    </div>
  );
}


