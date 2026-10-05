"use client";

import { useState } from "react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { MicroLicenseTemplate, MICRO_LICENSE_TEMPLATES } from "@/types/music";

interface MicroLicenseTemplateSelectorProps {
  selectedTemplateId?: string;
  onTemplateSelect: (template: MicroLicenseTemplate) => void;
  onCreateCustom: () => void;
}

export function MicroLicenseTemplateSelector({ 
  selectedTemplateId, 
  onTemplateSelect, 
  onCreateCustom 
}: MicroLicenseTemplateSelectorProps) {
  const [searchTerm, setSearchTerm] = useState("");
  const [platformFilter, setPlatformFilter] = useState("");

  const filteredTemplates = MICRO_LICENSE_TEMPLATES.filter(template => {
    const matchesSearch = template.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
                         template.platform.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesPlatform = platformFilter === "" || template.platform === platformFilter;
    return matchesSearch && matchesPlatform;
  });

  const platforms = Array.from(new Set(MICRO_LICENSE_TEMPLATES.map(t => t.platform)));

  return (
    <div className="space-y-6">
      <div className="text-center">
        <h3 className="text-xl font-semibold text-[var(--color-foreground)]">Select Micro-License Template</h3>
        <p className="mt-2 text-sm text-[var(--muted)]">
          Choose from pre-defined micro-licensing templates or create your own
        </p>
      </div>

      <div className="space-y-4">
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

        <div className="flex flex-wrap gap-2">
          <button
            onClick={() => setPlatformFilter("")}
            className={cn(
              "rounded-full border px-3 py-1 text-xs font-medium transition-colors",
              platformFilter === ""
                ? "border-[var(--accent)]/70 bg-[rgba(34,211,238,0.12)] text-[var(--accent)]"
                : "border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] text-[var(--muted)] hover:text-[var(--color-foreground)]"
            )}
          >
            All Platforms
          </button>
          {platforms.map(platform => (
            <button
              key={platform}
              onClick={() => setPlatformFilter(platform)}
              className={cn(
                "rounded-full border px-3 py-1 text-xs font-medium transition-colors capitalize",
                platformFilter === platform
                  ? "border-[var(--accent)]/70 bg-[rgba(34,211,238,0.12)] text-[var(--accent)]"
                  : "border-[var(--color-card-border)]/40 bg-[rgba(6,10,24,0.7)] text-[var(--muted)] hover:text-[var(--color-foreground)]"
              )}
            >
              {platform}
            </button>
          ))}
        </div>
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
                <div className="flex items-center gap-2 mb-2">
                  <h4 className="font-semibold text-[var(--color-foreground)]">{template.name}</h4>
                  <span className="inline-flex items-center rounded-full border border-[var(--color-card-border)]/40 bg-[rgba(8,11,26,0.6)] px-2 py-1 text-xs font-medium text-[var(--accent)] capitalize">
                    {template.platform}
                  </span>
                </div>
                
                <p className="text-sm text-[var(--muted)] mb-3">{template.terms}</p>
                
                <div className="grid grid-cols-3 gap-4 text-sm">
                  <div>
                    <span className="text-[var(--muted)]">Price per use:</span>
                    <div className="font-semibold text-[var(--color-foreground)]">${template.basePrice}</div>
                  </div>
                  <div>
                    <span className="text-[var(--muted)]">Max daily uses:</span>
                    <div className="font-semibold text-[var(--color-foreground)]">{template.maxUsesPerDay.toLocaleString()}</div>
                  </div>
                  <div>
                    <span className="text-[var(--muted)]">Approval:</span>
                    <div className={cn(
                      "font-semibold",
                      template.requiresApproval ? "text-[var(--warning)]" : "text-[var(--color-positive)]"
                    )}>
                      {template.requiresApproval ? "Required" : "Instant"}
                    </div>
                  </div>
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
          <p className="text-sm text-[var(--muted)]">No templates found matching your criteria.</p>
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



