import { OracleLayout } from "@/components/layout/oracle-layout";
import { OracleStatusPanel } from "@/components/dashboard/oracle-status-panel";
import { PriceFeedTable } from "@/components/dashboard/price-feed-table";
import { ChainObserverGrid } from "@/components/dashboard/chain-observer-grid";

export default function Home() {
  return (
    <OracleLayout>
      <div className="space-y-8">
        {/* Hero Section */}
        <div className="space-y-3">
          <h1 className="text-4xl font-bold tracking-tight text-[var(--color-foreground)]">
            Oracle Dashboard
          </h1>
          <p className="text-lg text-[var(--muted)] max-w-3xl">
            Real-time monitoring of the OASIS cross-chain oracle network. 
            Aggregating data from 20+ blockchains and 8+ price sources.
          </p>
        </div>

        {/* Oracle Status Overview */}
        <OracleStatusPanel />

        {/* Price Feeds Table */}
        <PriceFeedTable />

        {/* Chain Observer Grid */}
        <ChainObserverGrid />
      </div>
    </OracleLayout>
  );
}





