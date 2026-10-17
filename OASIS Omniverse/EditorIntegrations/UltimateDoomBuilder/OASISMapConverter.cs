#region ================== OASIS STAR – Map conversion

/*
 * Converts between OGame map formats and ODOOM (WAD/things).
 * Supported conversion pairs:
 *   OQUAKE .map   ↔ ODOOM  (bidirectional)
 *   OQUAKE2 .ent  →  ODOOM  (entity list import)
 *   OQUAKE3 .map  →  ODOOM  (entity list import)
 *   ODUKE3D CON   →  ODOOM  (actor list — EDuke32 classname mapping)
 *   OWOLF3D DECORATE → ODOOM (ECWolf actor mapping)
 *
 * Quake-family .map format parsing is shared (same brace-block structure).
 * Thing type cross-reference scheme:
 *   ODOOM native types (1-9999) → see OGAssetCatalog for per-game 5xxx-10xxx assignments.
 */

#endregion

#region ================== Namespaces

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.Map;

#endregion

namespace CodeImp.DoomBuilder.UDBScript
{
	public static class OASISMapConverter
	{
		// ── OQUAKE → ODOOM ───────────────────────────────────────────────────────

		private static readonly Dictionary<string, int> QuakeKeyToDoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			{ "key_silver", 13 }, { "key_gold", 5 }
		};

		private static readonly Dictionary<string, int> QuakeItemToDoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			{ "weapon_shotgun",        2001 }, { "weapon_supershotgun",     2001 },
			{ "weapon_nailgun",        2002 }, { "weapon_supernailgun",     2002 },
			{ "weapon_grenadelauncher",2003 }, { "weapon_rocketlauncher",   2003 },
			{ "weapon_lightning",      2004 }, { "item_shells",             2008 },
			{ "item_spikes",           2007 }, { "item_rockets",            2010 },
			{ "item_cells",            2047 }, { "item_health",             2011 },
			{ "item_health_small",     2012 }, { "item_armor1",             2015 },
			{ "item_armor2",           2016 }, { "item_armorInv",           2013 },
			{ "monster_grunt",         3004 }, { "monster_ogre",            9   },
			{ "monster_demon",         3002 }, { "monster_dog",             3002 },
			{ "monster_shambler",      3003 }, { "monster_zombie",          3004 },
			{ "monster_hell_knight",   69   }, { "monster_enforcer",        66  },
			{ "monster_fish",          3005 }, { "monster_spawn",           68  },
		};

		// ── OQUAKE2 → ODOOM ──────────────────────────────────────────────────────

		private static readonly Dictionary<string, int> Quake2ItemToDoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			// Keys
			{ "item_key_blue_key",        13   }, { "item_key_red_key",          38   },
			{ "item_key_commander_head",   5   },
			// Weapons
			{ "weapon_blaster",          2004 }, { "weapon_shotgun",            2001 },
			{ "weapon_supershotgun",     2001 }, { "weapon_machinegun",         2002 },
			{ "weapon_chaingun",         2002 }, { "weapon_grenadelauncher",    2003 },
			{ "weapon_rocketlauncher",   2003 }, { "weapon_hyperblaster",       2004 },
			{ "weapon_railgun",          2004 }, { "weapon_bfg",                2006 },
			// Ammo
			{ "ammo_bullets",            2007 }, { "ammo_shells",               2008 },
			{ "ammo_grenades",           2010 }, { "ammo_rockets",              2010 },
			{ "ammo_cells",              2047 }, { "ammo_slugs",                2007 },
			// Health
			{ "item_health",             2011 }, { "item_health_small",         2012 },
			{ "item_health_mega",        2013 },
			// Armor
			{ "item_armor_jacket",       2015 }, { "item_armor_combat",         2016 },
			{ "item_armor_body",         2013 }, { "item_power_screen",         2015 },
			// Monsters → closest Doom equivalent
			{ "monster_soldier",         3004 }, { "monster_infantry",          9    },
			{ "monster_gunner",          65   }, { "monster_berserker",         3003 },
			{ "monster_gladiator",       3003 }, { "monster_flyer",             3006 },
			{ "monster_medic",           3004 }, { "monster_parasite",          58   },
			{ "monster_brain",           67   }, { "monster_supertank",         16   },
			{ "monster_tank",            16   }, { "monster_tank_commander",    16   },
			{ "monster_boss2",           7    }, { "monster_jorg",              7    },
			{ "monster_makron",          7    },
		};

		// ── OQUAKE3 → ODOOM ──────────────────────────────────────────────────────

		private static readonly Dictionary<string, int> Quake3ItemToDoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			// Weapons
			{ "weapon_rocketlauncher",   2003 }, { "weapon_railgun",            2004 },
			{ "weapon_shotgun",          2001 }, { "weapon_lightning",          2004 },
			{ "weapon_plasmagun",        2004 }, { "weapon_bfg",                2006 },
			{ "weapon_grapplinghook",    2005 },
			// Ammo
			{ "ammo_rockets",            2010 }, { "ammo_slugs",                2007 },
			{ "ammo_shells",             2008 }, { "ammo_lightning",            2047 },
			{ "ammo_plasma",             2047 }, { "ammo_bfg",                  2047 },
			// Health
			{ "item_health",             2011 }, { "item_health_small",         2012 },
			{ "item_health_large",       2013 }, { "item_health_mega",          2013 },
			// Armor
			{ "item_armor_shard",        2015 }, { "item_armor_combat",         2016 },
			{ "item_armor_body",         2013 },
			// Power-ups → Doom sphere equivalents
			{ "item_quad",               2013 }, { "item_regen",                2013 },
			{ "item_haste",              2013 }, { "item_enviro",               2013 },
			{ "item_flight",             2013 }, { "item_invis",                2013 },
		};

		// ── ODUKE3D → ODOOM ──────────────────────────────────────────────────────
		// Duke3D uses Build engine actor names from CON scripting / EDuke32 source

		private static readonly Dictionary<string, int> DukeActorToDoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			// Keys
			{ "BLUEKEY",                 39   }, { "REDKEY",                    38   },
			{ "YELLOWKEY",               40   }, { "ACCESSCARD",                5    },
			// Weapons
			{ "PISTOL",                  2001 }, { "SHOTGUN",                   2001 },
			{ "CHAINGUNSPRITE",          2002 }, { "RPGSPRITE",                 2003 },
			{ "SHRINKERSPRITE",          2004 }, { "DEVISTATORSPRITE",          2004 },
			{ "TRIPBOMBSPRITE",          2005 }, { "FREEZESPRITE",              2004 },
			// Health
			{ "FIRSTAIDKIT",             2012 }, { "ATOMICHEALTH",              2013 },
			{ "HEALTHBOX",               2011 },
			// Enemies → closest Doom counterpart
			{ "LIZTROOP",                3004 }, { "PIGCOP",                    9    },
			{ "PIGCOPBOAT",              9    }, { "LIZMAN",                    3001 },
			{ "COMMANDER",               3005 }, { "OCTABRAIN",                 3005 },
			{ "DRONE",                   3006 }, { "RECON",                     3006 },
			{ "BATTLELORD",              16   }, { "QUEEN",                     7    },
			{ "OVERLORD",                7    },
		};

		// ── OWOLF3D → ODOOM ──────────────────────────────────────────────────────
		// ECWolf uses DECORATE actor classnames

		private static readonly Dictionary<string, int> WolfActorToDoom = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
		{
			// Keys
			{ "GoldKey",                 5    }, { "SilverKey",                 13   },
			// Health / pickups
			{ "Food",                    2012 }, { "FirstAidKit",               2011 },
			{ "GoldenCross",             2015 }, { "SilverCross",               2015 },
			{ "GoldCup",                 2015 }, { "Crown",                     2015 },
			{ "AnkVine",                 2013 },
			// Ammo
			{ "AmmoClip",               2007 }, { "BoxOfAmmo",                 2049 },
			// Enemies → closest Doom counterpart
			{ "Guard",                   3004 }, { "WolfensteinSS",             9    },
			{ "Officer",                 9    }, { "Mutant",                    3001 },
			{ "Dog",                     3002 }, { "HansGrosse",                16   },
			{ "DrSchabbs",               66   }, { "AdolfHitler",               16   },
			{ "MechaHitler",             16   }, { "GretelGrosse",              66   },
			{ "OttoSchabbs",             67   }, { "TransGrosse",               67   },
			{ "FettGesicht",             7    },
		};

		// ══════════════════════════════════════════════════════════════════════════
		// Public conversion entry points
		// ══════════════════════════════════════════════════════════════════════════

		/// <summary>Convert a Quake-family .map file to a Doom thing list text file.</summary>
		public static void ConvertQuakeToDoom(IWin32Window owner)
		{
			ConvertQEngineMapToDoom(owner, "OQUAKE", QuakeKeyToDoom, QuakeItemToDoom);
		}

		/// <summary>Convert a Quake II .ent / .map file to a Doom thing list text file.</summary>
		public static void ConvertQuake2ToDoom(IWin32Window owner)
		{
			ConvertQEngineMapToDoom(owner, "OQUAKE2", null, Quake2ItemToDoom);
		}

		/// <summary>Convert a Quake III .map entity list to a Doom thing list text file.</summary>
		public static void ConvertQuake3ToDoom(IWin32Window owner)
		{
			ConvertQEngineMapToDoom(owner, "OQUAKE3", null, Quake3ItemToDoom);
		}

		/// <summary>
		/// Import a Duke3D CON/EDuke32 actor name list (one classname per line) and output
		/// Doom thing placements. Currently maps classnames only; coordinates are zeroed
		/// since Build engine maps need EDuke32 map extraction for coordinates.
		/// </summary>
		public static void ConvertDukeToDoom(IWin32Window owner)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title = "Select Duke3D actor list (.txt) — one EDuke32 classname per line";
				ofd.Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*";
				if (ofd.ShowDialog(owner) != DialogResult.OK) return;
				string inPath = ofd.FileName;
				string outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + "_doom_things.txt");
				try
				{
					var sb = new StringBuilder();
					sb.AppendLine("# OASIS STAR – Doom thing list from Duke3D actor list: " + Path.GetFileName(inPath));
					sb.AppendLine("# Format: x y type (note: coordinates from Build engine maps are not extracted here)");
					foreach (string line in File.ReadAllLines(inPath))
					{
						string actor = line.Trim();
						if (string.IsNullOrEmpty(actor) || actor.StartsWith("#")) continue;
						if (DukeActorToDoom.TryGetValue(actor, out int doomType))
							sb.AppendLine("0 0 " + doomType + " # " + actor);
					}
					File.WriteAllText(outPath, sb.ToString());
					General.Interface.DisplayStatus(StatusType.Info, "OASIS STAR: Exported thing list to " + outPath);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Conversion failed: " + ex.Message, "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		/// <summary>
		/// Import a Wolf3D ECWolf DECORATE actor list (one classname per line) and output
		/// Doom thing placements. DECORATE classnames are mapped via WolfActorToDoom.
		/// </summary>
		public static void ConvertWolfToDoom(IWin32Window owner)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title = "Select ECWolf DECORATE actor list (.txt) — one classname per line";
				ofd.Filter = "Text file (*.txt)|*.txt|All files (*.*)|*.*";
				if (ofd.ShowDialog(owner) != DialogResult.OK) return;
				string inPath = ofd.FileName;
				string outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + "_doom_things.txt");
				try
				{
					var sb = new StringBuilder();
					sb.AppendLine("# OASIS STAR – Doom thing list from Wolf3D DECORATE actor list: " + Path.GetFileName(inPath));
					sb.AppendLine("# Format: x y type (note: coordinates from Wolf3D grid maps are not extracted here)");
					foreach (string line in File.ReadAllLines(inPath))
					{
						string actor = line.Trim();
						if (string.IsNullOrEmpty(actor) || actor.StartsWith("#")) continue;
						if (WolfActorToDoom.TryGetValue(actor, out int doomType))
							sb.AppendLine("0 0 " + doomType + " # " + actor);
					}
					File.WriteAllText(outPath, sb.ToString());
					General.Interface.DisplayStatus(StatusType.Info, "OASIS STAR: Exported thing list to " + outPath);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Conversion failed: " + ex.Message, "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		/// <summary>Convert current Doom map (or selected WAD) to Quake .map point entities.</summary>
		public static void ConvertDoomToQuake(IWin32Window owner)
		{
			ConvertDoomToQEngineMap(owner, "OQUAKE", "oasis_quake.map");
		}

		/// <summary>Convert current Doom map to Quake II .map point entities.</summary>
		public static void ConvertDoomToQuake2(IWin32Window owner)
		{
			ConvertDoomToQEngineMap(owner, "OQUAKE2", "oasis_quake2.map");
		}

		// ── Shared Q-engine .map → Doom conversion ────────────────────────────────

		private static void ConvertQEngineMapToDoom(IWin32Window owner, string sourceName,
			Dictionary<string, int> keyMap, Dictionary<string, int> itemMap)
		{
			using (var ofd = new OpenFileDialog())
			{
				ofd.Title = "Select " + sourceName + " .map file";
				ofd.Filter = "Quake-family map (*.map,*.ent)|*.map;*.ent|All files (*.*)|*.*";
				if (ofd.ShowDialog(owner) != DialogResult.OK) return;
				string inPath = ofd.FileName;
				string outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + "_doom_things.txt");
				try
				{
					var entities = ParseQuakeMapEntities(inPath);
					var sb = new StringBuilder();
					sb.AppendLine("# OASIS STAR – Doom thing list from " + sourceName + " map: " + Path.GetFileName(inPath));
					sb.AppendLine("# Format: x y type (Doom thing type)");
					foreach (var e in entities)
					{
						if (e.Classname.Equals("worldspawn", StringComparison.OrdinalIgnoreCase)) continue;
						int doomType = 0;
						bool found = (keyMap != null && keyMap.TryGetValue(e.Classname, out doomType))
						          || (itemMap != null && itemMap.TryGetValue(e.Classname, out doomType));
						if (found)
							sb.AppendLine((int)e.OriginX + " " + (int)e.OriginY + " " + doomType + " # " + e.Classname);
					}
					File.WriteAllText(outPath, sb.ToString());
					General.Interface.DisplayStatus(StatusType.Info, "OASIS STAR: Exported thing list to " + outPath);
				}
				catch (Exception ex)
				{
					MessageBox.Show("Conversion failed: " + ex.Message, "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
			}
		}

		// ── Shared Doom → Q-engine .map conversion ────────────────────────────────

		private static void ConvertDoomToQEngineMap(IWin32Window owner, string targetName, string defaultFileName)
		{
			if (General.Map == null)
			{
				MessageBox.Show("Open a map first.", "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string mapPath = General.Map.FilePathName ?? "";
			string outPath = Path.Combine(Path.GetDirectoryName(mapPath) ?? "", Path.GetFileNameWithoutExtension(mapPath) + "_" + targetName.ToLower() + ".map");
			using (var sfd = new SaveFileDialog())
			{
				sfd.Title = "Save as " + targetName + " .map";
				sfd.Filter = "Quake-family map (*.map)|*.map|All files (*.*)|*.*";
				sfd.FileName = Path.GetFileName(outPath);
				sfd.InitialDirectory = Path.GetDirectoryName(outPath) ?? Environment.CurrentDirectory;
				if (sfd.ShowDialog(owner) != DialogResult.OK) return;
				outPath = sfd.FileName;
			}
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine("// OASIS STAR – " + targetName + " .map from current Doom map");
				foreach (Thing t in General.Map.Map.Things)
				{
					string classname = DoomThingTypeToQuakeClassname(t.Type);
					if (classname == "info_null") continue; // skip unknowns
					sb.AppendLine("{");
					sb.AppendLine("  \"classname\" \"" + classname + "\"");
					sb.AppendLine("  \"origin\" \"" + (int)t.Position.x + " " + (int)t.Position.y + " 0\"");
					sb.AppendLine("}");
				}
				File.WriteAllText(outPath, sb.ToString());
				General.Interface.DisplayStatus(StatusType.Info, "OASIS STAR: Exported to " + outPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Conversion failed: " + ex.Message, "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		// ── Reverse lookup: Doom thing type → Quake classname ─────────────────────

		private static string DoomThingTypeToQuakeClassname(int type)
		{
			switch (type)
			{
				case 5:    return "key_gold";
				case 13:   return "key_silver";
				case 2001: return "weapon_shotgun";
				case 2002: return "weapon_nailgun";
				case 2003: return "weapon_rocketlauncher";
				case 2004: return "weapon_lightning";
				case 2005: return "weapon_grenadelauncher";
				case 2006: return "weapon_bfg"; // Q1 has no BFG; best approximation
				case 2007: return "item_spikes";
				case 2008: return "item_shells";
				case 2010: return "item_rockets";
				case 2011: return "item_health";
				case 2012: return "item_health_small";
				case 2013: return "item_armorInv";
				case 2015: return "item_armor1";
				case 2016: return "item_armor2";
				case 2047: return "item_cells";
				case 3001: return "monster_demon";
				case 3002: return "monster_dog";
				case 3003: return "monster_shambler";
				case 3004: return "monster_grunt";
				case 3005: return "monster_fish";
				case 3006: return "monster_spawn";
				case 9:    return "monster_ogre";
				case 58:   return "monster_demon";
				case 64:   return "monster_hell_knight";
				case 65:   return "monster_hell_knight";
				case 66:   return "monster_enforcer";
				case 67:   return "monster_spawn";
				case 68:   return "monster_spawn";
				case 69:   return "monster_hell_knight";
				case 7:    return "monster_shambler";
				case 16:   return "monster_shambler";
				default:   return "info_null";
			}
		}

		// ── OASIS Portal → Quake .map entity export ───────────────────────────────

		/// <summary>
		/// Export the OASIS portal sidecar data for the current map as oasis_portal entities
		/// in a Quake-family .map snippet that can be merged into a hand-edited .map file.
		/// </summary>
		public static void ExportPortalsToQuakeMap(IWin32Window owner)
		{
			if (General.Map == null)
			{
				MessageBox.Show("Open a map first.", "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string sidecarPath = OASISMapSidecar.GetSidecarPathForDisplay();
			if (string.IsNullOrEmpty(sidecarPath) || !File.Exists(sidecarPath))
			{
				MessageBox.Show(
					"No sidecar file found for this map.\nPlace an OASIS portal first via STAR → Open Portal Panel.",
					"OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
			string outPath = Path.Combine(Path.GetDirectoryName(sidecarPath), "oasis_portals_quake.map");
			using (var sfd = new SaveFileDialog())
			{
				sfd.Title = "Save OASIS portal entities as Quake .map snippet";
				sfd.Filter = "Quake map (*.map)|*.map|All files (*.*)|*.*";
				sfd.FileName = Path.GetFileName(outPath);
				sfd.InitialDirectory = Path.GetDirectoryName(outPath) ?? Environment.CurrentDirectory;
				if (sfd.ShowDialog(owner) != DialogResult.OK) return;
				outPath = sfd.FileName;
			}
			try
			{
				// Locate portal things in the current map
				var sb = new StringBuilder();
				sb.AppendLine("// OASIS portal entities — merge into your .map file");
				foreach (Thing t in General.Map.Map.Things)
				{
					if (t.Type != OASISPortalPanel.PORTAL_THING_TYPE) continue;
					sb.AppendLine("{");
					sb.AppendLine("  \"classname\" \"oasis_portal\"");
					sb.AppendLine("  \"origin\" \"" + (int)t.Position.x + " " + (int)t.Position.y + " 0\"");
					sb.AppendLine("  // destination written in sidecar oasis_{map}.json");
					sb.AppendLine("}");
				}
				File.WriteAllText(outPath, sb.ToString());
				General.Interface.DisplayStatus(StatusType.Info, "OASIS STAR: Portal entities exported to " + outPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show("Export failed: " + ex.Message, "OASIS STAR", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
		}

		// ── Shared .map entity parser (all Q-engine formats) ──────────────────────

		private struct QuakeEntity
		{
			public string Classname;
			public double OriginX, OriginY, OriginZ;
		}

		private static List<QuakeEntity> ParseQuakeMapEntities(string path)
		{
			var list = new List<QuakeEntity>();
			string text = File.ReadAllText(path);
			var blockRegex = new Regex(@"\{([^{}]*(?:\{[^{}]*\}[^{}]*)*)\}", RegexOptions.Singleline);
			var keyValRegex = new Regex(@"""([^""]+)""\s+""([^""]*)""", RegexOptions.Singleline);
			foreach (Match block in blockRegex.Matches(text))
			{
				string inner = block.Groups[1].Value;
				string classname = null;
				double ox = 0, oy = 0, oz = 0;
				foreach (Match kv in keyValRegex.Matches(inner))
				{
					string k = kv.Groups[1].Value.Trim();
					string v = kv.Groups[2].Value.Trim();
					if (k.Equals("classname", StringComparison.OrdinalIgnoreCase)) classname = v;
					if (k.Equals("origin", StringComparison.OrdinalIgnoreCase))
					{
						var parts = v.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
						if (parts.Length >= 3)
						{
							double.TryParse(parts[0], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out ox);
							double.TryParse(parts[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out oy);
							double.TryParse(parts[2], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out oz);
						}
					}
				}
				if (!string.IsNullOrEmpty(classname))
					list.Add(new QuakeEntity { Classname = classname, OriginX = ox, OriginY = oy, OriginZ = oz });
			}
			return list;
		}
	}
}
