"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import DashboardView from "@/components/qusdc/DashboardView";
import MintView from "@/components/qusdc/MintView";
import StakeView from "@/components/qusdc/StakeView";
import RedeemView from "@/components/qusdc/RedeemView";
import HowItWorks from "@/components/shared/HowItWorks";

export default function QUSDCContent() {
  const [activeTab, setActiveTab] = useState("dashboard");

  return (
    <main className="flex w-full flex-col gap-6 px-4 py-10 lg:px-10 xl:px-20">
      <section className="space-y-8">
        <div>
          <p className="text-sm uppercase tracking-[0.4em]" style={{color: 'var(--oasis-muted)'}}>Quantum Stablecoin</p>
          <div className="flex flex-col gap-4">
            <h2 className="mt-2 text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              qUSDC
            </h2>
            <p className="text-base max-w-3xl" style={{color: 'var(--oasis-muted)'}}>
              The yield-bearing stablecoin that works across all chains. Earn 12.5% APY from diversified real-world and DeFi strategies.
            </p>
          </div>
        </div>

        <HowItWorks sections={[
          {
            title: "What is qUSDC?",
            content: (
              <div className="space-y-3">
                <p>
                  qUSDC is a yield-bearing stablecoin backed by real-world assets, delta-neutral strategies, and liquid staking. 
                  Unlike traditional stablecoins that sit idle, qUSDC is collateralized by productive assets that generate 
                  sustainable returns.
                </p>
                <p>
                  When you mint qUSDC, your collateral is allocated across three diversified yield strategies: 40% to real-world 
                  assets (tokenized real estate and SMB revenue), 40% to delta-neutral perpetual trading, and 20% to a curated 
                  altcoin index. This diversification provides 12.5% average APY while maintaining dollar peg stability.
                </p>
              </div>
            )
          },
          {
            title: "How does sqUSDC staking work?",
            content: (
              <div className="space-y-3">
                <p>
                  sqUSDC is the staked version of qUSDC. When you stake qUSDC, you receive sqUSDC at the current exchange rate 
                  (initially 1:1). As yield is generated from the underlying strategies, the exchange rate increases. This means 
                  your sqUSDC becomes worth more qUSDC over time.
                </p>
                <p>
                  For example: If you stake 1,000 qUSDC today and receive 970.87 sqUSDC (at 1.03 rate), after 30 days at 12.5% APY, 
                  that sqUSDC might be worth 1,010 qUSDC. When you unstake, you receive the appreciated value including all accrued yield.
                </p>
                <p>
                  On Solana specifically, yield is also distributed directly to your wallet daily via the x402 protocol, 
                  providing immediate spendable income on top of the exchange rate appreciation.
                </p>
              </div>
            )
          },
          {
            title: "What is x402 and why is it better?",
            content: (
              <div className="space-y-3">
                <p>
                  x402 is an automatic payment distribution protocol designed for Solana. Instead of requiring users to manually 
                  claim their yield (which costs gas and requires active participation), x402 sends yield directly to holder 
                  wallets on a scheduled basis.
                </p>
                <p>
                  For qUSDC, this means Solana holders receive daily yield payments automatically at minimal cost ($0.001 per 
                  recipient). The system queries all sqUSDC holders, calculates proportional distributions, and executes 
                  multi-recipient transactions in 5-30 seconds.
                </p>
                <p>
                  This hybrid model - direct payments on Solana via x402, exchange rate updates on other chains - provides the 
                  best user experience while maintaining maximum capital efficiency. Distributing to 10,000 holders costs just 
                  $9 daily compared to $50,000+ on Ethereum.
                </p>
              </div>
            )
          },
          {
            title: "Where does the yield come from?",
            content: (
              <div className="space-y-3">
                <p>
                  qUSDC yield comes from three independent strategies that together create a diversified, sustainable income stream:
                </p>
                <p>
                  RWA Strategy (40% allocation, 4.2% APY): Deploys capital into tokenized real estate and SMB revenue streams 
                  through Quantum Street Smart Trusts. This provides stable, non-correlated returns backed by physical assets 
                  and real business cashflow.
                </p>
                <p>
                  Delta-Neutral Strategy (40% allocation, 6.8% APY): Holds crypto assets (ETH, BTC, SOL) while shorting 
                  equivalent perpetual futures positions. This hedges price exposure while capturing funding rate yields from 
                  perpetual markets. Rebalanced automatically to maintain zero directional risk.
                </p>
                <p>
                  Altcoin Strategy (20% allocation, 15% APY): Allocated to professionally managed altcoin index strategies that 
                  provide higher return potential. This portion accepts more volatility in exchange for enhanced yield, balanced 
                  by the stability of the other 80% allocation.
                </p>
              </div>
            )
          }
        ]} />

        <Tabs value={activeTab} onValueChange={setActiveTab} className="w-full">
          <TabsList className="grid w-full grid-cols-4 mb-8">
            <TabsTrigger value="dashboard">Dashboard</TabsTrigger>
            <TabsTrigger value="mint">Mint qUSDC</TabsTrigger>
            <TabsTrigger value="stake">Stake</TabsTrigger>
            <TabsTrigger value="redeem">Redeem</TabsTrigger>
          </TabsList>

          <TabsContent value="dashboard">
            <DashboardView />
          </TabsContent>

          <TabsContent value="mint">
            <MintView />
          </TabsContent>

          <TabsContent value="stake">
            <StakeView />
          </TabsContent>

          <TabsContent value="redeem">
            <RedeemView />
          </TabsContent>
        </Tabs>
      </section>
    </main>
  );
}

