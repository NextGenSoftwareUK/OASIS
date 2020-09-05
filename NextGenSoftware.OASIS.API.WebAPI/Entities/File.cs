
namespace NextGenSoftware.OASIS.API.ORIAServices
{
    public class File : BaseEntityTitleDesc
    {
        public string URI { get; set; }
        public FileType Type { get; set; }
    }

    public enum FileType
    {
        PDF, 
        WordDoc,
        Image,
        Video,
        Text
    }
}