"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";

export type CredentialsPanelProps = {
  defaultUsername?: string;
  defaultPassword?: string;
  baseUrl?: string;
  onAuthenticate?: (credentials: { username: string; password: string }) => void;
  onAcquireAvatar?: () => void;
  onToken?: (token: string) => void;
  onAuthenticated?: (payload: { token: string; avatarId?: string | null }) => void;
};

export function CredentialsPanel({
  defaultUsername = "metabricks_admin",
  defaultPassword = "Uppermall1!",
  baseUrl = "http://devnet.oasisweb4.one",
  onAuthenticate,
  onAcquireAvatar,
  onToken,
  onAuthenticated,
}: CredentialsPanelProps) {
  const [username, setUsername] = useState(defaultUsername);
  const [password, setPassword] = useState(defaultPassword);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const authenticate = async () => {
    try {
      setIsLoading(true);
      setError(null);
      onAuthenticate?.({ username, password });

      const normalizedBaseUrl = baseUrl.replace(/\/$/, "");
      const response = await fetch(`${normalizedBaseUrl}/api/avatar/authenticate`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Accept: "application/json",
        },
        credentials: "include",
        body: JSON.stringify({ username, password }),
      });

      const text = await response.text();
      let data: { message?: string; result?: { jwtToken?: string; avatarId?: string; avatar?: { id?: string; AvatarId?: string } } } | null = null;
      try {
        data = text ? JSON.parse(text) : null;
      } catch (parseError) {
        console.warn("Authentication response was not valid JSON", parseError);
      }

      const token = data?.result?.jwtToken;
      if (!response.ok || !token) {
        const message = data?.message ?? `Authentication failed (HTTP ${response.status})`;
        throw new Error(message);
      }

      const avatarId =
        data?.result?.avatarId ??
        data?.result?.avatar?.id ??
        data?.result?.avatar?.AvatarId ??
        null;

      onToken?.(token);
      onAuthenticated?.({ token, avatarId });
    } catch (err: unknown) {
      console.error("Authentication error", err);
      const message = err instanceof Error ? err.message : "Authentication failed";
      setError(message);
    } finally {
      setIsLoading(false);
    }
  };

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
            disabled={isLoading}
            onClick={authenticate}
            className="whitespace-nowrap"
          >
            {isLoading ? "Authenticating..." : "Authenticate Avatar"}
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
      {error ? <p className="text-xs text-[var(--negative)]">{error}</p> : null}
    </div>
  );
}

