class OASISInventoryOverlayHandler : EventHandler
{
	private bool popupOpen;
	private int activeTab;
	private int scrollOffset;
	private int selectedAbsolute;
	private bool wasUser1Down;
	private bool wasUser2Down;
	private bool wasUser3Down;
	private bool wasBackDown;
	private bool wasForwardDown;
	private bool wasLookUpDown;
	private bool wasLookDownDown;
	private bool wasUseDown;
	private bool wasUser4Down;
	private bool wasReloadDown;
	private bool wasJumpDown;
	private bool wasCrouchDown;
	private bool wasKeyUpDown;
	private bool wasKeyDownDown;
	private bool wasKeyLeftDown;
	private bool wasKeyRightDown;
	private bool wasKeyUseDown;
	private bool wasKeyADown;
	private bool wasKeyCDown;
	private bool wasKeyZDown;
	private bool wasKeyXDown;
	private bool wasKeyIDown;
	private bool wasKeyODown;
	private bool wasKeyPDown;
	private bool wasKeySDown;
	private bool wasKeyTDown;
	private bool wasKeyQDown;
	private bool wasKeyEnterDown;
	private bool wasKeyKDown;
	private bool wasKeyBackspaceDown;
	private bool wasKeyPgUpDown;
	private bool wasKeyPgDownDown;
	private bool wasKeyHomeDown;
	private bool wasKeyEndDown;
	private bool wasKeyBDown;
	private bool wasKeyNDown;
	private bool wasKeyMDown;
	private bool questPopupOpen;
	private int questSelectedIndex;
	private int questScrollOffset;
	private String questStatusMessage;
	private int questStatusFrames;
	// Quest detail (2nd) popup: P=Prereqs, O=Objectives, S=Subquests (separate views). Enter on prereq/subquest = close and select in main list.
	private bool questDetailPopupOpen;
	private int questDetailMode;   // 0=Objectives (O), 1=Prereqs (P), 2=Subquests (S)
	private String questDetailQuestId;
	private String questDetailQuestName;
	private String questDetailQuestDesc;
	private String questDetailQuestStatus;  // for Start/Set tracker in detail
	private int questDetailFocus;  // 0=objectives, 1=prereqs, 2=subquests (which list has focus in current mode)
	private int questDetailPrereqSelected;
	private int questDetailObjSelected;
	private int questDetailSubSelected;
	private int questDetailPrereqScroll;
	private int questDetailObjScroll;
	private int questDetailSubScroll;
	private bool questDetailSyncSelectionOnce;  // when true, sync questDetailObjSelected from tracker (by id; retry until applied or limit)
	private int questDetailSyncSelectionRetry; // frames we've tried sync without applying; stop after limit
	private bool questDetailIgnoreNextEnter;   // true after opening detail: ignore first Enter so we don't persist wrong objective (same key that opened detail)
	private String questGotoId;  // when set, next frame in 1st popup select this quest id in main list

	// Send popup (OQuake-style)
	private int sendPopupMode;   // 0=none, 1=avatar, 2=clan
	private int sendQuantity;
	private int sendButtonFocus;  // 0=Send, 1=Cancel
	private String sendItemClass;
	private String sendItemDisplayLabel;  // e.g. "Silver Key (OQUAKE) x2" - shown above name box
	private int sendMaxQty;
	private String sendInputLine;  // name built from odoom_send_last_char (C++ sets one char per frame)

	// Cached list for RenderOverlay (ui cannot call play-context; no array members in this ZScript build)
	private int cachedStarCount;       // number of rows in current window (from C++)
	private int cachedStarTotalCount;  // total STAR item count (for scroll math; C++ sends window + total)
	private String cachedStarListForTab;   // "name\tdesc\tgame\n" per STAR item in current tab
	private int cachedLocalCount;
	private String cachedLocalListForTab;  // "displayName\tamount\n" per actor item in current tab

	const TAB_KEYS = 0;
	const TAB_POWERUPS = 1;
	const TAB_WEAPONS = 2;
	const TAB_AMMO = 3;
	const TAB_ARMOR = 4;
	const TAB_ITEMS = 5;
	const TAB_MONSTERS = 6;
	const TAB_COUNT = 7;
	const MAX_VISIBLE_ROWS = 6;
	// Cap STAR list size so we never overflow engine CVar or ZScript string buffers ("attempted to write past end of stream").
	const MAX_STAR_ITEMS_TO_PARSE = 32;
	const MAX_STAR_GROUPS_TO_CACHE = 32;

	override void OnRegister()
	{
		IsUiProcessor = false;
		RequireMouse = false;
	}

	override void WorldTick()
	{
		if (!playeringame[consoleplayer]) return;
		let p = players[consoleplayer];
		if (!p || !p.mo) return;

		// Tell C++ whether inventory is open (so it can clear/restore key bindings, OQuake-style)
		CVar openVar = CVar.FindCVar("odoom_inventory_open");
		if (openVar != null)
			openVar.SetInt(popupOpen ? 1 : 0);
		// Quest popup state is owned by ZScript (same as inventory). Only write to CVar so C++ knows for refresh; do not read from CVar.
		CVar questPopupCv = CVar.FindCVar("odoom_quest_popup_open");
		if (questPopupCv != null)
			questPopupCv.SetInt(questPopupOpen ? 1 : 0);
		if (questPopupOpen && questStatusFrames > 0) {
			questStatusFrames--;
			if (questStatusFrames <= 0) questStatusMessage = "";
		}
		// Tell C++ which scroll offset and tab we want so it sends the right window of items (avoids CVar overflow)
		if (popupOpen)
		{
			CVar scrollCv = CVar.FindCVar("odoom_star_inventory_scroll_offset");
			if (scrollCv != null) scrollCv.SetInt(scrollOffset);
			CVar tabCv = CVar.FindCVar("odoom_star_inventory_tab");
			if (tabCv != null) tabCv.SetInt(activeTab);
		}
		// Tell C++ whether send popup is open every frame so it can capture name typing
		CVar sendOpenVar = CVar.FindCVar("odoom_send_popup_open");
		if (sendOpenVar != null)
			sendOpenVar.SetInt(sendPopupMode != 0 ? 1 : 0);

		// Only when beamed in: give OQuake key actors so HUD shows them (left). When not beamed in, remove them.
		CVar beamedVar = CVar.FindCVar("odoom_star_beamed_in");
		int beamedIn = (beamedVar != null) ? beamedVar.GetInt() : 0;
		if (beamedIn != 0)
		{
			CVar hasGoldVar = CVar.FindCVar("odoom_star_has_gold_key");
			CVar hasSilverVar = CVar.FindCVar("odoom_star_has_silver_key");
			if (hasGoldVar != null && hasGoldVar.GetInt() != 0 && p.mo.FindInventory("OQGoldKey") == null)
				p.mo.GiveInventory("OQGoldKey", 1);
			if (hasSilverVar != null && hasSilverVar.GetInt() != 0 && p.mo.FindInventory("OQSilverKey") == null)
				p.mo.GiveInventory("OQSilverKey", 1);
		}
		else
		{
			Inventory oqGold = p.mo.FindInventory("OQGoldKey");
			if (oqGold != null) p.mo.RemoveInventory(oqGold);
			Inventory oqSilver = p.mo.FindInventory("OQSilverKey");
			if (oqSilver != null) p.mo.RemoveInventory(oqSilver);
		}

		int buttons = p.cmd.buttons;
		bool user1Down = (buttons & BT_USER1) != 0;
		bool user2Down = (buttons & BT_USER2) != 0;
		bool user3Down = (buttons & BT_USER3) != 0;
		bool backDown = (buttons & BT_BACK) != 0;
		bool forwardDown = (buttons & BT_FORWARD) != 0;
		bool lookUpDown = (buttons & BT_LOOKUP) != 0;
		bool lookDownDown = (buttons & BT_LOOKDOWN) != 0;
		bool useDown = (buttons & BT_USE) != 0;
		bool user4Down = (buttons & BT_USER4) != 0;
		bool reloadDown = (buttons & BT_RELOAD) != 0;
		bool jumpDown = (buttons & BT_JUMP) != 0;
		bool crouchDown = (buttons & BT_CROUCH) != 0;

		// Keys captured by C++ when inventory open (odoom_key_* CVars). Read every frame so wasKey* stay in sync when closed.
		int keyUp = 0, keyDown = 0, keyLeft = 0, keyRight = 0, keyUse = 0, keyK = 0, keyA = 0, keyC = 0, keyZ = 0, keyX = 0, keyI = 0, keyO = 0, keyP = 0, keyS = 0, keyT = 0, keyQ = 0, keyEnter = 0, keyBackspace = 0;
		int keyPgUp = 0, keyPgDown = 0, keyHome = 0, keyEnd = 0, keyB = 0, keyN = 0, keyM = 0;
		CVar v;
		v = CVar.FindCVar("odoom_key_up"); if (v != null) keyUp = v.GetInt();
		v = CVar.FindCVar("odoom_key_down"); if (v != null) keyDown = v.GetInt();
		v = CVar.FindCVar("odoom_key_left"); if (v != null) keyLeft = v.GetInt();
		v = CVar.FindCVar("odoom_key_right"); if (v != null) keyRight = v.GetInt();
		v = CVar.FindCVar("odoom_key_pgup"); if (v != null) keyPgUp = v.GetInt();
		v = CVar.FindCVar("odoom_key_pgdown"); if (v != null) keyPgDown = v.GetInt();
		v = CVar.FindCVar("odoom_key_home"); if (v != null) keyHome = v.GetInt();
		v = CVar.FindCVar("odoom_key_end"); if (v != null) keyEnd = v.GetInt();
		v = CVar.FindCVar("odoom_key_b"); if (v != null) keyB = v.GetInt();
		v = CVar.FindCVar("odoom_key_n"); if (v != null) keyN = v.GetInt();
		v = CVar.FindCVar("odoom_key_m"); if (v != null) keyM = v.GetInt();
		v = CVar.FindCVar("odoom_key_use"); if (v != null) keyUse = v.GetInt();
		v = CVar.FindCVar("odoom_key_a"); if (v != null) keyA = v.GetInt();
		v = CVar.FindCVar("odoom_key_c"); if (v != null) keyC = v.GetInt();
		v = CVar.FindCVar("odoom_key_z"); if (v != null) keyZ = v.GetInt();
		v = CVar.FindCVar("odoom_key_x"); if (v != null) keyX = v.GetInt();
		v = CVar.FindCVar("odoom_key_i"); if (v != null) keyI = v.GetInt();
		v = CVar.FindCVar("odoom_key_o"); if (v != null) keyO = v.GetInt();
		v = CVar.FindCVar("odoom_key_p"); if (v != null) keyP = v.GetInt();
		v = CVar.FindCVar("odoom_key_s"); if (v != null) keyS = v.GetInt();
		v = CVar.FindCVar("odoom_key_t"); if (v != null) keyT = v.GetInt();
		v = CVar.FindCVar("odoom_key_q"); if (v != null) keyQ = v.GetInt();
		v = CVar.FindCVar("odoom_key_enter"); if (v != null) keyEnter = v.GetInt();
		v = CVar.FindCVar("odoom_key_k"); if (v != null) keyK = v.GetInt();
		v = CVar.FindCVar("odoom_key_backspace"); if (v != null) keyBackspace = v.GetInt();
		bool keyUpPressed = (keyUp != 0) && !wasKeyUpDown;
		bool keyPgUpPressed = (keyPgUp != 0) && !wasKeyPgUpDown;
		bool keyPgDownPressed = (keyPgDown != 0) && !wasKeyPgDownDown;
		bool keyHomePressed = (keyHome != 0) && !wasKeyHomeDown;
		bool keyEndPressed = (keyEnd != 0) && !wasKeyEndDown;
		bool keyBPressed = (keyB != 0) && !wasKeyBDown;
		bool keyNPressed = (keyN != 0) && !wasKeyNDown;
		bool keyMPressed = (keyM != 0) && !wasKeyMDown;
		bool keyDownPressed = (keyDown != 0) && !wasKeyDownDown;
		bool keyLeftPressed = (keyLeft != 0) && !wasKeyLeftDown;
		bool keyRightPressed = (keyRight != 0) && !wasKeyRightDown;
		bool keyUsePressed = (keyUse != 0) && !wasKeyUseDown;
		bool keyAPressed = (keyA != 0) && !wasKeyADown;
		bool keyCPressed = (keyC != 0) && !wasKeyCDown;
		bool keyZPressed = (keyZ != 0) && !wasKeyZDown;
		bool keyXPressed = (keyX != 0) && !wasKeyXDown;
		bool keyIPressed = (keyI != 0) && !wasKeyIDown;
		bool keyOPressed = (keyO != 0) && !wasKeyODown;
		bool keyPPressed = (keyP != 0) && !wasKeyPDown;
		bool keySPressed = (keyS != 0) && !wasKeySDown;
		bool keyTPressed = (keyT != 0) && !wasKeyTDown;
		bool keyQPressed = (keyQ != 0) && !wasKeyQDown;
		bool keyEnterPressed = (keyEnter != 0) && !wasKeyEnterDown;
		bool keyKPressed = (keyK != 0) && !wasKeyKDown;
		bool keyBackspacePressed = (keyBackspace != 0) && !wasKeyBackspaceDown;
		wasKeyUpDown = (keyUp != 0);
		wasKeyDownDown = (keyDown != 0);
		wasKeyLeftDown = (keyLeft != 0);
		wasKeyRightDown = (keyRight != 0);
		wasKeyUseDown = (keyUse != 0);
		wasKeyADown = (keyA != 0);
		wasKeyCDown = (keyC != 0);
		wasKeyZDown = (keyZ != 0);
		wasKeyXDown = (keyX != 0);
		wasKeyIDown = (keyI != 0);
		wasKeyODown = (keyO != 0);
		wasKeyPDown = (keyP != 0);
		wasKeySDown = (keyS != 0);
		wasKeyTDown = (keyT != 0);
		wasKeyQDown = (keyQ != 0);
		wasKeyEnterDown = (keyEnter != 0);
		wasKeyKDown = (keyK != 0);
		wasKeyBackspaceDown = (keyBackspace != 0);
		wasKeyPgUpDown = (keyPgUp != 0);
		wasKeyPgDownDown = (keyPgDown != 0);
		wasKeyHomeDown = (keyHome != 0);
		wasKeyEndDown = (keyEnd != 0);
		wasKeyBDown = (keyB != 0);
		wasKeyNDown = (keyN != 0);
		wasKeyMDown = (keyM != 0);

		/* B/X/Z HUD toggles (Beamed / XP / timer) are handled in C++ (ODOOM_InventoryInputCaptureFrame) so they work reliably with raw SDL keys. */

		if ((user1Down && !wasUser1Down) || keyIPressed)
		{
			popupOpen = !popupOpen;
			if (popupOpen)
			{
				scrollOffset = 0;
				selectedAbsolute = 0;
			}
		}
		// Q toggles quest popup (same technique as I for inventory: one place, edge-triggered toggle)
		if (keyQPressed)
		{
			questPopupOpen = !questPopupOpen;
			if (questPopupOpen) {
				questSelectedIndex = 0;
				questScrollOffset = 0;
				questStatusMessage = "";
				questStatusFrames = 0;
				questDetailPopupOpen = false;
				questGotoId = "";
				CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
				if (scrollCv != null) scrollCv.SetInt(0);
				CVar detailIdCv = CVar.FindCVar("odoom_quest_detail_quest_id");
				if (detailIdCv != null) detailIdCv.SetString("");
			}
			if (questPopupCv != null) questPopupCv.SetInt(questPopupOpen ? 1 : 0);
		}
		// Pressing I while quest popup is open closes quest (I will also toggle inventory)
		if (questPopupOpen && keyIPressed)
		{
			questPopupOpen = false;
			questStatusMessage = "";
			questStatusFrames = 0;
			if (questPopupCv != null) questPopupCv.SetInt(0);
		}
		// When quest popup is closed: O = cycle tracker (obj 1,2,3,...,All,Hide, then repeat). Hide is in the cycle (T reserved for chat).
		if (!questPopupOpen && keyOPressed)
		{
			CVar objLinesCv = CVar.FindCVar("odoom_quest_tracker_objectives");
			CVar idxCv = CVar.FindCVar("odoom_quest_tracker_objective_index");
			CVar showCv = CVar.FindCVar("odoom_quest_tracker_show");
			if (objLinesCv != null && idxCv != null && showCv != null)
			{
				String objStr = objLinesCv.GetString();
				array<String> lines;
				if (objStr.Length() > 0) objStr.Split(lines, "\n", false);
				int n = lines.Size();
				int choices = n + 2;  // 0..n-1 = single obj, n = All, n+1 = Hide
				if (choices < 2) choices = 2;
				int cur = idxCv.GetInt();
				if (cur < 0) cur = 0;
				cur = (cur + 1) % choices;
				idxCv.SetInt(cur);
				// When landing on Hide (n+1) hide tracker; when leaving Hide show tracker
				if (cur == n + 1) showCv.SetInt(0);
				else showCv.SetInt(1);
			}
		}
		if (questPopupOpen)
		{
			if (keyBackspacePressed && !questDetailPopupOpen)
			{
				questPopupOpen = false;
				questStatusMessage = "";
				questStatusFrames = 0;
				if (questPopupCv != null) questPopupCv.SetInt(0);
				CVar detailIdCv2 = CVar.FindCVar("odoom_quest_detail_quest_id");
				if (detailIdCv2 != null) detailIdCv2.SetString("");
			}
			CVar scrollCvSync = CVar.FindCVar("odoom_quest_scroll_offset");
			if (scrollCvSync != null) questScrollOffset = scrollCvSync.GetInt();
			CVar listCv = CVar.FindCVar("odoom_quest_list");
			String listStr = (listCv != null) ? listCv.GetString() : "";
			array<String> questLines;
			if (listStr.Length() > 0 && listStr.IndexOf("Error:") != 0 && listStr.IndexOf("Loading") != 0)
			{
				array<String> allLines;
				listStr.Split(allLines, "\n", false);
				for (int L = 0; L < allLines.Size(); L++)
				{
					if (allLines[L].Length() >= 2 && allLines[L].IndexOf("Q\t") == 0)
						questLines.Push(allLines[L]);
				}
			}
			CVar fnCv = CVar.FindCVar("odoom_quest_filter_not_started");
			CVar fiCv = CVar.FindCVar("odoom_quest_filter_in_progress");
			CVar fcCv = CVar.FindCVar("odoom_quest_filter_completed");
			int fn = (fnCv != null) ? fnCv.GetInt() : 1;
			int fi = (fiCv != null) ? fiCv.GetInt() : 1;
			int fc = (fcCv != null) ? fcCv.GetInt() : 1;
			/* Filter toggles: B=Not Started, N=In Progress, M=Completed */
			if (keyBPressed) {
				CVar cv = CVar.FindCVar("odoom_quest_filter_not_started");
				if (cv != null) cv.SetInt(cv.GetInt() != 0 ? 0 : 1);
			}
			if (keyNPressed) {
				CVar cv = CVar.FindCVar("odoom_quest_filter_in_progress");
				if (cv != null) cv.SetInt(cv.GetInt() != 0 ? 0 : 1);
			}
			if (keyMPressed) {
				CVar cv = CVar.FindCVar("odoom_quest_filter_completed");
				if (cv != null) cv.SetInt(cv.GetInt() != 0 ? 0 : 1);
			}
			array<int> filteredIndices;
			for (int b = 0; b < questLines.Size(); b++)
			{
				array<String> parts;
				questLines[b].Split(parts, "\t", false);
				if (parts.Size() < 5) continue;
				String st = parts[4];
				bool show = ((st.Compare("NotStarted") == 0 || st.Compare("Not Started") == 0) && fn != 0) || ((st.Compare("InProgress") == 0 || st.Compare("In Progress") == 0) && fi != 0) || (st.Compare("Completed") == 0 && fc != 0);
				if (show) filteredIndices.Push(b);
			}
			int qCount = filteredIndices.Size();
			int maxQuestRowsKey = (200 - 80) / 12 - 4; // match maxQuestRows (room for 2-line hint)
			if (maxQuestRowsKey < 5) maxQuestRowsKey = 5;
			// So C++ can react to K without relying on one-frame CVar handoff: set selected quest id every frame.
			CVar selectedIdCv = CVar.FindCVar("odoom_quest_selected_id");
			if (questDetailPopupOpen && questDetailQuestId.Length() > 0)
			{
				if (selectedIdCv != null) selectedIdCv.SetString(questDetailQuestId);
			}
			else
			{
				String selId = "";
				if (qCount > 0 && questSelectedIndex >= 0 && questSelectedIndex < filteredIndices.Size() && filteredIndices[questSelectedIndex] >= 0 && filteredIndices[questSelectedIndex] < questLines.Size())
				{
					array<String> parts;
					questLines[filteredIndices[questSelectedIndex]].Split(parts, "\t", false);
					if (parts.Size() >= 2) selId = parts[1];
				}
				if (selectedIdCv != null) selectedIdCv.SetString(selId);
			}
			if (qCount > 0 && !questDetailPopupOpen)
			{
				if (keyDownPressed) { questSelectedIndex++; if (questSelectedIndex >= qCount) questSelectedIndex = qCount - 1; }
				if (keyUpPressed) { questSelectedIndex--; if (questSelectedIndex < 0) questSelectedIndex = 0; }
				if (keyHomePressed) {
					questSelectedIndex = 0;
					CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
					if (scrollCv != null) scrollCv.SetInt(0);
				}
				if (keyEndPressed) {
					questSelectedIndex = qCount - 1;
					int so = questSelectedIndex - maxQuestRowsKey + 1;
					if (so < 0) so = 0;
					CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
					if (scrollCv != null) scrollCv.SetInt(so);
				}
				if (keyPgUpPressed) {
					questSelectedIndex -= maxQuestRowsKey;
					if (questSelectedIndex < 0) questSelectedIndex = 0;
					CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
					if (scrollCv != null) scrollCv.SetInt(questSelectedIndex);
				}
				if (keyPgDownPressed) {
					questSelectedIndex += maxQuestRowsKey;
					if (questSelectedIndex >= qCount) questSelectedIndex = qCount - 1;
					int so = questSelectedIndex - maxQuestRowsKey + 1;
					if (so < 0) so = 0;
					CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
					if (scrollCv != null) scrollCv.SetInt(so);
				}
				// Enter = open detail (2nd) popup with desc + prereqs/objectives/subquests
				if (keyEnterPressed)
				{
					if (questSelectedIndex >= 0 && questSelectedIndex < filteredIndices.Size() && filteredIndices[questSelectedIndex] >= 0 && filteredIndices[questSelectedIndex] < questLines.Size())
					{
						array<String> parts;
						questLines[filteredIndices[questSelectedIndex]].Split(parts, "\t", false);
						if (parts.Size() >= 5)
						{
							questDetailQuestId = parts[1];
							questDetailQuestName = parts[2];
							questDetailQuestDesc = parts.Size() > 3 ? parts[3] : "";
							questDetailQuestStatus = parts[4];
							questDetailPopupOpen = true;
							questDetailMode = 0;  // default: Objectives (O)
							questDetailFocus = 0;
							questDetailPrereqSelected = 0;
							questDetailObjSelected = 0;
							questDetailSubSelected = 0;
							questDetailPrereqScroll = 0;
							questDetailObjScroll = 0;
							questDetailSubScroll = 0;
							questDetailSyncSelectionOnce = true;
							questDetailSyncSelectionRetry = 0;
							questDetailIgnoreNextEnter = true;  // same Enter must not be treated as "set active objective"
							CVar detailIdCv = CVar.FindCVar("odoom_quest_detail_quest_id");
							if (detailIdCv != null) detailIdCv.SetString(questDetailQuestId);
						}
					}
				}
				// K = Start (Not Started) or Set tracker (In Progress) on selected quest (Space is jump)
				if (keyKPressed && !questDetailPopupOpen)
				{
					if (questSelectedIndex >= 0 && questSelectedIndex < filteredIndices.Size() && filteredIndices[questSelectedIndex] >= 0 && filteredIndices[questSelectedIndex] < questLines.Size())
					{
						array<String> parts;
						questLines[filteredIndices[questSelectedIndex]].Split(parts, "\t", false);
						if (parts.Size() >= 5)
						{
							String qid = parts[1];
							String status = parts[4];
							if ((status.Compare("NotStarted") == 0 || status.Compare("Not Started") == 0) && qid.Length() > 0)
							{
								questStatusMessage = "Starting quest...";
								questStatusFrames = 105;
								CVar idCv = CVar.FindCVar("odoom_quest_set_active_id");
								CVar doCv = CVar.FindCVar("odoom_quest_set_active_do_it");
								if (idCv != null) idCv.SetString(qid);
								if (doCv != null) doCv.SetInt(1);
							}
							else if ((status.Compare("InProgress") == 0 || status.Compare("In Progress") == 0) && qid.Length() > 0)
							{
								CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
								if (trackerIdCv != null) trackerIdCv.SetString(qid);
								CVar activeObjCv = CVar.FindCVar("odoom_quest_tracker_active_objective_id");
								if (activeObjCv != null) activeObjCv.SetString("");
							}
						}
					}
				}
			}
			// Resolve drill-down: when we closed detail with a selected prereq/subquest, select that quest in main list
			if (questPopupOpen && !questDetailPopupOpen && questGotoId.Length() > 0)
			{
				for (int b = 0; b < filteredIndices.Size(); b++)
				{
					if (filteredIndices[b] < 0 || filteredIndices[b] >= questLines.Size()) continue;
					array<String> parts;
					questLines[filteredIndices[b]].Split(parts, "\t", false);
					if (parts.Size() >= 2 && parts[1].Compare(questGotoId) == 0)
					{
						questSelectedIndex = b;
						int maxQuestRowsKey2 = (200 - 80) / 12 - 4;
						if (maxQuestRowsKey2 < 5) maxQuestRowsKey2 = 5;
						int so = questSelectedIndex - maxQuestRowsKey2 + 1;
						if (so < 0) so = 0;
						CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
						if (scrollCv != null) scrollCv.SetInt(so);
						break;
					}
				}
				questGotoId = "";
			}
			// Detail (2nd) popup input
			if (questDetailPopupOpen)
			{
				CVar prereqCv = CVar.FindCVar("odoom_quest_detail_prereqs");
				CVar objCv = CVar.FindCVar("odoom_quest_detail_objectives");
				CVar subCv = CVar.FindCVar("odoom_quest_detail_subquests");
				String prereqStr = (prereqCv != null) ? prereqCv.GetString() : "";
				String objStr = (objCv != null) ? objCv.GetString() : "";
				String subStr = (subCv != null) ? subCv.GetString() : "";
				array<String> prereqLines; array<String> objLines; array<String> subLines;
				if (prereqStr.Length() > 0) prereqStr.Split(prereqLines, "\n", false);
				if (objStr.Length() > 0) objStr.Split(objLines, "\n", false);
				if (subStr.Length() > 0) subStr.Split(subLines, "\n", false);
				array<int> prereqQ; array<int> objQ; array<int> subQ;
				for (int i = 0; i < prereqLines.Size(); i++) if (prereqLines[i].Length() >= 2 && prereqLines[i].IndexOf("Q\t") == 0) prereqQ.Push(i);
				for (int i = 0; i < objLines.Size(); i++) if (objLines[i].Length() >= 2 && (objLines[i].IndexOf("Q\t") == 0 || objLines[i].IndexOf("O\t") == 0)) objQ.Push(i);
				for (int i = 0; i < subLines.Size(); i++) if (subLines[i].Length() >= 2 && subLines[i].IndexOf("Q\t") == 0) subQ.Push(i);
				int nPrereq = prereqQ.Size(); int nObj = objQ.Size(); int nSub = subQ.Size();
				int maxRowsObj = 8;     // objectives list rows in mode 0 (above Requirements section)
				int maxRowsSingle = 13; // full-height list rows in mode 1 (Prereqs) or mode 2 (Subquests)
				// When detail opens: sync objective selection to tracker by active objective id (never by index clamp so we never jump to last)
				if (questDetailSyncSelectionOnce && nObj > 0)
				{
					CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
					CVar trackerActiveIdCv = CVar.FindCVar("odoom_quest_tracker_active_objective_id");
					CVar trackerActiveIdxCv = CVar.FindCVar("odoom_quest_tracker_active_index");
					String trackerQuestId = (trackerIdCv != null) ? trackerIdCv.GetString() : "";
					if (questDetailQuestId.Compare(trackerQuestId) == 0)
					{
						int idx = -1;
						String activeId = (trackerActiveIdCv != null) ? trackerActiveIdCv.GetString() : "";
						if (activeId.Length() > 0)
						{
							// Match by objective id so we always select the right row regardless of list order
							for (int i = 0; i < nObj; i++)
							{
								int lineIdx = objQ[i];
								if (lineIdx < 0 || lineIdx >= objLines.Size()) continue;
								array<String> parts;
								objLines[lineIdx].Split(parts, "\t", false);
								if (parts.Size() >= 2 && parts[1].Compare(activeId) == 0) { idx = i; break; }
							}
						}
						// Only use index when in range; never clamp to nObj-1 (that caused "resets to last")
						if (idx < 0 && trackerActiveIdxCv != null)
						{
							int rawIdx = trackerActiveIdxCv.GetInt();
							if (rawIdx >= 0 && rawIdx < nObj)
								idx = rawIdx;
						}
						if (idx >= 0)
						{
							questDetailObjSelected = idx;
							if (questDetailObjScroll > questDetailObjSelected) questDetailObjScroll = questDetailObjSelected;
							if (questDetailObjSelected >= questDetailObjScroll + maxRowsObj) questDetailObjScroll = questDetailObjSelected - maxRowsObj + 1;
							if (questDetailObjScroll < 0) questDetailObjScroll = 0;
							questDetailSyncSelectionOnce = false;
						}
						else
						{
							questDetailSyncSelectionRetry++;
							if (questDetailSyncSelectionRetry >= 30)
								questDetailSyncSelectionOnce = false;
						}
					}
					else
						questDetailSyncSelectionOnce = false;
				}
				// Tell C++ which objective is selected so it can fill requirement/progress lines (Objectives mode only)
				CVar selObjCv = CVar.FindCVar("odoom_quest_detail_selected_objective_id");
				CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
				String trackerId = (trackerIdCv != null) ? trackerIdCv.GetString() : "";
				bool detailIsTracked = (questDetailQuestId.Compare(trackerId) == 0);
				if (selObjCv != null)
				{
					if (questDetailMode == 0 && nObj > 0 && questDetailObjSelected >= 0 && questDetailObjSelected < objQ.Size())
					{
						int idx = objQ[questDetailObjSelected];
						if (idx >= 0 && idx < objLines.Size())
						{
							array<String> parts;
							objLines[idx].Split(parts, "\t", false);
							if (parts.Size() >= 2) selObjCv.SetString(parts[1]); else selObjCv.SetString("");
						}
						else selObjCv.SetString("");
					}
					else selObjCv.SetString("");
				}
				// Focus 0=Objectives, 1=Prereqs, 2=Subquests (only one list visible per mode)
				if (keyUpPressed) {
					if (questDetailFocus == 0 && questDetailObjSelected > 0) questDetailObjSelected--;
					else if (questDetailFocus == 1 && questDetailPrereqSelected > 0) questDetailPrereqSelected--;
					else if (questDetailFocus == 2 && questDetailSubSelected > 0) questDetailSubSelected--;
				}
				if (keyDownPressed) {
					if (questDetailFocus == 0 && questDetailObjSelected < nObj - 1) questDetailObjSelected++;
					else if (questDetailFocus == 1 && questDetailPrereqSelected < nPrereq - 1) questDetailPrereqSelected++;
					else if (questDetailFocus == 2 && questDetailSubSelected < nSub - 1) questDetailSubSelected++;
				}
				if (questDetailObjSelected < questDetailObjScroll) questDetailObjScroll = questDetailObjSelected;
				if (questDetailObjSelected >= questDetailObjScroll + maxRowsObj && nObj > maxRowsObj) questDetailObjScroll = questDetailObjSelected - maxRowsObj + 1;
				if (questDetailPrereqSelected < questDetailPrereqScroll) questDetailPrereqScroll = questDetailPrereqSelected;
				if (questDetailPrereqSelected >= questDetailPrereqScroll + maxRowsSingle && nPrereq > maxRowsSingle) questDetailPrereqScroll = questDetailPrereqSelected - maxRowsSingle + 1;
				if (questDetailSubSelected < questDetailSubScroll) questDetailSubScroll = questDetailSubSelected;
				if (questDetailSubSelected >= questDetailSubScroll + maxRowsSingle && nSub > maxRowsSingle) questDetailSubScroll = questDetailSubSelected - maxRowsSingle + 1;
				// P=Prereqs, O=Objectives, S=Subquests: switch detail view
				if (keyPPressed) { questDetailMode = 1; questDetailFocus = 1; }
				if (keyOPressed) { questDetailMode = 0; questDetailFocus = 0; }
				if (keySPressed) { questDetailMode = 2; questDetailFocus = 2; }
				if (keyLeftPressed) { questDetailFocus--; if (questDetailFocus < 0) questDetailFocus = 2; }
				if (keyRightPressed) { questDetailFocus++; if (questDetailFocus > 2) questDetailFocus = 0; }
				if (keyPgUpPressed) {
					if (questDetailFocus == 0) { questDetailObjSelected -= maxRowsObj; if (questDetailObjSelected < 0) questDetailObjSelected = 0; questDetailObjScroll = questDetailObjSelected; }
					else if (questDetailFocus == 1) { questDetailPrereqSelected -= maxRowsSingle; if (questDetailPrereqSelected < 0) questDetailPrereqSelected = 0; questDetailPrereqScroll = questDetailPrereqSelected; }
					else { questDetailSubSelected -= maxRowsSingle; if (questDetailSubSelected < 0) questDetailSubSelected = 0; questDetailSubScroll = questDetailSubSelected; }
				}
				if (keyPgDownPressed) {
					if (questDetailFocus == 0) { questDetailObjSelected += maxRowsObj; if (questDetailObjSelected >= nObj) questDetailObjSelected = nObj - 1; questDetailObjScroll = questDetailObjSelected - maxRowsObj + 1; if (questDetailObjScroll < 0) questDetailObjScroll = 0; }
					else if (questDetailFocus == 1) { questDetailPrereqSelected += maxRowsSingle; if (questDetailPrereqSelected >= nPrereq) questDetailPrereqSelected = nPrereq - 1; questDetailPrereqScroll = questDetailPrereqSelected - maxRowsSingle + 1; if (questDetailPrereqScroll < 0) questDetailPrereqScroll = 0; }
					else { questDetailSubSelected += maxRowsSingle; if (questDetailSubSelected >= nSub) questDetailSubSelected = nSub - 1; questDetailSubScroll = questDetailSubSelected - maxRowsSingle + 1; if (questDetailSubScroll < 0) questDetailSubScroll = 0; }
				}
				// Backspace: Prereqs/Subquests -> Objectives first; then back to main quest list (Escape is engine menu)
				if (keyBackspacePressed) {
					if (questDetailMode != 0) { questDetailMode = 0; questDetailFocus = 0; }
					else { questDetailPopupOpen = false; CVar detailIdCv = CVar.FindCVar("odoom_quest_detail_quest_id"); if (detailIdCv != null) detailIdCv.SetString(""); }
				}
				// Enter on objective (focus 0) = set as active, highlight green. Enter on prereq (focus 1) or subquest (focus 2) = drill down.
				// Ignore first Enter after opening detail so the key that opened the popup doesn't persist the (wrong) selected row.
				if (questDetailIgnoreNextEnter) { if (keyEnterPressed) questDetailIgnoreNextEnter = false; }
				else if (keyEnterPressed)
				{
					if (questDetailFocus == 0 && nObj > 0 && questDetailObjSelected >= 0 && questDetailObjSelected < objQ.Size())
					{
						CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
						String trackerId = (trackerIdCv != null) ? trackerIdCv.GetString() : "";
						if (questDetailQuestId.Compare(trackerId) == 0)
						{
							int idx = objQ[questDetailObjSelected];
							if (idx >= 0 && idx < objLines.Size())
							{
								array<String> parts;
								objLines[idx].Split(parts, "\t", false);
								if (parts.Size() >= 2)
								{
									CVar activeObjCv = CVar.FindCVar("odoom_quest_tracker_active_objective_id");
									if (activeObjCv != null) activeObjCv.SetString(parts[1]);
									CVar trackerIdxCv = CVar.FindCVar("odoom_quest_tracker_objective_index");
									if (trackerIdxCv != null) trackerIdxCv.SetInt(questDetailObjSelected);
									CVar trackerActiveIdxCv = CVar.FindCVar("odoom_quest_tracker_active_index");
									if (trackerActiveIdxCv != null) trackerActiveIdxCv.SetInt(questDetailObjSelected);
									CVar persistCv = CVar.FindCVar("odoom_quest_persist_active_now");
									if (persistCv != null) persistCv.SetInt(1);
								}
							}
						}
					}
					else if (questDetailFocus == 1 && nPrereq > 0 && questDetailPrereqSelected >= 0 && questDetailPrereqSelected < prereqQ.Size())
					{
						array<String> parts;
						prereqLines[prereqQ[questDetailPrereqSelected]].Split(parts, "\t", false);
						if (parts.Size() >= 2) { questGotoId = parts[1]; questDetailPopupOpen = false; CVar detailIdCv = CVar.FindCVar("odoom_quest_detail_quest_id"); if (detailIdCv != null) detailIdCv.SetString(""); }
					}
					else if (questDetailFocus == 2 && nSub > 0 && questDetailSubSelected >= 0 && questDetailSubSelected < subQ.Size())
					{
						array<String> parts;
						subLines[subQ[questDetailSubSelected]].Split(parts, "\t", false);
						if (parts.Size() >= 2) { questGotoId = parts[1]; questDetailPopupOpen = false; CVar detailIdCv = CVar.FindCVar("odoom_quest_detail_quest_id"); if (detailIdCv != null) detailIdCv.SetString(""); }
					}
				}
				// K in detail = Start or Set tracker for this quest
				if (keyKPressed)
				{
					if (questDetailQuestId.Length() > 0)
					{
						if (questDetailQuestStatus.Compare("NotStarted") == 0 || questDetailQuestStatus.Compare("Not Started") == 0)
						{
							questStatusMessage = "Starting quest...";
							questStatusFrames = 105;
							CVar idCv = CVar.FindCVar("odoom_quest_set_active_id");
							CVar doCv = CVar.FindCVar("odoom_quest_set_active_do_it");
							if (idCv != null) idCv.SetString(questDetailQuestId);
							if (doCv != null) doCv.SetInt(1);
						}
						else if (questDetailQuestStatus.Compare("InProgress") == 0 || questDetailQuestStatus.Compare("In Progress") == 0)
						{
							CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
							if (trackerIdCv != null) trackerIdCv.SetString(questDetailQuestId);
							CVar activeObjCv = CVar.FindCVar("odoom_quest_tracker_active_objective_id");
							if (activeObjCv != null) activeObjCv.SetString("");
							CVar persistCv = CVar.FindCVar("odoom_quest_persist_active_now");
							if (persistCv != null) persistCv.SetInt(1);
						}
					}
				}
			}
		}

		if (popupOpen)
		{
			if ((user2Down && !wasUser2Down) || keyLeftPressed || keyOPressed)
			{
				activeTab--;
				if (activeTab < 0) activeTab = TAB_COUNT - 1;
				scrollOffset = 0;
				selectedAbsolute = 0;
			}
			if ((user3Down && !wasUser3Down) || keyRightPressed || keyPPressed)
			{
				activeTab++;
				if (activeTab >= TAB_COUNT) activeTab = 0;
				scrollOffset = 0;
				selectedAbsolute = 0;
			}

			int starCount = 0;
			array<String> starNames, starDescs, starTypes, starGames;
			array<int> starQuantities;
			starCount = BuildStarItemsForTab(starNames, starDescs, starTypes, starGames, starQuantities);
			// Total count from C++ (so we can scroll through all items; list CVar only has current window)
			cachedStarTotalCount = 0;
			CVar countCv = CVar.FindCVar("odoom_star_inventory_count");
			if (countCv != null) cachedStarTotalCount = countCv.GetInt();
			if (cachedStarTotalCount < 0) cachedStarTotalCount = 0;

			array<Inventory> tabItems;
			BuildTabInventory(p.mo, tabItems);

			// One row per item in the window (no grouping) so scroll/selection indices match full list
			array<String> starGroupLabels;
			array<int> starGroupCounts;
			array<String> starGroupFirstNames;
			array<String> starGroupTypes;
			array<String> starGroupDescs;
			for (int i = 0; i < starCount; i++)
			{
				int qty = (i < starQuantities.Size() && starQuantities[i] > 0) ? starQuantities[i] : 1;
				// Monster items already have game in the name (e.g. "Dog (OQUAKE)"); don't append game again.
				String desc = (i < starDescs.Size()) ? starDescs[i] : "";
				String label = (i < starTypes.Size() && starTypes[i].Compare("Monster") == 0)
					? starNames[i]
					: StarItemShortLabelWithAmount(starNames[i], starGames[i], desc);
				starGroupLabels.Push(String.Format("%s x%d", label, qty));
				starGroupCounts.Push(qty);
				starGroupFirstNames.Push(starNames[i]);
				starGroupTypes.Push((i < starTypes.Size()) ? starTypes[i] : "Item");
				starGroupDescs.Push((i < starDescs.Size()) ? starDescs[i] : "");
			}
			int starGroupCount = starGroupLabels.Size();
			if (starGroupCount > MAX_STAR_GROUPS_TO_CACHE) starGroupCount = MAX_STAR_GROUPS_TO_CACHE;
			cachedStarCount = starGroupCount;
			cachedStarListForTab = "";
			for (int i = 0; i < starGroupCount; i++)
			{
				cachedStarListForTab = String.Format("%s%s\n", cachedStarListForTab, starGroupLabels[i]);
			}

			// Show only shared (STAR/ODOOM) inventory; do not show local Doom actor items to avoid duplicates.
			int localGroupCount = 0;
			cachedLocalCount = 0;
			cachedLocalListForTab = "";
			array<String> localGroupClass;
			array<String> localGroupDisp;
			array<int> localGroupAmount;
			array<int> localGroupRepIdx;

			// listCount = total STAR items (so scroll covers all) + local
			int listCount = cachedStarTotalCount + localGroupCount;
			int maxOffset = listCount - MAX_VISIBLE_ROWS;
			if (maxOffset < 0) maxOffset = 0;
			if (selectedAbsolute >= listCount && listCount > 0) selectedAbsolute = listCount - 1;
			if (selectedAbsolute < 0) selectedAbsolute = 0;
			// Keep selection inside current window so use/send can resolve starGroupFirstNames[selectedAbsolute - scrollOffset]
			if (cachedStarTotalCount > 0 && selectedAbsolute < scrollOffset) selectedAbsolute = scrollOffset;
			if (cachedStarTotalCount > 0 && starGroupCount > 0 && selectedAbsolute >= scrollOffset + starGroupCount)
				selectedAbsolute = scrollOffset + starGroupCount - 1;
			Inventory selectedItem = null;
			int groupAmountForSend = 0;
			int starWindowIdx = (cachedStarTotalCount > 0) ? (selectedAbsolute - scrollOffset) : selectedAbsolute;
			if (selectedAbsolute >= (cachedStarTotalCount > 0 ? cachedStarTotalCount : starGroupCount) && selectedAbsolute - (cachedStarTotalCount > 0 ? cachedStarTotalCount : starGroupCount) < localGroupCount)
			{
				int gidx = selectedAbsolute - (cachedStarTotalCount > 0 ? cachedStarTotalCount : starGroupCount);
				selectedItem = tabItems[localGroupRepIdx[gidx]];
				groupAmountForSend = localGroupAmount[gidx];
			}
			if (sendPopupMode == 0)
			{

			// Selection: arrows only (from captured key CVars). Do not use W/S so they don't move list or player.
			bool selUp = keyUpPressed || (lookUpDown && !wasLookUpDown) || (jumpDown && !wasJumpDown);
			bool selDown = keyDownPressed || (lookDownDown && !wasLookDownDown) || (crouchDown && !wasCrouchDown);
			if (selUp)
			{
				selectedAbsolute--;
				if (selectedAbsolute < 0) selectedAbsolute = 0;
				scrollOffset = selectedAbsolute - MAX_VISIBLE_ROWS + 1;
				if (scrollOffset < 0) scrollOffset = 0;
				if (scrollOffset > maxOffset) scrollOffset = maxOffset;
			}
			if (selDown)
			{
				selectedAbsolute++;
				if (selectedAbsolute >= listCount) selectedAbsolute = listCount - 1;
				if (selectedAbsolute < 0) selectedAbsolute = 0;
				scrollOffset = selectedAbsolute - MAX_VISIBLE_ROWS + 1;
				if (scrollOffset < 0) scrollOffset = 0;
				if (scrollOffset > maxOffset) scrollOffset = maxOffset;
			}

			// PgUp / PgDn / Home / End with bounds checks so we never scroll out of range
			if (keyPgUpPressed)
			{
				scrollOffset -= MAX_VISIBLE_ROWS;
				if (scrollOffset < 0) scrollOffset = 0;
				selectedAbsolute = scrollOffset;
				if (selectedAbsolute >= listCount && listCount > 0) selectedAbsolute = listCount - 1;
			}
			if (keyPgDownPressed)
			{
				scrollOffset += MAX_VISIBLE_ROWS;
				if (scrollOffset > maxOffset) scrollOffset = maxOffset;
				selectedAbsolute = scrollOffset + MAX_VISIBLE_ROWS - 1;
				if (selectedAbsolute >= listCount && listCount > 0) selectedAbsolute = listCount - 1;
				if (selectedAbsolute < 0) selectedAbsolute = 0;
			}
			if (keyHomePressed)
			{
				scrollOffset = 0;
				selectedAbsolute = 0;
			}
			if (keyEndPressed)
			{
				scrollOffset = maxOffset;
				if (scrollOffset < 0) scrollOffset = 0;
				selectedAbsolute = listCount - 1;
				if (selectedAbsolute < 0) selectedAbsolute = 0;
			}

			bool canUseStar = (selectedAbsolute < (cachedStarTotalCount > 0 ? cachedStarTotalCount : starGroupCount) && starGroupCount > 0 && starWindowIdx >= 0 && starWindowIdx < starGroupCount && starGroupCounts[starWindowIdx] > 0);
			if ((useDown && !wasUseDown) || keyUsePressed)
			{
				if (canUseStar)
				{
					String starType = starGroupTypes[starWindowIdx];
					// STAR weapons: switch to that weapon if player has it locally (do not consume from STAR).
					if (starType.IndexOf("Weapon") >= 0)
					{
						String wname = starGroupFirstNames[starWindowIdx];
						// Match by class name (wname may be "Shotgun" or "Shotgun (ODOOM)")
						for (let inv = p.mo.Inv; inv != null; inv = inv.Inv)
						{
							if (inv is "Weapon")
							{
								String cname = inv.GetClassName();
								if (cname.IndexOf(wname) >= 0 || wname.IndexOf(cname) >= 0)
								{
									p.PendingWeapon = Weapon(inv);
									break;
								}
							}
						}
					}
					// STAR ammo: pressing E has no effect (cannot use/consume ammo from inventory).
					else if (starType.IndexOf("Ammo") >= 0)
					{
						// no-op
					}
					// STAR keys/keycards: pressing E has no effect; keys can only be used when pressing E on a door.
					else if (starType.IndexOf("Key") >= 0)
					{
						// no-op
					}
					else
					{
						CVar nameCv = CVar.FindCVar("odoom_star_use_item_name");
						CVar typeCv = CVar.FindCVar("odoom_star_use_item_type");
						CVar descCv = CVar.FindCVar("odoom_star_use_item_description");
						CVar doCv = CVar.FindCVar("odoom_star_use_do_it");
						if (nameCv != null) nameCv.SetString(starGroupFirstNames[starWindowIdx]);
						if (typeCv != null) typeCv.SetString(starType);
						if (descCv != null && starWindowIdx < starGroupDescs.Size()) descCv.SetString(starGroupDescs[starWindowIdx]);
						if (doCv != null) doCv.SetInt(1);
					}
				}
				else if (selectedItem != null && selectedItem.Amount > 0)
				{
					// Weapons: switch to that weapon (do not consume); Ammo and Keys: no effect (use only on door for keys).
					if (selectedItem is "Weapon")
					{
						Weapon w = Weapon(selectedItem);
						if (w != null)
						{
							p.PendingWeapon = w;
						}
					}
					else if (!(selectedItem is "Ammo") && !(selectedItem is "Key"))
					{
						p.mo.UseInventory(selectedItem);
					}
					if (selectedAbsolute >= listCount && listCount > 0) selectedAbsolute = listCount - 1;
					if (selectedAbsolute < 0) selectedAbsolute = 0;
				}
			}

			// A or Z = Send to Avatar, C or X = Send to Clan - open send popup for STAR or local items
			bool canSendStar = (selectedAbsolute < (cachedStarTotalCount > 0 ? cachedStarTotalCount : starGroupCount) && starGroupCount > 0 && starWindowIdx >= 0 && starWindowIdx < starGroupCount && starGroupCounts[starWindowIdx] > 0);
			bool canSendLocal = (selectedItem != null && (selectedItem.Amount > 0 || groupAmountForSend > 0));
			if ((keyAPressed || keyZPressed) && (canSendStar || canSendLocal))
			{
				sendPopupMode = 1;
				if (canSendStar)
				{
					sendMaxQty = starGroupCounts[starWindowIdx];
					sendItemClass = String.Format("STAR:%s", starGroupFirstNames[starWindowIdx]);
					sendItemDisplayLabel = (starGroupCounts[starWindowIdx] > 1) ? String.Format("%s x%d", starGroupLabels[starWindowIdx], starGroupCounts[starWindowIdx]) : starGroupLabels[starWindowIdx];
				}
				else
				{
					sendMaxQty = groupAmountForSend > 0 ? groupAmountForSend : selectedItem.Amount;
					if (sendMaxQty < 1) sendMaxQty = 1;
					sendItemClass = selectedItem.GetClassName();
					String dispName = GetItemDisplayNamePlay(selectedItem);
					sendItemDisplayLabel = (sendMaxQty > 1) ? String.Format("%s x%d", dispName, sendMaxQty) : dispName;
				}
				sendQuantity = 1;
				sendButtonFocus = 0;
				sendInputLine = "";
				CVar lineVar = CVar.FindCVar("odoom_send_input_line");
				if (lineVar != null) lineVar.SetString("");
				CVar cv = CVar.FindCVar("odoom_send_popup_open");
				if (cv != null) cv.SetInt(1);
			}
			if ((keyCPressed || keyXPressed) && (canSendStar || canSendLocal))
			{
				sendPopupMode = 2;
				if (canSendStar)
				{
					sendMaxQty = starGroupCounts[starWindowIdx];
					sendItemClass = String.Format("STAR:%s", starGroupFirstNames[starWindowIdx]);
					sendItemDisplayLabel = (starGroupCounts[starWindowIdx] > 1) ? String.Format("%s x%d", starGroupLabels[starWindowIdx], starGroupCounts[starWindowIdx]) : starGroupLabels[starWindowIdx];
				}
				else
				{
					sendMaxQty = groupAmountForSend > 0 ? groupAmountForSend : selectedItem.Amount;
					if (sendMaxQty < 1) sendMaxQty = 1;
					sendItemClass = selectedItem.GetClassName();
					String dispName = GetItemDisplayNamePlay(selectedItem);
					sendItemDisplayLabel = (sendMaxQty > 1) ? String.Format("%s x%d", dispName, sendMaxQty) : dispName;
				}
				sendQuantity = 1;
				sendButtonFocus = 0;
				sendInputLine = "";
				CVar lineVar = CVar.FindCVar("odoom_send_input_line");
				if (lineVar != null) lineVar.SetString("");
				CVar cv = CVar.FindCVar("odoom_send_popup_open");
				if (cv != null) cv.SetInt(1);
			}
			} // sendPopupMode == 0

		// Send popup handling when open
		if (sendPopupMode != 0)
		{
			// Build name from C++ odoom_send_last_char (one char per frame; 0=none, 8=backspace)
			CVar lastCharVar = CVar.FindCVar("odoom_send_last_char");
			if (lastCharVar != null)
			{
				int ch = lastCharVar.GetInt();
				if (ch == 8 && sendInputLine.Length() > 0)
					sendInputLine = sendInputLine.Left(sendInputLine.Length() - 1);
				else if (ch != 0 && ch != 8 && sendInputLine.Length() < 64)
				{
					String oneChar = "";
					oneChar.AppendCharacter(ch);
					sendInputLine = String.Format("%s%s", sendInputLine, oneChar);
				}
			}
			// Also sync from full-line cvar so typing still shows if last-char path is unavailable.
			CVar lineVarSync = CVar.FindCVar("odoom_send_input_line");
			if (lineVarSync != null)
			{
				String syncLine = lineVarSync.GetString();
				if (syncLine.Length() > 0 || sendInputLine.Length() == 0)
					sendInputLine = syncLine;
			}
			else
			{
				// Fallback for builds missing odoom_send_last_char: read full line from string CVar.
				CVar lineVar = CVar.FindCVar("odoom_send_input_line");
				if (lineVar != null) sendInputLine = lineVar.GetString();
			}
			if (keyLeftPressed) sendButtonFocus = 0;
			if (keyRightPressed) sendButtonFocus = 1;
			if (keyUpPressed && sendQuantity < sendMaxQty) sendQuantity++;
			if (keyDownPressed && sendQuantity > 1) sendQuantity--;
			if (keyPgUpPressed && sendQuantity < sendMaxQty) { sendQuantity += 10; if (sendQuantity > sendMaxQty) sendQuantity = sendMaxQty; }
			if (keyPgDownPressed && sendQuantity > 1) { sendQuantity -= 10; if (sendQuantity < 1) sendQuantity = 1; }
			// I = close popup without sending (cancel)
			if (keyIPressed)
			{
				sendPopupMode = 0;
				CVar cv = CVar.FindCVar("odoom_send_popup_open");
				if (cv != null) cv.SetInt(0);
			}
			// Enter or E = confirm (Send if focus 0, else just close). When result is shown, Enter/E/I closes.
			else if (keyEnterPressed || keyUsePressed)
			{
				CVar statusCv = CVar.FindCVar("odoom_send_status");
				String sendStatus = (statusCv != null) ? statusCv.GetString() : "";
				bool isResult = (sendStatus.Length() > 0 && sendStatus.Compare("Sending...") != 0);
				if (isResult)
				{
					sendPopupMode = 0;
					CVar cv = CVar.FindCVar("odoom_send_popup_open");
					if (cv != null) cv.SetInt(0);
				}
				else if (sendButtonFocus == 0)
				{
					if (sendInputLine.Length() > 0)
					{
						CVar t = CVar.FindCVar("odoom_send_target");
						if (t != null) t.SetString(sendInputLine);
						t = CVar.FindCVar("odoom_send_item_class");
						if (t != null) t.SetString(sendItemClass);
						t = CVar.FindCVar("odoom_send_quantity");
						if (t != null) t.SetInt(sendQuantity);
						t = CVar.FindCVar("odoom_send_to_clan");
						if (t != null) t.SetInt(sendPopupMode == 2 ? 1 : 0);
						t = CVar.FindCVar("odoom_send_do_it");
						if (t != null) t.SetInt(1);
						// Keep popup open; C++ will set Sending... then Item sent./Send failed (we show and close on key)
					}
					else
					{
						sendPopupMode = 0;
						CVar cv = CVar.FindCVar("odoom_send_popup_open");
						if (cv != null) cv.SetInt(0);
					}
				}
				else
				{
					sendPopupMode = 0;
					CVar cv = CVar.FindCVar("odoom_send_popup_open");
					if (cv != null) cv.SetInt(0);
				}
			}
		}

	}

		// Keep button-edge state in sync every tic (was only updated inside inventory block before, which broke edge detection when popup closed).
		wasUser1Down = user1Down;
		wasUser2Down = user2Down;
		wasUser3Down = user3Down;
		wasBackDown = backDown;
		wasForwardDown = forwardDown;
		wasLookUpDown = lookUpDown;
		wasLookDownDown = lookDownDown;
		wasUseDown = useDown;
		wasUser4Down = user4Down;
		wasReloadDown = reloadDown;
		wasJumpDown = jumpDown;
		wasCrouchDown = crouchDown;
	} // end WorldTick

	private ui int GetClampedOffset(int listCount)
	{
		int maxOffset = listCount - MAX_VISIBLE_ROWS;
		if (maxOffset < 0) maxOffset = 0;
		int offset = scrollOffset;
		if (offset < 0) offset = 0;
		if (offset > maxOffset) offset = maxOffset;
		return offset;
	}

	private ui String TabName(int tabIndex)
	{
		switch (tabIndex)
		{
		case TAB_KEYS: return "Keys";
		case TAB_POWERUPS: return "Powerups";
		case TAB_WEAPONS: return "Weapons";
		case TAB_AMMO: return "Ammo";
		case TAB_ARMOR: return "Armor";
		case TAB_MONSTERS: return "Monsters";
		default: return "Items";
		}
	}

	private bool IsItemInActiveTab(Inventory item, int tabIndex)
	{
		if (item == null || item.Amount <= 0) return false;
		if (tabIndex == TAB_KEYS) return item is "Key";
		if (tabIndex == TAB_POWERUPS) return item is "Powerup";
		if (tabIndex == TAB_WEAPONS) return item is "Weapon";
		if (tabIndex == TAB_AMMO) return item is "Ammo";
		if (tabIndex == TAB_ARMOR) return item is "Armor";
		if (tabIndex == TAB_MONSTERS) return false;  // Monster NFTs are STAR-only, no local actor
		return !(item is "Key") && !(item is "Powerup") && !(item is "Weapon") && !(item is "Armor") && !(item is "Ammo");
	}

	// STAR item matches tab (same data as "star inventory" command, from odoom_star_inventory_list).
	private bool IsStarItemInTab(String itemType, String itemName, int tabIndex)
	{
		String t = itemType;
		String n = itemName;
		if (tabIndex == TAB_KEYS) return t.IndexOf("Key") >= 0 || n.IndexOf("key") >= 0;
		if (tabIndex == TAB_POWERUPS) return t.IndexOf("Powerup") >= 0 || t == "Powerup";
		if (tabIndex == TAB_WEAPONS) return t.IndexOf("Weapon") >= 0 || t == "Weapon";
		if (tabIndex == TAB_AMMO) return t.IndexOf("Ammo") >= 0 || t == "Ammo";
		if (tabIndex == TAB_ARMOR) return t.IndexOf("Armor") >= 0 || t == "Armor";
		if (tabIndex == TAB_MONSTERS) return t == "Monster" || t.IndexOf("Monster") >= 0 || n.IndexOf("[NFT]") >= 0 || n.IndexOf("[BOSSNFT]") >= 0;
		// TAB_ITEMS: only items that don't fit Keys, Powerups, Weapons, Ammo, Armor, or Monsters
		if (tabIndex == TAB_ITEMS)
			return (t.IndexOf("Key") < 0 && n.IndexOf("key") < 0) && (t.IndexOf("Powerup") < 0 && t != "Powerup") && (t.IndexOf("Weapon") < 0 && t != "Weapon") && (t.IndexOf("Ammo") < 0 && t != "Ammo") && (t.IndexOf("Armor") < 0 && t != "Armor") && (t != "Monster" && t.IndexOf("Monster") < 0);
		return true;
	}

	// Short display label for STAR items: "Shells (OQUAKE)" – item name (game in brackets). Names are already short (Shells, Shotgun, etc.).
	// If name already contains " (OQUAKE)" or " (ODOOM)" (e.g. monster kills), show as-is to avoid duplicating game.
	private String StarItemShortLabel(String name, String game)
	{
		String n = name;
		if (n.IndexOf(" (OQUAKE)") >= 0 || n.IndexOf(" (ODOOM)") >= 0)
			return n;
		String g = (game.Length() > 0) ? game : "STAR";
		if (g == "QUAKE" || g == "Quake" || g == "quake") g = "OQUAKE";
		// ODOOM/Doom-specific mappings
		if (n.IndexOf("Clip") >= 0 || n.IndexOf("clip") >= 0 || n.IndexOf("Bullet") >= 0) return String.Format("Bullets (%s)", g);
		if (n.IndexOf("Shell") >= 0 || n.IndexOf("shell") >= 0) return String.Format("Shells (%s)", g);
		if (n.IndexOf("Cell") >= 0 || n.IndexOf("cell") >= 0) return String.Format("Cells (%s)", g);
		if (n.IndexOf("Armor") >= 0 || n.IndexOf("armor") >= 0) return String.Format("Armor (%s)", g);
		if (n.IndexOf("Stimpack") >= 0 || n.IndexOf("stimpack") >= 0) return String.Format("Stimpack (%s)", g);
		if (n.IndexOf("Medikit") >= 0 || n.IndexOf("medikit") >= 0) return String.Format("Medikit (%s)", g);
		if (n.IndexOf("Backpack") >= 0 || n.IndexOf("backpack") >= 0) return String.Format("Backpack (%s)", g);
		if (n.IndexOf("Weapon") >= 0 || n.IndexOf("weapon") >= 0) return String.Format("Weapon (%s)", g);
		if (n.IndexOf("red_keycard") >= 0 || n.IndexOf("red_key") >= 0) return String.Format("Red Keycard (%s)", g);
		if (n.IndexOf("blue_keycard") >= 0 || n.IndexOf("blue_key") >= 0) return String.Format("Blue Keycard (%s)", g);
		if (n.IndexOf("yellow_keycard") >= 0 || n.IndexOf("yellow_key") >= 0) return String.Format("Yellow Keycard (%s)", g);
		if (n.Length() > 24) return String.Format("%s (%s)", n.Left(21), g);
		return String.Format("%s (%s)", n, g);
	}

	// Like StarItemShortLabel but if desc contains "(+N)" appends " +N" before game tag (e.g. "Green Armor +100 (OQUAKE)").
	private String StarItemShortLabelWithAmount(String name, String game, String desc)
	{
		String base = StarItemShortLabel(name, game);
		if (desc.Length() == 0) return base;
		int plusIdx = desc.IndexOf("(+");
		if (plusIdx < 0) return base;
		int numStart = plusIdx + 2;
		int numEnd = desc.IndexOf(")", numStart);
		if (numEnd <= numStart) return base;
		String amount = desc.Mid(numStart, numEnd - numStart);
		int insertAt = base.IndexOf(" (");
		if (insertAt >= 0)
			return String.Format("%s +%s%s", base.Left(insertAt), amount, base.Mid(insertAt));
		return String.Format("%s +%s", base, amount);
	}

	private void BuildTabInventory(Actor owner, out array<Inventory> outItems)
	{
		outItems.Clear();
		if (owner == null) return;

		for (let inv = owner.Inv; inv != null; inv = inv.Inv)
		{
			if (IsItemInActiveTab(inv, activeTab))
			{
				outItems.Push(inv);
			}
		}
	}

	// Parse odoom_star_inventory_list (format "name\tdesc\ttype\tgame\tquantity\n" per line, quantity optional) and append STAR items for active tab.
	// Returns number of STAR rows. Row data in starNames, starDescs, starTypes, starGames, starQuantities (qty per row, for grouping).
	private int BuildStarItemsForTab(out array<String> starNames, out array<String> starDescs, out array<String> starTypes, out array<String> starGames, out array<int> starQuantities)
	{
		starNames.Clear();
		starDescs.Clear();
		starTypes.Clear();
		starGames.Clear();
		starQuantities.Clear();
		CVar listVar = CVar.FindCVar("odoom_star_inventory_list");
		if (listVar == null) return 0;
		String listStr = listVar.GetString();
		if (listStr.Length() == 0) return 0;
		array<String> lines;
		listStr.Split(lines, "\n", false);
		int maxLines = lines.Size();
		if (maxLines > MAX_STAR_ITEMS_TO_PARSE) maxLines = MAX_STAR_ITEMS_TO_PARSE;
		for (int i = 0; i < maxLines; i++)
		{
			array<String> parts;
			lines[i].Split(parts, "\t", false);
			if (parts.Size() < 4) continue;
			String name = parts[0];
			String desc = parts[1];
			String typ = parts[2];
			String game = parts[3];
			int qty = 1;
			if (parts.Size() >= 5 && parts[4].Length() > 0)
			{
				qty = parts[4].ToInt();
				if (qty < 1) qty = 1;
			}
			if (!IsStarItemInTab(typ, name, activeTab)) continue;
			starNames.Push(name);
			starDescs.Push(desc);
			starTypes.Push(typ);
			starGames.Push(game);
			starQuantities.Push(qty);
		}
		return starNames.Size();
	}

	// Play-context version for building cachedLocalListForTab in WorldTick
	private String GetItemDisplayNamePlay(Inventory item)
	{
		if (item == null) return "";
		if (item is "OQGoldKey") return "Golden Key";
		if (item is "OQSilverKey") return "Silver Key";
		String tag = item.GetTag("");
		if (tag.Length() > 0) return tag;
		return item.GetClassName();
	}

	// Word-wrap with hard breaks for tokens wider than maxW (plain space-split leaves long URLs as one line).
	private void DrawWrappedWords(Screen s, Font font, int cr, int x0, int y0, int maxW, int rowH, int maxLines, array<String> words)
	{
		String line = "";
		int ly = y0;
		int nlines = 0;
		for (int wi = 0; wi < words.Size() && nlines < maxLines; wi++)
		{
			String w = words[wi];
			while (w.Length() > 0 && nlines < maxLines)
			{
				String candidate = line.Length() > 0 ? String.Format("%s %s", line, w) : w;
				if (font.StringWidth(candidate) <= maxW)
				{
					line = candidate;
					break;
				}
				if (line.Length() > 0)
				{
					s.DrawText(font, cr, x0, ly, line, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					ly += rowH;
					nlines++;
					line = "";
					continue;
				}
				int lo = 1, hi = w.Length(), best = 1;
				while (lo <= hi)
				{
					int mid = (lo + hi) / 2;
					if (font.StringWidth(w.Left(mid)) <= maxW) { best = mid; lo = mid + 1; }
					else hi = mid - 1;
				}
				if (best < 1) best = 1;
				s.DrawText(font, cr, x0, ly, w.Left(best), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				ly += rowH;
				nlines++;
				w = w.Mid(best);
			}
		}
		if (line.Length() > 0 && nlines < maxLines)
			s.DrawText(font, cr, x0, ly, line, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
	}

	private ui String ItemDisplayName(Inventory item)
	{
		if (item == null) return "";
		if (item is "OQGoldKey") return "Golden Key";
		if (item is "OQSilverKey") return "Silver Key";
		String tag = item.GetTag("");
		if (tag.Length() > 0) return tag;
		return item.GetClassName();
	}

	override void RenderOverlay(RenderEvent e)
	{
		let p = players[consoleplayer];
		if (!p || !p.mo) return;

		Font f = "SmallFont";

		CVar showTimerCv = CVar.FindCVar("odoom_hud_show_timer");
		int showTimerHud = (showTimerCv != null) ? showTimerCv.GetInt() : 1;
		// Level timer: right side, just above the status bar; fixed-width digits so it doesn't shift as numbers change. MapTime is in tics (35/sec).
		if (showTimerHud != 0)
		{
			int timeY = 161;  // down 5 from 156
			int tics = level.MapTime;
			int secs = tics / 35;
			int mins = secs / 60;
			secs = secs % 60;
			int digitW = f.StringWidth("0");
			int colonW = f.StringWidth(":");
			int totalW = 2 * digitW + colonW + 2 * digitW;  // MM:SS fixed width
			int baseX = (320 - 40) - totalW;
			String m1 = (mins >= 10) ? String.Format("%d", mins / 10) : " ";
			String m2 = String.Format("%d", mins % 10);
			String s1 = String.Format("%d", secs / 10);
			String s2 = String.Format("%d", secs % 10);
			screen.DrawText(f, Font.CR_WHITE, baseX, timeY, m1, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_WHITE, baseX + digitW, timeY, m2, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_WHITE, baseX + 2 * digitW, timeY, ":", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_WHITE, baseX + 2 * digitW + colonW, timeY, s1, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_WHITE, baseX + 3 * digitW + colonW, timeY, s2, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		}

		// XP at far right of screen when beamed in (always visible during play)
		CVar beamedVar = CVar.FindCVar("odoom_star_beamed_in");
		CVar xpVar = CVar.FindCVar("odoom_star_avatar_xp");
		CVar showXpCv = CVar.FindCVar("odoom_hud_show_xp");
		int showXpHud = (showXpCv != null) ? showXpCv.GetInt() : 1;
		if (beamedVar != null && beamedVar.GetInt() != 0 && xpVar != null && showXpHud != 0)
		{
			int xp = xpVar.GetInt();
			String xpText = String.Format("XP: %d", xp);
			int xpW = f.StringWidth(xpText);
			int xpX = 320 - xpW - 2 + 50;  // 50px to the right of original position
			screen.DrawText(f, Font.CR_GOLD, xpX, 2, xpText, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		}

		// Toast message at top center (~3 seconds when C++ sets odoom_star_toast_message and odoom_star_toast_frames). Scaled down so it fits on screen.
		CVar toastFramesCv = CVar.FindCVar("odoom_star_toast_frames");
		CVar toastMsgCv = CVar.FindCVar("odoom_star_toast_message");
		if (toastFramesCv != null && toastFramesCv.GetInt() > 0 && toastMsgCv != null)
		{
			String toastMsg = toastMsgCv.GetString();
			if (toastMsg.Length() > 0)
			{
				double toastScale = 0.5;
				int tw = int(f.StringWidth(toastMsg) * toastScale);
				int tx = 160 - (tw / 2);
				if (tx < 2) tx = 2;
				screen.DrawText(f, Font.CR_GOLD, tx, 4, toastMsg, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, toastScale, DTA_ScaleY, toastScale);
			}
		}

		// Pickup toast: "Picked up X" in red at top-left when we take health/armor/ammo into STAR (same style as engine pickup message).
		CVar pickupFramesCv = CVar.FindCVar("odoom_star_pickup_toast_frames");
		CVar pickupMsgCv = CVar.FindCVar("odoom_star_pickup_toast_message");
		if (pickupFramesCv != null && pickupFramesCv.GetInt() > 0 && pickupMsgCv != null)
		{
			String pickupMsg = pickupMsgCv.GetString();
			if (pickupMsg.Length() > 0)
			{
				double pickupScale = 0.5;
				screen.DrawText(f, Font.CR_RED, 2, 4, pickupMsg, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, pickupScale, DTA_ScaleY, pickupScale);
			}
		}

		// Quest Tracker: left side, below "Beamed In:". O=cycle (1,2,3,All,Hide,...). Progress text e.g. "Killed 3/10 monsters". Hide when quest popup open or cycle on Hide.
		CVar trackerShowCv = CVar.FindCVar("odoom_quest_tracker_show");
		int trackerShow = (trackerShowCv != null) ? trackerShowCv.GetInt() : 1;
		if (!questPopupOpen && trackerShow != 0)
		{
			CVar trackerTitleCv = CVar.FindCVar("odoom_quest_tracker_title");
			CVar trackerObjLinesCv = CVar.FindCVar("odoom_quest_tracker_objectives");
			CVar trackerIdxCv = CVar.FindCVar("odoom_quest_tracker_objective_index");
			CVar trackerActiveCv = CVar.FindCVar("odoom_quest_tracker_active_index");
			bool beamedIn = (beamedVar != null && beamedVar.GetInt() != 0);
			String qTitle = (trackerTitleCv != null) ? trackerTitleCv.GetString() : "";
			if (beamedIn && qTitle.Length() > 0)
			{
				int trackX = -53;
				int trackY = 7;
				double trackScale = 0.5;
				// Match OQuake: when loading show just "Loading..."; when loaded show "Quest: <title>"
				String titleLabel = (qTitle == "Loading...") ? "Loading..." : String.Format("Quest: %s", qTitle);
				double titleScale = 0.6;
				screen.DrawText(f, Font.CR_GOLD, trackX, trackY, titleLabel, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, titleScale, DTA_ScaleY, titleScale);
				String objStr = (trackerObjLinesCv != null) ? trackerObjLinesCv.GetString() : "";
				array<String> objLines;
				if (objStr.Length() > 0) objStr.Split(objLines, "\n", false);
				int nObj = objLines.Size();
				int dispIdx = (trackerIdxCv != null) ? trackerIdxCv.GetInt() : 0;
				if (dispIdx < 0) dispIdx = 0;
				if (dispIdx > nObj + 1) dispIdx = nObj + 1;  // clamp; nObj+1 = Hide (trackerShow already 0)
				// Green highlight: use odoom_quest_tracker_active_index (set on Enter in popup) when active_objective_id is set, else API first-incomplete from tracker_active_index
				CVar activeObjIdCv = CVar.FindCVar("odoom_quest_tracker_active_objective_id");
				String activeObjId = (activeObjIdCv != null) ? activeObjIdCv.GetString() : "";
				int activeIdx = (trackerActiveCv != null) ? trackerActiveCv.GetInt() : 0;
				if (activeIdx < 0) activeIdx = 0;
				if (activeIdx >= nObj) activeIdx = nObj > 0 ? nObj - 1 : 0;
				if (nObj > 0)
				{
					if (dispIdx >= nObj)
					{
						// All: show each objective line, highlight active in green
						for (int i = 0; i < nObj; i++)
						{
							String line = objLines[i];
							int cr = (i == activeIdx) ? Font.CR_GREEN : Font.CR_WHITE;
							screen.DrawText(f, cr, trackX, trackY + 10 + i * 10, line, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, trackScale, DTA_ScaleY, trackScale);
						}
					}
					else
					{
						// Single objective (progress text)
						String line = objLines[dispIdx];
						screen.DrawText(f, Font.CR_WHITE, trackX, trackY + 10, line, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, trackScale, DTA_ScaleY, trackScale);
					}
				}
			}
		}

		// Quest popup (Q key): 1st = list only; 2nd = detail. P=Prereqs, O=Objectives, S=Subquests (separate popup views).
		if (questPopupOpen && questDetailPopupOpen)
		{
			int popupW = 320;
			int popupH = 200;
			int popupX = -55;
			int popupY = 6;  // align "Quest: ..." heading with "QUESTS" on main popup (main uses popupY+14)
			int leftW = 120;
			int rightX = popupX + leftW + 8;
			int rightW = (320 - 8) - rightX;  // full width to screen edge
			int rowH = 10;
			int rightPaneH = popupH - 56;
			// 50/50 split: top half = quest desc (left) + list (right); bottom half = objective/prereq/sub desc (left) + requirements or nothing (right)
			int halfH = rightPaneH / 2;
			int sect0Y = popupY + 26;   // start of right-pane content (list in top half)
			int sect1Y = sect0Y + halfH; // start of bottom half (Requirements in mode 0)
			int listAreaH = halfH;
			int maxRowsObj = (listAreaH - 12) / rowH;
			if (maxRowsObj < 1) maxRowsObj = 1;
			int maxRowsSingle = (rightPaneH - 12) / rowH;  // full-height list in mode 1 or 2
			if (maxRowsSingle < 1) maxRowsSingle = 1;
			String questTitle = questDetailQuestName;
			if (questTitle.Length() > 28) questTitle = String.Format("%s..", questTitle.Left(26));
			screen.DrawText(f, Font.CR_GOLD, popupX + 8, popupY + 8, String.Format("Quest: %s", questTitle), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			// Mode tabs: [P] [O] [S] right-aligned with 10 between them (quest title no longer overlaps)
			int tabY = popupY + 6;
			int crP = (questDetailMode == 1) ? Font.CR_GOLD : Font.CR_GRAY;
			int crO = (questDetailMode == 0) ? Font.CR_GOLD : Font.CR_GRAY;
			int crS = (questDetailMode == 2) ? Font.CR_GOLD : Font.CR_GRAY;
			int wS = f.StringWidth("[S] Sub");
			int wO = f.StringWidth("[O] Obj");
			int wP = f.StringWidth("[P] Prereq");
			int tabRightEdge = rightX + rightW - 10 + 60;  // [P] [O] [S] moved right 60 total (20+40)
			int tabX_S = tabRightEdge - wS;
			int tabX_O = tabX_S - 10 - wO;
			int tabX_P = tabX_O - 10 - wP;
			screen.DrawText(f, crP, tabX_P, tabY, "[P] Prereq", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, crO, tabX_O, tabY, "[O] Obj", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, crS, tabX_S, tabY, "[S] Sub", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			CVar prereqCv = CVar.FindCVar("odoom_quest_detail_prereqs");
			CVar objCv = CVar.FindCVar("odoom_quest_detail_objectives");
			CVar subCv = CVar.FindCVar("odoom_quest_detail_subquests");
			String prereqStr = (prereqCv != null) ? prereqCv.GetString() : "";
			String objStr = (objCv != null) ? objCv.GetString() : "";
			String subStr = (subCv != null) ? subCv.GetString() : "";
			array<String> prereqLines; array<String> objLines; array<String> subLines;
			if (prereqStr.Length() > 0) prereqStr.Split(prereqLines, "\n", false);
			if (objStr.Length() > 0) objStr.Split(objLines, "\n", false);
			if (subStr.Length() > 0) subStr.Split(subLines, "\n", false);
			array<int> prereqQ; array<int> objQ; array<int> subQ;
			for (int i = 0; i < prereqLines.Size(); i++) if (prereqLines[i].Length() >= 2 && prereqLines[i].IndexOf("Q\t") == 0) prereqQ.Push(i);
			for (int i = 0; i < objLines.Size(); i++) if (objLines[i].Length() >= 2 && (objLines[i].IndexOf("Q\t") == 0 || objLines[i].IndexOf("O\t") == 0)) objQ.Push(i);
			for (int i = 0; i < subLines.Size(); i++) if (subLines[i].Length() >= 2 && subLines[i].IndexOf("Q\t") == 0) subQ.Push(i);
			// Left pane: top half = quest desc, bottom half = selected objective/prereq/subquest desc (50/50)
			int detailFooterReserve = 66;  // room for 3-line key hint at bottom
			int leftContentH = popupH - detailFooterReserve - 24;
			int leftTopH = leftContentH / 2;
			int descMaxW = leftW - 8;
			int maxLinesTop = leftTopH / rowH;
			if (maxLinesTop < 1) maxLinesTop = 1;
			int objSectionY = sect1Y - 2;  // label for bottom-half left (aligned with right pane bottom half)
			int maxLinesBottom = (popupY + popupH - detailFooterReserve - (sect1Y + 10)) / rowH;
			if (maxLinesBottom < 1) maxLinesBottom = 1;
			// Top left: quest description only (word wrap, clip to half height)
			String questDesc = questDetailQuestDesc;
			if (questDesc.Length() > 200) questDesc = String.Format("%s..", questDesc.Left(198));
			array<String> questWords;
			questDesc.Split(questWords, " ", false);
			DrawWrappedWords(screen, f, Font.CR_WHITE, popupX + 8, popupY + 24, descMaxW, rowH, maxLinesTop, questWords);
			// Bottom left: heading by mode; description from selected item
			String objDesc = "";
			String objLabel = "Objective";
			if (questDetailMode == 0) objLabel = "Objective";
			else if (questDetailMode == 1) objLabel = "Prerequisite Quest";
			else if (questDetailMode == 2) objLabel = "SubQuest";
			if (questDetailMode == 0 && objQ.Size() > 0 && questDetailObjSelected >= 0 && questDetailObjSelected < objQ.Size())
			{
				int idx = objQ[questDetailObjSelected];
				if (idx < objLines.Size()) {
					array<String> parts;
					objLines[idx].Split(parts, "\t", false);
					if (parts.Size() >= 3) objDesc = parts[2];
				}
			}
			else if (questDetailMode == 1 && prereqQ.Size() > 0 && questDetailPrereqSelected >= 0 && questDetailPrereqSelected < prereqQ.Size())
			{
				int idx = prereqQ[questDetailPrereqSelected];
				if (idx < prereqLines.Size()) {
					array<String> parts;
					prereqLines[idx].Split(parts, "\t", false);
					if (parts.Size() >= 3) objDesc = parts[2];
				}
			}
			else if (questDetailMode == 2 && subQ.Size() > 0 && questDetailSubSelected >= 0 && questDetailSubSelected < subQ.Size())
			{
				int idx = subQ[questDetailSubSelected];
				if (idx < subLines.Size()) {
					array<String> parts;
					subLines[idx].Split(parts, "\t", false);
					if (parts.Size() >= 3) objDesc = parts[2];
				}
			}
			screen.DrawText(f, Font.CR_GOLD, popupX + 8, objSectionY, objLabel, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			int objDescY = sect1Y + 10;  // line up with first row of Prereqs/Subquests
			if (objDesc.Length() > 200) objDesc = String.Format("%s..", objDesc.Left(198));
			array<String> objWords;
			objDesc.Split(objWords, " ", false);
			DrawWrappedWords(screen, f, Font.CR_WHITE, popupX + 8, objDescY, descMaxW, rowH, maxLinesBottom, objWords);
			// Right pane: one view per mode. Mode 0 = Objectives + Requirements; Mode 1 = Prereqs only; Mode 2 = Subquests only.
			if (questDetailMode == 0)
			{
				CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
				CVar activeObjIdCv = CVar.FindCVar("odoom_quest_tracker_active_objective_id");
				String trackerQuestId = (trackerIdCv != null) ? trackerIdCv.GetString() : "";
				String activeObjId = (activeObjIdCv != null) ? activeObjIdCv.GetString() : "";
				screen.DrawText(f, Font.CR_GOLD, rightX, sect0Y - 2, "Objectives", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				for (int i = 0; i < maxRowsObj && questDetailObjScroll + i < objQ.Size(); i++)
				{
					int idx = objQ[questDetailObjScroll + i];
					if (idx >= objLines.Size()) break;
					array<String> parts;
					objLines[idx].Split(parts, "\t", false);
					String rowName = parts.Size() >= 3 ? parts[2] : (parts.Size() >= 2 ? parts[1] : "");
					String objId = parts.Size() >= 2 ? parts[1] : "";
					String origName = rowName;
					while (rowName.Length() > 0 && f.StringWidth(rowName) > rightW) rowName = rowName.Left(rowName.Length() - 1);
					if (rowName.Length() < origName.Length()) rowName = String.Format("%s..", rowName);
					bool isActive = (questDetailQuestId.Compare(trackerQuestId) == 0 && objId.Compare(activeObjId) == 0);
					int cr = isActive ? Font.CR_GREEN : ((questDetailObjSelected == questDetailObjScroll + i) ? Font.CR_GOLD : Font.CR_WHITE);
					int rowY = sect0Y + (10 + i * rowH);
					screen.DrawText(f, cr, rightX, rowY, rowName, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				}
				if (objQ.Size() == 0) screen.DrawText(f, Font.CR_GRAY, rightX, sect0Y + 10, "(none)", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				// Requirements / Progress (objective + quest-level dictionaries, e.g. "Killed 3/10 monsters in ODOOM")
				screen.DrawText(f, Font.CR_GOLD, rightX, sect1Y - 2, "Requirements / Progress", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				CVar reqCv = CVar.FindCVar("odoom_quest_detail_requirements");
				String reqStr = (reqCv != null) ? reqCv.GetString() : "";
				array<String> reqLines;
				if (reqStr.Length() > 0 && reqStr.IndexOf("Loading") != 0) reqStr.Split(reqLines, "\n", false);
				int maxRowsReq = (popupY + popupH - detailFooterReserve - (sect1Y + 10)) / rowH;
				if (maxRowsReq < 1) maxRowsReq = 1;
				int reqY = sect1Y + 10;
				if (reqStr.Length() > 0 && reqStr.IndexOf("Loading") == 0)
					screen.DrawText(f, Font.CR_GRAY, rightX, reqY, "Loading...", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				else if (reqLines.Size() == 0)
					screen.DrawText(f, Font.CR_GRAY, rightX, reqY, "(none)", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				else
				{
					int rowsUsed = 0;
					int maxReqPx = rightW - 4;
					for (int ri = 0; ri < reqLines.Size() && rowsUsed < maxRowsReq; ri++)
					{
						String raw = reqLines[ri];
						if (raw.Length() == 0) continue;
						array<String> rw;
						raw.Split(rw, " ", false);
						String cur = "";
						for (int wi = 0; wi < rw.Size() && rowsUsed < maxRowsReq; wi++)
						{
							String nxt = cur.Length() > 0 ? String.Format("%s %s", cur, rw[wi]) : rw[wi];
							if (f.StringWidth(nxt) > maxReqPx && cur.Length() > 0)
							{
								screen.DrawText(f, Font.CR_WHITE, rightX, reqY + rowsUsed * rowH, cur, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
								rowsUsed++;
								cur = rw[wi];
							}
							else
								cur = nxt;
						}
						if (cur.Length() > 0 && rowsUsed < maxRowsReq)
						{
							screen.DrawText(f, Font.CR_WHITE, rightX, reqY + rowsUsed * rowH, cur, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
							rowsUsed++;
						}
					}
				}
			}
			else if (questDetailMode == 1)
			{
				screen.DrawText(f, Font.CR_GOLD, rightX, sect0Y - 2, "Prerequisites", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				for (int i = 0; i < maxRowsSingle && questDetailPrereqScroll + i < prereqQ.Size(); i++)
				{
					int idx = prereqQ[questDetailPrereqScroll + i];
					if (idx >= prereqLines.Size()) break;
					array<String> parts;
					prereqLines[idx].Split(parts, "\t", false);
					String rowName = parts.Size() >= 3 ? parts[2] : (parts.Size() >= 2 ? parts[1] : "");
					while (rowName.Length() > 0 && f.StringWidth(rowName) > rightW) rowName = rowName.Left(rowName.Length() - 1);
					if (rowName.Length() > 20) rowName = String.Format("%s..", rowName.Left(18));
					int cr = (questDetailPrereqSelected == questDetailPrereqScroll + i) ? Font.CR_GOLD : Font.CR_WHITE;
					int rowY = sect0Y + (10 + i * rowH);
					screen.DrawText(f, cr, rightX, rowY, rowName, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				}
				if (prereqQ.Size() == 0) screen.DrawText(f, Font.CR_GRAY, rightX, sect0Y + 10, "(none)", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			else
			{
				screen.DrawText(f, Font.CR_GOLD, rightX, sect0Y - 2, "Sub-quests", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				for (int i = 0; i < maxRowsSingle && questDetailSubScroll + i < subQ.Size(); i++)
				{
					int idx = subQ[questDetailSubScroll + i];
					if (idx >= subLines.Size()) break;
					array<String> parts;
					subLines[idx].Split(parts, "\t", false);
					String rowName = parts.Size() >= 3 ? parts[2] : (parts.Size() >= 2 ? parts[1] : "");
					while (rowName.Length() > 0 && f.StringWidth(rowName) > rightW) rowName = rowName.Left(rowName.Length() - 1);
					if (rowName.Length() > 20) rowName = String.Format("%s..", rowName.Left(18));
					int cr = (questDetailSubSelected == questDetailSubScroll + i) ? Font.CR_GOLD : Font.CR_WHITE;
					int rowY = sect0Y + (10 + i * rowH);
					screen.DrawText(f, cr, rightX, rowY, rowName, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				}
				if (subQ.Size() == 0) screen.DrawText(f, Font.CR_GRAY, rightX, sect0Y + 10, "(none)", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + popupH - 72, "P / O / S: Prereq / Objectives / Subquests", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + popupH - 58, "Arrows: move   Enter: open / go to", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + popupH - 44, "K: start quest or set tracker   Backsp: back", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			return;
		}
		if (questPopupOpen)
		{
			CVar listCv = CVar.FindCVar("odoom_quest_list");
			String listStr = (listCv != null) ? listCv.GetString() : "";
			array<String> drawQuestLines;
			if (listStr.Length() > 0 && listStr.IndexOf("Error:") != 0 && listStr.IndexOf("Loading") != 0)
			{
				array<String> allLines;
				listStr.Split(allLines, "\n", false);
				for (int L = 0; L < allLines.Size(); L++)
				{
					if (allLines[L].Length() >= 2 && allLines[L].IndexOf("Q\t") == 0)
						drawQuestLines.Push(allLines[L]);
				}
			}
			CVar fnCv = CVar.FindCVar("odoom_quest_filter_not_started");
			CVar fiCv = CVar.FindCVar("odoom_quest_filter_in_progress");
			CVar fcCv = CVar.FindCVar("odoom_quest_filter_completed");
			int fn = (fnCv != null) ? fnCv.GetInt() : 1;
			int fi = (fiCv != null) ? fiCv.GetInt() : 1;
			int fc = (fcCv != null) ? fcCv.GetInt() : 1;
			array<int> drawFilteredIndices;
			for (int b = 0; b < drawQuestLines.Size(); b++)
			{
				array<String> parts;
				drawQuestLines[b].Split(parts, "\t", false);
				if (parts.Size() < 5) continue;
				String st = parts[4];
				bool show = ((st.Compare("NotStarted") == 0 || st.Compare("Not Started") == 0) && fn != 0) || ((st.Compare("InProgress") == 0 || st.Compare("In Progress") == 0) && fi != 0) || (st.Compare("Completed") == 0 && fc != 0);
				if (show) drawFilteredIndices.Push(b);
			}
			int qCount = drawFilteredIndices.Size();
			int popupW = 320;  // full width of screen (virtual 320), left-aligned
			int popupH = 200;
			int popupX = -55;  // 55px left so popup is left-aligned to screen edge
			int popupY = 0;
			int rowH = 12;
			int col1X = popupX + 8;
			int nameColW = 32 * 8 + 20 + 5;  // name column: 32 chars + 20px + 5px wider
			int col2X = popupX + 8 + nameColW;
			int col3X = col2X + 6 * 8;  // % then Status
			int maxQuestRows = (popupH - 80) / rowH - 4; // room for 2-line hint
			if (maxQuestRows < 5) maxQuestRows = 5;
			screen.DrawText(f, Font.CR_GOLD, popupX + 8, popupY + 14, "QUESTS", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			String cb1 = (fn != 0) ? "[X] Not Started" : "[ ] Not Started";
			String cb2 = (fi != 0) ? "[X] In Progress" : "[ ] In Progress";
			String cb3 = (fc != 0) ? "[X] Completed" : "[ ] Completed";
			String toggleStr = String.Format("%s  %s  %s", cb1, cb2, cb3);
			int toggleW = f.StringWidth(toggleStr);
			screen.DrawText(f, Font.CR_GRAY, popupX + (popupW - toggleW) / 2 + 60, popupY + 34, toggleStr, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			CVar trackerIdCv = CVar.FindCVar("odoom_quest_tracker_quest_id");
			String trackerQuestId = (trackerIdCv != null) ? trackerIdCv.GetString() : "";
			CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
			int scrollFromCvar = (scrollCv != null) ? scrollCv.GetInt() : 0;
			int newScrollOffset = scrollFromCvar;
			if (listStr.IndexOf("Error:") == 0)
			{
				screen.DrawText(f, Font.CR_RED, popupX + 8, popupY + 48, "Error loading quests. Check console or star_api.log for details.", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			else if (listStr.IndexOf("Loading") == 0)
			{
				String loadMsg = "Loading quests...";
				int loadMsgW = f.StringWidth(loadMsg);
				screen.DrawText(f, Font.CR_GRAY, popupX + (popupW - loadMsgW) / 2 + 70, popupY + 48, loadMsg, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			else if (qCount > 0 && drawQuestLines.Size() > 0)
			{
				screen.DrawText(f, Font.CR_WHITE, col1X, popupY + 48, "Name", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				screen.DrawText(f, Font.CR_WHITE, col2X, popupY + 48, "%", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				screen.DrawText(f, Font.CR_WHITE, col3X, popupY + 48, "Status", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				int drawOffset = scrollFromCvar;
				if (drawOffset < 0) drawOffset = 0;
				if (questSelectedIndex >= drawOffset + maxQuestRows) drawOffset = questSelectedIndex - maxQuestRows + 1;
				if (questSelectedIndex < drawOffset) drawOffset = questSelectedIndex;
				newScrollOffset = drawOffset;
				int y = popupY + 48 + rowH;
				for (int i = 0; i < maxQuestRows && drawOffset + i < drawFilteredIndices.Size(); i++)
				{
					int idx = drawFilteredIndices[drawOffset + i];
					if (idx < 0 || idx >= drawQuestLines.Size()) continue;
					array<String> parts;
					drawQuestLines[idx].Split(parts, "\t", false);
					if (parts.Size() < 6) continue;
					String qName = parts[2];
					String status = parts[4];
					String pctStr = parts.Size() > 5 ? parts[5] : "0";
					String statusDisplay = status.Compare("Completed") == 0 ? "Completed" : (status.Compare("InProgress") == 0 || status.Compare("In Progress") == 0 ? "In Progress" : (status.Compare("NotStarted") == 0 || status.Compare("Not Started") == 0 ? "Not Started" : status));
					if (qName.Length() > 32) qName = String.Format("%s..", qName.Left(30));
					bool selected = (drawOffset + i == questSelectedIndex);
					bool isTracker = (trackerQuestId.Length() > 0 && parts[1].Compare(trackerQuestId) == 0);
					int cr = selected ? Font.CR_GOLD : (isTracker ? Font.CR_GREEN : Font.CR_WHITE);
					if (status.Compare("Completed") == 0) cr = selected ? Font.CR_GREEN : Font.CR_GRAY;
					screen.DrawText(f, cr, col1X, y, qName, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					screen.DrawText(f, cr, col2X, y, String.Format("%s%%", pctStr), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					screen.DrawText(f, cr, col3X, y, statusDisplay, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					y += rowH;
				}
			}
			else
				screen.DrawText(f, Font.CR_GRAY, popupX + 8, popupY + 48, "No Quests Found", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + popupH - 58, "B/N/M=filter  PgUp/PgDn  Home/End  Arrows  Enter  K", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + popupH - 43, "Backspace=back/close  Q=close list", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			if (questStatusFrames > 0 && questStatusMessage.Length() > 0)
			{
				// Same position as toast: top centre of screen
				double statusScale = 0.5;
				int msgW = int(f.StringWidth(questStatusMessage) * statusScale);
				int statusX = 160 - (msgW / 2);
				if (statusX < 2) statusX = 2;
				screen.DrawText(f, Font.CR_GREEN, statusX, 4, questStatusMessage, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, statusScale, DTA_ScaleY, statusScale);
			}
			if (scrollCv != null) scrollCv.SetInt(newScrollOffset);
			return;
		}

		if (!popupOpen) return;

		// When send popup is open, draw only the send popup (no inventory list behind it)
		if (sendPopupMode != 0)
		{
			// Draw send popup only - see below
		}
		else
		{
		// Use cached list from WorldTick (ui cannot call play-context; cache is string-based)
		int starCount = cachedStarCount;       // window size
		int starTotal = cachedStarTotalCount;  // total for scroll math
		int tabSize = cachedLocalCount;
		int listCount = (starTotal > 0) ? (starTotal + tabSize) : (starCount + tabSize);
		int maxOffset = listCount - MAX_VISIBLE_ROWS;
		if (maxOffset < 0) maxOffset = 0;
		int drawOffset = scrollOffset;
		if (drawOffset < 0) drawOffset = 0;
		if (drawOffset > maxOffset) drawOffset = maxOffset;
		int sel = selectedAbsolute;
		if (sel >= listCount && listCount > 0) sel = listCount - 1;
		if (sel < 0) sel = 0;
		int selectedRow = sel - drawOffset;

		int headerX = 160 - (f.StringWidth("OASIS Inventory") / 2);
		screen.DrawText(f, Font.CR_GOLD, headerX, 18, "OASIS Inventory", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

		int tabGap = 10;
		int tabX = -37;  // 5px further left (was -32)
		String tab0 = "Keys";
		String tab1 = "Powerups";
		String tab2 = "Weapons";
		String tab3 = "Ammo";
		String tab4 = "Armor";
		String tab5 = "Items";
		String tab6 = "Monsters";
		int tab0X = tabX;
		int tab1X = tab0X + f.StringWidth(tab0) + tabGap;
		int tab2X = tab1X + f.StringWidth(tab1) + tabGap;
		int tab3X = tab2X + f.StringWidth(tab2) + tabGap;
		int tab4X = tab3X + f.StringWidth(tab3) + tabGap;
		int tab5X = tab4X + f.StringWidth(tab4) + tabGap;
		int tab6X = tab5X + f.StringWidth(tab5) + tabGap;
		screen.DrawText(f, activeTab == TAB_KEYS ? Font.CR_GREEN : Font.CR_GRAY, tab0X, 33, tab0, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_POWERUPS ? Font.CR_GREEN : Font.CR_GRAY, tab1X, 33, tab1, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_WEAPONS ? Font.CR_GREEN : Font.CR_GRAY, tab2X, 33, tab2, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_AMMO ? Font.CR_GREEN : Font.CR_GRAY, tab3X, 33, tab3, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_ARMOR ? Font.CR_GREEN : Font.CR_GRAY, tab4X, 33, tab4, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_ITEMS ? Font.CR_GREEN : Font.CR_GRAY, tab5X, 33, tab5, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_MONSTERS ? Font.CR_GREEN : Font.CR_GRAY, tab6X, 33, tab6, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

		screen.DrawText(f, Font.CR_DARKGRAY, -26, 46, "Arrows=Select E=Use A=Avatar C=Clan I=Close O/P=Tabs", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

		// Parse cache strings into lines for indexing (local arrays only; no array members)
		array<String> starLines;
		array<String> localLines;
		if (cachedStarListForTab.Length() > 0) cachedStarListForTab.Split(starLines, "\n", false);
		if (cachedLocalListForTab.Length() > 0) cachedLocalListForTab.Split(localLines, "\n", false);

		int y = 58;
		for (int i = 0; i < MAX_VISIBLE_ROWS; i++)
		{
			int idx = drawOffset + i;
			if (idx >= listCount) break;

			bool selected = (i == selectedRow);

			// In windowed mode cache holds [scrollOffset..scrollOffset+N); row i = cache index i. Else full list = cache, row i = drawOffset+i.
			int starLineIdx = (starTotal > 0) ? i : idx;
			if (idx < (starTotal > 0 ? starTotal : starCount) && starLineIdx < starLines.Size())
			{
				String line = starLines[starLineIdx];
				screen.DrawText(f, selected ? Font.CR_GOLD : Font.CR_RED, 54, y + 1, line, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			else
			{
				int localIdx = idx - (starTotal > 0 ? starTotal : starCount);
				if (localIdx >= 0 && localIdx < localLines.Size())
				{
					String line = localLines[localIdx];
					screen.DrawText(f, selected ? Font.CR_GOLD : Font.CR_UNTRANSLATED, 54, y + 1, line, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				}
			}
			y += 16;
		}

		String keyLine2 = "PgUp/PgDn=Page Home=Top End=Bottom";
		int keyLine2X = 160 - (f.StringWidth(keyLine2) / 2);
		screen.DrawText(f, Font.CR_DARKGRAY, keyLine2X, 156, keyLine2, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		}

		// Send popup overlay (OQuake-style): show Sending... / Item sent. / Send failed like Quake
		if (sendPopupMode != 0)
		{
			CVar statusCv = CVar.FindCVar("odoom_send_status");
			String sendStatus = (statusCv != null) ? statusCv.GetString() : "";
			bool showingResult = (sendStatus.Length() > 0 && sendStatus.Compare("Sending...") != 0);

			String title = (sendPopupMode == 2) ? "SEND TO CLAN" : "SEND TO AVATAR";
			String label = (sendPopupMode == 2) ? "Clan" : "Username";
			int popupW = 200;
			int popupH = 98;
			int popupX = (320 - popupW) / 2;
			int popupY = (200 - popupH) / 2;
			screen.DrawText(f, Font.CR_GOLD, popupX + 8, popupY + 4, title, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			if (sendStatus.Compare("Sending...") == 0)
				screen.DrawText(f, Font.CR_GREEN, popupX + 8, popupY + 16, "Sending...", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			else if (showingResult)
			{
				int cr = (sendStatus.IndexOf("Send failed") >= 0) ? Font.CR_RED : Font.CR_GREEN;
				screen.DrawText(f, cr, popupX + 8, popupY + 16, sendStatus, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + 28, "Press Enter or I to close", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			else
			{
				if (sendItemDisplayLabel.Length() > 0)
					screen.DrawText(f, Font.CR_WHITE, popupX + 8, popupY + 16, String.Format("Item: %s", sendItemDisplayLabel), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				screen.DrawText(f, Font.CR_UNTRANSLATED, popupX + 8, popupY + 26, String.Format("%s: %s_", label, sendInputLine), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				String qtyText = String.Format("Quantity: %d / %d (PgUp/PgDn=10 Arrows=1)", sendQuantity, sendMaxQty);
				screen.DrawText(f, Font.CR_UNTRANSLATED, popupX + 8, popupY + 38, qtyText, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + 50, "Left=Send  Right=Cancel  Enter=Confirm", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				if (sendButtonFocus == 0)
					screen.DrawText(f, Font.CR_GREEN, popupX + 16, popupY + 66, "[SEND]", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				else
					screen.DrawText(f, Font.CR_GRAY, popupX + 16, popupY + 66, "SEND", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				if (sendButtonFocus == 1)
					screen.DrawText(f, Font.CR_GREEN, popupX + 80, popupY + 66, "[CANCEL]", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
				else
					screen.DrawText(f, Font.CR_GRAY, popupX + 80, popupY + 66, "CANCEL", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
		}
	}
}
