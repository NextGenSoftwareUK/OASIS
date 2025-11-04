"use client";

import { useState } from "react";
import Image from "next/image";
import { handleCopy } from "@/utils/handleCopy.util";
import { Button } from "@/components/ui/button";

interface CopyBtnProps {
  address: string;
}

export default function CopyBtn({ address }: CopyBtnProps) {
  const [isCopied, setIsCopied] = useState(false);

  return (
    <div className="relative">
      <Button
        variant="secondary"
        size="icon"
        className="flex justify-center gap-2 w-[48px] h-[48px] items-center 
        aspect-square sm:w-[46px] sm:h-[46px] hover:bg-secondary transition-all"
        onClick={() => handleCopy(address, { setIsCopied })}
      >
        <Image
          src="/copy.svg"
          alt="copy"
          width={22}
          height={22}
          className="xxs:w-5"
        />
      </Button>

      {isCopied && (
        <span className="absolute -top-8 left-1/2 -translate-x-1/2 bg-black text-white text-xs px-2 py-1 rounded-md opacity-90 transition">
          Copied
        </span>
      )}
    </div>
  );
}
