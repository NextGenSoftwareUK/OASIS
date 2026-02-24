// Emacs style mode select   -*- C++ -*- 
//-----------------------------------------------------------------------------
//
// $Id:$
//
// Copyright (C) 1993-1996 by id Software, Inc.
//
// This source is available for distribution and/or modification
// only under the terms of the DOOM Source Code License as
// published by id Software, Inc. All rights reserved.
//
// DESCRIPTION:
//	Main program, simply calls D_DoomMain high level loop.
//	Windows: attach/alloc console so user sees printf (sound init, etc.).
//
//-----------------------------------------------------------------------------

#ifdef _WIN32
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <stdio.h>
#endif

#include "doomdef.h"
#include "m_argv.h"
#include "d_main.h"

int main(int argc, char** argv) 
{ 
#ifdef _WIN32
    {
        /* Show console so user sees messages (e.g. sound init). Prefer parent console if run from cmd. */
        int have = AttachConsole(ATTACH_PARENT_PROCESS);
        if (!have) have = AllocConsole();
        if (have) {
            (void)freopen("CONIN$", "r", stdin);
            (void)freopen("CONOUT$", "w", stdout);
            (void)freopen("CONOUT$", "w", stderr);
            setvbuf(stdout, NULL, _IONBF, 0);
            setvbuf(stderr, NULL, _IONBF, 0);
        }
    }
#endif
    myargc = argc; 
    myargv = argv; 
    D_DoomMain(); 
    return 0;
}
