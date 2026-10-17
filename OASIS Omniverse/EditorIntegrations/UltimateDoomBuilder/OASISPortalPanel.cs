#region ================== OASIS Portal Panel

/*
 * OASISPortalPanel — places OASIS cross-game portal markers (thing type 5900) in any map.
 *
 * The portal entity (type 5900) acts as the in-map teleport trigger.  The destination
 * (target game, map, and optional spawn coordinates) is written to the per-map sidecar file
 * oasis_{mapname}.json so the OASIS Omniverse kernel can resolve the destination at runtime.
 *
 * Portals work in both directions:
 *   - Doom maps: placed as a thing; sidecar carries destination metadata.
 *   - Quake maps: placed as an oasis_portal point entity (by OASISMapConverter).
 *
 * Tip: leave destination coordinates at 0/0/0 to spawn at the target map's default start.
 */

#endregion

#region ================== Namespaces

using System;
using System.Drawing;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Windows;

#endregion

namespace CodeImp.DoomBuilder.UDBScript
{
	/// <summary>OASIS cross-game portal placement panel. Thing type 5900 = OASIS Portal.</summary>
	public class OASISPortalPanel : UserControl
	{
		/// <summary>OASIS Portal thing type — universal cross-game teleport marker.</summary>
		public const int PORTAL_THING_TYPE = 5900;

		private readonly BuilderPlug plug;
		private ComboBox comboDestGame;
		private TextBox txtDestMap;
		private TextBox txtDestX, txtDestY, txtDestZ;
		private Button btnPlace;
		private Label lblDestGame, lblDestMap, lblDestCoords, lblHelp, lblSidecar;

		// All OASIS Omniverse game IDs the portal can point to
		private static readonly string[] AllGames =
		{
			"ODOOM", "OQUAKE", "OQUAKE2", "OQUAKE2-RTX", "OQUAKE3",
			"ODUKE3D", "ODUKE3D-RT", "OWOLF3D", "ODOOM3", "ODOOM3-BFG"
		};

		public OASISPortalPanel(BuilderPlug builderPlug)
		{
			plug = builderPlug ?? throw new ArgumentNullException(nameof(builderPlug));
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			SuspendLayout();
			BackColor = SystemColors.Control;
			Padding = new Padding(6);
			MinimumSize = new Size(200, 260);
			Size = new Size(260, 300);

			int y = 8;

			lblDestGame = new Label { Text = "Destination game:", AutoSize = true, Location = new Point(6, y) };
			y += 18;
			comboDestGame = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Location = new Point(6, y),
				Width = 240
			};
			foreach (string g in AllGames) comboDestGame.Items.Add(g);
			comboDestGame.SelectedIndex = 0;
			y += 30;

			lblDestMap = new Label { Text = "Destination map / level (e.g. e1m1, q2dm1):", AutoSize = true, Location = new Point(6, y) };
			y += 18;
			txtDestMap = new TextBox { Location = new Point(6, y), Width = 240, Text = "" };
			y += 30;

			lblDestCoords = new Label { Text = "Spawn coords (leave 0 for map start):", AutoSize = true, Location = new Point(6, y) };
			y += 18;

			int coordW = 72;
			txtDestX = new TextBox { Location = new Point(6, y),           Width = coordW, Text = "0" };
			txtDestY = new TextBox { Location = new Point(6 + coordW + 4, y), Width = coordW, Text = "0" };
			txtDestZ = new TextBox { Location = new Point(6 + (coordW + 4) * 2, y), Width = coordW, Text = "0" };
			y += 30;

			btnPlace = new Button
			{
				Text = "⬡ Place portal at cursor",
				Location = new Point(6, y),
				Width = 240,
				Height = 28,
				FlatStyle = FlatStyle.Standard
			};
			btnPlace.Click += BtnPlace_Click;
			y += 36;

			lblSidecar = new Label
			{
				Text = "Destination saved to: oasis_{map}.json",
				AutoSize = true,
				Location = new Point(6, y),
				MaximumSize = new Size(240, 0),
				ForeColor = SystemColors.GrayText,
				Font = new Font(Font.FontFamily, Font.Size - 1f)
			};
			y += 32;

			lblHelp = new Label
			{
				Text = "Click the map after clicking Place. Use STAR → Open Portal Panel.",
				AutoSize = true,
				Location = new Point(6, y),
				MaximumSize = new Size(240, 0),
				ForeColor = SystemColors.GrayText,
				Font = new Font(Font.FontFamily, Font.Size - 1f)
			};

			Controls.Add(lblDestGame);
			Controls.Add(comboDestGame);
			Controls.Add(lblDestMap);
			Controls.Add(txtDestMap);
			Controls.Add(lblDestCoords);
			Controls.Add(txtDestX);
			Controls.Add(txtDestY);
			Controls.Add(txtDestZ);
			Controls.Add(btnPlace);
			Controls.Add(lblSidecar);
			Controls.Add(lblHelp);

			ResumeLayout(false);
		}

		private void BtnPlace_Click(object sender, EventArgs e)
		{
			if (General.Map == null)
			{
				MessageBox.Show("Open a map first.", "OASIS Portal", MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}

			string destGame = comboDestGame.SelectedItem?.ToString() ?? string.Empty;
			string destMap  = txtDestMap.Text.Trim();

			if (string.IsNullOrEmpty(destGame))
			{
				MessageBox.Show("Select a destination game.", "OASIS Portal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			if (!TryParseCoord(txtDestX.Text, out double dx) ||
			    !TryParseCoord(txtDestY.Text, out double dy) ||
			    !TryParseCoord(txtDestZ.Text, out double dz))
			{
				MessageBox.Show("Spawn coordinates must be numbers (leave 0 for default).", "OASIS Portal", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}

			// Stage the portal metadata so BuilderPlug can write the sidecar after placement
			plug.SetPendingPortalPlacement(destGame, destMap, dx, dy, dz);

			string label = string.IsNullOrEmpty(destMap)
				? "OASIS Portal → " + destGame
				: "OASIS Portal → " + destGame + "/" + destMap;

			plug.SetPendingStarPlacement(PORTAL_THING_TYPE, label);

			// Show sidecar path to the user
			string sidecarPath = OASISMapSidecar.GetSidecarPathForDisplay();
			if (!string.IsNullOrEmpty(sidecarPath))
				lblSidecar.Text = "Will save to: " + System.IO.Path.GetFileName(sidecarPath);
		}

		private static bool TryParseCoord(string text, out double value)
		{
			return double.TryParse(
				string.IsNullOrWhiteSpace(text) ? "0" : text.Trim(),
				System.Globalization.NumberStyles.Any,
				System.Globalization.CultureInfo.InvariantCulture,
				out value);
		}
	}
}
