
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    public interface IAvatarAura
    {
        int Progress { get; set; }
        int Brightness { get; set; }
        int Size { get; set; }
        int ColourBlue { get; set; }
        int ColourGreen { get; set; }
        int ColourRed { get; set; }
        int Level { get; set; }
        int Value { get; set; }
    }
}