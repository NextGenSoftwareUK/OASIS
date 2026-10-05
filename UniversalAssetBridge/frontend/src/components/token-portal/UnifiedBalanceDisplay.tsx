"use client";

interface UnifiedBalanceDisplayProps {
  balance: number;
  usdValue: number;
}

export default function UnifiedBalanceDisplay({ balance, usdValue }: UnifiedBalanceDisplayProps) {
  return (
    <div className="flex flex-col items-center gap-4 py-8">
      <h3 className="text-sm uppercase tracking-[0.3em]" style={{color: 'var(--oasis-muted)'}}>
        Your DPT Balance
      </h3>
      
      <div className="flex flex-col items-center gap-2">
        <div className="text-6xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
          {balance.toLocaleString('en-US')}
          <span className="ml-3 text-4xl" style={{color: 'var(--oasis-accent)'}}>DPT</span>
        </div>
        
        <div className="text-2xl" style={{color: 'var(--oasis-muted)'}}>
          ${usdValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} USD
        </div>
      </div>

      <div className="mt-4 flex items-center gap-2 rounded-full border px-4 py-2" style={{
        borderColor: 'var(--oasis-positive)',
        background: 'rgba(20,118,96,0.15)'
      }}>
        <div className="h-2 w-2 rounded-full animate-pulse" style={{backgroundColor: 'var(--oasis-positive)'}} />
        <span className="text-xs" style={{color: 'var(--oasis-positive)'}}>
          All chains synchronized
        </span>
      </div>
    </div>
  );
}

