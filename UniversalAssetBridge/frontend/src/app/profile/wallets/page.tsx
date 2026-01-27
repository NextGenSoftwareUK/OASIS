import ConnectedWallets from "@/app/profile/wallets/components/ConnectedWallets";
import SystemWallets from "@/app/profile/wallets/components/SystemWallets";

export default function page() {
  return (
    <div className="">
      <h2 className="h1 mb-1">System Wallets</h2>
      <p className="p-sm mb-7.5 text-secondary">
        Wallets created by Quantum Street for swapping
      </p>

      <SystemWallets />

      <h2 className="h1 mt-12.5 mb-1">Connected Wallets</h2>
      <p className="p-sm mb-7.5 text-secondary">Your connected wallets</p>

      <ConnectedWallets />
    </div>
  );
}
