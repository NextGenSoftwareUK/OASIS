"use client";

import { TokenConfig } from "@/app/mint-token/page-content";
import { Coins, Vote, Zap, Gamepad2, DollarSign, Shield } from "lucide-react";
import { cn } from "@/lib/utils";

type TemplateSelectionStepProps = {
  config: TokenConfig;
  updateConfig: (updates: Partial<TokenConfig>) => void;
};

const templates = [
  { id: "basic", name: "Basic Token", icon: Coins, description: "Simple transferrable token with balance tracking" },
  { id: "governance", name: "Governance Token", icon: Vote, description: "DAO voting and proposal management" },
  { id: "staking", name: "Staking Token", icon: Zap, description: "Built-in staking rewards and yield generation" },
  { id: "gaming", name: "Gaming Token", icon: Gamepad2, description: "XP, currency, and in-game items" },
  { id: "revenue", name: "Revenue Share", icon: DollarSign, description: "Automated profit distribution to holders" },
  { id: "security", name: "Security Token", icon: Shield, description: "Regulatory compliant with KYC/AML" },
];

export function TemplateSelectionStep({ config, updateConfig }: TemplateSelectionStepProps) {
  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Select Token Template</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Choose the smart contract template that best fits your use case.
        </p>
      </div>

      <div className="grid gap-5 grid-cols-3">
        {templates.map((template) => {
          const Icon = template.icon;
          const isSelected = config.template === template.id;
          return (
            <button
              key={template.id}
              type="button"
              onClick={() => updateConfig({ template: template.id })}
              className={cn(
                "glass-card relative overflow-hidden rounded-2xl border p-6 text-left transition hover:scale-105",
                isSelected
                  ? "border-[var(--oasis-accent)]/80 shadow-[0_25px_60px_rgba(34,211,238,0.35)] ring-2 ring-[var(--oasis-accent)]/50"
                  : "hover:border-[var(--oasis-accent)]/50"
              )}
              style={{
                borderColor: isSelected ? 'var(--oasis-accent)' : 'var(--oasis-card-border)',
                background: isSelected ? 'rgba(3,7,18,0.9)' : 'rgba(6,11,26,0.6)'
              }}
            >
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
              <div className="relative">
                <Icon size={32} color={isSelected ? 'var(--oasis-accent)' : 'var(--oasis-muted)'} />
                <h3 className="text-lg font-bold mt-4" style={{color: 'var(--oasis-foreground)'}}>{template.name}</h3>
                <p className="mt-3 text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
                  {template.description}
                </p>
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}

