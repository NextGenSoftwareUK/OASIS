"use client";

import { OracleLayout } from "@/components/layout/oracle-layout";
import { Card } from "@/components/ui/card";
import { StatCard } from "@/components/ui/stat-card";
import { Badge } from "@/components/ui/badge";
import { formatCurrency, formatNumber } from "@/lib/utils";
import { Activity, Users, Bed, Clock, AlertTriangle } from "lucide-react";
import Link from "next/link";

type HospitalSystem = {
  totalBeds: number;
  availableBeds: number;
  occupiedBeds: number;
  utilizationRate: number;
  waitingList: number;
  avgWaitTime: number; // hours
};

export default function ReformNHSPage() {
  // Mock data - simulates OASIS Oracle real-time tracking
  const system: HospitalSystem = {
    totalBeds: 12_450,
    availableBeds: 1_890,
    occupiedBeds: 10_560,
    utilizationRate: 85,
    waitingList: 11_720,
    avgWaitTime: 168, // 7 days
  };

  const capacityStatus =
    system.utilizationRate < 75 ? "success" :
    system.utilizationRate < 90 ? "warning" :
    "danger";

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
            NHS Hospital Management
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-3xl">
            Real-time bed tracking, patient flow optimization, and resource management. 
            Delivering on Reform UK's Pledge #3: Zero NHS Waiting Lists.
          </p>
        </div>

        {/* Capacity Alert */}
        {system.utilizationRate > 90 && (
          <div className="rounded-xl border border-[var(--negative)]/50 bg-[rgba(239,68,68,0.1)] p-4 flex items-start gap-3">
            <AlertTriangle className="h-5 w-5 text-[var(--negative)] mt-0.5" />
            <div>
              <h3 className="font-semibold text-[var(--negative)] mb-1">High Capacity Alert</h3>
              <p className="text-sm text-[var(--muted)]">
                System-wide capacity at {system.utilizationRate}%. OASIS recommends immediate patient redistribution and staff reallocation.
              </p>
            </div>
          </div>
        )}

        {/* Real-Time Stats */}
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-5">
          <StatCard
            label="Total Beds"
            value={formatNumber(system.totalBeds)}
            description="Across 11 London hospitals"
            icon={<Bed className="h-5 w-5" />}
            variant="default"
            trend="neutral"
            trendValue="Real-time"
          />

          <StatCard
            label="Available"
            value={formatNumber(system.availableBeds)}
            description={`${Math.round((system.availableBeds / system.totalBeds) * 100)}% capacity free`}
            icon={<Bed className="h-5 w-5" />}
            variant="success"
            trend="up"
            trendValue="Ready now"
          />

          <StatCard
            label="Occupied"
            value={formatNumber(system.occupiedBeds)}
            description={`${system.utilizationRate}% utilization`}
            icon={<Users className="h-5 w-5" />}
            variant={capacityStatus}
            trend="neutral"
            trendValue={`${system.utilizationRate}% used`}
          />

          <StatCard
            label="Waiting List"
            value={formatNumber(system.waitingList)}
            description="Patients waiting for treatment"
            icon={<Clock className="h-5 w-5" />}
            variant="warning"
            trend="down"
            trendValue="Needs action"
          />

          <StatCard
            label="Avg Wait Time"
            value={`${Math.floor(system.avgWaitTime / 24)}d`}
            description={`${system.avgWaitTime} hours`}
            icon={<Activity className="h-5 w-5" />}
            variant="danger"
            trend="down"
            trendValue="Too long"
          />
        </div>

        {/* Hospital Breakdown */}
        <Card
          title="Real-Time Hospital Capacity"
          description="Live bed availability across 11 London NHS trusts"
          variant="glass"
        >
          <div className="space-y-3">
            <HospitalRow
              name="St Thomas' Hospital"
              total={1_850}
              available={280}
              occupied={1_570}
              waiting={1_450}
              status="success"
            />
            <HospitalRow
              name="Royal London Hospital"
              total={1_200}
              available={150}
              occupied={1_050}
              waiting={980}
              status="warning"
            />
            <HospitalRow
              name="University College Hospital"
              total={1_100}
              available={120}
              occupied={980}
              waiting={890}
              status="warning"
            />
            <HospitalRow
              name="King's College Hospital"
              total={950}
              available={90}
              occupied={860}
              waiting={1_020}
              status="danger"
            />
            <HospitalRow
              name="Guy's Hospital"
              total={800}
              available={100}
              occupied={700}
              waiting={750}
              status="success"
            />
            <HospitalRow
              name="Chelsea & Westminster"
              total={750}
              available={80}
              occupied={670}
              waiting={820}
              status="warning"
            />
            <HospitalRow
              name="St Mary's Hospital"
              total={720}
              available={70}
              occupied={650}
              waiting={710}
              status="warning"
            />
            <HospitalRow
              name="Royal Free Hospital"
              total={680}
              available={60}
              occupied={620}
              waiting={890}
              status="danger"
            />
            <HospitalRow
              name="Hammersmith Hospital"
              total={620}
              available={50}
              occupied={570}
              waiting={670}
              status="warning"
            />
            <HospitalRow
              name="Whittington Hospital"
              total={580}
              available={45}
              occupied={535}
              waiting={620}
              status="warning"
            />
            <HospitalRow
              name="Northwick Park Hospital"
              total={550}
              available={40}
              occupied={510}
              waiting={580}
              status="warning"
            />
          </div>
        </Card>

        {/* OASIS Impact */}
        <Card
          title="Real-World Impact: Before vs After OASIS"
          description="How blockchain transparency solves the NHS waiting list crisis"
          variant="glass"
        >
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
            <ScenarioBox
              title="March 2024 Crisis (Before OASIS)"
              scenario="Patient needs urgent surgery at King's College Hospital"
              steps={[
                "Hospital at 95% capacity → No beds available",
                "Manual calls to find beds at other trusts (2-4 hours)",
                "Found bed at Guy's Hospital but transfer delayed",
                "Patient waits 7 days → Condition worsens",
                "Emergency surgery costs £25k (vs £8k if done immediately)"
              ]}
              outcome="£17k additional cost, patient suffers"
              color="negative"
            />

            <ScenarioBox
              title="With OASIS Oracle (Real-Time)"
              scenario="Patient needs urgent surgery at King's College Hospital"
              steps={[
                "Hospital at 95% capacity → OASIS alerts instantly",
                "OASIS finds available bed at Guy's (< 1 second)",
                "Patient transfer automated via smart contract",
                "Surgery performed same day",
                "Total cost: £8k (normal procedure cost)"
              ]}
              outcome="£17k saved, patient treated immediately"
              color="positive"
            />
          </div>
        </Card>

        {/* Savings Breakdown */}
        <Card
          title="OASIS NHS Savings Analysis"
          description="How Reform UK delivers £25-42bn annual savings"
          variant="glass"
        >
          <div className="space-y-4">
            <SavingsRow
              category="Real-Time Resource Optimization"
              current="£8-12bn waste (manual allocation, duplicate tests)"
              withOASIS="£500m cost (automated allocation)"
              savings="£7.5-11.5bn/year"
            />
            <SavingsRow
              category="Patient-Owned Records"
              current="£4-8bn waste (fragmented records, duplicate procedures)"
              withOASIS="£200m cost (blockchain records)"
              savings="£3.8-7.8bn/year"
            />
            <SavingsRow
              category="Prescription Management"
              current="£2-4bn fraud + waste (paper prescriptions)"
              withOASIS="£100m cost (blockchain verification)"
              savings="£1.9-3.9bn/year"
            />
            <SavingsRow
              category="Administrative Overhead"
              current="£40bn back-office costs (40% of budget)"
              withOASIS="£10bn costs (10% with automation)"
              savings="£30bn/year"
            />
            <div className="pt-4 border-t border-[var(--color-card-border)]/30">
              <div className="flex items-center justify-between">
                <span className="text-lg font-semibold text-[var(--color-foreground)]">Total Annual Savings</span>
                <span className="text-2xl font-bold text-[var(--positive)]">£43.2-53.2bn/year</span>
              </div>
              <p className="text-sm text-[var(--muted)] mt-2">
                Exceeds Reform UK's NHS savings target. Enough to eliminate waiting lists AND increase staff pay.
              </p>
            </div>
          </div>
        </Card>

        {/* Implementation */}
        <Card
          title="100-Day Implementation Plan"
          description="From pilot to nationwide rollout"
          variant="glass"
        >
          <div className="space-y-4">
            <MilestoneBox
              week="1-4"
              title="Pilot: 3 NHS Trusts"
              items={[
                "Deploy OASIS patient records (500k patients)",
                "Real-time bed tracking integration",
                "Staff training and onboarding",
                "Demonstrate 30% efficiency gains"
              ]}
            />
            <MilestoneBox
              week="5-8"
              title="Expansion: 5 London Regions"
              items={[
                "Scale to 11 hospitals (5M patients)",
                "Multi-trust interoperability proven",
                "Emergency access protocols established",
                "£5-10bn annual savings visible"
              ]}
            />
            <MilestoneBox
              week="9-12"
              title="National Preparation"
              items={[
                "All NHS trusts prepared for rollout",
                "Integration with NHS Digital complete",
                "Staff training nationwide",
                "£25-42bn annual savings roadmap clear"
              ]}
            />
          </div>
        </Card>

        {/* CTA */}
        <div className="rounded-2xl bg-gradient-to-br from-green-900/30 to-cyan-900/30 border border-[var(--positive)]/30 p-8 text-center">
          <h3 className="text-2xl font-bold text-[var(--color-foreground)] mb-3">
            Free at Point of Delivery, Zero Waiting Lists
          </h3>
          <p className="text-lg text-[var(--muted)] mb-6 max-w-2xl mx-auto">
            OASIS Oracle delivers on Reform UK's NHS pledge: maintain free healthcare while eliminating 
            waste through blockchain transparency and real-time resource optimization.
          </p>
          <div className="flex gap-4 justify-center">
            <Link
              href="/reform-government-spend-tracker"
              className="px-6 py-3 bg-[var(--accent)] text-[#041321] rounded-lg font-semibold hover:bg-[#38e0ff] transition-colors"
            >
              View Government Spending
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

function HospitalRow({ name, total, available, occupied, waiting, status }: {
  name: string;
  total: number;
  available: number;
  occupied: number;
  waiting: number;
  status: "success" | "warning" | "danger";
}) {
  const utilization = (occupied / total) * 100;

  return (
    <div className="flex items-center justify-between p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30 hover:bg-[rgba(34,211,238,0.05)] transition">
      <div className="flex items-center gap-3 flex-1">
        <div className="h-10 w-10 rounded-lg bg-[var(--accent-soft)] flex items-center justify-center">
          <span className="text-xs font-bold text-[var(--accent)]">🏥</span>
        </div>
        <div>
          <h4 className="font-semibold text-[var(--color-foreground)]">{name}</h4>
          <p className="text-xs text-[var(--muted)]">{formatNumber(total)} beds total</p>
        </div>
      </div>

      <div className="flex items-center gap-6">
        <div className="text-right">
          <p className="text-sm font-semibold text-[var(--positive)]">{formatNumber(available)}</p>
          <p className="text-xs text-[var(--muted)]">Available</p>
        </div>

        <div className="text-right">
          <p className="text-sm font-semibold text-[var(--warning)]">{formatNumber(waiting)}</p>
          <p className="text-xs text-[var(--muted)]">Waiting</p>
        </div>

        <div className="w-32">
          <div className="h-2 w-full rounded-full bg-[rgba(5,5,16,0.8)] overflow-hidden">
            <div
              className={`h-full rounded-full transition-all duration-500 ${
                utilization < 75 ? "bg-[var(--positive)]" :
                utilization < 90 ? "bg-[var(--warning)]" :
                "bg-[var(--negative)]"
              }`}
              style={{ width: `${utilization}%` }}
            />
          </div>
          <p className="text-xs text-[var(--muted)] mt-1 text-center">
            {utilization.toFixed(0)}% full
          </p>
        </div>

        <Badge variant={status} size="sm" dot>
          {status === "success" ? "Healthy" : status === "warning" ? "Monitor" : "Critical"}
        </Badge>
      </div>
    </div>
  );
}

function ScenarioBox({ title, scenario, steps, outcome, color }: {
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

function SavingsRow({ category, current, withOASIS, savings }: {
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

function MilestoneBox({ week, title, items }: {
  week: string;
  title: string;
  items: string[];
}) {
  return (
    <div className="flex gap-4 p-4 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <div className="flex items-center justify-center h-10 w-16 rounded-lg bg-[var(--accent-soft)] text-[var(--accent)] font-bold text-sm flex-shrink-0">
        Week<br/>{week}
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





