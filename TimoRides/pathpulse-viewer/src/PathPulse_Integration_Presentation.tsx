import React from "react";

// PathPulse.ai × TimoRides Integration - Comprehensive Presentation
// Print-ready PDF viewer with visual diagrams

const pathPulseData = {
  title: "PathPulse.ai × TimoRides Integration",
  subtitle: "Opening Africa's Under-Served Mobility Market with Next-Gen Routing & Data Monetization",
  version: "v1.0 | October 20, 2025",
  tagline: "Strategic partnership proposal: Empowering African drivers with data ownership while providing PathPulse with premium ground-truth data from emerging markets",
  
  contact: [
    { label: "TimoRides / OASIS Integration Team", icon: "brief" },
    { label: "TimoRides", icon: "building" },
    { label: "github.com/TimoRides", icon: "code" },
  ],
  
  executiveSummary: {
    overview: "TimoRides is pioneering premium ride-hailing in Africa, starting with Durban, South Africa. We're building on OASIS Web4/Web5 infrastructure to create a platform where drivers own their data and earn from it. PathPulse.ai is identified as our ideal routing partner - your whitepaper explicitly lists ride-sharing services as key customers, and your decentralized data marketplace aligns perfectly with our vision of driver empowerment.",
    opportunityStatement: "Africa represents a massive untapped opportunity: 1.4 billion people, rapidly growing smartphone adoption, and minimal competition from legacy players in secondary cities. TimoRides + PathPulse can define the future of mobility data in emerging markets.",
    benefits: [
      {
        title: "🌍 Opening Under-Served African Markets",
        description: "PathPulse gains access to ground-truth road data from African cities where Google Maps coverage is limited. TimoRides drivers become PathPulse's data contributors in Durban, Johannesburg, Cape Town, and beyond - markets with massive growth potential but limited current mapping data."
      },
      {
        title: "💰 Powerful Driver Revenue Streams",
        description: "Aligned with PathPulse's 'Contributor ID' and decentralized data marketplace model, TimoRides drivers earn passive income by sharing anonymized road condition data. This creates loyal contributors in emerging markets while providing PathPulse with high-value metadata for your government and fleet customers."
      },
      {
        title: "🏛️ Government & Infrastructure Partnerships",
        description: "PathPulse's whitepaper identifies government transportation departments as key customers. TimoRides provides the on-the-ground data collection network in African cities - real-time traffic, road conditions, and infrastructure insights that governments desperately need for urban planning."
      },
      {
        title: "🗺️ Empowering Alternative to Google Maps",
        description: "Together, we challenge Google's monopoly in African markets. PathPulse gets a competitive edge with exclusive ground-truth data from TimoRides' driver network, while our riders and drivers benefit from routing that understands local conditions - not just satellite imagery."
      }
    ]
  },
  
  useCases: [
    {
      title: "Ride Distance & Fare Calculation",
      current: "Google Maps Distance Matrix API",
      withPathPulse: "More accurate distance with traffic consideration",
      impact: "Fairer pricing, better rider expectations"
    },
    {
      title: "Driver ETA to Pickup",
      current: "Static distance calculation",
      withPathPulse: "Real-time traffic-aware ETA",
      impact: "Riders know exactly when driver will arrive"
    },
    {
      title: "Multi-Stop Rides",
      current: "Manual route planning",
      withPathPulse: "Intelligent route optimization",
      impact: "Drivers save time and fuel, riders pay less"
    },
    {
      title: "Premium Ride Experience",
      current: "Basic A→B routing",
      withPathPulse: "Scenic routes, avoid traffic, alternative options",
      impact: "True premium experience for premium rides"
    }
  ],
  
  driverDataMonetization: {
    overview: "Drivers earn additional revenue by sharing anonymized driving data with PathPulse.ai through a Web3-native data marketplace where drivers maintain ownership and control.",
    dataTypes: [
      "Route History: Actual routes taken (anonymized GPS trails)",
      "Traffic Patterns: Real-time traffic observations",
      "Speed Data: Average speeds on different road segments",
      "Road Conditions: Potholes, construction, road quality",
      "Parking Availability: Where parking was found",
      "Wait Times: Pickup wait times, drop-off times"
    ],
    earningsExamples: [
      {
        type: "Active Driver",
        ridesPerMonth: 200,
        baseDataSharing: "$20.00",
        trafficReports: "$0.15",
        roadConditions: "$0.10",
        revenueShare: "$1.00",
        totalMonthly: "$21.25",
        karmaPoints: "2,000"
      },
      {
        type: "Part-Time Driver",
        ridesPerMonth: 50,
        baseDataSharing: "$5.00",
        trafficReports: "$0.03",
        roadConditions: "$0.00",
        revenueShare: "$0.20",
        totalMonthly: "$5.23",
        karmaPoints: "500"
      }
    ],
    privacyProtection: [
      "No Personal Information: Names, addresses, rider info never shared",
      "GPS Anonymization: Location data anonymized to 100m radius",
      "Aggregation: Data combined with other drivers before PathPulse access",
      "Differential Privacy: Mathematical privacy guarantees",
      "GDPR/POPIA Compliant: Meets all data protection regulations"
    ]
  },
  
  technicalRequirements: {
    mustHave: [
      {
        feature: "Distance Calculation API",
        details: "Input: Origin (lat/lng), Destination (lat/lng). Output: Distance (km), Duration (minutes). Response time: <500ms"
      },
      {
        feature: "ETA with Traffic API",
        details: "Real-time traffic consideration, Departure time parameter, Alternative routes"
      },
      {
        feature: "Route Optimization API",
        details: "Multiple waypoints (5-10 stops), Optimize for time or distance, Return optimized order + total metrics"
      }
    ],
    niceToHave: [
      "Geocoding (address → coordinates)",
      "Reverse Geocoding (coordinates → address)",
      "Polyline/Route Geometry (for drawing on map)",
      "Historical Traffic Patterns",
      "WebSocket/Streaming API (for live tracking)"
    ]
  },
  
  timeline: [
    {
      phase: "Phase 1: Discovery & Scoping",
      weeks: "Week 1-2",
      tasks: [
        "✅ Technical architecture review (this document)",
        "🔲 PathPulse API documentation review",
        "🔲 Test API credentials & sandbox access",
        "🔲 Pricing & business terms discussion"
      ]
    },
    {
      phase: "Phase 2: MVP Integration",
      weeks: "Week 3-4",
      tasks: [
        "🔲 Implement PathPulse service layer in TimoRides backend",
        "🔲 Replace distance calculation with PathPulse",
        "🔲 Set up Google Maps as fallback",
        "🔲 Internal testing with real Durban routes"
      ]
    },
    {
      phase: "Phase 3: Pilot Testing",
      weeks: "Week 5-6",
      tasks: [
        "🔲 Closed beta with 50-100 rides",
        "🔲 Monitor accuracy, performance, costs",
        "🔲 Gather rider/driver feedback",
        "🔲 Compare vs Google Maps baseline"
      ]
    },
    {
      phase: "Phase 4: Production Launch",
      weeks: "Week 7-8",
      tasks: [
        "🔲 Full rollout to all TimoRides users",
        "🔲 Monitoring & analytics dashboard",
        "🔲 Cost tracking & optimization",
        "🔲 Performance SLA monitoring"
      ]
    },
    {
      phase: "Phase 5: Advanced Features",
      weeks: "Week 9-12+",
      tasks: [
        "🔲 Multi-stop optimization for shared rides",
        "🔲 Traffic-aware dynamic pricing",
        "🔲 Full OASIS provider implementation (optional)"
      ]
    }
  ],
  
  successMetrics: {
    technical: [
      "API Response Time: <500ms (95th percentile)",
      "Uptime: >99.5%",
      "Accuracy: <5% deviation from actual drive time",
      "Fallback Rate: <1% of requests"
    ],
    business: [
      "Cost per Ride: Track routing costs",
      "User Satisfaction: Rider ratings for ETAs",
      "Driver Efficiency: Time/fuel savings with optimization",
      "Revenue Impact: Premium rides booked due to better routing"
    ]
  },
  
  marketData: {
    estimatedVolume: [
      "Rides per month: ~10,000 (MVP), scaling to 100,000+",
      "Distance calculations per ride: 3-5 (Initial fare estimate, Driver assignment, Live tracking ETAs)",
      "Total API calls: ~30,000 - 50,000/month (MVP)"
    ],
    currentCosts: [
      "Google Maps Distance Matrix: ~$5-10 per 1,000 requests",
      "Monthly spend: ~$150-500 (MVP phase)",
      "Projected spend (scale): $5,000+/month at 1M rides/year"
    ],
    geographicFocus: [
      "Primary: Durban, South Africa",
      "Phase 1 Expansion: Johannesburg, Cape Town, Pretoria, Harare (Zimbabwe)",
      "Phase 2: Pan-African expansion (Kenya, Nigeria, Ghana)",
      "Phase 3: South American markets (Brazil, Colombia, Argentina)"
    ]
  },
  
  questionsForPathPulse: {
    technical: [
      "What is your API architecture? (REST, GraphQL, gRPC?)",
      "Do you provide SDKs? (Node.js, Python, C#?)",
      "What authentication method do you use? (API keys, OAuth, JWT?)",
      "What are your rate limits?",
      "Do you support batch requests?",
      "What is your typical API response time?"
    ],
    business: [
      "What is your pricing model?",
      "Do you offer volume discounts?",
      "What are typical contract terms?",
      "What SLA do you provide?",
      "Do you offer a free trial or sandbox?",
      "What support channels are available? (email, Slack, phone?)"
    ],
    product: [
      "What regions do you currently support?",
      "What's your roadmap for African coverage?",
      "Do you have traffic data for South Africa?",
      "What unique features differentiate you from Google Maps?",
      "Do you support offline/cached routing?",
      "Can you handle real-time tracking use cases?"
    ]
  },
  
  partnershipLevels: [
    {
      level: "Level 1: Technology Integration",
      description: "PathPulse as routing provider for TimoRides",
      details: ["Standard API integration", "Pay-per-use or subscription model"]
    },
    {
      level: "Level 2: Strategic Partnership",
      description: "Joint go-to-market in South Africa",
      details: ["Co-branded 'Powered by PathPulse' in app", "Case study & testimonials", "Preferential pricing for TimoRides"]
    },
    {
      level: "Level 3: Ecosystem Collaboration",
      description: "PathPulse becomes official OASIS routing provider",
      details: ["Available to all OASIS-based applications", "Joint development roadmap", "Potential equity/revenue share arrangement"]
    }
  ]
};

function Icon({ name, className = "w-4 h-4" }: { name: string; className?: string }) {
  const common = "inline-block align-[-0.125em]";
  switch (name) {
    case "brief":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M10 4h4a2 2 0 0 1 2 2v2h-8V6a2 2 0 0 1 2-2z"/> 
          <rect x="3" y="8" width="18" height="12" rx="2"/> 
        </svg>
      );
    case "building":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <rect x="4" y="2" width="16" height="20" rx="2" />
          <path d="M9 22v-4h6v4M8 6h.01M16 6h.01M8 10h.01M16 10h.01M8 14h.01M16 14h.01M12 6h.01M12 10h.01M12 14h.01" />
        </svg>
      );
    case "code":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M16 18l6-6-6-6M8 6l-6 6 6 6" />
        </svg>
      );
    case "check":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M20 6L9 17l-5-5" />
        </svg>
      );
    case "arrow-right":
      return (
        <svg className={`${className} ${common}`} viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5">
          <path d="M5 12h14M12 5l7 7-7 7" />
        </svg>
      );
    default:
      return null;
  }
}

function BenefitCard({ title, description }: { title: string; description: string }) {
  return (
    <div className="bg-gradient-to-br from-blue-50 to-indigo-50 border border-blue-200 rounded-lg p-5 hover:shadow-lg transition-all">
      <h3 className="font-bold text-blue-900 text-base mb-2">{title}</h3>
      <p className="text-sm text-blue-800/80 leading-relaxed">{description}</p>
    </div>
  );
}

function UseCaseCard({ useCase }: { useCase: any }) {
  return (
    <div className="bg-white border border-slate-200 rounded-lg p-5 hover:shadow-md transition-all">
      <h3 className="font-bold text-slate-900 text-base mb-3">{useCase.title}</h3>
      <div className="space-y-2 text-sm">
        <div className="flex items-start gap-2">
          <span className="text-slate-500 font-medium min-w-[80px]">Current:</span>
          <span className="text-slate-700">{useCase.current}</span>
        </div>
        <div className="flex items-start gap-2">
          <span className="text-blue-600 font-medium min-w-[80px]">PathPulse:</span>
          <span className="text-slate-700">{useCase.withPathPulse}</span>
        </div>
        <div className="flex items-start gap-2">
          <span className="text-green-600 font-medium min-w-[80px]">Impact:</span>
          <span className="text-slate-700 font-medium">{useCase.impact}</span>
        </div>
      </div>
    </div>
  );
}

function TimelinePhase({ phase }: { phase: any }) {
  return (
    <div className="relative pl-8 pb-8 border-l-2 border-blue-300 last:border-transparent last:pb-0">
      <div className="absolute left-[-9px] top-0 w-4 h-4 rounded-full bg-blue-500 border-4 border-white"></div>
      <div className="bg-white rounded-lg border border-blue-200 p-5 hover:shadow-md transition-all">
        <div className="flex items-start justify-between mb-2">
          <h3 className="font-bold text-blue-900 text-base">{phase.phase}</h3>
          <span className="text-xs text-blue-600 font-semibold bg-blue-50 px-2 py-1 rounded">{phase.weeks}</span>
        </div>
        <ul className="space-y-2 mt-3">
          {phase.tasks.map((task: string, i: number) => (
            <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
              <span className="mt-1">{task.startsWith('✅') ? '✅' : '🔲'}</span>
              <span>{task.replace(/^[✅🔲]\s*/, '')}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

function ArchitectureDiagram() {
  return (
    <div className="bg-gradient-to-br from-slate-50 to-blue-50 border-2 border-blue-200 rounded-xl p-6 font-mono text-xs">
      <pre className="text-slate-700 leading-relaxed overflow-x-auto">
{`┌───────────────────────────────────────────────────────────────────┐
│                      TimoRides Platform                           │
│                                                                   │
│  ┌─────────────────┐              ┌─────────────────┐           │
│  │  Rider Mobile   │              │ Driver Mobile   │           │
│  │      App        │              │      App        │           │
│  │   (Angular)     │              │   (Angular)     │           │
│  └────────┬────────┘              └────────┬────────┘           │
│           │                                 │                    │
│           └─────────────┬───────────────────┘                    │
│                         │                                        │
│                         ▼                                        │
│           ┌─────────────────────────────┐                       │
│           │  TimoRides Backend API      │                       │
│           │     (Node.js/Express)       │                       │
│           │  ┌─────────────────────┐   │                       │
│           │  │  Routing Service    │◄──┼─── PathPulse          │
│           │  │   (Abstraction)     │   │      Integration      │
│           │  └──────────┬──────────┘   │                       │
│           └─────────────┼───────────────┘                       │
└─────────────────────────┼───────────────────────────────────────┘
                          │
                          ▼
    ┌─────────────────────────────────────────────────────────┐
    │            OASIS Provider Layer                         │
    │                                                          │
    │   ┌──────────────────────────────────────────────┐    │
    │   │  OASIS Routing Manager / HyperDrive          │    │
    │   │  • Auto-Failover                              │    │
    │   │  • Load Balancing                             │    │
    │   │  • Provider Health Monitoring                 │    │
    │   │  • Cost Optimization                          │    │
    │   └──────┬───────────────────────────────┬────────┘    │
    └──────────┼───────────────────────────────┼─────────────┘
               │                               │
       ┌───────┴────────┬─────────────────────┴──────┐
       │                │                             │
       ▼                ▼                             ▼
┌────────────┐   ┌────────────┐            ┌────────────┐
│ PathPulse  │   │   Google   │            │  Mapbox    │
│   .ai      │   │    Maps    │            │            │
│  Provider  │   │  Provider  │            │  Provider  │
│            │   │            │            │            │
│ • Routes   │   │ • Fallback │            │ • Fallback │
│ • Traffic  │   │ • Reliable │            │ • Geodata  │
│ • Optimize │   │ • Proven   │            │ • Styles   │
└────────────┘   └────────────┘            └────────────┘
  Primary          Fallback #1              Fallback #2`}
      </pre>
    </div>
  );
}

function RequestFlowDiagram() {
  return (
    <div className="bg-gradient-to-br from-green-50 to-emerald-50 border-2 border-green-200 rounded-xl p-6 font-mono text-xs">
      <pre className="text-slate-700 leading-relaxed overflow-x-auto">
{`Rider requests ride
       │
       ▼
TimoRides Backend
       │
       ▼
routingService.js (NEW)
       │
       ├──► Try PRIMARY: PathPulse.ai API
       │         │
       │         ├─► ✅ Success
       │         │    Distance: 25.3 km
       │         │    Duration: 32 minutes (with traffic)
       │         │    Traffic: Moderate
       │         │    Alternative routes: 2
       │         │
       │         └─► ❌ Failed/Timeout
       │              │
       │              ▼
       │         Try FALLBACK: Google Maps
       │              │
       │              ▼
       │         Distance: 25.5 km
       │         Duration: 35 minutes
       │
       ▼
Calculate Fare: $18.20
       │
       ▼
Show estimate to Rider
   • "Estimated: 32 min (traffic: moderate)"
   • "Alternative routes available"`}
      </pre>
    </div>
  );
}

function DataMonetizationFlow() {
  return (
    <div className="bg-gradient-to-br from-purple-50 to-pink-50 border-2 border-purple-200 rounded-xl p-6 font-mono text-xs">
      <pre className="text-slate-700 leading-relaxed overflow-x-auto">
{`┌──────────────────────────────────────────────────────────────┐
│                    Driver Data Flow                          │
└──────────────────────────────────────────────────────────────┘

1. Driver opts-in to data sharing
   ├─ Accepts data sharing terms
   ├─ Sets data sharing preferences
   └─ Links wallet for micropayments

2. Driver's device collects data during rides
   ├─ GPS coordinates (anonymized)
   ├─ Speed, acceleration, braking patterns
   ├─ Route choices and traffic conditions
   └─ Road quality, potholes, obstacles

3. Data stored on Driver's OASIS Avatar
   ├─ Encrypted and attached to driver's Avatar
   ├─ Driver maintains ownership
   └─ Versioned and immutable (blockchain-backed)

4. PathPulse requests data access
   ├─ Driver receives notification
   ├─ Driver approves/rejects request
   └─ Smart contract governs data access terms

5. Data shared with PathPulse
   ├─ Anonymized and aggregated
   ├─ PathPulse improves routing algorithms
   └─ PathPulse pays for data access

6. Driver gets paid
   ├─ Micropayments to driver's wallet
   ├─ Paid in local currency, USDC, or mobile money
   └─ Driver earns karma for contributing to ecosystem`}
      </pre>
    </div>
  );
}

export default function PathPulseIntegrationViewer() {
  const data = pathPulseData;
  
  const exportPDF = () => {
    window.print();
  };
  
  return (
    <div className="min-h-screen w-full bg-gradient-to-br from-slate-50 via-blue-50 to-indigo-50 py-8 print:bg-white print:py-0 print:m-0 print:p-0">
      {/* Export Button */}
      <button
        onClick={exportPDF}
        className="fixed top-4 right-4 z-50 bg-gradient-to-r from-blue-600 to-indigo-600 hover:from-blue-700 hover:to-indigo-700 text-white px-6 py-3 rounded-lg shadow-lg font-semibold transition-all print:hidden flex items-center gap-2"
        style={{ display: typeof window !== 'undefined' && window.matchMedia ? (window.matchMedia('print').matches ? 'none' : 'flex') : 'flex' }}
      >
        <Icon name="arrow-right" className="w-4 h-4" />
        Download PDF
      </button>
      
      <div className="print-container mx-auto max-w-7xl bg-white shadow-2xl rounded-2xl overflow-hidden">
        {/* Header */}
        <div className="bg-gradient-to-br from-blue-900 via-indigo-800 to-blue-900 text-white p-12 relative overflow-hidden">
          <div className="absolute top-0 right-0 w-96 h-96 bg-white/5 rounded-full -mr-48 -mt-48" />
          <div className="absolute bottom-0 left-0 w-64 h-64 bg-indigo-500/10 rounded-full -ml-32 -mb-32" />
          <div className="relative z-10">
            {/* Partner Logos */}
            <div className="flex items-center gap-6 mb-8 pb-6">
              <img src="/pathpulse_logo.svg" alt="PathPulse.ai" className="h-12 w-auto" />
              <span className="text-3xl text-white/60 font-light">×</span>
              <div className="overflow-hidden flex items-center rounded-lg -mt-3" style={{ height: '4.5rem' }}>
                <img src="/timo_logo.png" alt="TimoRides" className="h-40 w-auto rounded-lg" style={{ objectFit: 'cover', objectPosition: 'center' }} />
              </div>
            </div>
            
            <h1 className="text-5xl font-bold leading-tight mb-3">{data.title}</h1>
            <p className="text-2xl text-white/90 mb-2">{data.subtitle}</p>
            <p className="text-sm text-white/60 mb-6">{data.version}</p>
            <p className="text-base text-white/90 leading-relaxed max-w-4xl mb-8">{data.tagline}</p>
            
            <div className="flex flex-wrap gap-4 text-sm">
              {data.contact.map((c, i) => (
                <div key={i} className="flex items-center gap-2 bg-white/10 px-4 py-2 rounded-lg backdrop-blur-sm border border-white/20">
                  <Icon name={c.icon} className="w-4 h-4" />
                  <span>{c.label}</span>
                </div>
              ))}
            </div>
          </div>
        </div>

        {/* Main Content */}
        <div className="p-12">
          {/* Executive Summary */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Executive Summary
            </h2>
            <p className="text-base text-slate-700 leading-relaxed mb-6">{data.executiveSummary.overview}</p>
            
            <div className="bg-gradient-to-r from-amber-50 to-orange-50 border-l-4 border-amber-500 rounded-lg p-6 mb-8">
              <h3 className="text-lg font-bold text-amber-900 mb-2 flex items-center gap-2">
                <span className="text-2xl">🌍</span>
                The Opportunity
              </h3>
              <p className="text-base text-amber-900/90 leading-relaxed">{data.executiveSummary.opportunityStatement}</p>
            </div>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {data.executiveSummary.benefits.map((benefit, i) => (
                <BenefitCard key={i} {...benefit} />
              ))}
            </div>
          </section>

          {/* Architecture Diagram */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Integration Architecture
            </h2>
            <ArchitectureDiagram />
          </section>

          {/* Use Cases */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Key Use Cases for PathPulse in TimoRides
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              {data.useCases.map((useCase, i) => (
                <UseCaseCard key={i} useCase={useCase} />
              ))}
            </div>
          </section>

          {/* Request Flow Diagram */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Request Flow: With PathPulse Integration
            </h2>
            <RequestFlowDiagram />
          </section>

          {/* Driver Data Monetization */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-purple-200">
              Driver Data Monetization Model
            </h2>
            <p className="text-base text-slate-700 leading-relaxed mb-6">
              {data.driverDataMonetization.overview}
            </p>
            
            <DataMonetizationFlow />
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8 mt-8">
              <div>
                <h3 className="text-xl font-bold text-slate-900 mb-4">Data Types Drivers Can Share</h3>
                <ul className="space-y-2">
                  {data.driverDataMonetization.dataTypes.map((type, i) => (
                    <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                      <span className="text-purple-500 mt-1">•</span>
                      <span>{type}</span>
                    </li>
                  ))}
                </ul>
              </div>
              
              <div>
                <h3 className="text-xl font-bold text-slate-900 mb-4">Example Driver Earnings</h3>
                {data.driverDataMonetization.earningsExamples.map((example, i) => (
                  <div key={i} className="bg-purple-50 border border-purple-200 rounded-lg p-4 mb-4">
                    <h4 className="font-bold text-purple-900 mb-2">{example.type} ({example.ridesPerMonth} rides/month)</h4>
                    <div className="text-xs text-slate-700 space-y-1">
                      <div>Base Data Sharing: {example.baseDataSharing}</div>
                      <div>Traffic Reports: {example.trafficReports}</div>
                      <div>Road Conditions: {example.roadConditions}</div>
                      <div>Revenue Share: {example.revenueShare}</div>
                      <div className="font-bold text-purple-900 pt-2 border-t border-purple-200 mt-2">
                        Total Monthly: {example.totalMonthly}
                      </div>
                      <div className="text-purple-700">Plus: {example.karmaPoints} karma points/month</div>
                    </div>
                  </div>
                ))}
              </div>
            </div>
            
            <div className="mt-8 bg-purple-50 border border-purple-200 rounded-lg p-6">
              <h3 className="text-xl font-bold text-slate-900 mb-4">Privacy Protection</h3>
              <ul className="space-y-2">
                {data.driverDataMonetization.privacyProtection.map((protection, i) => (
                  <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                    <Icon name="check" className="w-4 h-4 text-green-600 mt-0.5 flex-shrink-0" />
                    <span>{protection}</span>
                  </li>
                ))}
              </ul>
            </div>
          </section>

          {/* Technical Requirements */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Technical Requirements from PathPulse
            </h2>
            
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              <div>
                <h3 className="text-xl font-bold text-green-700 mb-4">✓ Must-Have Features</h3>
                <div className="space-y-4">
                  {data.technicalRequirements.mustHave.map((req, i) => (
                    <div key={i} className="bg-green-50 border border-green-200 rounded-lg p-4">
                      <h4 className="font-bold text-green-900 text-sm mb-2">{req.feature}</h4>
                      <p className="text-xs text-slate-700">{req.details}</p>
                    </div>
                  ))}
                </div>
              </div>
              
              <div>
                <h3 className="text-xl font-bold text-blue-700 mb-4">+ Nice-to-Have Features</h3>
                <div className="space-y-2">
                  {data.technicalRequirements.niceToHave.map((feature, i) => (
                    <div key={i} className="bg-blue-50 border border-blue-200 rounded-lg p-3">
                      <p className="text-sm text-slate-700">{feature}</p>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </section>

          {/* Timeline */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Proposed 8-Week Integration Timeline
            </h2>
            <div className="mt-8">
              {data.timeline.map((phase, i) => (
                <TimelinePhase key={i} phase={phase} />
              ))}
            </div>
          </section>

          {/* Success Metrics */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Success Metrics
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-6">
                <h3 className="text-xl font-bold text-blue-900 mb-4">Technical KPIs</h3>
                <ul className="space-y-2">
                  {data.successMetrics.technical.map((metric, i) => (
                    <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                      <Icon name="check" className="w-4 h-4 text-blue-600 mt-0.5 flex-shrink-0" />
                      <span>{metric}</span>
                    </li>
                  ))}
                </ul>
              </div>
              
              <div className="bg-green-50 border border-green-200 rounded-lg p-6">
                <h3 className="text-xl font-bold text-green-900 mb-4">Business KPIs</h3>
                <ul className="space-y-2">
                  {data.successMetrics.business.map((metric, i) => (
                    <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                      <Icon name="check" className="w-4 h-4 text-green-600 mt-0.5 flex-shrink-0" />
                      <span>{metric}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </section>

          {/* Market Data */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              TimoRides Market Data & Projections
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
                <h3 className="text-lg font-bold text-slate-900 mb-4">Estimated Volume</h3>
                <ul className="space-y-2">
                  {data.marketData.estimatedVolume.map((item, i) => (
                    <li key={i} className="text-xs text-slate-700">{item}</li>
                  ))}
                </ul>
              </div>
              
              <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
                <h3 className="text-lg font-bold text-slate-900 mb-4">Current Costs</h3>
                <ul className="space-y-2">
                  {data.marketData.currentCosts.map((item, i) => (
                    <li key={i} className="text-xs text-slate-700">{item}</li>
                  ))}
                </ul>
              </div>
              
              <div className="bg-slate-50 border border-slate-200 rounded-lg p-6">
                <h3 className="text-lg font-bold text-slate-900 mb-4">Geographic Focus</h3>
                <ul className="space-y-2">
                  {data.marketData.geographicFocus.map((item, i) => (
                    <li key={i} className="text-xs text-slate-700">{item}</li>
                  ))}
                </ul>
              </div>
            </div>
          </section>

          {/* Questions for PathPulse */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Questions for PathPulse Team
            </h2>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
              <div>
                <h3 className="text-lg font-bold text-blue-900 mb-4 bg-blue-50 px-4 py-2 rounded-lg">Technical</h3>
                <ul className="space-y-2">
                  {data.questionsForPathPulse.technical.map((q, i) => (
                    <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                      <span className="text-blue-500 mt-1">•</span>
                      <span>{q}</span>
                    </li>
                  ))}
                </ul>
              </div>
              
              <div>
                <h3 className="text-lg font-bold text-green-900 mb-4 bg-green-50 px-4 py-2 rounded-lg">Business</h3>
                <ul className="space-y-2">
                  {data.questionsForPathPulse.business.map((q, i) => (
                    <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                      <span className="text-green-500 mt-1">•</span>
                      <span>{q}</span>
                    </li>
                  ))}
                </ul>
              </div>
              
              <div>
                <h3 className="text-lg font-bold text-purple-900 mb-4 bg-purple-50 px-4 py-2 rounded-lg">Product</h3>
                <ul className="space-y-2">
                  {data.questionsForPathPulse.product.map((q, i) => (
                    <li key={i} className="text-sm text-slate-700 flex items-start gap-2">
                      <span className="text-purple-500 mt-1">•</span>
                      <span>{q}</span>
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          </section>

          {/* Partnership Levels */}
          <section className="mb-16">
            <h2 className="text-3xl font-bold text-slate-900 mb-6 pb-3 border-b-2 border-blue-200">
              Partnership Opportunity Levels
            </h2>
            <div className="space-y-6">
              {data.partnershipLevels.map((level, i) => (
                <div key={i} className="bg-gradient-to-r from-blue-50 to-indigo-50 border-l-4 border-blue-500 rounded-lg p-6">
                  <h3 className="text-xl font-bold text-blue-900 mb-2">{level.level}</h3>
                  <p className="text-base text-slate-700 mb-3">{level.description}</p>
                  <ul className="space-y-1">
                    {level.details.map((detail, j) => (
                      <li key={j} className="text-sm text-slate-700 flex items-start gap-2">
                        <Icon name="arrow-right" className="w-3 h-3 text-blue-600 mt-1 flex-shrink-0" />
                        <span>{detail}</span>
                      </li>
                    ))}
                  </ul>
                </div>
              ))}
            </div>
          </section>
        </div>

        {/* Footer */}
        <div className="bg-gradient-to-r from-slate-100 to-blue-50 border-t-2 border-blue-200 px-12 py-8">
          <div className="flex flex-col md:flex-row justify-between items-center gap-6">
            <div>
              <p className="text-lg font-bold text-slate-900 mb-2">
                PathPulse.ai × TimoRides Integration
              </p>
              <p className="text-sm text-slate-600">
                Prepared by: TimoRides/OASIS Integration Team • October 20, 2025
              </p>
            </div>
            <div className="text-right">
              <p className="text-sm text-slate-700 font-semibold mb-1">TimoRides</p>
              <p className="text-xs text-slate-600">github.com/TimoRides</p>
              <p className="text-xs text-slate-600 mt-2">
                <span className="font-semibold">Document Status:</span> ✅ Ready for PathPulse Review
              </p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}

