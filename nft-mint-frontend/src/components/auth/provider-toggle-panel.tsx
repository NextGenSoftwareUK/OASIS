"use client";

import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export type ProviderToggle = {
  id: string;
  label: string;
  description: string;
  registerEndpoint: string;
  activateEndpoint: string;
  state: "idle" | "registered" | "active";
};

export type ProviderTogglePanelProps = {
  providers: ProviderToggle[];
  onRegister?: (id: string) => void;
  onActivate?: (id: string) => void;
};

export function ProviderTogglePanel({ providers, onRegister, onActivate }: ProviderTogglePanelProps) {
  return (
    <div className="space-y-3">
      {providers.map((provider) => {
        const isRegistered = provider.state !== "idle";
        const isActive = provider.state === "active";
        return (
          <div
            key={provider.id}
            className="flex items-center justify-between rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(7,12,30,0.75)] px-4 py-3"
          >
            <div>
              <p className="text-sm font-semibold text-[var(--color-foreground)]">{provider.label}</p>
              <p className="text-xs text-[var(--muted)]">{provider.description}</p>
            </div>
            <div className="flex items-center gap-2">
              <Button
                variant="toggle"
                data-state={isRegistered ? "active" : undefined}
                onClick={() => onRegister?.(provider.id)}
                className="px-3 py-1 text-xs"
              >
                {isRegistered ? "Registered" : "Register"}
              </Button>
              <Button
                variant="toggle"
                data-state={isActive ? "active" : undefined}
                disabled={!isRegistered}
                onClick={() => onActivate?.(provider.id)}
                className="px-3 py-1 text-xs"
              >
                {isActive ? "Active" : "Activate"}
              </Button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
