namespace ScGen.Lib.Contracts.BaseContracts;

public interface IContractDeploy
{
    Task<Result<DeployContractResponse>>
        DeployAsync(IFormFile? schema, IFormFile bytecodeFile, CancellationToken token = default);
}