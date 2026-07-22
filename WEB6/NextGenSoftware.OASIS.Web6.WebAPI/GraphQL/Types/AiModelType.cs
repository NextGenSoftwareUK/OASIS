using HotChocolate.Types;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GraphQL.Types
{
    /// <summary>Describes an AI/ML model available in the FAHRN network.</summary>
    public class AiModel
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Provider { get; set; } = "";
        public string Type { get; set; } = "";
        public string Status { get; set; } = "";
        public string Description { get; set; } = "";
    }

    public class AiModelType : ObjectType<AiModel>
    {
        protected override void Configure(IObjectTypeDescriptor<AiModel> descriptor)
        {
            descriptor.Description("An AI or ML model registered in the OASIS Web6 FAHRN network.");
            descriptor.Field(f => f.Id).Description("Unique model identifier.");
            descriptor.Field(f => f.Name).Description("Human-readable model name.");
            descriptor.Field(f => f.Provider).Description("Provider name (e.g. OpenAI, Claude, HuggingFace).");
            descriptor.Field(f => f.Type).Description("Model capability type (e.g. Completion, Embedding, ImageGeneration).");
            descriptor.Field(f => f.Status).Description("Operational status of the model.");
            descriptor.Field(f => f.Description).Description("Short description of the model's purpose.");
        }
    }
}
