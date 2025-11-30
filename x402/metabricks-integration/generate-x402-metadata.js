/**
 * Generate and Upload x402-Enabled MetaBricks Metadata
 * 
 * Rarity-based distribution:
 * - Regular: 1x weight
 * - Industrial: 1.2x weight  
 * - Legendary: 1.5x weight
 */

const axios = require('axios');
const fs = require('fs').promises;
const path = require('path');

// Configuration
const PINATA_API_KEY = process.env.PINATA_API_KEY;
const PINATA_SECRET = process.env.PINATA_SECRET;

const TREASURY_ADDRESS = "H8tjfEMYNkTDdSbRBekMBTcE966vGRF2WnCThRRoQJ1";
const WEBHOOK_URL = "https://metabricks-backend.herokuapp.com/api/metabricks/sc-gen-webhook";

// Brick type mapping (from server.js)
const LEGENDARY_BRICKS = [78, 108, 153, 155, 185, 219, 278, 296, 397, 431, 432];
const INDUSTRIAL_BRICKS = [2, 19, 23, 27, 28, 30, 33, 35, 38, 59, 68, 69, 71, 76, 77, 82, 90, 118, 125, 129, 132, 138, 140, 141, 146, 151, 155, 159, 180, 186, 205, 210, 212, 214, 218, 229, 231, 239, 248, 257, 258, 261, 275, 283, 284, 288, 292, 301, 311, 312, 314, 318, 350, 352, 356, 374, 379, 383, 403, 420];

// Rarity weights
const WEIGHTS = {
  regular: 1.0,
  industrial: 1.2,
  legendary: 1.5
};

// Image URLs by type
const IMAGES = {
  regular: "https://gateway.pinata.cloud/ipfs/bafkreibhok44eomzkubmt3e2kzxip3w3b4pclixvgff5q7awhfa7kwlwsq",
  industrial: "https://gateway.pinata.cloud/ipfs/bafkreigqsyyi6qumiq544of4kzwfgffohvnvq36usivstvrfyw52u5qxf4",
  legendary: "https://gateway.pinata.cloud/ipfs/bafkreif7dqxfv2r76zvzxrkx3uhyklcqt7fv4vxqfqnb27z7uwwzskt2cu"
};

/**
 * Get brick type and weight
 */
function getBrickInfo(brickNumber) {
  if (LEGENDARY_BRICKS.includes(brickNumber)) {
    return { type: 'legendary', weight: WEIGHTS.legendary };
  } else if (INDUSTRIAL_BRICKS.includes(brickNumber)) {
    return { type: 'industrial', weight: WEIGHTS.industrial };
  } else {
    return { type: 'regular', weight: WEIGHTS.regular };
  }
}

/**
 * Generate metadata for a brick
 */
function generateMetadata(brickNumber) {
  const brickInfo = getBrickInfo(brickNumber);
  const imageUrl = IMAGES[brickInfo.type];
  
  return {
    name: `MetaBrick #${brickNumber}`,
    symbol: "MBRICK",
    description: `MetaBrick #${brickNumber} (${brickInfo.type.toUpperCase()}). Earn passive income from AssetRail Smart Contract Generator revenue. This ${brickInfo.type} brick receives ${brickInfo.weight}x the base distribution rate. Revenue sharing is embedded in this NFT - when you transfer it, the distribution rights transfer with it.`,
    
    image: imageUrl,
    
    attributes: [
      {
        trait_type: "Brick Number",
        value: brickNumber.toString()
      },
      {
        trait_type: "Rarity",
        value: brickInfo.type.charAt(0).toUpperCase() + brickInfo.type.slice(1)
      },
      {
        trait_type: "Distribution Weight",
        value: `${brickInfo.weight}x`,
        description: `Receives ${brickInfo.weight}x the base distribution amount`
      },
      {
        trait_type: "x402 Revenue Sharing",
        value: "Enabled"
      },
      {
        trait_type: "Revenue Source",
        value: "AssetRail SC-Gen API"
      },
      {
        trait_type: "Distribution Model",
        value: "Weighted by Rarity"
      }
    ],
    
    // x402 configuration embedded in metadata
    x402: {
      enabled: true,
      version: "1.0",
      protocolVersion: "x402-v1",
      distributionPercentage: 50,
      revenueSource: "AssetRail Smart Contract Generator",
      distributionModel: "weighted",
      weight: brickInfo.weight,
      rarity: brickInfo.type,
      treasuryAddress: TREASURY_ADDRESS,
      totalSupply: 432,
      webhookEndpoint: WEBHOOK_URL,
      collectionIdentifier: "MBRICK_X402_WEIGHTED",
      verificationUrl: "https://metabricks.xyz/verify-x402",
      distributionHistory: "https://metabricks.xyz/api/metabricks/stats"
    },
    
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

/**
 * Upload metadata to Pinata
 */
async function uploadToPinata(metadata, filename) {
  const url = 'https://api.pinata.cloud/pinning/pinJSONToIPFS';
  
  try {
    const response = await axios.post(
      url,
      {
        pinataContent: metadata,
        pinataMetadata: {
          name: filename,
          keyvalues: {
            brickNumber: metadata.x402.rarity === 'regular' ? 'regular' : metadata.x402.rarity,
            x402Enabled: 'true',
            weight: metadata.x402.weight.toString()
          }
        }
      },
      {
        headers: {
          'pinata_api_key': PINATA_API_KEY,
          'pinata_secret_api_key': PINATA_SECRET,
          'Content-Type': 'application/json'
        }
      }
    );
    
    return {
      success: true,
      ipfsHash: response.data.IpfsHash,
      url: `https://gateway.pinata.cloud/ipfs/${response.data.IpfsHash}`
    };
  } catch (error) {
    return {
      success: false,
      error: error.message
    };
  }
}

/**
 * Main execution
 */
async function main() {
  console.log('🚀 Generating x402-Enabled MetaBricks Metadata\n');
  console.log('Rarity Weights:');
  console.log('  Regular: 1.0x');
  console.log('  Industrial: 1.2x');
  console.log('  Legendary: 1.5x\n');
  
  if (!PINATA_API_KEY || !PINATA_SECRET) {
    console.error('❌ Pinata credentials not set!');
    console.log('\nSet environment variables:');
    console.log('  export PINATA_API_KEY=your_key');
    console.log('  export PINATA_SECRET=your_secret');
    process.exit(1);
  }
  
  // Calculate distribution
  const regularCount = 432 - INDUSTRIAL_BRICKS.length - LEGENDARY_BRICKS.length;
  const totalWeight = (regularCount * 1.0) + (INDUSTRIAL_BRICKS.length * 1.2) + (LEGENDARY_BRICKS.length * 1.5);
  
  console.log('Distribution Summary:');
  console.log(`  Regular: ${regularCount} bricks × 1.0 = ${regularCount * 1.0}`);
  console.log(`  Industrial: ${INDUSTRIAL_BRICKS.length} bricks × 1.2 = ${INDUSTRIAL_BRICKS.length * 1.2}`);
  console.log(`  Legendary: ${LEGENDARY_BRICKS.length} bricks × 1.5 = ${LEGENDARY_BRICKS.length * 1.5}`);
  console.log(`  Total Weight: ${totalWeight}\n`);
  
  // Example distribution
  console.log('Example: 100 SOL revenue (50 SOL to holders):');
  console.log(`  Regular brick: ${(50 / totalWeight * 1.0).toFixed(6)} SOL`);
  console.log(`  Industrial brick: ${(50 / totalWeight * 1.2).toFixed(6)} SOL (1.2x)`);
  console.log(`  Legendary brick: ${(50 / totalWeight * 1.5).toFixed(6)} SOL (1.5x)\n`);
  
  const results = [];
  const errors = [];
  
  // Option to generate without uploading (for review)
  const dryRun = process.argv.includes('--dry-run');
  
  if (dryRun) {
    console.log('🔍 DRY RUN MODE - Generating metadata only (not uploading)\n');
  }
  
  for (let i = 1; i <= 432; i++) {
    try {
      const metadata = generateMetadata(i);
      const brickInfo = getBrickInfo(i);
      
      if (dryRun) {
        // Just save to file
        const outputDir = path.join(__dirname, 'generated-metadata');
        await fs.mkdir(outputDir, { recursive: true });
        await fs.writeFile(
          path.join(outputDir, `brick-${i}.json`),
          JSON.stringify(metadata, null, 2)
        );
        
        if (i % 50 === 0 || brickInfo.type !== 'regular') {
          console.log(`✅ Generated Brick #${i} (${brickInfo.type}, ${brickInfo.weight}x)`);
        }
      } else {
        // Upload to Pinata
        console.log(`Uploading Brick #${i} (${brickInfo.type}, ${brickInfo.weight}x)...`);
        
        const result = await uploadToPinata(metadata, `metabrick-${i}-x402.json`);
        
        if (result.success) {
          results.push({
            brickNumber: i,
            type: brickInfo.type,
            weight: brickInfo.weight,
            ipfsHash: result.ipfsHash,
            url: result.url
          });
          
          console.log(`  ✅ ${result.url}`);
        } else {
          errors.push({ brickNumber: i, error: result.error });
          console.error(`  ❌ Failed: ${result.error}`);
        }
        
        // Rate limit: 2 requests per second max
        await new Promise(resolve => setTimeout(resolve, 600));
      }
      
    } catch (error) {
      errors.push({ brickNumber: i, error: error.message });
      console.error(`❌ Error on Brick #${i}:`, error.message);
    }
  }
  
  // Save results
  const resultsFile = path.join(__dirname, dryRun ? 'metadata-preview.json' : 'metadata-upload-results.json');
  await fs.writeFile(
    resultsFile,
    JSON.stringify({
      timestamp: new Date().toISOString(),
      totalGenerated: results.length,
      errors: errors.length,
      weights: WEIGHTS,
      results,
      errors
    }, null, 2)
  );
  
  console.log('\n' + '='.repeat(60));
  console.log('✅ Process Complete!');
  console.log('='.repeat(60));
  console.log(`Total Generated: ${results.length + errors.length}/432`);
  console.log(`Successful: ${results.length}`);
  console.log(`Errors: ${errors.length}`);
  console.log(`Results saved to: ${resultsFile}\n`);
  
  if (dryRun) {
    console.log('Preview metadata at: ./generated-metadata/');
    console.log('\nTo upload to Pinata, run without --dry-run flag');
  } else {
    console.log('📋 Next Steps:');
    console.log('  1. Update BRICK_METADATA_URLS in server.js with new IPFS hashes');
    console.log('  2. Deploy updated backend to Heroku');
    console.log('  3. Mint NFTs with new x402-enabled metadata!');
  }
}

// Run if called directly
if (require.main === module) {
  main().catch(error => {
    console.error('❌ Fatal error:', error);
    process.exit(1);
  });
}

module.exports = { generateMetadata, uploadToPinata, getBrickInfo, WEIGHTS };

