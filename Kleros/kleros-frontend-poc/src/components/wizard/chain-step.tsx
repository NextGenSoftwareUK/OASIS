"use client";

import { cn } from "@/lib/utils";

type SolanaConfigStepProps = {
  selectedOption: string;
  onSelect: (option: string) => void;
};

export function SolanaConfigStep({ selectedOption, onSelect }: SolanaConfigStepProps) {
  const baseClasses = "glass-card relative overflow-hidden rounded-2xl border border-[var(--color-card-border)]/60 p-5 text-left transition";
  const selectedClasses =
    "border-[var(--accent)]/80 shadow-[0_25px_60px_rgba(34,211,238,0.35)] ring-2 ring-[var(--accent)]/50";

  return (
    <div className="space-y-6">
      <div className="grid gap-4 sm:grid-cols-2">
        <button
          type="button"
          onClick={() => onSelect("Metaplex Standard")}
          className={cn(baseClasses, selectedOption === "Metaplex Standard" ? selectedClasses : "hover:border-[var(--accent)]/50")}
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
            baseClasses,
            selectedOption === "Collection with Verified Creator" ? selectedClasses : "hover:border-[var(--accent)]/50"
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
          className={cn(baseClasses, selectedOption === "Editioned Series" ? selectedClasses : "hover:border-[var(--accent)]/50")}
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
            baseClasses,
            selectedOption === "Compressed NFT (Bubblegum)" ? selectedClasses : "hover:border-[var(--accent)]/50"
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
