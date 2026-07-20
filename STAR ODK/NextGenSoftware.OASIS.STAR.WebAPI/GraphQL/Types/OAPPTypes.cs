using HotChocolate.Types;

namespace NextGenSoftware.OASIS.STAR.WebAPI.GraphQL.Types
{
    public class OAPPDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class OAPPObjectType : ObjectType<OAPPDto>
    {
        protected override void Configure(IObjectTypeDescriptor<OAPPDto> descriptor)
        {
            descriptor.Name("OAPP");
            descriptor.Description("An OASIS Application (oApp) built on the STAR ODK.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class GameDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class GameObjectType : ObjectType<GameDto>
    {
        protected override void Configure(IObjectTypeDescriptor<GameDto> descriptor)
        {
            descriptor.Name("Game");
            descriptor.Description("A game built on the STAR ODK.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class PluginDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class PluginObjectType : ObjectType<PluginDto>
    {
        protected override void Configure(IObjectTypeDescriptor<PluginDto> descriptor)
        {
            descriptor.Name("Plugin");
            descriptor.Description("A STAR ODK plugin.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class LibraryDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class LibraryObjectType : ObjectType<LibraryDto>
    {
        protected override void Configure(IObjectTypeDescriptor<LibraryDto> descriptor)
        {
            descriptor.Name("Library");
            descriptor.Description("A STAR ODK shared library.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class RuntimeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class RuntimeObjectType : ObjectType<RuntimeDto>
    {
        protected override void Configure(IObjectTypeDescriptor<RuntimeDto> descriptor)
        {
            descriptor.Name("Runtime");
            descriptor.Description("A STAR ODK runtime environment.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class OAPPTemplateDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class OAPPTemplateObjectType : ObjectType<OAPPTemplateDto>
    {
        protected override void Configure(IObjectTypeDescriptor<OAPPTemplateDto> descriptor)
        {
            descriptor.Name("OAPPTemplate");
            descriptor.Description("A template for creating OASIS Applications.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }

    public class ZomeDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class ZomeObjectType : ObjectType<ZomeDto>
    {
        protected override void Configure(IObjectTypeDescriptor<ZomeDto> descriptor)
        {
            descriptor.Name("Zome");
            descriptor.Description("A STAR ODK zome — module within an OAPP.");
            descriptor.Field(f => f.Id).Description("Unique identifier.");
            descriptor.Field(f => f.Name).Description("Display name.");
            descriptor.Field(f => f.Description).Description("Human-readable description.");
        }
    }
}
