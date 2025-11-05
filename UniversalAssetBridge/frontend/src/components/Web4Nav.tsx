"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";

const navLinks = [
  { name: "Bridge", path: "/" },
  { name: "qUSDC", path: "/qusdc" },
  { name: "Liquidity", path: "/liquidity" },
  { name: "Token Portal", path: "/token-portal" },
  { name: "Create Token", path: "/mint-token" },
  { name: "Upgrade Token", path: "/migrate-token" },
  { name: "Docs", path: "/docs" },
];

export default function Web4Nav() {
  const pathname = usePathname();

  return (
    <nav className="flex items-center justify-center gap-8 py-6 mb-6 border-b" style={{
      borderColor: 'var(--oasis-card-border)',
    }}>
      {navLinks.map((link) => {
        const isActive = pathname === link.path;
        return (
          <Link
            key={link.path}
            href={link.path}
            className="text-sm font-semibold transition-colors uppercase tracking-wider"
            style={{
              color: isActive ? 'var(--oasis-accent)' : 'var(--oasis-muted)',
            }}
          >
            {link.name}
          </Link>
        );
      })}
    </nav>
  );
}

