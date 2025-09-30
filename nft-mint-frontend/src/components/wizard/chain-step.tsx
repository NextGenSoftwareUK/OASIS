"use client";

import { cn } from "@/lib/utils";
import { SOLANA_CHAIN } from "@/types/chains";

const CONFIG_OPTIONS = SOLANA_CHAIN.configurationVariants ?? [];

type SolanaConfigStepProps = {
  selectedOption: string;
  onSelect: (option: string) => void;
};

export function SolanaConfigStep({ selectedOption, onSelect }: SolanaConfigStepProps) {
  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <button
          type="button"
          onClick={() => onSelect("Metaplex Standard")}
          className={cn(
            "glass-card relative overflow-hidden rounded-2xl border border-[var(--color-card-border)]/60 p-5 text-left transition",
            selectedOption === "Metaplex Standard" ? "shadow-[0_25px_60px_rgba(20,184,166,0.25)] border-[var(--accent)]/70" : "hover:border-[var(--accent)]/50"
          )}
        >
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
          <div className="relative">
            <h3 className="text-lg font-semibold text-[var(--color-foreground)]">Metaplex Standard</h3>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Default Solana NFT format with metadata hosted off-chain and verified collection support.
            </p>
          </div>
        </button>
        <button
          type="button"
          onClick={() => onSelect("Collection with Verified Creator")}
          className={cn(
            "glass-card relative overflow-hidden rounded-2xl border border-[var(--color-card-border)]/60 p-5 text-left transition",
            selectedOption === "Collection with Verified Creator" ? "shadow-[0_25px_60px_rgba(20,184,166,0.25)] border-[var(--accent)]/70" : "hover:border-[var(--accent)]/50"
          )}
        >
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
          <div className="relative">
            <h3 className="text-lg font-semibold text-[var(--color-foreground)]">Verified Creator</h3>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Configure collection and creator signatures to comply with marketplaces like Magic Eden.
            </p>
          </div>
        </button>
      </div>
      <div className="grid gap-4 sm:grid-cols-2">
        <button
          type="button"
          onClick={() => onSelect("Editioned Series")}
          className={cn(
            "glass-card relative overflow-hidden rounded-2xl border border-[var(--color-card-border)]/60 p-5 text-left transition",
            selectedOption === "Editioned Series" ? "shadow-[0_25px_60px_rgba(20,184,166,0.25)] border-[var(--accent)]/70" : "hover:border-[var(--accent)]/50"
          )}
        >
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
          <div className="relative">
            <h3 className="text-lg font-semibold text-[var(--color-foreground)]">Editioned Series</h3>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Enable limited edition prints or master edition drops managed through Metaplex.
            </p>
          </div>
        </button>
        <button
          type="button"
          onClick={() => onSelect("Compressed NFT (Bubblegum)")}
          className={cn(
            "glass-card relative overflow-hidden rounded-2xl border border-[var(--color-card-border)]/60 p-5 text-left transition",
            selectedOption === "Compressed NFT (Bubblegum)" ? "shadow-[0_25px_60px_rgba(20,184,166,0.25)] border-[var(--accent)]/70" : "hover:border-[var(--accent)]/50"
          )}
        >
          <div className="absolute inset-0 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.18),transparent_60%)]" />
          <div className="relative">
            <h3 className="text-lg font-semibold text-[var(--color-foreground)]">Compressed NFT (Bubblegum)</h3>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Prepare metadata for compressed mints via OASIS + Metaplex Bubblegum pipelines.
            </p>
          </div>
        </button>
      </div>
    </div>
  );
}
