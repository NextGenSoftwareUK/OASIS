import { ReactNode } from "react";
import { cn } from "@/lib/utils";

export type StatCardProps = {
  label: string;
  value: string | number;
  description?: string;
  icon?: ReactNode;
  variant?: "default" | "success" | "warning" | "danger";
  className?: string;
  size?: "default" | "compact";
  trend?: "up" | "down" | "neutral";
  trendValue?: string;
};

const variantClasses: Record<Required<StatCardProps>["variant"], string> = {
  default:
    "border-[var(--color-card-border)] bg-[rgba(8,11,26,0.85)] text-[var(--color-foreground)]",
  success: "border-[var(--color-positive)]/40 bg-[rgba(15,118,110,0.2)] text-[var(--color-foreground)]",
  warning: "border-[var(--color-warning)]/40 bg-[rgba(180,132,45,0.2)] text-[var(--color-foreground)]",
  danger: "border-[var(--negative)]/40 bg-[rgba(180,45,60,0.2)] text-[var(--color-foreground)]",
};

const trendIcons = {
  up: "↗",
  down: "↘",
  neutral: "→",
};

export function StatCard({ 
  label, 
  value, 
  description, 
  icon, 
  variant = "default", 
  className, 
  size = "default",
  trend,
  trendValue,
}: StatCardProps) {
  const isCompact = size === "compact";
  
  return (
    <div
      className={cn(
        "rounded-2xl border shadow-[0_20px_45px_rgba(13,148,136,0.15)] backdrop-blur-xl transition hover:shadow-[0_25px_50px_rgba(13,148,136,0.2)]",
        isCompact ? "p-4" : "p-5",
        variantClasses[variant],
        className
      )}
    >
      <div className="flex items-start justify-between">
        <div className="flex-1">
          <p
            className={cn(
              "uppercase tracking-wider text-[var(--muted)]",
              isCompact ? "text-[10px]" : "text-xs"
            )}
          >
            {label}
          </p>
          <p
            className={cn(
              "mt-2 font-semibold tracking-tight",
              isCompact ? "text-lg" : "text-2xl"
            )}
          >
            {value}
          </p>
          {(trend || trendValue) && (
            <p className={cn(
              "mt-1 text-xs flex items-center gap-1",
              trend === "up" ? "text-[var(--positive)]" : 
              trend === "down" ? "text-[var(--negative)]" : 
              "text-[var(--muted)]"
            )}>
              {trend && <span>{trendIcons[trend]}</span>}
              {trendValue && <span>{trendValue}</span>}
            </p>
          )}
        </div>
        {icon && <div className="text-[var(--accent)]">{icon}</div>}
      </div>
      {description && (
        <p
          className={cn(
            "text-[var(--muted)]",
            isCompact ? "mt-2 text-xs" : "mt-3 text-sm"
          )}
        >
          {description}
        </p>
      )}
    </div>
  );
}

