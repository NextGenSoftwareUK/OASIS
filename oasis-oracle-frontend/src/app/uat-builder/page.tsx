import { OracleLayout } from "@/components/layout/oracle-layout";
import { UatWorkspace } from "@/components/uat/uat-workspace";

export default function UatBuilderPage() {
  return (
    <OracleLayout>
      <div className="space-y-8">
        <div className="space-y-3">
          <p className="text-xs uppercase tracking-[0.6em] text-[var(--muted)]">UAT Builder · Drag & Drop</p>
          <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
            Token Design Workspace
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-4xl">
            Assemble compliant Universal Asset Tokens with trust structures, revenue automation, and multi-chain
            deployment. Configure modules, validate data, and generate mint-ready payloads for your analysts.
          </p>
        </div>

        <UatWorkspace />
      </div>
    </OracleLayout>
  );
}



