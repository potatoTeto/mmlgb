#ifndef MUSIC_H
#define MUSIC_H

#include <gbdk/platform.h>

#define WAVE_OFFSET 8U

#define CHN1_OFFSET	0U
#define CHN2_OFFSET	1U
#define CHN3_OFFSET	2U
#define CHN4_OFFSET	3U

void mus_init(UBYTE *song);
void mus_setPaused(UBYTE p);
void mus_togglePaused(void);
void mus_disable1(void);
void mus_disable4(void);
void mus_restore1(void);
void mus_restore4(void);
UBYTE mus_is_done(void);
void mus_update(void);
void mus_update1(void);
void mus_update2(void);
void mus_update3(void);
void mus_update4(void);

#define MUS_HAS_LENGTH 0x80U
#define MUS_FIRST_NOTE T_C

#endif
