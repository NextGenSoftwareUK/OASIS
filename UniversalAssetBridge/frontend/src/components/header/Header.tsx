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
          <ul className="flex items-center gap-7 lg:gap-3">
            <li className="">
              <Link href="/rwa">RWA Market</Link>
              {/* <RwaLink /> */}
            </li>
            <li className="">
              <Link href="/">Swap</Link>
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
