#!/usr/bin/env python3
"""Portable entry point for the existing adjacent provider producer/consumer lease.

Uses the same path and exclusive native lock as Invoke-WithProviderBuildLease.ps1:
Win32 FileShare.None on Windows, flock(LOCK_EX|LOCK_NB) on Unix. Busy means stop.
"""
import argparse
import contextlib
import ctypes
import datetime
import json
import os
from pathlib import Path
import subprocess
import sys

@contextlib.contextmanager
def acquire(workspace, owner):
    workspace=Path(workspace).resolve()
    directory=workspace.parent / '.oasis-provider-build'
    directory.mkdir(parents=True,exist_ok=True)
    lease=directory / 'producer-consumer.lock'
    if os.name=='nt':
        import msvcrt
        from ctypes import wintypes
        kernel=ctypes.WinDLL('kernel32',use_last_error=True)
        create=kernel.CreateFileW
        create.argtypes=[wintypes.LPCWSTR,wintypes.DWORD,wintypes.DWORD,ctypes.c_void_p,
                         wintypes.DWORD,wintypes.DWORD,wintypes.HANDLE]
        create.restype=wintypes.HANDLE
        kernel.CloseHandle.argtypes=[wintypes.HANDLE]
        kernel.CloseHandle.restype=wintypes.BOOL
        handle=create(str(lease),0xC0000000,0,None,4,128,None)
        if handle==ctypes.c_void_p(-1).value:
            raise ctypes.WinError(ctypes.get_last_error())
        try:
            fd=msvcrt.open_osfhandle(handle,os.O_RDWR|os.O_BINARY)
        except BaseException:
            kernel.CloseHandle(handle)
            raise
    else:
        import fcntl
        fd=os.open(lease,os.O_RDWR|os.O_CREAT,0o600)
        try:
            fcntl.flock(fd,fcntl.LOCK_EX|fcntl.LOCK_NB)
        except BaseException:
            os.close(fd)
            raise
    with os.fdopen(fd,'r+b',buffering=0) as stream:
        record={'Owner':owner,'ProcessId':os.getpid(),
                'StartedUtc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
                'Workspace':str(workspace)}
        stream.truncate(0)
        stream.write(json.dumps(record,separators=(',',':')).encode('utf-8'))
        stream.flush()
        yield

def main(argv=None):
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--owner',required=True)
    parser.add_argument('--workspace',type=Path,default=Path(__file__).resolve().parents[2])
    parser.add_argument('command',nargs=argparse.REMAINDER)
    args=parser.parse_args(argv)
    command=args.command[1:] if args.command[:1]==['--'] else args.command
    if not command or not args.owner.strip():
        parser.error('A named owner and a command are required.')
    acquired=False
    try:
        with acquire(args.workspace,args.owner):
            acquired=True
            return subprocess.run(command,cwd=args.workspace,check=False).returncode
    except OSError:
        if acquired:
            print('Provider build command could not start.',file=sys.stderr)
            return 127
        print('Provider build lease busy or unavailable; operation not started.',file=sys.stderr)
        return 75

if __name__=='__main__':
    sys.exit(main())
