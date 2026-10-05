namespace ScGen.Lib.Contracts.BaseContracts;

public interface IContractCompile
{
    Task<Result<CompileContractResponse>> CompileAsync(IFormFile sourceCodeFile, CancellationToken token = default);
}