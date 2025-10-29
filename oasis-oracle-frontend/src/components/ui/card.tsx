import { ReactNode } from "react";
import { cn } from "@/lib/utils";

export type CardProps = {
  children: ReactNode;
  title?: string;
  description?: string;
  headerAction?: ReactNode;
  footer?: ReactNode;
  variant?: "default" | "glass" | "gradient";
  className?: string;
};

const variantStyles = {
  default: "bg-[rgba(8,11,26,0.85)] border-[var(--color-card-border)]/50",
  glass: "bg-[rgba(6,11,26,0.7)] border-[var(--color-card-border)]/50 backdrop-blur-xl",
  gradient: "bg-gradient-to-br from-[rgba(8,11,26,0.95)] to-[rgba(15,23,42,0.85)] border-[var(--color-card-border)]/60",
};

export function Card({ 
  children, 
  title, 
  description, 
  headerAction, 
  footer, 
  variant = "default",
  className 
}: CardProps) {
  return (
    <div
      className={cn(
        "rounded-2xl border shadow-[0_15px_30px_rgba(15,118,110,0.18)] transition hover:shadow-[0_20px_40px_rgba(15,118,110,0.25)]",
        variantStyles[variant],
        className
      )}
    >
      {(title || description || headerAction) && (
        <div className="flex items-start justify-between border-b border-[var(--color-card-border)]/30 p-6">
          <div>
            {title && (
              <h3 className="text-lg font-semibold text-[var(--color-foreground)]">
                {title}
              </h3>
            )}
            {description && (
              <p className="mt-1 text-sm text-[var(--muted)]">{description}</p>
            )}
          </div>
          {headerAction && <div>{headerAction}</div>}
        </div>
      )}
      <div className="p-6">{children}</div>
      {footer && (
        <div className="border-t border-[var(--color-card-border)]/30 p-6">
          {footer}
        </div>
      )}
    </div>
  );
}


