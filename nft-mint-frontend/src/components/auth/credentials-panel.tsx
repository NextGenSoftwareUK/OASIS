"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

export type CredentialsPanelProps = {
  defaultUsername?: string;
  defaultPassword?: string;
  onAuthenticate?: (credentials: { username: string; password: string }) => void;
  onAcquireAvatar?: () => void;
};

export function CredentialsPanel({
  defaultUsername = "metabricks_admin",
  defaultPassword = "Uppermall1!",
  onAuthenticate,
  onAcquireAvatar,
}: CredentialsPanelProps) {
  const [username, setUsername] = useState(defaultUsername);
  const [password, setPassword] = useState(defaultPassword);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-end gap-4 rounded-2xl border border-[var(--color-card-border)]/60 bg-[rgba(7,12,30,0.75)] p-5">
        <div className="flex-1 min-w-[200px]">
          <label className="text-xs uppercase tracking-widest text-[var(--muted)]">Username</label>
          <input
            value={username}
            onChange={(event) => setUsername(event.target.value)}
            className="mt-2 w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
            placeholder="metabricks_admin"
          />
        </div>
        <div className="flex-1 min-w-[200px]">
          <label className="text-xs uppercase tracking-widest text-[var(--muted)]">Password</label>
          <input
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className="mt-2 w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
            placeholder="Uppermall1!"
          />
        </div>
        <div className="flex flex-col gap-3">
          <Button
            variant="primary"
            onClick={() => onAuthenticate?.({ username, password })}
            className="whitespace-nowrap"
          >
            Authenticate Avatar
          </Button>
          <Button
            variant="secondary"
            onClick={onAcquireAvatar}
            className="whitespace-nowrap"
          >
            Acquire Avatar
          </Button>
        </div>
      </div>
      <p className="text-xs text-[var(--muted)]">
        No avatar yet? Purchasing a MetaBrick at <a className="text-[var(--accent)] underline" href="https://metabricks.xyz" target="_blank" rel="noreferrer">MetaBricks.xyz</a> will provision credentials automatically.
      </p>
    </div>
  );
}
