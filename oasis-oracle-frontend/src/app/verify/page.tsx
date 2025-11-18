"use client";

import { useState } from "react";
import { OracleLayout } from "@/components/layout/oracle-layout";
import { VerificationForm } from "@/components/verify/verification-form";
import { VerificationResults } from "@/components/verify/verification-results";
import type { VerificationRequest, TransactionVerification } from "@/types/verification";

export default function VerifyPage() {
  const [verification, setVerification] = useState<TransactionVerification | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleVerify = async (request: VerificationRequest) => {
    setIsLoading(true);

    // Simulate API call
    await new Promise((resolve) => setTimeout(resolve, 2000));

    // Mock verification result
    const mockVerification: TransactionVerification = {
      transactionHash: request.transactionHash,
      chain: request.chain,
      status: "verified",
      confirmations: request.requiredConfirmations + 13,
      requiredConfirmations: request.requiredConfirmations,
      isFinalized: true,
      hasValidSignature: true,
      noDoubleSpend: true,
      blockNumber: Math.floor(Math.random() * 1000000) + 50000000,
      timestamp: new Date(Date.now() - Math.random() * 3600000),
      from: "0x742d35Cc6634C0532925a3b844Bc9e7595f0bEb0",
      to: "0x8Ba1f109551bD432803012645Ac136ddd64DBA72",
      amount: Math.random() * 10,
      token: request.chain === "Ethereum" ? "ETH" : request.chain === "Solana" ? "SOL" : "XRD",
    };

    setVerification(mockVerification);
    setIsLoading(false);
  };

  const handleVerifyAnother = () => {
    setVerification(null);
  };

  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Hero Section */}
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
            Transaction Verification
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-3xl">
            Verify cross-chain transactions with oracle-backed validation. Check confirmations,
            finality status, and transaction integrity across 20+ blockchains.
          </p>
        </div>

        {/* Verification Form or Results */}
        {!verification ? (
          <VerificationForm onSubmit={handleVerify} isLoading={isLoading} />
        ) : (
          <VerificationResults verification={verification} onVerifyAnother={handleVerifyAnother} />
        )}

        {/* Info Cards */}
        {!verification && (
          <div className="grid grid-cols-1 gap-6 mt-12 md:grid-cols-3">
            <InfoCard
              title="Oracle Verification"
              description="Each transaction is verified by multiple oracle nodes across the network for consensus-backed validation."
            />
            <InfoCard
              title="Finality Checks"
              description="Monitors block confirmations and finality status to ensure transactions are irreversible."
            />
            <InfoCard
              title="Cross-Chain Support"
              description="Verify transactions on 20+ blockchains including Ethereum, Solana, Polygon, and more."
            />
          </div>
        )}
      </div>
    </OracleLayout>
  );
}

function InfoCard({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-2xl border border-[var(--color-card-border)]/50 bg-[rgba(8,11,26,0.85)] p-6 backdrop-blur-xl">
      <h3 className="mb-2 text-lg font-semibold text-[var(--color-foreground)]">{title}</h3>
      <p className="text-sm leading-relaxed text-[var(--muted)]">{description}</p>
    </div>
  );
}





