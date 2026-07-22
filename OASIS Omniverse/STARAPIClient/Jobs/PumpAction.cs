namespace NextGenSoftware.OASIS.STARAPI.Client;

internal sealed class PumpAction
{
    public PumpActionKind Kind;
    public IntPtr DoneCallback;
    public IntPtr DoneUserData;
    public IntPtr AddItemLogCallback;
    public IntPtr AddItemLogUserData;
    public string? ItemName;
    public int Success;
    public string? ErrorMessage;
    public IntPtr LocalItemsPtr;
    public int LocalCount;
    public int[]? SyncedOut;
    public int OpResultCode;
    public int OpType;
}
