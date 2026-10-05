import { ModuleDefinition } from "@/lib/uat-modules";

export type CanvasModuleStatus = "draft" | "ready" | "needs-review";

export type CanvasModuleInstance = {
  instanceId: string;
  moduleId: ModuleDefinition["id"];
  values: Record<string, string>;
  status: CanvasModuleStatus;
  addedAt: number;
};



