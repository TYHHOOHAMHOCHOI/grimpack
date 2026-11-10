"""Master palette for Grimpack (working title), version v01.

Single source of truth for the 41 colours defined in design/art/art-bible.md
(Sections 4.2 to 4.8). Run this file to regenerate:

    Assets/_Project/Art/Palette/pal_master_v01_48.gpl
    Assets/_Project/Art/Palette/pal_master_v01_strip.png

Every hex value below is a PROPOSAL until swatches are viewed on a real
monitor (art bible 4.17). Changing any value is a breaking change: bump the
version in the file name, and re-quantise and re-review every asset.
"""

from __future__ import annotations

import sys
from pathlib import Path

# name -> hex, in palette order. The order is the .gpl order and the strip order.
PALETTE: dict[str, str] = {
    # Ink ramp (world and UI field) and Slate (structure)
    "INK-0": "#0B0F1C",  # ink floor
    "INK-1": "#151B2E",
    "INK-2": "#222B44",
    "INK-3": "#36415F",
    "INK-4": "#5A6785",  # brightest chrome allowed
    # Cold light
    "FOG": "#8795B3",
    "FOG-HI": "#AAB8D4",
    # Neutral light
    "BONE": "#E4DAC0",
    "CEILING": "#F2E8CE",  # light ceiling (payoff bloom core only)
    # Warm "you" light
    "CANDLE": "#EFC186",
    "CANDLE-D": "#7C5C3C",
    # HP
    "BLOOD": "#B0505A",
    "BLOOD-D": "#4E2229",
    # Curse
    "PLUM": "#2A1830",
    "PLUM-E": "#8A5C96",
    # Ambient damp (ink-class)
    "MOSS": "#2E3820",
    "MOSS-HI": "#4A5E34",
    # Red-room Wine ramp and glow
    "WINE-0": "#14080F",
    "WINE-1": "#200F1A",
    "WINE-2": "#311826",
    "WINE-3": "#4B2638",
    "WINE-4": "#75405A",
    "RED-GLOW": "#B02A40",
    # Tier ramps: base, highlight, edge
    "WHITE-BASE": "#C0C8D8",
    "WHITE-HI": "#DDE3EE",
    "WHITE-EDGE": "#4A5470",
    "GREEN-BASE": "#3CB878",
    "GREEN-HI": "#8CF0B4",
    "GREEN-EDGE": "#14502F",
    "BLUE-BASE": "#3E9BE8",
    "BLUE-HI": "#9CCBFF",
    "BLUE-EDGE": "#1A3F7A",
    "PURPLE-BASE": "#9A5CEB",
    "PURPLE-HI": "#CFA8FF",
    "PURPLE-EDGE": "#43257A",
    "GOLD-BASE": "#E6B422",
    "GOLD-HI": "#FFE58A",
    "GOLD-EDGE": "#7A5410",
    "RED-BASE": "#E5263F",
    "RED-HI": "#FF8A8F",
    "RED-EDGE": "#6E1020",
}

CAP = 48  # 41 in use, 7 slots reserved
TIERS = ["WHITE", "GREEN", "BLUE", "PURPLE", "GOLD", "RED"]


def rgb(name: str) -> tuple[int, int, int]:
    h = PALETTE[name].lstrip("#")
    return int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)


def srgb_to_linear(c: float) -> float:
    c = c / 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def luminance(c: tuple[int, int, int]) -> float:
    """WCAG relative luminance of an sRGB triple."""
    r, g, b = (srgb_to_linear(v) for v in c)
    return 0.2126 * r + 0.7152 * g + 0.0722 * b


def contrast(a: tuple[int, int, int], b: tuple[int, int, int]) -> float:
    la, lb = luminance(a), luminance(b)
    hi, lo = max(la, lb), min(la, lb)
    return (hi + 0.05) / (lo + 0.05)


def hsv_saturation_value(c: tuple[int, int, int]) -> tuple[float, float]:
    mx, mn = max(c), min(c)
    v = mx / 255.0
    s = 0.0 if mx == 0 else (mx - mn) / mx
    return s, v


DARK_VALUE = 0.25  # below this HSV value, HSV saturation is not meaningful


def notes() -> list[str]:
    """Findings that need an art-bible decision but are not script errors."""
    found: list[str] = []
    for name in PALETTE:
        if name.split("-")[0] in TIERS or name == "RED-GLOW":
            continue
        s, v = hsv_saturation_value(rgb(name))
        if s > 0.60 and v < DARK_VALUE:
            found.append(
                f"NOTE: {name} has HSV saturation {s:.3f} (> 0.60) at value {v:.2f}; "
                "exempt under art bible 4.1 (dark-value exemption: the cap applies at value >= 0.25)."
            )
    return found


def check() -> list[str]:
    """Return a list of problems (empty if none)."""
    problems: list[str] = []
    if len(PALETTE) != 41:
        problems.append(f"expected 41 colours, found {len(PALETTE)}")
    seen: dict[tuple[int, int, int], str] = {}
    for name in PALETTE:
        c = rgb(name)
        if c in seen:
            problems.append(f"duplicate colour {PALETTE[name]}: {seen[c]} and {name}")
        seen[c] = name
        if all(v <= 0x0A for v in c):
            problems.append(f"{name} is at or below the ink floor")
        if all(v >= 0xF6 for v in c):
            problems.append(f"{name} is at or above the light ceiling")
    # Non-tier colours should stay at or below saturation 0.60 (art bible 4.1).
    # Exceptions by design: Red-room glow (marks a room, 4.5 item 4).
    for name in PALETTE:
        if name.split("-")[0] in TIERS or name == "RED-GLOW":
            continue
        s, v = hsv_saturation_value(rgb(name))
        if s > 0.60 and v >= DARK_VALUE:
            problems.append(f"{name} has saturation {s:.2f} (> 0.60) but is not a tier colour")
    return problems


def write_gpl(path: Path) -> None:
    lines = ["GIMP Palette", "Name: pal_master_v01", "Columns: 8", "#",
             "# Grimpack master palette v01 (41 colours in use, 7 reserved, cap 48).",
             "# Generated by tools/art/palette.py; do not edit by hand."]
    for name in PALETTE:
        r, g, b = rgb(name)
        lines.append(f"{r:3d} {g:3d} {b:3d} {name}")
    path.write_text("\n".join(lines) + "\n", encoding="utf-8")


def write_strip(path: Path) -> None:
    from PIL import Image

    img = Image.new("RGBA", (CAP, 1), (0, 0, 0, 0))
    for i, name in enumerate(PALETTE):
        img.putpixel((i, 0), rgb(name) + (255,))
    img.save(path)


def main() -> int:
    problems = check()
    for p in problems:
        print("PROBLEM:", p)
    for n in notes():
        print(n)
    root = Path(__file__).resolve().parents[2]
    out = root / "Assets" / "_Project" / "Art" / "Palette"
    out.mkdir(parents=True, exist_ok=True)
    write_gpl(out / "pal_master_v01_48.gpl")
    write_strip(out / "pal_master_v01_strip.png")
    print(f"wrote {len(PALETTE)} colours to {out}")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
