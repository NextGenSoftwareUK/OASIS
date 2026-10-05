"use client";

import { ModuleDefinition } from "@/lib/uat-modules";
import { cn } from "@/lib/utils";
import { Search, Sparkles } from "lucide-react";
import { useMemo, useRef, useState } from "react";
import type { WheelEvent } from "react";

type ModulePaletteProps = {
  title: string;
  subtitle: string;
  modules: ModuleDefinition[];
  badgeLabel: string;
  accent: "required" | "optional";
  onQuickAdd: (moduleId: string) => void;
  className?: string;
};

export function ModulePalette({
  title,
  subtitle,
  modules,
  badgeLabel,
  accent,
  onQuickAdd,
  className,
}: ModulePaletteProps) {
  const [query, setQuery] = useState("");
  const scrollAreaRef = useRef<HTMLDivElement>(null);

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

  const handleWheel = (event: WheelEvent) => {
    const container = scrollAreaRef.current;
    if (!container) {
      return;
    }

    const previous = container.scrollTop;
    container.scrollTop += event.deltaY;

    if (container.scrollTop !== previous) {
      event.preventDefault();
    }
  };

  const accentClass =
    accent === "required"
      ? "border-[var(--oasis-accent)]/60 bg-[rgba(34,211,238,0.12)] text-[var(--oasis-accent)]"
      : "border-[rgba(148,163,184,0.35)] bg-[rgba(255,255,255,0.08)] text-[var(--oasis-foreground)]";

  return (
    <section
      onWheelCapture={handleWheel}
      className={cn(
        "flex h-full min-h-0 w-full flex-col overflow-hidden rounded-3xl border border-[var(--oasis-card-border)]/60 bg-[rgba(6,10,24,0.88)] px-5 py-6 backdrop-blur-xl shadow-[0_18px_45px_rgba(3,12,30,0.35)]",
        className
      )}
    >
      <header className="sticky top-0 z-20 border-b border-[var(--oasis-card-border)]/40 bg-[rgba(6,10,24,0.95)] pb-4 backdrop-blur-xl">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-[11px] uppercase tracking-[0.35em] text-[var(--oasis-muted)]">{subtitle}</p>
            <h2 className="mt-1 text-xl font-semibold text-[var(--oasis-foreground)]">{title}</h2>
          </div>
          <span
            className={cn(
              "rounded-full border px-3 py-1 text-xs font-medium uppercase tracking-wide",
              accentClass
            )}
          >
            {badgeLabel}
          </span>
        </div>
        <label className="mt-4 flex items-center gap-2 rounded-xl border border-[var(--oasis-card-border)]/50 bg-[rgba(5,9,20,0.92)] px-3 py-2 text-sm text-[var(--oasis-muted)] shadow-inner shadow-[rgba(34,211,238,0.12)]">
          <Search className="h-4 w-4 shrink-0 text-[var(--oasis-accent)]" />
          <input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder="Search modules, compliance domains, revenue flows…"
            className="w-full bg-transparent text-sm text-[var(--oasis-foreground)] outline-none placeholder:text-[rgba(148,163,184,0.45)]"
          />
        </label>
      </header>

      <div
        ref={scrollAreaRef}
        className="mt-4 flex-1 min-h-0 overflow-y-auto overscroll-contain pr-1 touch-pan-y [scrollbar-width:thin] [scrollbar-color:rgba(148,163,184,0.35)_rgba(9,15,30,0.4)]"
      >
        <div className="space-y-4 pb-4">
          {filteredModules.map((module) => {
            const Icon = module.icon;
            return (
              <button
                key={module.id}
                type="button"
                draggable
                onDragStart={(event) => {
                  event.dataTransfer.setData("application/uat-module", module.id);
                  event.dataTransfer.effectAllowed = "copy";
                }}
                onClick={() => onQuickAdd(module.id)}
                className="group relative w-full cursor-grab select-none overflow-hidden rounded-2xl border border-[#d7dfef] bg-[#f4f7fb] p-4 text-left transition duration-150 hover:-translate-y-1 hover:border-[#2d98ff]/60 hover:shadow-[0_20px_45px_rgba(12,24,48,0.16)] active:cursor-grabbing before:absolute before:left-0 before:top-1/2 before:h-8 before:w-8 before:-translate-x-1/2 before:-translate-y-1/2 before:rounded-full before:border before:border-[#d7dfef] before:bg-[#0b1424] before:transition before:duration-200 before:content-[''] before:pointer-events-none after:absolute after:right-0 after:top-1/2 after:h-8 after:w-8 after:translate-x-1/2 after:-translate-y-1/2 after:rounded-full after:border after:border-[#d7dfef] after:bg-[#0b1424] after:transition after:duration-200 after:content-[''] after:pointer-events-none"
              >
                <div className="relative flex items-start gap-4">
                  <div className="flex h-12 w-12 shrink-0 items-center justify-center rounded-xl border border-[#d7dfef] bg-white text-[#2d98ff] shadow-[0_12px_22px_rgba(31,74,135,0.12)]">
                    <Icon className="h-5 w-5" />
                  </div>
                  <div className="space-y-3">
                    <h3 className="text-lg font-semibold text-[#1f2a45]">{module.name}</h3>
                    <p className="text-sm leading-relaxed text-[#52607c]">{module.description}</p>
                    <div className="flex flex-wrap items-center gap-2 text-[10px] uppercase tracking-wide text-[#647491]">
                      <span className="rounded-full border border-[#d7dfef] bg-white px-2 py-1">
                        #{module.category}
                      </span>
                      <span
                        className={cn(
                          "rounded-full border px-2 py-1",
                          module.required
                            ? "border-[rgba(210,93,93,0.35)] bg-[rgba(252,217,217,0.85)] text-[#9c3232]"
                            : "border-[#d7dfef] bg-white text-[#52607c]"
                        )}
                      >
                        {module.required ? "Required" : "Optional"}
                      </span>
                      {module.allowMultiple && (
                        <span className="rounded-full border border-[#2d98ff]/40 bg-[rgba(45,152,255,0.12)] px-2 py-1 text-[#1f8de5]">
                          Multi-instance
                        </span>
                      )}
                    </div>
                    <div className="hidden items-center gap-2 text-xs text-[#52607c] group-hover:flex">
                      <Sparkles className="h-4 w-4 text-[#2d98ff]" />
                      <span>Drag into canvas or click to add</span>
                    </div>
                  </div>
                </div>
              </button>
            );
          })}

          {filteredModules.length === 0 && (
            <div className="rounded-xl border border-dashed border-[#24324c] bg-[rgba(11,18,36,0.72)] px-4 py-9 text-center text-sm text-[#7d8bb1]">
              No modules match “{query}”. Try searching by capability or category.
            </div>
          )}
        </div>
      </div>
    </section>
  );
}


