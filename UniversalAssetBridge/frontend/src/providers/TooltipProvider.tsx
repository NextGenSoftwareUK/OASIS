import { TooltipProvider } from "@/components/ui/tooltip";

interface TooltipProviderProps {
  children: React.ReactNode;
}

export default function TooltipProv({ children }: TooltipProviderProps) {
  return <TooltipProvider delayDuration={0}>{children}</TooltipProvider>;
}
