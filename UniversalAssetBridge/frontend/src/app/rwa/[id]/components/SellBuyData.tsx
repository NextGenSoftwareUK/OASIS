"use client";

import RwaPurchasHistoryeRow from "@/app/rwa/[id]/components/PurchaseHistoryRow";
import Loading from "@/components/Loading";
import { RwaPurchaseHistoryProps } from "@/types/rwa/rwaProps.type";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useGetRwaPurchaseHistory } from "@/requests/rwa/getRwaPurchaseHistory.request";

interface SellBuyDataProps {
  tokenId: string;
}

export default function SellBuyData({ tokenId }: SellBuyDataProps) {
  const { data, isFetching } = useGetRwaPurchaseHistory(tokenId);

  if (isFetching) {
    return (
      <div className="mt-16">
        <Loading
          className="flex justify-center"
          classNameLoading="border-white! border-r-transparent! w-10! h-10!"
        />
      </div>
    );
  }

  const purchases = data?.data as RwaPurchaseHistoryProps[];

  return (
    <div className="mt-16 text-white">
      <h3 className="h3">Sell / Buy History</h3>
      <Table className="mt-5 xs:min-w-[480px] xs:overflow-x-auto">
        <TableHeader>
          <TableRow className="hover:bg-transparent border-text-gray">
            {["Type", "Price", "Token", "Buyer", "Date"].map((col) => (
              <TableHead
                key={col}
                className="uppercase tracking-wider text-sm px-0"
              >
                {col}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {purchases?.length ? (
            purchases.map((row, i) => (
              <RwaPurchasHistoryeRow key={i} row={row} />
            ))
          ) : (
            <TableRow className="hover:bg-transparent">
              <TableCell
                colSpan={5}
                className="text-center py-8 px-0 text-text-gray"
              >
                No purchases or sales yet.
              </TableCell>
            </TableRow>
          )}
        </TableBody>
      </Table>
    </div>
  );
}
