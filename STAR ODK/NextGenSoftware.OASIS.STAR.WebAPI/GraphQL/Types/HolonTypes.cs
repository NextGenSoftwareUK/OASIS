using HotChocolate.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types
{
    public class HolonDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string HolonType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class HolonObjectType : ObjectType<HolonDto>
    {
        protected override void Configure(IObjectTypeDescriptor<HolonDto> descriptor)
        {
            descriptor.Name("Holon");
            descriptor.Description("A STAR holon — self-contained unit in the OASIS.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
            descriptor.Field(f => f.HolonType).Description("The holon type enum value.");
            descriptor.Field(f => f.Status).Description("Active or Inactive status.");
        }
    }

    public class CelestialSpaceDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CelestialSpaceObjectType : ObjectType<CelestialSpaceDto>
    {
        protected override void Configure(IObjectTypeDescriptor<CelestialSpaceDto> descriptor)
        {
            descriptor.Name("CelestialSpace");
            descriptor.Description("A STAR celestial space (galaxy cluster, universe, omniverse, etc.).");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }
}
