"use client";

import * as React from "react";
import { cn } from "@/lib/utils";

interface TabsContextValue {
  value: string;
  onValueChange: (value: string) => void;
}

const TabsContext = React.createContext<TabsContextValue | undefined>(undefined);

export function Tabs({ 
  value, 
  onValueChange, 
  children, 
  className 
}: { 
  value: string; 
  onValueChange: (value: string) => void; 
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <TabsContext.Provider value={{ value, onValueChange }}>
      <div className={className}>{children}</div>
    </TabsContext.Provider>
  );
}

export function TabsList({ children, className }: { children: React.ReactNode; className?: string }) {
  return (
    <div className={cn("inline-flex items-center justify-center rounded-lg p-1", className)} 
      style={{
        background: 'rgba(6,11,26,0.8)',
        border: '1px solid var(--oasis-card-border)'
      }}>
      {children}
    </div>
  );
}

export function TabsTrigger({ value, children }: { value: string; children: React.ReactNode }) {
  const context = React.useContext(TabsContext);
  if (!context) throw new Error("TabsTrigger must be used within Tabs");
  
  const isActive = context.value === value;
  
  return (
    <button
      onClick={() => context.onValueChange(value)}
      className={cn(
        "inline-flex items-center justify-center whitespace-nowrap rounded-md px-6 py-3 text-sm font-semibold transition-all",
        isActive
          ? ""
          : "hover:bg-[rgba(15,118,110,0.1)]"
      )}
      style={{
        background: isActive ? 'var(--oasis-accent)' : 'transparent',
        color: isActive ? '#041321' : 'var(--oasis-muted)'
      }}
    >
      {children}
    </button>
  );
}

export function TabsContent({ value, children }: { value: string; children: React.ReactNode }) {
  const context = React.useContext(TabsContext);
  if (!context) throw new Error("TabsContent must be used within Tabs");
  
  if (context.value !== value) return null;
  
  return <div className="mt-4">{children}</div>;
}

