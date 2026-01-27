"use server";

import CreateRwa from "@/app/rwa/create/components/CreateRwa";
import Steps from "@/app/rwa/create/components/Steps";
import Header from "@/components/header/Header";
import { SearchParams } from "@/types/params.type";

export default async function page({ searchParams }: SearchParams) {
  return (
    <>
      <Header searchParams={searchParams} />
      <div className="mt-20 lg:mt-16 md:mt-10! xxs:mt-5!">
        <div className="grid grid-cols-11 gap-20 mb-10">
          <div className="col-span-5">
            <Steps />
          </div>
          <div className="col-span-6">
            <h1 className="text-5xl font-semibold leading-14">
              Create Your Decentralized Trust Agreement RWA
            </h1>
            <p className="p-sm text-secondary mt-4 max-w-[600px]">
              Lorem ipsum dolor sit amet consectetur. Vestibulum magna quis
              tincidunt libero non ornare semper. Et ornare risus purus risus a.
            </p>
          </div>
        </div>
        <CreateRwa />
      </div>
    </>
  );
}
