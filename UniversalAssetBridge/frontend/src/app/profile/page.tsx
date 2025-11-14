"use server";

import Header from "@/components/header/Header";
import AccountAddresses from "@/app/profile/components/AccountAddresses";
import ChangePassword from "@/app/profile/components/ChangePassword";
import LinkedWallets from "@/app/profile/components/LinkedWallets";
import { SearchParams } from "@/types/params.type";
import PersonalnformationForm from "@/app/profile/components/form/PersonalnformationForm";

export default async function page({ searchParams }: SearchParams) {
  return (
    <div className="max-w-[550px]">
      {/* <Header searchParams={searchParams} /> */}
      {/* <div className="max-w-[512px] mx-auto mt-20 flex flex-col gap-7 md:mt-14 md:pb-5 sm:mt-10! xs:mt-7!">
        <AccountAddresses />
        <LinkedWallets />
        <ChangePassword />
      </div> */}
      <PersonalnformationForm />
    </div>
  );
}
