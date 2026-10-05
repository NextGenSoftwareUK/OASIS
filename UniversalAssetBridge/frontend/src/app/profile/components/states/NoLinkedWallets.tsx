import WalletSelector from "@/components/WalletSelector";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { NoLinkedWalletsProps } from "@/types/user/profileProps.type";

export default function NoLinkedWallets({ refetch }: NoLinkedWalletsProps) {
  const [showWalletSelector, setShowWalletSelector] = useState(false);
  return (
    <div className="flex flex-col gap-[5px]">
      <p className="p text-white">You don't have any linked wallets yet.</p>
      <Button
        variant="gray"
        size="xl"
        onClick={() => setShowWalletSelector(true)}
      >
        Link a wallet
      </Button>
      {showWalletSelector && (
        <WalletSelector
          refetch={refetch}
          setShowWalletSelector={setShowWalletSelector}
        />
      )}
    </div>
  );
}
