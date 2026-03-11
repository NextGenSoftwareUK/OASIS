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

    [Fact]
    public void StarApiConsumeLastMintResult_WhenNoMint_ReturnsZero()
    {
        delegate* unmanaged[Cdecl]<void> cleanupPtr = &StarApiExports.StarApiCleanup;
        delegate* unmanaged[Cdecl]<star_api_config_t*, int> initPtr = &StarApiExports.StarApiInit;
        delegate* unmanaged[Cdecl]<sbyte*, nuint, sbyte*, nuint, sbyte*, nuint, int> consumePtr = &StarApiExports.StarApiConsumeLastMintResult;

        cleanupPtr();
        var baseUrlPtr = StringToNativeUtf8("https://web5.example.com/api");
        try
        {
            var config = new star_api_config_t { base_url = baseUrlPtr, timeout_seconds = 30 };
            initPtr(&config);

            const int bufSize = 128;
            var itemBuf = (sbyte*)NativeMemory.AllocZeroed(bufSize);
            var nftBuf = (sbyte*)NativeMemory.AllocZeroed(bufSize);
            var hashBuf = (sbyte*)NativeMemory.AllocZeroed(bufSize);
            try
            {
                var code = consumePtr(itemBuf, bufSize, nftBuf, bufSize, hashBuf, bufSize);
                Assert.Equal(0, code);
            }
            finally
            {
                NativeMemory.Free(itemBuf);
                NativeMemory.Free(nftBuf);
                NativeMemory.Free(hashBuf);
                cleanupPtr();
            }
        }
        finally
        {
            NativeMemory.Free((void*)baseUrlPtr);
        }
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

