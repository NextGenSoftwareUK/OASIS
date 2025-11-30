"use server";

import Header from "@/components/header/Header";
import RwaData from "./components/RwaData";
import { SearchParams } from "@/types/params.type";

export default async function page({ searchParams }: SearchParams) {
  return (
    <>
      <Header searchParams={searchParams} />
      <div className="mt-16 md:mt-10 xl:px-5 md:px-0! lg:mt-10 sm:mb-0! sm:pb-5">
        <div className="mx-auto">
          <RwaData />
        </div>
      </div>
    </>
  );
}
