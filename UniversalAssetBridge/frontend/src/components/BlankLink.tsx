import { cn } from "@/lib/utils";
import { ArrowUpRight } from "lucide-react";
import Link from "next/link";

interface BlankLinkProps {
  href: string;
  children: React.ReactNode;
  className?: string;
  size: number;
  color?: string;
}

export default function BlankLink({
  href,
  children,
  className,
  size,
  color,
}: BlankLinkProps) {
  return (
    <Link
      href={href}
      target="_blank"
      rel="noopener noreferrer"
      className={cn("inline-flex gap-1 items-center", className)}
    >
      {children}
      <ArrowUpRight className="inline" size={size} color={color} />
    </Link>
  );
}
