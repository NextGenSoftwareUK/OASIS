"use client";

import ProfileSkeleton from "@/app/profile/components/states/ProfileSkeleton";
import NoAccounts from "@/app/profile/components/states/NoAccounts";
import AccountAddressItem from "@/app/profile/components/account/AccountAddressItem";
import { useUserStore } from "@/store/useUserStore";
import { useGetVirtualAccounts } from "@/requests/user/getVirtualAccounts.request";
import { AccountAddress } from "@/types/user/profile.type";

export default function AccountAddresses() {
  const { user } = useUserStore();
  const { data, isFetching } = useGetVirtualAccounts(true, user?.token || "");

  if (isFetching) return <ProfileSkeleton />;

  const accounts = data?.data || [];

  return (
    <div>
      <h2 className="h2 text-white mb-6">Account Addresses</h2>
      <div className="flex flex-col gap-[5px]">
        {accounts.length > 0 ? (
          accounts.map((address: AccountAddress) => (
            <AccountAddressItem key={address.token} address={address} />
          ))
        ) : (
          <NoAccounts />
        )}
      </div>
    </div>
  );
}
