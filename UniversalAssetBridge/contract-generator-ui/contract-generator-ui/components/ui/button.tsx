import * as React from "react"
import { cn } from "@/lib/utils"

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "default" | "secondary" | "outline" | "ghost"
  size?: "default" | "sm" | "lg"
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "default", size = "default", ...props }, ref) => {
    return (
      <button
        className={cn(
          "inline-flex items-center justify-center gap-2 rounded-xl font-semibold transition-all focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--accent)] focus-visible:ring-offset-2 disabled:pointer-events-none disabled:opacity-50",
          {
            "bg-[var(--accent)] text-[#041321] hover:bg-[var(--accent)]/90 shadow-lg shadow-[var(--accent)]/20":
              variant === "default",
            "bg-[rgba(34,211,238,0.15)] text-[var(--accent)] border border-[var(--accent)]/30 hover:bg-[rgba(34,211,238,0.25)]":
              variant === "secondary",
            "border border-[var(--card-border)] bg-transparent text-[var(--foreground)] hover:bg-[var(--accent-soft)]":
              variant === "outline",
            "text-[var(--muted)] hover:text-[var(--foreground)] hover:bg-[rgba(8,11,26,0.6)]":
              variant === "ghost",
          },
          {
            "h-10 px-4 py-2 text-sm": size === "default",
            "h-8 px-3 text-xs": size === "sm",
            "h-12 px-6 text-base": size === "lg",
          },
          className
        )}
        ref={ref}
        {...props}
      />
    )
  }
)
Button.displayName = "Button"

export { Button }


