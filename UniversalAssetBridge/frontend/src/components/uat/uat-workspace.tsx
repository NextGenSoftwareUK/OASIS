"use client";

import { useMemo, useState } from "react";
import { MODULE_DEFINITIONS, MODULE_DEFINITION_MAP, ModuleDefinition } from "@/lib/uat-modules";
import { ModulePalette } from "./module-palette";
import { ModuleCanvas } from "./module-canvas";
import { ModuleInspector } from "./module-inspector";
import { CanvasModuleInstance, CanvasModuleStatus } from "./types";
import { EnvironmentId, WorkspaceTopbar } from "./workspace-topbar";

const requiredModuleIds = MODULE_DEFINITIONS.filter((module) => module.required).map((module) => module.id);

function computeModuleCompletion(definition: ModuleDefinition, values: Record<string, string>): boolean {
  return definition.fields.every((field) => {
    if (!field.required) {
      return true;
    }
    const value = values[field.key];
    return Boolean(value?.toString().trim());
  });
}

export function UatWorkspace() {
  const [environment, setEnvironment] = useState<EnvironmentId>("devnet");
  const [jwtConnected, setJwtConnected] = useState(false);
  const [x402Enabled, setX402Enabled] = useState(true);
  const [selectedModuleId, setSelectedModuleId] = useState<string | null>(null);
  const [modules, setModules] = useState<CanvasModuleInstance[]>([]);
  const [duplicateWarning, setDuplicateWarning] = useState<string | null>(null);

  const requiredModules = useMemo(
    () => MODULE_DEFINITIONS.filter((module) => module.required),
    []
  );
  const optionalModules = useMemo(
    () => MODULE_DEFINITIONS.filter((module) => !module.required),
    []
  );

  const addModuleToCanvas = (moduleId: string) => {
    const definition = MODULE_DEFINITION_MAP[moduleId];
    if (!definition) {
      return;
    }

    const existing = modules.filter((module) => module.moduleId === moduleId);
    if (!definition.allowMultiple && existing.length > 0) {
      setSelectedModuleId(existing[0].instanceId);
      setDuplicateWarning(definition.name);
      window.setTimeout(() => setDuplicateWarning(null), 2600);
      return;
    }

    const instanceId = globalThis.crypto.randomUUID();
    const newModule: CanvasModuleInstance = {
      instanceId,
      moduleId,
      status: "draft",
      values: {},
      addedAt: Date.now(),
    };

    setModules((prev) => [...prev, newModule]);
    setSelectedModuleId(instanceId);
  };

  const updateModuleField = (instanceId: string, fieldKey: string, value: string) => {
    setModules((prev) =>
      prev.map((module) => {
        if (module.instanceId !== instanceId) {
          return module;
        }

        return {
          ...module,
          values: {
            ...module.values,
            [fieldKey]: value,
          },
          status: module.status === "ready" ? "needs-review" : module.status,
        };
      })
    );
  };

  const handleGenerateSample = (instanceId: string) => {
    setModules((prev) =>
      prev.map((module) => {
        if (module.instanceId !== instanceId) {
          return module;
        }
        const definition = MODULE_DEFINITION_MAP[module.moduleId];
        return {
          ...module,
          values: {
            ...module.values,
            ...definition.sampleValues,
          },
          status: "ready",
        };
      })
    );
  };

  const handleStatusChange = (instanceId: string, status: CanvasModuleStatus) => {
    setModules((prev) =>
      prev.map((module) => {
        if (module.instanceId !== instanceId) {
          return module;
        }
        return {
          ...module,
          status,
        };
      })
    );
  };

  const handleRemoveModule = (instanceId: string) => {
    setModules((prev) => prev.filter((module) => module.instanceId !== instanceId));
    if (selectedModuleId === instanceId) {
      setSelectedModuleId(null);
    }
  };

  const requiredMissing = useMemo(() => {
    return requiredModuleIds
      .filter((id) => !modules.some((module) => module.moduleId === id))
      .map((id) => MODULE_DEFINITION_MAP[id].name);
  }, [modules]);

  const complianceStats = useMemo(() => {
    const completed = requiredModuleIds.reduce((count, moduleId) => {
      const definition = MODULE_DEFINITION_MAP[moduleId];
      const moduleInstance = modules.find((module) => module.moduleId === moduleId);
      if (!moduleInstance) {
        return count;
      }
      const complete = computeModuleCompletion(definition, moduleInstance.values);
      return complete ? count + 1 : count;
    }, 0);

    return {
      completed,
      requiredTotal: requiredModuleIds.length,
    };
  }, [modules]);

  return (
    <div className="space-y-8 px-4 py-10 lg:px-10 xl:px-16">
      <WorkspaceTopbar
        environment={environment}
        onEnvironmentChange={setEnvironment}
        jwtConnected={jwtConnected}
        onToggleJwt={() => setJwtConnected((prev) => !prev)}
        x402Enabled={x402Enabled}
        onToggleX402={() => setX402Enabled((prev) => !prev)}
        complianceProgress={complianceStats}
      />

      {duplicateWarning && (
        <div className="rounded-2xl border border-[rgba(248,113,113,0.35)] bg-[rgba(127,29,29,0.25)] px-4 py-3 text-sm text-[rgba(252,165,165,0.95)] shadow-[0px_18px_45px_rgba(127,29,29,0.25)]">
          {duplicateWarning} already exists in the canvas. Select the existing module to edit or remove it before adding
          again.
        </div>
      )}

      <div className="mx-auto grid w-full max-w-[1800px] grid-cols-[minmax(320px,1.05fr)_minmax(660px,1.6fr)_minmax(340px,1.1fr)] items-start gap-8">
        <div className="w-full lg:sticky lg:top-28 lg:self-start lg:min-h-0 lg:pr-1">
          <ModulePalette
            title="Core Modules"
            subtitle="Compliance anchors"
            badgeLabel={`${requiredModules.length} required`}
            accent="required"
            modules={requiredModules}
            onQuickAdd={addModuleToCanvas}
            className="lg:max-h-[calc(100vh-240px)]"
          />
        </div>

        <div className="w-full">
          <ModuleCanvas
            modules={modules}
            selectedModuleId={selectedModuleId}
            onSelectModule={setSelectedModuleId}
            onRemoveModule={handleRemoveModule}
            onDropModule={addModuleToCanvas}
            requiredMissing={requiredMissing}
            className="w-full"
          />
        </div>

        <div className="flex w-full flex-col gap-6">
          <ModulePalette
            title="Advanced Modules"
            subtitle="Enhance distribution"
            badgeLabel={`${optionalModules.length} optional`}
            accent="optional"
            modules={optionalModules}
            onQuickAdd={addModuleToCanvas}
            className="min-h-[320px] flex-1"
          />

          <ModuleInspector
            modules={modules}
            selectedModuleId={selectedModuleId}
            onUpdateField={updateModuleField}
            onGenerateSample={handleGenerateSample}
            onChangeStatus={handleStatusChange}
            className="min-h-[360px]"
          />
        </div>
      </div>
    </div>
  );
}


