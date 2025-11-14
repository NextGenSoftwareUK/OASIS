import {
  ShieldCheck,
  ScrollText,
  FileText,
  Scale,
  Landmark,
  Coins,
  Gavel,
  LineChart,
  Users,
  LucideIcon,
} from "lucide-react";

export type ModuleFieldType = "text" | "textarea" | "select" | "number" | "percentage";

export type ModuleFieldOption = {
  label: string;
  value: string;
};

export type ModuleField = {
  key: string;
  label: string;
  helper?: string;
  type: ModuleFieldType;
  required?: boolean;
  placeholder?: string;
  options?: ModuleFieldOption[];
};

export type ModuleDefinition = {
  id: string;
  name: string;
  description: string;
  required: boolean;
  allowMultiple?: boolean;
  category: "core" | "compliance" | "finance" | "legal" | "governance" | "operations";
  icon: LucideIcon;
  headlineFields: string[];
  fields: ModuleField[];
  sampleValues: Record<string, string>;
};

export const MODULE_DEFINITIONS: ModuleDefinition[] = [
  {
    id: "core-metadata",
    name: "Core Metadata",
    description: "Foundational information for the UAT: identifiers, asset class, jurisdiction, distribution status.",
    required: true,
    category: "core",
    icon: ScrollText,
    allowMultiple: false,
    headlineFields: ["assetName", "assetType", "jurisdiction"],
    fields: [
      {
        key: "assetName",
        label: "Asset Name",
        type: "text",
        required: true,
        placeholder: "OASIS Midtown Commercial Complex",
      },
      {
        key: "assetType",
        label: "Asset Type",
        type: "select",
        required: true,
        options: [
          { label: "Real Estate", value: "real-estate" },
          { label: "Infrastructure", value: "infrastructure" },
          { label: "Energy", value: "energy" },
          { label: "Media", value: "media" },
          { label: "Other RWA", value: "other" },
        ],
      },
      {
        key: "jurisdiction",
        label: "Jurisdiction",
        type: "text",
        required: true,
        placeholder: "Wyoming, United States",
      },
      {
        key: "offeringMemoUrl",
        label: "Offering Memo URL",
        type: "text",
        placeholder: "https://example.com/offering-memo.pdf",
      },
      {
        key: "summary",
        label: "Executive Summary",
        type: "textarea",
        placeholder: "High-level overview of the asset and tokenization thesis.",
      },
    ],
    sampleValues: {
      assetName: "Skyline Logistics Park",
      assetType: "real-estate",
      jurisdiction: "Cheyenne, Wyoming",
      offeringMemoUrl: "https://assets.oasis.com/uat/skyline-offering.pdf",
      summary:
        "Tokenized logistics park with 220k sq ft warehouse capacity, secured by triple-net leases and 6.8% forecasted yield.",
    },
  },
  {
    id: "trust-structure",
    name: "Trust Structure",
    description: "Define trustee, beneficiaries, and legal structure underpinning the asset trust.",
    required: true,
    category: "legal",
    icon: Landmark,
    allowMultiple: false,
    headlineFields: ["trustName", "trustee"],
    fields: [
      {
        key: "trustName",
        label: "Trust Name",
        type: "text",
        required: true,
        placeholder: "Skyline Logistics Statutory Trust",
      },
      {
        key: "trustee",
        label: "Trustee",
        type: "text",
        required: true,
        placeholder: "OASIS Trust Company LLC",
      },
      {
        key: "beneficiaries",
        label: "Beneficiaries",
        type: "textarea",
        helper: "List primary and contingent beneficiaries with allocation notes.",
      },
      {
        key: "trustAgreementUrl",
        label: "Trust Agreement URL",
        type: "text",
        placeholder: "https://example.com/trust-agreement.pdf",
      },
    ],
    sampleValues: {
      trustName: "Wyoming RWA Series 12 Trust",
      trustee: "OASIS Trust Company LLC",
      beneficiaries: "Series token holders (90%), OASIS Treasury (10%)",
      trustAgreementUrl: "https://assets.oasis.com/uat/series12-trust-agreement.pdf",
    },
  },
  {
    id: "yield-distribution",
    name: "Yield Distribution",
    description: "Configure x402 revenue splits, distribution cadence, and reinvestment logic.",
    required: true,
    category: "finance",
    icon: Coins,
    allowMultiple: false,
    headlineFields: ["split", "cadence"],
    fields: [
      {
        key: "split",
        label: "Holder / Treasury Split",
        type: "text",
        required: true,
        placeholder: "90 / 10",
      },
      {
        key: "cadence",
        label: "Distribution Cadence",
        type: "select",
        required: true,
        options: [
          { label: "Monthly", value: "monthly" },
          { label: "Quarterly", value: "quarterly" },
          { label: "Semi-Annual", value: "semi-annual" },
          { label: "Annual", value: "annual" },
        ],
      },
      {
        key: "distributionMechanism",
        label: "Distribution Mechanism",
        type: "textarea",
        placeholder: "Describe hooks, webhooks, wallets, and any reinvestment policy.",
      },
      {
        key: "x402Endpoint",
        label: "x402 Endpoint",
        type: "text",
        placeholder: "https://api.oasis.com/x402/distribute",
      },
    ],
    sampleValues: {
      split: "90 / 10",
      cadence: "quarterly",
      distributionMechanism:
        "Automated x402 disbursement to holder wallets, 10% treasury reinvestment into liquidity reserves.",
      x402Endpoint: "https://api.oasis.com/x402/distribute/uat-series12",
    },
  },
  {
    id: "legal-documents",
    name: "Legal Documents",
    description: "Attach term sheets, offering memorandums, audited financials, and compliance certificates.",
    required: true,
    category: "legal",
    icon: FileText,
    allowMultiple: true,
    headlineFields: ["documentName", "documentType"],
    fields: [
      {
        key: "documentName",
        label: "Document Name",
        type: "text",
        required: true,
        placeholder: "Series 12 Offering Memorandum",
      },
      {
        key: "documentType",
        label: "Document Type",
        type: "select",
        required: true,
        options: [
          { label: "Offering Memorandum", value: "offering-memo" },
          { label: "Appraisal Report", value: "appraisal" },
          { label: "Audit", value: "audit" },
          { label: "Compliance Certificate", value: "compliance" },
          { label: "Insurance Certificate", value: "insurance" },
        ],
      },
      {
        key: "documentUrl",
        label: "Document URL",
        type: "text",
        required: true,
        placeholder: "https://assets.oasis.com/uat/documents/series12-offering.pdf",
      },
      {
        key: "effectiveDate",
        label: "Effective Date",
        type: "text",
        placeholder: "2025-10-01",
      },
    ],
    sampleValues: {
      documentName: "Series 12 Offering Memorandum",
      documentType: "offering-memo",
      documentUrl: "https://assets.oasis.com/uat/documents/series12-offering.pdf",
      effectiveDate: "2025-09-15",
    },
  },
  {
    id: "compliance-controls",
    name: "Compliance Controls",
    description: "Track KYC/AML providers, investor verification, and sanctions screening evidence.",
    required: true,
    category: "compliance",
    icon: ShieldCheck,
    allowMultiple: false,
    headlineFields: ["kycProvider", "amlStatus"],
    fields: [
      {
        key: "kycProvider",
        label: "KYC Provider",
        type: "text",
        required: true,
        placeholder: "Plaid KYC",
      },
      {
        key: "amlStatus",
        label: "AML Status",
        type: "select",
        required: true,
        options: [
          { label: "Pending", value: "pending" },
          { label: "Approved", value: "approved" },
          { label: "Manual Review", value: "manual-review" },
        ],
      },
      {
        key: "complianceNotes",
        label: "Compliance Notes",
        type: "textarea",
        placeholder: "Describe compliance checkpoints, sanctions screening cadence, and exemptions.",
      },
      {
        key: "providerCallback",
        label: "Provider Callback URL",
        type: "text",
        placeholder: "https://api.oasis.com/compliance/callback",
      },
    ],
    sampleValues: {
      kycProvider: "Synapse KYC",
      amlStatus: "approved",
      complianceNotes: "Accredited investor checks + OFAC screening. 24h timelock for non-KYC withdrawals.",
      providerCallback: "https://api.oasis.com/compliance/callback/series12",
    },
  },
  {
    id: "insurance-coverage",
    name: "Insurance Coverage",
    description: "Record coverage policies, carriers, insured amounts, and claims procedures.",
    required: false,
    category: "operations",
    icon: ShieldCheck,
    allowMultiple: true,
    headlineFields: ["policyNumber", "carrier"],
    fields: [
      {
        key: "policyNumber",
        label: "Policy Number",
        type: "text",
        required: true,
        placeholder: "POL-2025-8831",
      },
      {
        key: "carrier",
        label: "Carrier",
        type: "text",
        required: true,
        placeholder: "Mutual Guardian Insurance",
      },
      {
        key: "coverageAmount",
        label: "Coverage Amount (USD)",
        type: "text",
        placeholder: "5000000",
      },
      {
        key: "policyUrl",
        label: "Policy Documents URL",
        type: "text",
        placeholder: "https://assets.oasis.com/uat/documents/insurance-series12.pdf",
      },
    ],
    sampleValues: {
      policyNumber: "POL-2025-8831",
      carrier: "Mutual Guardian Insurance",
      coverageAmount: "5000000",
      policyUrl: "https://assets.oasis.com/uat/documents/insurance-series12.pdf",
    },
  },
  {
    id: "valuation",
    name: "Valuation",
    description: "Capture valuation approach, appraisal data, and independent assessor details.",
    required: true,
    category: "finance",
    icon: LineChart,
    allowMultiple: false,
    headlineFields: ["valuationDate", "currentValue"],
    fields: [
      {
        key: "valuationDate",
        label: "Valuation Date",
        type: "text",
        required: true,
        placeholder: "2025-08-31",
      },
      {
        key: "valuationMethod",
        label: "Valuation Method",
        type: "select",
        options: [
          { label: "Discounted Cash Flow", value: "dcf" },
          { label: "Comparable Sales", value: "comparable" },
          { label: "Capitalization Rate", value: "cap-rate" },
          { label: "Third-Party Appraisal", value: "third-party" },
        ],
        placeholder: "Select method",
      },
      {
        key: "currentValue",
        label: "Current Appraised Value (USD)",
        type: "text",
        required: true,
        placeholder: "12500000",
      },
      {
        key: "assessor",
        label: "Assessor",
        type: "text",
        placeholder: "Summit Valuation Partners",
      },
    ],
    sampleValues: {
      valuationDate: "2025-09-07",
      valuationMethod: "third-party",
      currentValue: "12800000",
      assessor: "Summit Valuation Partners",
    },
  },
  {
    id: "governance",
    name: "Governance",
    description: "Optional governance module for voting thresholds, quorums, and delegated rights.",
    required: false,
    category: "governance",
    icon: Gavel,
    allowMultiple: false,
    headlineFields: ["votingModel", "quorum"],
    fields: [
      {
        key: "votingModel",
        label: "Voting Model",
        type: "select",
        options: [
          { label: "Token-weighted", value: "token-weighted" },
          { label: "One-holder-one-vote", value: "one-holder" },
          { label: "Board delegated", value: "board-delegated" },
        ],
      },
      {
        key: "quorum",
        label: "Quorum Requirement (%)",
        type: "text",
        placeholder: "60",
      },
      {
        key: "timelock",
        label: "Execution Timelock (hrs)",
        type: "text",
        placeholder: "48",
      },
      {
        key: "delegationNotes",
        label: "Delegation Notes",
        type: "textarea",
        placeholder: "Describe delegated authorities and escalation paths.",
      },
    ],
    sampleValues: {
      votingModel: "token-weighted",
      quorum: "60",
      timelock: "48",
      delegationNotes: "Multi-sig council (5 of 7) with emergency circuit breaker at 10% drawdown.",
    },
  },
  {
    id: "asset-details",
    name: "Asset Details",
    description: "Rich metadata about the underlying asset including location, condition, and operational status.",
    required: true,
    category: "core",
    icon: Users,
    allowMultiple: false,
    headlineFields: ["location", "status"],
    fields: [
      {
        key: "location",
        label: "Location",
        type: "text",
        required: true,
        placeholder: "Cheyenne, WY",
      },
      {
        key: "status",
        label: "Operational Status",
        type: "select",
        required: true,
        options: [
          { label: "Stabilized", value: "stabilized" },
          { label: "Under Development", value: "development" },
          { label: "Renovation", value: "renovation" },
          { label: "Pre-Revenue", value: "pre-revenue" },
        ],
      },
      {
        key: "squareFootage",
        label: "Square Footage",
        type: "text",
        placeholder: "220000",
      },
      {
        key: "occupancyRate",
        label: "Occupancy Rate (%)",
        type: "text",
        placeholder: "92",
      },
    ],
    sampleValues: {
      location: "Cheyenne, WY",
      status: "stabilized",
      squareFootage: "220000",
      occupancyRate: "92",
    },
  },
  {
    id: "risk-controls",
    name: "Risk Controls",
    description: "Document timelocks, circuit breakers, and drawdown monitoring for the tokenized asset.",
    required: true,
    category: "operations",
    icon: Scale,
    allowMultiple: false,
    headlineFields: ["timelockHours", "maxDrawdown"],
    fields: [
      {
        key: "timelockHours",
        label: "Timelock (hours)",
        type: "text",
        required: true,
        placeholder: "24",
      },
      {
        key: "maxDrawdown",
        label: "Max Drawdown (%)",
        type: "text",
        required: true,
        placeholder: "15",
      },
      {
        key: "circuitBreakerLogic",
        label: "Circuit Breaker Logic",
        type: "textarea",
        placeholder: "Describe conditions under which trading halts or withdrawals pause.",
      },
    ],
    sampleValues: {
      timelockHours: "24",
      maxDrawdown: "15",
      circuitBreakerLogic: "Automatic halt if 12h drawdown exceeds 12% or liquidity drops below $1.2M.",
    },
  },
];

export const MODULE_DEFINITION_MAP: Record<string, ModuleDefinition> = MODULE_DEFINITIONS.reduce(
  (acc, module) => {
    acc[module.id] = module;
    return acc;
  },
  {} as Record<string, ModuleDefinition>
);



