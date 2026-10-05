namespace NextGenSoftware.OASIS.API.Providers.CargoOASIS.Core.Models.Cargo
{
    public class PurchaseRequestModel
    {
        /// <summary>
        /// Required. The ID of the sale
        /// </summary>
        public string SaleId { get; set; }

        public PurchaseRequestModel(string saleId)
        {
            SaleId = saleId;
        }
        
        public PurchaseRequestModel() {}
    }
}