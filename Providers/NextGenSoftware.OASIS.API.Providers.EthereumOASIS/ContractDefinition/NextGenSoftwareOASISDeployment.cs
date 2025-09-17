namespace NextGenSoftware.OASIS.API.Providers.EthereumOASIS.ContractDefinition
{
    public partial class NextGenSoftwareOASISDeployment : NextGenSoftwareOasisDeploymentBase
    {
        public NextGenSoftwareOASISDeployment() : base(BYTECODE) { }
        public NextGenSoftwareOASISDeployment(string byteCode) : base(byteCode) { }
    }
}