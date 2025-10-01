"use client";

import { Button } from "@/components/ui/button";

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
  onRegister?: (provider: ProviderToggle) => Promise<boolean> | boolean;
  onActivate?: (provider: ProviderToggle) => Promise<boolean> | boolean;
  onError?: (provider: ProviderToggle, mode: "register" | "activate", error: Error) => void;
  loadingIds?: string[];
};

export function ProviderTogglePanel({ providers, onRegister, onActivate, onError, loadingIds = [] }: ProviderTogglePanelProps) {
  return (
    <div className="space-y-3">
      {providers.map((provider) => {
        const isRegistered = provider.state !== "idle";
        const isActive = provider.state === "active";
        const isLoading = loadingIds.includes(provider.id);
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
                disabled={isLoading}
                onClick={async () => {
                  if (onRegister) {
                    try {
                      await onRegister(provider);
                    } catch (error) {
                      if (error instanceof Error) {
                        onError?.(provider, "register", error);
                      }
                    }
                  }
                }}
                className="px-3 py-1 text-xs"
              >
                {isLoading ? "Processing" : isRegistered ? "Registered" : "Register"}
              </Button>
              <Button
                variant="toggle"
                data-state={isActive ? "active" : undefined}
                disabled={!isRegistered || isLoading}
                onClick={async () => {
                  if (onActivate) {
                    try {
                      await onActivate(provider);
                    } catch (error) {
                      if (error instanceof Error) {
                        onError?.(provider, "activate", error);
                      }
                    }
                  }
                }}
                className="px-3 py-1 text-xs"
              >
                {isLoading ? "Processing" : isActive ? "Active" : "Activate"}
              </Button>
            </div>
          </div>
        );
      })}
    </div>
  );
}
