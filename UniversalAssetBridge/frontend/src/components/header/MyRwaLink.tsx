"use client";

import Link from "next/link";
import { useUserStore } from "@/store/useUserStore";

export default function MyRwaLink() {
  const { user } = useUserStore();
  return <Link href={`${user ? "/rwa/me" : "/signin"}`}>My RWAs</Link>;
}
