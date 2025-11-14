"use client";

import { MigrationConfig } from "@/app/migrate-token/page-content";
import { Input } from "@/components/ui/input";

type MigrationSettingsStepProps = {
  config: MigrationConfig;
  updateConfig: (updates: Partial<MigrationConfig>) => void;
};

export function MigrationSettingsStep({ config, updateConfig }: MigrationSettingsStepProps) {
  const balance = parseFloat(config.existingBalance) || 0;
  const migrationAmount = parseFloat(config.migrationAmount) || 0;
  const remaining = balance - migrationAmount;

  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Migration Settings</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Configure how many tokens to migrate to Web4. You can migrate partial amounts.
        </p>
      </div>

      <div className="space-y-6">
        {/* Migration Amount */}
        <div className="space-y-3">
          <label className="text-base font-semibold" style={{color: 'var(--oasis-foreground)'}}>
            Migration Amount *
          </label>
          <Input
            type="number"
            placeholder="Enter amount to migrate"
            value={config.migrationAmount}
            onChange={(e) => updateConfig({ migrationAmount: e.target.value })}
            className="h-14 text-xl font-bold text-center"
            style={{
              background: 'rgba(3,7,18,0.8)',
              borderColor: 'var(--oasis-card-border)',
              color: 'var(--oasis-accent)'
            }}
            max={balance}
          />
          <div className="flex justify-between text-sm">
            <span style={{color: 'var(--oasis-muted)'}}>Available: {balance.toLocaleString()} {config.existingTokenSymbol}</span>
            <button
              onClick={() => updateConfig({ migrationAmount: config.existingBalance })}
              className="font-semibold hover:underline"
              style={{color: 'var(--oasis-accent)'}}
            >
              Max
            </button>
          </div>
        </div>

        {/* Migration Breakdown */}
        <div className="grid grid-cols-2 gap-4">
          <div className="rounded-2xl border p-5" style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(6,11,26,0.8)'
          }}>
            <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>
              Will Lock on {config.existingChain}
            </p>
            <p className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
              {migrationAmount.toLocaleString()}
            </p>
            <p className="text-sm mt-1" style={{color: 'var(--oasis-muted)'}}>
              {config.existingTokenSymbol}
            </p>
          </div>

          <div className="rounded-2xl border p-5" style={{
            borderColor: 'var(--oasis-accent)',
            background: 'rgba(15,118,110,0.2)'
          }}>
            <p className="text-xs uppercase tracking-wider mb-2" style={{color: 'var(--oasis-muted)'}}>
              Will Create as Web4
            </p>
            <p className="text-2xl font-bold" style={{color: 'var(--oasis-accent)'}}>
              {migrationAmount.toLocaleString()}
            </p>
            <p className="text-sm mt-1" style={{color: 'var(--oasis-muted)'}}>
              Web4-{config.existingTokenSymbol}
            </p>
          </div>
        </div>

        {/* Remaining Balance */}
        {remaining > 0 && (
          <div className="rounded-xl border p-4" style={{
            borderColor: 'var(--oasis-warning)',
            background: 'rgba(250,204,21,0.1)'
          }}>
            <p className="text-sm" style={{color: 'var(--oasis-foreground)'}}>
              <strong>Remaining on {config.existingChain}:</strong> {remaining.toLocaleString()} {config.existingTokenSymbol}
            </p>
            <p className="text-xs mt-1" style={{color: 'var(--oasis-muted)'}}>
              You'll still have {remaining.toLocaleString()} {config.existingTokenSymbol} on {config.existingChain} (unmigrated)
            </p>
          </div>
        )}

        {/* Migration Info */}
        <div className="rounded-2xl border p-5" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(6,11,26,0.8)'
        }}>
          <h4 className="font-bold mb-3" style={{color: 'var(--oasis-foreground)'}}>How Migration Works</h4>
          <div className="space-y-2 text-sm" style={{color: 'var(--oasis-muted)'}}>
            <p>1. Your {config.existingTokenSymbol} tokens are locked in OASIS migration contract</p>
            <p>2. Web4-{config.existingTokenSymbol} is minted across all selected chains</p>
            <p>3. 1:1 ratio maintained (1 {config.existingTokenSymbol} = 1 Web4-{config.existingTokenSymbol})</p>
            <p>4. You can downgrade anytime (burn Web4 → unlock original)</p>
          </div>
        </div>
      </div>
    </div>
  );
}

