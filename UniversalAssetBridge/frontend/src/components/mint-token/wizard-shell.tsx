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
    <div className="glass-card gradient-ring relative overflow-hidden rounded-3xl border" style={{borderColor: 'var(--oasis-card-border)'}}>
      <div className="absolute inset-0 bg-[radial-gradient(circle_at_top_left,rgba(34,211,238,0.2),transparent_70%)]" />
      <div className="relative grid grid-cols-[280px_1fr] gap-10 p-8">
        <aside className="space-y-6">
          <h2 className="text-lg font-semibold" style={{color: 'var(--oasis-foreground)'}}>Web4 Token Creation</h2>
          <p className="text-sm leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
            Configure and deploy your token natively across multiple blockchains simultaneously.
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
                        ? "border-[var(--oasis-accent)]/70 bg-[rgba(34,211,238,0.12)]"
                        : "border-transparent bg-[rgba(8,11,26,0.6)] hover:border-[var(--oasis-accent)]/30"
                    )}
                  >
                    <span
                      className={cn(
                        "flex h-6 w-6 items-center justify-center rounded-full text-xs font-semibold",
                        isActive
                          ? "bg-[var(--oasis-accent)] text-[#041321]"
                          : isPast
                          ? "bg-[var(--oasis-accent)]/50 text-[var(--oasis-accent)]"
                          : "bg-[rgba(6,10,25,0.85)]"
                      )}
                      style={{color: isActive ? '#041321' : isPast ? 'var(--oasis-accent)' : 'var(--oasis-muted)'}}
                    >
                      {index + 1}
                    </span>
                    <div>
                      <p className="font-semibold leading-tight" style={{color: isActive ? 'var(--oasis-foreground)' : 'var(--oasis-muted)'}}>{step.title}</p>
                      <p className="text-xs" style={{color: 'var(--oasis-muted)'}}>{step.description}</p>
                    </div>
                  </button>
                </li>
              );
            })}
          </ol>
        </aside>
        <section className="min-h-[460px] rounded-2xl border p-8 shadow-inner" style={{
          borderColor: 'var(--oasis-card-border)',
          background: 'rgba(3,7,18,0.85)'
        }}>
          {children}
          {footer ? <div className="mt-8 border-t pt-6" style={{borderColor: 'var(--oasis-card-border)'}}>{footer}</div> : null}
        </section>
      </div>
    </div>
  );
}

