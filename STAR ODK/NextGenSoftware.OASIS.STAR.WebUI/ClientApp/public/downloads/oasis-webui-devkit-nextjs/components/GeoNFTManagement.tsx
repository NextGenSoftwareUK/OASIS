import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function GeoNFTManagement() {
  const { client } = useOASIS();
  const [geoNFTs, setGeoNFTs] = useState([]);

  useEffect(() => {
    loadGeoNFTs();
  }, []);

  async function loadGeoNFTs() {
    const response = await client.get('/api/geonft/nearby');
    setGeoNFTs(response.data);
  }

  return (
    <div className="geonft-management">
      <div className="map"></div>
      <div className="sidebar">
        <h3>GeoNFTs Nearby</h3>
        <button>Create GeoNFT</button>
        {geoNFTs.map((geonft: any) => (
          <div key={geonft.id} className="geonft">
            <div>📍</div>
            <div><strong>{geonft.name}</strong><br/><small>{geonft.distance}m away</small></div>
          </div>
        ))}
      </div>
      <style jsx>{`
        .geonft-management { display: flex; height: 600px; }
        .map { flex: 1; background: #e0e0e0; }
        .sidebar { width: 300px; padding: 20px; background: white; overflow-y: auto; }
        button { width: 100%; padding: 12px; background: #4A90E2; color: white; border: none; border-radius: 6px; cursor: pointer; margin-bottom: 20px; }
        .geonft { display: flex; gap: 12px; padding: 12px; border-bottom: 1px solid #eee; cursor: pointer; }
      `}</style>
    </div>
  );
}



