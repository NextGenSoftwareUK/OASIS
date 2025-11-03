"use client";

import { OracleLayout } from "@/components/layout/oracle-layout";
import { Card } from "@/components/ui/card";
import { StatCard } from "@/components/ui/stat-card";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatNumber } from "@/lib/utils";
import { Building2, AlertCircle, CheckCircle, TrendingUp, Shield } from "lucide-react";
import Link from "next/link";

type GovernmentBudget = {
  totalBudget: number;
  onBlockchain: number;
  transparent: number;
  wasteIdentified: number;
  savingsTarget: number;
};

export default function ReformGovernmentSpendPage() {
  // Mock data - simulates OASIS Oracle tracking government spending
  const budget: GovernmentBudget = {
    totalBudget: 702_000_000_000, // £702bn
    onBlockchain: 350_000_000_000, // £350bn (50% migrated)
    transparent: 50, // 50% transparency achieved
    wasteIdentified: 88_000_000_000, // £88bn waste found
    savingsTarget: 50_000_000_000, // £50bn target
  };

  const transparencyStatus =
    budget.transparent < 30 ? "danger" :
    budget.transparent < 70 ? "warning" :
    "success";

  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Back Button */}
        <Link
          href="/reform"
          className="inline-flex items-center gap-2 text-[var(--muted)] hover:text-[var(--accent)] transition-colors"
        >
          ← Back to Reform UK Overview
        </Link>

        {/* Hero Section */}
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
            Government Spending Transparency
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-3xl">
            Real-time blockchain tracking of all government spending. 
            Delivering on Reform UK's target: Cut waste, save £50bn annually.
          </p>
        </div>

        {/* Achievement Alert */}
        <div className="rounded-xl border border-[var(--positive)]/50 bg-[rgba(34,197,94,0.1)] p-4 flex items-start gap-3">
          <CheckCircle className="h-5 w-5 text-[var(--positive)] mt-0.5" />
          <div>
            <h3 className="font-semibold text-[var(--positive)] mb-1">Target Exceeded</h3>
            <p className="text-sm text-[var(--muted)]">
              OASIS Oracle has identified £{(budget.wasteIdentified / 1_000_000_000).toFixed(0)}bn in wasteful spending 
              - exceeding Reform UK's £50bn target by 76%. Blockchain audit trails make this verifiable and actionable.
            </p>
          </div>
        </div>

        {/* Real-Time Stats */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
          <StatCard
            label="Total Budget"
            value={`£${(budget.totalBudget / 1_000_000_000).toFixed(0)}bn`}
            description="Annual government spending"
            icon={<Building2 className="h-5 w-5" />}
            variant="default"
            trend="neutral"
            trendValue="2024-2025"
          />

          <StatCard
            label="On Blockchain"
            value={`£${(budget.onBlockchain / 1_000_000_000).toFixed(0)}bn`}
            description={`${Math.round((budget.onBlockchain / budget.totalBudget) * 100)}% migrated`}
            icon={<Shield className="h-5 w-5" />}
            variant="success"
            trend="up"
            trendValue="Increasing"
          />

          <StatCard
            label="Transparency"
            value={`${budget.transparent}%`}
            description="Real-time visibility achieved"
            icon={<CheckCircle className="h-5 w-5" />}
            variant={transparencyStatus}
            trend="up"
            trendValue="Improving"
          />

          <StatCard
            label="Waste Found"
            value={`£${(budget.wasteIdentified / 1_000_000_000).toFixed(0)}bn`}
            description="Identified via blockchain audit"
            icon={<AlertCircle className="h-5 w-5" />}
            variant="warning"
            trend="down"
            trendValue="Actionable"
          />

          <StatCard
            label="vs Target"
            value={`+${Math.round((budget.wasteIdentified / budget.savingsTarget - 1) * 100)}%`}
            description={`Target: £${(budget.savingsTarget / 1_000_000_000).toFixed(0)}bn`}
            icon={<TrendingUp className="h-5 w-5" />}
            variant="success"
            trend="up"
            trendValue="Exceeded"
          />
        </div>

        {/* Department Breakdown */}
        <Card
          title="Real-Time Department Spending"
          description="Live blockchain tracking of 11 major government departments"
          variant="glass"
        >
          <div className="space-y-3">
            <DepartmentRow
              name="NHS & Social Care"
              budget={180_000_000_000}
              onChain={100_000_000_000}
              waste={25_000_000_000}
              wasteScore={14}
              status="warning"
            />
            <DepartmentRow
              name="Education"
              budget={94_000_000_000}
              onChain={60_000_000_000}
              waste={8_000_000_000}
              wasteScore={9}
              status="success"
            />
            <DepartmentRow
              name="Defense"
              budget={52_000_000_000}
              onChain={30_000_000_000}
              waste={12_000_000_000}
              wasteScore={23}
              status="danger"
            />
            <DepartmentRow
              name="Transport (HS2)"
              budget={108_000_000_000}
              onChain={50_000_000_000}
              waste={42_000_000_000}
              wasteScore={39}
              status="danger"
            />
            <DepartmentRow
              name="Welfare & Pensions"
              budget={110_000_000_000}
              onChain={70_000_000_000}
              waste={5_000_000_000}
              wasteScore={5}
              status="success"
            />
            <DepartmentRow
              name="Business & Trade"
              budget={28_000_000_000}
              onChain={15_000_000_000}
              waste={3_000_000_000}
              wasteScore={11}
              status="warning"
            />
            <DepartmentRow
              name="Home Office"
              budget={24_000_000_000}
              onChain={10_000_000_000}
              waste={8_000_000_000}
              wasteScore={33}
              status="danger"
            />
            <DepartmentRow
              name="Justice"
              budget={22_000_000_000}
              onChain={12_000_000_000}
              waste={4_000_000_000}
              wasteScore={18}
              status="warning"
            />
            <DepartmentRow
              name="Foreign Office"
              budget={18_000_000_000}
              onChain={8_000_000_000}
              waste={5_000_000_000}
              wasteScore={28}
              status="danger"
            />
            <DepartmentRow
              name="Environment"
              budget={12_000_000_000}
              onChain={6_000_000_000}
              waste={1_500_000_000}
              wasteScore={13}
              status="warning"
            />
            <DepartmentRow
              name="Culture & Sport"
              budget={8_000_000_000}
              onChain={4_000_000_000}
              waste={800_000_000}
              wasteScore={10}
              status="success"
            />
          </div>
        </Card>

        {/* Waste Examples */}
        <Card
          title="Identified Wasteful Spending (Blockchain Verified)"
          description="Real examples caught by OASIS Oracle audit trails"
          variant="glass"
        >
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <WasteExample
              title="HS2 Cost Overruns"
              amount="£42bn"
              description="Original budget: £37.5bn. Current: £79.5bn. OASIS blockchain audit reveals cost escalation timeline and decision points."
              severity="critical"
            />
            <WasteExample
              title="PPE Procurement Scandal"
              amount="£4.7bn"
              description="COVID-19 PPE contracts awarded without proper tendering. Blockchain evidence shows contract dates vs delivery failures."
              severity="critical"
            />
            <WasteExample
              title="Test & Trace Program"
              amount="£37bn"
              description="Contact tracing system with limited effectiveness. OASIS tracks £37bn spend vs 50% app adoption rate."
              severity="critical"
            />
            <WasteExample
              title="NHS IT System Failures"
              amount="£10bn"
              description="Failed IT projects over 20 years. Blockchain shows repeated vendor failures and lack of accountability."
              severity="high"
            />
            <WasteExample
              title="Asylum Hotel Contracts"
              amount="£8bn"
              description="£15M/day on hotel accommodation. OASIS shows costs vs Reform UK's offshore processing alternative (£2bn)."
              severity="high"
            />
            <WasteExample
              title="Government Consultancy"
              amount="£3bn"
              description="Annual spend on external consultants. Blockchain shows overlap with internal civil service capabilities."
              severity="medium"
            />
          </div>
        </Card>

        {/* Before vs After */}
        <Card
          title="Real-World Impact: Before vs After Blockchain"
          description="How OASIS Oracle prevents waste through transparency"
          variant="glass"
        >
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <ImpactBox
              title="Traditional Government Spending (Before)"
              scenario="Major infrastructure project announced"
              steps={[
                "Budget set at £10bn with vague breakdown",
                "Contracts awarded to favored vendors",
                "Costs escalate to £18bn over 5 years",
                "Public learns about overruns after project complete",
                "No one accountable, taxpayers absorb £8bn waste"
              ]}
              outcome="£8bn wasted, zero accountability"
              color="negative"
            />

            <ImpactBox
              title="With OASIS Blockchain (After)"
              scenario="Major infrastructure project announced"
              steps={[
                "Budget £10bn on blockchain (itemized, public)",
                "Smart contracts enforce spending limits",
                "Real-time cost tracking (any citizen can audit)",
                "Alert triggered at £10.5bn (5% overrun)",
                "Project halted, re-tendered, completed at £10.2bn"
              ]}
              outcome="£7.8bn saved, full accountability"
              color="positive"
            />
          </div>
        </Card>

        {/* Savings Breakdown */}
        <Card
          title="OASIS Government Savings Analysis"
          description="How Reform UK delivers £52-88bn annual savings"
          variant="glass"
        >
          <div className="space-y-4">
            <GovernmentSavingsRow
              category="End Wasteful Contracts"
              current="£42bn lost (HS2, PPE, Test & Trace)"
              withOASIS="Smart contracts prevent overruns"
              savings="£40-50bn/year"
            />
            <GovernmentSavingsRow
              category="Real-Time Auditing"
              current="£10bn fraud + error (delayed detection)"
              withOASIS="Instant blockchain verification"
              savings="£8-12bn/year"
            />
            <GovernmentSavingsRow
              category="Reduce Consultancy"
              current="£3bn external consultants"
              withOASIS="Blockchain skills database (use internal talent)"
              savings="£2-3bn/year"
            />
            <GovernmentSavingsRow
              category="Procurement Efficiency"
              current="£5bn lost (non-competitive bidding)"
              withOASIS="Smart contract automated tendering"
              savings="£4-5bn/year"
            />
            <GovernmentSavingsRow
              category="Benefits Fraud Prevention"
              current="£8bn fraud (fake identities, double claims)"
              withOASIS="Blockchain identity verification"
              savings="£7-8bn/year"
            />
            <div className="pt-4 border-t border-[var(--color-card-border)]/30">
              <div className="flex items-center justify-between">
                <span className="text-lg font-semibold text-[var(--color-foreground)]">Total Annual Savings</span>
                <span className="text-2xl font-bold text-[var(--positive)]">£61-78bn/year</span>
              </div>
              <p className="text-sm text-[var(--muted)] mt-2">
                Significantly exceeds Reform UK's £50bn target. Enough to fund income tax threshold increase to £20k.
              </p>
            </div>
          </div>
        </Card>

        {/* Implementation Plan */}
        <Card
          title="100-Day Transparency Rollout"
          description="From pilot to full government blockchain"
          variant="glass"
        >
          <div className="space-y-4">
            <PhaseBox
              day="1-30"
              title="Phase 1: High-Waste Departments"
              items={[
                "Transport (HS2): Full blockchain audit",
                "Home Office (Asylum): Real-time spending tracker",
                "Demonstrate £5-10bn waste identification",
                "Media showcase: 'Reform UK Delivers Transparency'"
              ]}
            />
            <PhaseBox
              day="31-60"
              title="Phase 2: All Major Departments"
              items={[
                "Expand to all 11 departments (£702bn)",
                "Public blockchain explorer (any citizen can audit)",
                "Smart contracts for new procurement",
                "£20-30bn additional waste identified"
              ]}
            />
            <PhaseBox
              day="61-100"
              title="Phase 3: Full Government Blockchain"
              items={[
                "Local councils integrated",
                "Quangos and public bodies included",
                "Real-time tax revenue tracking",
                "£52-88bn annual savings pathway clear"
              ]}
            />
          </div>
        </Card>

        {/* CTA */}
        <div className="rounded-2xl bg-gradient-to-br from-blue-900/30 to-purple-900/30 border border-[var(--accent)]/30 p-8 text-center">
          <h3 className="text-2xl font-bold text-[var(--color-foreground)] mb-3">
            Transparency, Accountability, Savings
          </h3>
          <p className="text-lg text-[var(--muted)] mb-6 max-w-2xl mx-auto">
            OASIS Oracle makes every pound of taxpayer money traceable on the blockchain. 
            No more waste, no more scandals - just real-time accountability delivering Reform UK's pledge.
          </p>
          <div className="flex gap-4 justify-center">
            <Link
              href="/reform-nhs"
              className="px-6 py-3 bg-[var(--accent)] text-[#041321] rounded-lg font-semibold hover:bg-[#38e0ff] transition-colors"
            >
              View NHS Dashboard
            </Link>
            <Link
              href="/reform"
              className="px-6 py-3 bg-[rgba(12,16,34,0.85)] text-[var(--color-foreground)] border border-[var(--color-card-border)]/60 rounded-lg font-semibold hover:border-[var(--accent)]/50 transition-colors"
            >
              Back to Overview
            </Link>
          </div>
        </div>
      </div>
    </OracleLayout>
  );
}

function DepartmentRow({ name, budget, onChain, waste, wasteScore, status }: {
  name: string;
  budget: number;
  onChain: number;
  waste: number;
  wasteScore: number;
  status: "success" | "warning" | "danger";
}) {
  const transparency = (onChain / budget) * 100;

  return (
    <div className="flex items-center justify-between p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30 hover:bg-[rgba(34,211,238,0.05)] transition">
      <div className="flex items-center gap-3 flex-1">
        <div className="h-10 w-10 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
          <Building2 className="h-5 w-5 text-[var(--accent)]" />
        </div>
        <div>
          <h4 className="font-semibold text-[var(--color-foreground)]">{name}</h4>
          <p className="text-xs text-[var(--muted)]">£{(budget / 1_000_000_000).toFixed(1)}bn annual budget</p>
        </div>
      </div>

      <div className="flex items-center gap-6">
        <div className="text-right">
          <p className="text-sm font-semibold text-[var(--accent)]">£{(onChain / 1_000_000_000).toFixed(1)}bn</p>
          <p className="text-xs text-[var(--muted)]">On Blockchain</p>
        </div>

        <div className="text-right">
          <p className="text-sm font-semibold text-[var(--negative)]">£{(waste / 1_000_000_000).toFixed(1)}bn</p>
          <p className="text-xs text-[var(--muted)]">Waste Found</p>
        </div>

        <div className="w-32">
          <div className="h-2 w-full rounded-full bg-[rgba(5,5,16,0.8)] overflow-hidden">
            <div
              className={`h-full rounded-full transition-all duration-500 ${
                wasteScore < 15 ? "bg-[var(--positive)]" :
                wasteScore < 25 ? "bg-[var(--warning)]" :
                "bg-[var(--negative)]"
              }`}
              style={{ width: `${wasteScore}%` }}
            />
          </div>
          <p className="text-xs text-[var(--muted)] mt-1 text-center">
            {wasteScore}% waste
          </p>
        </div>

        <Badge variant={status} size="sm" dot>
          {status === "success" ? "Efficient" : status === "warning" ? "Review" : "Critical"}
        </Badge>
      </div>
    </div>
  );
}

function WasteExample({ title, amount, description, severity }: {
  title: string;
  amount: string;
  description: string;
  severity: "critical" | "high" | "medium";
}) {
  const color = severity === "critical" ? "negative" : severity === "high" ? "warning" : "accent";

  return (
    <div className={`p-4 rounded-xl bg-[rgba(5,5,16,0.5)] border border-[var(--${color})]/30`}>
      <div className="flex items-start justify-between mb-2">
        <h4 className="font-semibold text-[var(--color-foreground)]">{title}</h4>
        <span className={`text-lg font-bold text-[var(--${color})]`}>{amount}</span>
      </div>
      <p className="text-sm text-[var(--muted)] mb-2">{description}</p>
      <Badge variant={severity === "critical" ? "danger" : severity === "high" ? "warning" : "default"} size="sm">
        {severity.toUpperCase()} PRIORITY
      </Badge>
    </div>
  );
}

function ImpactBox({ title, scenario, steps, outcome, color }: {
  title: string;
  scenario: string;
  steps: string[];
  outcome: string;
  color: "positive" | "negative";
}) {
  return (
    <div className="p-6 rounded-xl bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <h4 className="font-semibold text-[var(--color-foreground)] mb-2">{title}</h4>
      <p className="text-sm text-[var(--muted)] mb-4 italic">{scenario}</p>
      <ol className="space-y-2 mb-4">
        {steps.map((step, i) => (
          <li key={i} className="text-sm text-[var(--muted)] flex items-start gap-2">
            <span className="text-[var(--accent)] font-semibold mt-0.5">{i + 1}.</span>
            <span>{step}</span>
          </li>
        ))}
      </ol>
      <div className="pt-3 border-t border-[var(--color-card-border)]/30">
        <p className="text-xs uppercase tracking-wide text-[var(--${color})] mb-1">Outcome</p>
        <p className={`font-semibold text-[var(--${color})]`}>{outcome}</p>
      </div>
    </div>
  );
}

function GovernmentSavingsRow({ category, current, withOASIS, savings }: {
  category: string;
  current: string;
  withOASIS: string;
  savings: string;
}) {
  return (
    <div className="p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <h4 className="font-semibold text-[var(--color-foreground)] mb-3">{category}</h4>
      <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--negative)] mb-1">Current (Before OASIS)</p>
          <p className="text-sm text-[var(--muted)]">{current}</p>
        </div>
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--accent)] mb-1">With OASIS</p>
          <p className="text-sm text-[var(--color-foreground)]">{withOASIS}</p>
        </div>
        <div>
          <p className="text-xs uppercase tracking-wide text-[var(--positive)] mb-1">Annual Savings</p>
          <p className="text-lg font-semibold text-[var(--positive)]">{savings}</p>
        </div>
      </div>
    </div>
  );
}

function PhaseBox({ day, title, items }: {
  day: string;
  title: string;
  items: string[];
}) {
  return (
    <div className="flex gap-4 p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <div className="flex items-center justify-center h-10 w-16 rounded-lg bg-[var(--accent-soft)] text-[var(--accent)] font-bold text-sm flex-shrink-0">
        Day<br/>{day}
      </div>
      <div className="flex-1">
        <h4 className="font-semibold text-[var(--color-foreground)] mb-2">{title}</h4>
        <ul className="space-y-1">
          {items.map((item, i) => (
            <li key={i} className="text-sm text-[var(--muted)] flex items-start gap-2">
              <span className="text-[var(--positive)] mt-1">✓</span>
              <span>{item}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

