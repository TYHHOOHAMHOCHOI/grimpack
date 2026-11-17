"""Run-map UI sprites: door cards, rank pips, curse tag and chip, curse and reward glyphs, monster size marks
and the corridor (floor progress) nodes (art bible 2.3.3, 3.2, 3.4.4, 4.7, 4.8, 7.10, 7.11, 8.8).

Door cards reuse the item-plate corner generator (plates.draw on a 1x1 plate), so a card carries exactly the
tier form of an item of that tier: square (White), one chamfer (Green), a diagonal pair (Blue), all four
(Purple), all four plus inner brackets (Gold). The INK-2 cell bed becomes the INK-1 card body and the plate is
widened to 24 x 24 by repeating its uniform middle row and column, so the corner pieces stay identical.

Everything is drawn from palette entries only, alpha 0 or 255; validate() checks every file before it is
written, and checks that the stretch bands of every 9-slice sprite are uniform.

Run:  python -I tools/art/build_run_ui.py
      python -I tools/art/build_run_ui.py --ui-dir <dir> --sheet <png>    (draft into another folder)
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))
import palette as P  # noqa: E402
import plates  # noqa: E402
import ui_glyphs as G  # noqa: E402
from build_combat_ui import Cv  # noqa: E402

UI = ROOT / "Assets/_Project/Art/UI"
SHEET = ROOT / "production/qa/evidence/run_ui_sprites_2026-11-17.png"
TIERS = ["white", "green", "blue", "purple", "gold"]
RANK = {t: i + 1 for i, t in enumerate(TIERS)}

# 9-slice borders in Unity order (left, bottom, right, top), reported to whoever wires the sprites.
CARD_BORDER = (8, 8, 8, 8)
RING_HOVER_BORDER = (2, 2, 2, 2)     # 1 px ring + 1 px clear gap; rect = card inflated by 2
RING_ARMED_BORDER = (3, 3, 3, 3)     # 2 px ring + 1 px clear gap; rect = card inflated by 3
CURSE_CHIP_BORDER = (3, 0, 3, 0)     # horizontal 3-slice, used at its native 12 px height (as ui_chip_base_9s)


# ------------------------------------------------------------------ helpers
def blank(w: int, h: int) -> np.ndarray:
    return np.zeros((h, w, 4), np.uint8)


def put(a: np.ndarray, m: np.ndarray, name: str) -> None:
    a[m] = (*P.rgb(name), 255)


def is_colour(a: np.ndarray, name: str) -> np.ndarray:
    return np.all(a[..., :3] == P.rgb(name), axis=-1) & (a[..., 3] == 255)


def paint(rows: list[str], legend: dict[str, str]) -> np.ndarray:
    """Character map to RGBA; '.' is transparent."""
    a = blank(len(rows[0]), len(rows))
    for y, row in enumerate(rows):
        assert len(row) == len(rows[0]), rows
        for x, ch in enumerate(row):
            if ch != ".":
                a[y, x] = (*P.rgb(legend[ch]), 255)
    return a


def bits(rows: list[str]) -> np.ndarray:
    return np.array([[c == "#" for c in r] for r in rows], bool)


def widen(a: np.ndarray, at: int, n: int, axis: int) -> np.ndarray:
    """Insert n copies of slice `at` before it (used to grow a uniform middle band)."""
    rep = np.repeat(np.take(a, [at], axis=axis), n, axis=axis)
    return np.concatenate([np.take(a, range(at), axis=axis), rep, np.take(a, range(at, a.shape[axis]), axis=axis)], axis=axis)


def to_cv(a: np.ndarray) -> Cv:
    c = Cv(a.shape[1], a.shape[0])
    c.a = a.copy()
    return c


# ------------------------------------------------------------------ 1-2. door cards, rings, armed corner
def door_card(tier: str) -> np.ndarray:
    """Tier Edge outer line, tier Base 1 px frame, INK-1 body; corners from the plate generator. 24 x 24, border 8."""
    a = plates.draw(tier, "1x1", "rest")                  # 20 x 20, frame at tier Base
    put(a, is_colour(a, "INK-2"), "INK-1")
    return widen(widen(a, 10, 4, 1), 10, 4, 0)


def door_card_disabled() -> np.ndarray:
    """Square corners (no tier is promised), INK-2 outer line, INK-3 frame, INK-1 body."""
    a = door_card("white")
    edge, base = is_colour(a, "WHITE-EDGE"), is_colour(a, "WHITE-BASE")
    put(a, edge, "INK-2"); put(a, base, "INK-3")
    return a


def ring(thick: int, size: int = 12) -> np.ndarray:
    """Candle ring on the sprite's outer edge; the clear gap is the transparent pixel inside it (inside the border)."""
    c = Cv(size, size)
    c.frame(0, 0, size, size, thick, "CANDLE")
    return c.a


def armed_corner() -> np.ndarray:
    a = blank(4, 4); a[:] = (*P.rgb("CANDLE"), 255)
    return a


# ------------------------------------------------------------------ 3. rank pips
def pip(tier: str) -> np.ndarray:
    """5 x 5 diamond, solid tier Base. An INK-0 rim does not fit: it would leave a 5 px plus as the lit shape."""
    a = blank(5, 5)
    for y in range(5):
        for x in range(5):
            if abs(x - 2) + abs(y - 2) <= 2:
                a[y, x] = (*P.rgb(f"{tier.upper()}-BASE"), 255)
    return a


# ------------------------------------------------------------------ 4. curse tag, curse chip, plum fill
def tag(w: int, h: int, notch: tuple[int, int]) -> np.ndarray:
    """Hanging curse tag: Plum body, Plum-E 1 px outline, 1 px rounded corners on the string end and a 2 x 2
    string notch cut into the middle of the left edge (rows notch[0]..notch[1])."""
    m = np.ones((h, w), bool)
    m[0, 0] = m[h - 1, 0] = False
    m[notch[0]:notch[1] + 1, 0:2] = False
    a = blank(w, h)
    put(a, m, "PLUM")
    put(a, m & ~plates.erode4(m), "PLUM-E")
    return a


def plum_fill() -> np.ndarray:
    a = blank(4, 4); a[:] = (*P.rgb("PLUM"), 255)
    return a


# ------------------------------------------------------------------ 5. glyphs (10 px master, 6 px reduction, Bone skin)
GLYPHS10: dict[str, list[str]] = {
    # Pinned: a pushpin (cap, waist, collar, needle). A bare nail read as the letter T at 8 px.
    "curse-pinned": [
        ".########.",
        "..######..",
        "...####...",
        "...####...",
        ".########.",
        "##########",
        "....##....",
        "....##....",
        "....##....",
        "....#.....",
    ],
    # Wither: a shard with the chip that broke off it
    "curse-wither": [
        "...#......",
        "...##.....",
        "..###.....",
        "..####....",
        ".#####....",
        ".######...",
        "#######...",
        "######....",
        "####...##.",
        "##.....##.",
    ],
    # Frailty: the heart mask from ui_glyphs with one crack from the cleft (carved below)
    "curse-frailty": G.ASCII10["heart"],
    # Sluggish: a snapped chain, a whole link with its broken stub, a gap, and the link that came away
    "curse-sluggish": [
        ".###......",
        "##.##.....",
        "#...#.....",
        "##.##.....",
        ".##.#.....",
        "....#.....",
        ".......##.",
        "......##.#",
        "......#..#",
        "......####",
    ],
    # +1 backpack cell: one empty cell with a plus badge at its upper right
    "cell": [
        "......#...",
        "......#...",
        "....#####.",
        "......#...",
        "######.#..",
        "#....#....",
        "#....#....",
        "#....#....",
        "#....#....",
        "######....",
    ],
}
GLYPHS6: dict[str, list[str]] = {
    "curse-pinned": [
        ".####.",
        ".####.",
        "######",
        "..##..",
        "..##..",
        "..#...",
    ],
    "curse-wither": [
        "..#...",
        ".##...",
        ".###..",
        "####..",
        "###.##",
        "##..#.",
    ],
    "curse-frailty": G.ASCII6["heart"],
    "curse-sluggish": [
        ".#....",
        "#.#...",
        ".##...",
        "....##",
        "...#.#",
        "....#.",
    ],
    "cell": [
        "....#.",
        "...###",
        "....#.",
        "####..",
        "#..#..",
        "####..",
    ],
}
CRACK = {10: [(5, 1), (4, 2), (5, 3), (4, 4)], 6: [(2, 1), (3, 2)]}   # frailty crack from the cleft, mask coordinates


def glyph_mask(name: str, size: int) -> np.ndarray:
    rows = GLYPHS10[name] if size == 10 else GLYPHS6[name]
    m = G.ascii_mask(rows, size)
    if name == "curse-frailty":
        h, w = len(rows), max(len(r) for r in rows)
        ox, oy = (size - w) // 2, (size - h) // 2
        for x, y in CRACK[size]:
            m[oy + y, ox + x] = False
    return m


def glyph(name: str, size: int) -> np.ndarray:
    return G.skin(glyph_mask(name, size), "bone")


# ------------------------------------------------------------------ 6. monster size marks and the elite crown (flat Bone)
SIZE_MARKS: dict[str, list[str]] = {
    "l": [
        "#........#",
        "##......##",
        ".########.",
        "##########",
        "##.####.##",
        "##########",
        "##########",
        ".########.",
        ".###..###.",
        ".##....##.",
    ],
    "m": [
        "#......#",
        "##....##",
        ".######.",
        "##.##.##",
        "########",
        "########",
        ".######.",
        ".##..##.",
    ],
    "s": [
        "#....#",
        ".####.",
        "#.##.#",
        "######",
        ".####.",
        ".#..#.",
    ],
}
ELITE = [
    "..#...",
    "..#..#",
    "#.##.#",
    "######",
    "######",
    "......",
]


def flat(rows: list[str], w: int, h: int, colour: str = "BONE", bottom: bool = True) -> np.ndarray:
    """Centre a character mask horizontally on a w x h canvas, bottom-aligned."""
    m = bits(rows)
    a = blank(w, h)
    ox = (w - m.shape[1]) // 2
    oy = h - m.shape[0] if bottom else (h - m.shape[0]) // 2
    sub = np.zeros((h, w), bool); sub[oy:oy + m.shape[0], ox:ox + m.shape[1]] = m
    put(a, sub, colour)
    return a


# ------------------------------------------------------------------ 7. corridor nodes
NODE_W, NODE_H = 10, 11


DOOR_W = 9  # the door is 9 px wide (columns 0..8) so it has a centre column; column 9 of the 10 px canvas stays clear


def door_mask() -> np.ndarray:
    """Round-headed doorway: a 2-step arch (3 px, then 7 px), full width from row 2, the sill on row 10."""
    m = np.zeros((NODE_H, NODE_W), bool)
    m[0, 3:6] = True
    m[1, 1:8] = True
    m[2:, 0:DOOR_W] = True
    return m


DICE = {  # rank dots, 1 px, dice faces in a 5 x 5 box centred on the door (columns 2..6, rows 3..7)
    1: [(4, 5)],
    2: [(2, 3), (6, 7)],
    3: [(2, 3), (4, 5), (6, 7)],
    4: [(2, 3), (6, 3), (2, 7), (6, 7)],
    5: [(2, 3), (6, 3), (4, 5), (2, 7), (6, 7)],
}


def dotted(m: np.ndarray) -> np.ndarray:
    """Every other outline pixel, mirrored about the door's centre column so the pattern is symmetric."""
    h, w = m.shape
    out = np.zeros_like(m)
    for y in range(h):
        for x in range(w):
            out[y, x] = m[y, x] and (min(x, DOOR_W - 1 - x) + y) % 2 == 0
    return out


def node(kind: str, tier: str | None = None) -> np.ndarray:
    m = door_mask()
    line = m & ~plates.erode4(m)
    inner = m & ~line
    a = blank(NODE_W, NODE_H)
    if kind == "ahead":                    # not yet reached: a closed door, unlit, with its handle
        put(a, inner, "INK-1"); put(a, line, "INK-3")
        a[6, 6] = (*P.rgb("INK-3"), 255)
    elif kind == "entrance":               # room 1: an open doorway, the leaf swung in on the left
        put(a, inner, "INK-0"); put(a, line, "INK-4")
        leaf = np.zeros_like(m)
        leaf[2:10, 1] = True
        leaf[3:9, 2] = True
        put(a, leaf & inner, "INK-3")
    elif kind == "cleared":
        T = tier.upper()
        put(a, inner, "INK-2"); put(a, line, f"{T}-BASE")
        for x, y in DICE[RANK[tier]]:
            a[y, x] = (*P.rgb(f"{T}-BASE"), 255)
        if tier == "gold":                 # the curse taken here: a Plum notch hanging from the sill
            a[NODE_H - 1, 3:6] = (*P.rgb("PLUM-E"), 255)
            a[NODE_H - 2, 4] = (*P.rgb("PLUM"), 255)
    elif kind == "fusion":                 # a fusion room (item-fusion.md): the door lit Candle, molten metal in a bowl
        put(a, inner, "INK-2"); put(a, line, "CANDLE-D")
        for x, y in ((4, 4), (5, 4), (3, 5), (4, 5), (5, 5), (6, 5)):
            a[y, x] = (*P.rgb("CANDLE"), 255)
        for x, y in ((3, 6), (6, 6), (4, 7), (5, 7)):
            a[y, x] = (*P.rgb("INK-4"), 255)
    elif kind == "preview":                # armed, not confirmed: the ahead door outlined in that tier, dotted
        T = tier.upper()
        put(a, inner, "INK-1"); put(a, dotted(line), f"{T}-BASE")
        for x, y in DICE[RANK[tier]]:
            a[y, x] = (*P.rgb(f"{T}-BASE"), 255)
    return a


BOSS = [  # 12 x 12: a square bay whose lintel has cracked and dropped on the right, Wine W4 / W3, three Red-hi teeth
    "444444......",
    "4444444.4444",
    "4RR3RR3.4444",
    "4RR3RR33RR34",
    "43333333RR34",
    "433333333334",
    "433333333334",
    "433333333334",
    "433333333334",
    "433333333334",
    "433333333334",
    "444444444444",
]


def boss_node() -> np.ndarray:
    return paint(BOSS, {"4": "WINE-4", "3": "WINE-3", "R": "RED-HI"})


def now_marker() -> np.ndarray:
    a = blank(6, 2); a[:] = (*P.rgb("CANDLE"), 255)
    return a


# ------------------------------------------------------------------ build list
def sprites() -> dict[str, tuple[np.ndarray, tuple[int, int, int, int] | None]]:
    """name -> (RGBA, 9-slice border or None)."""
    out: dict[str, tuple[np.ndarray, tuple[int, int, int, int] | None]] = {}
    for t in TIERS:
        out[f"ui_door-card_{t}_9s"] = (door_card(t), CARD_BORDER)
    out["ui_door-card_disabled_9s"] = (door_card_disabled(), CARD_BORDER)
    out["ui_door-card_ring-hover_9s"] = (ring(1), RING_HOVER_BORDER)
    out["ui_door-card_ring-armed_9s"] = (ring(2), RING_ARMED_BORDER)
    out["ui_door-card_armed-corner_4x4"] = (armed_corner(), None)
    for t in TIERS:
        out[f"ui_pip-rank_{t}_5x5"] = (pip(t), None)
    out["ui_curse-tag_card_24x10"] = (tag(24, 10, (4, 5)), None)
    out["ui_curse-chip_9s"] = (tag(24, 12, (5, 6)), CURSE_CHIP_BORDER)
    out["ui_fill_plum_4x4"] = (plum_fill(), None)
    for g in GLYPHS10:
        for size, px in ((10, 12), (6, 8)):
            out[f"ui_glyph-{g}_bone_{px}x{px}"] = (glyph(g, size), None)
    for k in ("s", "m", "l"):
        out[f"ui_glyph-size-{k}_bone_10x10"] = (flat(SIZE_MARKS[k], 10, 10), None)
    out["ui_glyph-elite_bone_6x6"] = (flat(ELITE, 6, 6), None)
    out["ui_map-node_ahead_10x11"] = (node("ahead"), None)
    out["ui_map-node_entrance_10x11"] = (node("entrance"), None)
    out["ui_map-node_fusion_10x11"] = (node("fusion"), None)
    for t in TIERS:
        out[f"ui_map-node_{t}_10x11"] = (node("cleared", t), None)
    for t in TIERS:
        out[f"ui_map-node-preview_{t}_10x11"] = (node("preview", t), None)
    out["ui_map-node_boss_12x12"] = (boss_node(), None)
    out["ui_map-marker_now_6x2"] = (now_marker(), None)
    return out


# ------------------------------------------------------------------ validation
PALETTE_SET = {P.rgb(n) for n in P.PALETTE}


def validate(name: str, a: np.ndarray, border: tuple[int, int, int, int] | None) -> list[str]:
    problems = []
    alpha = a[..., 3]
    if not np.isin(alpha, (0, 255)).all():
        problems.append(f"{name}: alpha other than 0/255")
    opaque = a[alpha == 255][:, :3]
    off = {tuple(int(v) for v in c) for c in opaque} - PALETTE_SET
    if off:
        problems.append(f"{name}: off-palette colours {sorted(off)}")
    if (a[alpha == 0][:, :3] != 0).any():
        problems.append(f"{name}: transparent pixels carry colour")
    stem = name.rsplit("_", 1)[-1]
    if stem != "9s":
        w, h = (int(v) for v in stem.split("x"))
        if (a.shape[1], a.shape[0]) != (w, h):
            problems.append(f"{name}: size {a.shape[1]}x{a.shape[0]} does not match its name")
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
def slice9(a: np.ndarray, border: tuple[int, int, int, int], w: int, h: int) -> np.ndarray:
    """Unity Sliced draw mode, simulated: corners fixed, edges and centre stretched (nearest)."""
    left, bottom, right, top = border
    sh, sw = a.shape[:2]
    xs = list(range(left)) + [left + (i * (sw - left - right)) // max(1, w - left - right) for i in range(w - left - right)] \
        + list(range(sw - right, sw))
    ys = list(range(top)) + [top + (j * (sh - top - bottom)) // max(1, h - top - bottom) for j in range(h - top - bottom)] \
        + list(range(sh - bottom, sh))
    return a[np.ix_(ys, xs)]


class Sheet:
    def __init__(self, w: int, h: int) -> None:
        self.a = np.zeros((h, w, 4), np.uint8)
        self.a[:] = (*P.rgb("INK-1"), 255)

    def blit(self, s: np.ndarray, x: int, y: int) -> None:
        h, w = s.shape[:2]
        region = self.a[y:y + h, x:x + w]
        m = s[..., 3] == 255
        region[m] = s[m]

    def band(self, x: int, y: int, w: int, h: int, colour: str) -> None:
        self.a[y:y + h, x:x + w] = (*P.rgb(colour), 255)


def card_preview(tier: str | None, w: int = 84, h: int = 76) -> np.ndarray:
    """A card at its real width, cut short: frame, pips, a size mark and a benefit glyph row."""
    a = slice9(door_card(tier) if tier else door_card_disabled(), CARD_BORDER, w, h)
    s = Sheet(w, h); s.a[:] = 0; s.blit(a, 0, 0)
    if tier:
        n = RANK[tier]
        x0 = (w - (n * 6 - 1)) // 2
        for i in range(n):
            s.blit(pip(tier), x0 + i * 6, 6)
    mark = flat(SIZE_MARKS["m"], 10, 10, "BONE" if tier else "INK-3")
    s.blit(mark, 8, 18)
    if tier == "gold":
        s.blit(flat(ELITE, 6, 6), 20, 22)
    s.blit(glyph("cell", 10), 8, 34)
    return s.a


def build_sheet(sp: dict) -> Image.Image:
    W, H = 640, 412
    s = Sheet(W, H)
    # row 1: card sources, rings, corner
    x = 4
    for t in TIERS + ["disabled"]:
        s.blit(sp[f"ui_door-card_{t}_9s"][0], x, 4); x += 28
    s.blit(sp["ui_door-card_ring-hover_9s"][0], x, 4); x += 16
    s.blit(sp["ui_door-card_ring-armed_9s"][0], x, 4); x += 16
    s.blit(sp["ui_door-card_armed-corner_4x4"][0], x, 4); x += 8
    for t in TIERS:
        s.blit(sp[f"ui_pip-rank_{t}_5x5"][0], x, 4); x += 7
    x += 4
    s.blit(sp["ui_curse-tag_card_24x10"][0], x, 4)
    s.blit(sp["ui_curse-chip_9s"][0], x, 18)
    s.blit(sp["ui_fill_plum_4x4"][0], x + 28, 4)
    x += 34
    # glyphs, 12 then 8, with existing glyphs for comparison
    gx = x
    for g in GLYPHS10:
        s.blit(sp[f"ui_glyph-{g}_bone_12x12"][0], gx, 4)
        s.blit(sp[f"ui_glyph-{g}_bone_8x8"][0], gx + 2, 18); gx += 14
    gx += 6
    for g in ("heart", "droplet", "seal", "shield", "speed", "damage", "attack"):
        a = np.array(Image.open(UI_DEFAULT / f"ui_glyph-{g}_bone_8x8.png").convert("RGBA"))
        s.blit(a, gx, 18); gx += 10
    s.blit(np.array(Image.open(UI_DEFAULT / "ui_glyph-cog_bone_8x8.png").convert("RGBA")), gx, 18)
    # row 2: size marks, elite, nodes
    y2 = 34
    x = 4
    for k in ("s", "m", "l"):
        s.blit(sp[f"ui_glyph-size-{k}_bone_10x10"][0], x, y2); x += 12
    s.blit(sp["ui_glyph-elite_bone_6x6"][0], x, y2 + 4); x += 10
    for n in ["ahead", "entrance"] + TIERS:
        s.blit(sp[f"ui_map-node_{n}_10x11"][0], x, y2); x += 12
    for t in TIERS:
        s.blit(sp[f"ui_map-node-preview_{t}_10x11"][0], x, y2); x += 12
    s.blit(sp["ui_map-node_boss_12x12"][0], x, y2 - 1); x += 14
    s.blit(sp["ui_map-marker_now_6x2"][0], x, y2 + 5); x += 10
    # corridor strip as it will sit: 9 nodes at 12 px pitch in a 16 px strip, now-marker under the current room
    sx, sy = x + 6, y2 - 3
    s.band(sx, sy, 9 * 12, 16, "INK-0")
    order = ["entrance", "white", "green", "gold", "purple", "preview-blue", "ahead", "ahead", "boss"]
    for i, n in enumerate(order):
        if n == "boss":
            s.blit(sp["ui_map-node_boss_12x12"][0], sx + i * 12, sy)
        elif n.startswith("preview-"):
            s.blit(sp[f"ui_map-node-preview_{n[8:]}_10x11"][0], sx + i * 12 + 1, sy + 1)
        else:
            s.blit(sp[f"ui_map-node_{n}_10x11"][0], sx + i * 12 + 1, sy + 1)
    s.blit(sp["ui_map-marker_now_6x2"][0], sx + 4 * 12 + 3, sy + 13)
    # row 3: cards at real width (84), cut to 76 tall; rings and tag shown in place
    y3 = 60
    x = 8
    for i, t in enumerate(TIERS + [None]):
        cp = card_preview(t)
        s.blit(cp, x, y3)
        if t == "blue":
            s.blit(slice9(sp["ui_door-card_ring-hover_9s"][0], RING_HOVER_BORDER, 84 + 4, 76 + 4), x - 2, y3 - 2)
        if t == "purple":
            s.blit(slice9(sp["ui_door-card_ring-armed_9s"][0], RING_ARMED_BORDER, 84 + 6, 76 + 6), x - 3, y3 - 3)
            s.blit(sp["ui_door-card_armed-corner_4x4"][0], x - 3, y3 - 3)
        if t == "gold":
            tx, ty = x + 84 - 30, y3 + 76
            s.blit(sp["ui_curse-tag_card_24x10"][0], tx, ty)
            s.blit(sp["ui_glyph-curse-pinned_bone_8x8"][0], tx + 4, ty + 1)
        x += 84 + 12
    # row 4: curse chips at hero stat-row width with each curse glyph; chip next to the stat chip
    y4 = y3 + 96
    x = 8
    chip_base = np.array(Image.open(UI_DEFAULT / "ui_chip_base_9s.png").convert("RGBA"))
    s.blit(slice9(chip_base, (3, 0, 3, 0), 30, 12), x, y4); x += 34
    for g in ("curse-pinned", "curse-wither", "curse-frailty", "curse-sluggish"):
        s.blit(slice9(sp["ui_curse-chip_9s"][0], CURSE_CHIP_BORDER, 30, 12), x, y4)
        s.blit(sp[f"ui_glyph-{g}_bone_8x8"][0], x + 4, y4 + 2)
        x += 34
    for g in ("curse-pinned", "curse-wither", "curse-frailty", "curse-sluggish"):
        s.blit(sp["ui_curse-tag_card_24x10"][0], x, y4 + 1)
        s.blit(sp[f"ui_glyph-{g}_bone_8x8"][0], x + 4, y4 + 2)
        x += 28
    rgb = Image.fromarray(s.a[: y4 + 18], "RGBA").convert("RGB")
    grey = rgb.convert("L").convert("RGB")
    both = Image.new("RGB", (rgb.width, rgb.height * 2 + 4), P.rgb("INK-0"))
    both.paste(rgb, (0, 0)); both.paste(grey, (0, rgb.height + 4))
    return both.resize((both.width * 4, both.height * 4), Image.NEAREST)


UI_DEFAULT = UI


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--ui-dir", type=Path, default=UI)
    ap.add_argument("--sheet", type=Path, default=SHEET)
    args = ap.parse_args()
    sp = sprites()
    problems = [p for n, (a, b) in sp.items() for p in validate(n, a, b)]
    for p in problems:
        print("PROBLEM:", p)
    if problems:
        return 1
    args.ui_dir.mkdir(parents=True, exist_ok=True)
    for n, (a, _) in sp.items():
        to_cv(a).save(args.ui_dir / f"{n}.png")
    args.sheet.parent.mkdir(parents=True, exist_ok=True)
    build_sheet(sp).save(args.sheet)
    print(f"wrote {len(sp)} sprites to {args.ui_dir} and the contact sheet to {args.sheet}")
    for n, (a, b) in sp.items():
        print(f"  {n}.png  {a.shape[1]}x{a.shape[0]}" + (f"  border(l,b,r,t)={b}" if b else ""))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
