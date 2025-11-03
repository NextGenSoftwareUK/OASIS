import React, { useState } from 'react';
import { useQuery } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface NFTGalleryProps {
  avatarId?: string;
  collections?: string[];
  columns?: number;
  theme?: 'light' | 'dark';
  onSelect?: (nft: any) => void;
  enableFilters?: boolean;
  sortBy?: 'date' | 'price' | 'name';
  customStyles?: React.CSSProperties;
}

export const NFTGallery: React.FC<NFTGalleryProps> = ({
  avatarId,
  collections = [],
  columns = 3,
  theme = 'dark',
  onSelect,
  enableFilters = true,
  sortBy = 'date',
  customStyles = {}
}) => {
  const [filter, setFilter] = useState('all');
  const [sort, setSort] = useState(sortBy);
  const client = new OASISClient();

  const { data: nfts, isLoading } = useQuery(
    ['nfts', avatarId, collections],
    () => client.getNFTs(avatarId, collections)
  );

  const filteredNFTs = nfts?.filter((nft: any) => {
    if (filter === 'all') return true;
    return nft.collection === filter;
  }).sort((a: any, b: any) => {
    if (sort === 'price') return b.price - a.price;
    if (sort === 'name') return a.name.localeCompare(b.name);
    return new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime();
  });

  return (
    <div className={`oasis-nft-gallery oasis-nft-gallery--${theme}`} style={customStyles}>
      {enableFilters && (
        <div className="gallery-controls">
          <select value={filter} onChange={(e) => setFilter(e.target.value)}>
            <option value="all">All Collections</option>
            {collections.map(col => (
              <option key={col} value={col}>{col}</option>
            ))}
          </select>
          <select value={sort} onChange={(e) => setSort(e.target.value)}>
            <option value="date">Sort by Date</option>
            <option value="price">Sort by Price</option>
            <option value="name">Sort by Name</option>
          </select>
        </div>
      )}

      <div 
        className="nft-grid"
        style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}
      >
        {isLoading ? (
          <div className="loading">Loading NFTs...</div>
        ) : (
          filteredNFTs?.map((nft: any) => (
            <div 
              key={nft.id} 
              className="nft-item"
              onClick={() => onSelect?.(nft)}
            >
              <img src={nft.imageUrl} alt={nft.name} />
              <div className="nft-info">
                <h4>{nft.name}</h4>
                <p className="collection">{nft.collection}</p>
                <p className="price">{nft.price} OASIS</p>
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
};

export default NFTGallery;

