"use client";

import { MODULE_DEFINITION_MAP } from "@/lib/uat-modules";
import { cn } from "@/lib/utils";
import { ClipboardList, CheckCircle2, Wand2 } from "lucide-react";
import { CanvasModuleInstance, CanvasModuleStatus } from "./types";

type ModuleInspectorProps = {
  selectedModuleId: string | null;
  modules: CanvasModuleInstance[];
  onUpdateField: (instanceId: string, fieldKey: string, value: string) => void;
  onGenerateSample: (instanceId: string) => void;
  onChangeStatus: (instanceId: string, status: CanvasModuleStatus) => void;
  className?: string;
};

const STATUS_STYLES: Record<"ready" | "draft" | "needs-review", string> = {
  ready: "border-[rgba(32,158,98,0.45)] bg-[rgba(32,158,98,0.15)] text-[#1f8f5a]",
  draft: "border-[rgba(241,196,15,0.35)] bg-[rgba(241,196,15,0.18)] text-[#7a5b12]",
  "needs-review": "border-[rgba(210,93,93,0.4)] bg-[rgba(255,217,217,0.85)] text-[#a23434]",
};

const VALIDATION_CLASSES = {
  satisfied: "border-[rgba(32,158,98,0.35)] bg-[rgba(236,253,245,0.9)] text-[#1f8f5a]",
  pending: "border-[rgba(241,196,15,0.35)] bg-[rgba(255,247,219,0.9)] text-[#7a5b12]",
};

export function ModuleInspector({
  selectedModuleId,
  modules,
  onUpdateField,
  onGenerateSample,
  onChangeStatus,
  className,
}: ModuleInspectorProps) {
  const selectedModule = modules.find((module) => module.instanceId === selectedModuleId);

  if (!selectedModule) {
    return (
      <aside
        className={cn(
          "flex h-full flex-col rounded-3xl border border-[#1f2b40] bg-[#0b1424] p-6 text-sm text-[#7d8bb1] shadow-[0_20px_45px_rgba(5,12,28,0.35)]",
          className,
        )}
      >
        <div className="flex-1 rounded-2xl border border-dashed border-[#25324a] bg-[rgba(11,18,36,0.72)] px-6 py-16 text-center">
          <ClipboardList className="mx-auto h-10 w-10 text-[#7d8bb1]" />
          <p className="mt-4 text-base font-medium text-[#d8e4ff]">Module Inspector</p>
          <p className="mt-2 text-sm text-[#9ba8c8]">
            Select a module from the canvas to configure details, run compliance validation, and preview metadata.
          </p>
        </div>
      </aside>
    );
  }

  const definition = MODULE_DEFINITION_MAP[selectedModule.moduleId];
  const isReady = selectedModule.status === "ready";

  return (
    <aside
      className={cn(
        "flex h-full flex-col rounded-3xl border border-[#1f2b40] bg-[#0b1424] p-6 shadow-[0_25px_60px_rgba(5,12,28,0.4)]",
        className,
      )}
    >
      <header className="space-y-2 border-b border-[#24324c] pb-5">
        <div className="flex items-center justify-between gap-3">
          <div>
            <p className="text-[11px] uppercase tracking-[0.35em] text-[#7d8bb1]">Inspector</p>
            <h2 className="mt-1 text-xl font-semibold text-[#d8e4ff]">{definition.name}</h2>
          </div>
          <span className={cn("rounded-full border px-3 py-1 text-[11px] font-medium uppercase tracking-wide", STATUS_STYLES[isReady ? "ready" : "draft"]) }>
            {isReady ? "Ready for mint" : "Draft"}
          </span>
        </div>
        <p className="text-sm leading-relaxed text-[#9ba8c8]">{definition.description}</p>
        <div className="flex flex-wrap gap-2 text-[10px] uppercase tracking-wide text-[#7d8bb1]">
          <span className="rounded-full border border-[#24324c] bg-[rgba(11,18,36,0.78)] px-2 py-1">#{definition.category}</span>
          {definition.required ? (
            <span className="rounded-full border border-[rgba(210,93,93,0.45)] bg-[rgba(255,217,217,0.85)] px-2 py-1 text-[#a23434]">
              Required
            </span>
          ) : (
            <span className="rounded-full border border-[#24324c] bg-[rgba(11,18,36,0.78)] px-2 py-1">Optional</span>
          )}
          {definition.allowMultiple && (
            <span className="rounded-full border border-[#2d98ff]/45 bg-[rgba(45,152,255,0.12)] px-2 py-1 text-[#2d98ff]">
              Multi-instance
            </span>
          )}
        </div>
      </header>

      <div className="mt-6 space-y-6 overflow-y-auto pr-2">
        <section className="space-y-4 rounded-2xl border border-[#d7dfef] bg-[#f5f7fb] p-5">
          <div className="flex items-center justify-between gap-3">
            <h3 className="text-sm font-semibold uppercase tracking-[0.3em] text-[#52607c]">Configuration</h3>
            <div className="flex gap-2">
              <button
                type="button"
                onClick={() => onGenerateSample(selectedModule.instanceId)}
                className="flex items-center gap-1 rounded-md border border-[#2d98ff]/45 bg-white px-3 py-1.5 text-xs text-[#1e8fff] transition hover:border-[#2d98ff]/60 hover:bg-[#eef5ff]"
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
                    ? "border-[rgba(32,158,98,0.35)] bg-[rgba(32,158,98,0.15)] text-[#1f8f5a] hover:border-[rgba(32,158,98,0.5)]"
                    : "border-[rgba(32,158,98,0.3)] bg-white text-[#1f8f5a] hover:border-[rgba(32,158,98,0.45)]"
                )}
              >
                <CheckCircle2 className="h-3.5 w-3.5" />
                {isReady ? "Mark draft" : "Mark ready"}
              </button>
            </div>
          </div>

          <form className="space-y-4">
            {definition.fields.map((field) => {
              const value = selectedModule.values[field.key] ?? "";
              const label = field.label;

              const labelRow = (
                <div className="flex items-center justify-between gap-2">
                  <span className="font-medium text-[#1f2a45]">{label}</span>
                  {field.required && (
                    <span className="rounded-full border border-[rgba(210,93,93,0.4)] bg-[rgba(255,217,217,0.8)] px-2 py-0.5 text-[10px] uppercase tracking-wide text-[#a23434]">
                      Required
                    </span>
                  )}
                </div>
              );

              if (field.type === "textarea") {
                return (
                  <label key={field.key} className="flex flex-col gap-2 text-sm text-[#5a6785]">
                    {labelRow}
                    <textarea
                      value={value}
                      placeholder={field.placeholder}
                      onChange={(event) => onUpdateField(selectedModule.instanceId, field.key, event.target.value)}
                      className="min-h-[112px] rounded-xl border border-[#d7dfef] bg-white px-3 py-2 text-sm text-[#1f2a45] outline-none transition focus:border-[#2d98ff]/60 focus:ring-2 focus:ring-[#2d98ff]/30"
                    />
                    {field.helper && <span className="text-xs text-[#7d8bb1]">{field.helper}</span>}
                  </label>
                );
              }

              if (field.type === "select" && field.options) {
                return (
                  <label key={field.key} className="flex flex-col gap-2 text-sm text-[#5a6785]">
                    {labelRow}
                    <div className="relative">
                      <select
                        value={value}
                        onChange={(event) => onUpdateField(selectedModule.instanceId, field.key, event.target.value)}
                        className="w-full appearance-none rounded-xl border border-[#d7dfef] bg-white px-3 py-2 text-sm text-[#1f2a45] outline-none transition focus:border-[#2d98ff]/60 focus:ring-2 focus:ring-[#2d98ff]/30"
                      >
                        <option value="">{field.placeholder ?? "Select option"}</option>
                        {field.options.map((option) => (
                          <option key={option.value} value={option.value}>
                            {option.label}
                          </option>
                        ))}
                      </select>
                      <div className="pointer-events-none absolute inset-y-0 right-3 flex items-center">
                        <div className="h-2 w-2 rotate-45 border-b border-r border-[#7d8bb1]" />
                      </div>
                    </div>
                    {field.helper && <span className="text-xs text-[#7d8bb1]">{field.helper}</span>}
                  </label>
                );
              }

              return (
                <label key={field.key} className="flex flex-col gap-2 text-sm text-[#5a6785]">
                  {labelRow}
                  <input
                    type={field.type === "number" || field.type === "percentage" ? "number" : "text"}
                    value={value}
                    placeholder={field.placeholder}
                    onChange={(event) => onUpdateField(selectedModule.instanceId, field.key, event.target.value)}
                    className="rounded-xl border border-[#d7dfef] bg-white px-3 py-2 text-sm text-[#1f2a45] outline-none transition focus:border-[#2d98ff]/60 focus:ring-2 focus:ring-[#2d98ff]/30"
                  />
                  {field.helper && <span className="text-xs text-[#7d8bb1]">{field.helper}</span>}
                </label>
              );
            })}
          </form>
        </section>

        <section className="space-y-3 rounded-2xl border border-[#d7dfef] bg-[#f5f7fb] p-5">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-semibold uppercase tracking-[0.3em] text-[#52607c]">Validation</h3>
            <span className="rounded-full border border-[#d7dfef] bg-white px-2 py-1 text-[10px] uppercase tracking-wide text-[#647491]">
              Live
            </span>
          </div>
          <ul className="space-y-3 text-sm text-[#5a6785]">
            {definition.fields.map((field) => {
              const value = selectedModule.values[field.key];
              const isSatisfied = field.required ? Boolean(value?.toString().trim()) : true;
              const validationClass = isSatisfied ? VALIDATION_CLASSES.satisfied : VALIDATION_CLASSES.pending;

              return (
                <li key={field.key} className={cn("flex items-center justify-between rounded-lg border px-3 py-2", validationClass)}>
                  <span>{field.label}</span>
                  <CheckCircle2 className={cn("h-4 w-4", isSatisfied ? "text-[#1f8f5a]" : "opacity-50") } />
                </li>
              );
            })}
          </ul>
        </section>
      </div>
    </aside>
  );
}


