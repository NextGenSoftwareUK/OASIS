"use client";

import Link from "next/link";
import { ReactNode } from "react";
import { Activity, TrendingUp, CheckCircle2, Coins, Shuffle, Image, Vote, Wallet, Network, Flag, Trophy } from "lucide-react";

const navItems = [
  { label: "Dashboard", href: "/", icon: Activity },
  { label: "Network 3D", href: "/network", icon: Network },
  { label: "Competition", href: "/competition", icon: Trophy },
  { label: "Collateral", href: "/collateral", icon: Wallet },
  { label: "Verify", href: "/verify", icon: CheckCircle2 },
  { label: "Prices", href: "/prices", icon: TrendingUp },
  { label: "Arbitrage", href: "/arbitrage", icon: Shuffle },
  { label: "Reform UK", href: "/reform", icon: Flag },
];

type OracleLayoutProps = {
  children: ReactNode;
};

export function OracleLayout({ children }: OracleLayoutProps) {
  return (
    <div className="min-h-screen bg-[var(--color-background)] text-[var(--color-foreground)]">
      {/* Radial gradient overlay */}
      <div className="relative overflow-hidden">
        <div className="absolute inset-x-0 top-0 h-72 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.35),transparent_60%)] blur-3xl" />
        
        {/* Header */}
        <header className="relative z-10 border-b border-[var(--color-card-border)]/40 bg-[rgba(5,5,16,0.85)]/90 backdrop-blur-xl">
          <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-5">
            {/* Logo & Title */}
            <Link href="/" className="flex items-center gap-3 hover:opacity-80 transition">
              <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-gradient-to-br from-[var(--accent)] to-[var(--accent-strong)] shadow-lg">
                <Activity className="h-6 w-6 text-[#041321]" strokeWidth={2.5} />
              </div>
              <div>
                <p className="text-xs uppercase tracking-[0.4em] text-[var(--muted)]">OASIS WEB4</p>
                <h1 className="text-xl font-semibold text-[var(--color-foreground)]">
                  Oracle Network
                </h1>
              </div>
            </Link>

            {/* Navigation */}
            <nav className="hidden gap-6 text-sm lg:flex">
              {navItems.map((item) => {
                const Icon = item.icon;
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className="flex items-center gap-2 text-[var(--muted)] transition hover:text-[var(--accent)] group"
                  >
                    <Icon className="h-4 w-4 transition group-hover:scale-110" />
                    <span>{item.label}</span>
                  </Link>
                );
              })}
            </nav>

            {/* Status Badge */}
            <div className="hidden items-center gap-3 md:flex">
              <span className="flex items-center gap-2 rounded-full border border-[var(--accent-soft)] bg-[var(--accent-soft)] px-3 py-1.5 text-xs uppercase tracking-wide text-[var(--accent)]">
                <span className="h-2 w-2 rounded-full bg-[var(--accent)] animate-pulse" />
                Live
              </span>
              <span className="flex items-center gap-2 rounded-full border border-[var(--color-card-border)]/50 bg-[rgba(8,10,25,0.85)] px-3 py-1.5 text-xs text-[var(--muted)]">
                <Coins className="h-3 w-3" />
                20 Chains
              </span>
            </div>
          </div>
        </header>
      </div>

      {/* Main Content */}
      <main className="mx-auto max-w-7xl px-6 py-10 lg:px-10">
        {children}
      </main>

      {/* Footer */}
      <footer className="border-t border-[var(--color-card-border)]/30 bg-[rgba(5,5,16,0.85)] mt-20">
        <div className="mx-auto max-w-7xl px-6 py-8 text-center">
          <p className="text-sm text-[var(--muted)]">
            OASIS Multi-Chain Oracle Network · Built for the decentralized web
          </p>
          <p className="mt-2 text-xs text-[var(--muted)]">
            Monitoring 20+ blockchains · Aggregating 8+ price sources · Real-time verification
          </p>
        </div>
      </footer>
    </div>
  );
}

