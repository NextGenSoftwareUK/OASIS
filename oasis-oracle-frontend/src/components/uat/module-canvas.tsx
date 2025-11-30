"use client";

import { MODULE_DEFINITION_MAP, ModuleDefinition } from "@/lib/uat-modules";
import { cn } from "@/lib/utils";
import { GripVertical, Trash2, CheckCircle2, AlertTriangle } from "lucide-react";
import { useState } from "react";
import { CanvasModuleInstance } from "./types";

type ModuleCanvasProps = {
  modules: CanvasModuleInstance[];
  selectedModuleId: string | null;
  onSelectModule: (instanceId: string) => void;
  onRemoveModule: (instanceId: string) => void;
  onDropModule: (moduleId: string) => void;
  requiredMissing: ModuleDefinition["id"][];
};

export function ModuleCanvas({
  modules,
  selectedModuleId,
  onSelectModule,
  onRemoveModule,
  onDropModule,
  requiredMissing,
}: ModuleCanvasProps) {
  const [isDragOver, setIsDragOver] = useState(false);

  return (
    <section
      onDragOver={(event) => {
        if (event.dataTransfer.types.includes("application/uat-module")) {
          event.preventDefault();
          event.dataTransfer.dropEffect = "copy";
          if (!isDragOver) setIsDragOver(true);
        }
      }}
      onDragLeave={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget as Node)) {
          setIsDragOver(false);
        }
      }}
      onDrop={(event) => {
        const moduleId = event.dataTransfer.getData("application/uat-module");
        if (moduleId) {
          onDropModule(moduleId);
        }
        setIsDragOver(false);
      }}
      className={cn(
        "serious-panel relative flex h-full min-h-[720px] flex-col rounded-3xl p-6 transition",
        isDragOver &&
          "border-[rgba(59,130,246,0.45)] bg-[rgba(6,12,28,0.92)] shadow-[0px_30px_70px_rgba(34,211,238,0.18)]"
      )}
    >
      <div className="absolute inset-x-6 top-6 flex items-center justify-between">
        <div>
          <p className="text-sm uppercase tracking-[0.35em] text-[var(--muted)]">Workspace</p>
          <h2 className="mt-1 text-2xl font-semibold text-[var(--color-foreground)]">UAT Assembly Canvas</h2>
        </div>
        <span className="rounded-full border border-[var(--color-card-border)]/50 bg-[rgba(7,12,24,0.85)] px-3 py-1 text-xs text-[var(--muted)]">
          {modules.length} module{modules.length === 1 ? "" : "s"}
        </span>
      </div>

      <div className="pointer-events-none absolute inset-x-12 top-28 h-32 rounded-full bg-[radial-gradient(circle,rgba(34,211,238,0.25),transparent_75%)] blur-3xl" />

      <div className="mt-28 flex-1 space-y-5 overflow-y-auto pr-2">
        {modules.length === 0 && (
          <div className="mt-12 flex flex-col items-center justify-center gap-4 rounded-2xl border border-dashed border-[var(--color-card-border)]/60 bg-[rgba(6,10,24,0.75)] px-8 py-20 text-center">
            <div className="rounded-full border border-[var(--accent)]/50 bg-[rgba(34,211,238,0.08)] px-4 py-2 text-xs uppercase tracking-[0.35em] text-[var(--accent)]">
              Drag modules here
            </div>
            <p className="max-w-xl text-base text-[var(--muted)]">
              Start assembling your mint payload by dragging required modules from the palette or using quick add.
              Required modules must be completed before initiating the mint review flow.
            </p>
          </div>
        )}

        {requiredMissing.length > 0 && (
          <div className="rounded-xl border border-[rgba(248,113,113,0.35)] bg-[rgba(254,226,226,0.9)] px-4 py-3 text-sm text-[#9f1239]">
            <div className="flex items-center gap-2 font-medium">
              <AlertTriangle className="h-4 w-4" />
              Missing required modules
            </div>
            <p className="mt-1 text-[rgba(136,19,55,0.9)]">
              Add the following modules to unlock compliance validation: {requiredMissing.join(", ")}.
            </p>
          </div>
        )}

        <ol className="space-y-4">
          {modules.map((moduleInstance, index) => {
            const definition = MODULE_DEFINITION_MAP[moduleInstance.moduleId];
            const isSelected = selectedModuleId === moduleInstance.instanceId;

            const statusDisplay = {
              draft: {
                label: "Draft",
                className:
                  "border-[rgba(253,186,116,0.6)] bg-[rgba(255,248,237,0.95)] text-[#b45309]",
              },
              ready: {
                label: "Ready",
                className:
                  "border-[rgba(74,222,128,0.55)] bg-[rgba(240,253,244,0.95)] text-[#15803d]",
              },
              "needs-review": {
                label: "Needs Review",
                className:
                  "border-[rgba(248,113,113,0.5)] bg-[rgba(254,226,226,0.95)] text-[#b91c1c]",
              },
            }[moduleInstance.status];

            return (
              <li
                key={moduleInstance.instanceId}
                onClick={() => onSelectModule(moduleInstance.instanceId)}
                className={cn(
                  "ticket-card group relative cursor-pointer overflow-hidden rounded-2xl border border-transparent p-5 transition hover:border-[rgba(59,130,246,0.45)] hover:shadow-[0px_25px_45px_rgba(15,23,42,0.18)]",
                  isSelected &&
                    "border-[rgba(59,130,246,0.8)] shadow-[0px_12px_35px_rgba(37,99,235,0.25)]"
                )}
              >
                <div className="absolute inset-0 opacity-0 transition duration-200 group-hover:opacity-100">
                  <div className="h-full w-full bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.16),transparent_75%)]" />
                </div>
                <div className="relative flex items-start gap-4">
                  <div className="mt-1 flex h-10 w-10 items-center justify-center rounded-lg border border-[var(--accent-soft)] bg-[rgba(10,18,34,0.92)] text-[var(--accent)] shadow-[0_0_16px_rgba(34,211,238,0.25)]">
                    <definition.icon className="h-5 w-5" />
                  </div>
                  <div className="flex-1 space-y-3">
                    <div className="flex items-center justify-between gap-3">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 text-[rgba(100,116,139,0.9)]">
                          <span className="text-xs uppercase tracking-[0.45em]">
                            {String(index + 1).padStart(2, "0")}
                          </span>
                          <h3 className="text-lg font-semibold text-[#0f172a]">
                            {definition.name}
                          </h3>
                        </div>
                        <p className="mt-1 text-sm text-[rgba(71,85,105,0.95)]">
                          {definition.description}
                        </p>
                      </div>
                      <div className="flex items-center gap-2">
                        <span
                          className={cn(
                            "rounded-full border px-2.5 py-1 text-[11px] font-medium uppercase tracking-wide",
                            statusDisplay.className
                          )}
                        >
                          {statusDisplay.label}
                        </span>
                        <button
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation();
                            onRemoveModule(moduleInstance.instanceId);
                          }}
                          className="rounded-full border border-transparent bg-[rgba(12,18,32,0.9)] p-2 text-[var(--muted)] transition hover:border-[rgba(248,113,113,0.35)] hover:text-[rgba(248,113,113,0.95)]"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </div>

                    <dl className="grid grid-cols-2 gap-3 text-xs text-[rgba(100,116,139,0.95)] md:grid-cols-3">
                      {definition.headlineFields.map((fieldKey) => {
                        const value = moduleInstance.values[fieldKey];
                        const fieldLabel =
                          definition.fields.find((field) => field.key === fieldKey)?.label ?? fieldKey;

                        if (!value) return null;

                        return (
                          <div
                            key={fieldKey}
                            className="rounded-lg border border-[rgba(148,163,184,0.3)] bg-[rgba(241,245,255,0.95)] px-3 py-2"
                          >
                            <dt className="text-[10px] uppercase tracking-wide text-[rgba(100,116,139,0.9)]">
                              {fieldLabel}
                            </dt>
                            <dd className="mt-1 text-sm text-[#0f172a]">{value}</dd>
                          </div>
                        );
                      })}
                    </dl>
                  </div>
                </div>

                {moduleInstance.status === "ready" && (
                  <div className="absolute right-4 top-4 flex items-center gap-1 rounded-full border border-[rgba(74,222,128,0.45)] bg-[rgba(240,253,244,0.95)] px-3 py-1 text-[11px] uppercase tracking-wide text-[#15803d]">
                    <CheckCircle2 className="h-3.5 w-3.5" />
                    Validated
                  </div>
                )}

                <div className="absolute left-4 top-4 text-[rgba(148,163,184,0.8)]">
                  <GripVertical className="h-4 w-4 opacity-0 transition group-hover:opacity-60" />
                </div>
              </li>
            );
          })}
        </ol>
      </div>
    </section>
  );
}


