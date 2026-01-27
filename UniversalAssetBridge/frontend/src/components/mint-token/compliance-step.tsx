"use client";

import { TokenConfig } from "@/app/mint-token/page-content";

type ComplianceStepProps = {
  config: TokenConfig;
  updateConfig: (updates: Partial<TokenConfig>) => void;
};

export function ComplianceStep({ config, updateConfig }: ComplianceStepProps) {
  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Compliance & Rules</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Configure optional compliance and access control settings. These apply across all chains.
        </p>
      </div>
      
      <div className="rounded-2xl border p-6" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(6,11,26,0.8)'
      }}>
        <p className="text-base font-medium" style={{color: 'var(--oasis-foreground)'}}>
          All tokens deployed without restrictions by default. Compliance features coming soon.
        </p>
      </div>
    </div>
  );
}

