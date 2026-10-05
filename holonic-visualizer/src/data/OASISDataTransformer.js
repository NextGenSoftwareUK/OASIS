/**
 * OASIS Data Transformer
 * Transforms OASIS API data structures to visualizer format
 */
export class OASISDataTransformer {
    /**
     * Transform a single holon from API format to visualizer format
     * @param {object} holon - Raw holon from API
     * @returns {object} Transformed holon
     */
    static transformHolon(holon) {
        const holonType = holon.holonType || holon.HolonType || holon.type || 'Unknown';
        const isAvatar = holonType === 'Avatar' || holonType === 3 || holonType === '3' ||
                        (typeof holonType === 'string' && holonType.toLowerCase() === 'avatar');
        const name = holon.name || holon.Name || holon.title || 
                    (isAvatar ? (holon.username || holon.Username || holon.email || holon.Email || holon.fullName || holon.FullName || 'Unnamed Avatar') : 'Unnamed Holon');
        
        return {
            id: holon.id || holon.Id || holon._id || holon.avatarId || holon.AvatarId || null,
            name: name,
            holonType: holonType,
            oappId: holon.oappId || holon.OAPPId || holon.oapp_id || 
                   holon.metadata?.oappId || holon.MetaData?.oappId || 
                   holon.metaData?.oappId || null,
            parentHolonId: holon.parentHolonId || holon.ParentHolonId || 
                          holon.parent_id || holon.metadata?.parentHolonId || null,
            metadata: holon.metadata || holon.MetaData || holon.metaData || {},
            providerKeys: holon.providerKeys || holon.ProviderKeys || {},
            createdDate: holon.createdDate || holon.CreatedDate || holon.created_date || null,
            modifiedDate: holon.modifiedDate || holon.ModifiedDate || holon.modified_date || null,
            // Avatar-specific fields
            ...(isAvatar && {
                username: holon.username || holon.Username,
                email: holon.email || holon.Email,
                firstName: holon.firstName || holon.FirstName,
                lastName: holon.lastName || holon.LastName,
                fullName: holon.fullName || holon.FullName || (holon.firstName || holon.FirstName) + ' ' + (holon.lastName || holon.LastName)
            })
        };
    }

    /**
     * Transform a single OAPP from API format to visualizer format
     * @param {object} oapp - Raw OAPP from API
     * @returns {object} Transformed OAPP
     */
    static transformOAPP(oapp) {
        return {
            id: oapp.id || oapp.Id || oapp._id || null,
            name: oapp.name || oapp.Name || oapp.title || 'Unnamed OAPP',
            description: oapp.description || oapp.Description || '',
            celestialType: oapp.celestialType || oapp.CelestialType || 
                          oapp.metadata?.celestialType || oapp.MetaData?.celestialType || null,
            metadata: {
                ...(oapp.metadata || oapp.MetaData || oapp.metaData || {}),
                version: oapp.version || oapp.Version || oapp.metadata?.version || '1.0.0'
            }
        };
    }

    /**
     * Transform OASIS data to visualizer format
     * Groups holons by OAPP and determines celestial types
     * @param {Array} oapps - Array of OAPP objects
     * @param {Array} holons - Array of holon objects
     * @param {boolean} includeAvatars - Whether to include avatars as separate celestial bodies
     * @returns {object} Visualizer data format
     */
    static transformToVisualizerFormat(oapps, holons, includeAvatars = true) {
        // Separate avatars from other holons
        // Avatars have HolonType = "Avatar" (string) or 3 (enum value: Default=0, All=1, Player=2, Avatar=3)
        const avatarHolons = includeAvatars ? holons.filter(h => {
            const type = h.holonType || h.HolonType || h.type;
            // Check for string "Avatar" or enum value 3 (Avatar is 3rd in enum after Default=0, All=1, Player=2)
            return type === 'Avatar' || type === 3 || type === '3' || 
                   (typeof type === 'string' && type.toLowerCase() === 'avatar');
        }) : [];
        
        const nonAvatarHolons = holons.filter(h => {
            const type = h.holonType || h.HolonType || h.type;
            return type !== 'Avatar' && type !== 3 && type !== '3' && 
                   !(typeof type === 'string' && type.toLowerCase() === 'avatar');
        });
        
        // Transform all OAPPs and holons
        const transformedOAPPs = oapps.map(oapp => this.transformOAPP(oapp));
        const transformedHolons = nonAvatarHolons.map(holon => this.transformHolon(holon));
        
        // Transform avatars and create virtual OAPPs for them
        const transformedAvatars = avatarHolons.map(avatar => this.transformHolon(avatar));
        
        // Create a virtual OAPP for each avatar (so they appear as celestial bodies)
        const avatarOAPPs = transformedAvatars.map(avatar => ({
            id: `avatar-${avatar.id}`,
            name: avatar.name || avatar.username || 'Unnamed Avatar',
            description: `Avatar: ${avatar.username || avatar.email || avatar.id}`,
            celestialType: 'moon', // Avatars are small, like moons
            metadata: {
                isAvatar: true,
                avatarId: avatar.id,
                holonCount: 1, // Each avatar is its own holon
                username: avatar.username,
                email: avatar.email
            }
        }));
        
        // Add avatar holons to the holons list, linked to their virtual OAPP
        transformedAvatars.forEach((avatar, index) => {
            avatar.oappId = avatarOAPPs[index].id;
            transformedHolons.push(avatar);
        });
        
        // Combine OAPPs with avatar OAPPs
        const allOAPPs = [...transformedOAPPs, ...avatarOAPPs];

        // Group holons by OAPP
        const holonsByOAPP = new Map();
        const unassignedHolons = [];

        transformedHolons.forEach(holon => {
            const oappId = holon.oappId;
            if (oappId) {
                if (!holonsByOAPP.has(oappId)) {
                    holonsByOAPP.set(oappId, []);
                }
                holonsByOAPP.get(oappId).push(holon);
            } else {
                unassignedHolons.push(holon);
            }
        });

        // Update OAPP metadata with holon counts and determine celestial types
        allOAPPs.forEach(oapp => {
            const holonCount = holonsByOAPP.get(oapp.id)?.length || 0;
            oapp.metadata.holonCount = holonCount;
            
            // Avatars are always moons, don't override
            if (oapp.metadata.isAvatar) {
                return;
            }
            
            // Determine celestial type based on holon count if not already set
            if (!oapp.celestialType) {
                if (holonCount >= 3000) {
                    oapp.celestialType = 'star';
                } else if (holonCount >= 800) {
                    oapp.celestialType = 'planet';
                } else if (holonCount > 0) {
                    oapp.celestialType = 'moon';
                } else {
                    oapp.celestialType = 'moon'; // Default for empty OAPPs
                }
            }
        });

        // Create a virtual OAPP for unassigned holons if there are any
        if (unassignedHolons.length > 0) {
            const unassignedOAPP = {
                id: 'unassigned',
                name: 'Unassigned Holons',
                description: 'Holons not assigned to any OAPP',
                celestialType: unassignedHolons.length >= 3000 ? 'star' : 
                              unassignedHolons.length >= 800 ? 'planet' : 'moon',
                metadata: {
                    holonCount: unassignedHolons.length,
                    isUnassigned: true
                }
            };
            transformedOAPPs.push(unassignedOAPP);
            holonsByOAPP.set('unassigned', unassignedHolons);
        }

        return {
            oapps: allOAPPs,
            holons: transformedHolons
        };
    }

    /**
     * Filter and limit data for performance
     * @param {object} data - Visualizer data
     * @param {number} maxHolons - Maximum holons to include
     * @returns {object} Filtered data
     */
    static limitData(data, maxHolons = 50000) {
        if (data.holons.length <= maxHolons) {
            return data;
        }

        // Sample holons proportionally from each OAPP
        const holonsByOAPP = new Map();
        data.holons.forEach(holon => {
            const oappId = holon.oappId || 'unassigned';
            if (!holonsByOAPP.has(oappId)) {
                holonsByOAPP.set(oappId, []);
            }
            holonsByOAPP.get(oappId).push(holon);
        });

        const totalHolons = data.holons.length;
        const sampleRatio = maxHolons / totalHolons;
        const sampledHolons = [];

        holonsByOAPP.forEach((holons, oappId) => {
            const sampleSize = Math.max(1, Math.floor(holons.length * sampleRatio));
            const shuffled = [...holons].sort(() => 0.5 - Math.random());
            sampledHolons.push(...shuffled.slice(0, sampleSize));
        });

        return {
            oapps: data.oapps,
            holons: sampledHolons
        };
    }
}


