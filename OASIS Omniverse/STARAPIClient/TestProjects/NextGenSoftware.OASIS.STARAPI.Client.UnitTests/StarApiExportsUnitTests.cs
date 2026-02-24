using System.Runtime.InteropServices;
using System.Text;
using NextGenSoftware.OASIS.STARAPI.Client;

namespace NextGenSoftware.OASIS.STARAPI.Client.UnitTests;

public unsafe class StarApiExportsUnitTests
{
    [Fact]
    public void StarApiInit_NullConfig_ReturnsInvalidParam_AndSetsError()
    {
        delegate* unmanaged[Cdecl]<star_api_config_t*, int> initPtr = &StarApiExports.StarApiInit;
        delegate* unmanaged[Cdecl]<sbyte*> getLastErrorPtr = &StarApiExports.StarApiGetLastError;

        var code = initPtr(null);
        var err = PtrToString(getLastErrorPtr());

        Assert.Equal((int)StarApiResultCode.InvalidParam, code);
        Assert.Contains("Invalid configuration", err ?? string.Empty, StringComparison.OrdinalIgnoreCase);

        // Ensure no state leak between tests.
        delegate* unmanaged[Cdecl]<void> cleanupPtr = &StarApiExports.StarApiCleanup;
        cleanupPtr();
    }

    [Fact]
    public void StarApiSetOasisBaseUrl_BeforeInit_ReturnsNotInitialized()
    {
        delegate* unmanaged[Cdecl]<sbyte*, int> setWeb4Ptr = &StarApiExports.StarApiSetOasisBaseUrl;
        delegate* unmanaged[Cdecl]<sbyte*> getLastErrorPtr = &StarApiExports.StarApiGetLastError;
        delegate* unmanaged[Cdecl]<void> cleanupPtr = &StarApiExports.StarApiCleanup;

        var web4 = StringToNativeUtf8("https://web4.example.com");
        try
        {
            cleanupPtr();
            var code = setWeb4Ptr(web4);
            var err = PtrToString(getLastErrorPtr());

            Assert.Equal((int)StarApiResultCode.NotInitialized, code);
            Assert.Contains("not initialized", err ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            NativeMemory.Free(web4);
            cleanupPtr();
        }
    }

    [Fact]
    public void StarApiGetInventory_NullOutPointer_ReturnsInvalidParam()
    {
        delegate* unmanaged[Cdecl]<star_item_list_t**, int> getInventoryPtr = &StarApiExports.StarApiGetInventory;
        delegate* unmanaged[Cdecl]<sbyte*> getLastErrorPtr = &StarApiExports.StarApiGetLastError;

        var code = getInventoryPtr(null);
        var err = PtrToString(getLastErrorPtr());

        Assert.Equal((int)StarApiResultCode.InvalidParam, code);
        Assert.Contains("itemList", err ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static sbyte* StringToNativeUtf8(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var ptr = (byte*)NativeMemory.Alloc((nuint)bytes.Length + 1);
        bytes.CopyTo(new Span<byte>(ptr, bytes.Length));
        ptr[bytes.Length] = 0;
        return (sbyte*)ptr;
    }

    private static string? PtrToString(sbyte* value)
    {
        return value is null ? null : Marshal.PtrToStringUTF8((nint)value);
    }
}

