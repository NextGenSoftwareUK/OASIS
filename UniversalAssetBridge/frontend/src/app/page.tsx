import Web4Nav from "@/components/Web4Nav";
import BridgePageClient from "@/app/BridgePageClient";

export default function page() {
  return (
    <div className="min-h-screen">
      <Web4Nav />
      <BridgePageClient />
    </div>
  );
}
