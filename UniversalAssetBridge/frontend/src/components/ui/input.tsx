import * as React from "react";

import { cn } from "@/lib/utils";

interface InputProps extends React.ComponentProps<"input"> {
  icon?: React.ReactNode;
  iconPosition?: "left" | "right";
  isDark?: boolean;
}

const Input = React.forwardRef<HTMLInputElement, InputProps>(
  ({ className, type, icon, iconPosition, isDark = false, ...props }, ref) => {
    return (
      <div className="relative flex w-full items-center">
        {icon && (
          <div
            className={`absolute top-1/2 -translate-y-1/2 ${iconPosition === "left" && "left-5"} ${iconPosition === "right" && "right-5"}`}
          >
            {icon}
          </div>
        )}
        <input
          onWheel={(e) => {
            if (type === "number") e.currentTarget.blur();
          }}
          type={type}
          className={cn(
            `flex px-4 py-2.5 w-full text-black rounded-md border border-input bg-muted text-base shadow-xs transition-[color,box-shadow] focus-visible:border-ring file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:outline-hidden focus-visible:ring-[3px] focus-visible:ring-ring/40 aria-invalid:ring-destructive/20 aria-invalid:border-destructive disabled:cursor-not-allowed disabled:opacity-80 disabled:bg-muted md:text-sm 
            ${icon && iconPosition === "left" && "left-5"} ${iconPosition === "right" && "right-5"}
            ${isDark && "text-white bg-accent-foreground placeholder:text-secondary/80 border-input-dark"}`,
            className
          )}
          ref={ref}
          {...props}
        />
      </div>
    );
  }
);
Input.displayName = "Input";

export { Input };
