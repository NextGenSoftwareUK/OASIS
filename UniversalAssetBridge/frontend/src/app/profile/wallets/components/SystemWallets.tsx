"use client";

import Wallet from "@/app/profile/wallets/components/Wallet";
import { Skeleton } from "@/components/ui/skeleton";
import { useGetVirtualAccounts } from "@/requests/user/getVirtualAccounts.request";
import { useUserStore } from "@/store/useUserStore";
import { AccountAddress } from "@/types/user/profile.type";

export default function SystemWallets() {
  const { user } = useUserStore();
  const { data, isFetching } = useGetVirtualAccounts(true, user?.token || "");

  if (isFetching)
    return <Skeleton className="bg-primary w-[230px] h-[209px]" />;

  // const accounts = data?.data || [];
  const accounts = [
    {
      token: "SOL",
      balance: "3.545635",
      address: "ffUhngvbngjkfbdhgjfdsb",
    },
    {
      token: "XRD",
      balance: "12352.635",
      address: "nfmdspjIFnv438ndu",
    },
  ];
  return (
    <div className="flex gap-2">
      {accounts.length > 0 &&
        accounts.map((account: AccountAddress) => (
          <Wallet
            key={account.address}
            title={account.token}
            subtitle="Solana"
            {...account}
          />
        ))}
    </div>
  );
}
