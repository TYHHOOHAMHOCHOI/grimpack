"""Enemy debuff UI (Poison, Burn, Frost, Slow, Weaken), the detonate glyph, the upgrade ("Ren") drawer and the
upgraded-item badge (user request 2026-11-17; art director's decisions of the same date).

Writes to Assets/_Project/Art/UI:
  ui_glyph-{burn,frost,slow,weaken,detonate,anvil}_bone_{12x12,8x8}, ui_glyph-scale_bone_8x8
  ui_chip-status_{poison,burn,frost,slow,weaken}_9s           24 x 12 horizontal 3-slice, border (3, 0, 3, 0)
  ui_upgrade-frame_{default,active,hover,invalid}_9s          24 x 24, border (8, 8, 8, 8), sell-frame brackets
  ui_upgrade-anvil_{default,active,strike}_48x28              same canvas and hi/lo scheme as ui_sell-scale
  ui_item-plus_bone_7x7
and to Assets/_Project/Art/VFX:
  vfx_status_mote-{poison,burn,frost,slow,weaken}_{01,02}     recolours of vfx_status_mote_01/02
plus the contact sheet production/qa/evidence/status_forge_sprites_2026-11-17.png (4x, INK-1, greyscale copy below).

Poison reuses the existing droplet glyph (ui_glyph-droplet_bone_*, written by ui_glyphs.py); it is not rewritten here.

Glyph skin: as ui_glyphs.skin (Bone fill, Fog shade on the lower and right edge, 1 px INK-0 outline), except that a
1 px stroke keeps its Bone: a pixel is shaded only when it is the lower or right edge of a part at least 2 px thick.
The two tab glyphs at 8 x 8 (anvil, scale) are flat Bone on the full 8 x 8 with no outline, as the cog and the
drawer-tab arrows they sit beside.

Every colour is an exact palette entry and alpha is 0 or 255; validate() checks each file before anything is written.

Run:  python -I tools/art/build_status_ui.py
"""

from __future__ import annotations

import re
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import palette as P  # noqa: E402
import ui_glyphs as G  # noqa: E402
from build_combat_ui import Cv  # noqa: E402
from build_run_ui import Sheet, slice9  # noqa: E402

UI = ROOT / "Assets/_Project/Art/UI"
VFX = ROOT / "Assets/_Project/Art/VFX"
SHEET = ROOT / "production/qa/evidence/status_forge_sprites_2026-11-17.png"

# 9-slice borders in Unity order (left, bottom, right, top).
STATUS_CHIP_BORDER = (3, 0, 3, 0)    # horizontal 3-slice at its native 12 px height, as ui_chip_base_9s
UPGRADE_FRAME_BORDER = (8, 8, 8, 8)  # as ui_sell-frame_*_9s

# Debuff colours (art director 2026-11-17): mid frames the chip, mid and shade recolour the mote.
DEBUFFS: dict[str, tuple[str, str]] = {
    "poison": ("MOSS-HI", "MOSS"),
    "burn": ("CANDLE-D", "BLOOD-D"),   # never bright Candle for Burn
    "frost": ("FOG", "INK-3"),
    "slow": ("INK-4", "INK-2"),
    "weaken": ("WINE-4", "WINE-2"),
}
GLYPH_OF = {"poison": "droplet", "burn": "burn", "frost": "frost", "slow": "slow", "weaken": "weaken"}


# ------------------------------------------------------------------ glyph drawings ('#' = Bone)
GLYPHS10: dict[str, list[str]] = {
    "burn": [          # flame on a flat base, two tips, the left one taller
        "..#.......",
        "..##......",
        ".###......",
        ".####...#.",
        ".#####.##.",
        "#########.",
        "##########",
        "##########",
        "##########",
        ".########.",
    ],
    "frost": [         # six arms (vertical + both diagonals), hollow centre
        "#...#...#",
        ".#..#..#.",
        "..#.#.#..",
        "...###...",
        "...#.#...",
        "...###...",
        "..#.#.#..",
        ".#..#..#.",
        "#...#...#",
    ],
    "slow": [          # hourglass, 2 px waist; the empty row under the cap leaves the sand line below it
        "##########",
        ".#......#.",
        ".########.",
        "..######..",
        "...####...",
        "....##....",
        "...####...",
        "..######..",
        ".########.",
        "##########",
    ],
    "weaken": [        # snapped blade: the tip piece sits 1 px to the right across a 1 px gap
        "......#...",
        ".....##...",
        ".....##...",
        "..........",
        "....##....",
        "....##....",
        "....##....",
        "..######..",
        "....##....",
        "...####...",
    ],
    "detonate": [      # bomb: round body, fuse stem up and right, one spark pixel
        ".......#.#",
        "......#...",
        ".....#....",
        "..####....",
        ".######...",
        "########..",
        "########..",
        "########..",
        ".######...",
        "..####....",
    ],
    "anvil": [         # horn left, flat face, waist, foot
        "##########",
        ".#########",
        "..########",
        "....####..",
        "....####..",
        "...######.",
        "..########",
    ],
}
GLYPHS6: dict[str, list[str]] = {
    "burn": [
        ".#....",
        ".##.#.",
        "###.##",
        "######",
        "######",
        ".####.",
    ],
    "slow": [          # waist 1 px
        "#####",
        ".#.#.",
        ".###.",
        "..#..",
        ".###.",
        "#####",
    ],
    "weaken": [
        "...#..",
        "...#..",
        "......",
        "..#...",
        ".###..",
        "..#...",
    ],
    "detonate": [
        ".....#",
        "...#..",
        ".###..",
        "#####.",
        "#####.",
        ".###..",
    ],
}
FROST7 = [             # 8 px frost: a 7 x 7 asterisk, placed at (0, 0) of the 8 x 8 canvas
    "#..#..#",
    ".#.#.#.",
    "..###..",
    "...#...",
    "..###..",
    ".#.#.#.",
    "#..#..#",
]
FLAT8: dict[str, list[str]] = {   # tab glyphs, flat Bone on the full 8 x 8 (as ui_glyph-cog_bone_8x8)
    "anvil": [
        "........",
        "########",
        ".#######",
        "..######",
        "....##..",
        "...####.",
        "..######",
        "........",
    ],
    "scale": [         # hanging balance: ring, beam, and two pans whose strings meet the rim in a triangle (as the sell scale)
        "........",
        "........",
        "...##...",
        "...##...",
        "########",
        ".#....#.",
        "###..###",
        "........",
    ],
}
PLUS5 = ["..#..", "..#..", "#####", "..#..", "..#.."]


# ------------------------------------------------------------------ helpers
def bits(rows: list[str]) -> np.ndarray:
    return np.array([[c == "#" for c in r] for r in rows], bool)


def shift(m: np.ndarray, dy: int, dx: int) -> np.ndarray:
    """m moved by (dy, dx) with no wrap-around; vacated cells are False."""
    out = np.zeros_like(m)
    h, w = m.shape
    out[max(dy, 0):h + min(dy, 0), max(dx, 0):w + min(dx, 0)] = m[max(-dy, 0):h - max(dy, 0), max(-dx, 0):w - max(dx, 0)]
    return out


def skin_on(inner: np.ndarray, shade: bool = True) -> np.ndarray:
    """Colour a mask that already sits on its final canvas: Bone fill, INK-0 4-connected outline where it fits,
    Fog on the lower and right edge of parts at least 2 px thick (1 px strokes stay Bone)."""
    out = np.zeros((*inner.shape, 4), np.uint8)
    grown = inner | shift(inner, 1, 0) | shift(inner, -1, 0) | shift(inner, 0, 1) | shift(inner, 0, -1)
    out[grown & ~inner] = (*P.rgb("INK-0"), 255)
    out[inner] = (*P.rgb("BONE"), 255)
    if shade:
        below, above = shift(inner, -1, 0), shift(inner, 1, 0)    # below[y] = inner[y + 1]
        right, left = shift(inner, 0, -1), shift(inner, 0, 1)
        edge = inner & ((~below & above) | (~right & left))
        out[edge] = (*P.rgb("FOG"), 255)
    return out


def glyph(name: str, px: int) -> np.ndarray:
    if px == 8 and name == "frost":
        canvas = np.zeros((8, 8), bool)
        canvas[:7, :7] = bits(FROST7)
        return skin_on(canvas)
    if px == 8 and name in FLAT8:
        a = np.zeros((8, 8, 4), np.uint8)
        a[bits(FLAT8[name])] = (*P.rgb("BONE"), 255)
        return a
    size = 10 if px == 12 else 6
    rows = GLYPHS10[name] if px == 12 else GLYPHS6[name]
    canvas = np.zeros((px, px), bool)
    canvas[1:-1, 1:-1] = G.ascii_mask(rows, size)
    return skin_on(canvas)


def plus_badge() -> np.ndarray:
    canvas = np.zeros((7, 7), bool)
    canvas[1:6, 1:6] = bits(PLUS5)
    return skin_on(canvas, shade=False)


def status_chip(kind: str) -> np.ndarray:
    c = Cv(24, 12)
    c.rect(0, 0, 24, 12, "INK-1"); c.frame(0, 0, 24, 12, 1, DEBUFFS[kind][0])
    return c.a


def upgrade_frame(state: str) -> np.ndarray:
    """24 x 24, border 8: the sell frame's thin edge and bracket corners in the forge colours."""
    fill = {"default": "INK-1", "active": "INK-1", "hover": "INK-2", "invalid": "INK-1"}[state]
    edge = {"default": "INK-3", "active": "INK-4", "hover": "CANDLE", "invalid": "INK-3"}[state]
    corner = {"default": "CANDLE-D", "active": "CANDLE", "hover": "BONE", "invalid": "INK-3"}[state]
    c = Cv(24, 24)
    c.rect(2, 2, 20, 20, fill)
    c.frame(2, 2, 20, 20, 1, edge)
    for x0, y0, sx, sy in ((0, 0, 1, 1), (23, 0, -1, 1), (0, 23, 1, -1), (23, 23, -1, -1)):
        for i in range(6):
            c.px(x0 + sx * i, y0, corner); c.px(x0, y0 + sy * i, corner)
            c.px(x0 + sx * i, y0 + sy, corner); c.px(x0 + sx, y0 + sy * i, corner)
        c.px(x0 + sx * 3, y0 + sy * 3, corner)
    return c.a


ANVIL_ROWS = [  # (y, x0, x1, colour) with "hi" / "lo"; lit from the top and the left as the sell scale
    (14, 4, 41, "hi"),                          # face, running out into the horn tip on the left
    (15, 7, 41, "lo"), (16, 10, 41, "lo"), (17, 13, 40, "lo"), (18, 15, 39, "lo"),   # horn underside, body
    *[(y, 21, 33, "lo") for y in range(19, 24)],                                       # waist
    (24, 18, 36, "hi"), (25, 16, 38, "lo"), (26, 15, 39, "lo"), (27, 15, 39, "lo"),   # foot
]
HAMMER_X = 20
HAMMER_Y = {"default": 8, "active": 5, "strike": 10}   # head top; active lifts 3 px, strike rests on the face (y 14)


def upgrade_anvil(state: str) -> np.ndarray:
    hi, lo = ("FOG-HI", "FOG") if state in ("active", "strike") else ("INK-4", "INK-3")
    col = {"hi": hi, "lo": lo}
    c = Cv(48, 28)
    for y, x0, x1, k in ANVIL_ROWS:
        c.rect(x0, y, x1 - x0 + 1, 1, col[k])
        if k == "lo":
            c.px(x0, y, hi)                     # lit left edge
    hx, hy = HAMMER_X, HAMMER_Y[state]
    for s in range(5):                          # handle: 2 px treads stepping up and right from the head's top centre
        x = hx + 3 + 2 * s
        c.rect(x, hy - 1 - s, 2, 1, hi)
        c.rect(x, hy - s, 2, 1, lo)
    c.rect(hx, hy, 6, 4, lo); c.rect(hx, hy, 6, 1, hi); c.rect(hx, hy, 1, 4, hi)   # 6 x 4 head
    if state == "strike":
        c.px(hx - 2, 12, "BONE"); c.px(hx + 7, 11, "BONE")
    return c.a


def recolour_mote(src: np.ndarray, kind: str) -> np.ndarray:
    """Same pixels as vfx_status_mote_0k. Source Bone (core): its upper-left pixel stays Bone, the rest takes the mid.
    Source Fog (arms): the upper and left arms take the mid, the lower and right arms the shade (x + y >= 8)."""
    mid, shade = DEBUFFS[kind]
    out = np.zeros_like(src)
    opaque = src[..., 3] == 255
    bone = opaque & np.all(src[..., :3] == P.rgb("BONE"), axis=-1)
    fog = opaque & np.all(src[..., :3] == P.rgb("FOG"), axis=-1)
    if (opaque & ~bone & ~fog).any():
        raise SystemExit("vfx_status_mote has a colour other than Bone and Fog; the recolour map needs updating")
    ys, xs = np.nonzero(bone)
    tl = int(np.argmin(xs + ys))
    for y, x in zip(ys, xs):
        out[y, x] = (*P.rgb(mid), 255)
    out[ys[tl], xs[tl]] = (*P.rgb("BONE"), 255)
    for y, x in zip(*np.nonzero(fog)):
        out[y, x] = (*P.rgb(shade if x + y >= 8 else mid), 255)
    return out


# ------------------------------------------------------------------ build and validate
Sprite = tuple[np.ndarray, "tuple[int, int, int, int] | None", Path]


def sprites() -> dict[str, Sprite]:
    out: dict[str, Sprite] = {}
    for g in ("burn", "frost", "slow", "weaken", "detonate", "anvil"):
        for px in (12, 8):
            out[f"ui_glyph-{g}_bone_{px}x{px}"] = (glyph(g, px), None, UI)
    out["ui_glyph-scale_bone_8x8"] = (glyph("scale", 8), None, UI)
    for k in DEBUFFS:
        out[f"ui_chip-status_{k}_9s"] = (status_chip(k), STATUS_CHIP_BORDER, UI)
    for st in ("default", "active", "hover", "invalid"):
        out[f"ui_upgrade-frame_{st}_9s"] = (upgrade_frame(st), UPGRADE_FRAME_BORDER, UI)
    for st in ("default", "active", "strike"):
        out[f"ui_upgrade-anvil_{st}_48x28"] = (upgrade_anvil(st), None, UI)
    out["ui_item-plus_bone_7x7"] = (plus_badge(), None, UI)
    for i in (1, 2):
        src = np.array(Image.open(VFX / f"vfx_status_mote_{i:02d}.png").convert("RGBA"))
        for k in DEBUFFS:
            out[f"vfx_status_mote-{k}_{i:02d}"] = (recolour_mote(src, k), None, VFX)
    return out


PALETTE_SET = {P.rgb(n) for n in P.PALETTE}


def validate(name: str, a: np.ndarray, border: tuple[int, int, int, int] | None) -> list[str]:
    problems = []
    alpha = a[..., 3]
    if not np.isin(alpha, (0, 255)).all():
        problems.append(f"{name}: alpha other than 0/255")
    off = {tuple(int(v) for v in c) for c in a[alpha == 255][:, :3]} - PALETTE_SET
    if off:
        problems.append(f"{name}: off-palette colours {sorted(off)}")
    if (a[alpha == 0][:, :3] != 0).any():
        problems.append(f"{name}: transparent pixels carry colour")
    size = re.fullmatch(r"(\d+)x(\d+)", name.rsplit("_", 1)[-1])
    if size and (a.shape[1], a.shape[0]) != (int(size[1]), int(size[2])):
        problems.append(f"{name}: size {a.shape[1]}x{a.shape[0]} does not match its name")
    if name.startswith("vfx_") and a.shape[:2] != (8, 8):
        problems.append(f"{name}: mote is not 8 x 8")
    if "burn" in name and (np.all(a[..., :3] == P.rgb("CANDLE"), axis=-1) & (alpha == 255)).any():
        problems.append(f"{name}: bright Candle in a Burn sprite")
    if border:
        left, bottom, right, top = border
        h, w = a.shape[:2]
        cols = a[:, left:w - right]
        if not (cols == cols[:, :1]).all():
            problems.append(f"{name}: horizontal stretch band is not uniform")
        if top or bottom:
            rows = a[top:h - bottom]
            if not (rows == rows[:1]).all():
                problems.append(f"{name}: vertical stretch band is not uniform")
    return problems


# ------------------------------------------------------------------ contact sheet
def load(path: Path) -> np.ndarray:
    return np.array(Image.open(path).convert("RGBA"))


def build_sheet(sp: dict[str, Sprite]) -> Image.Image:
    s = Sheet(360, 176)
    a = {n: v[0] for n, v in sp.items()}
    old = lambda n: load(UI / f"{n}.png")  # noqa: E731
    # row 1: new glyphs at 12 (top) and 8 (below), droplet for Poison, then the tab glyphs and the badge
    x = 4
    for g in ("droplet", "burn", "frost", "slow", "weaken", "detonate", "anvil"):
        big = old(f"ui_glyph-{g}_bone_12x12") if g == "droplet" else a[f"ui_glyph-{g}_bone_12x12"]
        small = old(f"ui_glyph-{g}_bone_8x8") if g == "droplet" else a[f"ui_glyph-{g}_bone_8x8"]
        s.blit(big, x, 4); s.blit(small, x + 2, 18); x += 15
    x += 6
    s.blit(a["ui_glyph-scale_bone_8x8"], x, 6); s.blit(old("ui_glyph-cog_bone_8x8"), x + 10, 6)
    s.blit(a["ui_item-plus_bone_7x7"], x + 20, 6)
    for i, g in enumerate(("scale", "anvil")):   # tab glyphs on the drawer tab they are made for
        tx = x + 32 + i * 18
        s.blit(old("ui_drawer-tab_default_14x28"), tx, 2)
        s.blit(a[f"ui_glyph-{g}_bone_8x8"], tx + 3, 12)
    cx = x + 72                                    # plus badge on an item cell, top-right corner
    s.blit(old("ui_cell-bed_default_20x20"), cx, 4)
    s.blit(old("ui_glyph-attack_bone_12x12"), cx + 4, 8)
    s.blit(a["ui_item-plus_bone_7x7"], cx + 13, 4)
    # row 2: the 8 px distinctness line-up, new against existing
    x, y = 4, 32
    for g in ("burn", "frost", "slow", "weaken", "droplet", "detonate"):
        s.blit(a[f"ui_glyph-{g}_bone_8x8"] if f"ui_glyph-{g}_bone_8x8" in a else old(f"ui_glyph-{g}_bone_8x8"), x, y)
        x += 10
    s.band(x + 1, y, 1, 8, "INK-3"); x += 5
    for g in ("attack", "sweep", "windup", "multi", "damage", "seal", "speed"):
        s.blit(old(f"ui_glyph-{g}_bone_8x8"), x, y); x += 10
    # row 3: status chips with their glyph at the curse-chip width (30), base chip first; chips at native 24 px
    x, y = 4, 46
    s.blit(slice9(old("ui_chip_base_9s"), STATUS_CHIP_BORDER, 30, 12), x, y); x += 34
    for k in DEBUFFS:
        s.blit(slice9(a[f"ui_chip-status_{k}_9s"], STATUS_CHIP_BORDER, 30, 12), x, y)
        g = GLYPH_OF[k]
        s.blit(a[f"ui_glyph-{g}_bone_8x8"] if f"ui_glyph-{g}_bone_8x8" in a else old(f"ui_glyph-{g}_bone_8x8"), x + 3, y + 2)
        x += 34
    for k in DEBUFFS:
        s.blit(a[f"ui_chip-status_{k}_9s"], x, y); x += 27
    # row 4: motes (01 above 02) on INK-1 and on INK-0, original first
    x, y = 4, 62
    s.band(x + 70, y - 1, 70, 20, "INK-0")
    for bed in (0, 70):
        s.blit(load(VFX / "vfx_status_mote_01.png"), x + bed, y); s.blit(load(VFX / "vfx_status_mote_02.png"), x + bed, y + 9)
        for i, k in enumerate(DEBUFFS):
            s.blit(a[f"vfx_status_mote-{k}_01"], x + bed + 12 + i * 11, y)
            s.blit(a[f"vfx_status_mote-{k}_02"], x + bed + 12 + i * 11, y + 9)
    # row 5: upgrade frames sliced to a drawer-sized 64 x 44, sell frame for comparison
    x, y = 4, 84
    for st in ("default", "active", "hover", "invalid"):
        s.blit(slice9(a[f"ui_upgrade-frame_{st}_9s"], UPGRADE_FRAME_BORDER, 64, 44), x, y); x += 68
    s.blit(slice9(old("ui_sell-frame_default_9s"), UPGRADE_FRAME_BORDER, 64, 44), x, y)
    # row 6: anvil states, the anvil in the active drawer, the sell scale for comparison
    x, y = 4, 134
    for st in ("default", "active", "strike"):
        s.blit(a[f"ui_upgrade-anvil_{st}_48x28"], x, y); x += 52
    s.blit(slice9(a["ui_upgrade-frame_active_9s"], UPGRADE_FRAME_BORDER, 64, 40), x, y - 6)
    s.blit(a["ui_upgrade-anvil_active_48x28"], x + 8, y + 2); x += 70
    s.blit(old("ui_sell-scale_default_48x28"), x, y); s.blit(old("ui_sell-scale_active_48x28"), x + 52, y)
    rgb = Image.fromarray(s.a, "RGBA").convert("RGB")
    grey = rgb.convert("L").convert("RGB")
    both = Image.new("RGB", (rgb.width, rgb.height * 2 + 4), P.rgb("INK-0"))
    both.paste(rgb, (0, 0)); both.paste(grey, (0, rgb.height + 4))
    return both.resize((both.width * 4, both.height * 4), Image.NEAREST)


def main() -> int:
    sp = sprites()
    problems = [p for n, (a, b, _) in sp.items() for p in validate(n, a, b)]
    for p in problems:
        print("PROBLEM:", p)
    if problems:
        return 1
    for n, (a, _, folder) in sp.items():
        folder.mkdir(parents=True, exist_ok=True)
        Image.fromarray(a, "RGBA").save(folder / f"{n}.png")
    SHEET.parent.mkdir(parents=True, exist_ok=True)
    build_sheet(sp).save(SHEET)
    print(f"wrote {len(sp)} sprites and the contact sheet {SHEET}")
    for n, (a, b, folder) in sp.items():
        print(f"  {folder.name}/{n}.png  {a.shape[1]}x{a.shape[0]}" + (f"  border(l,b,r,t)={b}" if b else ""))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
