"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

const navItems = [
  { label: "Bridge", href: "/" },
  { label: "Token Portal", href: "/token-portal" },
  { label: "Create Token", href: "/mint-token" },
  { label: "Migrate Token", href: "/migrate-token" },
  { label: "Docs", href: "/docs" },
];

export default function Web4Header() {
  const pathname = usePathname();

  return (
    <div className="relative overflow-hidden">
      <div className="absolute inset-x-0 top-0 h-72 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.35),transparent_60%)] blur-3xl" />
      <header className="relative z-10 border-b backdrop-blur-xl" style={{
        borderColor: 'rgba(56, 189, 248, 0.2)',
        background: 'rgba(5,5,16,0.85)'
      }}>
        <div className="mx-auto flex max-w-7xl items-center justify-between px-6 py-5">
          <div className="flex items-center gap-3">
            <div className="flex h-10 w-10 items-center justify-center rounded-lg" style={{background: 'var(--oasis-accent-soft)'}}>
              <span className="text-xl font-bold" style={{color: 'var(--oasis-accent)'}}>W4</span>
            </div>
            <div>
              <p className="text-xs uppercase tracking-[0.5em]" style={{color: 'var(--oasis-muted)'}}>OASIS WEB4</p>
              <h1 className="text-xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
                Token Platform
              </h1>
            </div>
          </div>
          <nav className="hidden gap-6 text-sm md:flex">
            {navItems.map((item) => (
              <Link
                key={item.href}
                href={item.href}
                className={cn(
                  "transition font-medium",
                  pathname === item.href
                    ? "text-[var(--oasis-accent)]"
                    : "text-[var(--oasis-muted)] hover:text-[var(--oasis-accent)]"
                )}
              >
                {item.label}
              </Link>
            ))}
          </nav>
          <div className="hidden items-center gap-3 md:flex">
            <span className="flex items-center gap-2 rounded-full border px-3 py-1 text-xs uppercase tracking-wide" style={{
              borderColor: 'var(--oasis-accent)',
              background: 'var(--oasis-accent-soft)',
              color: 'var(--oasis-accent)'
            }}>
              10 Chains Active
            </span>
          </div>
        </div>
      </header>
    </div>
  );
}

