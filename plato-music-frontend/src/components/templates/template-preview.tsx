"use client";

import { cn } from "@/lib/utils";
import { RoyaltySplitTemplate, MicroLicenseTemplate } from "@/types/music";

interface TemplatePreviewProps {
  royaltyTemplate?: RoyaltySplitTemplate;
  microLicenseTemplate?: MicroLicenseTemplate;
  className?: string;
}

export function TemplatePreview({ 
  royaltyTemplate, 
  microLicenseTemplate, 
  className 
}: TemplatePreviewProps) {
  return (
    <div className={cn("space-y-4", className)}>
      <h4 className="text-lg font-semibold text-[var(--color-foreground)]">Template Configuration</h4>
      
      {royaltyTemplate && (
        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4">
          <div className="flex items-center gap-2 mb-3">
            <h5 className="font-medium text-[var(--color-foreground)]">Royalty Split Template</h5>
            <span className="inline-flex items-center rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(8,11,26,0.6)] px-2 py-1 text-xs font-medium text-[var(--accent)]">
              {royaltyTemplate.isCustom ? "Custom" : "Standard"}
            </span>
          </div>
          
          <div className="mb-3">
            <h6 className="text-sm font-medium text-[var(--color-foreground)]">{royaltyTemplate.name}</h6>
            <p className="text-xs text-[var(--muted)]">{royaltyTemplate.description}</p>
          </div>
          
          <div className="space-y-2">
            {royaltyTemplate.splits.map((split, index) => (
              <div key={index} className="flex items-center justify-between rounded-lg border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] px-3 py-2">
                <div className="flex items-center gap-2">
                  <span className="text-sm font-medium text-[var(--color-foreground)]">{split.role}</span>
                  {split.walletAddress && (
                    <span className="text-xs text-[var(--muted)]">
                      {split.walletAddress.slice(0, 6)}...{split.walletAddress.slice(-4)}
                    </span>
                  )}
                </div>
                <span className="text-sm font-semibold text-[var(--accent)]">{split.percentage}%</span>
              </div>
            ))}
          </div>
          
          <div className="mt-3 flex items-center justify-between text-xs text-[var(--muted)]">
            <span>Total: {royaltyTemplate.splits.reduce((sum, split) => sum + split.percentage, 0)}%</span>
            <span>Used {royaltyTemplate.usageCount} times</span>
          </div>
        </div>
      )}
      
      {microLicenseTemplate && (
        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4">
          <div className="flex items-center gap-2 mb-3">
            <h5 className="font-medium text-[var(--color-foreground)]">Micro-License Template</h5>
            <span className="inline-flex items-center rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(8,11,26,0.6)] px-2 py-1 text-xs font-medium text-[var(--accent)] capitalize">
              {microLicenseTemplate.platform}
            </span>
            <span className="inline-flex items-center rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(8,11,26,0.6)] px-2 py-1 text-xs font-medium text-[var(--accent)]">
              {microLicenseTemplate.isCustom ? "Custom" : "Standard"}
            </span>
          </div>
          
          <div className="mb-3">
            <h6 className="text-sm font-medium text-[var(--color-foreground)]">{microLicenseTemplate.name}</h6>
            <p className="text-xs text-[var(--muted)]">{microLicenseTemplate.terms}</p>
          </div>
          
          <div className="grid grid-cols-3 gap-4 text-sm">
            <div>
              <span className="text-[var(--muted)]">Price per use:</span>
              <div className="font-semibold text-[var(--color-foreground)]">${microLicenseTemplate.basePrice}</div>
            </div>
            <div>
              <span className="text-[var(--muted)]">Max daily uses:</span>
              <div className="font-semibold text-[var(--color-foreground)]">{microLicenseTemplate.maxUsesPerDay.toLocaleString()}</div>
            </div>
            <div>
              <span className="text-[var(--muted)]">Approval:</span>
              <div className={cn(
                "font-semibold",
                microLicenseTemplate.requiresApproval ? "text-[var(--warning)]" : "text-[var(--color-positive)]"
              )}>
                {microLicenseTemplate.requiresApproval ? "Required" : "Instant"}
              </div>
            </div>
          </div>
          
          <div className="mt-3 text-xs text-[var(--muted)]">
            Used {microLicenseTemplate.usageCount} times
          </div>
        </div>
      )}
      
      {!royaltyTemplate && !microLicenseTemplate && (
        <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-6 text-center">
          <p className="text-sm text-[var(--muted)]">No templates selected</p>
          <p className="text-xs text-[var(--muted)] mt-1">Select royalty split and micro-license templates to see preview</p>
        </div>
      )}
    </div>
  );
}



