#!/usr/bin/env node

/**
 * Mint NFT with torus.gif image using MCP/OASIS API
 */

import { OASISClient } from './src/clients/oasisClient.js';

async function mintTorusNFT() {
  const client = new OASISClient();
  
  try {
    console.log('🔐 Authenticating as OASIS_ADMIN...');
    
    // Authenticate
    const authResponse = await client.authenticateAvatar('OASIS_ADMIN', 'Uppermall1!');
    
    if (authResponse.isError) {
      console.error('❌ Authentication failed:', authResponse.message);
      process.exit(1);
    }
    
    // Extract token
    const result = authResponse.result?.result || authResponse.result;
    const token = result?.jwtToken || result?.token;
    
    if (token) {
      client.setAuthToken(token);
      console.log('✅ Authentication successful');
    } else {
      console.error('❌ No token found in response');
      process.exit(1);
    }
    
    console.log('\n🎨 Minting NFT with torus.gif...');
    
    // Mint NFT - the tool will auto-upload the local image to Pinata
    const mintResponse = await client.mintNFT({
      JSONMetaDataURL: 'https://jsonplaceholder.typicode.com/posts/1', // Placeholder metadata URL
      Symbol: 'TORUS',
      Title: 'OASIS Torus - STAR OMNIVERSE',
      Description: 'A 3D wireframe torus representing the OASIS API architecture and STAR OMNIVERSE, featuring Web1-Web5 integration and COSMIC ORM.',
      ImageUrl: '/Users/maxgershfield/OASIS_CLEAN/NFT_Content/torus.gif', // Local file - will auto-upload to Pinata
      NumberToMint: 1,
      OnChainProvider: 'SolanaOASIS',
      OffChainProvider: 'MongoDBOASIS',
      NFTOffChainMetaType: 'OASIS',
      NFTStandardType: 'SPL',
      Price: 0,
      MetaData: {
        category: 'image',
        attributes: [
          { trait_type: 'Type', value: 'Architecture' },
          { trait_type: 'Theme', value: 'OASIS API' },
          { trait_type: 'Format', value: 'Animated GIF' },
          { trait_type: 'Collection', value: 'STAR OMNIVERSE' }
        ],
        external_url: 'https://www.oasisweb4.com'
      }
    });
    
    if (mintResponse.isError) {
      console.error('❌ NFT minting failed:', mintResponse.message);
      console.error('Details:', JSON.stringify(mintResponse, null, 2));
      process.exit(1);
    }
    
    console.log('\n✅ NFT minted successfully!');
    console.log('Result:', JSON.stringify(mintResponse.result, null, 2));
    
    if (mintResponse.result?.transactionHash) {
      console.log(`\n🔗 Transaction: ${mintResponse.result.transactionHash}`);
    }
    if (mintResponse.result?.mintAddress) {
      console.log(`📍 Mint Address: ${mintResponse.result.mintAddress}`);
    }
    
  } catch (error: any) {
    console.error('❌ Error:', error.message);
    console.error(error.stack);
    process.exit(1);
  }
}

mintTorusNFT();
