import { ReactNode } from "react";
import { cn } from "@/lib/utils";

export type BadgeProps = {
  children: ReactNode;
  variant?: "default" | "success" | "warning" | "danger" | "info";
  size?: "sm" | "md" | "lg";
  dot?: boolean;
  className?: string;
};

const variantStyles = {
  default: "bg-[var(--accent-soft)] text-[var(--accent)] border-[var(--accent-soft)]",
  success: "bg-[rgba(34,197,94,0.15)] text-[var(--positive)] border-[var(--color-positive)]/30",
  warning: "bg-[rgba(250,204,21,0.15)] text-[var(--warning)] border-[var(--color-warning)]/30",
  danger: "bg-[rgba(239,68,68,0.15)] text-[var(--negative)] border-[var(--negative)]/30",
  info: "bg-[rgba(56,189,248,0.15)] text-[#38bdf8] border-[#38bdf8]/30",
};

const sizeStyles = {
  sm: "text-[10px] px-2 py-0.5",
  md: "text-xs px-3 py-1",
  lg: "text-sm px-4 py-1.5",
};

export function Badge({ 
  children, 
  variant = "default", 
  size = "md", 
  dot = false,
  className 
}: BadgeProps) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 rounded-full border uppercase tracking-wide font-medium transition",
        variantStyles[variant],
        sizeStyles[size],
        className
      )}
    >
      {dot && (
        <span 
          className={cn(
            "rounded-full animate-pulse",
            size === "sm" ? "h-1.5 w-1.5" : "h-2 w-2",
            variant === "success" ? "bg-[var(--positive)]" :
            variant === "warning" ? "bg-[var(--warning)]" :
            variant === "danger" ? "bg-[var(--negative)]" :
            "bg-[var(--accent)]"
          )}
        />
      )}
      {children}
    </span>
  );
}





