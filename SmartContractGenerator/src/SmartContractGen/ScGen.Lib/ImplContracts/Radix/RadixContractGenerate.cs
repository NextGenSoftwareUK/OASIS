namespace ScGen.Lib.ImplContracts.Radix;

public sealed partial class RadixContractGenerate : IRadixContractGenerate
{
    private readonly ILogger<RadixContractGenerate> _logger;
    private readonly IHandlebars _handlebars;
    private readonly string _handlebarTemplatePath;
    private readonly string _scTemplatePath;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RadixContractGenerate(ILogger<RadixContractGenerate> logger, IHttpContextAccessor httpContext,
        IHandlebars handlebars)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        logger.OperationStarted(nameof(RadixContractGenerate),
            httpContext.GetId().ToString(), httpContext.GetCorrelationId());

        _logger = logger;
        _handlebars = handlebars;
        _httpContextAccessor = httpContext;


        _handlebarTemplatePath = Path.GetFullPath(Path.Combine(
            Directory.GetCurrentDirectory(), "..",
            KeyNames.ScGenLib,
            KeyNames.HandlebarsTemplates,
            KeyNames.SmartContracts,
            KeyNames.ScryptTemplateHb));

        _scTemplatePath = Path.GetFullPath(
            Path.Combine(
                Directory.GetCurrentDirectory(), "..",
                KeyNames.ScGenLib,
                KeyNames.ScProjectScaffolds,
                KeyNames.ScryptoMainTemplate));

        _handlebars.RadixRegisterHelpers();
        ScProjectScaffoldHelper.CreateRadixProjectTemplate(_scTemplatePath, logger);
        stopwatch.Stop();
        _logger.OperationCompleted(nameof(RadixContractGenerate),
            stopwatch.ElapsedMilliseconds, _httpContextAccessor.GetCorrelationId());
    }


    public async Task<Result<GenerateContractResponse>> GenerateAsync(IFormFile jsonFile, CancellationToken token = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        _logger.OperationStarted(nameof(GenerateAsync),
            _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
        string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

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

            JObject processedJson = ProcessTemplateData(jObj);

            AddDefaultDerive(processedJson);

            LogProcessedJson(processedJson);

            IDictionary<string, object?> model = processedJson.RadixConvertJToken() as IDictionary<string, object?> ??
                                                 new Dictionary<string, object?>();

            model = model.RadixCleanModel() as IDictionary<string, object?> ?? new Dictionary<string, object?>();

            if (!File.Exists(_handlebarTemplatePath))
            {
                _logger.OperationFailed(nameof(GenerateAsync), Messages.HandlebarTemplateNotFound,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.HandlebarTemplateNotFound));
            }

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

            string scryptoCode;
            try
            {
                scryptoCode = template(model) ?? string.Empty;
            }
            catch (Exception ex)
            {
                _logger.OperationFailedWithException(nameof(GenerateAsync), ex.Message,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
            }

            if (string.IsNullOrWhiteSpace(scryptoCode))
            {
                _logger.OperationFailed(nameof(GenerateAsync), Messages.HandlebarTemplateProcessingError,
                    _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
                return Result<GenerateContractResponse>.Failure(
                    ResultPatternError.InternalServerError(Messages.HandlebarTemplateProcessingError));
            }
            
            return await CreateResponseAsync(tempDir, scryptoCode, jObj, token);
        }
        catch (Exception ex)
        {
            _logger.OperationFailedWithException(nameof(GenerateAsync), ex.Message,
                _httpContextAccessor.GetId().ToString(), _httpContextAccessor.GetCorrelationId());
            return Result<GenerateContractResponse>.Failure(ResultPatternError.InternalServerError(ex.Message));
        }
        finally
        {
            tempDir.DeleteDirectorySafe();
            stopwatch.Stop();
            _logger.OperationCompleted(nameof(GenerateAsync),
                stopwatch.ElapsedMilliseconds, _httpContextAccessor.GetCorrelationId());
        }
    }
}