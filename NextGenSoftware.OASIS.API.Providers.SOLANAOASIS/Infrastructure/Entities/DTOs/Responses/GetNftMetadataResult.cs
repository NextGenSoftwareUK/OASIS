using System;
using System.Collections.Generic;
using System.Linq;
using Solnet.Metaplex;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Entities.DTOs.Responses
{
    public sealed class GetNftMetadataResult
    {
        public string Owner { get; set; }
        public string UpdateAuthority { get; set; }
        public string Mint { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Url { get; set; }
        public uint SellerFeeBasisPoints { get; set; }
        public IEnumerable<NftCreatorMedataResult> Creators { get; set; }

        public GetNftMetadataResult(MetadataAccount metadataAccount)
        {
            if(metadataAccount == null)
                throw new ArgumentNullException();
            Owner = metadataAccount.owner.Key;
            UpdateAuthority = metadataAccount.updateAuthority.Key;
            Mint = metadataAccount.mint;
            Name = metadataAccount.data.name;
            Symbol = metadataAccount.data.symbol;
            Url = metadataAccount.data.uri;
            SellerFeeBasisPoints = metadataAccount.data.sellerFeeBasisPoints;
            Creators = metadataAccount.data.creators.Select(x => new NftCreatorMedataResult(x));
        }
    }
}