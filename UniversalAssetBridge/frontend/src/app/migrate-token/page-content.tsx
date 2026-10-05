"use client";

import { useState, useMemo } from "react";
import { WizardShell } from "@/components/migrate-token/wizard-shell";
import { ConnectDetectStep } from "@/components/migrate-token/connect-detect-step";
import { ChainSelectionStep } from "@/components/migrate-token/chain-selection-step";
import { MigrationSettingsStep } from "@/components/migrate-token/migration-settings-step";
import { ReviewMigrationStep } from "@/components/migrate-token/review-migration-step";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import HowItWorks from "@/components/shared/HowItWorks";

export interface MigrationConfig {
  // Detected token info
  existingTokenName: string;
  existingTokenSymbol: string;
  existingTokenContract: string;
  existingChain: string;
  existingBalance: string;
  tokenImage: string;
  
  // Migration settings
  migrationAmount: string;
  selectedChains: string[];
  
  // Connection
  walletConnected: boolean;
  walletAddress: string;
}

const WIZARD_STEPS = [
  {
    id: "connect",
    title: "Connect & Detect",
    description: "Connect wallet and identify your existing token.",
  },
  {
    id: "chains",
    title: "Select Chains",
    description: "Choose which chains to deploy your Web4 token on.",
  },
  {
    id: "settings",
    title: "Migration Settings",
    description: "Configure migration amount and parameters.",
  },
  {
    id: "review",
    title: "Review & Migrate",
    description: "Confirm and execute the migration to Web4.",
  },
];

export default function PageContent() {
  const [activeStep, setActiveStep] = useState<string>(WIZARD_STEPS[0]?.id ?? "connect");
  const [config, setConfig] = useState<MigrationConfig>({
    existingTokenName: "",
    existingTokenSymbol: "",
    existingTokenContract: "",
    existingChain: "Ethereum",
    existingBalance: "",
    tokenImage: "",
    migrationAmount: "",
    selectedChains: [],
    walletConnected: false,
    walletAddress: "",
  });

  const updateConfig = (updates: Partial<MigrationConfig>) => {
    setConfig({ ...config, ...updates });
  };

  const currentIndex = WIZARD_STEPS.findIndex((s) => s.id === activeStep);

  const canProceed = useMemo(() => {
    switch (activeStep) {
      case "connect":
        return Boolean(config.walletConnected && config.existingTokenContract);
      case "chains":
        return config.selectedChains.length > 0;
      case "settings":
        return Boolean(config.migrationAmount && parseFloat(config.migrationAmount) > 0);
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
    return config.selectedChains.reduce((sum, chain) => sum + (gasCosts[chain] || 0), 0) + 200;
  }, [config.selectedChains]);

  const renderSessionSummary = (
    <div className="flex flex-wrap items-center gap-4 rounded-2xl border px-4 py-3 text-[11px]" style={{
      borderColor: 'var(--oasis-card-border)',
      background: 'rgba(8,12,26,0.7)',
      color: 'var(--oasis-muted)'
    }}>
      <span className="text-[9px] uppercase tracking-[0.4em]">Migration Summary</span>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Token</span>
        <span>{config.existingTokenSymbol || "—"}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>From Chain</span>
        <span>{config.existingChain}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>To Chains</span>
        <span>{config.selectedChains.length}</span>
      </div>
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Amount</span>
        <span>{config.migrationAmount || "0"}</span>
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
        <p className="text-sm uppercase tracking-[0.4em]" style={{color: 'var(--oasis-muted)'}}>Web4 Token Migration</p>
        <div className="flex flex-col gap-4">
          <div className="flex flex-wrap items-center gap-4">
            <h2 className="mt-2 text-4xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              Migrate {config.existingTokenSymbol || 'Existing Token'} to Web4
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
              {canProceed && activeStep === "review" ? "Ready To Migrate" : "Configuring"}
            </span>
          </div>
          {renderSessionSummary}
        </div>
        <p className="mt-3 max-w-3xl text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
          Upgrade your existing token to Web4. Lock tokens on your original chain and deploy Web4 version across multiple blockchains simultaneously.
        </p>
      </div>

      <HowItWorks sections={[
        {
          title: "How does token migration work?",
          content: (
            <div className="space-y-3">
              <p>
                Token migration upgrades your existing single-chain token to a Web4 token that exists on multiple chains. 
                Your original tokens are locked in an OASIS migration contract (held in escrow with cryptographic proof), 
                and an equivalent amount of Web4 tokens is minted across all selected chains.
              </p>
              <p>
                The migration maintains a 1:1 ratio: if you lock 10,000 tokens, exactly 10,000 Web4 tokens are created. 
                All deployments happen in parallel, taking 2-5 minutes total regardless of how many chains you select.
              </p>
            </div>
          )
        },
        {
          title: "Is migration reversible?",
          content: (
            <div className="space-y-3">
              <p>
                Yes. Migration is fully reversible through the downgrade process. You can burn your Web4 tokens at any time 
                to unlock your original tokens from the escrow contract. The 1:1 guarantee is enforced by smart contracts 
                on all chains.
              </p>
              <p>
                This means you can try Web4 without risk. If you decide multi-chain isn't needed, simply downgrade and 
                retrieve your original tokens exactly as they were locked.
              </p>
            </div>
          )
        },
        {
          title: "What happens to liquidity and holders?",
          content: (
            <div className="space-y-3">
              <p>
                Existing token holders can migrate their tokens individually - there's no forced migration. This allows for 
                a gradual transition where both versions coexist temporarily. Liquidity providers can migrate their LP positions 
                or create new unified pools using HyperDrive.
              </p>
              <p>
                Once migrated, your token gains instant access to HyperDrive Liquidity Pools, the Universal Asset Bridge, 
                and the entire Web4 ecosystem. Users can trade on any of the 10+ chains without bridges, and liquidity 
                providers earn fees from all chains simultaneously instead of just one.
              </p>
              <p>
                For DAOs, migration enables governance participation from any chain. Token holders no longer need to bridge 
                to Ethereum to vote - they can participate directly from Solana, Polygon, or any supported chain.
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
                {currentIndex === WIZARD_STEPS.length - 1 ? 'Migrate to Web4' : 'Next'}
              </Button>
            </div>
          </div>
        }
      >
        {activeStep === "connect" ? (
          <ConnectDetectStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "chains" ? (
          <ChainSelectionStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "settings" ? (
          <MigrationSettingsStep config={config} updateConfig={updateConfig} />
        ) : null}
        {activeStep === "review" ? (
          <ReviewMigrationStep config={config} />
        ) : null}
      </WizardShell>
    </section>
  );
}

