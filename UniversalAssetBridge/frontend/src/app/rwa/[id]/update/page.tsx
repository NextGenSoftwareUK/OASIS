"use server";

import Header from "@/components/header/Header";
import ChangeRwa from "./components/ChangeRwa";
import { SearchParams } from "@/types/params.type";

export default async function page({ searchParams }: SearchParams) {
  return (
    <div>
      <Header searchParams={searchParams} />
      <div className="mt-24 xl:px-5 md:mt-14 md:px-0! sm:mt-10!">
        <div className="mx-auto">
          <ChangeRwa />
        </div>
      </div>
    </div>
  );
}
