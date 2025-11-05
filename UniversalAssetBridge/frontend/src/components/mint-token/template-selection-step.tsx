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
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
        {templates.map((template) => {
          const Icon = template.icon;
          const isSelected = config.template === template.id;
          return (
            <button
              key={template.id}
              type="button"
              onClick={() => updateConfig({ template: template.id })}
              className={cn(
                "glass-card relative overflow-hidden rounded-2xl border p-5 text-left transition",
                isSelected
                  ? "border-[var(--oasis-accent)]/80 shadow-[0_25px_60px_rgba(34,211,238,0.35)] ring-2 ring-[var(--oasis-accent)]/50"
                  : "hover:border-[var(--oasis-accent)]/50"
              )}
              style={{borderColor: isSelected ? 'var(--oasis-accent)' : 'var(--oasis-card-border)'}}
            >
              <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
              <div className="relative">
                <Icon size={24} color={isSelected ? 'var(--oasis-accent)' : 'var(--oasis-muted)'} />
                <h3 className="text-lg font-semibold mt-3" style={{color: 'var(--oasis-foreground)'}}>{template.name}</h3>
                <p className="mt-2 text-sm" style={{color: 'var(--oasis-muted)'}}>
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

