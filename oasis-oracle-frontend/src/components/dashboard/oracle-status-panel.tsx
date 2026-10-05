"use client";

import { StatCard } from "@/components/ui/stat-card";
import { Database, Server, CheckCircle2, Activity } from "lucide-react";

export function OracleStatusPanel() {
  // Mock data - will be replaced with real API calls
  const mockData = {
    dataSources: { active: 8, total: 12 },
    chains: { healthy: 20, total: 20 },
    verifications: 1234,
    consensus: 98.5,
  };

  return (
    <div className="space-y-6">
      <h2 className="text-2xl font-semibold text-[var(--color-foreground)]">
        Oracle Status
      </h2>
      
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          label="Data Sources"
          value={`${mockData.dataSources.active}/${mockData.dataSources.total}`}
          description="Price feed sources online"
          icon={<Database className="h-5 w-5" />}
          variant="success"
          trend="up"
          trendValue="All active"
        />
        
        <StatCard
          label="Chain Health"
          value={`${mockData.chains.healthy}/${mockData.chains.total}`}
          description="Blockchain observers"
          icon={<Server className="h-5 w-5" />}
          variant="success"
          trend="neutral"
          trendValue="100% healthy"
        />
        
        <StatCard
          label="Verifications"
          value={mockData.verifications.toLocaleString()}
          description="Transactions verified today"
          icon={<CheckCircle2 className="h-5 w-5" />}
          variant="default"
          trend="up"
          trendValue="+12% vs yesterday"
        />
        
        <StatCard
          label="Consensus"
          value={`${mockData.consensus}%`}
          description="Oracle agreement level"
          icon={<Activity className="h-5 w-5" />}
          variant="success"
          trend="up"
          trendValue="Optimal"
        />
      </div>

      {/* Consensus Progress Bar */}
      <div className="rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,11,26,0.85)] p-6 backdrop-blur-xl">
        <div className="flex items-center justify-between mb-3">
          <span className="text-sm font-medium text-[var(--color-foreground)]">
            Consensus Status
          </span>
          <span className="text-sm text-[var(--accent)] font-semibold">
            {mockData.consensus}% Agreement
          </span>
        </div>
        <div className="h-2 w-full rounded-full bg-[rgba(5,5,16,0.8)] overflow-hidden">
          <div 
            className="h-full bg-gradient-to-r from-[var(--accent)] to-[var(--accent-strong)] rounded-full transition-all duration-500"
            style={{ width: `${mockData.consensus}%` }}
          />
        </div>
        <p className="mt-2 text-xs text-[var(--muted)]">
          High consensus indicates reliable oracle data across all sources
        </p>
      </div>
    </div>
  );
}






