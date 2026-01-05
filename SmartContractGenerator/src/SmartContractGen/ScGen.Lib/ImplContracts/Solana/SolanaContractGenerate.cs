namespace ScGen.Lib.ImplContracts.Solana;

public sealed partial class SolanaContractGenerate : ISolanaContractGenerate
{
    private readonly ILogger<SolanaContractGenerate> _logger;
    private readonly IHandlebars _handlebars;
    private readonly string _handlebarTemplatePath;
    private readonly string _scTemplatePath;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SolanaContractGenerate(ILogger<SolanaContractGenerate> logger, IHttpContextAccessor httpContext,
        IHandlebars handlebars)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(SolanaContractGenerate),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        _logger = logger;
        _handlebars = handlebars;
        _httpContextAccessor = httpContext;


        _handlebarTemplatePath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..",
            KeyNames.ScGenLib,
            KeyNames.HandlebarsTemplates,
            KeyNames.SmartContracts,
            KeyNames.RustTemplateHb));

        _scTemplatePath = Path.GetFullPath(
            Path.Combine(
                Directory.GetCurrentDirectory(), "..",
                KeyNames.ScGenLib,
                KeyNames.ScProjectScaffolds,
                KeyNames.RustMainTemplate));

        _handlebars.SolanaRegisterHelpers();
        ScProjectScaffoldHelper.CreateSolanaProjectTemplate(_scTemplatePath, logger);
        stopwatch.Stop();
        _logger.OperationCompleted(nameof(SolanaContractGenerate),
            stopwatch.ElapsedMilliseconds, _httpContextAccessor.GetCorrelationId());
    }

    public async Task<Result<GenerateContractResponse>> GenerateAsync(IFormFile jsonFile, CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        _logger.OperationStarted(nameof(SolanaContractGenerate),
            _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());

        try
        {
            Result<GenerateContractResponse> validation = Validation(jsonFile);
            if (!validation.IsSuccess) return validation;

            string jsonContent;
            using (StreamReader sr = new StreamReader(jsonFile.OpenReadStream()))
            {
                jsonContent = await sr.ReadToEndAsync(token).ConfigureAwait(false);
            }

            jsonContent = WebUtility.HtmlDecode(jsonContent);

            JObject jObj;
            try
            {
                jObj = JObject.Parse(jsonContent);
            }
            catch (Exception ex)
            {
                _logger.OperationFailedWithException(nameof(GenerateAsync), ex.Message,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(ResultPatternError.BadRequest(ex.Message));
            }

            IDictionary<string, object?> model = jObj.SolanaConvertJToken() as IDictionary<string, object?> ??
                                                 new Dictionary<string, object?>();


            model = model.SolanaCleanModel() as IDictionary<string, object?> ?? new Dictionary<string, object?>();

            if (!File.Exists(_handlebarTemplatePath))
            {
                _logger.OperationFailed(nameof(GenerateAsync), Messages.HandlebarTemplateNotFound,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.HandlebarTemplateNotFound));
            }

            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            DirectoryExtensions.CopyDirectory(_scTemplatePath, tempDir);

            string tplText = await File.ReadAllTextAsync(_handlebarTemplatePath, token).ConfigureAwait(false);

            HandlebarsTemplate<object, object>? template;
            try
            {
                template = _handlebars.Compile(tplText);
            }
            catch (Exception ex)
            {
                _logger.OperationFailedWithException(nameof(GenerateAsync), ex.Message,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
            }

            string rustCode;
            try
            {
                rustCode = template(model) ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.OperationFailedWithException(nameof(GenerateAsync), ex.Message,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
            }

            if (string.IsNullOrWhiteSpace(rustCode))
            {
                _logger.OperationFailed(nameof(GenerateAsync), Messages.HandlebarTemplateProcessingError,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.HandlebarTemplateProcessingError));
            }

            return await CreateResponseAsync(tempDir, rustCode, jObj, token);
        }
        catch (Exception ex)
        {
            _logger.OperationFailedWithException(nameof(GenerateAsync), ex.Message,
                _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
            return Result<GenerateContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
        finally
        {
            stopwatch.Stop();
            _logger.OperationCompleted(nameof(SolanaContractGenerate),
                stopwatch.ElapsedMilliseconds, _httpContextAccessor.GetCorrelationId());
        }
    }
}