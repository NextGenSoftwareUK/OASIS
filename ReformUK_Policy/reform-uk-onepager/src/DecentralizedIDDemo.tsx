import { useState } from 'react';

type Credential = {
  type: string;
  issuer: string;
  issuedDate: string;
  status: 'verified' | 'pending' | 'expired';
  icon: string;
};

type AvatarData = {
  id: string;
  username: string;
  nationality: string;
  residencyYears: number;
  karma: number;
  trustLevel: string;
  credentials: Credential[];
  wallets: { chain: string; address: string; balance: string }[];
  privacy: 'high' | 'medium' | 'low';
};

const mockAvatar: AvatarData = {
  id: 'avatar-uk-12345',
  username: 'john_smith_uk',
  nationality: 'British',
  residencyYears: 8,
  karma: 1350,
  trustLevel: 'Verified Citizen',
  credentials: [
    { type: 'UK Citizenship', issuer: 'Home Office', issuedDate: '2016-03-15', status: 'verified', icon: '🇬🇧' },
    { type: 'NHS Patient Record', issuer: 'NHS Digital', issuedDate: '2020-01-01', status: 'verified', icon: '🏥' },
    { type: 'Tax Compliance', issuer: 'HMRC', issuedDate: '2024-04-06', status: 'verified', icon: '💰' },
    { type: 'Electoral Registration', issuer: 'Electoral Commission', issuedDate: '2019-11-01', status: 'verified', icon: '🗳️' },
    { type: 'Right to Work', issuer: 'Home Office', issuedDate: '2016-03-15', status: 'verified', icon: '💼' },
  ],
  wallets: [
    { chain: 'Ethereum', address: '0x742d...3a9f', balance: '2.5 ETH' },
    { chain: 'Solana', address: 'Bx8k...9mPq', balance: '145.8 SOL' },
    { chain: 'Bitcoin', address: 'bc1q...8xkm', balance: '0.15 BTC' },
  ],
  privacy: 'high',
};

export default function DecentralizedIDDemo() {
  const [avatar] = useState<AvatarData>(mockAvatar);
  const [selectedService, setSelectedService] = useState<string | null>(null);
  const [showBiometric, setShowBiometric] = useState(false);

  return (
    <div className="min-h-screen bg-gradient-to-br from-slate-900 via-blue-900 to-slate-900">
      {/* Header */}
      <div className="bg-gradient-to-r from-blue-900 to-blue-800 text-white p-8 shadow-2xl">
        <div className="max-w-7xl mx-auto">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-4xl font-bold mb-2">Decentralized Digital ID</h1>
              <p className="text-blue-200">OASIS Avatar - Self-Sovereign Identity for UK Citizens</p>
            </div>
            <div className="text-right">
              <p className="text-sm text-blue-300">Reform UK Solution</p>
              <p className="text-lg font-semibold">You Own Your ID, Not the Government</p>
            </div>
          </div>
        </div>
      </div>

      {/* Comparison Banner */}
      <div className="bg-slate-800 border-b border-slate-700 py-4">
        <div className="max-w-7xl mx-auto px-8">
          <div className="grid grid-cols-2 gap-8">
            <div className="bg-red-900/20 border border-red-500/30 rounded-lg p-4">
              <h3 className="text-red-400 font-bold mb-2">❌ Government CBDC + Digital ID</h3>
              <ul className="text-sm text-slate-300 space-y-1">
                <li>• Central control & surveillance</li>
                <li>• Can freeze your account</li>
                <li>• Tracks every transaction</li>
                <li>• Social credit potential</li>
              </ul>
            </div>
            <div className="bg-green-900/20 border border-green-500/30 rounded-lg p-4">
              <h3 className="text-green-400 font-bold mb-2">✅ OASIS Decentralized Avatar</h3>
              <ul className="text-sm text-slate-300 space-y-1">
                <li>• Self-custody & privacy</li>
                <li>• Cannot be frozen (without court order)</li>
                <li>• Private transactions (ZK-proofs)</li>
                <li>• Karma (not social credit)</li>
              </ul>
            </div>
          </div>
        </div>
      </div>

      {/* Main Content */}
      <div className="max-w-7xl mx-auto p-8 space-y-8">
        
        {/* Avatar Profile Card */}
        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          
          {/* Left: Avatar Overview */}
          <div className="lg:col-span-1 space-y-6">
            <div className="rounded-2xl bg-gradient-to-br from-blue-800/50 to-slate-800/50 border border-cyan-500/30 p-6">
              <div className="flex items-center gap-4 mb-6">
                <div className="w-20 h-20 rounded-full bg-gradient-to-br from-cyan-400 to-blue-600 flex items-center justify-center text-white text-3xl font-bold">
                  JS
                </div>
                <div>
                  <h2 className="text-2xl font-bold text-white">{avatar.username}</h2>
                  <p className="text-cyan-400">{avatar.nationality} Citizen</p>
                </div>
              </div>

              <div className="space-y-3">
                <InfoRow label="Avatar ID" value={avatar.id} />
                <InfoRow label="Residency" value={`${avatar.residencyYears} years`} />
                <InfoRow label="Karma Score" value={avatar.karma.toString()} color="cyan" />
                <InfoRow label="Trust Level" value={avatar.trustLevel} color="green" />
                <InfoRow label="Privacy Mode" value={avatar.privacy.toUpperCase()} color="yellow" />
              </div>

              <button
                onClick={() => setShowBiometric(!showBiometric)}
                className="mt-6 w-full px-4 py-3 bg-gradient-to-r from-cyan-600 to-blue-600 text-white rounded-lg font-semibold hover:from-cyan-500 hover:to-blue-500 transition-all"
              >
                {showBiometric ? '🔒 Hide Biometric' : '👤 Show Biometric Verification'}
              </button>

              {showBiometric && (
                <div className="mt-4 p-4 bg-slate-900/50 rounded-lg border border-cyan-500/30">
                  <p className="text-xs uppercase text-slate-400 mb-2">Biometric Hash (SHA-256)</p>
                  <p className="text-xs font-mono text-cyan-400 break-all">
                    a7f2...8c4e
                  </p>
                  <p className="text-xs text-green-400 mt-2">✓ Verified by UK Border Force</p>
                </div>
              )}
            </div>

            {/* Karma Breakdown */}
            <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-6">
              <h3 className="text-lg font-bold text-white mb-4">Karma Breakdown</h3>
              <div className="space-y-3">
                <KarmaBar label="Immigration Compliance" value={600} max={1350} color="cyan" />
                <KarmaBar label="Tax Compliance" value={300} max={1350} color="green" />
                <KarmaBar label="NHS Good Standing" value={200} max={1350} color="blue" />
                <KarmaBar label="Electoral Participation" value={150} max={1350} color="purple" />
                <KarmaBar label="Community Service" value={100} max={1350} color="yellow" />
              </div>
              <p className="text-xs text-slate-400 mt-4 italic">
                Note: Karma rewards good behavior, never punishes. This is NOT social credit.
              </p>
            </div>
          </div>

          {/* Right: Credentials & Services */}
          <div className="lg:col-span-2 space-y-6">
            
            {/* Verified Credentials */}
            <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-6">
              <h3 className="text-xl font-bold text-white mb-4">Verified Credentials (Blockchain)</h3>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {avatar.credentials.map((cred, i) => (
                  <div key={i} className="bg-slate-900/50 border border-green-500/30 rounded-lg p-4 hover:border-green-500/50 transition-colors">
                    <div className="flex items-start justify-between mb-2">
                      <div className="flex items-center gap-2">
                        <span className="text-2xl">{cred.icon}</span>
                        <div>
                          <p className="font-semibold text-white text-sm">{cred.type}</p>
                          <p className="text-xs text-slate-400">Issued by {cred.issuer}</p>
                        </div>
                      </div>
                      <span className="text-green-400 text-lg">✓</span>
                    </div>
                    <p className="text-xs text-slate-500">Issued: {cred.issuedDate}</p>
                    <div className="mt-2 flex items-center gap-2">
                      <div className="h-1 flex-1 bg-slate-700 rounded-full overflow-hidden">
                        <div className="h-full w-full bg-green-500"></div>
                      </div>
                      <span className="text-xs text-green-400 font-semibold">{cred.status}</span>
                    </div>
                  </div>
                ))}
              </div>
            </div>

            {/* Multi-Chain Wallets */}
            <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-6">
              <h3 className="text-xl font-bold text-white mb-4">Self-Custody Wallets (Anti-CBDC)</h3>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                {avatar.wallets.map((wallet, i) => (
                  <div key={i} className="bg-gradient-to-br from-cyan-900/30 to-blue-900/30 border border-cyan-500/30 rounded-lg p-4">
                    <p className="text-sm font-semibold text-cyan-400 mb-2">{wallet.chain}</p>
                    <p className="text-xs font-mono text-slate-300 mb-3">{wallet.address}</p>
                    <p className="text-lg font-bold text-white">{wallet.balance}</p>
                    <p className="text-xs text-slate-400 mt-2">✓ Self-custody (you control keys)</p>
                  </div>
                ))}
              </div>
              <p className="text-sm text-cyan-300 mt-4">
                🛡️ <strong>CBDC Protection:</strong> Multi-chain self-custody means government cannot freeze, monitor, or control your money.
              </p>
            </div>

            {/* Government Services Access */}
            <div className="rounded-2xl bg-slate-800/50 border border-slate-700 p-6">
              <h3 className="text-xl font-bold text-white mb-4">Government Services (Zero-Knowledge Access)</h3>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <ServiceButton 
                  icon="🛂" 
                  label="Immigration" 
                  onClick={() => setSelectedService('immigration')}
                  active={selectedService === 'immigration'}
                />
                <ServiceButton 
                  icon="🏥" 
                  label="NHS" 
                  onClick={() => setSelectedService('nhs')}
                  active={selectedService === 'nhs'}
                />
                <ServiceButton 
                  icon="💰" 
                  label="Tax (HMRC)" 
                  onClick={() => setSelectedService('tax')}
                  active={selectedService === 'tax'}
                />
                <ServiceButton 
                  icon="🗳️" 
                  label="Voting" 
                  onClick={() => setSelectedService('voting')}
                  active={selectedService === 'voting'}
                />
              </div>

              {/* Service Details */}
              {selectedService && (
                <div className="mt-6 p-6 bg-slate-900/50 rounded-lg border border-cyan-500/20">
                  {selectedService === 'immigration' && (
                    <ServiceDetail
                      title="Immigration Status Verification"
                      dataShared="✓ Biometric hash only (ZK-proof)"
                      dataKept="✗ Full biometric data"
                      dataKept2="✗ Travel history"
                      dataKept3="✗ Personal details"
                      benefit="Border Force verifies you're you, without seeing private data"
                    />
                  )}
                  {selectedService === 'nhs' && (
                    <ServiceDetail
                      title="NHS Patient Record Access"
                      dataShared="✓ Medical history (temporary access granted)"
                      dataKept="✗ Future appointments"
                      dataKept2="✗ Prescription access history"
                      dataKept3="✗ Insurance information"
                      benefit="Doctor sees what they need, you keep control of your data"
                    />
                  )}
                  {selectedService === 'tax' && (
                    <ServiceDetail
                      title="HMRC Tax Verification"
                      dataShared="✓ Employment status (ZK-proof)"
                      dataKept="✗ Salary amount"
                      dataKept2="✗ Bank accounts"
                      dataKept3="✗ Spending patterns"
                      benefit="Prove tax compliance without revealing full financial details"
                    />
                  )}
                  {selectedService === 'voting' && (
                    <ServiceDetail
                      title="Electoral Registration"
                      dataShared="✓ Age verification (ZK-proof: over 18)"
                      dataKept="✗ Date of birth"
                      dataKept2="✗ Home address"
                      dataKept3="✗ Party affiliation"
                      benefit="Register to vote, maintain complete privacy"
                    />
                  )}
                </div>
              )}
            </div>

          </div>
        </div>

        {/* How It Works */}
        <div className="rounded-2xl bg-gradient-to-br from-green-900/30 to-blue-900/30 border border-green-500/30 p-8">
          <h2 className="text-2xl font-bold text-white mb-6">How OASIS Decentralized ID Works</h2>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <Step
              number="1"
              title="Create Your Avatar"
              description="Register once, use everywhere. Your digital identity across all government services."
              icon="🆔"
            />
            <Step
              number="2"
              title="Collect Credentials"
              description="Government services issue verified credentials (citizenship, NHS, tax, etc.) stored on blockchain."
              icon="📜"
            />
            <Step
              number="3"
              title="Access Services (Zero-Knowledge)"
              description="Prove you're eligible without revealing everything. Privacy-first, sovereignty-preserving."
              icon="🔐"
            />
          </div>
        </div>

        {/* Reform UK Value Proposition */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <ValueCard
            icon="🇬🇧"
            title="British Sovereignty"
            description="UK-controlled infrastructure. Your data stays in Britain. No EU, WEF, or foreign control."
          />
          <ValueCard
            icon="🛡️"
            title="CBDC Alternative"
            description="Self-custody wallets on 15+ blockchains. Government can't freeze, monitor, or control your money."
          />
          <ValueCard
            icon="🔒"
            title="Privacy-First"
            description="Zero-knowledge proofs reveal only what's needed. No surveillance, no tracking, no social credit."
          />
        </div>

        {/* Technical Stats */}
        <div className="rounded-2xl bg-slate-800/50 border border-cyan-500/30 p-8">
          <h3 className="text-2xl font-bold text-white mb-6">Technical Capabilities</h3>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-6">
            <TechStat label="Blockchains" value="15+" />
            <TechStat label="Providers" value="50+" />
            <TechStat label="Uptime" value="99.9%" />
            <TechStat label="Verification" value="<1s" />
          </div>
        </div>

        {/* Call to Action */}
        <div className="rounded-2xl bg-gradient-to-r from-blue-900 to-blue-800 p-8 text-center">
          <h3 className="text-3xl font-bold text-white mb-3">
            Reform UK: Digital Sovereignty = National Sovereignty
          </h3>
          <p className="text-blue-200 text-lg max-w-3xl mx-auto mb-6">
            OASIS Avatar provides the decentralized digital identity infrastructure for Reform UK's vision: 
            secure borders, patient-owned NHS records, and financial freedom without CBDCs.
          </p>
          <div className="flex gap-4 justify-center">
            <div className="bg-white/10 px-6 py-3 rounded-lg">
              <p className="text-sm text-blue-200">Immigration Control</p>
              <p className="text-2xl font-bold text-white">£5-11bn</p>
              <p className="text-xs text-blue-300">Annual Savings</p>
            </div>
            <div className="bg-white/10 px-6 py-3 rounded-lg">
              <p className="text-sm text-blue-200">NHS Efficiency</p>
              <p className="text-2xl font-bold text-white">£25-42bn</p>
              <p className="text-xs text-blue-300">Annual Savings</p>
            </div>
            <div className="bg-white/10 px-6 py-3 rounded-lg">
              <p className="text-sm text-blue-200">CBDC Opposition</p>
              <p className="text-2xl font-bold text-green-400">Priceless</p>
              <p className="text-xs text-blue-300">Freedom Preserved</p>
            </div>
          </div>
        </div>

      </div>
    </div>
  );
}

function InfoRow({ label, value, color = 'slate' }: { label: string; value: string; color?: string }) {
  const colorClasses = {
    slate: 'text-slate-300',
    cyan: 'text-cyan-400',
    green: 'text-green-400',
    yellow: 'text-yellow-400',
  };

  return (
    <div className="flex justify-between items-center py-2 border-b border-slate-700/50">
      <span className="text-sm text-slate-400">{label}</span>
      <span className={`text-sm font-semibold ${colorClasses[color as keyof typeof colorClasses]}`}>{value}</span>
    </div>
  );
}

function KarmaBar({ label, value, max, color }: { label: string; value: number; max: number; color: string }) {
  const percentage = (value / max) * 100;
  const colorClasses = {
    cyan: 'bg-cyan-500',
    green: 'bg-green-500',
    blue: 'bg-blue-500',
    purple: 'bg-purple-500',
    yellow: 'bg-yellow-500',
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-1">
        <span className="text-xs text-slate-400">{label}</span>
        <span className="text-xs font-semibold text-white">{value}</span>
      </div>
      <div className="h-2 bg-slate-700 rounded-full overflow-hidden">
        <div 
          className={`h-full ${colorClasses[color as keyof typeof colorClasses]} transition-all`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
}

function ServiceButton({ icon, label, onClick, active }: { icon: string; label: string; onClick: () => void; active: boolean }) {
  return (
    <button
      onClick={onClick}
      className={`p-4 rounded-lg border transition-all ${
        active 
          ? 'bg-cyan-900/50 border-cyan-500/50 shadow-lg shadow-cyan-500/20' 
          : 'bg-slate-900/30 border-slate-700 hover:border-cyan-500/30'
      }`}
    >
      <div className="text-3xl mb-2">{icon}</div>
      <p className="text-sm text-white font-medium">{label}</p>
    </button>
  );
}

function ServiceDetail({ title, dataShared, dataKept, dataKept2, dataKept3, benefit }: any) {
  return (
    <div>
      <h4 className="text-lg font-bold text-white mb-4">{title}</h4>
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-4">
        <div>
          <p className="text-xs uppercase text-green-400 mb-2">Data Shared (Zero-Knowledge)</p>
          <p className="text-sm text-green-300">{dataShared}</p>
        </div>
        <div>
          <p className="text-xs uppercase text-cyan-400 mb-2">Data You Keep Private</p>
          <div className="space-y-1 text-sm text-slate-300">
            <p>{dataKept}</p>
            <p>{dataKept2}</p>
            <p>{dataKept3}</p>
          </div>
        </div>
      </div>
      <div className="pt-4 border-t border-slate-700">
        <p className="text-xs uppercase text-yellow-400 mb-1">Benefit</p>
        <p className="text-sm text-white">{benefit}</p>
      </div>
    </div>
  );
}

function Step({ number, title, description, icon }: { number: string; title: string; description: string; icon: string }) {
  return (
    <div className="text-center">
      <div className="w-12 h-12 bg-cyan-600 text-white rounded-full flex items-center justify-center font-bold text-xl mx-auto mb-3">
        {number}
      </div>
      <div className="text-4xl mb-3">{icon}</div>
      <h4 className="text-lg font-bold text-white mb-2">{title}</h4>
      <p className="text-sm text-slate-300">{description}</p>
    </div>
  );
}

function ValueCard({ icon, title, description }: { icon: string; title: string; description: string }) {
  return (
    <div className="rounded-2xl bg-slate-800/50 border border-blue-500/30 p-6 hover:border-blue-500/50 transition-colors">
      <div className="text-4xl mb-3">{icon}</div>
      <h4 className="text-lg font-bold text-white mb-2">{title}</h4>
      <p className="text-sm text-slate-300">{description}</p>
    </div>
  );
}

function TechStat({ label, value }: { label: string; value: string }) {
  return (
    <div className="text-center">
      <p className="text-3xl font-bold text-cyan-400 mb-1">{value}</p>
      <p className="text-sm text-slate-400">{label}</p>
    </div>
  );
}

