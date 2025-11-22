export type Geolocation = {
  latitude: number;
  longitude: number;
};

export type Rwa = {
  tokenId: string;
  title: string;
  assetDescription: string;
  proofOfOwnershipDocument: string;
  uniqueIdentifier: string;
  royalty: number;
  price: number;
  network: string;
  image: string;
  ownerContact: string;
  assetType: string;
  insuranceStatus: string;
  geolocation: Geolocation;
  valuationDate: string;
  propertyType: string;
  area: number;
  constructionYear: number;
  metadata: string;
  mintAccount: string;
  transactionHash: string;
  version: number;
  createdAt: string;
  updatedAt: string | null;
  ownerEmail: string;
  ownerUsername: string;
};

export type RwaWithPagination = Pick<
  Rwa,
  | "tokenId"
  | "title"
  | "price"
  | "assetType"
  | "insuranceStatus"
  | "geolocation"
  | "image"
  | "version"
  | "createdAt"
  | "updatedAt"
>;

export type RwasWithPagination = {
  totalPages: number;
  totalRecords: number;
  pageSize: number;
  pageNumber: number;
  data: RwaWithPagination[];
};

export interface RwaChanges extends Rwa {
  oldPrice: number;
  newPrice: number;
  changedAt: string;
}

export type CombinedRwa = RwaChanges & Rwa;

export type RwaFiltersParams = {
  assetType?: string | null;
  priceMin?: number | null;
  priceMax?: number | null;
  sortBy?: string | null;
  sortOrder?: string | null;
  pageSize: number;
  pageNumber: number;
};

export type RwasReq = {
  assetType?: string | null;
  priceMin?: number | null;
  priceMax?: number | null;
  sortBy?: string | null;
  sortOrder?: string | null;
  pageSize: number;
  pageNumber: number;
};
