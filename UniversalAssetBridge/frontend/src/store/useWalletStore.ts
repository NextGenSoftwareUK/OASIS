import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";

interface WalletState {
  publicKey: string | null;
  setPublicKey: (key: string | null) => void;
  disconnectWallet: () => void;
}

export const useWalletStore = create<WalletState>()(
  persist(
    (set) => {
      return {
        publicKey: null,

        setPublicKey: (key) => {
          set({ publicKey: key });
          console.log("Wallet Connected:", key);
        },

        disconnectWallet: () => {
          set({ publicKey: null });
          console.log("Wallet Disconnected");
        },
      };
    },
    {
      name: "wallet-store",
      storage: createJSONStorage(() => localStorage),
    }
  )
);
