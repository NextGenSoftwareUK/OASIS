
namespace NextGenSoftware.Holochain.HoloNET.Client
{
    public enum CapGrantAccessType
    {
        /// <summary>
        /// No restriction: callable by anyone.
        /// </summary>
        Unrestricted,

        /// <summary>
        /// Callable by anyone who can provide the secret (the secret is a randomly generated 64 byte number and is accessible from the GetCapGrantSecret function once the AdminAuthorizeSigningCredentialsAndGrantZomeCallCapability or AdminAuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync function have been called).
        /// </summary>
        Transferable,

        /// <summary>
        /// Callable by anyone in the list of assignees who possesses the secret (the secret is a randomly generated 64 byte number and is accessible from the GetCapGrantSecret function once the AdminAuthorizeSigningCredentialsAndGrantZomeCallCapability or AdminAuthorizeSigningCredentialsAndGrantZomeCallCapabilityAsync function have been called).
        /// </summary>
        Assigned
    }
}