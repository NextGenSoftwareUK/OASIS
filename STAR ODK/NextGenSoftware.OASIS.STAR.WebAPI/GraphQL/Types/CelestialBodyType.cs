using HotChocolate.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types
{
    /// <summary>Represents a STAR celestial body (planet, solar system, galaxy, etc.).</summary>
    public class CelestialBodyDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class CelestialBodyType : ObjectType<CelestialBodyDto>
    {
        protected override void Configure(IObjectTypeDescriptor<CelestialBodyDto> descriptor)
        {
            descriptor.Description("A STAR celestial body — planet, solar system, galaxy, or multiverse node.");
            descriptor.Field(f => f.Id).Description("Unique identifier of the celestial body.");
            descriptor.Field(f => f.Name).Description("Display name of the celestial body.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }
}
