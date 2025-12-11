"use client";

import PurchaseButton from "@/components/PurchaseButton";
import Link from "next/link";
import Filters from "./Filters";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { shortDescription } from "@/utils/shortSomething";
import { buttonVariants } from "@/components/ui/button";
import { PaginationButtons } from "@/components/PaginationButtons";
import { useUserStore } from "@/store/useUserStore";
import { useSearchParams } from "next/navigation";
import { useRwasData } from "@/hooks/useRwasData";
import { CombinedRwa } from "@/types/rwa/rwa.type";
import { PriceChangeIndicator } from "@/app/rwa/components/PriceChangeIndicator";
import { Loader2 } from "lucide-react";
import TopBar from "@/app/rwa/components/TopBar";
import { useState } from "react";

export default function RWATable() {
  const searchParams = useSearchParams();
  const initialPage = parseInt(searchParams.get("page") || "1");

  const [hideFilters, setHideFilers] = useState(false);

  const { user } = useUserStore();

  const { rwas, isSomeFetching, combinedRwas, reqParams, setReqParams } =
    useRwasData(initialPage);

  return (
    <>
      <TopBar hideFilters={hideFilters} setHideFilers={setHideFilers} />
      <div
        className={`flex gap-10 mt-10 transition-all duration-400 ease-in-out ${hideFilters && "!gap-0"}`}
      >
        <div
          className={`mb-5 text-sm transition-all duration-400 ease-in-out overflow-hidden 
            ${hideFilters && "!w-0"}`}
        >
          <Filters setReqParams={setReqParams} />
        </div>
        {isSomeFetching ? (
          <Loader2
            className={`animate-spin text-center mt-14 w-[960px] ${hideFilters && "w-full"}`}
            size={60}
            strokeWidth={3}
          />
        ) : (
          <>
            {combinedRwas.length ? (
              <Table className={`min-w-[965px] ${hideFilters && "w-full"}`}>
                <TableHeader>
                  <TableRow className="hover:bg-transparent border-primary">
                    <TableHead colSpan={2} className="w-[100px]">
                      RWA
                    </TableHead>
                    <TableHead className="text-right">Network</TableHead>
                    <TableHead className="text-right">Price</TableHead>
                    <TableHead className="text-right">Asset Type</TableHead>
                    <TableHead className="text-right">
                      Price Change (%)
                    </TableHead>
                    <TableHead className="text-right"></TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {combinedRwas.map((rwa: CombinedRwa) => {
                    return (
                      <TableRow
                        key={rwa.tokenId}
                        className="border-primary hover:bg-transparent"
                      >
                        <TableCell colSpan={2} className="font-medium py-3">
                          <div className="">
                            <Link
                              className="flex gap-3 items-center"
                              href={`/rwa/${rwa.tokenId}`}
                            >
                              <img
                                src={rwa.image}
                                alt={rwa.title}
                                className="rounded-md w-[50px] h-[50px]"
                              />
                              <div className="flex flex-col">
                                <p className="p">{rwa.title}</p>
                                <p className="text-text-gray">
                                  {shortDescription(rwa.assetDescription)}
                                </p>
                              </div>
                            </Link>
                          </div>
                        </TableCell>
                        <TableCell className="text-right">
                          {rwa.network}
                        </TableCell>
                        <TableCell className="text-right">
                          {rwa.price} zBTC
                        </TableCell>
                        <TableCell className="text-right">
                          {rwa.assetType}
                        </TableCell>
                        <TableCell className="text-right">
                          <PriceChangeIndicator
                            price={rwa.price}
                            oldPrice={rwa.oldPrice}
                          />
                        </TableCell>
                        <TableCell className="text-right space-x-2">
                          {rwa.ownerUsername === user?.UserName && (
                            <Link
                              href={`/rwa/${rwa.tokenId}/update`}
                              className={buttonVariants({
                                variant: "gray",
                                size: "sm",
                              })}
                            >
                              Update
                            </Link>
                          )}
                          <Link
                            href={`/rwa/${rwa.tokenId}`}
                            className={buttonVariants({
                              variant: "gray",
                              size: "sm",
                            })}
                          >
                            Details
                          </Link>
                          <PurchaseButton
                            usageInMarketPage={true}
                            tokenId={rwa.tokenId}
                          />
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            ) : (
              <>
                <h3
                  className={`h3 text-center mt-20 opacity-60 w-[960px] ${hideFilters && "w-full"}`}
                >
                  There are no RWAs available yet.
                </h3>
              </>
            )}
          </>
        )}
        {rwas?.data?.data?.totalPages > 1 && (
          <PaginationButtons
            className="mt-10"
            pages={rwas.data.data.totalPages}
            currentPage={reqParams.pageNumber}
            searchParams={searchParams}
          />
        )}
      </div>
    </>
  );
}
