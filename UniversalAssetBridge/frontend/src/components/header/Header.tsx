"use server";

import Link from "next/link";
// import RwaLink from "@/components/RwaLink";
import CreateRwaLink from "@/components/header/CreateRwaLink";
import WalletConnect from "@/components/WalletConnect";
import HeaderBtns from "@/components/header/HeaderBtns";
import SignInModal from "@/components/SignInModal";
import SignUpModal from "@/components/SignUpModal";
import MobileHeader from "@/components/header/MobileHeader";
import { SearchParams } from "@/types/params.type";

export default async function Header({ searchParams }: SearchParams) {
  const { signin, signup } = await searchParams;

  return (
    <>
      <header className="flex justify-between items-center text-white pt-6 pb-5 mb-10 w-full xl:px-5 lg:hidden">
        <div className="flex items-center gap-7">
          <Link href="/" className="text-2xl font-black mr-12">
            Quantum Street
          </Link>
          <ul className="flex items-center gap-5 lg:gap-3">
            <li className="">
              <Link href="/" className="hover:text-[var(--oasis-accent)] transition">Bridge</Link>
            </li>
            <li className="">
              <Link href="/token-portal" className="hover:text-[var(--oasis-accent)] transition">Token Portal</Link>
            </li>
            <li className="">
              <Link href="/mint-token" className="hover:text-[var(--oasis-accent)] transition">Create Token</Link>
            </li>
            <li className="">
              <Link href="/migrate-token" className="hover:text-[var(--oasis-accent)] transition">Migrate Token</Link>
            </li>
            <li className="">
              <Link href="/docs" className="hover:text-[var(--oasis-accent)] transition">Docs</Link>
            </li>
          </ul>
        </div>
        <div className="flex gap-[10px] sm:gap-2">
          <CreateRwaLink />
          <WalletConnect />
          <HeaderBtns />
        </div>
        {signin && <SignInModal />}
        {signup && <SignUpModal />}
      </header>
      {/* Mobile Header */}
      <MobileHeader signin={signin} signup={signup} />
    </>
  );
}
