export type CanvasModuleStatus = "draft" | "ready" | "needs-review";

export type CanvasModuleInstance = {
  instanceId: string;
  moduleId: string;
  values: Record<string, string>;
  status: CanvasModuleStatus;
  addedAt: number;
};



