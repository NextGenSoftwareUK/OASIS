namespace NextGenSoftware.OASIS.API.Providers.HoloOASIS.Core
{
    public interface IProfile : API.Core.IProfile
    {
        string HcAddressHash { get; set; }
    }
}
