"use client";

import CryptoItem from "@/components/CryptoItem";
import CopyBtn from "@/components/CopyBtn";
import ProfileSkeleton from "./states/ProfileSkeleton";
import NoLinkedWallets from "./states/NoLinkedWallets";
import { buttonVariants } from "@/components/ui/button";
import { useGetLinkedWallets } from "@/requests/user/getLinkedWallets.request";
import { useUserStore } from "@/store/useUserStore";
import { shortAddress } from "@/utils/shortSomething";
import { LinkedWallet } from "@/types/user/profile.type";

export default function LinkedWallets() {
  const { user } = useUserStore();
  const { data, isFetching, refetch } = useGetLinkedWallets(user?.token || "");

  if (isFetching) return <ProfileSkeleton />;

  const wallets = data?.data || [];

  return (
    <div className="">
      <h2 className="h2 text-white mb-4">Linked Wallets</h2>
      {wallets.length > 0 ? (
        <>
          <div className="flex flex-col gap-[5px]">
            {data?.data.map((wallet: LinkedWallet, i: number) => (
              <div key={i} className="flex gap-[5px]">
                <CryptoItem
                  image={`/${wallet.network}.png`}
                  crypto={wallet.network}
                />
                <div
                  className={`${buttonVariants({
                    variant: "empty",
                    size: "xl",
                  })} flex gap-2 bg-gray px-5 rounded-xl flex-1 sm:py-1 sm:px-3 relative sm:h-[46px] justify-between!`}
                >
                  <p className="p sm:absolute sm:text-text-gray sm:top-1 sm:text-xs">
                    Address:
                  </p>
                  <p className="p sm:pt-3 sm:text-sm">
                    {shortAddress(wallet.walletAddress)}
                  </p>
                </div>
                <CopyBtn address={wallet.walletAddress} />
              </div>
            ))}
          </div>
        </>
      ) : (
        <NoLinkedWallets refetch={refetch} />
      )}
    </div>
  );
}
