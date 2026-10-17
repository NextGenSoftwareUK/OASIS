param([Parameter(Mandatory=$true)][string]$QuakeDir)
$ErrorActionPreference = 'Stop'
$path = Join-Path $QuakeDir 'menu.c'
$menu = [IO.File]::ReadAllText($path)
if ($menu -notmatch '#include "oquake_ogengine_integration.h"') {
    if ($menu -notmatch '#include "quakedef.h"') { throw 'Quake menu include anchor changed.' }
    $menu = $menu.Replace('#include "quakedef.h"', "#include `"quakedef.h`"`n#include `"oquake_ogengine_integration.h`"")
}
if ($menu -notmatch 'GAME_OPT_OFFLINE_SYNC,') {
    if ($menu -notmatch 'GAME_OPT_SHOWFPS,') { throw 'Quake game options enum changed.' }
    $menu = $menu.Replace('GAME_OPT_SHOWFPS,', "GAME_OPT_SHOWFPS,`n`tGAME_OPT_OFFLINE_SYNC,`n`tGAME_OPT_OFFLINE_DRAIN,")
}
# Named blocks let subsequent builds replace this integration without duplicating menu rows.
$menu = [regex]::Replace($menu, '(?s)\s*// OASIS_EDGE_ADJUST_BEGIN.*?// OASIS_EDGE_ADJUST_END', '')
$menu = [regex]::Replace($menu, '(?s)\s*// OASIS_EDGE_DRAW_BEGIN.*?// OASIS_EDGE_DRAW_END', '')
$adjust = @'

	// OASIS_EDGE_ADJUST_BEGIN
	case GAME_OPT_OFFLINE_SYNC:
		if (dir && OQuake_STAR_OfflineSyncMode() >= 0)
			OQuake_STAR_OfflineSyncCommand(OQuake_STAR_OfflineSyncMode() ? "off" : "on");
		break;
	case GAME_OPT_OFFLINE_DRAIN:
		if (dir && OQuake_STAR_OfflineSyncMode() > 0)
			OQuake_STAR_OfflineSyncCommand("sync-and-off");
		break;
	// OASIS_EDGE_ADJUST_END
'@
$draw = @'

		// OASIS_EDGE_DRAW_BEGIN
		case GAME_OPT_OFFLINE_SYNC:
			M_Print(cbx, MENU_LABEL_X, y, "Offline Sync");
			if (OQuake_STAR_OfflineSyncMode() < 0)
				M_Print(cbx, MENU_VALUE_X, y, "Unavailable");
			else M_DrawCheckbox(cbx, MENU_VALUE_X, y, OQuake_STAR_OfflineSyncMode());
			break;
		case GAME_OPT_OFFLINE_DRAIN:
			M_Print(cbx, MENU_LABEL_X, y, "Sync and disable");
			M_Print(cbx, MENU_VALUE_X, y, OQuake_STAR_OfflineSyncMode() > 0 ? "Enter" : "Unavailable");
			break;
		// OASIS_EDGE_DRAW_END
'@
$adjustAnchor = '(?m)^\tcase GAME_OPT_SCALE:'
$drawAnchor = '(?m)^\t\tcase GAME_OPT_SCALE:'
if ($menu -notmatch $adjustAnchor -or $menu -notmatch $drawAnchor) { throw 'Quake game options handlers changed.' }
$menu = [regex]::Replace($menu, $adjustAnchor, $adjust + "`n" + '$0', 1)
$menu = [regex]::Replace($menu, $drawAnchor, $draw + "`n" + '$0', 1)
[IO.File]::WriteAllText($path, $menu)
