"use client";

import dynamic from "next/dynamic";

const UatWorkspace = dynamic(
  () =>
    import("../../../../oasis-oracle-frontend/src/components/uat/uat-workspace").then(
      (mod) => mod.UatWorkspace
    ),
  { ssr: false }
);

export default function UatMintingPage() {
  return <UatWorkspace />;
}



