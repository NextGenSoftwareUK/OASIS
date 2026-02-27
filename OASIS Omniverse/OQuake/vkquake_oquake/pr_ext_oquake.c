/**
 * OQuake extension builtins
 *
 * Provides PF_ wrappers for OQuake STAR API so QuakeC can call
 * OQuake_OnKeyPickup, OQuake_CheckDoorAccess, OQuake_OnBossKilled.
 * Add this file to the OQuake build and register these builtins in pr_ext.c's extension table.
 *
 * QuakeC (defs.qc):
 *   void(string keyname) OQuake_OnKeyPickup = #0:ex_OQuake_OnKeyPickup;
 *   float(string doorname, string requiredkey) OQuake_CheckDoorAccess = #0:ex_OQuake_CheckDoorAccess;
 *   void(string bossname) OQuake_OnBossKilled = #0:ex_OQuake_OnBossKilled;
 */

#include "quakedef.h"
#include "oquake_star_integration.h"

/* OQuake builtin: void(string keyname) - report key pickup to STAR */
void PF_OQuake_OnKeyPickup (void)
{
	const char *keyname = G_STRING (OFS_PARM0);
	if (keyname)
		OQuake_STAR_OnKeyPickup (keyname);
}

/* OQuake builtin: float(string doorname, string requiredkey) - 1 if door can open, 0 otherwise */
void PF_OQuake_CheckDoorAccess (void)
{
	const char *doorname = G_STRING (OFS_PARM0);
	const char *requiredkey = G_STRING (OFS_PARM1);
	float r = 0.0f;
	if (requiredkey)
		r = (float) OQuake_STAR_CheckDoorAccess (doorname ? doorname : "", requiredkey);
	G_FLOAT (OFS_RETURN) = r;
}

/* OQuake builtin: void(string bossname) - report boss kill to STAR (mint boss NFT) */
void PF_OQuake_OnBossKilled (void)
{
	const char *bossname = G_STRING (OFS_PARM0);
	if (bossname)
		OQuake_STAR_OnBossKilled (bossname);
}
