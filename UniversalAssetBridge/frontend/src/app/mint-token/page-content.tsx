"use client";

import { useState, useMemo } from "react";
import { WizardShell } from "@/components/mint-token/wizard-shell";
import { ChainSelectionStep } from "@/components/mint-token/chain-selection-step";
import { TokenEconomicsStep } from "@/components/mint-token/token-economics-step";
import { TemplateSelectionStep } from "@/components/mint-token/template-selection-step";
import { ComplianceStep } from "@/components/mint-token/compliance-step";
import { ReviewStep } from "@/components/mint-token/review-step";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import HowItWorks from "@/components/shared/HowItWorks";

export interface TokenConfig {
  name: string;
  symbol: string;
  description: string;
  totalSupply: string;
  decimals: number;
  imageUrl: string;
  selectedChains: string[];
  distribution: {
    team: number;
    public: number;
    treasury: number;
    rewards: number;
  };
  template: string;
}

const WIZARD_STEPS = [
  {
    id: "chain-selection",
    title: "Configure & Deploy",
    description: "Set token details and select deployment chains.",
  },
  {
    id: "economics",
    title: "Token Economics",
    description: "Define distribution percentages and allocation strategy.",
  },
  {
    id: "template",
    title: "Smart Contract Template",
    description: "Select the contract type that matches your use case.",
  },
  {
    id: "compliance",
    title: "Compliance & Rules",
    description: "Configure access controls and regulatory requirements.",
  },
  {
    id: "review",
    title: "Review & Deploy",
    description: "Confirm configuration and deploy across all selected chains.",
  },
];

export default function PageContent() {
  const [activeStep, setActiveStep] = useState<string>(WIZARD_STEPS[0]?.id ?? "chain-selection");
  const [config, setConfig] = useState<TokenConfig>({
    name: "",
    symbol: "",
    description: "",
    totalSupply: "",
    decimals: 18,
    imageUrl: "",
    selectedChains: [],
    distribution: { team: 20, public: 40, treasury: 30, rewards: 10 },
    template: "basic",
  });

  const updateConfig = (updates: Partial<TokenConfig>) => {
    setConfig({ ...config, ...updates });
  };

  const currentIndex = WIZARD_STEPS.findIndex((s) => s.id === activeStep);

  const canProceed = useMemo(() => {
    switch (activeStep) {
      case "chain-selection":
        return Boolean(config.name && config.symbol && config.totalSupply && config.selectedChains.length > 0);
      case "economics":
        const total = Object.values(config.distribution).reduce((sum, val) => sum + val, 0);
        return total === 100;
      default:
        return true;
    }
  }, [activeStep, config]);

  const totalCost = useMemo(() => {
    const gasCosts: Record<string, number> = {
      "Solana": 5, "Ethereum": 150, "Polygon": 2, "Base": 10,
      "Arbitrum": 8, "Optimism": 12, "BNB Chain": 3, "Avalanche": 7,
      "Fantom": 4, "Radix": 1,
    };
    return config.selectedChains.reduce((sum, chain) => sum + (gasCosts[chain] || 0), 0) + 100;
  }, [config.selectedChains]);

  const renderSessionSummary = (
    <div className="flex flex-wrap items-center gap-4 rounded-2xl border px-4 py-3 text-[11px]" style={{
      borderColor: 'var(--oasis-card-border)',
      background: 'rgba(8,12,26,0.7)',
      color: 'var(--oasis-muted)'
    }}>
      <span className="text-[9px] uppercase tracking-[0.4em]">Configuration Summary</span>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Symbol</span>
        <span>{config.symbol || "—"}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Chains</span>
        <span>{config.selectedChains.length}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Template</span>
        <span className="capitalize">{config.template}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Est. Cost</span>
        <span>${totalCost}</span>
      </div>
    </div>
  );

  return (
    <section className="space-y-6 px-4 py-10 lg:px-10 xl:px-20">
      <div>
        <p className="text-sm uppercase tracking-[0.4em]" style={{color: 'var(--oasis-muted)'}}>Web4 Token Factory</p>
        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap items-center gap-4">
            <h2 className="mt-2 text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              Create {config.name ? `${config.name}` : 'Web4'} Token
            </h2>
            <span
              className={cn(
                "mt-2 h-fit rounded-full border px-3 py-1 text-xs uppercase tracking-[0.4em]",
                canProceed && activeStep === "review"
                  ? "border-[var(--oasis-positive)]/60 bg-[rgba(20,118,96,0.25)]"
                  : "border-[var(--oasis-accent)]/60 bg-[rgba(20,118,96,0.25)]"
              )}
              style={{color: canProceed && activeStep === "review" ? 'var(--oasis-positive)' : 'var(--oasis-accent)'}}
            >
              {canProceed && activeStep === "review" ? "Ready To Deploy" : "Configuring"}
            </span>
          </div>
          {renderSessionSummary}
        </div>
        <p className="mt-3 max-w-3xl text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
          Deploy your token natively to multiple blockchains simultaneously using OASIS HyperDrive. No bridges, no wrapped versions.
        </p>
      </div>

      <HowItWorks sections={[
        {
          title: "What are Web4 Tokens?",
          content: (
            <div className="space-y-3">
              <p>
                Web4 tokens exist natively on multiple blockchains simultaneously. Unlike traditional tokens that live on 
                one chain (requiring bridges to move elsewhere), Web4 tokens are deployed to all selected chains at once 
                using HyperDrive.
              </p>
              <p>
                When you spend 1 token on Solana, your balance updates on Ethereum, Polygon, Base, and all other chains 
                instantly. This eliminates the need for bridges entirely, removing the $2 billion annual bridge hack risk.
              </p>
            </div>
          )
        },
        {
          title: "How does multi-chain deployment work?",
          content: (
            <div className="space-y-3">
              <p>
                The Smart Contract Generator creates platform-specific contracts from your configuration. Your token 
                details (name, symbol, supply) remain identical across all chains, but the underlying smart contract 
                code is optimized for each blockchain's architecture.
              </p>
              <p>
                Deployment happens in parallel: Ethereum receives a Solidity contract, Solana gets a Rust program, 
                and Radix receives Scrypto code - all in 2-5 minutes. HyperDrive then synchronizes the token state 
                across all deployments, ensuring your balance is always consistent.
              </p>
            </div>
          )
        },
        {
          title: "Why is this better than traditional multi-chain launches?",
          content: (
            <div className="space-y-3">
              <p>
                Traditional approach: Launch on Ethereum ($50K), then separately launch on Polygon ($50K), Solana ($50K), etc. 
                Each requires audits, liquidity setup, and bridge contracts. Total cost for 10 chains: $800K+. Time: 6+ months.
              </p>
              <p>
                Web4 approach: Configure once, deploy to all 10 chains simultaneously for $300. Time: 5 minutes. No bridges 
                means no bridge security audits needed. Liquidity is unified through HyperDrive Pools, so one deployment 
                serves all chains.
              </p>
              <p>
                The savings come from eliminating redundant work: one audit covers all chains, one liquidity pool serves all 
                chains, and HyperDrive handles synchronization automatically.
              </p>
            </div>
          )
        }
      ]} />
      
      <WizardShell
        steps={WIZARD_STEPS}
        activeStep={activeStep}
        onStepChange={setActiveStep}
        footer={
          <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
            <div className="flex flex-wrap gap-2 text-[11px]" style={{color: 'var(--oasis-muted)'}}>
              <span>Step {currentIndex + 1} of {WIZARD_STEPS.length}</span>
            </div>
            <div className="flex gap-3">
              <Button
                variant="secondary"
                disabled={currentIndex === 0}
                onClick={() => {
                  if (currentIndex > 0) {
                    setActiveStep(WIZARD_STEPS[currentIndex - 1].id);
                  }
                }}
              >
                Previous
              </Button>
              <Button
                variant="primary"
                onClick={() => {
                  if (currentIndex < WIZARD_STEPS.length - 1) {
                    setActiveStep(WIZARD_STEPS[currentIndex + 1].id);
                  }
                }}
                disabled={!canProceed}
              >
                {currentIndex === WIZARD_STEPS.length - 1 ? 'Deploy Token' : 'Next'}
              </Button>
            </div>
          </div>
        }
      >
        {activeStep === "chain-selection" ? (
          <ChainSelectionStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "economics" ? (
          <TokenEconomicsStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "template" ? (
          <TemplateSelectionStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "compliance" ? (
          <ComplianceStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "review" ? (
          <ReviewStep config={config} />
        ) : null}
      </WizardShell>
    </section>
  );
}

