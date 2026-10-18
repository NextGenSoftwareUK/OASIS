using System.IO;
using Solnet.Wallet;

namespace NextGenSoftware.OASIS.API.Providers.SOLANAOASIS.Infrastructure.Services.Solana;

// Borsh MetadataV1 prefix used by official BurnNft; never fetches the embedded URI.
internal static class MetaplexBurnMetadata
{
    internal static PublicKey ReadVerifiedCollection(byte[] bytes,PublicKey expectedMint)
    {
        using var stream=new MemoryStream(bytes,false);
        using var reader=new BinaryReader(stream);
        byte Option()
        {
            var tag=reader.ReadByte();
            if(tag>1) throw new InvalidDataException("Invalid Metaplex Borsh option/boolean.");
            return tag;
        }
        byte[] Exact(int count)
        {
            var value=reader.ReadBytes(count);
            if(value.Length!=count) throw new EndOfStreamException("Truncated Metaplex metadata.");
            return value;
        }
        void Text(int maximum)
        {
            var length=reader.ReadUInt32();
            if(length>maximum) throw new InvalidDataException("Metaplex metadata string exceeds protocol limit.");
            _=new System.Text.UTF8Encoding(false,true).GetString(Exact((int)length));
        }
        if(reader.ReadByte()!=4) throw new InvalidDataException("Expected Metaplex MetadataV1 account.");
        Exact(32); // update authority
        if(new PublicKey(Exact(32)).Key!=expectedMint.Key) throw new InvalidDataException("NFT metadata mint does not match requested NFT.");
        Text(32);Text(10);Text(200);reader.ReadUInt16();
        if(Option()==1)
        {
            var creators=reader.ReadUInt32();
            if(creators>5) throw new InvalidDataException("Invalid Metaplex creator count.");
            for(var index=0;index<creators;index++){Exact(32);Option();reader.ReadByte();}
        }
        Option();Option(); // primary sale / mutable
        if(stream.Position==stream.Length) return null; // defined legacy trailing fields absent
        if(Option()==1) reader.ReadByte(); // edition nonce
        if(stream.Position==stream.Length) return null;
        if(Option()==1 && reader.ReadByte()!=0)
            throw new NotSupportedException("This Metaplex BurnNft path requires a legacy nonfungible master edition; other token standards need their native burn instruction.");
        if(stream.Position==stream.Length || Option()==0) return null;
        var verified=Option()==1;var collection=new PublicKey(Exact(32));
        return verified ? collection:null;
    }
}
