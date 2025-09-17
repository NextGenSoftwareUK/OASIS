namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models
{
    public sealed class ManageOlandUnitRequestDto
    {
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public int OlandsCount { get; set; }
        public decimal TopSize { get; set; }
        public decimal RightSize { get; set; }
        public string UnitOfMeasure { get; set; }
        public bool IsRemoved { get; set; }
    }
}