"use client";

import { useMemo, useState } from "react";
import { MODULE_DEFINITIONS, MODULE_DEFINITION_MAP, ModuleDefinition } from "@/lib/uat-modules";
import { ModulePalette } from "./module-palette";
import { ModuleCanvas } from "./module-canvas";
import { ModuleInspector } from "./module-inspector";
import { CanvasModuleInstance, CanvasModuleStatus } from "./types";
import { WorkspaceTopbar, EnvironmentId } from "./workspace-topbar";

const requiredModuleDefinitions = MODULE_DEFINITIONS.filter((module) => module.required);
const optionalModuleDefinitions = MODULE_DEFINITIONS.filter((module) => !module.required);
const requiredModuleIds = requiredModuleDefinitions.map((module) => module.id);

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
    <div className="serious-ui space-y-8">
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
        <div className="rounded-2xl border border-[rgba(248,113,113,0.35)] bg-[rgba(127,29,29,0.25)] px-4 py-3 text-sm text-[rgba(252,165,165,0.95)] shadow-[0px_10px_25px_rgba(127,29,29,0.2)]">
          {duplicateWarning} already exists in the canvas. Select the existing module to edit or remove it before adding
          again.
        </div>
      )}

      <div className="grid gap-6 xl:grid-cols-[320px_minmax(0,1fr)_420px]">
        <ModulePalette
          modules={requiredModuleDefinitions}
          onQuickAdd={addModuleToCanvas}
          title="Core Modules"
          subtitle="Compliance Anchors"
          badgeText={`${requiredModuleDefinitions.length} required`}
          badgeTone="required"
        />

        <ModuleCanvas
          modules={modules}
          selectedModuleId={selectedModuleId}
          onSelectModule={setSelectedModuleId}
          onRemoveModule={handleRemoveModule}
          onDropModule={addModuleToCanvas}
          requiredMissing={requiredMissing}
        />

        <div className="flex flex-col gap-6">
          <ModulePalette
            modules={optionalModuleDefinitions}
            onQuickAdd={addModuleToCanvas}
            title="Advanced Modules"
            subtitle="Enhance Distribution"
            badgeText={`${optionalModuleDefinitions.length} optional`}
            badgeTone="optional"
            searchPlaceholder="Search optional modules, operations, governance…"
            fillHeight={false}
          />
          <ModuleInspector
            modules={modules}
            selectedModuleId={selectedModuleId}
            onUpdateField={updateModuleField}
            onGenerateSample={handleGenerateSample}
            onChangeStatus={handleStatusChange}
          />
        </div>
      </div>
    </div>
  );
}


