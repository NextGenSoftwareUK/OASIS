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
	private int lastUser4Tick;

	// One-shot flags set by InputProcess, consumed in WorldTick (arrow keys + Z/X)
	private bool arrowUpPressed;
	private bool arrowDownPressed;
	private bool arrowLeftPressed;
	private bool arrowRightPressed;
	private bool keyUsePressed;
	private bool keyZPressed;
	private bool keyXPressed;

	// Send popup: 0=none, 1=Send to Avatar, 2=Send to Clan
	private int sendPopupMode;
	private String sendPopupBuffer;
	private String pendingSendItemClass;
	private bool sendPopupSubmit;
	private bool sendPopupCancel;
	private String submitSendTarget;

	const TAB_KEYS = 0;
	const TAB_POWERUPS = 1;
	const TAB_WEAPONS = 2;
	const TAB_AMMO = 3;
	const TAB_ARMOR = 4;
	const TAB_ITEMS = 5;
	const TAB_COUNT = 6;
	const MAX_VISIBLE_ROWS = 7;
	const EV_KeyDown = 1;
	const EV_Char = 4;
	const GK_UP = 11;
	const GK_DOWN = 10;
	const GK_LEFT = 5;
	const GK_RIGHT = 6;
	const GK_RETURN = 13;
	const GK_ESCAPE = 27;
	const GK_BACKSPACE = 8;

	override void OnRegister()
	{
		IsUiProcessor = false;
		RequireMouse = false;
	}

	override bool InputProcess(InputEvent e)
	{
		if (sendPopupMode != 0)
		{
			// In send popup: capture text input, Enter, Escape, Backspace
			if (e.Type == EV_KeyDown || e.Type == EV_Char)
			{
				int k = e.Key;
				if (k == GK_RETURN)
				{
					sendPopupSubmit = true;
					submitSendTarget = sendPopupBuffer;
					return true;
				}
				if (k == GK_ESCAPE)
				{
					sendPopupCancel = true;
					return true;
				}
				if (k == GK_BACKSPACE)
				{
					if (sendPopupBuffer.Length() > 0)
						sendPopupBuffer = sendPopupBuffer.Left(sendPopupBuffer.Length() - 1);
					return true;
				}
				// Printable ASCII 32-126 (append as single-char string via lookup)
				if (k >= 32 && k <= 126 && sendPopupBuffer.Length() < 48)
				{
					String oneChar = GetCharFromCode(k);
					if (oneChar.Length() > 0) sendPopupBuffer += oneChar;
					return true;
				}
			}
			return true;
		}

		if (!popupOpen) return false;

		if (e.Type == EV_KeyDown)
		{
			int k = e.Key;
			if (k == GK_UP) { arrowUpPressed = true; return true; }
			if (k == GK_DOWN) { arrowDownPressed = true; return true; }
			if (k == GK_LEFT) { arrowLeftPressed = true; return true; }
			if (k == GK_RIGHT) { arrowRightPressed = true; return true; }
			// E = use, Z = send to avatar, X = send to clan (by key code / ASCII)
			if (k == 69 || k == 101) { keyUsePressed = true; return true; }
			if (k == 90 || k == 122) { keyZPressed = true; return true; }
			if (k == 88 || k == 120) { keyXPressed = true; return true; }
		}
		return true;
	}

	override void WorldTick()
	{
		if (!playeringame[consoleplayer]) return;
		let p = players[consoleplayer];
		if (!p || !p.mo) return;

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

		if (user1Down && !wasUser1Down)
		{
			popupOpen = !popupOpen;
			if (popupOpen)
			{
				scrollOffset = 0;
				selectedAbsolute = 0;
			}
		}

		if (popupOpen)
		{
			// Tab change: O/P buttons or arrow left/right
			if ((user2Down && !wasUser2Down) || arrowLeftPressed)
			{
				activeTab--;
				if (activeTab < 0) activeTab = TAB_COUNT - 1;
				scrollOffset = 0;
				selectedAbsolute = 0;
			}
			if ((user3Down && !wasUser3Down) || arrowRightPressed)
			{
				activeTab++;
				if (activeTab >= TAB_COUNT) activeTab = 0;
				scrollOffset = 0;
				selectedAbsolute = 0;
			}

			int listCount = 0;
			Inventory selectedItem = null;
			for (let inv = p.mo.Inv; inv != null; inv = inv.Inv)
			{
				if (inv.Amount <= 0) continue;
				bool inTab = false;
				if (activeTab == TAB_KEYS && inv is "Key") inTab = true;
				else if (activeTab == TAB_POWERUPS && inv is "Powerup") inTab = true;
				else if (activeTab == TAB_WEAPONS && inv is "Weapon") inTab = true;
				else if (activeTab == TAB_AMMO && inv is "Ammo") inTab = true;
				else if (activeTab == TAB_ARMOR && inv is "Armor") inTab = true;
				else if (activeTab == TAB_ITEMS && !(inv is "Key") && !(inv is "Powerup") && !(inv is "Weapon") && !(inv is "Armor") && !(inv is "Ammo")) inTab = true;
				if (!inTab) continue;
				if (listCount == selectedAbsolute) selectedItem = inv;
				listCount++;
			}
			int maxOffset = listCount - MAX_VISIBLE_ROWS;
			if (maxOffset < 0) maxOffset = 0;
			if (selectedAbsolute >= listCount && listCount > 0) selectedAbsolute = listCount - 1;
			if (selectedAbsolute < 0) selectedAbsolute = 0;

			// Selection: arrow keys only (not W/S)
			if (arrowUpPressed)
			{
				selectedAbsolute--;
				if (selectedAbsolute < 0) selectedAbsolute = 0;
				scrollOffset = selectedAbsolute - MAX_VISIBLE_ROWS + 1;
				if (scrollOffset < 0) scrollOffset = 0;
				if (scrollOffset > maxOffset) scrollOffset = maxOffset;
			}
			if (arrowDownPressed)
			{
				selectedAbsolute++;
				if (selectedAbsolute >= listCount) selectedAbsolute = listCount - 1;
				if (selectedAbsolute < 0) selectedAbsolute = 0;
				scrollOffset = selectedAbsolute - MAX_VISIBLE_ROWS + 1;
				if (scrollOffset < 0) scrollOffset = 0;
				if (scrollOffset > maxOffset) scrollOffset = maxOffset;
			}

			if ((useDown && !wasUseDown) || keyUsePressed)
			{
				if (selectedItem != null && selectedItem.Amount > 0)
				{
					p.mo.UseInventory(selectedItem);
					if (selectedAbsolute >= listCount && listCount > 0) selectedAbsolute = listCount - 1;
					if (selectedAbsolute < 0) selectedAbsolute = 0;
				}
			}

			// Z = Send to Avatar popup, X = Send to Clan popup
			if ((keyZPressed || (user4Down && !wasUser4Down)) && selectedItem != null && selectedItem.Amount > 0)
			{
				sendPopupMode = 1;
				pendingSendItemClass = selectedItem.GetClassName();
				sendPopupBuffer = "";
			}
			if ((keyXPressed || (reloadDown && !wasReloadDown)) && selectedItem != null && selectedItem.Amount > 0)
			{
				sendPopupMode = 2;
				pendingSendItemClass = selectedItem.GetClassName();
				sendPopupBuffer = "";
			}
		}

		// Consume send-popup submit/cancel and run command
		if (sendPopupSubmit && pendingSendItemClass.Length() > 0)
		{
			String target = (submitSendTarget != null && submitSendTarget.Length() > 0) ? submitSendTarget : " ";
			if (sendPopupMode == 1)
				ConsoleCommand(String.Format("star send_avatar \"%s\" \"%s\"", target, pendingSendItemClass));
			else if (sendPopupMode == 2)
				ConsoleCommand(String.Format("star send_clan \"%s\" \"%s\"", target, pendingSendItemClass));
			sendPopupMode = 0;
			sendPopupBuffer = "";
			pendingSendItemClass = "";
			sendPopupSubmit = false;
			submitSendTarget = "";
		}
		if (sendPopupCancel)
		{
			sendPopupMode = 0;
			sendPopupBuffer = "";
			pendingSendItemClass = "";
			sendPopupCancel = false;
		}

		// Clear one-shot flags
		arrowUpPressed = false;
		arrowDownPressed = false;
		arrowLeftPressed = false;
		arrowRightPressed = false;
		keyUsePressed = false;
		keyZPressed = false;
		keyXPressed = false;

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
	}

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
		default: return "Items";
		}
	}

	private ui bool IsItemInActiveTab(Inventory item, int tabIndex)
	{
		if (item == null || item.Amount <= 0) return false;
		if (tabIndex == TAB_KEYS) return item is "Key";
		if (tabIndex == TAB_POWERUPS) return item is "Powerup";
		if (tabIndex == TAB_WEAPONS) return item is "Weapon";
		if (tabIndex == TAB_AMMO) return item is "Ammo";
		if (tabIndex == TAB_ARMOR) return item is "Armor";
		return !(item is "Key") && !(item is "Powerup") && !(item is "Weapon") && !(item is "Armor") && !(item is "Ammo");
	}

	private ui void BuildTabInventory(Actor owner, out Array<Inventory> outItems)
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

	private ui String ItemDisplayName(Inventory item)
	{
		if (item == null) return "";
		if (item is "OQGoldKey") return "Golden Key";
		if (item is "OQSilverKey") return "Silver Key";
		String tag = item.GetTag("");
		if (tag.Length() > 0) return tag;
		return item.GetClassName();
	}

	private String GetCharFromCode(int code)
	{
		if (code < 32 || code > 126) return "";
		// Printable ASCII as single-char string via substring of constant
		String all = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~";
		int idx = code - 32;
		if (idx < 0 || idx >= all.Length()) return "";
		return all.Mid(idx, 1);
	}

	override void RenderOverlay(RenderEvent e)
	{
		if (!popupOpen) return;
		let p = players[consoleplayer];
		if (!p || !p.mo) return;

		Array<Inventory> tabItems;
		BuildTabInventory(p.mo, tabItems);
		int listCount = tabItems.Size();
		int maxOffset = listCount - MAX_VISIBLE_ROWS;
		if (maxOffset < 0) maxOffset = 0;
		int drawOffset = scrollOffset;
		if (drawOffset < 0) drawOffset = 0;
		if (drawOffset > maxOffset) drawOffset = maxOffset;
		int sel = selectedAbsolute;
		if (sel >= listCount && listCount > 0) sel = listCount - 1;
		if (sel < 0) sel = 0;
		int selectedRow = sel - drawOffset;

		Font f = "SmallFont";

		int headerX = 160 - (f.StringWidth("OASIS Inventory") / 2);
		screen.DrawText(f, Font.CR_GOLD, headerX, 18, "OASIS Inventory", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

		int tabGap = 10;
		int tabX = 6;
		String tab0 = "Keys";
		String tab1 = "Powerups";
		String tab2 = "Weapons";
		String tab3 = "Ammo";
		String tab4 = "Armor";
		String tab5 = "Items";
		int tab0X = tabX;
		int tab1X = tab0X + f.StringWidth(tab0) + tabGap;
		int tab2X = tab1X + f.StringWidth(tab1) + tabGap;
		int tab3X = tab2X + f.StringWidth(tab2) + tabGap;
		int tab4X = tab3X + f.StringWidth(tab3) + tabGap;
		int tab5X = tab4X + f.StringWidth(tab4) + tabGap;
		screen.DrawText(f, activeTab == TAB_KEYS ? Font.CR_GREEN : Font.CR_GRAY, tab0X, 33, tab0, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_POWERUPS ? Font.CR_GREEN : Font.CR_GRAY, tab1X, 33, tab1, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_WEAPONS ? Font.CR_GREEN : Font.CR_GRAY, tab2X, 33, tab2, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_AMMO ? Font.CR_GREEN : Font.CR_GRAY, tab3X, 33, tab3, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_ARMOR ? Font.CR_GREEN : Font.CR_GRAY, tab4X, 33, tab4, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		screen.DrawText(f, activeTab == TAB_ITEMS ? Font.CR_GREEN : Font.CR_GRAY, tab5X, 33, tab5, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

		screen.DrawText(f, Font.CR_DARKGRAY, 6, 46, "Arrows=Select  E=Use  Z=To Avatar  X=To Clan  I=Toggle  O/P=Tabs", DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

		int y = 58;
		for (int i = 0; i < MAX_VISIBLE_ROWS; i++)
		{
			int idx = drawOffset + i;
			if (idx >= tabItems.Size()) break;

			bool selected = (i == selectedRow);

			let item = tabItems[idx];
			TextureID icon = item.Icon;
			if (icon.IsValid())
			{
				screen.DrawTexture(icon, true, 40, y, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43, DTA_DestWidth, 10, DTA_DestHeight, 10);
			}

			String itemLabel = ItemDisplayName(item);
			String itemDesc = itemLabel;
			if (item.Amount > 1) itemDesc = String.Format("%s  x%d", itemLabel, item.Amount);
			screen.DrawText(f, selected ? Font.CR_GOLD : Font.CR_UNTRANSLATED, 54, y + 1, itemDesc, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);

			y += 16;
		}

		// Send to Avatar / Send to Clan popup (overlay on top of inventory)
		if (sendPopupMode != 0)
		{
			String title = sendPopupMode == 1 ? "Send to Avatar" : "Send to Clan";
			String prompt = sendPopupMode == 1 ? "Username:" : "Clan name:";
			String hint = "Enter=Send  Escape=Cancel  Backspace=Delete";
			int boxL = 50;
			int boxT = 70;
			int boxW = 220;
			int boxH = 56;
			screen.Clear(boxL, boxT, boxL + boxW, boxT + boxH, color(32, 32, 32), -1);
			screen.DrawText(f, Font.CR_GOLD, boxL + 4, boxT + 2, title, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_UNTRANSLATED, boxL + 4, boxT + 14, prompt, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			String displayBuf = sendPopupBuffer.Length() > 0 ? sendPopupBuffer : "_";
			screen.DrawText(f, Font.CR_WHITE, boxL + 4, boxT + 26, displayBuf, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
			screen.DrawText(f, Font.CR_DARKGRAY, boxL + 4, boxT + 40, hint, DTA_VirtualWidth, 320, DTA_VirtualHeight, 200, DTA_FullscreenScale, FSMode_ScaleToFit43);
		}
	}
}
