using HotChocolate.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types
{
    /// <summary>Represents an OASIS NFT.</summary>
    public class NftDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string MintedByAvatarId { get; set; } = string.Empty;
    }

    public class NftType : ObjectType<NftDto>
    {
        protected override void Configure(IObjectTypeDescriptor<NftDto> descriptor)
        {
            descriptor.Description("An OASIS NFT minted via the STAR ODK.");
            descriptor.Field(f => f.Id).Description("Unique identifier of the NFT.");
            descriptor.Field(f => f.Name).Description("Display name of the NFT.");
            descriptor.Field(f => f.Description).Description("Description of the NFT.");
            descriptor.Field(f => f.MintedByAvatarId).Description("Avatar ID of the minter.");
        }
    }
}
