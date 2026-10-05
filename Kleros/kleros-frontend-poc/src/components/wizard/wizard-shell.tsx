"use client";

import { ReactNode } from "react";
import { cn } from "@/lib/utils";

type WizardShellProps = {
  steps: { id: string; title: string; description: string }[];
  activeStep: string;
  onStepChange?: (stepId: string) => void;
  children: ReactNode;
  footer?: ReactNode;
};

export function WizardShell({ steps, activeStep, onStepChange, children, footer }: WizardShellProps) {
  return (
    <div className="glass-card gradient-ring relative overflow-hidden rounded-3xl border border-[var(--color-card-border)]/60">
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.2),transparent_70%)]" />
      <div className="relative grid gap-6 p-6 lg:grid-cols-[280px_1fr] lg:gap-10 lg:p-8">
        <aside className="space-y-6">
          <h2 className="text-lg font-semibold text-[var(--color-foreground)]">Solana Mint Flow</h2>
          <p className="text-sm leading-relaxed text-[var(--muted)]">
            Configure SolanaOASIS + MongoDBOASIS, upload your assets, and submit a compliant `/api/nft/mint-nft` payload.
          </p>
          <ol className="space-y-3 text-sm">
            {steps.map((step, index) => {
              const isActive = step.id === activeStep;
              const isPast = steps.findIndex((s) => s.id === activeStep) > index;
              return (
                <li key={step.id}>
                  <button
                    type="button"
                    onClick={() => onStepChange?.(step.id)}
                    className={cn(
                      "flex w-full items-center gap-3 rounded-xl border px-4 py-3 text-left transition",
                      isActive
                        ? "border-[var(--accent)]/70 bg-[rgba(34,211,238,0.12)] text-[var(--color-foreground)]"
                        : "border-transparent bg-[rgba(8,11,26,0.6)] text-[var(--muted)] hover:border-[var(--accent)]/30 hover:text-[var(--color-foreground)]"
                    )}
                  >
                    <span
                      className={cn(
                        "flex h-6 w-6 items-center justify-center rounded-full text-xs font-semibold",
                        isActive
                          ? "bg-[var(--accent)] text-[#041321]"
                          : isPast
                          ? "bg-[var(--accent-soft)] text-[var(--accent)]"
                          : "bg-[rgba(6,10,25,0.85)] text-[var(--muted)]"
                      )}
                    >
                      {index + 1}
                    </span>
                    <div>
                      <p className="font-semibold leading-tight">{step.title}</p>
                      <p className="text-xs text-[var(--muted)]">{step.description}</p>
                    </div>
                  </button>
                </li>
              );
            })}
          </ol>
        </aside>
        <section className="min-h-[460px] rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(6,11,26,0.7)] p-6 shadow-inner lg:p-8">
          {children}
          {footer ? <div className="mt-8 border-t border-[var(--color-card-border)]/30 pt-6">{footer}</div> : null}
        </section>
      </div>
    </div>
  );
}
