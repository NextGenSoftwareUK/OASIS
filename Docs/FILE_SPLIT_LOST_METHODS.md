# Methods Lost in the File-Splitting Refactors

_Audit date: 2026-09-04_

## What this is

The repo contains ~162 `refactor: split ... into partial class files` commits. Some of them **deleted** members instead of redistributing them. This lists every method a split commit removed from the source that no later commit re-added.

**These are not commented out — the source was deleted.** They are also not referenced anywhere today (verified across all 4,151 `.cs` files on disk, submodules included), so nothing is currently broken. They are catalogued here because unreferenced does not mean unwanted — several form coherent feature sets.

A separate case, the 12 `WalletManager` import overloads, **was** still being called from STAR ODK and has already been restored.

**Total: 98 methods, dropped across 5 commits.**

Full recovered source for each is in the companion file `FILE_SPLIT_LOST_METHODS_SOURCE.md` — every one can be restored verbatim.

---

## `b51741f46` — 60 methods

> - Started work on re-factoring the Publish methods in OAPPManager and OAPPSystemManagerBase so it is split into smaller sub-functions that the base class can call into effectively creating a publish sub-api for the new OAPP Template/Version/Runtime system.

| Method | Original file |
|---|---|
| `ActivateRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ActivateRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `CreateRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `CreateRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DeactivateRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DeactivateRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DeleteRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DeleteRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DownloadAndInstallRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DownloadAndInstallRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DownloadRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `DownloadRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `EditRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `InstallRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `InstallRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `IsRuntimeTemplateInstalled` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `IsRuntimeTemplateInstalledAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListDeactivatedRuntimeTemplates` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListDeactivatedRuntimeTemplatesAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListInstalledRuntimeTemplates` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListInstalledRuntimeTemplatesAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListUnInstalledRuntimeTemplates` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListUnInstalledRuntimeTemplatesAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListUnpublishedRuntimeTemplates` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ListUnpublishedRuntimeTemplatesAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadAllRuntimeTemplates` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadAllRuntimeTemplatesAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadAllRuntimeTemplatesForAvatar` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadAllRuntimeTemplatesForAvatarAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadInstalledRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadInstalledRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadRuntimeTemplateVersion` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadRuntimeTemplateVersionAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadRuntimeTemplateVersions` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `LoadRuntimeTemplateVersionsAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `OpenRuntimeTemplateFolder` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `OpenRuntimeTemplateFolderAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `PublishOAPPTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `PublishRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `PublishRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadOAPPSystemHolonDNAFromPublishedFile` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` |
| `ReadOAPPSystemHolonDNAFromPublishedFileAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` |
| `ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFile` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadRuntimeTemplateDNAFromPublishedRuntimeTemplateFileAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadRuntimeTemplateDNAFromSourceOrInstalledFolder` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadRuntimeTemplateDNAFromSourceOrInstalledFolderAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `RepublishRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `RepublishRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `SaveRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `SaveRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `SearchRuntimeTemplates` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `SearchRuntimeTemplatesAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `UninstallRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `UninstallRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `UnpublishRuntimeTemplate` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `UnpublishRuntimeTemplateAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `WriteRuntimeTemplateDNA` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `WriteRuntimeTemplateDNAAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |

## `3fdf511ee` — 22 methods

> - Finished refactoring the Publish functions in OAPPSystemManagerBase and splitting them out into seperate sub-functions.

| Method | Original file |
|---|---|
| `ReadOAPPDNAFromPublishedOAPPFile` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` |
| `ReadOAPPDNAFromPublishedOAPPFileAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` |
| `ReadOAPPDNAFromSourceOrInstalledFolder` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` |
| `ReadOAPPDNAFromSourceOrInstalledFolderAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` |
| `ReadOAPPSystemHolonDNAFromSourceOrInstallFolder` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs	` |
| `ReadOAPPSystemHolonDNAFromSourceOrInstallFolderAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs	` |
| `ReadOAPPTemplateDNAFromPublishedOAPPTemplateFile` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `ReadOAPPTemplateDNAFromPublishedOAPPTemplateFileAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `ReadOAPPTemplateDNAFromSourceOrInstalledFolder` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `ReadOAPPTemplateDNAFromSourceOrInstalledFolderAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `ReadRuntimeDNAFromPublishedRuntimeFile` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadRuntimeDNAFromPublishedRuntimeFileAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadRuntimeDNAFromSourceOrInstalledFolder` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `ReadRuntimeDNAFromSourceOrInstalledFolderAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `WriteOAPPDNA` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` |
| `WriteOAPPDNAAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPManager.cs` |
| `WriteOAPPSystemHolonDNA` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` |
| `WriteOAPPSystemHolonDNAAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPSystemManagerBase.cs` |
| `WriteOAPPTemplateDNA` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `WriteOAPPTemplateDNAAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/OAPPTemplateManager.cs` |
| `WriteRuntimeDNA` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |
| `WriteRuntimeDNAAsync` | `NextGenSoftware.OASIS.API.ONODE.Core/Managers/OAPP System/RuntimeManager.cs` |

## `f2b2cebf2` — 11 methods

> refactor: split ProviderManager.cs (1800 lines) into 4 partial class files

| Method | Original file |
|---|---|
| `AddToAutoFailOverListNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `AddToAutoLoadBalanceListNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `AddToAutoReplicationListNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `GetAvailableProvidersNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `GetProviderConfigurationNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `GetSwitchStatusNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `RemoveFromAutoFailOverListNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `RemoveFromAutoLoadBalanceListNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `RemoveFromAutoReplicationListNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `SelectOptimalProviderForLoadBalancingNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |
| `SwitchStorageProviderAsyncNew` | `OASIS Architecture/NextGenSoftware.OASIS.API.Core/Managers/OASIS HyperDrive/Provider Management/ProviderManager.cs` |

## `88817e70b` — 3 methods

> Lots done on implementing new cross-chain OASISNFTCollections & OASISGeoNFTCollections. Also continued work on ONET and also refactored core OASIS Holonic Architecture so Holons are now split out to AuditBase, HolonBase, SemanticHolon, Holon & CelestialHolon making it easier to use/follow and can use ligher weight holons to suit different use cases...

| Method | Original file |
|---|---|
| `CalculateHealthFromMetrics` | `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Network/NetworkMetricsService.cs` |
| `GetOASISGeoNFTCollectionAsync` | `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/NFTManager.cs` |
| `GetOASISNFTCollectionAsync` | `ONODE/NextGenSoftware.OASIS.API.ONODE.Core/Managers/NFTManager.cs` |

## `7b673485a` — 2 methods

> - Finished BIG massive refactoring of splitting out extended properties into new AvatarDetail object.

| Method | Original file |
|---|---|
| `GetAllAvatarDetail` | `NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs` |
| `GetAllAvatarDetailAsync` | `NextGenSoftware.OASIS.API.Providers.MongoOASIS/Repositories/AvatarRepository.cs` |

---

## Restoring

```bash
git show <commit>^:<original file> | grep -n -A40 '<MethodName>'
```

Then paste into the matching partial class file. The companion source file already has each body extracted.

## Related

- `REMOVED_AND_MOVED_PROJECTS.md` — project-level removals and relocations (including OPORTAL)
