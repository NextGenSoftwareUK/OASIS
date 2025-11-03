using System;

namespace NextGenSoftware.OASIS.API.Core.Interfaces.NFT
{
    public interface IOLand
    {
        public Guid Id { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int OlandsCount { get; set; }
        public decimal TopSize { get; set; }
        public decimal RightSize { get; set; }
        public string UnitOfMeasure { get; set; }
        public bool IsRemoved { get; set; }
        public Guid PreviousId { get; set; }
    }
}