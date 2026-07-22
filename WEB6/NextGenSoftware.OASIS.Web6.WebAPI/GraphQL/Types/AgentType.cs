using HotChocolate.Types;

namespace NextGenSoftware.OASIS.Web6.WebAPI.GraphQL.Types
{
    /// <summary>Represents a FAHRN reasoning agent registered in the network.</summary>
    public class Agent
    {
        public string AgentId { get; set; } = "";
        public string AgentName { get; set; } = "";
        public string Provider { get; set; } = "";
        public string Model { get; set; } = "";
        public double CompositeScore { get; set; }
        public string State { get; set; } = "idle";
    }

    public class AgentType : ObjectType<Agent>
    {
        protected override void Configure(IObjectTypeDescriptor<Agent> descriptor)
        {
            descriptor.Description("A FAHRN reasoning agent capable of solving AI tasks via Agent-to-Agent (A2A) dispatch.");
            descriptor.Field(f => f.AgentId).Description("Unique identifier for this agent (GUID).");
            descriptor.Field(f => f.AgentName).Description("Display name of the agent.");
            descriptor.Field(f => f.Provider).Description("Underlying AI provider powering this agent.");
            descriptor.Field(f => f.Model).Description("Specific model version used by this agent.");
            descriptor.Field(f => f.CompositeScore).Description("Performance score used by FAHRN for agent selection.");
            descriptor.Field(f => f.State).Description("Current operational state of the agent.");
        }
    }
}
