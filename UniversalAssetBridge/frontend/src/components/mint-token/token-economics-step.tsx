"use client";

import { TokenConfig } from "@/app/mint-token/page-content";

type TokenEconomicsStepProps = {
  config: TokenConfig;
  updateConfig: (updates: Partial<TokenConfig>) => void;
};

export function TokenEconomicsStep({ config, updateConfig }: TokenEconomicsStepProps) {
  const updateDistribution = (key: keyof TokenConfig['distribution'], value: number) => {
    updateConfig({
      distribution: { ...config.distribution, [key]: value }
    });
  };

  const total = Object.values(config.distribution).reduce((sum, val) => sum + val, 0);

  return (
    <div className="space-y-6">
      <div className="space-y-4">
        {Object.entries(config.distribution).map(([key, value]) => (
          <div key={key} className="space-y-2">
            <div className="flex items-center justify-between">
              <label className="text-sm font-medium capitalize" style={{color: 'var(--oasis-foreground)'}}>
                {key} Allocation
              </label>
              <span className="text-sm font-bold" style={{color: 'var(--oasis-accent)'}}>
                {value}%
              </span>
            </div>
            <input
              type="range"
              min="0"
              max="100"
              value={value}
              onChange={(e) => updateDistribution(key as keyof TokenConfig['distribution'], parseInt(e.target.value))}
              className="w-full"
            />
          </div>
        ))}
      </div>

      <div className={`rounded-lg border p-4`} style={{
        borderColor: total === 100 ? 'var(--oasis-positive)' : '#ef4444',
        background: total === 100 ? 'rgba(20,118,96,0.1)' : 'rgba(239,68,68,0.1)'
      }}>
        <div className="flex items-center justify-between">
          <span className="text-sm font-semibold" style={{color: 'var(--oasis-foreground)'}}>
            Total Distribution
          </span>
          <span className="text-2xl font-bold" style={{color: total === 100 ? 'var(--oasis-positive)' : '#ef4444'}}>
            {total}%
          </span>
        </div>
        {total !== 100 && (
          <p className="text-xs mt-2" style={{color: '#ef4444'}}>
            Must equal exactly 100%
          </p>
        )}
      </div>
    </div>
  );
}

