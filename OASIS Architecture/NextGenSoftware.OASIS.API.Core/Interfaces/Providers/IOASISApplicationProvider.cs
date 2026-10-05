
namespace NextGenSoftware.OASIS.API.Core.Interfaces
{
    // NOTE: These are OASIS Providers that offer an application layer that may or may not use other OASIS Providers such as a
    // storage provider. For example SEEDSOASIS is a OASISApplicationProvider but uses the EOSIOOASISProvider (Storage/Network Provider) for its storage/EOS calls etc.
    // An OASISApplicationProvider is NOT a OApp (OASIS Application), these are client applications built on top of the OASIS API.
    public interface IOASISApplicationProvider : IOASISProvider
    {
        //More to come here soon... ;-)
    }
}
