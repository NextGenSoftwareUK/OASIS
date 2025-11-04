"use client";

import React, { useEffect, useState } from "react";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { shortDescription } from "@/utils/shortSomething";
import { ChevronDown, ChevronUp } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { PaginationButtons } from "@/components/PaginationButtons";
import Loading from "@/components/Loading";
import Link from "next/link";
import { useUserStore } from "@/store/useUserStore";
import { useSearchParams } from "next/navigation";
import { useGetRwasMe } from "@/requests/rwa/getRwasMe.request";
import { RwaChanges, RwasReq } from "@/types/rwa/rwa.type";

export default function RwaTableMe() {
  const searchParams = useSearchParams();
  const initialPage = parseInt(searchParams.get("page") || "1");
  const { user } = useUserStore();
  const [reqParams, setReqParams] = useState<RwasReq>({
    pageSize: 10,
    pageNumber: initialPage,
  });

  const { data: rwas, isFetching: rwasFetching } = useGetRwasMe(
    reqParams,
    user?.token || ""
  );

  useEffect(() => {
    setReqParams((prev) => ({
      ...prev,
      pageNumber: initialPage,
    }));
  }, [initialPage]);

  return (
    <div>
      {rwasFetching ? (
        <Loading
          className="flex justify-center mt-14"
          classNameLoading="border-white! border-r-transparent! w-14! h-14!"
        />
      ) : (
        <>
          {rwas?.data?.data.length > 0 ? (
            <Table className="min-w-[965px]">
              <TableHeader>
                <TableRow className="hover:bg-transparent border-primary">
                  <TableHead colSpan={2} className="w-[100px]">
                    RWA
                  </TableHead>
                  <TableHead className="text-right">Network</TableHead>
                  <TableHead className="text-right">Price</TableHead>
                  <TableHead className="text-right">Asset Type</TableHead>
                  {/* <TableHead className="text-right">Geolocation</TableHead> */}
                  <TableHead className="text-right">Price Change (%)</TableHead>
                  <TableHead className="text-right"></TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {rwas?.data?.data.map((rwa: RwaChanges) => {
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
                              src={
                                rwa.image !== "string" ? rwa.image : "/nft.avif"
                              }
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
                        {/* <span className="text-red-600 text-xs flex items-center justify-end">
                        <span className="inline">
                          <ChevronDown size={15} />
                        </span>
                        {rwa.price.secondValue}
                      </span> */}
                      </TableCell>
                      <TableCell className="text-right">
                        {rwa.assetType}
                      </TableCell>
                      {/* <TableCell className="text-right">
                      {rwa.geolocation}
                    </TableCell> */}
                      <TableCell className="text-right">
                        {(rwa?.oldPrice && rwa?.oldPrice - rwa?.price) || (
                          <>
                            <p className="p opacity-60">---</p>
                          </>
                        )}
                        {rwa?.oldPrice &&
                          (() => {
                            const diff =
                              ((rwa.price - rwa.oldPrice) / rwa.oldPrice) * 100;
                            const isPositive = diff > 0;
                            const isNeutral = diff === 0;
                            const percentage = `${Math.abs(diff).toFixed(2)}%`;

                            return (
                              <span
                                className={`flex items-center text-xs justify-end ${
                                  isPositive
                                    ? "text-green-500"
                                    : isNeutral
                                      ? "text-text-gray"
                                      : "text-red-600"
                                }`}
                              >
                                {isPositive && (
                                  <ChevronUp size={15} className="inline" />
                                )}
                                {!isPositive && !isNeutral && (
                                  <ChevronDown size={15} className="inline" />
                                )}
                                {percentage}
                              </span>
                            );
                          })()}
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
                      </TableCell>
                    </TableRow>
                  );
                })}
              </TableBody>
            </Table>
          ) : (
            <>
              <h3 className="h3 text-center mt-20 opacity-60">
                There are no RWAs available yet.
              </h3>
            </>
          )}
        </>
      )}
      {rwas?.data?.totalPages > 1 && (
        <PaginationButtons
          className="mt-10"
          pages={rwas?.data.totalPages}
          currentPage={reqParams.pageNumber}
          searchParams={searchParams}
        />
      )}
    </div>
  );
}
