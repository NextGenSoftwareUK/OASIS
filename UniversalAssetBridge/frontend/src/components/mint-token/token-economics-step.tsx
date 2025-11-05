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
    <div className="space-y-8">
      <div>
        <h3 className="text-2xl font-bold" style={{color: 'var(--oasis-foreground)'}}>Token Economics</h3>
        <p className="mt-3 text-base" style={{color: 'var(--oasis-muted)'}}>
          Define how your tokens will be distributed. Total must equal 100%.
        </p>
      </div>

      <div className="space-y-6">
        {Object.entries(config.distribution).map(([key, value]) => (
          <div key={key} className="space-y-3">
            <div className="flex items-center justify-between">
              <label className="text-base font-semibold capitalize" style={{color: 'var(--oasis-foreground)'}}>
                {key} Allocation
              </label>
              <span className="text-xl font-bold" style={{color: 'var(--oasis-accent)'}}>
                {value}%
              </span>
            </div>
            <input
              type="range"
              min="0"
              max="100"
              value={value}
              onChange={(e) => updateDistribution(key as keyof TokenConfig['distribution'], parseInt(e.target.value))}
              className="w-full h-3"
              style={{
                accentColor: 'var(--oasis-accent)'
              }}
            />
          </div>
        ))}
      </div>

      <div className={`rounded-2xl border p-6`} style={{
        borderColor: total === 100 ? 'var(--oasis-positive)' : '#ef4444',
        background: total === 100 ? 'rgba(20,118,96,0.2)' : 'rgba(239,68,68,0.15)'
      }}>
        <div className="flex items-center justify-between">
          <span className="text-lg font-bold" style={{color: 'var(--oasis-foreground)'}}>
            Total Distribution
          </span>
          <span className="text-3xl font-bold" style={{color: total === 100 ? 'var(--oasis-positive)' : '#ef4444'}}>
            {total}%
          </span>
        </div>
        {total !== 100 && (
          <p className="text-sm mt-3 font-semibold" style={{color: '#ef4444'}}>
            ⚠️ Must equal exactly 100%
          </p>
        )}
      </div>
    </div>
  );
}

