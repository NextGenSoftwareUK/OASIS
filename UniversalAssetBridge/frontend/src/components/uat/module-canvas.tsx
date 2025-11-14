"use client";

import { MODULE_DEFINITION_MAP, ModuleDefinition } from "@/lib/uat-modules";
import { cn } from "@/lib/utils";
import { AlertTriangle, CheckCircle2, GripVertical, Trash2 } from "lucide-react";
import { useState } from "react";
import { CanvasModuleInstance } from "./types";

type ModuleCanvasProps = {
  modules: CanvasModuleInstance[];
  selectedModuleId: string | null;
  onSelectModule: (instanceId: string) => void;
  onRemoveModule: (instanceId: string) => void;
  onDropModule: (moduleId: string) => void;
  requiredMissing: ModuleDefinition["name"][];
  className?: string;
};

export function ModuleCanvas({
  modules,
  selectedModuleId,
  onSelectModule,
  onRemoveModule,
  onDropModule,
  requiredMissing,
  className,
}: ModuleCanvasProps) {
  const [isDragOver, setIsDragOver] = useState(false);

  return (
    <section
      style={{
        backgroundImage:
          "linear-gradient(rgba(11,18,36,0.94), rgba(11,18,36,0.94)), linear-gradient(90deg, rgba(255,255,255,0.04) 1px, transparent 1px), linear-gradient(180deg, rgba(255,255,255,0.04) 1px, transparent 1px)",
        backgroundSize: "auto, 48px 48px, 48px 48px",
        backgroundPosition: "0 0, center, center",
      }}
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
        className,
        "relative flex h-full min-h-[720px] flex-col rounded-3xl border border-[#1f2b40] px-6 py-7 shadow-[0_25px_60px_rgba(5,12,30,0.45)] transition",
        isDragOver && "border-[#2d98ff]/80 shadow-[0px_32px_80px_rgba(34,149,255,0.22)]"
      )}
    >
      <div className="absolute inset-x-6 top-6 flex items-center justify-between">
        <div>
          <p className="text-[11px] uppercase tracking-[0.35em] text-[#7d8bb1]">Workspace</p>
          <h2 className="mt-1 text-2xl font-semibold text-[#d8e4ff]">UAT Assembly Canvas</h2>
        </div>
        <span className="rounded-full border border-[#25324a] bg-[rgba(11,18,36,0.8)] px-3 py-1 text-xs text-[#7d8bb1]">
          {modules.length} module{modules.length === 1 ? "" : "s"}
        </span>
      </div>

      <div className="mt-28 flex-1 space-y-5 overflow-y-auto pr-2">
        {modules.length === 0 && (
          <div className="mt-12 flex flex-col items-center justify-center gap-4 rounded-2xl border border-dashed border-[#25324a] bg-[rgba(8,14,26,0.78)] px-8 py-20 text-center text-[#7d8bb1]">
            <div className="rounded-full border border-[#2d98ff]/45 bg-[rgba(45,152,255,0.12)] px-4 py-2 text-xs uppercase tracking-[0.35em] text-[#2d98ff]">
              Drag modules here
            </div>
            <p className="max-w-xl text-base text-[#9ba8c8]">
              Start assembling your mint payload by dragging required modules from the palette or using quick add.
              Required modules must be completed before initiating the mint review flow.
            </p>
          </div>
        )}

        {requiredMissing.length > 0 && (
          <div className="rounded-xl border border-[rgba(210,93,93,0.45)] bg-[rgba(255,217,217,0.85)] px-4 py-3 text-sm text-[#a23434] shadow-[0_18px_45px_rgba(120,32,32,0.18)]">
            <div className="flex items-center gap-2 font-medium">
              <AlertTriangle className="h-4 w-4 text-[#a23434]" />
              Missing required modules
            </div>
            <p className="mt-1 text-[rgba(162,52,52,0.85)]">
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
                  "border-[rgba(241,196,15,0.3)] bg-[rgba(251,232,166,0.6)] text-[#7a5b12]",
              },
              ready: {
                label: "Ready",
                className:
                  "border-[rgba(32,158,98,0.35)] bg-[rgba(32,158,98,0.15)] text-[#1f8f5a]",
              },
              "needs-review": {
                label: "Needs Review",
                className:
                  "border-[rgba(210,93,93,0.4)] bg-[rgba(255,217,217,0.8)] text-[#a23434]",
              },
            }[moduleInstance.status];

            return (
              <li
                key={moduleInstance.instanceId}
                onClick={() => onSelectModule(moduleInstance.instanceId)}
                className={cn(
                  "group relative cursor-pointer overflow-hidden rounded-2xl border border-[#d7dfef] bg-[#f5f7fb] p-5 transition hover:border-[#2d98ff]/60 hover:shadow-[0_22px_55px_rgba(12,24,48,0.18)] before:absolute before:left-0 before:top-1/2 before:h-8 before:w-8 before:-translate-x-1/2 before:-translate-y-1/2 before:rounded-full before:border before:border-[#d7dfef] before:bg-[#0b1424] before:transition before:duration-200 before:content-[''] before:pointer-events-none after:absolute after:right-0 after:top-1/2 after:h-8 after:w-8 after:translate-x-1/2 after:-translate-y-1/2 after:rounded-full after:border after:border-[#d7dfef] after:bg-[#0b1424] after:transition after:duration-200 after:content-[''] after:pointer-events-none",
                  isSelected && "border-[#2d98ff]/80 shadow-[0_0_35px_rgba(45,152,255,0.24)] before:border-[#2d98ff]/70 after:border-[#2d98ff]/70"
                )}
              >
                <div className="relative flex items-start gap-4">
                  <div className="mt-1 flex h-12 w-12 items-center justify-center rounded-xl border border-[#d7dfef] bg-white text-[#2d98ff] shadow-[0_12px_22px_rgba(31,74,135,0.12)]">
                    <definition.icon className="h-6 w-6" />
                  </div>
                  <div className="flex-1 space-y-3">
                    <div className="flex items-center justify-between gap-3">
                      <div>
                        <div className="flex items-center gap-3">
                          <span className="text-xs uppercase tracking-[0.35em] text-[#7d8bb1]">
                            {String(index + 1).padStart(2, "0")}
                          </span>
                          <h3 className="text-lg font-semibold text-[#1f2a45]">
                            {definition.name}
                          </h3>
                        </div>
                        <p className="mt-1 text-sm text-[#52607c]">{definition.description}</p>
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
                          className="rounded-full border border-[#d7dfef] bg-white p-2 text-[#647491] transition hover:border-[rgba(210,93,93,0.4)] hover:text-[#a23434]"
                        >
                          <Trash2 className="h-4 w-4" />
                        </button>
                      </div>
                    </div>

                    <dl className="grid grid-cols-2 gap-3 text-xs text-[#647491] md:grid-cols-3">
                      {definition.headlineFields.map((fieldKey) => {
                        const value = moduleInstance.values[fieldKey];
                        const fieldLabel =
                          definition.fields.find((field) => field.key === fieldKey)?.label ?? fieldKey;

                        if (!value) return null;

                        return (
                          <div
                            key={fieldKey}
                            className="rounded-lg border border-[#d7dfef] bg-white px-3 py-2"
                          >
                            <dt className="text-[10px] uppercase tracking-wide text-[#8e9bb8]">
                              {fieldLabel}
                            </dt>
                            <dd className="mt-1 text-sm text-[#1f2a45]">{value}</dd>
                          </div>
                        );
                      })}
                    </dl>
                  </div>
                </div>

                {moduleInstance.status === "ready" && (
                  <div className="absolute right-4 top-4 flex items-center gap-1 rounded-full border border-[rgba(32,158,98,0.45)] bg-[rgba(32,158,98,0.15)] px-3 py-1 text-[11px] uppercase tracking-wide text-[#1f8f5a]">
                    <CheckCircle2 className="h-3.5 w-3.5" />
                    Validated
                  </div>
                )}

                <div className="absolute left-4 top-4 text-[#8e9bb8]">
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


