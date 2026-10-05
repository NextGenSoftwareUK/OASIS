"use client";

import Link from "next/link";
import { useUserStore } from "@/store/useUserStore";
import { cn } from "@/lib/utils";
import { buttonVariants } from "@/components/ui/button";

interface CreateRwaLinkProps {
  className?: string;
}

export default function CreateRwaLink({ className }: CreateRwaLinkProps) {
  const { user } = useUserStore();

  if (user)
    return (
      <Link
        href={`${user ? "/rwa/create" : "/signin"}`}
        className={cn(
          `${buttonVariants({ variant: "muted", size: "lg" })} !px-5`,
          className
        )}
      >
        Create RWA
      </Link>
    );
}
