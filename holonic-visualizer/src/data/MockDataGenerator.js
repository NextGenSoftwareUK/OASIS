export class MockDataGenerator {
    static generate(options = {}) {
        try {
            const { type, count = 1 } = options; // type: 'moon' | 'planet' | 'star' | undefined (random)
            
            console.log(`🎲 MockDataGenerator.generate called with:`, { type, count });
            
            const holons = [];
            const oapps = [];

        const holonTypes = [
            'Mission',
            'NFT',
            'Inventory',
            'CelestialBody',
            'Wallet',
            'DeFi',
            'Identity',
            'Template',
            'Library',
            'Runtime'
        ];

        // Generate OAPP names pool
        const oappNamePools = {
            moon: [
                'Lunar Module',
                'Moon Base',
                'Satellite Station',
                'Orbital Outpost',
                'Small Cluster',
                'Mini Hub',
                'Compact Node'
            ],
            planet: [
                'Planetary System',
                'World Hub',
                'Major Node',
                'Central Station',
                'Core Platform',
                'Main Network',
                'Primary System'
            ],
            star: [
                'Galactic Core',
                'Star System',
                'Major Hub',
                'Central Network',
                'Primary Cluster',
                'Main Galaxy',
                'Nexus Point'
            ]
        };

        const allNames = [
            'Galactic Explorer',
            'DeFi Universe',
            'NFT Marketplace',
            'Identity Hub',
            'Mission Control',
            'Wallet Station',
            'Celestial Network',
            'Metaverse Portal'
        ];

        // Determine what to generate
        let typesToGenerate = [];
        if (type) {
            // Generate specific type
            typesToGenerate = Array(count).fill(type);
        } else {
            // Generate random mix
            typesToGenerate = ['moon', 'planet', 'star', 'moon', 'planet', 'star', 'moon', 'planet'];
        }

        typesToGenerate.forEach((celestialType, oappIndex) => {
            // Determine holon count based on type
            // Holons are basic data points, so apps need many of them
            let holonCount;
            let namePool;
            
            if (celestialType === 'moon') {
                // Moon = Basic app: 200-800 holons (basic data points)
                holonCount = 200 + Math.floor(Math.random() * 600);
                namePool = oappNamePools.moon;
            } else if (celestialType === 'planet') {
                // Planet = Medium app: 800-3000 holons
                holonCount = 800 + Math.floor(Math.random() * 2200);
                namePool = oappNamePools.planet;
            } else if (celestialType === 'star') {
                // Star = Major app: 3000-15000+ holons
                holonCount = 3000 + Math.floor(Math.random() * 12000);
                namePool = oappNamePools.star;
            } else {
                // Fallback for random - mix of all sizes
                const rand = Math.random();
                if (rand < 0.4) {
                    holonCount = 200 + Math.floor(Math.random() * 600); // moon
                } else if (rand < 0.8) {
                    holonCount = 800 + Math.floor(Math.random() * 2200); // planet
                } else {
                    holonCount = 3000 + Math.floor(Math.random() * 12000); // star
                }
                namePool = allNames;
            }

            const oappId = `oapp-${oappIndex}`;
            const name = namePool[Math.floor(Math.random() * namePool.length)] + ` ${oappIndex + 1}`;

            const oapp = {
                id: oappId,
                name: name,
                celestialType: celestialType,
                metadata: {
                    description: `${celestialType.toUpperCase()}: ${name}`,
                    version: '1.0.0',
                    holonCount: holonCount
                }
            };

            oapps.push(oapp);

            // Generate holons for this OAPP
            for (let i = 0; i < holonCount; i++) {
                const holonType = holonTypes[Math.floor(Math.random() * holonTypes.length)];
                
                const holon = {
                    id: `holon-${oappIndex}-${i}`,
                    name: `${holonType} ${i + 1}`,
                    holonType: holonType,
                    oappId: oappId,
                    metadata: {
                        description: `${holonType} holon in ${name}`,
                        created: new Date().toISOString(),
                        version: '1.0.0'
                    },
                    providerKeys: {
                        mongodb: `mongo-${oappIndex}-${i}`,
                        ethereum: `0x${Math.random().toString(16).substr(2, 40)}`,
                        solana: `${Math.random().toString(36).substr(2, 44)}`
                    }
                };

                holons.push(holon);
            }
        });

        // Add some free-floating holons (not assigned to OAPPs) only for random generation
        if (!type) {
            for (let i = 0; i < 10; i++) {
                const holonType = holonTypes[Math.floor(Math.random() * holonTypes.length)];
                holons.push({
                    id: `holon-free-${i}`,
                    name: `Free ${holonType} ${i + 1}`,
                    holonType: holonType,
                    oappId: null,
                    metadata: {
                        description: `Unassigned ${holonType} holon`,
                        created: new Date().toISOString()
                    }
                });
            }
        }

            const result = {
                oapps: oapps,
                holons: holons
            };
            
            console.log(`✅ Generated ${oapps.length} OAPP(s) and ${holons.length} holon(s)`);
            return result;
        } catch (error) {
            console.error('❌ Error in MockDataGenerator.generate:', error);
            throw error;
        }
    }
}

