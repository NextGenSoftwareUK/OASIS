"use client";

import Wallet from "@/app/profile/wallets/components/Wallet";
import { Skeleton } from "@/components/ui/skeleton";
import { useGetLinkedWallets } from "@/requests/user/getLinkedWallets.request";
import { useUserStore } from "@/store/useUserStore";
import { LinkedWallet } from "@/types/user/profile.type";

export default function ConnectedWallets() {
  const { user } = useUserStore();
  const { data, isFetching, refetch } = useGetLinkedWallets(user?.token || "");

  if (isFetching)
    return <Skeleton className="bg-primary w-[230px] h-[209px]" />;

  // const wallets = data?.data || [];
  const wallets = [
    {
      network: "Solana",
      walletAddress: "ffUhngvbngjkfbdhgjfdsb",
    },
    {
      network: "Solana",
      walletAddress: "nfmdspjIFnv438ndu",
    },
    {
      network: "Solana",
      walletAddress: "okpfNG483ndksB",
    },
  ];
  return (
    <div className="flex gap-2">
      {wallets.length > 0 &&
        wallets.map((wallet: LinkedWallet) => (
          <Wallet
            key={wallet.walletAddress}
            title={wallet.network}
            address={wallet.walletAddress}
            subtitle="Phantom"
          />
        ))}
    </div>
  );
}
