"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { RoyaltySplitTemplate, ROYALTY_SPLIT_TEMPLATES } from "@/types/music";

interface RoyaltyTemplateSelectorProps {
  selectedTemplateId?: string;
  onTemplateSelect: (template: RoyaltySplitTemplate) => void;
  onCreateCustom: () => void;
}

export function RoyaltyTemplateSelector({ 
  selectedTemplateId, 
  onTemplateSelect, 
  onCreateCustom 
}: RoyaltyTemplateSelectorProps) {
  const [searchTerm, setSearchTerm] = useState("");

  const filteredTemplates = ROYALTY_SPLIT_TEMPLATES.filter(template =>
    template.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
    template.description.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Select Royalty Split Template</h3>
        <p className="mt-2 text-sm text-[var(--muted)]">
          Choose from pre-defined templates or create your own custom split
        </p>
      </div>

      <div className="relative">
        <input
          type="text"
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          placeholder="Search templates..."
          className="w-full rounded-lg border border-[var(--color-card-border)]/60 bg-[rgba(9,14,32,0.85)] px-4 py-3 pl-10 text-sm text-[var(--color-foreground)] focus:border-[var(--accent)] focus:outline-none"
        />
        <svg
          className="absolute left-3 top-3.5 h-4 w-4 text-[var(--muted)]"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z"
          />
        </svg>
      </div>

      <div className="grid gap-4">
        {filteredTemplates.map((template) => (
          <div
            key={template.id}
            className={cn(
              "rounded-xl border p-4 cursor-pointer transition-all",
              selectedTemplateId === template.id
                ? "border-[var(--accent)]/70 bg-[rgba(34,211,238,0.12)]"
                : "border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] hover:border-[var(--accent)]/30"
            )}
            onClick={() => onTemplateSelect(template)}
          >
            <div className="flex items-start justify-between">
              <div className="flex-1">
                <h4 className="font-semibold text-[var(--color-foreground)]">{template.name}</h4>
                <p className="mt-1 text-sm text-[var(--muted)]">{template.description}</p>
                
                <div className="mt-3 flex flex-wrap gap-2">
                  {template.splits.map((split, index) => (
                    <span
                      key={index}
                      className="inline-flex items-center rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(8,11,26,0.6)] px-2 py-1 text-xs text-[var(--color-foreground)]"
                    >
                      <span className="font-medium">{split.role}</span>
                      <span className="ml-1 text-[var(--accent)]">{split.percentage}%</span>
                    </span>
                  ))}
                </div>
              </div>
              
              <div className="ml-4 flex items-center">
                {selectedTemplateId === template.id && (
                  <div className="flex h-5 w-5 items-center justify-center rounded-full bg-[var(--accent)]">
                    <svg className="h-3 w-3 text-[#041321]" fill="currentColor" viewBox="0 0 20 20">
                      <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                    </svg>
                  </div>
                )}
              </div>
            </div>
          </div>
        ))}
      </div>

      {filteredTemplates.length === 0 && (
        <div className="text-center py-8">
          <p className="text-sm text-[var(--muted)]">No templates found matching your search.</p>
        </div>
      )}

      <div className="pt-4 border-t border-[var(--color-card-border)]/30">
        <Button 
          variant="secondary" 
          onClick={onCreateCustom}
          className="w-full"
        >
          Create Custom Template
        </Button>
      </div>
    </div>
  );
}



