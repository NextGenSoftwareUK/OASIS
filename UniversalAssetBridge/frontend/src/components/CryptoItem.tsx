import Image from "next/image";
import { buttonVariants } from "@/components/ui/button";

interface CryptoItemProps {
  image: string;
  crypto: string;
  className?: string;
  onClick?: () => void;
}

export default function CryptoItem({
  image,
  crypto,
  className,
  onClick,
}: CryptoItemProps) {
  return (
    <div
      className={`${buttonVariants({
        variant: "empty",
        size: "md",
      })} flex gap-2 bg-muted !px-3 !py-3 rounded-lg items-center sm:px-2 sm:py-1 sm:h-[46px] ${className}`}
      onClick={onClick}
    >
      <Image
        src={image}
        alt={crypto}
        width={18}
        height={18}
        className="sm:w-6"
      />
      <p className="p">{crypto}</p>
    </div>
  );
}
