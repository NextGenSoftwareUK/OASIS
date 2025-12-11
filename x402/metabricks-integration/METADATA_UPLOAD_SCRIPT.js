/**
 * Upload Updated MetaBricks Metadata to Pinata with x402
 * 
 * This script creates and uploads metadata for all 432 MetaBricks
 * with x402 revenue distribution embedded
 */

const axios = require('axios');
const FormData = require('form-data');
const fs = require('fs');

// Pinata credentials (get from environment or config)
const PINATA_API_KEY = process.env.PINATA_API_KEY;
const PINATA_SECRET = process.env.PINATA_SECRET;

// x402 configuration (same for all bricks)
const X402_CONFIG = {
  enabled: true,
  version: "1.0",
  protocolVersion: "x402-v1",
  distributionPercentage: 50,
  revenueSource: "AssetRail Smart Contract Generator",
  distributionModel: "equal",
  treasuryAddress: "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1",
  totalSupply: 432,
  webhookEndpoint: "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook",
  collectionIdentifier: "MBRICK_X402_V1",
  verificationUrl: "https://metabricks.xyz/verify-x402",
  distributionHistory: "https://metabricks.xyz/api/metabricks/stats"
};

// Brick type mapping (from server.js)
function getBrickType(brickNumber) {
  const legendaryBricks = [78, 108, 153, 155, 185, 219, 278, 296, 397, 431, 432];
  const industrialBricks = [2, 19, 23, 27, 28, 30, 33, 35, 38, 59, 68, 69, 71, 76, 77, 82, 90, 118, 125, 129, 132, 138, 140, 141, 146, 151, 155, 159, 180, 186, 205, 210, 212, 214, 218, 229, 231, 239, 248, 257, 258, 261, 275, 283, 284, 288, 292, 301, 311, 312, 314, 318, 350, 352, 356, 374, 379, 383, 403, 420];
  
  if (legendaryBricks.includes(brickNumber)) {
    return 'legendary';
  } else if (industrialBricks.includes(brickNumber)) {
    return 'industrial';
  } else {
    return 'regular';
  }
}

// Get image URL based on brick type
function getImageUrl(brickType) {
  switch (brickType) {
    case 'legendary':
      return 'https://gateway.pinata.cloud/ipfs/bafkreif7dqxfv2r76zvzxrkx3uhyklcqt7fv4vxqfqnb27z7uwwzskt2cu';
    case 'industrial':
      return 'https://gateway.pinata.cloud/ipfs/bafkreigqsyyi6qumiq544of4kzwfgffohvnvq36usivstvrfyw52u5qxf4';
    default:
      return 'https://gateway.pinata.cloud/ipfs/bafkreibhok44eomzkubmt3e2kzxip3w3b4pclixvgff5q7awhfa7kwlwsq';
  }
}

// Generate metadata for a single brick
function generateMetadata(brickNumber) {
  const brickType = getBrickType(brickNumber);
  const imageUrl = getImageUrl(brickType);
  
  return {
    name: `MetaBrick #${brickNumber}`,
    symbol: "MBRICK",
    description: `MetaBrick NFT #${brickNumber}. Earn passive income from AssetRail Smart Contract Generator API revenue. This ${brickType} brick automatically distributes 50% of SC-Gen revenue equally among all 432 brick holders. When you transfer this NFT, the distribution rights transfer with it.`,
    image: imageUrl,
    
    attributes: [
      {
        trait_type: "Brick Number",
        value: brickNumber.toString()
      },
      {
        trait_type: "Brick Type",
        value: brickType.charAt(0).toUpperCase() + brickType.slice(1)
      },
      {
        trait_type: "x402 Revenue Sharing",
        value: "Enabled"
      },
      {
        trait_type: "Distribution Model",
        value: "Equal Split (1/432)"
      },
      {
        trait_type: "Holder Share",
        value: "50% of SC-Gen Revenue"
      },
      {
        trait_type: "Revenue Source",
        value: "AssetRail SC-Gen API"
      },
      {
        trait_type: "Treasury Address",
        value: "H8tjf...QJ1"
      },
      {
        trait_type: "Token Airdrop",
        value: "Guaranteed",
        description: "Token airdrop on TGE"
      },
      {
        trait_type: "TGE Discount",
        value: "15%",
        description: "Token Generation Event discount"
      }
    ],
    
    // CRITICAL: x402 configuration embedded
    x402: X402_CONFIG,
    
    external_url: "https://metabricks.xyz",
    seller_fee_basis_points: 500,
    
    properties: {
      files: [
        {
          uri: imageUrl,
          type: "image/png"
        }
      ],
      category: "image",
      creators: [
        {
          address: "85ArqfA2fy8spGcMGsSW7cbEJAWj26vewmmoG2bwkgT9",
          share: 100
        }
      ]
    }
  };
}

// Upload to Pinata
async function uploadToPinata(metadata, filename) {
  const url = 'https://api.pinata.cloud/pinning/pinJSONToIPFS';
  
  const response = await axios.post(
    url,
    {
      pinataContent: metadata,
      pinataMetadata: {
        name: filename
      }
    },
    {
      headers: {
        'pinata_api_key': PINATA_API_KEY,
        'pinata_secret_api_key': PINATA_SECRET
      }
    }
  );
  
  return {
    ipfsHash: response.data.IpfsHash,
    url: `https://gateway.pinata.cloud/ipfs/${response.data.IpfsHash}`
  };
}

// Main execution
async function main() {
  if (!PINATA_API_KEY || !PINATA_SECRET) {
    console.error('❌ Pinata credentials not set!');
    console.log('   Set PINATA_API_KEY and PINATA_SECRET environment variables');
    process.exit(1);
  }
  
  console.log('🚀 Uploading x402-enabled metadata for 432 MetaBricks...\n');
  
  const results = [];
  
  for (let i = 1; i <= 432; i++) {
    try {
      console.log(`Uploading Brick #${i}...`);
      
      const metadata = generateMetadata(i);
      const result = await uploadToPinata(metadata, `metabrick-${i}-x402.json`);
      
      results.push({
        brickNumber: i,
        ipfsHash: result.ipfsHash,
        url: result.url
      });
      
      console.log(`✅ Brick #${i}: ${result.url}`);
      
      // Rate limit: 1 request per second
      await new Promise(resolve => setTimeout(resolve, 1000));
      
    } catch (error) {
      console.error(`❌ Failed to upload Brick #${i}:`, error.message);
    }
  }
  
  // Save results to file
  fs.writeFileSync(
    'metadata-upload-results.json',
    JSON.stringify(results, null, 2)
  );
  
  console.log('\n✅ Upload complete!');
  console.log(`   Results saved to: metadata-upload-results.json`);
  console.log(`\n📋 Next: Update server.js BRICK_METADATA_URLS with new IPFS hashes`);
}

// Run if called directly
if (require.main === module) {
  main().catch(console.error);
}

module.exports = { generateMetadata, uploadToPinata };


