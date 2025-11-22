"use client";

import { MODULE_DEFINITION_MAP } from "@/lib/uat-modules";
import { cn } from "@/lib/utils";
import { Wand2, ClipboardList, CheckCircle2 } from "lucide-react";
import { CanvasModuleInstance, CanvasModuleStatus } from "./types";

type ModuleInspectorProps = {
  selectedModuleId: string | null;
  modules: CanvasModuleInstance[];
  onUpdateField: (instanceId: string, fieldKey: string, value: string) => void;
  onGenerateSample: (instanceId: string) => void;
  onChangeStatus: (instanceId: string, status: CanvasModuleStatus) => void;
};

export function ModuleInspector({
  selectedModuleId,
  modules,
  onUpdateField,
  onGenerateSample,
  onChangeStatus,
}: ModuleInspectorProps) {
  const selectedModule = modules.find((module) => module.instanceId === selectedModuleId);

  if (!selectedModule) {
    return (
      <aside className="serious-panel flex h-full flex-col rounded-3xl p-6 text-sm text-[var(--muted)]">
        <div className="flex-1 rounded-2xl border border-dashed border-[var(--color-card-border)]/40 bg-[rgba(9,14,30,0.7)] px-6 py-16 text-center">
          <ClipboardList className="mx-auto h-10 w-10 text-[var(--muted)]/50" />
          <p className="mt-4 text-base font-medium text-[var(--color-foreground)]">Module Inspector</p>
          <p className="mt-2 text-sm text-[var(--muted)]">
            Select a module from the canvas to configure details, run compliance validation, and preview metadata.
          </p>
        </div>
      </aside>
    );
  }

  const definition = MODULE_DEFINITION_MAP[selectedModule.moduleId];
  const isReady = selectedModule.status === "ready";

  return (
    <aside className="serious-panel flex h-full flex-col rounded-3xl p-6">
      <header className="space-y-2 border-b border-[var(--color-card-border)]/40 pb-5">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-xs uppercase tracking-[0.35em] text-[var(--muted)]">Inspector</p>
            <h2 className="mt-1 text-xl font-semibold text-[var(--color-foreground)]">{definition.name}</h2>
          </div>
          <span
            className={cn(
              "rounded-full border px-3 py-1 text-[11px] font-medium uppercase tracking-wide",
              isReady
                ? "border-[rgba(74,222,128,0.35)] bg-[rgba(21,128,61,0.25)] text-[rgba(187,247,208,0.95)]"
                : "border-[rgba(253,224,71,0.35)] bg-[rgba(180,83,9,0.25)] text-[rgba(253,224,71,0.95)]"
            )}
          >
            {isReady ? "Ready for mint" : "Draft"}
          </span>
        </div>
        <p className="text-sm leading-relaxed text-[var(--muted)]">{definition.description}</p>
        <div className="flex flex-wrap gap-2 text-[10px] uppercase tracking-wide text-[var(--muted)]/75">
          <span className="rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(9,15,28,0.8)] px-2 py-1">
            #{definition.category}
          </span>
          {definition.required ? (
            <span className="rounded-full border border-[rgba(248,113,113,0.3)] bg-[rgba(127,29,29,0.35)] px-2 py-1 text-[rgba(252,165,165,0.95)]">
              Required
            </span>
          ) : (
            <span className="rounded-full border border-[var(--color-card-border)]/50 bg-[rgba(9,15,28,0.8)] px-2 py-1">
              Optional
            </span>
          )}
          {definition.allowMultiple && (
            <span className="rounded-full border border-[var(--accent-soft)]/60 bg-[rgba(34,211,238,0.1)] px-2 py-1 text-[var(--accent)]">
              Multi-instance
            </span>
          )}
        </div>
      </header>

      <div className="mt-6 space-y-6 overflow-y-auto pr-2">
        <section className="space-y-4 rounded-2xl border border-[rgba(148,163,184,0.25)] bg-white p-5 shadow-[0_12px_30px_rgba(15,23,42,0.12)]">
          <div className="flex items-center justify-between gap-3">
            <h3 className="text-sm font-semibold uppercase tracking-[0.3em] text-[rgba(71,85,105,0.9)]">
              Configuration
            </h3>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => onGenerateSample(selectedModule.instanceId)}
                className="flex items-center gap-1 rounded-md border border-[rgba(59,130,246,0.3)] bg-[rgba(59,130,246,0.08)] px-3 py-1.5 text-xs text-[rgba(37,99,235,0.95)] transition hover:border-[rgba(59,130,246,0.5)]"
              >
                <Wand2 className="h-3.5 w-3.5" />
                Sample data
              </button>
              <button
                type="button"
                onClick={() => onChangeStatus(selectedModule.instanceId, isReady ? "draft" : "ready")}
                className={cn(
                  "flex items-center gap-1 rounded-md border px-3 py-1.5 text-xs transition",
                  isReady
                    ? "border-[rgba(74,222,128,0.45)] bg-[rgba(240,253,244,0.95)] text-[#15803d]"
                    : "border-[rgba(74,222,128,0.35)] bg-[rgba(236,253,245,1)] text-[#15803d]"
                )}
              >
                <CheckCircle2 className="h-3.5 w-3.5" />
                {isReady ? "Mark draft" : "Mark ready"}
              </button>
            </div>
          </div>

          <form className="space-y-4 text-[rgba(71,85,105,0.95)]">
            {definition.fields.map((field) => {
              const value = selectedModule.values[field.key] ?? "";
              const label = field.label;

              if (field.type === "textarea") {
                return (
                  <label key={field.key} className="flex flex-col gap-2 text-sm">
                    <div className="flex items-center justify-between gap-2 text-[#0f172a]">
                      <span className="font-medium">{label}</span>
                      {field.required && (
                        <span className="rounded-full border border-[rgba(248,113,113,0.35)] px-2 py-0.5 text-[10px] uppercase tracking-wide text-[rgba(185,28,28,0.85)]">
                          Required
                        </span>
                      )}
                    </div>
                    <textarea
                      value={value}
                      placeholder={field.placeholder}
                      onChange={(event) => onUpdateField(selectedModule.instanceId, field.key, event.target.value)}
                      className="min-h-[112px] rounded-xl border border-[rgba(148,163,184,0.35)] bg-white px-3 py-2 text-sm text-[#0f172a] outline-none transition focus:border-[rgba(59,130,246,0.65)] focus:ring-2 focus:ring-[rgba(59,130,246,0.2)]"
                    />
                    {field.helper && <span className="text-xs text-[rgba(100,116,139,0.85)]">{field.helper}</span>}
                  </label>
                );
              }

              if (field.type === "select" && field.options) {
                return (
                  <label key={field.key} className="flex flex-col gap-2 text-sm">
                    <div className="flex items-center justify-between gap-2 text-[#0f172a]">
                      <span className="font-medium">{label}</span>
                      {field.required && (
                        <span className="rounded-full border border-[rgba(248,113,113,0.35)] px-2 py-0.5 text-[10px] uppercase tracking-wide text-[rgba(185,28,28,0.85)]">
                          Required
                        </span>
                      )}
                    </div>
                    <div className="relative">
                      <select
                        value={value}
                        onChange={(event) =>
                          onUpdateField(selectedModule.instanceId, field.key, event.target.value)
                        }
                        className="w-full appearance-none rounded-xl border border-[rgba(148,163,184,0.35)] bg-white px-3 py-2 text-sm text-[#0f172a] outline-none transition focus:border-[rgba(59,130,246,0.65)] focus:ring-2 focus:ring-[rgba(59,130,246,0.2)]"
                      >
                        <option value="">{field.placeholder ?? "Select option"}</option>
                        {field.options.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center">
                        <div className="h-2 w-2 rotate-45 border-b border-r border-[rgba(148,163,184,0.6)]" />
                      </div>
                    </div>
                    {field.helper && <span className="text-xs text-[rgba(100,116,139,0.85)]">{field.helper}</span>}
                  </label>
                );
              }

              return (
                <label key={field.key} className="flex flex-col gap-2 text-sm">
                  <div className="flex items-center justify-between gap-2 text-[#0f172a]">
                    <span className="font-medium">{label}</span>
                    {field.required && (
                      <span className="rounded-full border border-[rgba(248,113,113,0.35)] px-2 py-0.5 text-[10px] uppercase tracking-wide text-[rgba(185,28,28,0.85)]">
                        Required
                      </span>
                    )}
                  </div>
                  <input
                    type={field.type === "number" || field.type === "percentage" ? "number" : "text"}
                    value={value}
                    placeholder={field.placeholder}
                    onChange={(event) => onUpdateField(selectedModule.instanceId, field.key, event.target.value)}
                    className="rounded-xl border border-[rgba(148,163,184,0.35)] bg-white px-3 py-2 text-sm text-[#0f172a] outline-none transition focus:border-[rgba(59,130,246,0.65)] focus:ring-2 focus:ring-[rgba(59,130,246,0.2)]"
                  />
                  {field.helper && <span className="text-xs text-[rgba(100,116,139,0.85)]">{field.helper}</span>}
                </label>
              );
            })}
          </form>
        </section>

        <section className="space-y-3 rounded-2xl border border-[rgba(148,163,184,0.25)] bg-white p-5 shadow-[0_12px_30px_rgba(15,23,42,0.12)]">
          <div className="flex items-center justify-between text-[rgba(71,85,105,0.95)]">
            <h3 className="text-sm font-semibold uppercase tracking-[0.3em]">Validation</h3>
            <span className="rounded-full border border-[rgba(148,163,184,0.35)] bg-white px-2 py-1 text-[10px] uppercase tracking-wide">
              Live
            </span>
          </div>
          <ul className="space-y-3 text-sm text-[rgba(71,85,105,0.95)]">
            {definition.fields.map((field) => {
              const value = selectedModule.values[field.key];
              const isSatisfied = field.required ? Boolean(value?.toString().trim()) : true;

              return (
                <li
                  key={field.key}
                  className={cn(
                    "flex items-center justify-between rounded-lg border px-3 py-2",
                    isSatisfied
                      ? "border-[rgba(74,222,128,0.45)] bg-[rgba(240,253,244,0.95)] text-[#166534]"
                      : "border-[rgba(251,191,36,0.45)] bg-[rgba(255,251,235,0.95)] text-[#92400e]"
                  )}
                >
                  <span>{field.label}</span>
                  <CheckCircle2 className={cn("h-4 w-4", isSatisfied ? "text-[#16a34a]" : "opacity-40")} />
                </li>
              );
            })}
          </ul>
        </section>
      </div>
    </aside>
  );
}


