using Microsoft.VisualStudio.TestTools.UnitTesting;
using NextGenSoftware.OASIS.API.Core.Objects.NativeData;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Models.Data;
using NextGenSoftware.OASIS.API.ONODE.WebAPI.Services.Cql;

namespace NextGenSoftware.OASIS.API.Providers.Neo4jOASIS.IntegrationTests;

public sealed partial class CqlProjectedAllAcceptanceTests
{
    private static async Task VerifyMappedNativeProjectionCompiler(INativeDataProjectionQueryProvider provider,
        VirtualHolonMapping mapping, Guid actor, VirtualHolonRead original)
    {
        var request = new CqlProjectionV1Request
        {
            OrderBy = "Id", VirtualMappingId = mapping.Id, Take = 2,
            Filters = new[] { new CqlV1Filter {
                MetadataPath = new[] { "textAlias" }, Scalar = CqlV1Scalar.String,
                Function = CqlV1Function.Utf16Length, Operator = CqlV1Operator.Equal,
                Value = System.Text.Json.JsonSerializer.SerializeToElement("2")
            } },
            Columns = new[] {
                new CqlProjectionV1Column { Field = "Id", Alias = "Id" },
                new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Original" },
                new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Units",
                    Function = CqlV1Function.Utf16Length, Nullable = true },
                new CqlProjectionV1Column { MetadataPath = new[] { "textAlias" }, ValueType = "String", Alias = "Again",
                    Function = CqlV1Function.Utf16Length, Nullable = true }
            }
        };
        var projection = CqlV1Compiler.CompileVirtualProjection(request);
        var read = await mapping.QueryProjectionAsync(actor, provider, null, projection.Matches, projection.Columns, 2);
        Assert.IsFalse(read.IsError, read.Message + read.Exception);
        var row = read.Result.Single();
        var cells = projection.Select(row);
        Assert.AreEqual(original.Id, row.Id);
        Assert.AreEqual(mapping.Id, row.MappingId);
        Assert.AreEqual(mapping.Version, row.MappingVersion);
        Assert.AreEqual(row.Id, cells[0]);
        Assert.AreEqual("😀", cells[1]);
        Assert.AreEqual(2, cells[2]); Assert.AreEqual(2, cells[3]);
        Assert.AreEqual("😀", original.PrepareSnapshot(actor).MetaData["textAlias"]);
        Assert.IsNull(typeof(VirtualHolonProjectionRead).GetMethod("PrepareSnapshot"));
        var forbidden = await mapping.QueryProjectionAsync(Guid.NewGuid(), provider, null,
            projection.Matches, projection.Columns, 2);
        Assert.AreEqual("VIRTUAL_HOLON_FORBIDDEN", forbidden.ErrorCode);
    }
}
