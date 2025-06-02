
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI
{
    public class Trigger : BaseEntity
    {
        [BsonRepresentation(BsonType.ObjectId)]
        public string PhaseId { get; set; }

        public TriggerWhenCondition WhenCondition1 { get; set; }
        public TriggerWhenCondition WhenCondition2 { get; set; }
        public TriggerWhenCondition WhenCondition3 { get; set; }
        public TriggerLogicOperator LogicOperator1 { get; set; }
        public TriggerLogicOperator LogicOperator2 { get; set; }
        public TriggerLogicOperator LogicOperator3 { get; set; }
        public TriggerAction Action1 { get; set; }
        public TriggerAction Action2 { get; set; }
        public TriggerAction Action3 { get; set; }
        public string Description { get; set; }
    }

    public enum TriggerWhenCondition
    {
        PhaseStarts,
        PhaseEnds,
        PhaseIsHandedOver,
        PhaseIsLate,
        PhaseIsEarly,
        MaterialsArrive,
        MaterialsDispatched,
        MaterialsAreLate,
        DeliverySent,
        DeliveryArrives,
        OperativeAcceptsHandover,
        OperativeRejectsHandover
    }

    public enum TriggerAction
    {
        EmailPhaseOperatives,
        MessagePhaseOperatives,
        PayPhaseOperatives,
        EmailPhaseManagers,
        MessagePhaseManagers,
        EmailMaterialsProvider,
        MessageMaterialsProvider,
        EmailDeliveryProvider,
        MessageDeliveryProvider,
        SendDeliveryToPhaseAfterNext,
        SkipPhase,
        PausePhase,
        JumpToPhase
    }

    public enum TriggerLogicOperator
    {
        OR,
        AND,
        NOT
    }
}