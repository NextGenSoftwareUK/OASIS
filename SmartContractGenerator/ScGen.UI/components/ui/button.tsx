import * as React from "react"
import { cn } from "@/lib/utils"

export interface ButtonProps
  extends React.ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: "default" | "secondary" | "outline" | "ghost"
  size?: "default" | "sm" | "lg"
}

const Button = React.forwardRef<HTMLButtonElement, ButtonProps>(
  ({ className, variant = "default", size = "default", ...props }, ref) => {
    // Map variants to portal button classes
    const variantClass = {
      default: "btn-primary",
      secondary: "btn-secondary",
      outline: "btn-secondary",
      ghost: "btn-text",
    }[variant];
    
    return (
      <button
        className={cn(
          variantClass,
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


