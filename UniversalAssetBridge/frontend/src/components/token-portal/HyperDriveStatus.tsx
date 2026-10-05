"use client";

export default function HyperDriveStatus() {
  return (
    <div className="flex flex-wrap items-center gap-4 rounded-2xl border px-4 py-3 text-[11px]" style={{
      borderColor: 'var(--oasis-card-border)',
      background: 'rgba(8,12,26,0.7)',
      color: 'var(--oasis-muted)'
    }}>
      <span className="text-[9px] uppercase tracking-[0.4em]">HyperDrive Status</span>
      
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>10/10 Chains</span>
        <span className="text-[var(--oasis-positive)]">✓ Active</span>
      </div>
      
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Sync Latency</span>
        <span className="text-[var(--oasis-positive)]">&lt;2s</span>
      </div>
      
      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Auto-Failover</span>
        <span className="text-[var(--oasis-positive)]">Enabled</span>
      </div>

      <div className="flex items-center gap-3">
        <span className="text-xs font-semibold" style={{color: 'var(--oasis-accent)'}}>Network</span>
        <span>Testnet</span>
      </div>
    </div>
  );
}

