# TimoRides Pitch Deck Automation

This module turns the structured schema in `../timo_rides_new_deck.schema.yaml` into a live PowerPoint deck. It follows the same automation pattern as our CV and UAT one‑pager generators.

## Folder Layout

- `generate_pitch_deck.py` – main script that reads a schema and renders a `.pptx`.
- `templates/` – optional PowerPoint templates to override the default layout (drop a `.pptx` file here if you want custom master slides).
- `output/` – generated decks are written here by default (git-ignored).

## Quick Start

1. **Create a virtual environment**
   ```bash
   cd "/Volumes/Storage 1/OASIS_CLEAN"
   python3 -m venv /tmp/oasis_venv
   source /tmp/oasis_venv/bin/activate
   ```

2. **Install dependencies**
   ```bash
   pip install python-pptx pyyaml
   ```

3. **Generate the deck**
   ```bash
   cd "/Volumes/Storage 1/OASIS_CLEAN/TimoRides/automation/pitch_deck"
   python generate_pitch_deck.py \
     --schema ../timo_rides_new_deck.schema.yaml \
     --output output/TimoRides_Smart_Mobility_Intelligence.pptx
   ```

4. **Customize**
   - Update the YAML schema to tweak slide copy or data.
   - Provide `--template templates/<file>.pptx` to apply bespoke slide masters, colors, and fonts.

## Schema Mapping

Every entry under `slides:` in the YAML file includes a `layout` key. The generator groups layouts into these buckets:

| Layout Family | Slide Style |
| --- | --- |
| `cover` | Title slide (title + subtitle + optional footer) |
| Narrative (`narrative`, `mission_perspective`, `closing`) | Title with rich body text |
| Bullets (`bullet_summary`, `mission_targets`, `growth_levers`, `risk_mitigation`, etc.) | Title + bullet list |
| Tables/Stats (`market_beachhead`, `unit_economics_snapshot`, `market_opportunity`, `fundraising`, `revenue_model`) | Title + formatted bullet or table output |
| Multi-section (`differentiators_grid`, `problem_landscape`, `product_flow`) | Title + multi-level bullets with section headers |
| Team (`team_core`, `team_extended`, `advisors`) | Title + bullet list per person (Name – Role – Blurb) |
| Metrics (`traction`) | Title + bullet list of key numbers |

The script already contains layout handlers for all layouts present in the current schema. Unknown layouts fall back to a generic title + paragraph rendering.

## Updating or Extending

1. Add/edit slides in `timo_rides_new_deck.schema.yaml`.
2. Run `generate_pitch_deck.py` with the updated schema.
3. If you introduce a new `layout` value, update the `LAYOUT_HANDLERS` map in the script to control its formatting.

## Assets

Add imagery or icon placeholders manually after generation, or adapt the script to inject images by extending the handlers (see the TODOs inside the Python file).

## Next Steps

- Connect the generator to a brand PowerPoint template once final designs are ready.
- Automate image/icon placement from a manifest.
- Export to PDF automatically after deck creation (python-pptx creates PPTX; PDF export would require PowerPoint or LibreOffice on a runner).

Happy pitching!


