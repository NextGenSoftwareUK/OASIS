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
import { shortDescription } from "@/utils/shortSomething";

const tableHead = ["RWA", "Price", "Property type", "Type", "Date"];

const rwas = [
  {
    rwa: "Cedar Heights Mall",
    description: "Lorem ipsum dolor it ammet consecuert osibay in que sui",
    image: "/nft.avif",
    price: "0.563 zBTC",
    propertyType: "Real Estate",
    type: "Buy",
    date: "2025-08-31",
  },
  {
    rwa: "The Mall of America",
    description: "Lorem ipsum dolor it ammet consecuert osibay in que sui",
    image: "/nft.avif",
    price: "1.765 zBTC",
    propertyType: "Real Estate",
    type: "Buy",
    date: "2025-08-31",
  },
  {
    rwa: "St. Louis Zoo",
    description: "Lorem ipsum dolor it ammet consecuert osibay in que sui",
    image: "/nft.avif",
    price: "2.63 zBTC",
    propertyType: "Real Estate",
    type: "Buy",
    date: "2025-08-31",
  },
];

export function RwaHistory() {
  if (!rwas)
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
          You haven't bought, sold or created an RWA yet.
        </p>

        <div className="space-x-3.5">
          <Link
            href="/rwa"
            className={`${buttonVariants({ variant: "default", size: "lg" })}`}
          >
            RWA Market
          </Link>
          <Link
            href="/rwa/create"
            className={`${buttonVariants({ variant: "muted", size: "lg" })}`}
          >
            Create RWA
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
        {rwas.map((rwa, i) => (
          <TableRow className="border-white/30 hover:bg-transparent" key={i}>
            <TableCell className="text-white pl-0 flex items-center gap-3.5">
              <img src={rwa.image} alt={rwa.rwa} className="w-10 h-10" />
              <div className="">
                <p className="p-sm">{rwa.rwa}</p>
                <p className="p-sm text-secondary">
                  {shortDescription(rwa.description, 40)}
                </p>
              </div>
            </TableCell>
            <TableCell className="text-white">{rwa.price}</TableCell>
            <TableCell className="text-white">{rwa.propertyType}</TableCell>
            <TableCell className="text-white">{rwa.type}</TableCell>
            <TableCell className="text-white">{rwa.date}</TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
