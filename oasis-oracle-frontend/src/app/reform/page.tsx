"use client";

import { OracleLayout } from "@/components/layout/oracle-layout";
import { Card } from "@/components/ui/card";
import Link from "next/link";

export default function ReformUKOnePager() {
  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Hero Section */}
        <div className="flex items-center gap-6 mb-8">
          <div className="h-20 w-20 rounded-xl bg-[var(--accent-soft)] flex items-center justify-center overflow-hidden border border-[var(--accent)]/30">
            <span className="text-3xl font-bold text-[var(--accent)]">🇬🇧</span>
          </div>
          <div>
            <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
              OASIS × Reform UK
            </h1>
            <p className="text-xl text-[var(--accent)] mt-1">
              Blockchain for British Sovereignty
            </p>
            <p className="text-sm text-[var(--muted)] mt-2">
              Digital Infrastructure to Deliver Reform UK's Contract with You
            </p>
          </div>
        </div>

        {/* Key Stats */}
        <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
          <StatBox
            label="Reform UK Target"
            value="£150bn"
            subtitle="Annual savings promised"
            color="blue"
          />
          <StatBox
            label="OASIS Delivers"
            value="£120-170bn"
            subtitle="Verified annually"
            color="green"
          />
          <StatBox
            label="Implementation Time"
            value="100 Days"
            subtitle="To first pilots"
            color="cyan"
          />
          <StatBox
            label="ROI"
            value="25-1,700x"
            subtitle="Pilot to full deployment"
            color="yellow"
          />
        </div>

        {/* Core Dashboards */}
        <div className="space-y-3">
          <h2 className="text-2xl font-semibold text-[var(--color-foreground)]">
            Live Dashboards
          </h2>
          <p className="text-[var(--muted)]">
            Interactive demonstrations of OASIS Oracle delivering on Reform UK policy pledges
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <DashboardCard
            href="/reform-nhs"
            icon="🏥"
            title="NHS Hospital Management"
            description="Real-time bed tracking, patient flow optimization, and resource management across NHS trusts"
            stats={["11 Hospitals", "11.7k Waiting", "£25-42bn Savings"]}
            pledge="Pledge #3: Zero NHS Waiting Lists"
          />

          <DashboardCard
            href="/reform-government-spend-tracker"
            icon="🏛️"
            title="Government Spending Transparency"
            description="Real-time spending tracking, waste identification, and blockchain audit trails across departments"
            stats={["11 Departments", "£702bn Budget", "£52-88bn Savings"]}
            pledge="Target: £50bn Waste Reduction"
          />
        </div>

        {/* Policy Alignment */}
        <Card
          title="How OASIS Delivers Reform UK's 5 Core Pledges"
          description="Technical solutions mapped to policy priorities"
          variant="glass"
        >
          <div className="space-y-6">
            <PolicyRow
              number="1"
              icon="🛂"
              title="Smart Immigration"
              goal="Freeze non-essential immigration, stop the boats"
              solution="Sovereign digital identity with biometric blockchain verification"
              savings="£5-11bn/year"
            />
            <PolicyRow
              number="2"
              icon="🚢"
              title="Stop the Boats"
              goal="Zero illegal immigrant resettlement"
              solution="Cross-jurisdiction border intelligence with immutable records"
              savings="£3-5bn/year"
            />
            <PolicyRow
              number="3"
              icon="🏥"
              title="Zero NHS Waiting Lists"
              goal="Free at point of delivery, cut back-office waste"
              solution="Patient-owned records + AI optimization + real-time resource tracking"
              savings="£25-42bn/year"
            />
            <PolicyRow
              number="4"
              icon="🏛️"
              title="Government Transparency"
              goal="Cut waste, reduce £50bn public spending"
              solution="Blockchain ledger for all government spending with real-time audits"
              savings="£52-88bn/year"
            />
            <PolicyRow
              number="5"
              icon="🔐"
              title="CBDC Opposition"
              goal="Protect financial privacy, oppose surveillance money"
              solution="Self-sovereign digital wallets with multi-chain redundancy"
              savings="Priceless - preserves freedom"
            />
          </div>
        </Card>

        {/* Implementation Roadmap */}
        <Card
          title="3-Phase Implementation Roadmap"
          description="From pilot to nationwide deployment"
          variant="glass"
        >
          <div className="space-y-4">
            <PhaseBox
              phase="1"
              title="First 100 Days - Pilots"
              timeline="Months 1-3"
              items={[
                "Immigration Digital ID at 3 border points",
                "NHS patient records at 3 hospital trusts",
                "Government spending blockchain for 2 departments",
                "Prove 90% cost reduction + zero fraud"
              ]}
            />
            <PhaseBox
              phase="2"
              title="Production Rollout"
              timeline="Months 4-12"
              items={[
                "Scale NHS to 50+ trusts (5M patients)",
                "Government transparency to all 11 departments",
                "Immigration system nationwide",
                "£10-50bn annual savings demonstrated"
              ]}
            />
            <PhaseBox
              phase="3"
              title="Full UK Deployment"
              timeline="Months 13-24"
              items={[
                "All NHS trusts on OASIS (67M patients)",
                "Complete government spending transparency",
                "Universal digital ID (optional)",
                "£120-170bn annual savings delivered"
              ]}
            />
          </div>
        </Card>

        {/* Technology Stack */}
        <Card
          title="OASIS Oracle Technology"
          description="The infrastructure powering Reform UK's digital transformation"
          variant="glass"
        >
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <TechFeature
              title="Real-Time Ownership Tracking"
              description="Know exactly who owns what, when - across all systems and blockchains. Prevents SVB-style crises."
            />
            <TechFeature
              title="Multi-Chain Interoperability"
              description="15+ blockchain integrations + legacy systems (SWIFT, NHS, HMRC). Write once, deploy everywhere."
            />
            <TechFeature
              title="Compliance-First Design"
              description="KYC/AML embedded, GDPR compliant, multi-jurisdiction support. Regulators love it."
            />
            <TechFeature
              title="Intelligent Auto-Failover"
              description="If one blockchain goes down, instantly switch to another. 100% uptime guarantee."
            />
          </div>
        </Card>

        {/* Call to Action */}
        <div className="rounded-2xl bg-gradient-to-br from-blue-900/30 to-cyan-900/30 border border-[var(--accent)]/30 p-8 text-center">
          <h3 className="text-2xl font-bold text-[var(--color-foreground)] mb-3">
            Only Reform UK will secure Britain's future as a free, proud and independent sovereign nation.
          </h3>
          <p className="text-lg text-[var(--accent)] mb-6">
            And only OASIS can provide the technology to make it happen.
          </p>
          <div className="flex gap-4 justify-center">
            <Link
              href="/reform-nhs"
              className="px-6 py-3 bg-[var(--accent)] text-[#041321] rounded-lg font-semibold hover:bg-[#38e0ff] transition-colors"
            >
              View NHS Dashboard
            </Link>
            <Link
              href="/reform-government-spend-tracker"
              className="px-6 py-3 bg-[rgba(12,16,34,0.85)] text-[var(--color-foreground)] border border-[var(--color-card-border)]/60 rounded-lg font-semibold hover:border-[var(--accent)]/50 transition-colors"
            >
              View Government Spending
            </Link>
          </div>
        </div>

        {/* Footer */}
        <div className="text-center text-xs uppercase tracking-[0.4em] text-[var(--muted)] pt-8">
          Powered by OASIS Web4 Infrastructure | November 2025
        </div>
      </div>
    </OracleLayout>
  );
}

function StatBox({ label, value, subtitle, color }: { label: string; value: string; subtitle: string; color: string }) {
  const colorClasses = {
    blue: 'text-blue-400',
    green: 'text-green-400',
    cyan: 'text-cyan-400',
    yellow: 'text-yellow-400',
  };

  return (
    <div className="rounded-xl border border-[var(--color-card-border)]/50 bg-[rgba(8,11,26,0.85)] p-6 backdrop-blur-xl">
      <p className="text-xs uppercase tracking-wide text-[var(--muted)] mb-2">{label}</p>
      <p className={`text-3xl font-bold mb-1 ${colorClasses[color as keyof typeof colorClasses]}`}>{value}</p>
      <p className="text-xs text-[var(--muted)]">{subtitle}</p>
    </div>
  );
}

function DashboardCard({ href, icon, title, description, stats, pledge }: {
  href: string;
  icon: string;
  title: string;
  description: string;
  stats: string[];
  pledge: string;
}) {
  return (
    <Link
      href={href}
      className="group rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,11,26,0.85)] p-6 backdrop-blur-xl hover:border-[var(--accent)]/50 hover:bg-[rgba(34,211,238,0.05)] transition-all"
    >
      <div className="text-4xl mb-4">{icon}</div>
      <h3 className="text-xl font-bold text-[var(--color-foreground)] mb-2 group-hover:text-[var(--accent)] transition-colors">
        {title}
      </h3>
      <p className="text-sm text-[var(--accent)] mb-3">{pledge}</p>
      <p className="text-sm text-[var(--muted)] leading-relaxed mb-4">{description}</p>
      <div className="flex gap-2 flex-wrap">
        {stats.map((stat, i) => (
          <span key={i} className="text-xs px-2 py-1 bg-[rgba(5,5,16,0.5)] text-slate-300 rounded-full border border-[var(--color-card-border)]/30">
            {stat}
          </span>
        ))}
      </div>
    </Link>
  );
}

function PolicyRow({ number, icon, title, goal, solution, savings }: {
  number: string;
  icon: string;
  title: string;
  goal: string;
  solution: string;
  savings: string;
}) {
  return (
    <div className="flex gap-4 p-4 rounded-xl bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <div className="flex items-center justify-center h-12 w-12 rounded-lg bg-[var(--accent-soft)] text-[var(--accent)] font-bold text-xl flex-shrink-0">
        {icon}
      </div>
      <div className="flex-1">
        <h4 className="font-semibold text-[var(--color-foreground)] mb-1">{number}. {title}</h4>
        <p className="text-xs text-[var(--muted)] mb-2">{goal}</p>
        <p className="text-sm text-[var(--color-foreground)] mb-2">{solution}</p>
        <p className="text-xs text-[var(--positive)] font-semibold">Savings: {savings}</p>
      </div>
    </div>
  );
}

function PhaseBox({ phase, title, timeline, items }: {
  phase: string;
  title: string;
  timeline: string;
  items: string[];
}) {
  return (
    <div className="flex gap-4 p-6 rounded-xl bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <div className="flex items-center justify-center h-10 w-10 rounded-full bg-[var(--accent)] text-[#041321] font-bold text-lg flex-shrink-0">
        {phase}
      </div>
      <div className="flex-1">
        <div className="flex items-baseline justify-between mb-3">
          <h4 className="font-semibold text-[var(--color-foreground)]">{title}</h4>
          <span className="text-xs text-[var(--accent)]">{timeline}</span>
        </div>
        <ul className="space-y-2">
          {items.map((item, i) => (
            <li key={i} className="text-sm text-[var(--muted)] flex items-start gap-2">
              <span className="text-[var(--accent)] mt-1">✓</span>
              <span>{item}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function TechFeature({ title, description }: { title: string; description: string }) {
  return (
    <div className="p-4 rounded-xl bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <h4 className="font-semibold text-[var(--color-foreground)] mb-2">{title}</h4>
      <p className="text-sm text-[var(--muted)]">{description}</p>
    </div>
  );
}

