"use client";

import type { ButtonHTMLAttributes, DetailedHTMLProps } from "react";
import { cn } from "@/lib/utils";

const baseStyles = "inline-flex items-center justify-center gap-2 rounded-md px-4 py-2 text-sm font-medium transition focus:outline-none focus-visible:ring-2 focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50";

const variantStyles: Record<ButtonProps["variant"], string> = {
  primary:
    "bg-[var(--accent)] text-[#041321] shadow-[0_20px_40px_rgba(34,211,238,0.25)] hover:bg-[#38e0ff] focus-visible:ring-[var(--accent)] focus-visible:ring-offset-0",
  outline:
    "border border-[var(--color-card-border)]/70 bg-transparent text-[var(--foreground)] hover:border-[var(--accent)] hover:text-[var(--accent)]",
  ghost:
    "text-[var(--muted)] hover:text-[var(--accent)] hover:bg-[var(--accent-soft)]",
  secondary:
    "bg-[rgba(12,16,34,0.85)] text-[var(--color-foreground)] border border-[var(--color-card-border)]/60 hover:border-[var(--accent)]/50",
  toggle:
    "bg-[rgba(8,12,28,0.75)] text-[var(--muted)] border border-[var(--color-card-border)]/40 data-[state=active]:bg-[var(--accent)] data-[state=active]:text-[#041321] data-[state=active]:border-transparent hover:border-[var(--accent)]/50",
};

export type ButtonProps = DetailedHTMLProps<
  ButtonHTMLAttributes<HTMLButtonElement>,
  HTMLButtonElement
> & {
  variant?: "primary" | "outline" | "ghost" | "secondary" | "toggle";
};

export function Button({ className, variant = "primary", "data-state": dataState, ...props }: ButtonProps) {
  return (
    <button
      className={cn(baseStyles, variantStyles[variant], className)}
      data-state={dataState}
      {...props}
    />
  );
}
