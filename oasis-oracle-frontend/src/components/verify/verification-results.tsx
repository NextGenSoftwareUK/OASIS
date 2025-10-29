"use client";

import { Card } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { 
  CheckCircle2, 
  XCircle, 
  AlertCircle, 
  ExternalLink, 
  Download, 
  Share2,
  Clock
} from "lucide-react";
import { formatCurrency, truncateAddress } from "@/lib/utils";
import type { TransactionVerification } from "@/types/verification";

type VerificationResultsProps = {
  verification: TransactionVerification;
  onVerifyAnother: () => void;
};

export function VerificationResults({ verification, onVerifyAnother }: VerificationResultsProps) {
  const isVerified = verification.status === "verified";
  const isFailed = verification.status === "failed";
  const isPending = verification.status === "pending";

  const statusConfig = {
    verified: {
      icon: CheckCircle2,
      color: "text-[var(--positive)]",
      bg: "bg-[rgba(34,197,94,0.15)]",
      border: "border-[var(--positive)]/30",
      badge: "success" as const,
    },
    failed: {
      icon: XCircle,
      color: "text-[var(--negative)]",
      bg: "bg-[rgba(239,68,68,0.15)]",
      border: "border-[var(--negative)]/30",
      badge: "danger" as const,
    },
    pending: {
      icon: Clock,
      color: "text-[var(--warning)]",
      bg: "bg-[rgba(250,204,21,0.15)]",
      border: "border-[var(--warning)]/30",
      badge: "warning" as const,
    },
    rejected: {
      icon: AlertCircle,
      color: "text-[var(--negative)]",
      bg: "bg-[rgba(239,68,68,0.15)]",
      border: "border-[var(--negative)]/30",
      badge: "danger" as const,
    },
  };

  const config = statusConfig[verification.status];
  const Icon = config.icon;

  return (
    <div className="space-y-6 max-w-4xl mx-auto">
      {/* Status Header */}
      <Card
        variant="gradient"
        className={`${config.border} border-2`}
      >
        <div className={`flex items-center justify-center gap-4 p-8 rounded-2xl ${config.bg}`}>
          <Icon className={`h-16 w-16 ${config.color}`} strokeWidth={2} />
          <div>
            <h2 className="text-3xl font-bold text-[var(--color-foreground)]">
              {verification.status.toUpperCase()}
            </h2>
            <p className="text-[var(--muted)] mt-1">
              {isVerified && "Transaction has been successfully verified"}
              {isFailed && "Transaction verification failed"}
              {isPending && "Transaction is pending confirmation"}
            </p>
          </div>
        </div>
      </Card>

      {/* Verification Checklist */}
      <Card title="Verification Checklist" variant="glass">
        <div className="space-y-4">
          <ChecklistItem
            label="Transaction Found"
            status={verification.isFinalized ? "success" : "pending"}
          />
          <ChecklistItem
            label={`Confirmations: ${verification.confirmations}/${verification.requiredConfirmations}`}
            status={
              verification.confirmations >= verification.requiredConfirmations
                ? "success"
                : "pending"
            }
            details={`${Math.round((verification.confirmations / verification.requiredConfirmations) * 100)}% complete`}
          />
          <ChecklistItem
            label="Finality Reached"
            status={verification.isFinalized ? "success" : "pending"}
          />
          <ChecklistItem
            label="Valid Signature"
            status={verification.hasValidSignature ? "success" : "failed"}
          />
          <ChecklistItem
            label="No Double-Spend Detected"
            status={verification.noDoubleSpend ? "success" : "failed"}
          />
        </div>
      </Card>

      {/* Transaction Details */}
      <Card title="Transaction Details" variant="glass">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <DetailRow label="Chain" value={verification.chain} />
          <DetailRow 
            label="Status" 
            value={<Badge variant={config.badge} dot>{verification.status}</Badge>} 
          />
          <DetailRow 
            label="Transaction Hash" 
            value={
              <div className="flex items-center gap-2">
                <code className="text-sm">{truncateAddress(verification.transactionHash, 8, 8)}</code>
                <button className="text-[var(--accent)] hover:text-[#38e0ff]">
                  <ExternalLink className="h-4 w-4" />
                </button>
              </div>
            } 
          />
          <DetailRow label="Block Number" value={`#${verification.blockNumber.toLocaleString()}`} />
          <DetailRow 
            label="From" 
            value={<code className="text-sm">{truncateAddress(verification.from)}</code>} 
          />
          <DetailRow 
            label="To" 
            value={<code className="text-sm">{truncateAddress(verification.to)}</code>} 
          />
          <DetailRow 
            label="Amount" 
            value={`${verification.amount} ${verification.token}`} 
          />
          <DetailRow 
            label="Timestamp" 
            value={verification.timestamp.toLocaleString()} 
          />
        </div>
      </Card>

      {/* Action Buttons */}
      <div className="flex items-center justify-center gap-4">
        <Button variant="secondary" className="flex items-center gap-2">
          <Download className="h-4 w-4" />
          Download Proof
        </Button>
        <Button variant="secondary" className="flex items-center gap-2">
          <Share2 className="h-4 w-4" />
          Share Link
        </Button>
        <Button variant="outline" className="flex items-center gap-2">
          <ExternalLink className="h-4 w-4" />
          View on Explorer
        </Button>
      </div>

      {/* Verify Another */}
      <div className="text-center">
        <Button variant="primary" onClick={onVerifyAnother}>
          Verify Another Transaction
        </Button>
      </div>
    </div>
  );
}

function ChecklistItem({
  label,
  status,
  details,
}: {
  label: string;
  status: "success" | "failed" | "pending";
  details?: string;
}) {
  const icons = {
    success: CheckCircle2,
    failed: XCircle,
    pending: Clock,
  };

  const colors = {
    success: "text-[var(--positive)]",
    failed: "text-[var(--negative)]",
    pending: "text-[var(--warning)]",
  };

  const Icon = icons[status];

  return (
    <div className="flex items-center justify-between p-3 rounded-lg bg-[rgba(5,5,16,0.5)] border border-[var(--color-card-border)]/30">
      <div className="flex items-center gap-3">
        <Icon className={`h-5 w-5 ${colors[status]}`} />
        <span className="text-sm font-medium">{label}</span>
      </div>
      {details && (
        <span className="text-xs text-[var(--muted)]">{details}</span>
      )}
    </div>
  );
}

function DetailRow({ label, value }: { label: string; value: React.ReactNode }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs uppercase tracking-wide text-[var(--muted)]">{label}</span>
      <div className="text-sm font-medium text-[var(--color-foreground)]">{value}</div>
    </div>
  );
}

