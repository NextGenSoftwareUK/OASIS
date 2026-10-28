using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NextGenSoftware.OASIS.API.Core.Enums;
using NextGenSoftware.OASIS.API.Core.Interfaces;
using NextGenSoftware.OASIS.API.Core.Managers;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Helpers;
using NextGenSoftware.OASIS.Common;

namespace NextGenSoftware.OASIS.API.ONODE.WebAPI.Controllers
{
    /// <summary>
    /// Native data import: turn data other applications stored in a provider into OASIS holons. Importing reads the
    /// provider's own databases, so it is a Wizard (admin) operation; on the sandbox every signed-in demo avatar may use it,
    /// with smaller limits. See Docs/Devs/NATIVE_DATA_IMPORT.md.
    /// </summary>
    [Route("api/native-data")]
    [ApiController]
    [Authorize]
    public class NativeDataController : OASISControllerBase
    {
        public const int SandboxMaximumRecords = 500;
        public const int MaximumPreviewRecords = 20;

        HolonManager _holonManager;

        HolonManager HolonManager
        {
            get
            {
                if (_holonManager == null)
                {
                    if (AdmittedStorageManagerAcquisition.TryCreate(
                        () => OASISBootLoader.OASISBootLoader.OASISDNA,
                        () => ProviderManager.Instance.CurrentStorageProvider,
                        (provider, dna) => new HolonManager(null, dna, ProviderManager.Instance), out HolonManager admitted))
                        return _holonManager = admitted;

                    // Same acquisition as DataController: outside admitted V2 execution, use the activated default provider.
                    OASISResult<IOASISStorageProvider> result = Task.Run(OASISBootLoader.OASISBootLoader.GetAndActivateDefaultStorageProviderAsync).Result;
                    if (result.IsError || result.Result == null)
                        throw new InvalidOperationException($"No storage provider is available for native data import: {result.Message}");
                    _holonManager = new HolonManager(result.Result);
                }
                return _holonManager;
            }
        }

        public sealed class NativeImportRequest
        {
            public NativeDataMapping Mapping { get; set; }
            public int PageSize { get; set; } = 200;
            public int MaximumRecords { get; set; } = 10000;
        }

        public sealed class NativeImportAllRequest
        {
            public Dictionary<string, NativeDataMapping> Mappings { get; set; }
            public int PageSize { get; set; } = 200;
            public int MaximumRecordsPerSource { get; set; } = 10000;
        }

        public sealed class NativeRecordPreview
        {
            public NativeRecord Native { get; set; }
            public Guid HolonId { get; set; }
            public string HolonName { get; set; }
            public string HolonDescription { get; set; }
            public HolonType HolonType { get; set; }
            public Dictionary<string, object> HolonMetaData { get; set; }
            public string Error { get; set; }
        }

        /// <summary>Registered providers that can import native data.</summary>
        [HttpGet("providers")]
        public ActionResult<OASISResult<IEnumerable<ProviderType>>> GetProviders()
        {
            if (!MayImport()) return Forbidden<IEnumerable<ProviderType>>();
            var types = ProviderManager.Instance.GetAllRegisteredProviders().OfType<INativeDataImportProvider>()
                .Select(provider => provider.NativeDataProviderType).Distinct().ToList();
            return Ok(new OASISResult<IEnumerable<ProviderType>>(types) { Message = $"{types.Count} providers support native import." });
        }

        /// <summary>The collections, tables or account sets a provider can import.</summary>
        [HttpGet("{providerType}/sources")]
        public async Task<ActionResult<OASISResult<IReadOnlyList<NativeDataSource>>>> GetSources(ProviderType providerType,
            CancellationToken cancellationToken)
        {
            if (!MayImport()) return Forbidden<IReadOnlyList<NativeDataSource>>();
            var provider = Resolve<IReadOnlyList<NativeDataSource>>(providerType, out var refusal);
            if (provider == null) return refusal;
            return Respond(await new NativeDataImportManager(HolonManager).ListSourcesAsync(provider, cancellationToken));
        }

        /// <summary>The first records of a source next to the holons they would become. Nothing is saved.</summary>
        [HttpPost("{providerType}/sources/{sourceName}/preview")]
        public async Task<ActionResult<OASISResult<IReadOnlyList<NativeRecordPreview>>>> Preview(ProviderType providerType, string sourceName,
            [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] NativeDataMapping mapping,
            [FromQuery] int count = 5, CancellationToken cancellationToken = default)
        {
            if (!MayImport()) return Forbidden<IReadOnlyList<NativeRecordPreview>>();
            var provider = Resolve<IReadOnlyList<NativeRecordPreview>>(providerType, out var refusal);
            if (provider == null) return refusal;
            if (count < 1 || count > MaximumPreviewRecords)
                return BadRequest(new OASISResult<IReadOnlyList<NativeRecordPreview>> { IsError = true, Message = $"count must be between 1 and {MaximumPreviewRecords}." });

            var saved = await SavedMappingAsync(providerType, sourceName, mapping);
            if (saved.IsError) return Respond(new OASISResult<IReadOnlyList<NativeRecordPreview>> { IsError = true, ErrorCode = saved.ErrorCode, Message = saved.Message });
            mapping = saved.Result;
            var page = await provider.ReadNativeRecordsAsync(sourceName, null, count, cancellationToken);
            if (page.IsError)
                return Respond(new OASISResult<IReadOnlyList<NativeRecordPreview>> { IsError = true, ErrorCode = page.ErrorCode, Message = page.Message });

            var previews = page.Result.Records.Select(record =>
            {
                var mapped = NativeRecordHolonMapper.CreateHolon(AvatarId, providerType, record, mapping);
                return new NativeRecordPreview
                {
                    Native = record,
                    HolonId = NativeRecordHolonMapper.StableHolonId(AvatarId, providerType, record.SourceName, record.NativeKey),
                    HolonName = mapped.Result?.Name,
                    HolonDescription = mapped.Result?.Description,
                    HolonType = mapped.Result?.HolonType ?? HolonType.Holon,
                    HolonMetaData = mapped.Result?.MetaData,
                    Error = mapped.IsError ? mapped.Message : null
                };
            }).ToList();
            return Ok(new OASISResult<IReadOnlyList<NativeRecordPreview>>(previews) { Message = $"Previewed {previews.Count} records from {providerType}/{sourceName}." });
        }

        /// <summary>Imports one source. Running it again updates the same holons instead of creating new ones.</summary>
        [HttpPost("{providerType}/sources/{sourceName}/import")]
        public async Task<ActionResult<OASISResult<NativeDataImportReport>>> Import(ProviderType providerType, string sourceName,
            [FromBody] NativeImportRequest request, CancellationToken cancellationToken)
        {
            if (!MayImport()) return Forbidden<NativeDataImportReport>();
            var provider = Resolve<NativeDataImportReport>(providerType, out var refusal);
            if (provider == null) return refusal;
            request ??= new NativeImportRequest();
            var mapping = await SavedMappingAsync(providerType, sourceName, request.Mapping);
            if (mapping.IsError) return Respond(new OASISResult<NativeDataImportReport> { IsError = true, ErrorCode = mapping.ErrorCode, Message = mapping.Message });
            return Respond(await new NativeDataImportManager(HolonManager).ImportSourceAsync(provider, sourceName, AvatarId,
                mapping.Result, request.PageSize, Limit(request.MaximumRecords), cancellationToken));
        }

        /// <summary>Imports every source the provider lists, with the given mappings and the naming convention for the rest.</summary>
        [HttpPost("{providerType}/import-all")]
        public async Task<ActionResult<OASISResult<IReadOnlyList<NativeDataImportReport>>>> ImportAll(ProviderType providerType,
            [FromBody] NativeImportAllRequest request, CancellationToken cancellationToken)
        {
            if (!MayImport()) return Forbidden<IReadOnlyList<NativeDataImportReport>>();
            var provider = Resolve<IReadOnlyList<NativeDataImportReport>>(providerType, out var refusal);
            if (provider == null) return refusal;
            request ??= new NativeImportAllRequest();
            var saved = await Program.SettingsManager.GetNativeImportMappingsAsync(AvatarId, providerType);
            if (saved.IsError) return Respond(new OASISResult<IReadOnlyList<NativeDataImportReport>> { IsError = true, ErrorCode = saved.ErrorCode, Message = saved.Message });
            var mappings = new Dictionary<string, NativeDataMapping>(saved.Result, StringComparer.Ordinal);
            foreach (var entry in request.Mappings ?? new Dictionary<string, NativeDataMapping>()) mappings[entry.Key] = entry.Value;
            return Respond(await new NativeDataImportManager(HolonManager).ImportAllAsync(provider, AvatarId, mappings,
                request.PageSize, Limit(request.MaximumRecordsPerSource), cancellationToken));
        }

        /// <summary>This avatar's saved mappings for a provider. Imports and previews use them when no mapping is sent.</summary>
        [HttpGet("{providerType}/mappings")]
        public async Task<ActionResult<OASISResult<Dictionary<string, NativeDataMapping>>>> GetMappings(ProviderType providerType)
        {
            if (!MayImport()) return Forbidden<Dictionary<string, NativeDataMapping>>();
            return Respond(await Program.SettingsManager.GetNativeImportMappingsAsync(AvatarId, providerType));
        }

        /// <summary>Saves how one source becomes holons, so later imports of it need no mapping.</summary>
        [HttpPut("{providerType}/sources/{sourceName}/mapping")]
        public async Task<ActionResult<OASISResult<bool>>> SaveMapping(ProviderType providerType, string sourceName, [FromBody] NativeDataMapping mapping)
        {
            if (!MayImport()) return Forbidden<bool>();
            if (mapping == null) return BadRequest(new OASISResult<bool> { IsError = true, Message = "A mapping is required." });
            if (mapping.SourceName != null && mapping.SourceName != sourceName)
                return BadRequest(new OASISResult<bool> { IsError = true, ErrorCode = "NATIVE_IMPORT_MAPPING_MISMATCH", Message = $"The mapping is for '{mapping.SourceName}', not '{sourceName}'." });
            mapping.SourceName = sourceName;
            return Respond(await Program.SettingsManager.SaveNativeImportMappingAsync(AvatarId, providerType, mapping));
        }

        [HttpDelete("{providerType}/sources/{sourceName}/mapping")]
        public async Task<ActionResult<OASISResult<bool>>> DeleteMapping(ProviderType providerType, string sourceName)
        {
            if (!MayImport()) return Forbidden<bool>();
            return Respond(await Program.SettingsManager.DeleteNativeImportMappingAsync(AvatarId, providerType, sourceName));
        }

        /// <summary>The mapping sent with the request, else the avatar's saved one, else null (the naming convention).</summary>
        private async Task<OASISResult<NativeDataMapping>> SavedMappingAsync(ProviderType providerType, string sourceName, NativeDataMapping sent)
        {
            if (sent != null) return new OASISResult<NativeDataMapping>(sent);
            var saved = await Program.SettingsManager.GetNativeImportMappingsAsync(AvatarId, providerType);
            if (saved.IsError) return new OASISResult<NativeDataMapping> { IsError = true, ErrorCode = saved.ErrorCode, Message = saved.Message };
            saved.Result.TryGetValue(sourceName, out var mapping);
            return new OASISResult<NativeDataMapping>(mapping);
        }

        private bool MayImport() => ProviderFaultGate.IsEnabled || Avatar?.AvatarType.Value == AvatarType.Wizard;

        private static int Limit(int requested) => ProviderFaultGate.IsEnabled ? Math.Min(requested, SandboxMaximumRecords) : requested;

        private INativeDataImportProvider Resolve<T>(ProviderType providerType, out ActionResult refusal)
        {
            refusal = null;
            var resolved = ProviderManager.Instance.ResolveStorageProvider(providerType);
            if (resolved.IsError || resolved.Result == null)
            {
                refusal = BadRequest(new OASISResult<T> { IsError = true, Message = $"{providerType} is not available: {resolved.Message}" });
                return null;
            }
            if (resolved.Result is not INativeDataImportProvider importer)
            {
                refusal = BadRequest(new OASISResult<T> { IsError = true, ErrorCode = "NATIVE_IMPORT_UNSUPPORTED", Message = $"{providerType} does not support native data import yet." });
                return null;
            }
            return importer;
        }

        private ActionResult Respond<T>(OASISResult<T> result) => result.IsError ? BadRequest(result) : Ok(result);

        private ActionResult Forbidden<T>() => StatusCode(403, new OASISResult<T>
        { IsError = true, ErrorCode = "NATIVE_IMPORT_FORBIDDEN", Message = "Native data import reads the provider's own databases and needs a Wizard avatar." });
    }
}
