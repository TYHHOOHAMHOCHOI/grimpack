"""Append a manifest row for every art file under Assets/_Project/Art that has none (art bible 8.13).

The manifest (art-source/art-manifest.csv) records provenance: the generator or tool, status and
date. Rows already present are left alone. The generator is inferred from the file name.

Run:  python -I tools/art/update_manifest.py
"""

from __future__ import annotations

import csv
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
ART = ROOT / "Assets/_Project/Art"
MANIFEST = ROOT / "art-source/art-manifest.csv"
DATE = "2026-11-15"

RULES = [
    (r"^(char|mon)_", "tools/art/build_characters.py", "Pixellab pixen idle frame (art-source/ai-raw) + whole-pixel edits"),
    (r"^plate_", "tools/art/plates.py", "code-drawn"),
    (r"^item_", "tools/art/build_items.py", "Pixellab pixen raw image (art-source/ai-raw) + palette mapping and outline"),
    (r"^ui_(glyph|chevron|keycap)", "tools/art/ui_glyphs.py", "code-drawn"),
    (r"^ui_(title|btn|panel|cursor)", "tools/art/build_menu_kit.py", "code-drawn"),
    (r"^ui_", "tools/art/build_combat_ui.py", "code-drawn"),
    (r"^prop_", "tools/art/build_combat_ui.py", "code-drawn"),
    (r"^vfx_", "tools/art/build_combat_ui.py", "code-drawn"),
    (r"^font_grimpack-numeral", "tools/art/numerals.py", "code-drawn"),
    (r"^font_grimpack-pixel", "tools/art/pixel_font.py", "code-drawn"),
    (r"^env_title-pool", "tools/art/build_title_light.py", "code-drawn"),
]


def main() -> int:
    with MANIFEST.open(encoding="utf-8", newline="") as f:
        rows = list(csv.reader(f))
    header = rows[0]
    rows = [header] + [r for r in rows[1:] if r and (ROOT / r[0]).exists()]   # drop rows of files that no longer exist
    known = {r[0] for r in rows[1:]}
    added = []
    for p in sorted(ART.rglob("*")):
        if p.suffix not in (".png", ".json", ".ttf") or p.parent.name == "Palette":
            continue
        rel = p.relative_to(ROOT).as_posix()
        if rel in known:
            continue
        for rx, tool, how in RULES:
            if re.match(rx, p.name):
                m = re.search(r"_(\d+x\d+|9s|\d+)\.", p.name)
                added.append([rel, p.name.split("_")[0], m.group(1) if m else "", "pal_master_v01", "gen", tool, how, "n/a", "n/a", DATE, "generator rule"])
                break
    with MANIFEST.open("w", encoding="utf-8", newline="") as f:
        csv.writer(f).writerows(rows + added)
    print(f"added {len(added)} rows ({len(known)} already present)")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
