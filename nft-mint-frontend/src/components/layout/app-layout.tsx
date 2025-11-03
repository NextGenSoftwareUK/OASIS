"use client";

import Link from "next/link";
import { ReactNode } from "react";
import { cn } from "@/lib/utils";

const navItems = [
  { label: "Solana", href: "#wizard" },
  { label: "Providers", href: "#providers" },
  { label: "Assets", href: "#assets" },
  { label: "Mint", href: "#mint" },
  { label: "x402 Dashboard", href: "/x402-dashboard", isExternal: true },
];

type AppLayoutProps = {
  children: ReactNode;
  sidebar?: ReactNode;
  footer?: ReactNode;
};

export function AppLayout({ children, sidebar, footer }: AppLayoutProps) {
  return (
    <div className="min-h-screen bg-[var(--color-background)] text-[var(--color-foreground)]">
      <div className="relative overflow-hidden">
        <div className="absolute inset-x-0 top-0 h-72 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.35),transparent_60%)] blur-3xl" />
        <header className="relative z-10 border-b border-[var(--color-card-border)]/40 bg-[rgba(5,5,16,0.85)]/90 backdrop-blur-xl">
          <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-5">
            <div className="flex items-center gap-3">
              <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-[var(--accent-soft)]">
                <span className="text-xl font-bold text-[var(--accent)]">O4</span>
              </div>
              <div>
                <p className="text-sm uppercase tracking-[0.5em] text-[var(--muted)]">OASIS WEB4</p>
                <h1 className="text-2xl font-semibold text-[var(--color-foreground)]">
                  Solana NFT Mint Studio
                </h1>
              </div>
            </div>
            <nav className="hidden gap-6 text-sm md:flex">
              {navItems.map((item) => (
                <Link
                  key={item.href}
                  href={item.href}
                  className="text-[var(--muted)] transition hover:text-[var(--accent)]"
                >
                  {item.label}
                </Link>
              ))}
            </nav>
            <div className="hidden items-center gap-3 md:flex">
              <span className="flex items-center gap-2 rounded-full border border-[var(--accent-soft)] bg-[var(--accent-soft)] px-3 py-1 text-xs uppercase tracking-wide text-[var(--accent)]">
                Solana Devnet
              </span>
            </div>
          </div>
        </header>
      </div>
      <main className="flex w-full flex-col gap-6 px-4 py-10 md:flex-row md:items-start lg:px-10 xl:px-20">
        {sidebar ? (
          <aside className="w-full md:w-60 lg:w-64">
            <div className="space-y-4 rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,10,25,0.85)] p-4 shadow-[0_15px_30px_rgba(15,118,110,0.18)] backdrop-blur-xl">
              {sidebar}
            </div>
          </aside>
        ) : null}
        <section className={cn("flex-1 space-y-8", sidebar ? "md:pl-4 lg:pl-6" : "")}>{children}</section>
      </main>
      {footer ? <footer className="mx-auto mt-12 max-w-7xl px-6 pb-12">{footer}</footer> : null}
    </div>
  );
}
