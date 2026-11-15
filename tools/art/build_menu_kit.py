"""Build the menu UI kit and the title mock-up in the master palette.

Draws (art bible 3.6, 7.11, 8.8): the panel 9-slice, the five button states and
the cursor, all as RGBA PNGs with binary alpha. Composes the title mock-up over
the quantised title backdrop and audits it against the palette and the soft
limits (4.3).

Text in the mock-up and the title wordmark use the project's own pixel face
(tools/art/pixel_font.py). The wordmark is an art asset drawn from the face's
capitals at 4x blocks (a logo, not running text), working title only.

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
import pixel_font as PF  # noqa: E402

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


_GLYPHS = PF.build_glyphs()


def text(im: Img, s: str, x: int, y: int, colour: str) -> int:
    """Draw s with the Body 12 face; (x, y) is the top-left of the 12 px line box. Returns the end x."""
    for ch in s:
        bm, adv = _GLYPHS[ord(ch)]
        for cx, cy in bm.px:
            im.px(x + cx, y + cy, colour)
        x += adv
    return x


def wordmark(word: str = "GRIMPACK", block: int = 4) -> Img:
    """Title wordmark: the face's capitals at block x block pixels, Bone with a 1 px INK-0 outline."""
    cols = sum(_GLYPHS[ord(c)][1] for c in word) - 1
    body_rows = 6  # capitals occupy rows 4-9
    w, h = cols * block + 2, body_rows * block + 2
    mask = np.zeros((h, w), bool)
    x = 0
    for ch in word:
        bm, adv = _GLYPHS[ord(ch)]
        for cx, cy in bm.px:
            if 4 <= cy <= 9:
                y0 = (cy - 4) * block + 1
                mask[y0:y0 + block, (x + cx) * block + 1:(x + cx + 1) * block + 1] = True
        x += adv
    grown = mask.copy()
    for dy in (-1, 0, 1):
        for dx in (-1, 0, 1):
            grown |= np.roll(np.roll(mask, dy, 0), dx, 1)
    im = Img(w, h)
    for yy in range(h):
        for xx in range(w):
            if mask[yy, xx]:
                im.px(xx, yy, "BONE")
            elif grown[yy, xx]:
                im.px(xx, yy, "INK-0")
    return im


LABEL_COLOUR = {"default": "BONE", "hover": "BONE", "pressed": "INK-0", "disabled": "INK-3", "focus": "BONE"}


def main() -> int:
    panel_9s().save(UI_DIR / "ui_panel_default_9s.png")
    size = f"{BTN_W + 2 * BTN_PAD}x{BTN_H + 2 * BTN_PAD}"
    for st in STATES:
        button(st).save(UI_DIR / f"ui_btn_{st}_{size}.png")
    cursor().save(UI_DIR / "ui_cursor_default_13x13.png")
    wm = wordmark()
    wm.save(UI_DIR / f"ui_title-wordmark_default_{wm.w}x{wm.h}.png")

    # kit sheet (evidence only)
    sheet = Img(400, 150, "INK-0")
    sheet.paste(panel_9s(), 8, 8)
    sheet.paste(cursor(), 8, 40)
    y = 8
    for st in STATES:
        sheet.paste(button(st), 40, y - BTN_PAD)
        text(sheet, "Chơi", 40 + BTN_PAD + 10, y + 4, LABEL_COLOUR[st])
        text(sheet, st, 40 + BTN_PAD + BTN_W + 14, y + 4, "FOG")
        y += 28
    sheet.save(EVIDENCE / f"ui_kit_sheet_{DATE}.png")

    # title mock-up
    if not BG.exists():
        print(f"missing {BG}; run quantise.py first")
        return 1
    t = Img(640, 360)
    t.a[:, :, :3] = np.array(Image.open(BG).convert("RGB"))
    t.a[:, :, 3] = 255
    t.paste(wordmark(), 44, 40)  # Bone on ink, no glow (2.3.1)
    by = 150
    for i, label in enumerate(["Chơi", "Cài đặt", "Thoát"]):
        st = "focus" if i == 0 else "default"
        t.paste(button(st), 52 - BTN_PAD, by - BTN_PAD)
        text(t, label, 52 + 10, by + 4, "BONE")
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
