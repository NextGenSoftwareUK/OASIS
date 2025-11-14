import Header from "@/components/header/Header";
import PageTitle from "@/components/PageTitle";
import RwaTableMe from "./components/RwaTableMe";
import { SearchParams } from "@/types/params.type";
import { Suspense } from "react";

export default function page({ searchParams }: SearchParams) {
  return (
    <>
      <Header searchParams={searchParams} />
      <div className="pb-10 md:py-10 xl:px-5 md:px-0! text-white">
        <PageTitle title="My RWAs" />
        <Suspense fallback={<div>Loading...</div>}>
          <RwaTableMe />
        </Suspense>
      </div>
    </>
  );
}
