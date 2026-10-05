"use client";

import Image from "next/image";
import WalletSelector from "@/components/WalletSelector";
import { usePhantomWallet } from "@/hooks/usePhantomWallet";
import { Button } from "@/components/ui/button";
import { useWalletStore } from "@/store/useWalletStore";
import { shortAddress } from "@/utils/shortSomething";
import { useUserStore } from "@/store/useUserStore";
import { useState } from "react";

export default function WalletConnect({ className }: { className?: string }) {
  const [showWalletSelector, setShowWalletSelector] = useState(false);
  const { disconnectWallet } = usePhantomWallet();
  const { publicKey } = useWalletStore();
  const { user } = useUserStore();

  if (user) {
    return (
      <div>
        {publicKey ? (
          <div className="flex gap-2">
            <Button
              className={`group relative ${className}`}
              variant="default"
              size="lg"
              onClick={disconnectWallet}
            >
              <Image
                src="/phantom.svg"
                alt="Phantom Wallet"
                width={20}
                height={20}
                className="mr-2"
              />
              <span className="group-hover:hidden md:group-hover:inline">
                {shortAddress(publicKey)}
              </span>
              <span className="hidden group-hover:inline md:group-hover:hidden">
                Disconnect
              </span>
            </Button>

            <Button
              className={`group relative hidden md:block ${className}`}
              variant="default"
              size="lg"
              onClick={disconnectWallet}
            >
              Disconnect
            </Button>
          </div>
        ) : (
          <Button
            className={`${className} !px-5`}
            variant="default"
            size="lg"
            onClick={() => {
              setShowWalletSelector(true);
            }}
          >
            Connect wallet
          </Button>
        )}
        {showWalletSelector && (
          <WalletSelector setShowWalletSelector={setShowWalletSelector} />
        )}
      </div>
    );
  }
}
