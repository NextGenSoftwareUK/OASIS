"use client";

import { useState } from "react";
import { ChevronDown, ChevronUp, HelpCircle } from "lucide-react";

interface HowItWorksProps {
  sections: {
    title: string;
    content: React.ReactNode;
  }[];
  defaultOpen?: boolean;
}

export default function HowItWorks({ sections, defaultOpen = false }: HowItWorksProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  return (
    <div className="rounded-2xl border" style={{
      borderColor: 'var(--oasis-card-border)',
      background: 'rgba(6,11,26,0.6)'
    }}>
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="w-full p-6 flex items-center justify-between transition hover:bg-[rgba(15,118,110,0.05)]"
      >
        <div className="flex items-center gap-3">
          <HelpCircle size={24} color="var(--oasis-accent)" />
          <h3 className="text-xl font-bold" style={{color: 'var(--oasis-foreground)'}}>
            How It Works
          </h3>
        </div>
        {isOpen ? (
          <ChevronUp size={20} color="var(--oasis-accent)" />
        ) : (
          <ChevronDown size={20} color="var(--oasis-accent)" />
        )}
      </button>
      
      {isOpen && (
        <div className="px-6 pb-6 space-y-6 border-t" style={{borderColor: 'var(--oasis-card-border)'}}>
          {sections.map((section, index) => (
            <div key={index} className="space-y-3">
              <h4 className="text-lg font-bold" style={{color: 'var(--oasis-accent)'}}>
                {section.title}
              </h4>
              <div className="text-base leading-relaxed" style={{color: 'var(--oasis-muted)'}}>
                {section.content}
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
