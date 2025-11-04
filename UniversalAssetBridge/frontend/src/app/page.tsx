"use server";

import Header from "@/components/header/Header";
import BridgePageClient from "@/app/BridgePageClient";
import { SearchParams } from "@/types/params.type";

export default async function page({ searchParams }: SearchParams) {
  return (
    <div className="min-h-screen">
      <div className="relative overflow-hidden">
        <div className="absolute inset-x-0 top-0 h-72 bg-[radial-gradient(circle_at_top,rgba(34,211,238,0.35),transparent_60%)] blur-3xl" />
        <header className="relative z-10 border-b backdrop-blur-xl" style={{
          borderColor: 'rgba(56, 189, 248, 0.2)',
          background: 'rgba(5,5,16,0.85)'
        }}>
          <div className="max-w-[1300px] mx-auto md:px-5">
            <Header searchParams={searchParams} />
          </div>
        </header>
      </div>
      <div className="max-w-7xl mx-auto">
        <BridgePageClient />
      </div>
    </div>
  );
}
