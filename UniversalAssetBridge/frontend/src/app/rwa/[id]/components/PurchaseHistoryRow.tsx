import { TableRow, TableCell } from "@/components/ui/table";
import { shortAddress } from "@/utils/shortSomething";
import { format } from "date-fns";
import { RwaPurchaseHistoryProps } from "@/types/rwa/rwaProps.type";

interface RwaPurchaseHistoryRowProps {
  row: RwaPurchaseHistoryProps;
}

export default function RwaPurchasHistoryeRow({
  row,
}: RwaPurchaseHistoryRowProps) {
  return (
    <TableRow className="hover:bg-transparent">
      <TableCell
        className={`px-0 ${
          row.type === "Buy" ? "text-rose-400" : "text-emerald-400"
        }`}
      >
        {row.type}
      </TableCell>
      <TableCell className="py-5 px-0">{row.price}</TableCell>
      <TableCell className="py-5 px-0">zBTC</TableCell>
      <TableCell className="py-5 px-0">
        {shortAddress(row.buyerPublicKey)}
      </TableCell>
      <TableCell className="text-zinc-500 py-5 px-0 text-right">
        {format(new Date(row.transactionDate), "yyyy-MM-dd")}
      </TableCell>
    </TableRow>
  );
}
