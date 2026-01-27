"use client";

import { cn } from "@/lib/utils";
import { BadgeCheck, CloudLightning, Database, Play, Shield, Workflow, Zap } from "lucide-react";

const environments = [
  { id: "devnet", label: "Devnet", accent: "text-[var(--accent)]" },
  { id: "testnet", label: "Testnet", accent: "text-[rgba(168,85,247,0.9)]" },
  { id: "local", label: "Local", accent: "text-[rgba(96,165,250,0.95)]" },
] as const;

export type EnvironmentId = (typeof environments)[number]["id"];

type WorkspaceTopbarProps = {
  environment: EnvironmentId;
  onEnvironmentChange: (env: EnvironmentId) => void;
  jwtConnected: boolean;
  onToggleJwt: () => void;
  x402Enabled: boolean;
  onToggleX402: () => void;
  complianceProgress: {
    completed: number;
    requiredTotal: number;
  };
};

export function WorkspaceTopbar({
  environment,
  onEnvironmentChange,
  jwtConnected,
  onToggleJwt,
  x402Enabled,
  onToggleX402,
  complianceProgress,
}: WorkspaceTopbarProps) {
  const compliancePercent =
    complianceProgress.requiredTotal === 0
      ? 0
      : Math.round((complianceProgress.completed / complianceProgress.requiredTotal) * 100);

  return (
    <section className="serious-panel relative overflow-hidden rounded-3xl p-6">
      <div className="relative flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
        <div className="space-y-4">
          <div className="flex items-center gap-3">
            <span className="rounded-full border border-[var(--accent)]/50 bg-[rgba(34,211,238,0.08)] px-3 py-1 text-xs uppercase tracking-[0.4em] text-[var(--accent)]">
              UAT Builder
            </span>
            <span className="rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(9,15,30,0.75)] px-3 py-1 text-[10px] uppercase tracking-[0.3em] text-[var(--muted)]">
              Drag & Drop
            </span>
          </div>
          <div>
            <h1 className="text-3xl font-semibold tracking-tight text-[var(--color-foreground)]">
              Token Design Workspace
            </h1>
            <p className="mt-2 max-w-3xl text-sm text-[var(--muted)]">
              Assemble compliant Universal Asset Tokens with trust structures, revenue automation, and multi-chain
              deployment. Configure modules, validate data, and generate mint-ready payloads for your analysts.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3 text-xs uppercase tracking-wide text-[var(--muted)]">
            <span className="flex items-center gap-2 rounded-full border border-[var(--accent)]/35 bg-[rgba(9,15,28,0.9)] px-3 py-1.5 text-[var(--accent)]">
              <Shield className="h-3.5 w-3.5" />
              {compliancePercent}% Compliance
            </span>
            <span className="flex items-center gap-2 rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(9,15,28,0.9)] px-3 py-1.5 text-[var(--muted)]">
              <Workflow className="h-3.5 w-3.5" />
              {complianceProgress.completed}/{complianceProgress.requiredTotal} Required
            </span>
            <span className="flex items-center gap-2 rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(9,15,28,0.9)] px-3 py-1.5 text-[var(--muted)]">
              <BadgeCheck className="h-3.5 w-3.5" />
              Schema v1.0
            </span>
          </div>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row lg:flex-col">
          <button
            type="button"
            className="flex-1 rounded-xl border border-[var(--accent)]/60 bg-[rgba(34,211,238,0.12)] px-4 py-3 text-sm font-medium text-[var(--accent)] transition hover:border-[var(--accent)]/80"
          >
            <Play className="mr-2 inline h-4 w-4" />
            Preview payload
          </button>
          <button
            type="button"
            className="flex-1 rounded-xl border border-[rgba(74,222,128,0.35)] bg-[rgba(21,128,61,0.25)] px-4 py-3 text-sm font-medium text-[rgba(187,247,208,0.95)] transition hover:border-[rgba(74,222,128,0.55)]"
          >
            <CloudLightning className="mr-2 inline h-4 w-4" />
            Mint ready
          </button>
        </div>
      </div>

      <div className="mt-6 grid gap-4 lg:grid-cols-3">
        <details className="serious-dropdown" open>
          <summary>Environment & Controls</summary>
          <div className="serious-dropdown__body space-y-4">
            <div className="flex flex-wrap gap-2">
              {environments.map((env) => (
                <button
                  key={env.id}
                  type="button"
                  onClick={() => onEnvironmentChange(env.id)}
                  className={cn(
                    "flex-1 min-w-[140px] rounded-xl border px-3 py-2 text-sm font-medium transition",
                    environment === env.id
                      ? "border-[var(--accent)]/60 bg-[rgba(34,211,238,0.12)] text-[var(--accent)] shadow-[0_0_18px_rgba(34,211,238,0.25)]"
                      : "border-[var(--color-card-border)]/40 bg-[rgba(6,10,22,0.92)] text-[var(--muted)] hover:border-[var(--accent)]/30"
                  )}
                >
                  <span className={cn("block text-left text-xs uppercase tracking-[0.3em]", env.accent)}>
                    {env.label}
                  </span>
                  <span className="mt-1 block text-left text-[10px] uppercase tracking-wide text-[var(--muted)]">
                    {env.id === "local" && "Workbench"}
                    {env.id === "devnet" && "QA & Staging"}
                    {env.id === "testnet" && "Audit-ready"}
                  </span>
                </button>
              ))}
            </div>
            <div className="flex flex-wrap items-center gap-3 text-sm text-[var(--muted)]">
              <div className="flex items-center gap-2 rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(7,12,24,0.8)] px-3 py-1.5">
                <Database className="h-4 w-4 text-[var(--accent)]" />
                <button
                  type="button"
                  onClick={onToggleJwt}
                  className={cn(
                    "rounded-full border px-3 py-0.5 text-xs transition",
                    jwtConnected
                      ? "border-[var(--accent)]/45 bg-[rgba(34,211,238,0.12)] text-[var(--accent)]"
                      : "border-[rgba(248,113,113,0.35)] bg-[rgba(127,29,29,0.3)] text-[rgba(252,165,165,0.95)]"
                  )}
                >
                  {jwtConnected ? "JWT linked" : "JWT disconnected"}
                </button>
              </div>
              <p className="text-xs text-[var(--muted)]">
                Required for AssetRail contract generator + secure module presets.
              </p>
            </div>
          </div>
        </details>

        <details className="serious-dropdown" open>
          <summary>Compliance Overview</summary>
          <div className="serious-dropdown__body space-y-4">
            <div className="flex items-center justify-between text-sm text-[var(--muted)]">
              <span>{complianceProgress.completed} of {complianceProgress.requiredTotal} anchors complete</span>
              <span className="text-[var(--color-foreground)] font-semibold">{compliancePercent}%</span>
            </div>
            <div className="h-2 w-full rounded-full bg-[rgba(7,12,24,0.7)]">
              <div
                className="h-full rounded-full bg-[linear-gradient(90deg,rgba(59,130,246,0.9),rgba(34,211,238,0.9))]"
                style={{ width: `${Math.min(100, compliancePercent)}%` }}
              />
            </div>
            <div className="rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,22,0.85)] px-4 py-3 text-xs text-[var(--muted)]">
              Compliance gating prevents minting until core modules are validated. Use the inspector to resolve missing
              data and re-run validation.
            </div>
          </div>
        </details>

        <details className="serious-dropdown" open>
          <summary>Revenue & Actions</summary>
          <div className="serious-dropdown__body space-y-4">
            <button
              type="button"
              onClick={onToggleX402}
              className={cn(
                "flex w-full items-center justify-between rounded-2xl border px-4 py-3 text-sm transition",
                x402Enabled
                  ? "border-[rgba(34,211,238,0.35)] bg-[rgba(34,211,238,0.08)] text-[var(--accent)]"
                  : "border-[rgba(148,163,184,0.3)] bg-[rgba(9,15,28,0.85)] text-[var(--muted)]"
              )}
            >
              <span className="flex items-center gap-2">
                <Zap className="h-4 w-4" />
                x402 revenue automation
              </span>
              <span>{x402Enabled ? "Enabled" : "Disabled"}</span>
            </button>
            <p className="text-xs text-[var(--muted)]">
              When enabled, mint payloads include revenue hooks for automated distributions, treasury share enforcement,
              and compliance-ready payout logs.
            </p>
          </div>
        </details>
      </div>
    </section>
  );
}


