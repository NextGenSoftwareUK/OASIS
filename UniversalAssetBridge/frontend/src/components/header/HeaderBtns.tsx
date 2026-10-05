"use client";

import Link from "next/link";
import Image from "next/image";
import Cookies from "js-cookie";
import { useEffect, useState } from "react";
import { useUserStore } from "@/store/useUserStore";
import { useRouter } from "nextjs-toploader/app";
import { useWalletStore } from "@/store/useWalletStore";
import { Button, buttonVariants } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

export default function HeaderBtns() {
  const router = useRouter();
  const { user, logout, setUser } = useUserStore();
  const { disconnectWallet } = useWalletStore();
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const storedUser = localStorage.getItem("user");
    if (storedUser) {
      setUser(JSON.parse(storedUser));
    }
    setLoading(false);
  }, [setUser]);

  useEffect(() => {
    if (!Cookies.get("oasisToken")) {
      setUser(null);
    }
  }, [Cookies.get("oasisToken")]);

  if (loading) return <Skeleton className="h-10 w-36 bg-primary" />;

  return (
    <>
      {!user ? (
        <>
          <Link
            href="/signin"
            className={`${buttonVariants({ variant: "default", size: "lg" })} !px-5`}
          >
            Sign In
          </Link>
          <Link
            href="/signup"
            className={`${buttonVariants({ variant: "muted", size: "lg" })} !px-5`}
          >
            Get Started
          </Link>
        </>
      ) : (
        <>
          <Link
            href="/profile"
            className={`${buttonVariants({ variant: "default", size: "lg" })} !px-5 flex gap-2 items-center`}
          >
            <Image
              src="/profile.svg"
              alt={user.UserName}
              width={21}
              height={21}
            />
            {user.UserName}
          </Link>
          {/* <Button
            variant="gray"
            size="icon"
            onClick={() => {
              logout();
              disconnectWallet();
              localStorage.removeItem("user");
              router.push("/");
            }}
          >
            <Image src="/logout.svg" alt="Logout" width={24} height={24} />
          </Button> */}
        </>
      )}
    </>
  );
}
