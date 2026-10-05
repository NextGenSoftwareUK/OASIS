# Universal Asset Token (UAT) Specification
## Technical Brief for Implementation & Web4/Web5 NFT Compatibility Analysis

**Document Version:** 1.0  
**Date:** October 17, 2025  
**Project:** AssetRail - Quantum Securities Platform  
**Purpose:** Define a universal, modular token standard for multi-chain real-world asset tokenization

---

## Executive Summary

The **Universal Asset Token (UAT)** is a cross-chain token standard designed to represent fractional ownership of real-world assets (RWA) across Ethereum, Solana, and Radix blockchains. It provides a modular metadata framework that supports property tokenization, trust structures, yield distribution, and compliance tracking.

This specification enables AssetRail's smart contract generator to produce tokens with consistent metadata across all supported chains while maintaining blockchain-specific optimizations.

---

## 1. Overview

### 1.1 Problem Statement

Current AssetRail implementation faces:
- **Inconsistent metadata** across blockchains (Ethereum, Solana, Radix)
- **Limited extensibility** for different asset types (real estate, art, commodities, intellectual property)
- **No standardized trust/legal wrapper** integration at the token level
- **Fragmented yield distribution** tracking
- **Compliance gaps** for regulatory requirements (KYC, accreditation, jurisdiction)

### 1.2 Solution: Universal Asset Token

A **modular, extensible metadata standard** that:
- Works across all supported blockchains
- Supports multiple asset classes
- Embeds trust/legal structures
- Tracks yield and distributions
- Enables compliance and regulatory integration
- Compatible with existing NFT standards (ERC-721, ERC-1155, Metaplex, Radix Component)

---

## 2. Core Architecture

### 2.1 Token Types

```typescript
enum TokenType {
  FRACTIONAL_OWNERSHIP = 'fractional',    // Multiple owners, divisible
  WHOLE_ASSET = 'whole',                  // Single owner, non-divisible  
  YIELD_BEARING = 'yield',                // Generates distributions
  GOVERNANCE = 'governance',              // Voting rights
  HYBRID = 'hybrid'                       // Combination of above
}
```

### 2.2 Asset Classes

```typescript
enum AssetClass {
  REAL_ESTATE = 'real_estate',
  COMMERCIAL_PROPERTY = 'commercial',
  RESIDENTIAL_PROPERTY = 'residential',
  ART = 'art',
  COLLECTIBLES = 'collectibles',
  COMMODITIES = 'commodities',
  INTELLECTUAL_PROPERTY = 'ip',
  SECURITIES = 'securities',
  DEBT_INSTRUMENT = 'debt',
  CUSTOM = 'custom'
}
```

---

## 3. Modular Metadata Specification

### 3.1 Core Metadata (Required)

**Every UAT must include:**

```json
{
  "standard": "UAT-1.0",
  "schema_version": "1.0.0",
  "token_type": "fractional",
  "asset_class": "real_estate",
  
  "name": "Beverly Hills Estate Token",
  "symbol": "BHE",
  "description": "Fractional ownership of luxury property at 123 Sunset Blvd",
  
  "total_supply": 3500,
  "decimals": 0,
  "divisible": false,
  
  "issuer": {
    "name": "Quantum Securities Trust",
    "legal_entity": "Wyoming Statutory Trust",
    "jurisdiction": "Wyoming, USA",
    "registration_number": "WST-2025-001234",
    "contact": {
      "email": "trust@quantumsecurities.com",
      "website": "https://quantumsecurities.com"
    }
  },
  
  "created_at": "2025-10-17T12:00:00Z",
  "updated_at": "2025-10-17T12:00:00Z",
  
  "blockchain": {
    "chain": "solana",
    "network": "devnet",
    "contract_address": "7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU",
    "token_program": "TokenkegQfeZyiNwAJbNbGKPFXCWuBvf9Ss623VQ5DA"
  },
  
  "media": {
    "image": "ipfs://QmXz5...",
    "thumbnail": "ipfs://QmYt2...",
    "video": "ipfs://QmPl8...",
    "additional_images": [
      "ipfs://QmAd1...",
      "ipfs://QmAd2..."
    ]
  },
  
  "modules": {
    "asset_details": true,
    "trust_structure": true,
    "yield_distribution": true,
    "legal_documents": true,
    "compliance": true,
    "governance": false,
    "insurance": true,
    "valuation": true
  }
}
```

---

### 3.2 Module: Asset Details

**For real estate and physical assets:**

```json
{
  "asset_details": {
    "module_version": "1.0",
    "asset_type": "residential_property",
    
    "physical_address": {
      "street": "123 Sunset Boulevard",
      "city": "Beverly Hills",
      "state": "California",
      "zip": "90210",
      "country": "United States",
      "coordinates": {
        "latitude": 34.0736,
        "longitude": -118.4004
      }
    },
    
    "property_characteristics": {
      "square_footage": 3500,
      "lot_size": 8500,
      "bedrooms": 5,
      "bathrooms": 4.5,
      "year_built": 1985,
      "year_renovated": 2020,
      "stories": 2,
      "parking_spaces": 3,
      "property_type": "single_family",
      "architectural_style": "Mediterranean"
    },
    
    "valuation": {
      "appraised_value": 1890000,
      "appraisal_date": "2025-09-15",
      "appraiser": {
        "name": "Premier Valuation Services",
        "license": "CA-APP-12345",
        "appraiser_name": "Jane Thompson, MAI"
      },
      "market_value": 1920000,
      "assessed_tax_value": 1750000,
      "ai_estimated_value": 1885000,
      "ai_confidence": 87.5,
      "comparable_properties": [
        {
          "address": "125 Sunset Blvd",
          "sold_price": 1850000,
          "sold_date": "2025-08-01",
          "similarity_score": 0.92
        }
      ]
    },
    
    "ownership_history": {
      "previous_owner": "John Smith Family Trust",
      "acquisition_date": "2015-03-20",
      "acquisition_price": 1200000,
      "title_status": "clear",
      "liens": []
    },
    
    "condition": {
      "overall_rating": "excellent",
      "last_inspection_date": "2025-09-01",
      "inspector": "HomeGuard Inspections",
      "major_repairs_needed": false,
      "recent_upgrades": [
        "New HVAC system (2023)",
        "Roof replacement (2021)",
        "Kitchen remodel (2020)"
      ]
    }
  }
}
```

---

### 3.3 Module: Trust Structure

**Wyoming Statutory Trust / SPV integration:**

```json
{
  "trust_structure": {
    "module_version": "1.0",
    "legal_entity_type": "Wyoming Statutory Trust",
    "trust_name": "Beverly Hills Property Trust 2025-A",
    "trust_agreement_hash": "ipfs://QmTrust123...",
    
    "formation": {
      "jurisdiction": "Wyoming",
      "formation_date": "2025-09-01",
      "trust_ein": "88-1234567",
      "registered_agent": {
        "name": "Wyoming Trust Services LLC",
        "address": "123 Main St, Cheyenne, WY 82001"
      }
    },
    
    "roles": {
      "settlor": {
        "name": "Quantum Securities LLC",
        "entity_type": "LLC",
        "jurisdiction": "Delaware"
      },
      "trustee": {
        "name": "Professional Trustees Inc",
        "entity_type": "Corporation",
        "license": "WY-TRUST-5678",
        "fiduciary_bond": 1000000,
        "contact": "trustees@protrustees.com"
      },
      "beneficiaries": {
        "type": "token_holders",
        "total_count": "dynamic",
        "min_holding": 1,
        "max_per_holder": 500
      }
    },
    
    "governance": {
      "trustee_fee_rate": 0.01,
      "trustee_fee_frequency": "annual",
      "amendment_threshold": 0.67,
      "dissolution_threshold": 0.75,
      "voting_mechanism": "on_chain",
      "quorum_requirement": 0.51
    },
    
    "duration": {
      "term_years": 30,
      "termination_date": "2055-09-01",
      "early_termination_allowed": true,
      "extension_allowed": true,
      "extension_vote_required": 0.75
    },
    
    "purpose": "To hold legal title to the Property located at 123 Sunset Boulevard, Beverly Hills, CA 90210 and distribute income to token holders as beneficial owners."
  }
}
```

---

### 3.4 Module: Yield Distribution

**Income tracking and distribution schedule:**

```json
{
  "yield_distribution": {
    "module_version": "1.0",
    "enabled": true,
    
    "income_sources": [
      {
        "type": "rental_income",
        "annual_amount": 94500,
        "payment_frequency": "monthly",
        "tenant": "Redacted for privacy",
        "lease_expiry": "2027-12-31",
        "payment_history_url": "ipfs://QmPayments..."
      }
    ],
    
    "expenses": {
      "annual_property_tax": 21000,
      "annual_insurance": 4500,
      "annual_maintenance": 8000,
      "annual_hoa_fees": 0,
      "annual_management_fee": 5670,
      "annual_trustee_fee": 1890,
      "total_annual_expenses": 41060
    },
    
    "net_income": {
      "annual": 53440,
      "monthly_average": 4453.33,
      "per_token_annual": 15.27,
      "per_token_monthly": 1.27
    },
    
    "distribution_policy": {
      "frequency": "quarterly",
      "distribution_rate": 0.90,
      "reserve_rate": 0.10,
      "distribution_dates": [
        "2025-12-31",
        "2026-03-31",
        "2026-06-30",
        "2026-09-30"
      ],
      "minimum_distribution": 0.01,
      "automatic_reinvestment_option": true
    },
    
    "distribution_waterfall": [
      {
        "priority": 1,
        "recipient": "operating_expenses",
        "allocation": "actual",
        "description": "Property taxes, insurance, maintenance"
      },
      {
        "priority": 2,
        "recipient": "trustee_fees",
        "allocation": 0.01,
        "description": "Trustee compensation"
      },
      {
        "priority": 3,
        "recipient": "reserve_fund",
        "allocation": 0.10,
        "description": "Capital improvements and contingencies"
      },
      {
        "priority": 4,
        "recipient": "token_holders",
        "allocation": 0.90,
        "description": "Pro-rata distribution to all token holders"
      }
    ],
    
    "historical_distributions": [
      {
        "distribution_date": "2025-09-30",
        "total_amount": 12086.00,
        "per_token_amount": 3.45,
        "distribution_tx": "DFj2wK9v...",
        "recipients": 3500
      }
    ],
    
    "projected_returns": {
      "year_1": {
        "net_income": 53440,
        "distribution_to_holders": 48096,
        "yield_percentage": 7.48,
        "roi_on_token_price": 8.91
      },
      "year_5": {
        "net_income": 58900,
        "distribution_to_holders": 53010,
        "yield_percentage": 8.25,
        "cumulative_roi": 44.55
      },
      "year_10": {
        "net_income": 70500,
        "distribution_to_holders": 63450,
        "yield_percentage": 9.88,
        "cumulative_roi": 98.75,
        "property_appreciation_estimate": 25.0
      }
    }
  }
}
```

---

### 3.5 Module: Legal Documents

**All supporting documentation:**

```json
{
  "legal_documents": {
    "module_version": "1.0",
    
    "core_documents": {
      "trust_agreement": {
        "url": "ipfs://QmTrustDoc...",
        "hash": "0x7f83b1657ff1fc53b92dc18148a1d65dfc2d4b1fa3d677284addd200126d9069",
        "signed_date": "2025-09-01",
        "signatures": [
          {
            "role": "settlor",
            "signer": "Quantum Securities LLC",
            "signature": "0x...",
            "timestamp": "2025-09-01T10:00:00Z"
          }
        ]
      },
      
      "operating_agreement": {
        "url": "ipfs://QmOpAgree...",
        "hash": "0x...",
        "version": "1.0",
        "effective_date": "2025-09-01"
      },
      
      "tokenization_memorandum": {
        "url": "ipfs://QmTokenMemo...",
        "hash": "0x...",
        "offering_date": "2025-10-01",
        "total_offering": 1890000
      }
    },
    
    "property_documents": {
      "title_deed": {
        "url": "ipfs://QmTitleDeed...",
        "hash": "0x...",
        "recording_date": "2015-03-25",
        "county": "Los Angeles County",
        "book_page": "Book 12345, Page 678"
      },
      
      "title_insurance": {
        "url": "ipfs://QmTitleIns...",
        "policy_number": "TI-2025-123456",
        "insurer": "First American Title",
        "coverage_amount": 1890000,
        "effective_date": "2025-09-01",
        "expiry_date": "perpetual"
      },
      
      "property_appraisal": {
        "url": "ipfs://QmAppraisal...",
        "appraisal_date": "2025-09-15",
        "appraised_value": 1890000,
        "appraiser_license": "CA-APP-12345"
      },
      
      "property_survey": {
        "url": "ipfs://QmSurvey...",
        "survey_date": "2025-08-20",
        "surveyor": "Precision Land Surveys",
        "surveyor_license": "CA-LS-67890"
      },
      
      "hazard_insurance": {
        "url": "ipfs://QmHazardIns...",
        "policy_number": "HO-2025-987654",
        "insurer": "State Farm Insurance",
        "coverage_amount": 2100000,
        "effective_date": "2025-09-01",
        "expiry_date": "2026-09-01",
        "premium_annual": 4500
      }
    },
    
    "compliance_documents": {
      "securities_registration": {
        "type": "Regulation D 506(c)",
        "filing_number": "021-123456",
        "filing_date": "2025-08-15",
        "jurisdiction": "SEC",
        "url": "ipfs://QmRegD..."
      },
      
      "private_placement_memorandum": {
        "url": "ipfs://QmPPM...",
        "date": "2025-09-01",
        "version": "1.0"
      },
      
      "subscription_agreement_template": {
        "url": "ipfs://QmSubAgree...",
        "version": "1.0",
        "required_for_purchase": true
      }
    },
    
    "tax_documents": {
      "k1_template": {
        "url": "ipfs://QmK1Template...",
        "form_type": "Schedule K-1",
        "tax_year": 2025
      },
      "ein_confirmation": {
        "url": "ipfs://QmEIN...",
        "trust_ein": "88-1234567"
      }
    }
  }
}
```

---

### 3.6 Module: Compliance

**KYC/AML, accreditation, and regulatory tracking:**

```json
{
  "compliance": {
    "module_version": "1.0",
    
    "regulatory_framework": {
      "primary_jurisdiction": "United States",
      "securities_classification": "Security Token",
      "regulatory_exemption": "Regulation D 506(c)",
      "accredited_investors_only": true,
      "max_investors": 2000,
      "holding_period": "12 months",
      "transfer_restrictions": true
    },
    
    "kyc_aml": {
      "required": true,
      "provider": "Jumio / Sumsub",
      "verification_level": "Level 2 (Enhanced)",
      "documents_required": [
        "Government ID",
        "Proof of Address",
        "Selfie Verification"
      ],
      "sanctions_screening": true,
      "pep_screening": true,
      "ongoing_monitoring": true
    },
    
    "accreditation": {
      "required": true,
      "verification_method": "third_party",
      "verification_provider": "VerifyInvestor",
      "criteria": [
        "Net worth > $1,000,000 (excluding primary residence)",
        "OR Annual income > $200,000 (individual) or $300,000 (joint)"
      ],
      "verification_validity": "90 days",
      "re_verification_required": true
    },
    
    "transfer_restrictions": {
      "lock_up_period": "12 months",
      "lock_up_start_date": "2025-10-01",
      "lock_up_end_date": "2026-10-01",
      "whitelist_required": true,
      "blacklist_jurisdictions": [
        "North Korea",
        "Iran",
        "Syria",
        "Cuba"
      ],
      "transfer_approval_required": true,
      "transfer_approval_authority": "trustee"
    },
    
    "reporting": {
      "tax_reporting": true,
      "tax_form": "Schedule K-1",
      "quarterly_statements": true,
      "annual_audit": true,
      "auditor": "Deloitte LLP",
      "investor_portal": "https://portal.quantumsecurities.com"
    },
    
    "data_privacy": {
      "gdpr_compliant": true,
      "ccpa_compliant": true,
      "data_controller": "Quantum Securities Trust",
      "data_processor": "Trustee",
      "privacy_policy": "ipfs://QmPrivacy...",
      "data_retention_period": "7 years post-liquidation"
    }
  }
}
```

---

### 3.7 Module: Insurance

**Property and liability coverage:**

```json
{
  "insurance": {
    "module_version": "1.0",
    
    "property_insurance": {
      "policy_number": "HO-2025-987654",
      "insurer": "State Farm Insurance",
      "insurer_rating": "A++ (AM Best)",
      "policy_type": "Dwelling Fire Policy - DP3",
      "coverage_amount": 2100000,
      "replacement_cost": true,
      "effective_date": "2025-09-01",
      "expiry_date": "2026-09-01",
      "premium_annual": 4500,
      "deductible": 2500,
      "additional_coverages": [
        "Extended Replacement Cost",
        "Ordinance or Law Coverage",
        "Loss of Rental Income"
      ],
      "policy_document": "ipfs://QmPropertyIns..."
    },
    
    "liability_insurance": {
      "policy_number": "GL-2025-456789",
      "insurer": "Chubb Insurance",
      "policy_type": "Commercial General Liability",
      "coverage_amount": 2000000,
      "aggregate_limit": 4000000,
      "effective_date": "2025-09-01",
      "expiry_date": "2026-09-01",
      "premium_annual": 1200,
      "policy_document": "ipfs://QmLiabilityIns..."
    },
    
    "title_insurance": {
      "policy_number": "TI-2025-123456",
      "insurer": "First American Title",
      "coverage_amount": 1890000,
      "effective_date": "2025-09-01",
      "policy_type": "Owner's Policy",
      "enhanced_coverage": true,
      "policy_document": "ipfs://QmTitleIns..."
    },
    
    "umbrella_coverage": {
      "policy_number": "UMB-2025-789012",
      "insurer": "Travelers Insurance",
      "coverage_amount": 5000000,
      "effective_date": "2025-09-01",
      "expiry_date": "2026-09-01",
      "premium_annual": 800
    },
    
    "claims_history": {
      "total_claims": 0,
      "last_claim_date": null,
      "claims_free_years": 10
    }
  }
}
```

---

### 3.8 Module: Valuation

**AI-powered and professional appraisals:**

```json
{
  "valuation": {
    "module_version": "1.0",
    
    "current_valuation": {
      "official_value": 1890000,
      "valuation_date": "2025-09-15",
      "valuation_method": "Professional Appraisal",
      "next_valuation_date": "2026-09-15",
      "valuation_frequency": "annual"
    },
    
    "professional_appraisal": {
      "appraiser": {
        "name": "Jane Thompson, MAI",
        "company": "Premier Valuation Services",
        "license": "CA-APP-12345",
        "credentials": ["MAI", "SRA"],
        "contact": "jthompson@premiervaluation.com"
      },
      "appraisal_date": "2025-09-15",
      "appraised_value": 1890000,
      "approach": "Sales Comparison Approach",
      "report_url": "ipfs://QmAppraisal...",
      "effective_date": "2025-09-15",
      "report_type": "Uniform Residential Appraisal Report (URAR)"
    },
    
    "ai_valuation": {
      "model": "AssetRail Valuation Engine v2.0",
      "estimated_value": 1885000,
      "confidence_score": 87.5,
      "valuation_date": "2025-10-17",
      "data_sources": [
        "US Census Bureau (Median Home Values)",
        "Zillow ZTRAX (Transaction Data)",
        "CoreLogic (Property Characteristics)",
        "Local MLS Listings"
      ],
      "comparable_properties": [
        {
          "address": "125 Sunset Blvd, Beverly Hills",
          "distance_miles": 0.2,
          "sold_price": 1850000,
          "sold_date": "2025-08-01",
          "square_footage": 3400,
          "similarity_score": 0.92,
          "adjustments": {
            "size_adjustment": 10000,
            "condition_adjustment": 5000,
            "location_adjustment": 0
          }
        },
        {
          "address": "130 Palm Dr, Beverly Hills",
          "distance_miles": 0.5,
          "sold_price": 1920000,
          "sold_date": "2025-07-15",
          "square_footage": 3600,
          "similarity_score": 0.89,
          "adjustments": {
            "size_adjustment": -15000,
            "condition_adjustment": 0,
            "location_adjustment": 5000
          }
        }
      ],
      "market_trends": {
        "12_month_appreciation": 5.2,
        "36_month_appreciation": 12.8,
        "median_days_on_market": 45,
        "market_temperature": "hot"
      }
    },
    
    "tax_assessment": {
      "assessed_value": 1750000,
      "assessment_year": 2025,
      "assessment_ratio": 0.926,
      "annual_property_tax": 21000,
      "tax_rate": 0.012
    },
    
    "valuation_history": [
      {
        "date": "2024-09-15",
        "value": 1800000,
        "method": "Professional Appraisal",
        "source": "Premier Valuation Services"
      },
      {
        "date": "2023-09-15",
        "value": 1700000,
        "method": "Professional Appraisal",
        "source": "Premier Valuation Services"
      }
    ],
    
    "price_per_token": {
      "initial_offering_price": 540.00,
      "calculation_method": "appraised_value / total_supply",
      "current_market_price": 540.00,
      "price_history": []
    }
  }
}
```

---

### 3.9 Module: Governance (Optional)

**For tokens with voting/decision rights:**

```json
{
  "governance": {
    "module_version": "1.0",
    "enabled": true,
    
    "voting_rights": {
      "one_token_one_vote": true,
      "voting_threshold": 0.67,
      "quorum_requirement": 0.51,
      "voting_period_days": 7,
      "proposal_deposit": 100
    },
    
    "proposal_types": [
      "property_sale",
      "major_renovation",
      "refinancing",
      "trustee_replacement",
      "fee_structure_change",
      "trust_amendment",
      "early_liquidation"
    ],
    
    "decision_rights": {
      "property_sale_approval": true,
      "major_capital_expenditures": true,
      "refinancing_approval": true,
      "trustee_removal": true,
      "threshold_amount": 50000
    },
    
    "voting_mechanism": {
      "platform": "Snapshot / Realms",
      "on_chain_execution": true,
      "off_chain_signaling": true,
      "voting_portal": "https://vote.quantumsecurities.com"
    }
  }
}
```

---

## 4. Blockchain-Specific Implementations

### 4.1 Ethereum (ERC-721 / ERC-1155)

```solidity
// SPDX-License-Identifier: MIT
pragma solidity ^0.8.19;

import "@openzeppelin/contracts/token/ERC1155/ERC1155.sol";

contract UniversalAssetToken is ERC1155 {
    // Core metadata stored on-chain
    string public name;
    string public symbol;
    string public uatMetadataURI; // Points to full UAT JSON
    
    // Modular flags
    bool public hasYieldDistribution;
    bool public hasTrustStructure;
    bool public hasCompliance;
    
    // Compliance mapping
    mapping(address => bool) public whitelisted;
    mapping(address => bool) public accredited;
    
    constructor(
        string memory _name,
        string memory _symbol,
        string memory _uatMetadataURI
    ) ERC1155(_uatMetadataURI) {
        name = _name;
        symbol = _symbol;
        uatMetadataURI = _uatMetadataURI;
    }
    
    // Override transfer to enforce compliance
    function safeTransferFrom(
        address from,
        address to,
        uint256 id,
        uint256 amount,
        bytes memory data
    ) public override {
        require(whitelisted[to], "Recipient not whitelisted");
        require(accredited[to], "Recipient not accredited");
        super.safeTransferFrom(from, to, id, amount, data);
    }
}
```

### 4.2 Solana (SPL Token + Metaplex)

```rust
use anchor_lang::prelude::*;
use anchor_spl::token::{self, Token, TokenAccount, Mint};

#[program]
pub mod universal_asset_token {
    use super::*;
    
    pub fn initialize(
        ctx: Context<Initialize>,
        name: String,
        symbol: String,
        uat_metadata_uri: String,
        total_supply: u64,
    ) -> Result<()> {
        let token_data = &mut ctx.accounts.token_data;
        token_data.name = name;
        token_data.symbol = symbol;
        token_data.uat_metadata_uri = uat_metadata_uri;
        token_data.total_supply = total_supply;
        token_data.authority = ctx.accounts.authority.key();
        
        // Module flags
        token_data.has_yield_distribution = true;
        token_data.has_trust_structure = true;
        token_data.has_compliance = true;
        
        Ok(())
    }
    
    pub fn mint_tokens(
        ctx: Context<MintTokens>,
        amount: u64,
    ) -> Result<()> {
        // Check compliance (whitelist, accreditation)
        require!(
            ctx.accounts.token_data.is_whitelisted(ctx.accounts.recipient.key()),
            ErrorCode::NotWhitelisted
        );
        
        // Mint tokens via SPL Token Program
        token::mint_to(
            ctx.accounts.into_mint_to_context(),
            amount,
        )?;
        
        Ok(())
    }
}

#[account]
pub struct TokenData {
    pub authority: Pubkey,
    pub name: String,
    pub symbol: String,
    pub uat_metadata_uri: String,
    pub total_supply: u64,
    pub has_yield_distribution: bool,
    pub has_trust_structure: bool,
    pub has_compliance: bool,
}
```

### 4.3 Radix (Component/Blueprint)

```rust
use scrypto::prelude::*;

#[blueprint]
mod universal_asset_token {
    struct UniversalAssetToken {
        // Token vault
        token_vault: Vault,
        
        // Core metadata
        name: String,
        symbol: String,
        uat_metadata_uri: String,
        total_supply: Decimal,
        
        // Module flags
        has_yield_distribution: bool,
        has_trust_structure: bool,
        has_compliance: bool,
        
        // Compliance
        whitelist: HashMap<ComponentAddress, bool>,
        accredited: HashMap<ComponentAddress, bool>,
    }
    
    impl UniversalAssetToken {
        pub fn instantiate(
            name: String,
            symbol: String,
            uat_metadata_uri: String,
            total_supply: Decimal,
        ) -> ComponentAddress {
            // Create token resource
            let token_bucket = ResourceBuilder::new_fungible()
                .metadata("name", name.clone())
                .metadata("symbol", symbol.clone())
                .metadata("uat_metadata_uri", uat_metadata_uri.clone())
                .mint_initial_supply(total_supply);
            
            // Instantiate component
            Self {
                token_vault: Vault::with_bucket(token_bucket),
                name,
                symbol,
                uat_metadata_uri,
                total_supply,
                has_yield_distribution: true,
                has_trust_structure: true,
                has_compliance: true,
                whitelist: HashMap::new(),
                accredited: HashMap::new(),
            }
            .instantiate()
            .globalize()
        }
    }
}
```

---

## 5. Storage & Distribution

### 5.1 Metadata Storage

**Recommended architecture:**

```
1. IPFS (Primary)
   - Full UAT JSON stored as single file
   - Pinned via Pinata/Filebase
   - Content addressing ensures immutability
   - Hash stored on-chain

2. Arweave (Backup)
   - Permanent storage
   - Pay-once, store forever
   - Used for critical documents

3. On-Chain (Minimal)
   - Token name, symbol, supply
   - IPFS CID of full metadata
   - Module activation flags
   - Compliance status
```

### 5.2 Metadata URI Structure

```
Primary: ipfs://QmXyZ.../uat-metadata.json
Backup: arweave://ABC123.../uat-metadata.json
Gateway: https://gateway.pinata.cloud/ipfs/QmXyZ.../uat-metadata.json
```

---

## 6. Integration with AssetRail Smart Contract Generator

### 6.1 Generator Input Schema

**JSON input to contract generator:**

```json
{
  "blockchain": "solana",
  "token_standard": "UAT-1.0",
  "contract_type": "universal_asset_token",
  
  "core_metadata": {
    "name": "Beverly Hills Estate Token",
    "symbol": "BHE",
    "total_supply": 3500,
    "decimals": 0
  },
  
  "modules_enabled": [
    "asset_details",
    "trust_structure",
    "yield_distribution",
    "legal_documents",
    "compliance",
    "insurance",
    "valuation"
  ],
  
  "module_data": {
    "asset_details": { /* ... */ },
    "trust_structure": { /* ... */ },
    "yield_distribution": { /* ... */ },
    "compliance": { /* ... */ }
  },
  
  "deployment_config": {
    "network": "devnet",
    "authority_wallet": "7xKXtg2CW87d97TXJSDpbD5jBkheTqA83TZRuJosgAsU",
    "initial_mint_recipient": "8yZXtg3DX98f08YKTErbM6jCkhfUqB84UZSmKqjphBtV"
  }
}
```

### 6.2 Generator Output

```json
{
  "contract_code": "/* Generated Rust/Solidity/Scrypto code */",
  "abi": "/* Contract ABI */",
  "metadata_json": "/* Full UAT metadata JSON */",
  "deployment_instructions": "/* Chain-specific deployment steps */",
  
  "ipfs_uploads": {
    "metadata": "ipfs://QmMetadata...",
    "contract_code": "ipfs://QmContract...",
    "abi": "ipfs://QmABI..."
  },
  
  "verification": {
    "metadata_hash": "0x...",
    "contract_hash": "0x...",
    "checksums_match": true
  }
}
```

---

## 7. Web4 & Web5 NFT Compatibility Analysis

### 7.1 Evaluation Criteria

To determine if your existing **Web4 & Web5 NFTs** are compatible with UAT, please evaluate against these criteria:

#### **A. Metadata Structure**
- ✅ **Compatible:** Uses JSON-based extensible metadata
- ✅ **Compatible:** Supports IPFS/decentralized storage
- ✅ **Compatible:** Separates core metadata from optional modules
- ❌ **Incompatible:** Hardcoded fixed schema with no extensibility
- ❌ **Incompatible:** Centralized metadata storage only

#### **B. Multi-Chain Support**
- ✅ **Compatible:** Works across multiple blockchains (Ethereum, Solana, Radix)
- ✅ **Compatible:** Chain-agnostic metadata format
- ⚠️ **Partial:** Single-chain only (can be extended)
- ❌ **Incompatible:** Chain-specific hardcoded implementation

#### **C. Real-World Asset Support**
- ✅ **Compatible:** Designed for physical/real-world assets
- ✅ **Compatible:** Supports legal wrappers (trusts, SPVs)
- ✅ **Compatible:** Includes compliance/regulatory fields
- ⚠️ **Partial:** Digital-only but extensible
- ❌ **Incompatible:** Pure digital collectibles with no RWA support

#### **D. Yield/Distribution Tracking**
- ✅ **Compatible:** Built-in yield distribution mechanism
- ✅ **Compatible:** Tracks income, expenses, distributions
- ⚠️ **Partial:** Basic payment tracking
- ❌ **Incompatible:** No financial tracking

#### **E. Compliance Integration**
- ✅ **Compatible:** KYC/AML, accreditation, transfer restrictions
- ✅ **Compatible:** Whitelist/blacklist mechanisms
- ⚠️ **Partial:** Basic access control
- ❌ **Incompatible:** No compliance features

#### **F. Modular Architecture**
- ✅ **Compatible:** Optional modules can be enabled/disabled
- ✅ **Compatible:** Supports custom extensions
- ⚠️ **Partial:** Extensible but not explicitly modular
- ❌ **Incompatible:** Monolithic fixed structure

### 7.2 Compatibility Matrix

| Feature | UAT Requirement | Web4/Web5 Must Have |
|---------|----------------|---------------------|
| JSON Metadata | ✅ Required | ? |
| IPFS Storage | ✅ Required | ? |
| Multi-Chain | ✅ Required | ? |
| Modular Design | ✅ Required | ? |
| Asset Details | ✅ Required (for RWA) | ? |
| Trust Structure | ✅ Required (for RWA) | ? |
| Yield Tracking | ✅ Required (for income-generating) | ? |
| Compliance | ✅ Required (for securities) | ? |
| Legal Docs | ✅ Required | ? |
| Extensibility | ✅ Required | ? |

### 7.3 Migration Path

**If Web4/Web5 NFTs are partially compatible:**

```json
{
  "migration_strategy": {
    "step_1": "Extract existing metadata",
    "step_2": "Map to UAT core metadata format",
    "step_3": "Add missing required modules",
    "step_4": "Enable optional modules as needed",
    "step_5": "Validate against UAT schema",
    "step_6": "Upload to IPFS",
    "step_7": "Deploy new UAT-compliant contract"
  },
  
  "backward_compatibility": {
    "legacy_metadata_url": "ipfs://QmOldMetadata...",
    "uat_metadata_url": "ipfs://QmNewUATMetadata...",
    "migration_date": "2025-11-01",
    "grace_period_days": 90
  }
}
```

---

## 8. Implementation Checklist

### Phase 1: Specification Finalization
- [ ] Review this specification with legal team
- [ ] Review with compliance team
- [ ] Review with development team
- [ ] Validate Web4/Web5 NFT compatibility
- [ ] Finalize module requirements
- [ ] Create reference implementations

### Phase 2: Smart Contract Development
- [ ] Develop Ethereum UAT contract template
- [ ] Develop Solana UAT program template
- [ ] Develop Radix UAT blueprint template
- [ ] Add UAT templates to contract generator
- [ ] Test cross-chain compatibility
- [ ] Security audit

### Phase 3: Metadata Infrastructure
- [ ] Set up IPFS pinning service (Pinata/Filebase)
- [ ] Create metadata validation service
- [ ] Build metadata generator UI
- [ ] Implement on-chain metadata verification
- [ ] Create metadata explorer

### Phase 4: Integration
- [ ] Update property tokenization wizard to use UAT
- [ ] Integrate with existing smart contract generator
- [ ] Build UAT minting service
- [ ] Create token holder portal
- [ ] Implement compliance verification

### Phase 5: Testing & Launch
- [ ] Testnet deployment (all chains)
- [ ] End-to-end testing
- [ ] Security audit
- [ ] Legal review
- [ ] Mainnet deployment
- [ ] Documentation and training

---

## 9. Questions for Web4/Web5 NFT Evaluation

**Please provide answers to these questions to assess compatibility:**

1. **What metadata format do Web4/Web5 NFTs use?**
   - JSON? Custom binary? Other?

2. **Where is metadata stored?**
   - IPFS? Arweave? Centralized server?

3. **Is the metadata extensible?**
   - Can new fields be added without breaking existing implementations?

4. **What blockchains are supported?**
   - Single chain? Multi-chain?

5. **Do they support real-world assets?**
   - Legal wrappers? Compliance? Physical asset tracking?

6. **Is there yield/distribution support?**
   - Income tracking? Payment distribution?

7. **What compliance features exist?**
   - KYC? Accreditation? Transfer restrictions?

8. **Is the architecture modular?**
   - Can features be enabled/disabled independently?

9. **Is there a versioning mechanism?**
   - How are metadata upgrades handled?

10. **What is the governance model?**
    - On-chain voting? Off-chain signaling?

---

## 10. Next Steps

1. **Review this specification** - Provide feedback and requirements
2. **Evaluate Web4/Web5 NFTs** - Answer compatibility questions above
3. **Determine migration strategy** - Full adoption, partial integration, or custom bridge
4. **Prioritize modules** - Which modules are critical for Phase 1?
5. **Assign development team** - Backend, smart contracts, frontend
6. **Set timeline** - Realistic deployment schedule

---

## 11. Contact & Support

**Technical Lead:** [Your Name]  
**Email:** dev@quantumsecurities.com  
**Documentation:** https://docs.quantumsecurities.com/uat  
**GitHub:** https://github.com/quantumsecurities/universal-asset-token

---

## Appendix A: Complete UAT JSON Example

See next section for a **complete, real-world example** with all modules enabled.

---

**END OF SPECIFICATION**

*Universal Asset Token (UAT) v1.0 - AssetRail Quantum Securities Platform*

