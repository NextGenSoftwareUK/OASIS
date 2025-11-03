"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { MicroLicenseTemplate } from "@/types/music";

interface MicroLicenseTemplateCreatorProps {
  onTemplateCreate: (template: MicroLicenseTemplate) => void;
  onCancel: () => void;
}

export function MicroLicenseTemplateCreator({ onTemplateCreate, onCancel }: MicroLicenseTemplateCreatorProps) {
  const [templateName, setTemplateName] = useState("");
  const [platform, setPlatform] = useState("");
  const [basePrice, setBasePrice] = useState(0);
  const [maxUsesPerDay, setMaxUsesPerDay] = useState(1000);
  const [requiresApproval, setRequiresApproval] = useState(false);
  const [terms, setTerms] = useState("");

  const isValid = templateName.trim() !== "" && 
                  platform.trim() !== "" && 
                  basePrice > 0 &&
                  maxUsesPerDay > 0 &&
                  terms.trim() !== "";

  const handleCreate = () => {
    if (!isValid) return;

    const template: MicroLicenseTemplate = {
      id: `custom-${Date.now()}`,
      name: templateName,
      platform: platform.toLowerCase(),
      basePrice,
      maxUsesPerDay,
      requiresApproval,
      terms,
      isCustom: true,
      usageCount: 0,
      createdAt: new Date()
    };

    onTemplateCreate(template);
  };

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Create Custom Micro-License Template</h3>
        <p className="mt-2 text-sm text-[var(--muted)]">
          Define custom per-use licensing terms for specific platforms
        </p>
      </div>

      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            Template Name
          </label>
          <input
            type="text"
            value={templateName}
            onChange={(e) => setTemplateName(e.target.value)}
            placeholder="e.g., My TikTok License"
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            Platform
          </label>
          <select
            value={platform}
            onChange={(e) => setPlatform(e.target.value)}
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
          >
            <option value="">Select platform...</option>
            <option value="tiktok">TikTok</option>
            <option value="instagram">Instagram</option>
            <option value="youtube">YouTube</option>
            <option value="twitter">Twitter/X</option>
            <option value="facebook">Facebook</option>
            <option value="snapchat">Snapchat</option>
            <option value="custom">Custom Platform</option>
          </select>
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            Price Per Use (USD)
          </label>
          <input
            type="number"
            value={basePrice}
            onChange={(e) => setBasePrice(Number(e.target.value))}
            min="0"
            step="0.01"
            placeholder="0.50"
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            Max Uses Per Day
          </label>
          <input
            type="number"
            value={maxUsesPerDay}
            onChange={(e) => setMaxUsesPerDay(Number(e.target.value))}
            min="1"
            placeholder="1000"
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
          />
        </div>
      </div>

      <div className="space-y-4">
        <div className="flex items-center justify-between rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] px-4 py-3">
          <div>
            <h4 className="text-sm font-medium text-[var(--color-foreground)]">Requires Approval</h4>
            <p className="text-xs text-[var(--muted)]">Manual approval required for each license request</p>
          </div>
          <Button
            variant="toggle"
            data-state={requiresApproval ? "active" : undefined}
            onClick={() => setRequiresApproval(!requiresApproval)}
            className="px-3 py-1"
          >
            {requiresApproval ? "Enabled" : "Disabled"}
          </Button>
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            License Terms
          </label>
          <textarea
            value={terms}
            onChange={(e) => setTerms(e.target.value)}
            placeholder="Describe the licensing terms and usage rights..."
            rows={4}
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none resize-none"
          />
        </div>
      </div>

      <div className="rounded-xl border border-[var(--color-card-border)]/60 bg-[rgba(8,12,28,0.85)] p-4">
        <h4 className="text-sm font-medium text-[var(--color-foreground)] mb-3">Template Preview</h4>
        <div className="space-y-2 text-sm text-[var(--muted)]">
          <div className="flex justify-between">
            <span>Platform:</span>
            <span className="text-[var(--color-foreground)]">{platform || "—"}</span>
          </div>
          <div className="flex justify-between">
            <span>Price per use:</span>
            <span className="text-[var(--color-foreground)]">${basePrice}</span>
          </div>
          <div className="flex justify-between">
            <span>Max uses per day:</span>
            <span className="text-[var(--color-foreground)]">{maxUsesPerDay.toLocaleString()}</span>
          </div>
          <div className="flex justify-between">
            <span>Requires approval:</span>
            <span className={cn(
              "font-medium",
              requiresApproval ? "text-[var(--warning)]" : "text-[var(--color-positive)]"
            )}>
              {requiresApproval ? "Yes" : "No"}
            </span>
          </div>
        </div>
      </div>

      <div className="flex gap-3 pt-4 border-t border-[var(--color-card-border)]/30">
        <Button variant="secondary" onClick={onCancel} className="flex-1">
          Cancel
        </Button>
        <Button 
          variant="primary" 
          onClick={handleCreate}
          disabled={!isValid}
          className="flex-1"
        >
          Create Template
        </Button>
      </div>
    </div>
  );
}



