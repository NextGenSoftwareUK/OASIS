import React, { useState } from 'react';
import { useQuery, useMutation } from 'react-query';
import { OASISClient } from '@oasis/api-client';

export interface NFTManagementProps {
  avatarId: string;
  enableMinting?: boolean;
  enableTransfer?: boolean;
  theme?: 'light' | 'dark';
  customStyles?: React.CSSProperties;
}

export const NFTManagement: React.FC<NFTManagementProps> = ({
  avatarId,
  enableMinting = true,
  enableTransfer = true,
  theme = 'dark',
  customStyles = {}
}) => {
  const [selectedNFT, setSelectedNFT] = useState<any>(null);
  const client = new OASISClient();

  const { data: nfts, isLoading, refetch } = useQuery(
    ['nfts', avatarId],
    () => client.getAvatarNFTs(avatarId)
  );

  const mintMutation = useMutation(
    (nftData: any) => client.mintNFT(nftData),
    {
      onSuccess: () => {
        refetch();
      }
    }
  );

  const transferMutation = useMutation(
    ({ nftId, toAddress }: any) => client.transferNFT(nftId, toAddress),
    {
      onSuccess: () => {
        refetch();
        setSelectedNFT(null);
      }
    }
  );

  return (
    <div className={`oasis-nft oasis-nft--${theme}`} style={customStyles}>
      <div className="oasis-nft__header">
        <h3>NFT Collection</h3>
        {enableMinting && (
          <button 
            className="oasis-nft__mint-btn"
            onClick={() => {/* Open mint dialog */}}
          >
            Mint NFT
          </button>
        )}
      </div>

      <div className="oasis-nft__grid">
        {isLoading ? (
          <div className="oasis-nft__loading">Loading NFTs...</div>
        ) : (
          nfts?.map((nft: any) => (
            <div 
              key={nft.id} 
              className="oasis-nft__item"
              onClick={() => setSelectedNFT(nft)}
            >
              <img src={nft.imageUrl} alt={nft.name} />
              <div className="nft-info">
                <h4>{nft.name}</h4>
                <p className="nft-id">#{nft.tokenId}</p>
                <p className="nft-price">{nft.price} OASIS</p>
              </div>
            </div>
          ))
        )}
      </div>

      {selectedNFT && (
        <div className="oasis-nft__details">
          <h3>{selectedNFT.name}</h3>
          <img src={selectedNFT.imageUrl} alt={selectedNFT.name} />
          <p>{selectedNFT.description}</p>
          
          <div className="nft-metadata">
            <div><strong>Token ID:</strong> {selectedNFT.tokenId}</div>
            <div><strong>Owner:</strong> {selectedNFT.owner}</div>
            <div><strong>Blockchain:</strong> {selectedNFT.blockchain}</div>
          </div>

          {enableTransfer && (
            <div className="nft-actions">
              <input 
                type="text" 
                placeholder="Recipient address"
                id="transfer-address"
              />
              <button onClick={() => {
                const address = (document.getElementById('transfer-address') as HTMLInputElement)?.value;
                if (address) {
                  transferMutation.mutate({ nftId: selectedNFT.id, toAddress: address });
                }
              }}>
                Transfer NFT
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default NFTManagement;

