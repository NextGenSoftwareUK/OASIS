import { useState } from "react";
import { handleCopy } from "@/utils/handleCopy.util";
import { CopyIpfsButtonProps } from "@/types/rwa/rwaProps.type";
import { shortAddress } from "@/utils/shortSomething";

export default function CopyIpfsButton({ cid }: CopyIpfsButtonProps) {
  const [isCopied, setIsCopied] = useState(false);

  const handleClick = () => {
    handleCopy(cid, { setIsCopied });
  };

  return (
    <span onClick={handleClick} className="cursor-pointer relative">
      {shortAddress(cid)}
      {isCopied && (
        <span className="absolute right-0 -top-6 bg-white text-black text-xs px-2 py-1 rounded-md opacity-90 transition">
          Copied
        </span>
      )}
    </span>
  );
}
