"""Send Ctrl+C only to a console containing the exact owned target and this helper."""
import argparse
import ctypes
from ctypes import wintypes
import json
import os


def signal_owned_console(pid):
    if os.name != 'nt' or pid <= 0 or pid == os.getpid():
        raise RuntimeError('An explicit distinct Windows owned-process PID is required')
    kernel = ctypes.WinDLL('kernel32',use_last_error=True)
    kernel.AttachConsole.argtypes=[wintypes.DWORD];kernel.AttachConsole.restype=wintypes.BOOL
    kernel.GetConsoleProcessList.argtypes=[ctypes.POINTER(wintypes.DWORD),wintypes.DWORD];kernel.GetConsoleProcessList.restype=wintypes.DWORD
    kernel.SetConsoleCtrlHandler.argtypes=[ctypes.c_void_p,wintypes.BOOL];kernel.SetConsoleCtrlHandler.restype=wintypes.BOOL
    kernel.GenerateConsoleCtrlEvent.argtypes=[wintypes.DWORD,wintypes.DWORD];kernel.GenerateConsoleCtrlEvent.restype=wintypes.BOOL
    kernel.FreeConsole.argtypes=[];kernel.FreeConsole.restype=wintypes.BOOL
    # The helper may already be attached to an inherited/hidden console.
    # Detach this helper only, before attaching to the owned target's console.
    current=(wintypes.DWORD*16)()
    count=kernel.GetConsoleProcessList(current,len(current))
    if count:
        if not kernel.FreeConsole():raise ctypes.WinError(ctypes.get_last_error())
    elif ctypes.get_last_error() != 6:
        raise ctypes.WinError(ctypes.get_last_error())
    if not kernel.AttachConsole(pid):raise ctypes.WinError(ctypes.get_last_error())
    try:
        members=(wintypes.DWORD*16)()
        count=kernel.GetConsoleProcessList(members,len(members))
        if not count:raise ctypes.WinError(ctypes.get_last_error())
        actual=set(members[:min(count,len(members))])
        if count != 2 or actual != {pid,os.getpid()}:
            raise RuntimeError('Refusing to signal a console with unexpected processes: ' + str(sorted(actual)))
        if not kernel.SetConsoleCtrlHandler(None,True):raise ctypes.WinError(ctypes.get_last_error())
        if not kernel.GenerateConsoleCtrlEvent(0,0):raise ctypes.WinError(ctypes.get_last_error())
        return {'targetPid':pid,'consoleMembers':sorted(actual),'ctrlCSent':True}
    finally:
        if not kernel.FreeConsole():raise ctypes.WinError(ctypes.get_last_error())

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('--pid',type=int,required=True)
    args=parser.parse_args()
    print(json.dumps(signal_owned_console(args.pid)))
