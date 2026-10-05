import React, { useEffect, useRef, useState } from 'react';
import { OASISClient } from '@oasis/api-client';

export interface GeoNFTMapProps {
  avatarId?: string;
  center?: { lat: number; lng: number };
  zoom?: number;
  theme?: 'light' | 'dark';
  enableCreation?: boolean;
  customStyles?: React.CSSProperties;
}

export const GeoNFTMap: React.FC<GeoNFTMapProps> = ({
  avatarId,
  center = { lat: 0, lng: 0 },
  zoom = 2,
  theme = 'dark',
  enableCreation = true,
  customStyles = {}
}) => {
  const mapRef = useRef<HTMLDivElement>(null);
  const [geoNFTs, setGeoNFTs] = useState<any[]>([]);
  const [selectedNFT, setSelectedNFT] = useState<any>(null);
  const client = new OASISClient();

  useEffect(() => {
    loadGeoNFTs();
  }, [avatarId]);

  const loadGeoNFTs = async () => {
    const result = await client.getGeoNFTs(avatarId);
    setGeoNFTs(result);
  };

  return (
    <div className={`oasis-geonft-map oasis-geonft-map--${theme}`} style={customStyles}>
      <div ref={mapRef} className="map-container" />
      
      <div className="map-controls">
        {enableCreation && (
          <button className="create-geonft-btn">
            📍 Create Geo-NFT
          </button>
        )}
      </div>

      {selectedNFT && (
        <div className="geonft-popup">
          <img src={selectedNFT.imageUrl} alt={selectedNFT.name} />
          <h4>{selectedNFT.name}</h4>
          <p>{selectedNFT.description}</p>
          <div className="location">
            📍 {selectedNFT.location.lat.toFixed(6)}, {selectedNFT.location.lng.toFixed(6)}
          </div>
        </div>
      )}
    </div>
  );
};

export default GeoNFTMap;

