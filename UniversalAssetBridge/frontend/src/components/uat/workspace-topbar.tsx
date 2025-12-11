"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import { BadgeCheck, ChevronDown, CloudLightning, Database, HelpCircle, Play, Shield, Workflow, Zap } from "lucide-react";

const environments = [
  { id: "devnet", label: "Devnet", accent: "text-[#2d98ff]" },
  { id: "testnet", label: "Testnet", accent: "text-[#b28bff]" },
  { id: "local", label: "Local", accent: "text-[#7fb5ff]" },
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
  const [controlsOpen, setControlsOpen] = useState(false);
  const [howItWorksOpen, setHowItWorksOpen] = useState(false);

  const compliancePercent =
    complianceProgress.requiredTotal === 0
      ? 0
      : Math.round((complianceProgress.completed / complianceProgress.requiredTotal) * 100);

  const currentEnvironment = environments.find((env) => env.id === environment);

  return (
    <section className="relative rounded-3xl border border-[#1f2b40] bg-[#0b1424] px-6 py-5 shadow-[0_16px_45px_rgba(5,12,28,0.35)]">
      <div className="pointer-events-auto absolute right-6 top-5 z-40 flex items-center gap-2">
        <button
          type="button"
          onClick={() => setHowItWorksOpen((prev) => !prev)}
          className={cn(
            "flex items-center gap-2 rounded-full border px-4 py-2 text-sm font-semibold uppercase tracking-[0.25em] transition shadow-[0_8px_20px_rgba(12,24,48,0.25)]",
            howItWorksOpen
              ? "border-[#2d98ff]/60 bg-[rgba(45,152,255,0.18)] text-[#2d98ff]"
              : "border-[#24324c] bg-[rgba(11,18,36,0.78)] text-[#7d8bb1] hover:border-[#2d98ff]/40"
          )}
        >
          <HelpCircle className="h-4 w-4" />
          How it works
        </button>
      </div>

      {howItWorksOpen && (
        <div className="pointer-events-auto absolute right-6 top-20 z-50 w-full max-w-sm rounded-2xl border border-[#24324c] bg-[rgba(11,18,36,0.95)] p-5 text-sm text-[#9ba8c8] shadow-[0_24px_55px_rgba(6,14,28,0.45)] backdrop-blur-xl">
          <p className="text-xs uppercase tracking-[0.35em] text-[#7d8bb1]">What this builder does</p>
          <p className="mt-3 leading-relaxed text-[#d0d9f5]">
            The UAT Builder is the authoring environment for Universal Asset Tokens. Modules encapsulate the legal,
            compliance, and commercial blocks that must travel with a token. Core modules declare the asset identity,
            jurisdiction, and distribution rights while advanced modules layer revenue automation, governance hooks, and
            risk controls.
          </p>
          <p className="mt-3 leading-relaxed text-[#d0d9f5]">
            Drag the required modules into the canvas, complete the inspector fields until each badge reads Ready, and
            then enrich with optional modules that match your structure. Once every compliance anchor is ready, the
            workspace outputs a mint payload that feeds the trust, tokenization, and x402 distribution pipelines.
          </p>
          <p className="mt-3 leading-relaxed text-[#d0d9f5]">
            The final output is a Real World Asset blueprint designed for robust compliance: a deterministic module stack
            that auditors, trustees, and distribution partners can verify against our policy engine before capital moves.
          </p>
          <p className="mt-3 text-xs leading-relaxed text-[#7d8bb1]">
            Disclaimers: This workspace does not constitute investment advice or legal counsel. All payloads require
            external counsel sign-off, jurisdiction-specific filings, and ongoing monitoring through the compliance
            program before tokens are offered to the public.
          </p>
        </div>
      )
      }

      <div className="relative grid gap-6 md:grid-cols-[minmax(0,1fr)_minmax(0,0.85fr)]">
        <div className="space-y-3">
          <div className="flex items-center gap-3 text-xs uppercase tracking-[0.4em] text-[#7d8bb1]">
            <span className="rounded-full border border-[#2d98ff]/45 bg-[rgba(45,152,255,0.12)] px-3 py-1 text-[#2d98ff]">
              UAT Builder
            </span>
            <span className="rounded-full border border-[#24324c] bg-[rgba(11,18,36,0.78)] px-3 py-1 text-[10px] uppercase tracking-[0.3em] text-[#7d8bb1]">
              Drag & Drop
            </span>
          </div>
          <div>
            <h1 className="text-3xl font-semibold tracking-tight text-[#d8e4ff]">Token Design Workspace</h1>
            <p className="mt-1 max-w-3xl text-sm text-[#9ba8c8]">
              Assemble compliant Universal Asset Tokens with trust structures, revenue automation, and multi-chain
              deployment. Configure modules, validate data, and generate mint-ready payloads for your analysts.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3 text-xs uppercase tracking-wide text-[#7d8bb1]">
            <span className="flex items-center gap-2 rounded-full border border-[rgba(32,158,98,0.35)] bg-[rgba(32,158,98,0.12)] px-3 py-1.5 text-[#1f8f5a]">
              <Shield className="h-3.5 w-3.5" />
              {compliancePercent}% Compliance
            </span>
            <span className="flex items-center gap-2 rounded-full border border-[#25324a] bg-[rgba(11,18,36,0.78)] px-3 py-1.5 text-[#7d8bb1]">
              <Workflow className="h-3.5 w-3.5" />
              {complianceProgress.completed}/{complianceProgress.requiredTotal} Required Modules Complete
            </span>
            <span className="flex items-center gap-2 rounded-full border border-[#25324a] bg-[rgba(11,18,36,0.78)] px-3 py-1.5 text-[#7d8bb1]">
              <BadgeCheck className="h-3.5 w-3.5" />
              Schema v1.0
            </span>
          </div>
        </div>

        <div className="flex flex-col gap-3 text-sm text-[#7d8bb1]">
          <div className="flex flex-wrap items-center justify-end gap-3">
            <button
              type="button"
              className="flex items-center gap-2 rounded-xl border border-[#2d98ff]/60 bg-[rgba(45,152,255,0.15)] px-4 py-3 text-sm font-semibold text-[#2d98ff] transition hover:border-[#2d98ff]"
            >
              <Play className="h-4 w-4" />
              Preview payload
            </button>
            <button
              type="button"
              className="flex items-center gap-2 rounded-xl border border-[rgba(32,158,98,0.35)] bg-[rgba(32,158,98,0.18)] px-4 py-3 text-sm font-semibold text-[#1f8f5a] transition hover:border-[rgba(32,158,98,0.5)]"
            >
              <CloudLightning className="h-4 w-4" />
              Mint ready
            </button>
          </div>

          <CollapsibleCard
            title="Environment & Controls"
            subtitle={`${currentEnvironment?.label ?? "Select"} • JWT ${jwtConnected ? "linked" : "disconnected"} • x402 ${
              x402Enabled ? "enabled" : "disabled"
            }`}
            open={controlsOpen}
            onToggle={() => setControlsOpen((prev) => !prev)}
          >
            <div className="space-y-4 text-xs text-[#9ba8c8]">
              <div className="space-y-2">
                <span className="text-xs uppercase tracking-[0.35em] text-[#7d8bb1]">Environment</span>
                <div className="flex gap-2">
                  {environments.map((env) => (
                    <button
                      key={env.id}
                      type="button"
                      onClick={() => onEnvironmentChange(env.id)}
                      className={cn(
                        "flex-1 rounded-lg border px-3 py-2 text-xs font-medium transition",
                        environment === env.id
                          ? "border-[#2d98ff]/60 bg-[rgba(45,152,255,0.15)] text-[#2d98ff]"
                          : "border-[#24324c] bg-[rgba(11,18,36,0.78)] text-[#7d8bb1] hover:border-[#2d98ff]/40"
                      )}
                    >
                      <span className={cn("block text-left text-[10px] uppercase tracking-[0.3em]", env.accent)}>
                        {env.label}
                      </span>
                      <span className="mt-1 block text-left text-[10px] uppercase tracking-wide text-[#7d8bb1]">
                        {env.id === "local" && "Workbench"}
                        {env.id === "devnet" && "QA & Staging"}
                        {env.id === "testnet" && "Audit-ready"}
                      </span>
                    </button>
                  ))}
                </div>
              </div>

              <div className="space-y-2">
                <span className="text-xs uppercase tracking-[0.35em] text-[#7d8bb1]">JWT Session</span>
                <div className="flex items-center justify-between gap-4 rounded-xl border border-[#1f2b40] bg-[#0e1a2d] px-3 py-3 text-xs">
                  <div className="flex items-center gap-2">
                    <Database className="h-4 w-4 text-[#2d98ff]" />
                    Required for secure access to AssetRail contract generator and x402 distribution endpoints.
                  </div>
                  <button
                    type="button"
                    onClick={onToggleJwt}
                    className={cn(
                      "rounded-full border px-3 py-1 text-xs transition",
                      jwtConnected
                        ? "border-[#2d98ff]/45 bg-[rgba(45,152,255,0.2)] text-[#2d98ff]"
                        : "border-[rgba(210,93,93,0.4)] bg-[rgba(255,217,217,0.2)] text-[#a23434]"
                    )}
                  >
                    {jwtConnected ? "Linked" : "Connect"}
                  </button>
                </div>
              </div>

              <div className="space-y-2">
                <span className="text-xs uppercase tracking-[0.35em] text-[#7d8bb1]">Compliance & Revenue</span>
                <div className="flex flex-wrap items-center gap-3 text-xs uppercase tracking-wide text-[#7d8bb1]">
                  <span className="flex items-center gap-2 rounded-xl border border-[#24324c] bg-[rgba(11,18,36,0.78)] px-3 py-2">
                    <Shield className="h-4 w-4 text-[#2d98ff]" />
                    Compliance gating active
                  </span>
                  <button
                    type="button"
                    onClick={onToggleX402}
                    className={cn(
                      "flex items-center gap-2 rounded-xl border px-3 py-2 transition",
                      x402Enabled
                        ? "border-[rgba(45,152,255,0.35)] bg-[rgba(45,152,255,0.12)] text-[#2d98ff]"
                        : "border-[rgba(148,163,184,0.3)] bg-[rgba(11,18,36,0.78)] text-[#7d8bb1]"
                    )}
                  >
                    <Zap className="h-4 w-4" />
                    x402 revenue {x402Enabled ? "enabled" : "disabled"}
                  </button>
                  <span className="flex items-center gap-2 rounded-xl border border-[#24324c] bg-[rgba(11,18,36,0.78)] px-3 py-2">
                    <Play className="h-4 w-4 text-[#7d8bb1]" />
                    Drag modules into canvas to configure
                  </span>
                </div>
              </div>
            </div>
          </CollapsibleCard>
        </div>
      </div>
    </section>
  );
}

type CollapsibleCardProps = {
  title: string;
  subtitle: string;
  open: boolean;
  onToggle: () => void;
  children: React.ReactNode;
};

function CollapsibleCard({ title, subtitle, open, onToggle, children }: CollapsibleCardProps) {
  return (
    <div className="rounded-2xl border border-[#1f2b40] bg-[#0e1a2d]">
      <button
        type="button"
        onClick={onToggle}
        className="flex w-full items-center justify-between gap-3 px-4 py-3 text-left text-sm text-[#d8e4ff]"
      >
        <div className="space-y-1">
          <span className="text-xs uppercase tracking-[0.35em] text-[#7d8bb1]">{title}</span>
          <span className="block text-[12px] text-[#9ba8c8]">{subtitle}</span>
        </div>
        <ChevronDown
          className={cn(
            "h-4 w-4 text-[#7d8bb1] transition-transform duration-200",
            open ? "rotate-180 text-[#2d98ff]" : ""
          )}
        />
      </button>
      {open && <div className="border-t border-[#1f2b40] px-4 py-4">{children}</div>}
    </div>
  );
}


