"use client";

import Link from "next/link";
import { useUserStore } from "@/store/useUserStore";

export default function RwaLink() {
  const { user } = useUserStore();
  return <Link href={`${user ? "/rwa" : "/signin"}`}>RWA Market</Link>;
}
