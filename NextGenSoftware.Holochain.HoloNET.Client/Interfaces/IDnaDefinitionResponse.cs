
namespace NextGenSoftware.Holochain.HoloNET.Client.Interfaces
{
    public interface IDnaDefinitionResponse
    {
        public string type { get; set; }
        public DnaDefinitionResponseDetail data { get; set; }
    }
}