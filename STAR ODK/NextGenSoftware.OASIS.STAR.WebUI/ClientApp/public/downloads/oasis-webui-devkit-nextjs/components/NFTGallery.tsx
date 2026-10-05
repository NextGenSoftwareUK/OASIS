import { useState, useEffect } from 'react';
import { useOASIS } from '../hooks/useOASIS';

export default function NFTGallery() {
  const { client } = useOASIS();
  const [nfts, setNfts] = useState([]);

  useEffect(() => {
    loadNFTs();
  }, []);

  async function loadNFTs() {
    const response = await client.get('/api/nfts');
    setNfts(response.data);
  }

  return (
    <div className="nft-gallery">
      <h2>My NFTs</h2>
      <div className="grid">
        {nfts.map((nft: any) => (
          <div key={nft.id} className="nft-card">
            <div className="nft-image"></div>
            <div className="nft-info">
              <h3>{nft.name}</h3>
              <span className="chain-badge">{nft.chain}</span>
            </div>
          </div>
        ))}
      </div>
      <style jsx>{`
        .grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(250px, 1fr)); gap: 20px; }
        .nft-card { background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
        .nft-image { aspect-ratio: 1; background: #f5f5f5; }
        .nft-info { padding: 16px; }
        .chain-badge { background: #e3f2fd; color: #1976d2; padding: 4px 8px; border-radius: 4px; font-size: 12px; }
      `}</style>
    </div>
  );
}



