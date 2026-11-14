"""Build the menu UI kit and the title mock-up in the master palette.

Draws (art bible 3.6, 7.11, 8.8): the panel 9-slice, the five button states and
the cursor, all as RGBA PNGs with binary alpha. Composes the title mock-up over
the quantised title backdrop and audits it against the palette and the soft
limits (4.3).

The 5x7 letters are a PLACEHOLDER. The native pixel font is gated by the
Vietnamese render test (8.9) and does not exist yet; swap them when it does.

Run:  python -I tools/art/build_menu_kit.py
Needs: Assets/_Project/Art/Environment/env_title_bg_640x360.png
       (made by quantise.py from art-source/ai-raw/env_title_bg_640x360_raw.png)
Writes: Assets/_Project/Art/UI/ui_*.png and
        production/qa/evidence/title_mock_2026-11-15.png, ui_kit_sheet_2026-11-15.png
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

UI_DIR = ROOT / "Assets/_Project/Art/UI"
BG = ROOT / "Assets/_Project/Art/Environment/env_title_bg_640x360.png"
EVIDENCE = ROOT / "production/qa/evidence"
DATE = "2026-11-15"

BTN_W, BTN_H, BTN_PAD = 112, 20, 4  # PAD fits the 2 px focus ring plus its 1 px gap
STATES = ["default", "hover", "pressed", "disabled", "focus"]


class Img:
    def __init__(self, w: int, h: int, fill: str | None = None) -> None:
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 4), np.uint8)
        if fill:
            self.a[:, :, :3] = P.rgb(fill)
            self.a[:, :, 3] = 255

    def px(self, x: int, y: int, name: str) -> None:
        if 0 <= x < self.w and 0 <= y < self.h:
            self.a[y, x, :3] = P.rgb(name)
            self.a[y, x, 3] = 255

    def rect(self, x: int, y: int, w: int, h: int, name: str) -> None:
        for j in range(h):
            for i in range(w):
                self.px(x + i, y + j, name)

    def frame(self, x: int, y: int, w: int, h: int, t: int, name: str) -> None:
        self.rect(x, y, w, t, name)
        self.rect(x, y + h - t, w, t, name)
        self.rect(x, y, t, h, name)
        self.rect(x + w - t, y, t, h, name)

    def paste(self, o: "Img", x: int, y: int) -> None:
        for j in range(o.h):
            for i in range(o.w):
                if o.a[j, i, 3] and 0 <= x + i < self.w and 0 <= y + j < self.h:
                    self.a[y + j, x + i] = o.a[j, i]

    def save(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        Image.fromarray(self.a, "RGBA").save(path)


def panel_9s() -> Img:
    """16x16, 9-slice border 4: 3 px INK-3 frame, 1 px INK-4 highlight top-left, INK-1 fill."""
    im = Img(16, 16, "INK-1")
    im.frame(0, 0, 16, 16, 3, "INK-3")
    im.rect(3, 3, 10, 1, "INK-4")
    im.rect(3, 3, 1, 10, "INK-4")
    return im


def button(state: str) -> Img:
    """Plain rectangle. Hover 1 px and focus 2 px Candle ring, 1 px gap (7.11). Pressed is a Candle fill."""
    fill = "CANDLE" if state == "pressed" else "INK-1"
    edge = {"disabled": "INK-3", "pressed": "CANDLE"}.get(state, "INK-4")
    ring = {"hover": 1, "focus": 2}.get(state, 0)
    im = Img(BTN_W + 2 * BTN_PAD, BTN_H + 2 * BTN_PAD)
    if ring:
        im.frame(BTN_PAD - 1 - ring, BTN_PAD - 1 - ring, BTN_W + 2 + 2 * ring, BTN_H + 2 + 2 * ring, ring, "CANDLE")
    im.rect(BTN_PAD, BTN_PAD, BTN_W, BTN_H, fill)
    im.frame(BTN_PAD, BTN_PAD, BTN_W, BTN_H, 1, edge)
    return im


def cursor() -> Img:
    """12 px Bone pointer with INK-0 outline (7.2). Canvas 13x13."""
    rows = ["X...........", "XX..........", "XBX.........", "XBBX........", "XBBBX.......", "XBBBBX......",
            "XBBBBBX.....", "XBBBBBBX....", "XBBBXXXXX...", "XBXBBX......", "XX.XBBX.....", "X...XBBX...."]
    im = Img(13, 13)
    for y, r in enumerate(rows):
        for x, ch in enumerate(r):
            if ch in "BX":
                im.px(x, y, "BONE" if ch == "B" else "INK-0")
    return im


GLYPHS = {
    "A": "01110 10001 10001 11111 10001 10001 10001", "B": "11110 10001 11110 10001 10001 10001 11110",
    "C": "01111 10000 10000 10000 10000 10000 01111", "D": "11110 10001 10001 10001 10001 10001 11110",
    "E": "11111 10000 11110 10000 10000 10000 11111", "F": "11111 10000 11110 10000 10000 10000 10000",
    "G": "01111 10000 10000 10111 10001 10001 01111", "H": "10001 10001 10001 11111 10001 10001 10001",
    "I": "11111 00100 00100 00100 00100 00100 11111", "J": "00111 00010 00010 00010 10010 10010 01100",
    "K": "10001 10010 10100 11000 10100 10010 10001", "L": "10000 10000 10000 10000 10000 10000 11111",
    "M": "10001 11011 10101 10101 10001 10001 10001", "N": "10001 11001 10101 10011 10001 10001 10001",
    "O": "01110 10001 10001 10001 10001 10001 01110", "P": "11110 10001 10001 11110 10000 10000 10000",
    "Q": "01110 10001 10001 10001 10101 10010 01101", "R": "11110 10001 10001 11110 10100 10010 10001",
    "S": "01111 10000 10000 01110 00001 00001 11110", "T": "11111 00100 00100 00100 00100 00100 00100",
    "U": "10001 10001 10001 10001 10001 10001 01110", "V": "10001 10001 10001 10001 10001 01010 00100",
    "W": "10001 10001 10001 10101 10101 11011 10001", "X": "10001 10001 01010 00100 01010 10001 10001",
    "Y": "10001 10001 01010 00100 00100 00100 00100", "Z": "11111 00001 00010 00100 01000 10000 11111",
    " ": "00000 00000 00000 00000 00000 00000 00000",
}


def text(im: Img, s: str, x: int, y: int, colour: str, cell: int = 1, gap: int = 1) -> None:
    """Placeholder lettering. cell > 1 draws each glyph pixel as a block (never a scaled font)."""
    for ch in s.upper():
        for j, row in enumerate(GLYPHS[ch].split()):
            for i, bit in enumerate(row):
                if bit == "1":
                    im.rect(x + i * cell, y + j * cell, cell, cell, colour)
        x += (5 + gap) * cell


LABEL_COLOUR = {"default": "BONE", "hover": "BONE", "pressed": "INK-0", "disabled": "INK-3", "focus": "BONE"}


def main() -> int:
    panel_9s().save(UI_DIR / "ui_panel_default_9s.png")
    size = f"{BTN_W + 2 * BTN_PAD}x{BTN_H + 2 * BTN_PAD}"
    for st in STATES:
        button(st).save(UI_DIR / f"ui_btn_{st}_{size}.png")
    cursor().save(UI_DIR / "ui_cursor_default_13x13.png")

    # kit sheet (evidence only)
    sheet = Img(400, 150, "INK-0")
    sheet.paste(panel_9s(), 8, 8)
    sheet.paste(cursor(), 8, 40)
    y = 8
    for st in STATES:
        sheet.paste(button(st), 40, y - BTN_PAD)
        text(sheet, "PLAY", 40 + BTN_PAD + 8, y + 6, LABEL_COLOUR[st])
        text(sheet, st, 40 + BTN_PAD + BTN_W + 14, y + 6, "FOG")
        y += 28
    sheet.save(EVIDENCE / f"ui_kit_sheet_{DATE}.png")

    # title mock-up
    if not BG.exists():
        print(f"missing {BG}; run quantise.py first")
        return 1
    t = Img(640, 360)
    t.a[:, :, :3] = np.array(Image.open(BG).convert("RGB"))
    t.a[:, :, 3] = 255
    text(t, "GRIMPACK", 44, 40, "BONE", cell=4)  # Bone on ink, no glow (2.3.1)
    by = 150
    for i, label in enumerate(["PLAY", "SETTINGS", "QUIT"]):
        st = "focus" if i == 0 else "default"
        t.paste(button(st), 52 - BTN_PAD, by - BTN_PAD)
        text(t, label, 52 + 10, by + 6, "BONE")
        by += 32
    t.paste(cursor(), 330, 220)
    t.save(EVIDENCE / f"title_mock_{DATE}.png")

    # audit: palette membership, soft limits (4.3), alpha
    pal = {tuple(P.rgb(n)) for n in P.PALETTE}
    bad = 0
    for path in [*UI_DIR.glob("ui_*.png"), BG, EVIDENCE / f"title_mock_{DATE}.png", EVIDENCE / f"ui_kit_sheet_{DATE}.png"]:
        a = np.array(Image.open(path).convert("RGBA")).reshape(-1, 4)
        a = a[a[:, 3] > 0]
        off = {tuple(p) for p in np.unique(a[:, :3], axis=0)} - pal
        soft_alpha = set(np.unique(a[:, 3]).tolist()) - {255}
        floor = bool((a[:, :3] <= 0x0A).all(axis=1).any())
        ceil = bool((a[:, :3] >= 0xF6).all(axis=1).any())
        flag = off or soft_alpha or floor or ceil
        bad += bool(flag)
        print(f"{'FAIL' if flag else 'ok  '} {path.name}: off-palette={len(off)} soft-alpha={sorted(soft_alpha)} floor={floor} ceiling={ceil}")
    return 1 if bad else 0


if __name__ == "__main__":
    raise SystemExit(main())
