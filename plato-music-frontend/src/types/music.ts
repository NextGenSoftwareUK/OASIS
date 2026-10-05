export interface MusicTrack {
  id: string;
  title: string;
  artist: string;
  album?: string;
  isrc: string;
  genre: string;
  duration: number;
  bpm?: number;
  key?: string;
  mood?: string[];
  releaseDate: Date;
}

export interface RightsHolder {
  id: string;
  name: string;
  role: 'writer' | 'producer' | 'publisher' | 'label' | 'artist';
  walletAddress: string;
  royaltyPercentage: number;
  isVerified: boolean;
}

export interface RoyaltySplit {
  trackId: string;
  rightsHolders: RightsHolder[];
  totalPercentage: number; // Must equal 100%
  splitType: 'equal' | 'custom' | 'template';
}

export interface LicenseType {
  id: string;
  name: string;
  description: string;
  pricingModel: 'fixed' | 'per-use' | 'tiered' | 'subscription';
  basePrice: number;
  currency: string;
  terms: string;
  allowedUse: string[];
}

export interface RoyaltySplitTemplate {
  id: string;
  name: string;
  description: string;
  splits: {
    role: string;
    percentage: number;
    walletAddress?: string;
  }[];
  isCustom: boolean;
  usageCount: number;
  createdAt: Date;
}

export interface MicroLicenseTemplate {
  id: string;
  name: string;
  platform: string;
  basePrice: number;
  maxUsesPerDay: number;
  requiresApproval: boolean;
  terms: string;
  isCustom: boolean;
  usageCount: number;
  createdAt: Date;
}

// Pre-defined template configurations
export const ROYALTY_SPLIT_TEMPLATES: RoyaltySplitTemplate[] = [
  {
    id: 'standard-writer-producer',
    name: 'Standard Writer/Producer',
    description: 'Classic 50/50 split between writer and producer',
    splits: [
      { role: 'writer', percentage: 50 },
      { role: 'producer', percentage: 50 }
    ],
    isCustom: false,
    usageCount: 0,
    createdAt: new Date()
  },
  {
    id: 'three-way-split',
    name: 'Three-Way Split',
    description: 'Equal split between three parties (40/30/30)',
    splits: [
      { role: 'lead-writer', percentage: 40 },
      { role: 'co-writer', percentage: 30 },
      { role: 'producer', percentage: 30 }
    ],
    isCustom: false,
    usageCount: 0,
    createdAt: new Date()
  },
  {
    id: 'label-artist-producer',
    name: 'Label/Artist/Producer',
    description: 'Label takes 50%, artist and producer split remaining 50%',
    splits: [
      { role: 'label', percentage: 50 },
      { role: 'artist', percentage: 25 },
      { role: 'producer', percentage: 25 }
    ],
    isCustom: false,
    usageCount: 0,
    createdAt: new Date()
  }
];

export const MICRO_LICENSE_TEMPLATES: MicroLicenseTemplate[] = [
  {
    id: 'tiktok-standard',
    name: 'TikTok Standard',
    platform: 'tiktok',
    basePrice: 0.50, // $0.50 per use
    maxUsesPerDay: 1000,
    requiresApproval: false,
    terms: 'Standard TikTok usage license for 15-60 second videos',
    isCustom: false,
    usageCount: 0,
    createdAt: new Date()
  },
  {
    id: 'instagram-standard',
    name: 'Instagram Standard',
    platform: 'instagram',
    basePrice: 0.75, // $0.75 per use
    maxUsesPerDay: 500,
    requiresApproval: false,
    terms: 'Standard Instagram usage license for Stories and Reels',
    isCustom: false,
    usageCount: 0,
    createdAt: new Date()
  },
  {
    id: 'youtube-standard',
    name: 'YouTube Standard',
    platform: 'youtube',
    basePrice: 2.00, // $2.00 per use
    maxUsesPerDay: 100,
    requiresApproval: true,
    terms: 'YouTube usage license for content creators (requires approval)',
    isCustom: false,
    usageCount: 0,
    createdAt: new Date()
  }
];



