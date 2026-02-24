/**
 * Doom - SDL2 video layer for Windows (OASIS STAR integration build)
 * Implements i_video.h using SDL2. Mouse: grab + relative + hide cursor when not in menu for 360 turn.
 */
#ifdef _WIN32

#include <stdlib.h>
#include <stdio.h>
#include <string.h>
#include <SDL.h>

#include "doomdef.h"
#include "doomstat.h"
#include "doomtype.h"
#include "i_system.h"
#include "odoom_version.h"
#include "i_video.h"
#include "m_argv.h"
#include "d_main.h"
#include "v_video.h"

extern int usegamma;
extern boolean menuactive;
extern byte gammatable[5][256];

static SDL_Window*   sdl_window = NULL;
static SDL_Renderer* sdl_renderer = NULL;
static SDL_Texture*  sdl_texture = NULL;
static SDL_Palette*  sdl_palette = NULL;
static Uint32*       sdl_rgb_buf = NULL;
static int sdl_multiply = 2;
static boolean grabMouse = false;
static boolean sdl_fullscreen = 1;
static int doPointerWarp = 1;
#define POINTER_WARP_COUNTDOWN 1

static int xlatekey(SDL_Keysym sym)
{
    switch (sym.sym) {
        case SDLK_LEFT:   return KEY_LEFTARROW;
        case SDLK_RIGHT:  return KEY_RIGHTARROW;
        case SDLK_DOWN:   return KEY_DOWNARROW;
        case SDLK_UP:     return KEY_UPARROW;
        case SDLK_ESCAPE: return KEY_ESCAPE;
        case SDLK_RETURN: return KEY_ENTER;
        case SDLK_TAB:    return KEY_TAB;
        case SDLK_F1:     return KEY_F1;
        case SDLK_F2:     return KEY_F2;
        case SDLK_F3:     return KEY_F3;
        case SDLK_F4:     return KEY_F4;
        case SDLK_F5:     return KEY_F5;
        case SDLK_F6:     return KEY_F6;
        case SDLK_F7:     return KEY_F7;
        case SDLK_F8:     return KEY_F8;
        case SDLK_F9:     return KEY_F9;
        case SDLK_F10:    return KEY_F10;
        case SDLK_F11:    return KEY_F11;
        case SDLK_F12:    return KEY_F12;
        case SDLK_BACKSPACE: return KEY_BACKSPACE;
        case SDLK_PAUSE:  return KEY_PAUSE;
        case SDLK_EQUALS: return KEY_EQUALS;
        case SDLK_MINUS:  return KEY_MINUS;
        case SDLK_RSHIFT: return KEY_RSHIFT;
        case SDLK_RCTRL:  return KEY_RCTRL;
        case SDLK_RALT:   return KEY_RALT;
        default:
            if (sym.sym >= 32 && sym.sym < 127)
                return (int)sym.sym;
            return 0;
    }
}

static void I_GetEvent(void)
{
    SDL_Event e;
    event_t event;

    while (SDL_PollEvent(&e)) {
        switch (e.type) {
            case SDL_QUIT:
                I_Quit();
                break;
            case SDL_KEYDOWN:
                if ((e.key.keysym.sym == SDLK_RETURN || e.key.keysym.sym == SDLK_KP_ENTER) &&
                    (e.key.keysym.mod & KMOD_ALT)) {
                    sdl_fullscreen = !sdl_fullscreen;
                    SDL_SetWindowFullscreen(sdl_window, sdl_fullscreen ? SDL_WINDOW_FULLSCREEN_DESKTOP : 0);
                    if (sdl_fullscreen) {
                        int ww, hh;
                        SDL_GetWindowSize(sdl_window, &ww, &hh);
                        SDL_RenderSetLogicalSize(sdl_renderer, ww, hh);
                    } else {
                        SDL_RenderSetLogicalSize(sdl_renderer, SCREENWIDTH * sdl_multiply, SCREENHEIGHT * sdl_multiply);
                    }
                    break;
                }
                event.type = ev_keydown;
                event.data1 = xlatekey(e.key.keysym);
                if (event.data1) D_PostEvent(&event);
                break;
            case SDL_KEYUP:
                event.type = ev_keyup;
                event.data1 = xlatekey(e.key.keysym);
                if (event.data1) D_PostEvent(&event);
                break;
            case SDL_MOUSEBUTTONDOWN: {
                event.type = ev_mouse;
                event.data1 = (e.button.button == SDL_BUTTON_LEFT ? 1 : 0)
                    | (e.button.button == SDL_BUTTON_RIGHT ? 2 : 0)
                    | (e.button.button == SDL_BUTTON_MIDDLE ? 4 : 0);
                event.data2 = event.data3 = 0;
                D_PostEvent(&event);
                break;
            }
            case SDL_MOUSEBUTTONUP: {
                event.type = ev_mouse;
                event.data1 = 0;
                event.data2 = event.data3 = 0;
                D_PostEvent(&event);
                break;
            }
            case SDL_MOUSEMOTION: {
                event.type = ev_mouse;
                event.data1 = (e.motion.state & SDL_BUTTON_LMASK ? 1 : 0)
                    | (e.motion.state & SDL_BUTTON_RMASK ? 2 : 0)
                    | (e.motion.state & SDL_BUTTON_MMASK ? 4 : 0);
                event.data2 = e.motion.xrel << 2;
                event.data3 = -e.motion.yrel << 2;
                if (event.data2 || event.data3) D_PostEvent(&event);
                break;
            }
            default:
                break;
        }
    }
}

void I_ShutdownGraphics(void)
{
    if (sdl_rgb_buf) { free(sdl_rgb_buf); sdl_rgb_buf = NULL; }
    if (sdl_palette) { SDL_FreePalette(sdl_palette); sdl_palette = NULL; }
    if (sdl_texture) { SDL_DestroyTexture(sdl_texture); sdl_texture = NULL; }
    if (sdl_renderer) { SDL_DestroyRenderer(sdl_renderer); sdl_renderer = NULL; }
    if (sdl_window) { SDL_DestroyWindow(sdl_window); sdl_window = NULL; }
    SDL_QuitSubSystem(SDL_INIT_VIDEO);
}

void I_StartFrame(void) { }

void I_SetMouseCapture(boolean in_menu)
{
    if (!sdl_window) return;
    if (in_menu) {
        SDL_SetWindowGrab(sdl_window, SDL_FALSE);
        SDL_SetRelativeMouseMode(SDL_FALSE);
        SDL_ShowCursor(SDL_ENABLE);
    } else {
        SDL_SetWindowGrab(sdl_window, SDL_TRUE);
        SDL_SetRelativeMouseMode(SDL_TRUE);
        SDL_ShowCursor(0);
    }
}

void I_StartTic(void)
{
    if (!sdl_window) return;
    if (menuactive) {
        SDL_SetWindowGrab(sdl_window, SDL_FALSE);
        SDL_SetRelativeMouseMode(SDL_FALSE);
        SDL_ShowCursor(SDL_ENABLE);
    } else {
        SDL_SetWindowGrab(sdl_window, SDL_TRUE);
        SDL_SetRelativeMouseMode(SDL_TRUE);
        SDL_ShowCursor(0);
    }
    I_GetEvent();
}

void I_UpdateNoBlit(void) { }

void I_FinishUpdate(void)
{
    static int lasttic;
    int tics, i;
    int n = SCREENWIDTH * SCREENHEIGHT;

    if (devparm) {
        int now = I_GetTime();
        tics = now - lasttic;
        lasttic = now;
        if (tics > 20) tics = 20;
        for (i = 0; i < tics*2; i += 2)
            screens[0][(SCREENHEIGHT-1)*SCREENWIDTH + i] = 0xff;
        for (; i < 20*2; i += 2)
            screens[0][(SCREENHEIGHT-1)*SCREENWIDTH + i] = 0x0;
    }

    if (sdl_texture && sdl_rgb_buf && sdl_palette && screens[0]) {
        byte* src = screens[0];
        for (i = 0; i < n; i++) {
            int idx = src[i];
            SDL_Color* c = &sdl_palette->colors[idx];
            sdl_rgb_buf[i] = (c->r << 16) | (c->g << 8) | c->b;
        }
        SDL_UpdateTexture(sdl_texture, NULL, sdl_rgb_buf, SCREENWIDTH * 4);
        SDL_RenderClear(sdl_renderer);
        SDL_RenderCopy(sdl_renderer, sdl_texture, NULL, NULL);
        SDL_RenderPresent(sdl_renderer);
    }
}

void I_ReadScreen(byte* scr)
{
    if (screens[0]) memcpy(scr, screens[0], SCREENWIDTH * SCREENHEIGHT);
}

void I_SetPalette(byte* palette)
{
    int i;
    byte* p = palette;
    if (!sdl_palette) return;
    for (i = 0; i < 256; i++) {
        int c = gammatable[usegamma][*p++];
        sdl_palette->colors[i].r = (c<<8)+c;
        c = gammatable[usegamma][*p++];
        sdl_palette->colors[i].g = (c<<8)+c;
        c = gammatable[usegamma][*p++];
        sdl_palette->colors[i].b = (c<<8)+c;
        sdl_palette->colors[i].a = 255;
    }
}

void I_WaitVBL(int count)
{
    SDL_Delay(count * 10);
}

void I_BeginRead(void) { }
void I_EndRead(void) { }

void I_InitGraphics(void)
{
    static int firsttime = 1;
    int w, h, i;

    if (!firsttime) return;
    firsttime = 0;

    if (M_CheckParm("-2")) sdl_multiply = 2;
    if (M_CheckParm("-3")) sdl_multiply = 3;
    if (M_CheckParm("-4")) sdl_multiply = 4;
    if (M_CheckParm("-window")) sdl_fullscreen = 0;
    grabMouse = !!M_CheckParm("-grabmouse");

    w = SCREENWIDTH * sdl_multiply;
    h = SCREENHEIGHT * sdl_multiply;

    if (SDL_InitSubSystem(SDL_INIT_VIDEO) < 0) {
        I_Error("SDL_InitSubSystem(VIDEO) failed: %s", SDL_GetError());
    }

    {
        Uint32 winflags = SDL_WINDOW_SHOWN;
        if (sdl_fullscreen)
            winflags |= SDL_WINDOW_FULLSCREEN_DESKTOP;
        sdl_window = SDL_CreateWindow(ODOOM_TITLE,
            SDL_WINDOWPOS_CENTERED, SDL_WINDOWPOS_CENTERED,
            w, h, winflags);
    }
    if (!sdl_window) {
        I_Error("SDL_CreateWindow failed: %s", SDL_GetError());
    }

    if (sdl_fullscreen) {
        SDL_GetWindowSize(sdl_window, &w, &h);
    }

    SDL_SetWindowGrab(sdl_window, SDL_TRUE);
    SDL_SetRelativeMouseMode(SDL_TRUE);
    SDL_ShowCursor(SDL_DISABLE);

    sdl_renderer = SDL_CreateRenderer(sdl_window, -1,
        SDL_RENDERER_ACCELERATED | SDL_RENDERER_PRESENTVSYNC);
    if (!sdl_renderer) {
        sdl_renderer = SDL_CreateRenderer(sdl_window, -1, 0);
    }
    if (!sdl_renderer) {
        I_Error("SDL_CreateRenderer failed: %s", SDL_GetError());
    }

    sdl_palette = SDL_AllocPalette(256);
    if (!sdl_palette) {
        I_Error("SDL_AllocPalette failed: %s", SDL_GetError());
    }
    for (i = 0; i < 256; i++) {
        sdl_palette->colors[i].r = sdl_palette->colors[i].g = sdl_palette->colors[i].b = 0;
        sdl_palette->colors[i].a = 255;
    }

    sdl_rgb_buf = (Uint32*)malloc(SCREENWIDTH * SCREENHEIGHT * sizeof(Uint32));
    if (!sdl_rgb_buf) {
        I_Error("malloc sdl_rgb_buf failed");
    }

    sdl_texture = SDL_CreateTexture(sdl_renderer, SDL_PIXELFORMAT_RGB888,
        SDL_TEXTUREACCESS_STREAMING, SCREENWIDTH, SCREENHEIGHT);
    if (!sdl_texture) {
        I_Error("SDL_CreateTexture failed: %s", SDL_GetError());
    }
    SDL_SetTextureScaleMode(sdl_texture, SDL_ScaleModeNearest);

    SDL_RenderSetLogicalSize(sdl_renderer, w, h);
}

#endif
