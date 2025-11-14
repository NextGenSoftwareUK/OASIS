"use client";

import Loading from "@/components/Loading";
import Chart from "@/components/Chart";
import PurchaseButton from "@/components/PurchaseButton";
import CopyIpfsButton from "@/app/rwa/components/CopyIpfsButton";
import AllRwaData from "@/components/AllRwaData";
import SellBuyData from "./SellBuyData";
import InfoRow from "@/app/rwa/components/InfoRow";
import Link from "next/link";
import { useRwaData } from "@/hooks/useRwaData";
import { buttonVariants, Button } from "@/components/ui/button";
import { SOLANA_ENVIRONMENT } from "@/lib/constants";
import { useState } from "react";
import { useParams } from "next/navigation";

export default function RwaData() {
  const params = useParams<{ id: string }>();
  const [isAlldataOpen, setIsAlldataOpen] = useState(false);
  const { rwaData, rwaChanges, isLoading } = useRwaData(params.id);

  if (isLoading || !rwaData) {
    return (
      <Loading
        className="flex justify-center mt-14"
        classNameLoading="border-white! border-r-transparent! w-14! h-14!"
      />
    );
  }

  const ipfsCID = rwaData.image.replace("https://ipfs.io/ipfs/", "");

  return (
    <>
      <div className="grid grid-cols-3 gap-10 lg:flex lg:flex-col-reverse">
        <Chart
          firstData={rwaData}
          data={rwaChanges}
          className="col-span-2 lg:col-span-1 lg:order-1"
        />

        <div className="flex flex-col gap-5 text-white col-span-1 lg:flex-row lg:order-3 sm:flex-col">
          <div className="flex gap-5 shrink-0 lg:flex-col sm:w-full">
            <img
              src={rwaData.image}
              alt={rwaData.title}
              className="w-[100px] rounded-2xl lg:w-[200px] sm:w-full sm:aspect-square"
            />
            <div className="sm:flex sm:justify-between sm:mt-3 sm:mb-1">
              <h3 className="h1 sm:text-2xl">{rwaData.title}</h3>
              <p className="p text-text-gray hidden sm:block">Price</p>
              <h2 className="h1 text-green-500 font-bold sm:text-2xl">
                {rwaData.price} zBTC
              </h2>
            </div>
          </div>

          <div className="w-full flex flex-col gap-2">
            <span className="text-text-gray text-sm">Asset Description:</span>
            <p>{rwaData.assetDescription}</p>

            <InfoRow
              label="Unique Identifier"
              value={rwaData.uniqueIdentifier}
            />
            <InfoRow label="Royalty" value={`${rwaData.royalty}%`} />
            <InfoRow label="Network" value={rwaData.network} />
            <InfoRow
              label="IPFS CID"
              value={<CopyIpfsButton cid={ipfsCID} />}
            />

            <Button
              variant="gray"
              onClick={() => setIsAlldataOpen(true)}
              className="w-full"
            >
              Show all information
            </Button>

            <Link
              href={`https://explorer.solana.com/address/${rwaData.mintAccount}${
                SOLANA_ENVIRONMENT === "devnet" ? "?cluster=devnet" : ""
              }`}
              target="_blank"
              className={buttonVariants({ variant: "empty", size: "lg" })}
            >
              Check in Solana Explorer
            </Link>

            <PurchaseButton tokenId={params.id} />
          </div>
        </div>

        {isAlldataOpen && (
          <AllRwaData setIsOpen={setIsAlldataOpen} data={rwaData} />
        )}
      </div>

      <SellBuyData tokenId={params.id} />
    </>
  );
}
