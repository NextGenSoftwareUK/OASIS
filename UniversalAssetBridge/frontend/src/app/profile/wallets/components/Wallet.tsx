import CopyBtn from "@/components/CopyBtn";
import { shortAddress } from "@/utils/shortSomething";
import Image from "next/image";

interface WalletProps {
  title: string;
  subtitle?: string;
  balance?: string;
  address: string;
}

export default function Wallet({
  title,
  subtitle,
  balance,
  address,
}: WalletProps) {
  return (
    <div className="bg-muted p-5 rounded-md text-black min-w-[224px]">
      <div className="flex gap-[15px] items-center">
        <Image
          src={`/icons/${title}-black.svg`}
          alt={title}
          width={35}
          height={35}
          priority
        />
        <div className="">
          <p className="p">{title}</p>
          <p className="p-sm text-black/40 -mt-1">{subtitle}</p>
        </div>
      </div>

      <div className="mt-20 flex items-end gap-5 justify-between">
        <div className="">
          {balance && (
            <p className="p-sm">
              <span className="text-black/40">Balance:</span> {balance}
            </p>
          )}
          <p className="p-sm">
            <span className="text-black/40">Address:</span>{" "}
            <span className="border-b">{shortAddress(address)}</span>
          </p>
        </div>
        <CopyBtn address={address} />
      </div>
    </div>
  );
}
