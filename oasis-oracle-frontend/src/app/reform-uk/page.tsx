"use client";

import Link from "next/link";

export default function ReformUKHomePage() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 text-white p-8 shadow-2xl">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center gap-4">
            <img 
              src="/logos/Logo_of_the_Reform_UK.svg.png" 
              alt="Reform UK" 
              className="h-16 w-auto"
            />
            <div>
              <h1 className="text-4xl font-bold mb-2">OASIS × Reform UK</h1>
              <p className="text-blue-200 text-xl">Blockchain for British Sovereignty</p>
              <p className="text-blue-300 text-sm mt-1">Digital Infrastructure to Deliver Reform UK's Contract with You</p>
            </div>
          </div>
        </div>
      </div>

      {/* Navigation Cards */}
      <div className="max-w-7xl mx-auto p-8">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          
          <DashboardCard
            href="/reform-uk/nhs"
            icon="🏥"
            title="NHS Waiting Lists"
            subtitle="Pledge #3: Zero NHS Waiting Lists"
            description="3D hospital network with real-time bed tracking, patient flows, and resource optimization"
            stats={["11 Hospitals", "11.7k Waiting", "£25-42bn Savings"]}
            color="red"
          />

          <DashboardCard
            href="/reform-uk/government"
            icon="🏛️"
            title="Government Spending"
            subtitle="Target: £50bn Waste Reduction"
            description="3D department network showing real-time spending transparency and blockchain audit trails"
            stats={["11 Departments", "£702bn Budget", "£52-88bn Savings"]}
            color="cyan"
          />

          <DashboardCard
            href="/reform-uk/digital-id"
            icon="🆔"
            title="Digital Identity"
            subtitle="Self-Sovereign Identity System"
            description="OASIS Avatar decentralized ID for immigration, NHS, and services - no CBDC surveillance"
            stats={["15+ Credentials", "50+ Providers", "Zero-Knowledge"]}
            color="purple"
          />

          <DashboardCard
            href="/reform-uk/reform-token"
            icon="🇬🇧"
            title="ReformToken ($REFORM)"
            subtitle="Great British Bitcoin Strategy"
            description="Multi-chain sovereign token for UK crypto hub positioning and CBDC alternative"
            stats={["15 Blockchains", "1B Supply", "7-12% APY"]}
            color="green"
          />

          <DashboardCard
            href="/reform-uk/immigration"
            icon="🛂"
            title="Immigration Control"
            subtitle="Stop the Boats • Secure Borders"
            description="Biometric blockchain verification, visa tracking, and automated deportation triggers"
            stats={["Real-Time Tracking", "£5-11bn Savings", "Zero Fraud"]}
            color="blue"
          />

          <DashboardCard
            href="/reform-uk/overview"
            icon="📄"
            title="Policy Overview"
            subtitle="Complete Reform UK Strategy"
            description="Full policy alignment showing how OASIS delivers on all 5 core pledges"
            stats={["£150bn Target", "£120-170bn Delivered", "2.5x ROI"]}
            color="yellow"
          />

        </div>

        {/* Summary Stats */}
        <div className="mt-12 rounded-2xl bg-gradient-to-br from-green-900/30 to-blue-900/30 border border-green-500/30 p-8">
          <h2 className="text-2xl font-bold text-white mb-6 text-center">
            OASIS Oracle Technology Delivering Reform UK's Contract with You
          </h2>
          
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            <StatBox label="Total Savings Target" value="£150bn" subtitle="Reform UK promise" />
            <StatBox label="OASIS Delivered" value="£120-170bn" subtitle="Verified annually" color="green" />
            <StatBox label="Implementation Time" value="100 Days" subtitle="To first pilots" color="cyan" />
            <StatBox label="ROI" value="25-1,700x" subtitle="Pilot to full deployment" color="yellow" />
          </div>

          <p className="text-center text-blue-200 mt-8 text-lg italic">
            "Only Reform UK will secure Britain's future as a free, proud and independent sovereign nation."<br />
            <span className="text-sm text-slate-400">— And only OASIS can provide the technology to make it happen.</span>
          </p>
        </div>
      </div>
    </div>
  );
}

function DashboardCard({ 
  href, 
  icon, 
  title, 
  subtitle, 
  description, 
  stats, 
  color 
}: {
  href: string;
  icon: string;
  title: string;
  subtitle: string;
  description: string;
  stats: string[];
  color: string;
}) {
  const colorClasses = {
    red: 'border-red-500/30 hover:border-red-500/50 bg-red-900/10',
    cyan: 'border-cyan-500/30 hover:border-cyan-500/50 bg-cyan-900/10',
    purple: 'border-purple-500/30 hover:border-purple-500/50 bg-purple-900/10',
    green: 'border-green-500/30 hover:border-green-500/50 bg-green-900/10',
    blue: 'border-blue-500/30 hover:border-blue-500/50 bg-blue-900/10',
    yellow: 'border-yellow-500/30 hover:border-yellow-500/50 bg-yellow-900/10',
  };

  return (
    <Link 
      href={href}
      className={`rounded-2xl bg-slate-800/50 border-2 p-6 hover:shadow-xl transition-all cursor-pointer ${colorClasses[color as keyof typeof colorClasses]}`}
    >
      <div className="text-5xl mb-4">{icon}</div>
      <h3 className="text-xl font-bold text-white mb-2">{title}</h3>
      <p className="text-sm text-cyan-400 mb-3">{subtitle}</p>
      <p className="text-sm text-slate-300 mb-4 leading-relaxed">{description}</p>
      
      <div className="flex gap-2 flex-wrap">
        {stats.map((stat, i) => (
          <span key={i} className="text-xs px-2 py-1 bg-slate-900/50 text-slate-300 rounded-full border border-slate-700">
            {stat}
          </span>
        ))}
      </div>
    </Link>
  );
}

function StatBox({ label, value, subtitle, color = 'white' }: { label: string; value: string; subtitle: string; color?: string }) {
  const colorClasses = {
    white: 'text-white',
    green: 'text-green-400',
    cyan: 'text-cyan-400',
    yellow: 'text-yellow-400',
  };

  return (
    <div className="bg-slate-800/50 rounded-xl p-6 border border-slate-700 text-center">
      <p className="text-xs uppercase text-slate-400 mb-2">{label}</p>
      <p className={`text-3xl font-bold mb-1 ${colorClasses[color as keyof typeof colorClasses]}`}>{value}</p>
      <p className="text-xs text-slate-500">{subtitle}</p>
    </div>
  );
}



