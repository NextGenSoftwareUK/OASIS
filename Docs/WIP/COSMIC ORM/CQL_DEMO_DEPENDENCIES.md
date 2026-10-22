# CQL demo dependency inventory

22 October v1 extension: the three-provider walkthrough reuses the existing native integration project, MongoDB/Neo4j services and shared SDK/package cache. Its optional UI links the existing example sources and reuses cached Swashbuckle 6.4.0. No additional runtime, database installation, container image or environment was created. The table below is the original SQLite demo dependency snapshot; native service identities, ownership and lifecycle remain in the [shared resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

21 October 2026. Generated from the demo project assets and actual shared cache after restore. Entries are used by this sample; reused/inherited packages are not claimed as newly installed by this chat. Shared dependencies must not be deleted solely because this demo ends. See [resource ledger](ENVIRONMENT_AND_CLEANUP_LEDGER.md).

| Package | Version | Exact cache path | Logical bytes | Origin at Swagger increment |
|---|---|---|---:|---|
| AutoMapper | 16.2.0 | `C:/Users/david/.nuget/packages/automapper/16.2.0` | 2972052 | Reused shared cache |
| Azure.Core | 1.38.0 | `C:/Users/david/.nuget/packages/azure.core/1.38.0` | 4726100 | Reused shared cache |
| Azure.Identity | 1.11.4 | `C:/Users/david/.nuget/packages/azure.identity/1.11.4` | 902994 | Reused shared cache |
| BCrypt.Net-Next | 4.0.2 | `C:/Users/david/.nuget/packages/bcrypt.net-next/4.0.2` | 886644 | Reused shared cache |
| BetterConsoleTables | 1.1.3 | `C:/Users/david/.nuget/packages/betterconsoletables/1.1.3` | 128306 | Reused shared cache |
| BouncyCastle.Cryptography | 2.6.2 | `C:/Users/david/.nuget/packages/bouncycastle.cryptography/2.6.2` | 28106550 | Reused shared cache |
| Colorful.Console | 1.2.15 | `C:/Users/david/.nuget/packages/colorful.console/1.2.15` | 878759 | Reused shared cache |
| ConsoleTables | 2.6.2 | `C:/Users/david/.nuget/packages/consoletables/2.6.2` | 83697 | Reused shared cache |
| Cryptography.ECDSA.Secp256K1 | 1.1.3 | `C:/Users/david/.nuget/packages/cryptography.ecdsa.secp256k1/1.1.3` | 122236 | Reused shared cache |
| DnsClient | 1.6.1 | `C:/Users/david/.nuget/packages/dnsclient/1.6.1` | 3274959 | Reused shared cache |
| MailKit | 4.17.0 | `C:/Users/david/.nuget/packages/mailkit/4.17.0` | 47059725 | Reused shared cache |
| Microsoft.Bcl.AsyncInterfaces | 1.1.1 | `C:/Users/david/.nuget/packages/microsoft.bcl.asyncinterfaces/1.1.1` | 277777 | Reused shared cache |
| Microsoft.Bcl.Cryptography | 10.0.2 | `C:/Users/david/.nuget/packages/microsoft.bcl.cryptography/10.0.2` | 4485420 | Reused shared cache |
| Microsoft.Data.SqlClient | 5.2.2 | `C:/Users/david/.nuget/packages/microsoft.data.sqlclient/5.2.2` | 66508589 | Reused shared cache |
| Microsoft.Data.SqlClient.SNI.runtime | 5.2.0 | `C:/Users/david/.nuget/packages/microsoft.data.sqlclient.sni.runtime/5.2.0` | 2712672 | Reused shared cache |
| Microsoft.Data.Sqlite.Core | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.data.sqlite.core/10.0.9` | 1106463 | Reused shared cache |
| Microsoft.EntityFrameworkCore | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.entityframeworkcore/10.0.9` | 12806996 | Reused shared cache |
| Microsoft.EntityFrameworkCore.Abstractions | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.entityframeworkcore.abstractions/10.0.9` | 166585 | Reused shared cache |
| Microsoft.EntityFrameworkCore.Analyzers | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.entityframeworkcore.analyzers/10.0.9` | 123615 | Reused shared cache |
| Microsoft.EntityFrameworkCore.Relational | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.entityframeworkcore.relational/10.0.9` | 8105920 | Reused shared cache |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.entityframeworkcore.sqlite/10.0.9` | 59904 | Reused shared cache |
| Microsoft.EntityFrameworkCore.Sqlite.Core | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.entityframeworkcore.sqlite.core/10.0.9` | 1028923 | Reused shared cache |
| Microsoft.Extensions.ApiDescription.Server | 6.0.5 | `C:/Users/david/.nuget/packages/microsoft.extensions.apidescription.server/6.0.5` | 10846129 | Restored/downloaded: absent in pre-Swagger cache inventory |
| Microsoft.Extensions.DependencyModel | 10.0.9 | `C:/Users/david/.nuget/packages/microsoft.extensions.dependencymodel/10.0.9` | 1010488 | Reused shared cache |
| Microsoft.Identity.Client | 4.61.3 | `C:/Users/david/.nuget/packages/microsoft.identity.client/4.61.3` | 17591173 | Reused shared cache |
| Microsoft.Identity.Client.Extensions.Msal | 4.61.3 | `C:/Users/david/.nuget/packages/microsoft.identity.client.extensions.msal/4.61.3` | 357123 | Reused shared cache |
| Microsoft.IdentityModel.Abstractions | 8.19.1 | `C:/Users/david/.nuget/packages/microsoft.identitymodel.abstractions/8.19.1` | 414367 | Reused shared cache |
| Microsoft.IdentityModel.JsonWebTokens | 8.19.1 | `C:/Users/david/.nuget/packages/microsoft.identitymodel.jsonwebtokens/8.19.1` | 2741429 | Reused shared cache |
| Microsoft.IdentityModel.Logging | 8.19.1 | `C:/Users/david/.nuget/packages/microsoft.identitymodel.logging/8.19.1` | 839276 | Reused shared cache |
| Microsoft.IdentityModel.Protocols | 8.19.1 | `C:/Users/david/.nuget/packages/microsoft.identitymodel.protocols/8.19.1` | 987148 | Reused shared cache |
| Microsoft.IdentityModel.Protocols.OpenIdConnect | 8.19.1 | `C:/Users/david/.nuget/packages/microsoft.identitymodel.protocols.openidconnect/8.19.1` | 2224472 | Reused shared cache |
| Microsoft.IdentityModel.Tokens | 8.19.1 | `C:/Users/david/.nuget/packages/microsoft.identitymodel.tokens/8.19.1` | 8838389 | Reused shared cache |
| Microsoft.NETCore.Platforms | 1.0.1 | `C:/Users/david/.nuget/packages/microsoft.netcore.platforms/1.0.1` | 29712 | Reused shared cache |
| Microsoft.OpenApi | 1.2.3 | `C:/Users/david/.nuget/packages/microsoft.openapi/1.2.3` | 1339920 | Restored/downloaded: absent in pre-Swagger cache inventory |
| Microsoft.SqlServer.Server | 1.0.0 | `C:/Users/david/.nuget/packages/microsoft.sqlserver.server/1.0.0` | 205899 | Reused shared cache |
| MimeKit | 4.17.0 | `C:/Users/david/.nuget/packages/mimekit/4.17.0` | 47623578 | Reused shared cache |
| MongoDB.Bson | 3.12.0 | `C:/Users/david/.nuget/packages/mongodb.bson/3.12.0` | 6185534 | Reused shared cache |
| MongoDB.Driver | 3.12.0 | `C:/Users/david/.nuget/packages/mongodb.driver/3.12.0` | 19293881 | Reused shared cache |
| NBitcoin | 9.0.3 | `C:/Users/david/.nuget/packages/nbitcoin/9.0.3` | 11125957 | Reused shared cache |
| NETStandard.Library | 1.6.0 | `C:/Users/david/.nuget/packages/netstandard.library/1.6.0` | 29564 | Reused shared cache |
| Newtonsoft.Json | 13.0.1 | `C:/Users/david/.nuget/packages/newtonsoft.json/13.0.1` | 11036229 | Reused shared cache |
| NLog | 4.7.9 | `C:/Users/david/.nuget/packages/nlog/4.7.9` | 29191031 | Reused shared cache |
| OpenTelemetry | 1.19.1 | `C:/Users/david/.nuget/packages/opentelemetry/1.19.1` | 4309257 | Reused shared cache |
| OpenTelemetry.Api | 1.19.1 | `C:/Users/david/.nuget/packages/opentelemetry.api/1.19.1` | 1381483 | Reused shared cache |
| OpenTelemetry.Api.ProviderBuilderExtensions | 1.19.1 | `C:/Users/david/.nuget/packages/opentelemetry.api.providerbuilderextensions/1.19.1` | 567624 | Reused shared cache |
| OpenTelemetry.Exporter.OpenTelemetryProtocol | 1.19.1 | `C:/Users/david/.nuget/packages/opentelemetry.exporter.opentelemetryprotocol/1.19.1` | 2617300 | Reused shared cache |
| OpenTelemetry.Extensions.Hosting | 1.19.1 | `C:/Users/david/.nuget/packages/opentelemetry.extensions.hosting/1.19.1` | 340452 | Reused shared cache |
| Portable.BouncyCastle | 1.9.0 | `C:/Users/david/.nuget/packages/portable.bouncycastle/1.9.0` | 13855240 | Reused shared cache |
| Resend | 0.5.1 | `C:/Users/david/.nuget/packages/resend/0.5.1` | 404314 | Reused shared cache |
| Rijndael256.Core | 3.2.5 | `C:/Users/david/.nuget/packages/rijndael256.core/3.2.5` | 148159 | Reused shared cache |
| SharpCompress | 0.48.1 | `C:/Users/david/.nuget/packages/sharpcompress/0.48.1` | 29996730 | Reused shared cache |
| ShellProgressBar | 5.2.0 | `C:/Users/david/.nuget/packages/shellprogressbar/5.2.0` | 160268 | Reused shared cache |
| Snappier | 1.3.1 | `C:/Users/david/.nuget/packages/snappier/1.3.1` | 439340 | Reused shared cache |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.11 | `C:/Users/david/.nuget/packages/sqlitepclraw.bundle_e_sqlite3/2.1.11` | 107707 | Reused shared cache |
| SQLitePCLRaw.core | 2.1.11 | `C:/Users/david/.nuget/packages/sqlitepclraw.core/2.1.11` | 101817 | Reused shared cache |
| SQLitePCLRaw.lib.e_sqlite3 | 2.1.12 | `C:/Users/david/.nuget/packages/sqlitepclraw.lib.e_sqlite3/2.1.12` | 56777567 | Reused shared cache |
| SQLitePCLRaw.provider.e_sqlite3 | 2.1.11 | `C:/Users/david/.nuget/packages/sqlitepclraw.provider.e_sqlite3/2.1.11` | 189613 | Reused shared cache |
| SshNet.Security.Cryptography | 1.3.0 | `C:/Users/david/.nuget/packages/sshnet.security.cryptography/1.3.0` | 1408229 | Reused shared cache |
| Swashbuckle.AspNetCore | 6.4.0 | `C:/Users/david/.nuget/packages/swashbuckle.aspnetcore/6.4.0` | 23206 | Restored/downloaded: absent in pre-Swagger cache inventory |
| Swashbuckle.AspNetCore.Swagger | 6.4.0 | `C:/Users/david/.nuget/packages/swashbuckle.aspnetcore.swagger/6.4.0` | 245494 | Restored/downloaded: absent in pre-Swagger cache inventory |
| Swashbuckle.AspNetCore.SwaggerGen | 6.4.0 | `C:/Users/david/.nuget/packages/swashbuckle.aspnetcore.swaggergen/6.4.0` | 936878 | Restored/downloaded: absent in pre-Swagger cache inventory |
| Swashbuckle.AspNetCore.SwaggerUI | 6.4.0 | `C:/Users/david/.nuget/packages/swashbuckle.aspnetcore.swaggerui/6.4.0` | 17997891 | Restored/downloaded: absent in pre-Swagger cache inventory |
| System.ClientModel | 1.0.0 | `C:/Users/david/.nuget/packages/system.clientmodel/1.0.0` | 225086 | Reused shared cache |
| System.Configuration.ConfigurationManager | 8.0.0 | `C:/Users/david/.nuget/packages/system.configuration.configurationmanager/8.0.0` | 5264849 | Reused shared cache |
| System.IdentityModel.Tokens.Jwt | 8.19.1 | `C:/Users/david/.nuget/packages/system.identitymodel.tokens.jwt/8.19.1` | 2074976 | Reused shared cache |
| System.Memory.Data | 1.0.2 | `C:/Users/david/.nuget/packages/system.memory.data/1.0.2` | 147676 | Reused shared cache |
| System.Runtime.Caching | 8.0.0 | `C:/Users/david/.nuget/packages/system.runtime.caching/8.0.0` | 1477384 | Reused shared cache |
| System.Security.Cryptography.Pkcs | 10.0.13 | `C:/Users/david/.nuget/packages/system.security.cryptography.pkcs/10.0.13` | 5235925 | Reused shared cache |
| System.Security.Cryptography.ProtectedData | 8.0.0 | `C:/Users/david/.nuget/packages/system.security.cryptography.protecteddata/8.0.0` | 450834 | Reused shared cache |
| Wcwidth | 1.0.0 | `C:/Users/david/.nuget/packages/wcwidth/1.0.0` | 162440 | Reused shared cache |
| ZstdSharp.Port | 0.7.3 | `C:/Users/david/.nuget/packages/zstdsharp.port/0.7.3` | 3944538 | Reused shared cache |
