/**
 * Doom - Windows SDL2 sound (OASIS STAR build)
 * Full sound effects via SDL2; music via MUS->MIDI and Windows MCI.
 */
#ifdef _WIN32

#include <stdio.h>
#include <stdlib.h>
#include <stddef.h>
#include <stdarg.h>
#include <string.h>
#include <math.h>
#include <SDL.h>
#include <windows.h>
#include <mmsystem.h>
#include "mus2midi.h"

static FILE* sound_log = NULL;
static void sound_log_msg(const char* fmt, ...)
{
    if (!sound_log) return;
    va_list ap;
    va_start(ap, fmt);
    vfprintf(sound_log, fmt, ap);
    va_end(ap);
    fflush(sound_log);
}

#include "doomdef.h"
#include "doomstat.h"
#include "i_system.h"
#include "i_sound.h"
#include "sounds.h"
#include "w_wad.h"
#include "z_zone.h"

#define SAMPLECOUNT  512
#define NUM_CHANNELS 8
#define BUFMUL       4
#define MIXBUFFERSIZE (SAMPLECOUNT*BUFMUL)
#define SAMPLERATE   11025

int lengths[NUMSFX];
char* sndserver_filename = "";

static SDL_AudioDeviceID sdl_audio_dev = 0;
static signed short mixbuffer[MIXBUFFERSIZE];

static unsigned int channelstep[NUM_CHANNELS];
static unsigned int channelstepremainder[NUM_CHANNELS];
static unsigned char* channels[NUM_CHANNELS];
static unsigned char* channelsend[NUM_CHANNELS];
static int channelstart[NUM_CHANNELS];
static int channelhandles[NUM_CHANNELS];
static int channelids[NUM_CHANNELS];
static int* channelleftvol_lookup[NUM_CHANNELS];
static int* channelrightvol_lookup[NUM_CHANNELS];
static int steptable[256];
static int vol_lookup[128*256];

static void* getsfx(char* sfxname, int* len)
{
    unsigned char* sfx;
    unsigned char* paddedsfx;
    int i, size, paddedsize;
    char name[20];
    int sfxlump;

    sprintf(name, "ds%s", sfxname);
    if (W_CheckNumForName(name) == -1)
        sfxlump = W_GetNumForName("dspistol");
    else
        sfxlump = W_GetNumForName(name);

    size = W_LumpLength(sfxlump);
    paddedsize = ((size - 8 + (SAMPLECOUNT - 1)) / SAMPLECOUNT) * SAMPLECOUNT;

    sfx = (unsigned char*)W_CacheLumpNum(sfxlump, PU_STATIC);
    paddedsfx = (unsigned char*)Z_Malloc(paddedsize + 8, PU_STATIC, 0);
    memcpy(paddedsfx, sfx, size);
    for (i = size; i < paddedsize + 8; i++)
        paddedsfx[i] = 128;
    Z_Free(sfx);

    *len = paddedsize;
    return (void*)(paddedsfx + 8);
}

static int addsfx(int sfxid, int volume, int step, int seperation)
{
    static unsigned short handlenums = 0;
    int i, rc = -1, oldest = gametic, oldestnum = 0, slot;
    int rightvol, leftvol;

    if (sfxid == sfx_sawup || sfxid == sfx_sawidl || sfxid == sfx_sawful ||
        sfxid == sfx_sawhit || sfxid == sfx_stnmov || sfxid == sfx_pistol) {
        for (i = 0; i < NUM_CHANNELS; i++) {
            if (channels[i] && channelids[i] == sfxid) {
                channels[i] = 0;
                break;
            }
        }
    }

    for (i = 0; i < NUM_CHANNELS && channels[i]; i++) {
        if (channelstart[i] < oldest) {
            oldestnum = i;
            oldest = channelstart[i];
        }
    }
    slot = (i == NUM_CHANNELS) ? oldestnum : i;

    channels[slot] = (unsigned char*)S_sfx[sfxid].data;
    channelsend[slot] = channels[slot] + lengths[sfxid];

    if (!handlenums) handlenums = 100;
    channelhandles[slot] = rc = handlenums++;
    channelstep[slot] = step;
    channelstepremainder[slot] = 0;
    channelstart[slot] = gametic;

    seperation += 1;
    leftvol = volume - ((volume * seperation * seperation) >> 16);
    seperation = seperation - 257;
    rightvol = volume - ((volume * seperation * seperation) >> 16);

    if (rightvol < 0 || rightvol > 127) rightvol = (rightvol < 0) ? 0 : 127;
    if (leftvol < 0 || leftvol > 127) leftvol = (leftvol < 0) ? 0 : 127;

    channelleftvol_lookup[slot] = &vol_lookup[leftvol * 256];
    channelrightvol_lookup[slot] = &vol_lookup[rightvol * 256];
    channelids[slot] = sfxid;
    return rc;
}

void I_SetChannels(void)
{
    int i, j;
    int* steptablemid = steptable + 128;
    for (i = -128; i < 128; i++)
        steptablemid[i] = (int)(pow(2.0, (i / 64.0)) * 65536.0);
    for (i = 0; i < 128; i++)
        for (j = 0; j < 256; j++)
            vol_lookup[i * 256 + j] = (i * (j - 128) * 256) / 127;
}

int I_GetSfxLumpNum(sfxinfo_t* sfxinfo)
{
    char namebuf[9];
    sprintf(namebuf, "ds%s", sfxinfo->name);
    return W_GetNumForName(namebuf);
}

int I_StartSound(int id, int vol, int sep, int pitch, int priority)
{
    (void)priority;
    return addsfx(id, vol, steptable[pitch + 128], sep);
}

void I_StopSound(int handle)
{
    int i;
    for (i = 0; i < NUM_CHANNELS; i++)
        if (channelhandles[i] == handle) {
            channels[i] = 0;
            return;
        }
}

int I_SoundIsPlaying(int handle)
{
    int i;
    for (i = 0; i < NUM_CHANNELS; i++)
        if (channelhandles[i] == handle && channels[i])
            return 1;
    return 0;
}

void I_UpdateSoundParams(int handle, int vol, int sep, int pitch)
{
    (void)handle; (void)vol; (void)sep; (void)pitch;
}

void I_UpdateSound(void)
{
    unsigned int sample;
    int dl, dr;
    signed short* leftout = mixbuffer;
    signed short* rightout = mixbuffer + 1;
    signed short* leftend = mixbuffer + SAMPLECOUNT * 2;
    int step = 2;
    int chan;

    while (leftout != leftend) {
        dl = 0;
        dr = 0;
        for (chan = 0; chan < NUM_CHANNELS; chan++) {
            if (channels[chan]) {
                sample = (unsigned int)channels[chan][0];
                dl += channelleftvol_lookup[chan][sample];
                dr += channelrightvol_lookup[chan][sample];
                channelstepremainder[chan] += channelstep[chan];
                channels[chan] += channelstepremainder[chan] >> 16;
                channelstepremainder[chan] &= 65535;
                if (channels[chan] >= channelsend[chan])
                    channels[chan] = 0;
            }
        }
        *leftout = (dl > 0x7fff) ? 0x7fff : (dl < -0x8000) ? (signed short)-0x8000 : (signed short)dl;
        *rightout = (dr > 0x7fff) ? 0x7fff : (dr < -0x8000) ? (signed short)-0x8000 : (signed short)dr;
        leftout += step;
        rightout += step;
    }
}

void I_SubmitSound(void)
{
    if (!sdl_audio_dev) return;
    /* Keep queue filled: queue at least one buffer per frame so playback doesn't starve */
    if (SDL_GetQueuedAudioSize(sdl_audio_dev) < (unsigned)sizeof(mixbuffer) * 8)
        SDL_QueueAudio(sdl_audio_dev, mixbuffer, sizeof(mixbuffer));
}

void I_InitSound(void)
{
    SDL_AudioSpec want, have;
    int i;
    char logpath[MAX_PATH];

    /* Log next to exe so user finds it when running by double-click */
    if (GetModuleFileNameA(NULL, logpath, MAX_PATH) > 0) {
        char* slash = strrchr(logpath, '\\');
        if (slash) {
            slash[1] = '\0';
            strcat(logpath, "doom_sound.log");
        } else
            strcpy(logpath, "doom_sound.log");
    } else
        strcpy(logpath, "doom_sound.log");

    sound_log = fopen(logpath, "w");
    if (sound_log)
        sound_log_msg("I_InitSound: log %s\n", logpath);

    /* Init both VIDEO and AUDIO so drivers have a display context (helps on Windows) */
    if (SDL_Init(SDL_INIT_VIDEO | SDL_INIT_AUDIO) < 0) {
        sound_log_msg("I_InitSound: SDL_Init failed: %s\n", SDL_GetError());
        fprintf(stderr, "I_InitSound: SDL_Init(VIDEO|AUDIO) failed: %s\n", SDL_GetError());
        return;
    }
    sound_log_msg("I_InitSound: SDL_Init OK\n");
    memset(channels, 0, sizeof(channels));
    want.freq = SAMPLERATE;
    want.format = AUDIO_S16SYS;
    want.channels = 2;
    want.samples = SAMPLECOUNT;
    want.callback = NULL;
    want.userdata = NULL;

    sdl_audio_dev = SDL_OpenAudioDevice(NULL, 0, &want, &have, 0);
    if (sdl_audio_dev == 0) {
        sound_log_msg("I_InitSound: SDL_OpenAudioDevice failed: %s\n", SDL_GetError());
        fprintf(stderr, "I_InitSound: SDL_OpenAudioDevice failed: %s\n", SDL_GetError());
        return;
    }
    SDL_PauseAudioDevice(sdl_audio_dev, 0);  /* start playback */
    sound_log_msg("I_InitSound: SDL audio opened %d Hz, %d ch, format %u\n",
        (int)have.freq, (int)have.channels, (unsigned)have.format);
    printf("I_InitSound: SDL audio opened (%d Hz, %d ch)\n", (int)have.freq, (int)have.channels);

    I_InitMusic();

    for (i = 1; i < NUMSFX; i++) {
        if (!S_sfx[i].link) {
            S_sfx[i].data = getsfx(S_sfx[i].name, &lengths[i]);
        } else {
            S_sfx[i].data = S_sfx[i].link->data;
            lengths[i] = lengths[(int)(ptrdiff_t)(S_sfx[i].link - S_sfx)];
        }
    }
    memset(mixbuffer, 0, sizeof(mixbuffer));
}

void I_ShutdownSound(void)
{
    if (sdl_audio_dev) {
        SDL_CloseAudioDevice(sdl_audio_dev);
        sdl_audio_dev = 0;
    }
    if (sound_log) {
        fclose(sound_log);
        sound_log = NULL;
    }
}

/* --- Music: MUS -> MIDI, playback via MCI sequencer --- */
#define MUS_ALIAS "doom_mus"
#define MAX_MUSIC_HANDLES 64
static char* music_paths[MAX_MUSIC_HANDLES];
static int next_music_handle = 1;
static int music_volume = 127;

static void mus_close(void)
{
    mciSendStringA("close " MUS_ALIAS, NULL, 0, NULL);
}

static int mus_get_length_from_header(const unsigned char* data)
{
    if (!data || data[0] != 'M' || data[1] != 'U' || data[2] != 'S' || data[3] != 0x1a)
        return 0;
    return (int)((unsigned)data[4] | ((unsigned)data[5] << 8)) +
           (int)((unsigned)data[6] | ((unsigned)data[7] << 8));
}

void I_InitMusic(void)
{
    memset(music_paths, 0, sizeof(music_paths));
    next_music_handle = 1;
    music_volume = 127;
}

void I_ShutdownMusic(void)
{
    mus_close();
    for (int i = 0; i < MAX_MUSIC_HANDLES; i++) {
        if (music_paths[i]) {
            DeleteFileA(music_paths[i]);
            free(music_paths[i]);
            music_paths[i] = NULL;
        }
    }
}

void I_SetMusicVolume(int volume)
{
    music_volume = (volume < 0) ? 0 : (volume > 127) ? 127 : volume;
    /* Apply when a song is open; I_PlaySong also sets volume */
    {
        char buf[64];
        int v = (music_volume * 1000) / 127;
        sprintf(buf, "setaudio " MUS_ALIAS " volume to %d", v);
        mciSendStringA(buf, NULL, 0, NULL); /* no-op if nothing open */
    }
}

void I_PauseSong(int handle)
{
    (void)handle;
    mciSendStringA("pause " MUS_ALIAS, NULL, 0, NULL);
}

void I_ResumeSong(int handle)
{
    (void)handle;
    mciSendStringA("resume " MUS_ALIAS, NULL, 0, NULL);
}

int I_RegisterSong(void* data)
{
    unsigned char* mus = (unsigned char*)data;
    int len, mid_len;
    unsigned char* mid_buf;
    char path[MAX_PATH];
    char dir[MAX_PATH];
    int handle;

    if (!data) return 0;
    len = mus_get_length_from_header(mus);
    if (len <= 0) return 0;

    mid_len = len;
    mid_buf = mus2midi(mus, &mid_len);
    if (!mid_buf) return 0;

    if (next_music_handle >= MAX_MUSIC_HANDLES)
        next_music_handle = 1;
    handle = next_music_handle++;

    GetTempPathA(MAX_PATH, dir);
    sprintf(path, "%sdoom_mus_%d.mid", dir, handle);

    {
        FILE* f = fopen(path, "wb");
        if (!f) {
            free(mid_buf);
            return 0;
        }
        if (fwrite(mid_buf, 1, (size_t)mid_len, f) != (size_t)mid_len) {
            fclose(f);
            DeleteFileA(path);
            free(mid_buf);
            return 0;
        }
        fclose(f);
    }
    free(mid_buf);

    if (music_paths[handle])
        free(music_paths[handle]);
    music_paths[handle] = (char*)malloc(strlen(path) + 1);
    if (music_paths[handle])
        strcpy(music_paths[handle], path);
    return handle;
}

void I_PlaySong(int handle, int looping)
{
    char buf[512];
    const char* path;

    if (handle <= 0 || handle >= MAX_MUSIC_HANDLES || !music_paths[handle])
        return;
    path = music_paths[handle];

    mus_close();

    sprintf(buf, "open \"%s\" type sequencer alias " MUS_ALIAS, path);
    {
        MCIERROR mcierr = mciSendStringA(buf, NULL, 0, NULL);
        if (mcierr != 0) {
            char errbuf[128];
            mciGetErrorStringA(mcierr, errbuf, sizeof(errbuf));
            sound_log_msg("I_PlaySong: MCI open failed (%u): %s\n", (unsigned)mcierr, errbuf);
            return;
        }
    }

    if (music_volume >= 0) {
        char vbuf[64];
        int v = (music_volume * 1000) / 127;
        sprintf(vbuf, "setaudio " MUS_ALIAS " volume to %d", v);
        mciSendStringA(vbuf, NULL, 0, NULL);
    }

    if (looping)
        mciSendStringA("play " MUS_ALIAS " repeat", NULL, 0, NULL);
    else
        mciSendStringA("play " MUS_ALIAS, NULL, 0, NULL);
}

void I_StopSong(int handle)
{
    (void)handle;
    mus_close();
}

void I_UnRegisterSong(int handle)
{
    if (handle <= 0 || handle >= MAX_MUSIC_HANDLES)
        return;
    mus_close();
    if (music_paths[handle]) {
        DeleteFileA(music_paths[handle]);
        free(music_paths[handle]);
        music_paths[handle] = NULL;
    }
}

#endif /* _WIN32 */
