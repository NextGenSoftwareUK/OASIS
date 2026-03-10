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
	private bool wasKeyQDown;
	private bool wasKeyEnterDown;
	private bool wasKeyPgUpDown;
	private bool wasKeyPgDownDown;
	private bool wasKeyHomeDown;
	private bool wasKeyEndDown;
	private bool questPopupOpen;
	private int questSelectedIndex;
	private int questScrollOffset;
	private String questStatusMessage;
	private int questStatusFrames;

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
		int keyUp = 0, keyDown = 0, keyLeft = 0, keyRight = 0, keyUse = 0, keyA = 0, keyC = 0, keyZ = 0, keyX = 0, keyI = 0, keyO = 0, keyP = 0, keyQ = 0, keyEnter = 0;
		int keyPgUp = 0, keyPgDown = 0, keyHome = 0, keyEnd = 0;
		CVar v;
		v = CVar.FindCVar("odoom_key_up"); if (v != null) keyUp = v.GetInt();
		v = CVar.FindCVar("odoom_key_down"); if (v != null) keyDown = v.GetInt();
		v = CVar.FindCVar("odoom_key_left"); if (v != null) keyLeft = v.GetInt();
		v = CVar.FindCVar("odoom_key_right"); if (v != null) keyRight = v.GetInt();
		v = CVar.FindCVar("odoom_key_pgup"); if (v != null) keyPgUp = v.GetInt();
		v = CVar.FindCVar("odoom_key_pgdown"); if (v != null) keyPgDown = v.GetInt();
		v = CVar.FindCVar("odoom_key_home"); if (v != null) keyHome = v.GetInt();
		v = CVar.FindCVar("odoom_key_end"); if (v != null) keyEnd = v.GetInt();
		v = CVar.FindCVar("odoom_key_use"); if (v != null) keyUse = v.GetInt();
		v = CVar.FindCVar("odoom_key_a"); if (v != null) keyA = v.GetInt();
		v = CVar.FindCVar("odoom_key_c"); if (v != null) keyC = v.GetInt();
		v = CVar.FindCVar("odoom_key_z"); if (v != null) keyZ = v.GetInt();
		v = CVar.FindCVar("odoom_key_x"); if (v != null) keyX = v.GetInt();
		v = CVar.FindCVar("odoom_key_i"); if (v != null) keyI = v.GetInt();
		v = CVar.FindCVar("odoom_key_o"); if (v != null) keyO = v.GetInt();
		v = CVar.FindCVar("odoom_key_p"); if (v != null) keyP = v.GetInt();
		v = CVar.FindCVar("odoom_key_q"); if (v != null) keyQ = v.GetInt();
		v = CVar.FindCVar("odoom_key_enter"); if (v != null) keyEnter = v.GetInt();
		bool keyUpPressed = (keyUp != 0) && !wasKeyUpDown;
		bool keyPgUpPressed = (keyPgUp != 0) && !wasKeyPgUpDown;
		bool keyPgDownPressed = (keyPgDown != 0) && !wasKeyPgDownDown;
		bool keyHomePressed = (keyHome != 0) && !wasKeyHomeDown;
		bool keyEndPressed = (keyEnd != 0) && !wasKeyEndDown;
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
		bool keyQPressed = (keyQ != 0) && !wasKeyQDown;
		bool keyEnterPressed = (keyEnter != 0) && !wasKeyEnterDown;
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
		wasKeyQDown = (keyQ != 0);
		wasKeyEnterDown = (keyEnter != 0);
		wasKeyPgUpDown = (keyPgUp != 0);
		wasKeyPgDownDown = (keyPgDown != 0);
		wasKeyHomeDown = (keyHome != 0);
		wasKeyEndDown = (keyEnd != 0);

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
				CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
				if (scrollCv != null) scrollCv.SetInt(0);
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
		if (questPopupOpen)
		{
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
			/* Handle filter toggles whenever popup is open so they work even when list is empty (e.g. after turning off Not Started). */
			if (keyHomePressed) {
				CVar cv = CVar.FindCVar("odoom_quest_filter_not_started");
				if (cv != null) cv.SetInt(cv.GetInt() != 0 ? 0 : 1);
			}
			if (keyEndPressed) {
				CVar cv = CVar.FindCVar("odoom_quest_filter_completed");
				if (cv != null) cv.SetInt(cv.GetInt() != 0 ? 0 : 1);
			}
			if (keyPgUpPressed) {
				CVar cv = CVar.FindCVar("odoom_quest_filter_in_progress");
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
			if (qCount > 0)
			{
				if (keyDownPressed) { questSelectedIndex++; if (questSelectedIndex >= qCount) questSelectedIndex = qCount - 1; }
				if (keyUpPressed) { questSelectedIndex--; if (questSelectedIndex < 0) questSelectedIndex = 0; }
				if (keyEnterPressed)
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
							}
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
	}

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

		// XP at far right of screen when beamed in (always visible during play)
		CVar beamedVar = CVar.FindCVar("odoom_star_beamed_in");
		CVar xpVar = CVar.FindCVar("odoom_star_avatar_xp");
		if (beamedVar != null && beamedVar.GetInt() != 0 && xpVar != null)
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

		// Quest Tracker: left side near top, only when beamed in and we have a quest title (no "Q = Quests" hint)
		CVar trackerTitleCv = CVar.FindCVar("odoom_quest_tracker_title");
		CVar trackerObjCv = CVar.FindCVar("odoom_quest_tracker_objective");
		bool beamedIn = (beamedVar != null && beamedVar.GetInt() != 0);
		String qTitle = (trackerTitleCv != null) ? trackerTitleCv.GetString() : "";
		if (beamedIn && qTitle.Length() > 0)
		{
			int trackX = 4;
			int trackY = 22;
			double trackScale = 0.5;
			screen.DrawText(f, Font.CR_GOLD, trackX, trackY, qTitle, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, trackScale, DTA_ScaleY, trackScale);
			String qObj = (trackerObjCv != null) ? trackerObjCv.GetString() : "";
			if (qObj.Length() > 0)
				screen.DrawText(f, Font.CR_WHITE, trackX, trackY + 10, qObj, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_ScaleX, trackScale, DTA_ScaleY, trackScale);
		}

		// Quest popup (Q key): same layout as OQuake - table Name | % | Status, left-aligned, "Starting quest..." bottom-right
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
			int popupW = 200;
			int popupH = 200;
			int popupX = 8;
			int popupY = 0;
			int rowH = 12;
			int col1X = popupX + 8;
			int col2X = popupX + 8 + 16 * 8;
			int col3X = popupX + 8 + 22 * 8;
			int maxQuestRows = (popupH - 80) / rowH;
			if (maxQuestRows < 8) maxQuestRows = 8;
			screen.DrawText(f, Font.CR_GOLD, popupX + 8, popupY + 4, "QUESTS", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			String cb1 = (fn != 0) ? "[X] Not Started" : "[ ] Not Started";
			String cb2 = (fi != 0) ? "[X] In Progress" : "[ ] In Progress";
			String cb3 = (fc != 0) ? "[X] Completed" : "[ ] Completed";
			screen.DrawText(f, Font.CR_GRAY, popupX + 8, popupY + 24, String.Format("%s  %s  %s", cb1, cb2, cb3), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			CVar scrollCv = CVar.FindCVar("odoom_quest_scroll_offset");
			int scrollFromCvar = (scrollCv != null) ? scrollCv.GetInt() : 0;
			int newScrollOffset = scrollFromCvar;
			if (listStr.IndexOf("Error:") == 0)
			{
				screen.DrawText(f, Font.CR_RED, popupX + 8, popupY + 48, "Error loading quests. Check console or star_api.log for details.", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			}
			else if (listStr.IndexOf("Loading") == 0)
			{
				screen.DrawText(f, Font.CR_GRAY, popupX + 8, popupY + 48, "Loading quests...", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
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
					if (qName.Length() > 14) qName = String.Format("%s..", qName.Left(12));
					bool selected = (drawOffset + i == questSelectedIndex);
					int cr = selected ? Font.CR_GOLD : Font.CR_WHITE;
					if (status.Compare("Completed") == 0) cr = selected ? Font.CR_GREEN : Font.CR_GRAY;
					screen.DrawText(f, cr, col1X, y, qName, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					screen.DrawText(f, cr, col2X, y, String.Format("%s%%", pctStr), DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					screen.DrawText(f, cr, col3X, y, statusDisplay, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
					y += rowH;
				}
			}
			else
				screen.DrawText(f, Font.CR_GRAY, popupX + 8, popupY + 48, "No Quests Found", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_DARKGRAY, popupX + 8, popupY + popupH - 20, "Home/End/PgUp=Filter  Arrows=Select  Enter=Start or Set tracker  Q=Close", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			if (questStatusFrames > 0 && questStatusMessage.Length() > 0)
			{
				int msgW = f.StringWidth(questStatusMessage);
				int statusX = popupX + popupW - msgW - 8;
				if (statusX < popupX + 8) statusX = popupX + 8;
				screen.DrawText(f, Font.CR_GREEN, statusX, popupY + popupH - 16, questStatusMessage, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
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
