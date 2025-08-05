
namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISTARNETHolon : IHolon
    {
        public ISTARNETDNA STARNETDNA { get; set; }
        public byte[] PublishedSTARNETHolon { get; set; }

        //IList<ISTARNETHolonMetaData> LibrariesMetaData { get; set; }
        //IList<ISTARNETHolonMetaData> RuntimesMetaData { get; set; }
        //IList<ISTARNETHolonMetaData> OAPPTemplatesMetaData { get; set; }
    }
}