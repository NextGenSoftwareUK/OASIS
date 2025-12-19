/**
 * Seed OASIS with sample holon and OAPP data for visualization
 * Run this script to populate OASIS with test data
 */

import { OASISClient } from '../api/OASISClient.js';

// Note: This will use the default credentials, but you can pass a different client
// if you want to seed data for a specific avatar
export function createSeedClient(config = {}) {
    return new OASISClient({
        baseUrl: config.baseUrl || import.meta.env.VITE_OASIS_API_URL || 'http://localhost:5003',
        username: config.username || import.meta.env.VITE_OASIS_USERNAME || 'OASIS_ADMIN',
        password: config.password || import.meta.env.VITE_OASIS_PASSWORD || 'Uppermall1!'
    });
}

const oasisClient = createSeedClient();

/**
 * Create a sample holon
 * OASIS API expects capitalized properties: Name, Description, HolonType, MetaData
 */
function createHolon(name, holonType, oappId = null, avatarId = null, metadata = {}) {
    // holonType is now an integer enum value, use metadata.holonTypeName for description if available
    const typeName = metadata.holonTypeName || (holonType === 74 ? 'OAPP' : 'Holon');
    return {
        Name: name,
        Description: `Sample ${typeName} holon: ${name}`,
        HolonType: holonType,  // Integer enum value (74 for OAPP, 0 for default, etc.)
        ProviderKey: 'MongoOASIS', // Use MongoDB provider like the example
        MetaData: {
            ...metadata,
            createdBy: 'holonic-visualizer-seed-script',
            createdDate: new Date().toISOString(),
            ...(oappId && { oappId: oappId }),
            ...(avatarId && { createdByAvatarId: avatarId })
        }
        // Removed ProviderMetaData - it was causing validation errors
        // The API expects ProviderMetaData.collection to be a ProviderType enum, not a string
    };
}

/**
 * Create sample OAPP data structure
 * Note: OAPPs are created as holons with HolonType = 74 (OAPP enum value)
 */
function createOAPP(name, description, holonCount) {
    return {
        Name: name,
        Description: description,
        HolonType: 74,  // OAPP enum value (not string!)
        ProviderKey: 'MongoOASIS',
        MetaData: {
            oappType: 'application',
            expectedHolonCount: holonCount,
            createdBy: 'holonic-visualizer-seed-script',
            createdDate: new Date().toISOString()
        }
        // Removed ProviderMetaData - it was causing validation errors
    };
}

/**
 * Save a holon to OASIS
 */
async function saveHolon(holon, client = null) {
    const clientToUse = client || oasisClient;
    try {
        const holonName = holon.Name || 'Unknown';
        
        // Send holon using the correct endpoint: /api/data/save-holon
        // The endpoint expects a SaveHolonRequest with PascalCase properties (C# convention)
        const requestBody = {
            Holon: holon,  // Capital H - C# property name
            SaveChildren: false,  // Capital S
            Recursive: false,
            MaxChildDepth: 0,
            ContinueOnError: true,
            ShowDetailedSettings: false
        };
        
        // Log the request for debugging (first holon only to avoid spam)
        if (!saveHolon._loggedFirst) {
            console.log('Sample request body:', JSON.stringify(requestBody, null, 2));
            saveHolon._loggedFirst = true;
        }
        
        const response = await clientToUse.request('/api/data/save-holon', {
            method: 'POST',
            body: JSON.stringify(requestBody)
        });
        
        if (response.isError || response.IsError) {
            console.error(`Error saving holon ${holonName}:`, response.message || response.Message);
            return null;
        }
        
        return response.result || response.Result || response;
    } catch (error) {
        const holonName = holon.Name || 'Unknown';
        console.error(`Exception saving holon ${holonName}:`, error);
        return null;
    }
}

// Track if seeding is in progress to prevent multiple concurrent runs
let isSeedingInProgress = false;

/**
 * Seed OASIS with sample data
 */
export async function seedOASISData(client = null) {
    // Prevent multiple seeding processes from running simultaneously
    if (isSeedingInProgress) {
        console.warn('⚠️ Seeding already in progress. Please wait for the current seeding to complete.');
        return;
    }
    
    isSeedingInProgress = true;
    const clientToUse = client || oasisClient;
    console.log('🌱 Starting OASIS data seeding...');
    
    try {
        // Authenticate first
        await clientToUse.authenticate();
        console.log('✅ Authenticated with OASIS API');
        
        // Get current avatar to associate holons with it
        let currentAvatarId = null;
        try {
            const avatar = await clientToUse.getCurrentAvatar();
            if (avatar && (avatar.id || avatar.Id)) {
                currentAvatarId = avatar.id || avatar.Id;
                console.log(`✅ Using avatar: ${avatar.username || avatar.Username || currentAvatarId.substring(0, 8)}...`);
            }
        } catch (e) {
            console.warn('Could not get current avatar, holons will not be associated with an avatar:', e);
        }
        
        // Create OAPPs (as holons with type OAPP)
        // Using smaller counts for faster seeding - you can increase these
        const oapps = [
            createOAPP('Star Navigation System', 'A major navigation application', 300),
            createOAPP('Planet Explorer', 'Medium exploration tool', 150),
            createOAPP('Moon Base Manager', 'Small base management app', 50),
            createOAPP('Galaxy Tracker', 'Large-scale tracking system', 500),
            createOAPP('Solar System Simulator', 'Medium simulation app', 200)
        ];
        
        const createdOAPPs = [];
        for (const oapp of oapps) {
            console.log(`Creating OAPP: ${oapp.Name}...`);
            const created = await saveHolon(oapp, clientToUse);
            if (created) {
                // Store both the original oapp (for Name) and the created response (for ID)
                createdOAPPs.push({
                    original: oapp,  // Keep original for Name and other properties
                    created: created  // API response with ID
                });
                const createdId = created.id || created.Id || created.result?.id || created.result?.Id || 'unknown';
                console.log(`✅ Created OAPP: ${oapp.Name} (ID: ${createdId})`);
            }
        }
        
        // Create holons for each OAPP
        // Use enum integer values for HolonType (not strings!)
        // For generic holons, use 0 (Default) or a small positive integer
        // The API expects HolonType to be an enum integer, not a string
        const holonTypeNames = ['Mission', 'NFT', 'Wallet', 'DeFi', 'Identity', 'Quest', 'Achievement', 'Item'];
        // Use 0 for generic holons (Default enum value) - the API will accept this
        const defaultHolonType = 0;
        
        for (const oappData of createdOAPPs) {
            const oapp = oappData.original;  // Use original for Name
            const created = oappData.created;  // Use created response for ID
            const oappId = created.id || created.Id || created.result?.id || created.result?.Id;
            const holonCount = oapp.MetaData?.expectedHolonCount || oapp.metadata?.expectedHolonCount || 500;
            const oappName = oapp.Name || oapp.name || 'Unknown OAPP';
            
            console.log(`\nCreating ${holonCount} holons for OAPP: ${oappName}...`);
            
            // Create holons in batches to avoid overwhelming the API
            const batchSize = 50;
            let holonsCreated = 0;  // Renamed from 'created' to avoid conflict with oappData.created
            
            for (let i = 0; i < holonCount; i += batchSize) {
                const batch = [];
                for (let j = 0; j < batchSize && (i + j) < holonCount; j++) {
                    const holonTypeName = holonTypeNames[Math.floor(Math.random() * holonTypeNames.length)];
                    const holon = createHolon(
                        `${holonTypeName} ${i + j + 1}`,
                        defaultHolonType,  // Use enum integer, not string
                        oappId,
                        currentAvatarId,
                        {
                            index: i + j,
                            batch: Math.floor(i / batchSize),
                            parentOAPP: oappName,
                            holonTypeName: holonTypeName  // Store the name in metadata for reference
                        }
                    );
                    batch.push(holon);
                }
                
                // Save batch in parallel
                const results = await Promise.all(batch.map(h => saveHolon(h, clientToUse)));
                const successCount = results.filter(r => r !== null).length;
                holonsCreated += successCount;
                
                console.log(`  Batch ${Math.floor(i / batchSize) + 1}: Created ${successCount}/${batch.length} holons (Total: ${holonsCreated}/${holonCount})`);
                
                // Small delay to avoid rate limiting
                await new Promise(resolve => setTimeout(resolve, 100));
            }
            
            console.log(`✅ Completed OAPP ${oappName}: ${holonsCreated} holons created`);
        }
        
        // Create some unassigned holons
        console.log('\nCreating unassigned holons...');
        const unassignedCount = 50; // Reduced for faster seeding
        for (let i = 0; i < unassignedCount; i++) {
            const holonTypeName = holonTypeNames[Math.floor(Math.random() * holonTypeNames.length)];
            const holon = createHolon(
                `Unassigned ${holonTypeName} ${i + 1}`,
                defaultHolonType,  // Use enum integer, not string
                null,
                currentAvatarId,
                { 
                    isUnassigned: true,
                    holonTypeName: holonTypeName  // Store the name in metadata for reference
                }
            );
            await saveHolon(holon, clientToUse);
            
            if ((i + 1) % 50 === 0) {
                console.log(`  Created ${i + 1}/${unassignedCount} unassigned holons...`);
            }
        }
        
        console.log('\n✅ OASIS data seeding completed!');
        console.log(`   Created ${createdOAPPs.length} OAPPs`);
        console.log(`   Created holons across all OAPPs`);
        console.log(`   Created ${unassignedCount} unassigned holons`);
        
        isSeedingInProgress = false;  // Reset flag on success
        return {
            success: true,
            oappsCreated: createdOAPPs.length,
            unassignedHolons: unassignedCount
        };
        
    } catch (error) {
        console.error('❌ Error seeding OASIS data:', error);
        isSeedingInProgress = false;  // Reset flag on error
        return {
            success: false,
            error: error.message
        };
    }
}

// Note: This file is designed to run in the browser via import
// The seedOASISData() function is called from main.js
// If you need to run this as a Node.js script, use a separate script file


