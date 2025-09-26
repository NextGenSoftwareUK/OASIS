
namespace NextGenSoftware.OASIS.API.Core.Interfaces.STAR
{
    public interface ISTARNETHolon : IHolon
    {
        public ISTARNETDNA STARNETDNA { get; set; }
        public byte[] PublishedSTARNETHolon { get; set; }

        //IList<ISTARNETDependency> LibrariesMetaData { get; set; }
        //IList<ISTARNETDependency> RuntimesMetaData { get; set; }
        //IList<ISTARNETDependency> OAPPTemplatesMetaData { get; set; }
    }
}