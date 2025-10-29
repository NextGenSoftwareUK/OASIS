"use client";

import { OracleLayout } from "@/components/layout/oracle-layout";
import { OwnershipTracker } from "@/components/collateral/ownership-tracker";
import { MaturityCalendar } from "@/components/collateral/maturity-calendar";
import { PortfolioBreakdown } from "@/components/collateral/portfolio-breakdown";
import { MarginCallAlert } from "@/components/collateral/margin-call-alert";

export default function CollateralPage() {
  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Hero Section */}
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
            Collateral Management
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-3xl">
            Real-time ownership tracking and collateral mobility across 20+ blockchains. 
            Know exactly who owns what, when - solving the $100-150 billion problem.
          </p>
        </div>

        {/* Margin Call Alert (if any) */}
        <MarginCallAlert />

        {/* Real-Time Position */}
        <OwnershipTracker />

        {/* Maturity Calendar */}
        <MaturityCalendar />

        {/* Portfolio Breakdown */}
        <PortfolioBreakdown />

        {/* Info Cards */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6 mt-12">
          <InfoCard
            title="Real-Time Ownership"
            description="Complete visibility across all chains and legacy systems. Answer 'who owns what, when' in <1 second with multi-oracle consensus."
          />
          <InfoCard
            title="Encumbrance Tracking"
            description="Monitor all pledges, liens, and locks across all chains. Know exactly what collateral is available vs locked at any moment."
          />
          <InfoCard
            title="Maturity Predictions"
            description="See when pledged collateral will become available. Plan ahead and optimize collateral usage with hour-by-hour scheduling."
          />
        </div>

        {/* Use Case Examples */}
        <Card
          title="Real-World Impact"
          description="How this prevents banking crises"
          variant="glass"
        >
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <UseCaseBox
              title="March 2023 SVB Collapse"
              before="Days to locate collateral → Bank failed"
              after="Minutes to locate collateral → Crisis averted"
              savings="$212B in losses prevented"
            />
            <UseCaseBox
              title="Regulatory Audit"
              before="2-4 weeks to compile position → $500k cost"
              after="2 seconds for complete snapshot → $0 cost"
              savings="$500k saved per audit"
            />
          </div>
        </Card>
      </div>
    </OracleLayout>
  );
}

function InfoCard({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,11,26,0.85)] p-6 backdrop-blur-xl">
      <h3 className="text-lg font-semibold text-[var(--color-foreground)] mb-2">
        {title}
      </h3>
      <p className="text-sm text-[var(--muted)] leading-relaxed">
        {description}
      </p>
    </div>
  );
}

function UseCaseBox({
  title,
  before,
  after,
  savings,
}: {
  title: string;
  before: string;
  after: string;
  savings: string;
}) {
  return (
    <div className="p-6 rounded-xl bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <h4 className="font-semibold text-[var(--color-foreground)] mb-4">{title}</h4>
      <div className="space-y-3">
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--negative)] mb-1">Before OASIS</p>
          <p className="text-sm text-[var(--muted)]">{before}</p>
        </div>
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--positive)] mb-1">With Oracle</p>
          <p className="text-sm text-[var(--color-foreground)]">{after}</p>
        </div>
        <div className="pt-3 border-t border-[var(--color-card-border)]/30">
          <p className="text-xs uppercase tracking-wide text-[var(--accent)] mb-1">Impact</p>
          <p className="text-lg font-semibold text-[var(--accent)]">{savings}</p>
        </div>
      </div>
    </div>
  );
}

