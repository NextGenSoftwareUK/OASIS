"use client";

import Link from "next/link";
import Image from "next/image";
import MobileHeaderBtns from "./MobileHeaderBtns";
import WalletConnect from "@/components/WalletConnect";
import SignInModal from "@/components/SignInModal";
import SignUpModal from "@/components/SignUpModal";
import { useEffect, useRef, useState } from "react";
import {
  ArrowLeftRight,
  ChartCandlestick,
  ListCheck,
  ListPlus,
  Menu,
} from "lucide-react";
import { useRouter } from "nextjs-toploader/app";
import { useUserStore } from "@/store/useUserStore";
import { Button, buttonVariants } from "@/components/ui/button";

interface MobileHeaderProps {
  signin?: string;
  signup?: string;
}

export default function MobileHeader({ signin, signup }: MobileHeaderProps) {
  const [isOpen, setIsOpen] = useState(false);
  const menuRef = useRef<HTMLDivElement>(null);
  const router = useRouter();
  const { user, logout, setUser } = useUserStore();

  useEffect(() => {
    const storedUser = localStorage.getItem("user");
    if (storedUser) {
      setUser(JSON.parse(storedUser));
    }
  }, [setUser]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        isOpen &&
        menuRef.current &&
        !menuRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [isOpen]);

  return (
    <>
      <header className="justify-between items-center py-4 px-5 w-full text-white hidden lg:flex md:px-0!">
        <Link href="/" className="text-lg font-semibold">
          Quantum Street Bridge
        </Link>
        <button onClick={() => setIsOpen(true)} aria-label="Open menu">
          <Menu className="w-6 h-6" />
        </button>
      </header>

      {/* Sidebar / Drawer */}
      <div
        className={`fixed -left-full top-0 bottom-0 z-50 bg-black/60 backdrop-blur-xs transition-all ${
          isOpen && "right-0! left-0!"
        }`}
      >
        <div
          ref={menuRef}
          className={`absolute -left-full top-0 h-full w-4/5 bg-white text-black py-4 px-2 shadow-xl transition-all
          ${isOpen && "left-0!"}`}
        >
          <nav className="flex flex-col gap-2">
            <MobileHeaderBtns />
            <WalletConnect className="w-full justify-start!" />
            <div
              className={`${buttonVariants({
                variant: "empty",
                size: "default",
              })} flex-col h-auto gap-3 px-5! py-3!`}
            >
              <p className="p w-full mb-2 font-semibold">Web4 Platforms</p>
              <Link href="/" className="w-full flex gap-2 items-center">
                <ArrowLeftRight size={5} strokeWidth={1} className="mr-1" />
                Universal Bridge
              </Link>
              <Link href="/token-portal" className="w-full flex gap-2 items-center">
                <ChartCandlestick size={5} strokeWidth={1} className="mr-1" />
                Token Portal
              </Link>
              <Link href="/mint-token" className="w-full flex gap-2 items-center">
                <ListPlus size={5} strokeWidth={1} className="mr-1" />
                Create Token
              </Link>
              <Link href="/migrate-token" className="w-full flex gap-2 items-center">
                <ListCheck size={5} strokeWidth={1} className="mr-1" />
                Migrate Token
              </Link>
              <Link href="/docs" className="w-full flex gap-2 items-center">
                <ListCheck size={5} strokeWidth={1} className="mr-1" />
                Documentation
              </Link>
            </div>
          </nav>
          {user && (
            <Button
              variant="gray"
              size="default"
              className="mt-2 justify-between! w-full"
              onClick={() => {
                logout();
                localStorage.removeItem("user");
                router.push("/");
              }}
            >
              Logout
              <Image src="/logout.svg" alt="Logout" width={20} height={20} />
            </Button>
          )}
        </div>
      </div>

      {signin && <SignInModal />}
      {signup && <SignUpModal />}
    </>
  );
}
