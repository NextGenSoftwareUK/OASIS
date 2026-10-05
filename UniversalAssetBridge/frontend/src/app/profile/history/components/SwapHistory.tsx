"use client";

import { buttonVariants } from "@/components/ui/button";
import Image from "next/image";
import Link from "next/link";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";

const tableHead = [
  "From token",
  "From amount",
  "To token",
  "Received",
  "Recepient address",
  "Rate (1 token)",
  "Date",
];

const swaps = [
  {
    fromToken: "SOL",
    fromAmount: "3.545635",
    toToken: "XRD",
    received: "12352.635",
    recipient: "nfmdspjIFnv438ndu",
    rate: "12.5686",
    date: "2023-08-05",
  },
  {
    fromToken: "XRD",
    fromAmount: "3.545635",
    toToken: "SOL",
    received: "12352.635",
    recipient: "nfmdspjIFnv438ndu",
    rate: "12.5686",
    date: "2023-08-05",
  },
  {
    fromToken: "SOL",
    fromAmount: "3.545635",
    toToken: "XRD",
    received: "1252.635",
    recipient: "nfmdspjIFnv438ndu",
    rate: "12.5686",
    date: "2023-08-05",
  },
];

export function SwapHistory() {
  if (!swaps)
    return (
      <div className="my-12.5 flex flex-col items-center">
        <Image
          src="/tumbleweed.png"
          alt="tumbleweed"
          width={80}
          height={80}
          priority
          className="opacity-80"
        />
        <p className="p-sm text-secondary text-center max-w-[190px] mt-3.5 mb-5">
          You haven't made a swap yet.
        </p>

        <div className="space-x-3.5">
          <Link
            href="/"
            className={`${buttonVariants({ variant: "muted", size: "lg" })}`}
          >
            Make Swap
          </Link>
        </div>
      </div>
    );

  return (
    <Table className="mt-4">
      <TableHeader>
        <TableRow className="border-white/30 hover:bg-transparent">
          {tableHead.map((item, i) => (
            <TableHead className="text-white font-semibold first:pl-0" key={i}>
              {item}
            </TableHead>
          ))}
        </TableRow>
      </TableHeader>
      <TableBody>
        {swaps.map((swap, i) => (
          <TableRow className="border-white/30 hover:bg-transparent" key={i}>
            {Object.entries(swap).map(([key, value], j) => (
              <TableCell className="py-4 first:pl-0" key={j}>
                <div className="flex gap-2.5">
                  {key === "fromToken" && (
                    <Image
                      src={`/icons/${value}-black.svg`}
                      alt={value}
                      width={16}
                      height={16}
                      className="invert"
                    />
                  )}
                  {key === "toToken" && (
                    <Image
                      src={`/icons/${value}-black.svg`}
                      alt={value}
                      width={16}
                      height={16}
                      className="invert"
                    />
                  )}
                  {value}
                </div>
              </TableCell>
            ))}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
