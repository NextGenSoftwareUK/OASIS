"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { RoyaltySplitTemplate } from "@/types/music";

interface RoyaltyTemplateCreatorProps {
  onTemplateCreate: (template: RoyaltySplitTemplate) => void;
  onCancel: () => void;
}

export function RoyaltyTemplateCreator({ onTemplateCreate, onCancel }: RoyaltyTemplateCreatorProps) {
  const [templateName, setTemplateName] = useState("");
  const [templateDescription, setTemplateDescription] = useState("");
  const [splits, setSplits] = useState([
    { role: "", percentage: 0, walletAddress: "" }
  ]);

  const totalPercentage = splits.reduce((sum, split) => sum + split.percentage, 0);
  const isValid = templateName.trim() !== "" && 
                  templateDescription.trim() !== "" && 
                  totalPercentage === 100 &&
                  splits.every(split => split.role.trim() !== "" && split.percentage > 0);

  const addSplit = () => {
    setSplits([...splits, { role: "", percentage: 0, walletAddress: "" }]);
  };

  const removeSplit = (index: number) => {
    if (splits.length > 1) {
      setSplits(splits.filter((_, i) => i !== index));
    }
  };

  const updateSplit = (index: number, field: keyof typeof splits[0], value: string | number) => {
    const newSplits = [...splits];
    newSplits[index] = { ...newSplits[index], [field]: value };
    setSplits(newSplits);
  };

  const handleCreate = () => {
    if (!isValid) return;

    const template: RoyaltySplitTemplate = {
      id: `custom-${Date.now()}`,
      name: templateName,
      description: templateDescription,
      splits: splits.map(split => ({
        role: split.role,
        percentage: split.percentage,
        walletAddress: split.walletAddress || undefined
      })),
      isCustom: true,
      usageCount: 0,
      createdAt: new Date()
    };

    onTemplateCreate(template);
  };

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Create Custom Royalty Split</h3>
        <p className="mt-2 text-sm text-[var(--muted)]">
          Define custom royalty percentages for your rights holders
        </p>
      </div>

      <div className="space-y-4">
        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            Template Name
          </label>
          <input
            type="text"
            value={templateName}
            onChange={(e) => setTemplateName(e.target.value)}
            placeholder="e.g., My Custom Split"
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-[var(--color-foreground)] mb-2">
            Description
          </label>
          <textarea
            value={templateDescription}
            onChange={(e) => setTemplateDescription(e.target.value)}
            placeholder="Describe this royalty split configuration..."
            rows={3}
            className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none resize-none"
          />
        </div>
      </div>

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <h4 className="text-lg font-medium text-[var(--color-foreground)]">Royalty Splits</h4>
          <Button variant="secondary" size="sm" onClick={addSplit}>
            Add Split
          </Button>
        </div>

        <div className="space-y-3">
          {splits.map((split, index) => (
            <div key={index} className="flex items-center gap-3 p-4 rounded-xl border border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)]">
              <div className="flex-1">
                <label className="block text-xs font-medium text-[var(--muted)] mb-1">
                  Role
                </label>
                <input
                  type="text"
                  value={split.role}
                  onChange={(e) => updateSplit(index, 'role', e.target.value)}
                  placeholder="e.g., Writer, Producer, Label"
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                />
              </div>
              <div className="w-24">
                <label className="block text-xs font-medium text-[var(--muted)] mb-1">
                  Percentage
                </label>
                <input
                  type="number"
                  value={split.percentage}
                  onChange={(e) => updateSplit(index, 'percentage', Number(e.target.value))}
                  min="0"
                  max="100"
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                />
              </div>
              <div className="flex-1">
                <label className="block text-xs font-medium text-[var(--muted)] mb-1">
                  Wallet Address (Optional)
                </label>
                <input
                  type="text"
                  value={split.walletAddress}
                  onChange={(e) => updateSplit(index, 'walletAddress', e.target.value)}
                  placeholder="0x..."
                  className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-3 py-2 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
                />
              </div>
              {splits.length > 1 && (
                <Button
                  variant="secondary"
                  size="sm"
                  onClick={() => removeSplit(index)}
                  className="text-[var(--negative)] hover:text-[var(--negative)]"
                >
                  Remove
                </Button>
              )}
            </div>
          ))}
        </div>

        <div className={cn(
          "text-sm font-medium",
          totalPercentage === 100 ? "text-[var(--color-positive)]" : "text-[var(--negative)]"
        )}>
          Total: {totalPercentage}% {totalPercentage === 100 ? "✓" : "(Must equal 100%)"}
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



