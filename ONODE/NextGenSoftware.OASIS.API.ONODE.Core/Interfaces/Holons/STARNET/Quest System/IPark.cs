using NextGenSoftware.OASIS.API.Core.Interfaces.STAR;

namespace NextGenSoftware.OASIS.API.ONODE.Core.Interfaces.Holons
{
    public interface IPark : ISTARNETHolon
    {
        string ParkName { get; set; }
        string Description { get; set; }
        double Latitude { get; set; }
        double Longitude { get; set; }
        int RadiusInMetres { get; set; }
        string Address { get; set; }
        string City { get; set; }
        string Country { get; set; }
        string PostalCode { get; set; }
        bool IsPublic { get; set; }
        bool IsActive { get; set; }
    }
}
