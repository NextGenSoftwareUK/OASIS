"use client";

import { TokenConfig } from "@/app/mint-token/page-content";

type ComplianceStepProps = {
  config: TokenConfig;
  updateConfig: (updates: Partial<TokenConfig>) => void;
};

export function ComplianceStep({ config, updateConfig }: ComplianceStepProps) {
  return (
    <div className="space-y-4">
      <p className="text-sm" style={{color: 'var(--oasis-muted)'}}>
        Configure optional compliance and access control settings. These apply across all chains.
      </p>
      
      <div className="rounded-lg border p-5" style={{
        borderColor: 'var(--oasis-card-border)',
        background: 'rgba(15,118,110,0.05)'
      }}>
        <p className="text-sm" style={{color: 'var(--oasis-foreground)'}}>
          All tokens deployed without restrictions by default. Compliance features coming soon.
        </p>
      </div>
    </div>
  );
}

