"use client";

import { TokenConfig } from "@/app/mint-token/page-content";
import { Input } from "@/components/ui/input";

type TokenDetailsStepProps = {
  config: TokenConfig;
  updateConfig: (updates: Partial<TokenConfig>) => void;
};

export function TokenDetailsStep({ config, updateConfig }: TokenDetailsStepProps) {
  return (
    <div className="space-y-8">
      <div>
        <h3 className="text-xl font-semibold" style={{color: 'var(--oasis-foreground)'}}>Token Details</h3>
        <p className="mt-2 text-sm" style={{color: 'var(--oasis-muted)'}}>
          Enter the basic information for your Web4 token. This will be identical across all blockchains.
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="space-y-2">
          <label className="text-sm font-medium" style={{color: 'var(--oasis-foreground)'}}>
            Token Name *
          </label>
          <Input
            placeholder="e.g., Don't Panic Token"
            value={config.name}
            onChange={(e) => updateConfig({ name: e.target.value })}
            className="bg-muted"
          />
          <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
            Full name of your token
          </p>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium" style={{color: 'var(--oasis-foreground)'}}>
            Token Symbol *
          </label>
          <Input
            placeholder="e.g., DPT"
            value={config.symbol}
            onChange={(e) => updateConfig({ symbol: e.target.value.toUpperCase() })}
            className="bg-muted"
            maxLength={10}
          />
          <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
            3-5 character ticker symbol
          </p>
        </div>
      </div>

      <div className="space-y-2">
        <label className="text-sm font-medium" style={{color: 'var(--oasis-foreground)'}}>
          Description
        </label>
        <textarea
          placeholder="Describe your token's purpose and utility..."
          value={config.description}
          onChange={(e) => updateConfig({ description: e.target.value })}
          className="w-full rounded-lg border px-4 py-3 text-sm min-h-[100px]"
          style={{
            borderColor: 'var(--oasis-card-border)',
            background: 'rgba(8,11,26,0.6)',
            color: 'var(--oasis-foreground)'
          }}
        />
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div className="space-y-2">
          <label className="text-sm font-medium" style={{color: 'var(--oasis-foreground)'}}>
            Total Supply *
          </label>
          <Input
            type="number"
            placeholder="e.g., 1000000000"
            value={config.totalSupply}
            onChange={(e) => updateConfig({ totalSupply: e.target.value })}
            className="bg-muted"
          />
          <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
            Total number of tokens to create
          </p>
        </div>

        <div className="space-y-2">
          <label className="text-sm font-medium" style={{color: 'var(--oasis-foreground)'}}>
            Decimals
          </label>
          <Input
            type="number"
            value={config.decimals}
            onChange={(e) => updateConfig({ decimals: parseInt(e.target.value) || 18 })}
            className="bg-muted"
            min={0}
            max={18}
          />
          <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>
            Token precision (18 is standard)
          </p>
        </div>
      </div>
    </div>
  );
}

