using HotChocolate.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types
{
    public class QuestDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string QuestType { get; set; } = string.Empty;
    }

    public class QuestObjectType : ObjectType<QuestDto>
    {
        protected override void Configure(IObjectTypeDescriptor<QuestDto> descriptor)
        {
            descriptor.Name("Quest");
            descriptor.Description("A STAR quest — interactive challenge avatars can complete for rewards.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
            descriptor.Field(f => f.QuestType).Description("The quest type.");
        }
    }

    public class MissionDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class MissionObjectType : ObjectType<MissionDto>
    {
        protected override void Configure(IObjectTypeDescriptor<MissionDto> descriptor)
        {
            descriptor.Name("Mission");
            descriptor.Description("A STAR mission — structured objective for avatars.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class ChapterDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ChapterObjectType : ObjectType<ChapterDto>
    {
        protected override void Configure(IObjectTypeDescriptor<ChapterDto> descriptor)
        {
            descriptor.Name("Chapter");
            descriptor.Description("A STAR chapter within a quest or game.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class GeoHotSpotDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Long { get; set; }
    }

    public class GeoHotSpotObjectType : ObjectType<GeoHotSpotDto>
    {
        protected override void Configure(IObjectTypeDescriptor<GeoHotSpotDto> descriptor)
        {
            descriptor.Name("GeoHotSpot");
            descriptor.Description("A geographic hot spot in the OASIS Omniverse/Our World.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
            descriptor.Field(f => f.Lat).Description("Latitude.");
            descriptor.Field(f => f.Long).Description("Longitude.");
        }
    }

    public class GeoNFTDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string NftType { get; set; } = string.Empty;
    }

    public class GeoNFTObjectType : ObjectType<GeoNFTDto>
    {
        protected override void Configure(IObjectTypeDescriptor<GeoNFTDto> descriptor)
        {
            descriptor.Name("GeoNFT");
            descriptor.Description("A geo-spatial NFT in the OASIS.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
            descriptor.Field(f => f.NftType).Description("The NFT type.");
        }
    }

    public class InventoryItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class InventoryItemObjectType : ObjectType<InventoryItemDto>
    {
        protected override void Configure(IObjectTypeDescriptor<InventoryItemDto> descriptor)
        {
            descriptor.Name("InventoryItem");
            descriptor.Description("An avatar inventory item in the OASIS.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }
}
