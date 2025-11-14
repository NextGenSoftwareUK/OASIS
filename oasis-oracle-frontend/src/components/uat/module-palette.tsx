"use client";

import { ModuleDefinition } from "@/lib/uat-modules";
import { cn } from "@/lib/utils";
import { Search, Sparkles } from "lucide-react";
import { useMemo, useState } from "react";

type ModulePaletteProps = {
  modules: ModuleDefinition[];
  onQuickAdd: (moduleId: string) => void;
  title: string;
  subtitle: string;
  badgeText?: string;
  badgeTone?: "required" | "optional";
  searchPlaceholder?: string;
  className?: string;
  fillHeight?: boolean;
};

export function ModulePalette({
  modules,
  onQuickAdd,
  title,
  subtitle,
  badgeText,
  badgeTone = "required",
  searchPlaceholder = "Search modules, compliance domains, revenue flows…",
  className,
  fillHeight = true,
}: ModulePaletteProps) {
  const [query, setQuery] = useState("");

  const filteredModules = useMemo(() => {
    if (!query.trim()) {
      return modules;
    }
    const lower = query.toLowerCase();
    return modules.filter((module) => {
      return (
        module.name.toLowerCase().includes(lower) ||
        module.description.toLowerCase().includes(lower) ||
        module.category.toLowerCase().includes(lower)
      );
    });
  }, [modules, query]);

  const badgeStyles =
    badgeTone === "required"
      ? "border-[rgba(248,113,113,0.35)] bg-[rgba(248,113,113,0.1)] text-[rgba(185,28,28,0.9)]"
      : "border-[rgba(96,165,250,0.4)] bg-[rgba(59,130,246,0.12)] text-[rgba(37,99,235,0.95)]";

  return (
    <section
      className={cn(
        "serious-panel rounded-2xl",
        fillHeight ? "flex h-full flex-col" : "flex flex-col",
        className
      )}
    >
      <header className="border-b border-[var(--color-card-border)]/50 px-4 py-5">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-xs uppercase tracking-[0.45em] text-[var(--muted)]">{subtitle}</p>
            <h2 className="mt-1 text-xl font-semibold text-[var(--color-foreground)]">{title}</h2>
          </div>
          {badgeText ? (
            <span
              className={cn(
                "rounded-full border px-3 py-1 text-xs font-medium uppercase tracking-wide",
                badgeStyles
              )}
            >
              {badgeText}
            </span>
          ) : null}
        </div>

        <div className="mt-4 flex items-center gap-2 rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(4,7,18,0.9)] px-3 py-2 text-sm text-[var(--muted)] shadow-inner shadow-[rgba(34,211,238,0.12)]">
          <Search className="h-4 w-4 shrink-0 text-[var(--accent)]" />
          <input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={searchPlaceholder}
            className="w-full bg-transparent text-sm text-[var(--color-foreground)] outline-none placeholder:text-[rgba(148,163,184,0.45)]"
          />
        </div>
      </header>

      <div className="flex-1 overflow-hidden">
        <div className="h-full overflow-y-auto px-4 py-4 space-y-4 pr-2">
          {filteredModules.map((module) => {
            const Icon = module.icon;
            return (
              <article
                key={module.id}
                draggable
                onDragStart={(event) => {
                  event.dataTransfer.setData("application/uat-module", module.id);
                  event.dataTransfer.effectAllowed = "copy";
                }}
                className="ticket-card group relative cursor-grab select-none overflow-hidden rounded-xl border border-transparent p-5 transition duration-200 hover:border-[rgba(59,130,246,0.35)] hover:shadow-[0px_15px_40px_rgba(15,23,42,0.2)] active:cursor-grabbing"
              >
                <div className="pointer-events-none absolute inset-0 bg-[radial-gradient(circle_at_top,#e0ecff,transparent_70%)] opacity-0 transition-opacity duration-300 group-hover:opacity-100" />
                <div className="relative flex items-start gap-4">
                  <div className="flex h-11 w-11 shrink-0 items-center justify-center rounded-lg border border-[rgba(59,130,246,0.25)] bg-white text-[rgba(15,23,42,0.85)] shadow-[0_8px_18px_rgba(30,64,175,0.15)]">
                    <Icon className="h-5 w-5" strokeWidth={1.75} />
                  </div>
                  <div className="space-y-2">
                    <div className="flex items-center gap-2">
                      <h3 className="text-base font-semibold text-[#0b1d33]">{module.name}</h3>
                      {module.required ? (
                        <span className="rounded-full border border-[rgba(248,113,113,0.35)] bg-[rgba(248,113,113,0.1)] px-2 py-0.5 text-[10px] uppercase tracking-wide text-[rgba(185,28,28,0.85)]">
                          Required
                        </span>
                      ) : (
                        <span className="rounded-full border border-[rgba(148,163,184,0.35)] bg-white px-2 py-0.5 text-[10px] uppercase tracking-wide text-[rgba(71,85,105,0.95)]">
                          Optional
                        </span>
                      )}
                    </div>
                    <p className="text-sm leading-relaxed text-[rgba(71,85,105,0.95)]">{module.description}</p>
                    <div className="flex flex-wrap gap-2 text-[10px] uppercase tracking-wide text-[rgba(100,116,139,0.95)]">
                      <span className="rounded-full border border-[rgba(148,163,184,0.35)] bg-white px-2 py-1">#{module.category}</span>
                      {module.allowMultiple && (
                        <span className="rounded-full border border-[rgba(59,130,246,0.25)] bg-[rgba(59,130,246,0.08)] px-2 py-1 text-[rgba(30,64,175,0.95)]">
                          Multi-instance
                        </span>
                      )}
                    </div>
                  </div>
                </div>

                <footer className="relative mt-3 flex items-center justify-between text-xs text-[rgba(99,102,106,0.9)]">
                  <button
                    type="button"
                    onClick={() => onQuickAdd(module.id)}
                    className="flex items-center gap-1 rounded-md border border-[rgba(59,130,246,0.25)] bg-white px-3 py-1.5 text-[rgba(15,23,42,0.9)] transition hover:border-[rgba(59,130,246,0.5)] hover:text-[rgba(30,64,175,0.95)]"
                  >
                    <Sparkles className="h-3.5 w-3.5" />
                    Add to canvas
                  </button>
                  <span className="text-[rgba(100,116,139,0.9)]">Drag to arrange</span>
                </footer>
              </article>
            );
          })}

          {filteredModules.length === 0 && (
            <div className="rounded-xl border border-dashed border-[var(--color-card-border)]/50 bg-[rgba(7,10,24,0.7)] px-4 py-9 text-center text-sm text-[var(--muted)]">
              No modules match “{query}”. Try searching by capability or category.
            </div>
          )}
        </div>
      </div>
    </section>
  );
}


