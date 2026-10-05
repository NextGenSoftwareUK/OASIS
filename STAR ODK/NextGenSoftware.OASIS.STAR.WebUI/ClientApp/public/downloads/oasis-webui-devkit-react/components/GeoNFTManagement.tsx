import React, { useState } from 'react';
import { useMutation, useQueryClient } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface GeoNFTManagementProps {
  avatarId: string;
  location?: { lat: number; lng: number };
  theme?: 'light' | 'dark';
  onMint?: (geoNFT: any) => void;
  customStyles?: React.CSSProperties;
}

export const GeoNFTManagement: React.FC<GeoNFTManagementProps> = ({
  avatarId,
  location,
  theme = 'dark',
  onMint,
  customStyles = {}
}) => {
  const [formData, setFormData] = useState({
    name: '',
    description: '',
    imageUrl: '',
    radius: 100, // meters
    metadata: {}
  });
  const queryClient = useQueryClient();
  const client = new OASISClient();

  const mintMutation = useMutation(
    (data: any) => client.mintGeoNFT(avatarId, {
      ...data,
      location
    }),
    {
      onSuccess: (geoNFT) => {
        queryClient.invalidateQueries(['geonft']);
        onMint?.(geoNFT);
        setFormData({
          name: '',
          description: '',
          imageUrl: '',
          radius: 100,
          metadata: {}
        });
      }
    }
  );

  const handleMint = () => {
    if (!location) {
      alert('Please select a location on the map');
      return;
    }
    mintMutation.mutate(formData);
  };

  return (
    <div className={`oasis-geonft-mgmt oasis-geonft-mgmt--${theme}`} style={customStyles}>
      <h3>Create Geo-NFT</h3>

      <div className="form-group">
        <label>Name</label>
        <input
          type="text"
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          placeholder="My Geo-NFT"
        />
      </div>

      <div className="form-group">
        <label>Description</label>
        <textarea
          value={formData.description}
          onChange={(e) => setFormData({ ...formData, description: e.target.value })}
          placeholder="Description of your Geo-NFT"
          rows={3}
        />
      </div>

      <div className="form-group">
        <label>Image URL</label>
        <input
          type="text"
          value={formData.imageUrl}
          onChange={(e) => setFormData({ ...formData, imageUrl: e.target.value })}
          placeholder="https://..."
        />
      </div>

      <div className="form-group">
        <label>Radius (meters)</label>
        <input
          type="number"
          value={formData.radius}
          onChange={(e) => setFormData({ ...formData, radius: parseInt(e.target.value) })}
          min="10"
          max="10000"
        />
      </div>

      {location && (
        <div className="location-info">
          <h4>Selected Location:</h4>
          <p>Latitude: {location.lat.toFixed(6)}</p>
          <p>Longitude: {location.lng.toFixed(6)}</p>
        </div>
      )}

      <button
        className="mint-btn"
        onClick={handleMint}
        disabled={mintMutation.isLoading || !formData.name || !location}
      >
        {mintMutation.isLoading ? 'Minting...' : 'Mint Geo-NFT'}
      </button>

      {mintMutation.isError && (
        <div className="error">Failed to mint Geo-NFT. Please try again.</div>
      )}

      {mintMutation.isSuccess && (
        <div className="success">Geo-NFT minted successfully!</div>
      )}
    </div>
  );
};

export default GeoNFTManagement;

