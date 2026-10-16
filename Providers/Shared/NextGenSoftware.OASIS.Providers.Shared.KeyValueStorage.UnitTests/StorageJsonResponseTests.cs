using System;
using System.IO;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NextGenSoftware.OASIS.Providers.Shared.KeyValueStorage.UnitTests;

[TestClass]
public class StorageJsonResponseTests
{
    [TestMethod]
    public void GetPreservesExplicitAbsenceEmptyAndOpaqueValues()
    {
        using var missing=JsonDocument.Parse("null");Assert.IsNull(StorageJsonResponse.NullableString(missing.RootElement));
        foreach(var value in new[] {"","opaque encrypted value"})
        {using var document=JsonDocument.Parse(JsonSerializer.Serialize(value));Assert.AreEqual(value,StorageJsonResponse.NullableString(document.RootElement));}
    }
    [DataTestMethod]
    [DataRow("{}")] [DataRow("[]")] [DataRow("true")] [DataRow("12")]
    public void WrongGetShapeIsTerminalDataFailure(string json)
    {using var document=JsonDocument.Parse(json);Assert.ThrowsException<InvalidDataException>(()=>StorageJsonResponse.NullableString(document.RootElement));}
    [TestMethod]
    public void UndefinedGetIsNotAbsence()
        =>Assert.ThrowsException<InvalidDataException>(()=>StorageJsonResponse.NullableString(default));
    [DataTestMethod]
    [DataRow("null")] [DataRow("{}")] [DataRow("[null]")] [DataRow("[12]")]
    [DataRow("[\"p/a\",\"p/a\"]")] [DataRow("[\"p/b\",\"p/a\"]")] [DataRow("[\"other\"]")]
    public void WrongListShapeAndOrderAreTerminal(string json)
    {using var document=JsonDocument.Parse(json);Assert.ThrowsException<InvalidDataException>(()=>StorageJsonResponse.KeyPage(document.RootElement,"p/"));}
    [TestMethod]
    public void PageValidatesPriorCursorAndReturnsNoPartialList()
    {
        using var valid=JsonDocument.Parse("[\"p/b\",\"p/c\"]");
        CollectionAssert.AreEqual(new[] {"p/b","p/c"},System.Linq.Enumerable.ToArray(StorageJsonResponse.KeyPage(valid.RootElement,"p/","p/a")));
        Assert.ThrowsException<InvalidDataException>(()=>StorageJsonResponse.KeyPage(valid.RootElement,"p/","p/b"));
        using var empty=JsonDocument.Parse("[]");Assert.AreEqual(0,StorageJsonResponse.KeyPage(empty.RootElement,"p/","p/c").Count);
    }
}
