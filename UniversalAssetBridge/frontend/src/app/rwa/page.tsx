import Header from "@/components/header/Header";
import RWATable from "./components/RWATable";
import { SearchParams } from "@/types/params.type";
import { Suspense } from "react";

export default function page({ searchParams }: SearchParams) {
  return (
    <>
      <Header searchParams={searchParams} />
      <div className="pb-10 md:py-10 xl:px-5 md:px-0! text-white">
        <div className="mx-auto">
          <Suspense fallback={<div>Loading...</div>}>
            <RWATable />
          </Suspense>
        </div>
      </div>
    </>
  );
}
