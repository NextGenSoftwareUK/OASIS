"use client";

import { useState } from "react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import DashboardView from "@/components/qusdc/DashboardView";
import MintView from "@/components/qusdc/MintView";
import StakeView from "@/components/qusdc/StakeView";
import RedeemView from "@/components/qusdc/RedeemView";

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

