import CryptoItem from "@/components/CryptoItem";
import CopyBtn from "@/components/CopyBtn";
import { buttonVariants } from "@/components/ui/button";
import { AccountAddress } from "@/types/user/profile.type";

export default function AccountAddressItem({
  address,
}: {
  address: AccountAddress;
}) {
  return (
    <div className="flex gap-[5px]">
      <CryptoItem image={`/${address.token}.png`} crypto={address.token} />
      <div
        className={`${buttonVariants({
          variant: "empty",
          size: "xl",
        })} flex gap-2 bg-gray px-5 rounded-xl justify-between items-center flex-1 sm:py-1 sm:px-3 sm:h-[46px] relative`}
      >
        <p className="p sm:absolute sm:text-text-gray sm:top-1 sm:text-xs">
          Balance:
        </p>
        <p className="p sm:pt-3 sm:text-sm">{address.balance}</p>
      </div>
      <CopyBtn address={address.address} />
    </div>
  );
}
