import Link from "next/link";
import { Dispatch, SetStateAction, useMemo, useState } from "react";
import { handleCopy } from "@/utils/handleCopy.util";
import { shortAddress, shortDescription } from "@/utils/shortSomething";
import { tokenizeBaseSchemaFields } from "@/schemas/rwa/tokenizeBase.schema";
import { tokenizeRealEstateSchemaFields } from "@/schemas/rwa/tokenizeRealEstate.schema";
import { Button } from "@/components/ui/button";
import { Table, TableBody, TableCell, TableRow } from "@/components/ui/table";
import { Rwa } from "@/types/rwa/rwa.type";
import { X } from "lucide-react";

const fieldsMeta = [
  ...tokenizeBaseSchemaFields.flatMap(group => group.fields),
  ...tokenizeRealEstateSchemaFields.flatMap(group => group.fields),
  { name: "insuranceStatus", placeholder: "Insurance Status" },
  { name: "mintAccount", placeholder: "Mint Account" },
  { name: "transactionHash", placeholder: "Transaction Hash" },
  { name: "metadata", placeholder: "Metadata" },
];

interface AllRwaDataProps {
  data: Rwa;
  setIsOpen: Dispatch<SetStateAction<boolean>>;
}

export default function AllRwaData({ data, setIsOpen }: AllRwaDataProps) {
  const [copiedMap, setCopiedMap] = useState<Record<string, boolean>>({});
  const sortedData = useMemo(() => {
    if (!data) return {};
    const excluded = ["tokenId", "ownerEmail", "ownerUsername", "ownerContact"];
    return Object.fromEntries(
      Object.entries(data).filter(([key]) => !excluded.includes(key))
    );
  }, [data]);

  const getPlaceholder = (key: string) => {
    const field = fieldsMeta.find((item) => item.name === key);
    return field?.placeholder || key;
  };

  return (
    <div className="fixed inset-0 z-1000000 bg-background-website">
      <div className="w-full h-full overflow-y-auto p-10 md:p-5">
        <div className="max-w-[1300px] mx-auto">
          <div className="flex justify-between items-center mb-10 md:mb-5">
            <h2 className="h2 text-white">All RWA information</h2>
            <Button
              variant="empty"
              size="icon"
              onClick={() => setIsOpen(false)}
            >
              <X size={18} />
            </Button>
          </div>

          <Table className="text-white">
            <TableBody>
              {sortedData &&
                Object.entries(sortedData).map(
                  ([key, value]: [string, any]) => (
                    <TableRow
                      key={key}
                      className="hover:bg-transparent border-text-gray relative md:flex md:gap-2 md:justify-between"
                    >
                      <TableCell className="px-0">
                        {getPlaceholder(key)}
                      </TableCell>
                      {key === "geolocation" ? (
                        <TableCell className="px-0 text-right">
                          latitude: {value.latitude} longitude:{" "}
                          {value.longitude}
                        </TableCell>
                      ) : (
                        <>
                          {typeof value === "string" &&
                          value.includes("http") ? (
                            <TableCell className="px-0 text-right">
                              <div>
                                <Link
                                  target="_blank"
                                  className="px-0 text-right"
                                  href={value}
                                >
                                  {shortDescription(value, 25)}
                                </Link>
                              </div>
                            </TableCell>
                          ) : (
                            <>
                              {key === "mintAccount" ||
                              key === "transactionHash" ? (
                                <TableCell
                                  onClick={() =>
                                    handleCopy(value, { setCopiedMap, key })
                                  }
                                  className="px-0 text-right cursor-pointer block"
                                >
                                  {shortAddress(value)}
                                  {copiedMap[key] && (
                                    <span className="absolute right-0 -top-5 bg-white text-black text-xs px-2 py-1 rounded-md opacity-90 transition">
                                      Copied
                                    </span>
                                  )}
                                </TableCell>
                              ) : (
                                <TableCell className="px-0 text-right">
                                  {value}
                                </TableCell>
                              )}
                            </>
                          )}
                        </>
                      )}
                    </TableRow>
                  )
                )}
            </TableBody>
          </Table>
        </div>
      </div>
    </div>
  );
}
