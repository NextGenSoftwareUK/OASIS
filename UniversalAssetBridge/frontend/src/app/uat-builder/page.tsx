import Web4Nav from "@/components/Web4Nav";
import { UatWorkspace } from "@/components/uat/uat-workspace";

export const metadata = {
  title: "UAT Builder | Web4 Tokens",
  description:
    "Drag-and-drop minting workspace for assembling compliant Universal Asset Tokens with trust structures, revenue automation, and compliance gating.",
};

export default function UatBuilderPage() {
  return (
    <div className="min-h-screen">
      <Web4Nav />
      <UatWorkspace />
    </div>
  );
}


