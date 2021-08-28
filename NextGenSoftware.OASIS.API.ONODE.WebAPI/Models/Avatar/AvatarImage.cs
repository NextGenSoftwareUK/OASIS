namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Avatar
{
    public class AvatarImage
    {
        public byte[] Image { get; set; }

        public AvatarImage()
        {
            Image = new Byte[] { };
        }

        public AvatarImage(byte[] data)
        {
            Image = data;
        }
    }
}