"""Combat HUD pieces and shared effect art as files (art bible 7.4, 7.5, 7.11, 8.7, 8.8).

UI pieces: stat/status chip 3-slice, intent pennant tab, HP bar frame / fill / trail,
speed gauge, pedestal (default, empty outline, fallen), enemy contact shadow, and the
segment button 9-slice with its five states plus selected.
Effects (shared, 13 frames; each has a cold -foe recolour, bursts also a -calm set):
swing smear x3, impact spark x2, throw trail x2, status mote x2, detonate burst x4.

Run:  python -I tools/art/build_combat_ui.py
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402

UI = ROOT / "Assets/_Project/Art/UI"
VFX = ROOT / "Assets/_Project/Art/VFX"
STAGE = ROOT / "Assets/_Project/Art/Characters"


class Cv:
    def __init__(self, w: int, h: int) -> None:
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 4), np.uint8)

    def px(self, x: int, y: int, n: str) -> None:
        if 0 <= x < self.w and 0 <= y < self.h:
            self.a[y, x] = (*P.rgb(n), 255)

    def rect(self, x: int, y: int, w: int, h: int, n: str) -> None:
        for j in range(h):
            for i in range(w):
                self.px(x + i, y + j, n)

    def frame(self, x: int, y: int, w: int, h: int, t: int, n: str) -> None:
        self.rect(x, y, w, t, n); self.rect(x, y + h - t, w, t, n)
        self.rect(x, y, t, h, n); self.rect(x + w - t, y, t, h, n)

    def ellipse(self, cx: float, cy: float, rx: float, ry: float, n: str, outline: bool = False) -> None:
        for y in range(self.h):
            for x in range(self.w):
                d = ((x - cx) / rx) ** 2 + ((y - cy) / ry) ** 2
                inner = ((x - cx) / (rx - 1)) ** 2 + ((y - cy) / max(ry - 1, .5)) ** 2
                if (d <= 1 and not outline) or (outline and d <= 1 < inner + 0.0001 and inner >= 1):
                    self.px(x, y, n)

    def save(self, path: Path) -> None:
        path.parent.mkdir(parents=True, exist_ok=True)
        Image.fromarray(self.a, "RGBA").save(path)


# ------------------------------------------------------------------ UI pieces
def chip() -> Cv:
    c = Cv(24, 12)
    c.rect(0, 0, 24, 12, "INK-1"); c.frame(0, 0, 24, 12, 1, "INK-4")
    return c


def tab() -> Cv:
    """Downward pennant tab (3.2): 46 x 24 including the point. INK-1 bed, INK-4 frame."""
    c = Cv(46, 24)
    c.rect(0, 0, 46, 18, "INK-1"); c.frame(0, 0, 46, 18, 1, "INK-4")
    for i in range(6):
        left, right = 18 + i, 27 - i
        c.rect(left, 18 + i, right - left + 1, 1, "INK-1")
        c.px(left, 18 + i, "INK-4"); c.px(right, 18 + i, "INK-4")
    c.rect(19, 17, 8, 1, "INK-1")  # open the frame where the point joins
    return c


def hp_bar() -> dict[str, Cv]:
    f = Cv(36, 8); f.rect(0, 0, 36, 8, "INK-4"); f.rect(1, 1, 34, 6, "INK-0")
    low = Cv(36, 8); low.rect(0, 0, 36, 8, "BONE"); low.rect(1, 1, 34, 6, "INK-4"); low.rect(2, 2, 32, 4, "INK-0")  # low HP: 1 px Bone outline
    fill = Cv(34, 6); fill.rect(0, 0, 34, 6, "BLOOD")
    trail = Cv(34, 6); trail.rect(0, 0, 34, 6, "BLOOD-D")
    return {"ui_hp-bar_frame_36x8": f, "ui_hp-bar_frame-low_36x8": low, "ui_hp-bar_fill_34x6": fill, "ui_hp-bar_trail_34x6": trail}


def gauge() -> dict[str, Cv]:
    f = Cv(20, 6); f.rect(0, 2, 20, 4, "INK-4"); f.rect(1, 3, 18, 2, "INK-0")
    for x in (5, 10, 15):
        f.px(x, 1, "BONE")
    fill = Cv(18, 2); fill.rect(0, 0, 18, 2, "FOG")
    return {"ui_speed-gauge_frame_20x6": f, "ui_speed-gauge_fill_18x2": fill}


def pedestal() -> dict[str, Cv]:
    out = {}
    d = Cv(52, 12)
    d.ellipse(25.5, 6, 24, 5, "CANDLE-D")                  # warm pool (the "you" light)
    d.ellipse(25.5, 6, 15.5, 3, "INK-3")                   # pedestal
    d.ellipse(25.5, 5, 15.5, 2.2, "INK-4")                 # top highlight
    d.ellipse(25.5, 6, 14, 2.4, "INK-3")
    out["prop_pedestal_default_52x12"] = d
    e = Cv(52, 12); e.ellipse(25.5, 6, 15.5, 3, "INK-4", outline=True)   # empty slot: outline only (3.3.2)
    out["prop_pedestal_empty_52x12"] = e
    f = Cv(52, 12); f.ellipse(25.5, 6, 15.5, 3, "INK-2"); f.ellipse(25.5, 6, 14, 2.4, "INK-1")   # fallen: pool out
    out["prop_pedestal_dim_52x12"] = f
    s = Cv(30, 4); s.ellipse(14.5, 1.5, 14, 1.6, "INK-1")
    out["prop_shadow-contact_default_30x4"] = s
    return out


def segment(state: str) -> Cv:
    """Segment button 9-slice, 24 x 24 with the body at 4..19 (16 x 16); rings sit outside with a 1 px gap."""
    c = Cv(24, 24)
    edge = {"disabled": "INK-3", "pressed": "CANDLE"}.get(state, "INK-4")
    fill = "CANDLE" if state == "pressed" else "INK-1"
    if state in ("hover", "selected"):
        c.frame(2, 2, 20, 20, 1, "CANDLE")
    if state == "focus":
        c.frame(1, 1, 22, 22, 2, "CANDLE")
    if state == "selected":
        c.rect(2, 2, 4, 4, "CANDLE")
    c.rect(4, 4, 16, 16, fill); c.frame(4, 4, 16, 16, 1, edge)
    return c


# ------------------------------------------------------------------ effects
RAMPS = {"": ("BONE", "FOG", "INK-4"), "-foe": ("FOG-HI", "FOG", "INK-4")}


def shells(size: int, cx: float, cy: float, radii: list[float], ramp: tuple[str, str, str], ring: bool = False) -> Cv:
    c = Cv(size, size)
    for y in range(size):
        for x in range(size):
            d = ((x - cx) ** 2 + (y - cy) ** 2) ** .5
            for r, col in zip(radii, ramp[::-1]):
                if (not ring and d <= r) or (ring and r - 1.5 <= d <= r):
                    c.px(x, y, col)
    return c


def crescent(k: int, ramp: tuple[str, str, str]) -> Cv:
    """Swing smear: a crescent growing then thinning over 3 frames; light on the key (upper-left) side."""
    c = Cv(32, 32)
    r_out, off = [(12, 2), (14, 4), (13, 7)][k]
    for y in range(32):
        for x in range(32):
            d_out = ((x - 16) ** 2 + (y - 16) ** 2) ** .5
            d_in = ((x - 16 - off) ** 2 + (y - 16 - off) ** 2) ** .5
            if d_out <= r_out and d_in > r_out - 3 - (2 - k):
                depth = r_out - d_out
                c.px(x, y, ramp[2] if depth < 1.2 else (ramp[1] if depth < 3 else ramp[0]))
    return c


def spark(k: int, ramp: tuple[str, str, str]) -> Cv:
    c = Cv(16, 16)
    n = (3, 6)[k]
    for i in range(-n, n + 1):
        c.px(8 + i, 8, ramp[1] if abs(i) > 1 else ramp[0]); c.px(8, 8 + i, ramp[1] if abs(i) > 1 else ramp[0])
    for i in range(-(n // 2), n // 2 + 1):
        c.px(8 + i, 8 + i, ramp[2]); c.px(8 + i, 8 - i, ramp[2])
    c.px(8, 8, ramp[0])
    return c


def trail(k: int, ramp: tuple[str, str, str]) -> Cv:
    c = Cv(16, 16)
    for i, x in enumerate(range(2 + 3 * k, 14)):
        c.px(x, 8, ramp[0] if i < 2 else ramp[1]); c.px(x, 9, ramp[2])
    return c


def mote(k: int, ramp: tuple[str, str, str]) -> Cv:
    c = Cv(8, 8)
    if k == 0:
        c.rect(3, 3, 2, 2, ramp[0])
    else:
        c.rect(2, 3, 4, 2, ramp[1]); c.rect(3, 2, 2, 4, ramp[1]); c.rect(3, 3, 2, 2, ramp[0])
    return c


def burst(k: int, ramp: tuple[str, str, str], calm: bool) -> Cv:
    c = Cv(48, 48)
    if calm:  # steady glow: two stepped shells at the final radius, no spokes, no growth
        return shells(48, 23.5, 23.5, [14, 11, 7], ramp)
    radius = (6, 11, 17, 21)[k]
    ring = shells(48, 23.5, 23.5, [radius, radius - 2, max(1, radius - 4)], ramp, ring=True)
    for ang in range(8):
        dx, dy = (1, 0, -1, 0, 1, -1, 1, -1)[ang], (0, 1, 0, -1, 1, 1, -1, -1)[ang]
        for i in range(2, radius + 2):
            ring.px(round(23.5 + dx * i), round(23.5 + dy * i), ramp[0] if i < 4 else ramp[1])
    ring.rect(22, 22, 4, 4, ramp[0])
    return ring


FILLS = {"ink-0": "INK-0", "ink-1": "INK-1", "ink-2": "INK-2", "ink-3": "INK-3", "ink-4": "INK-4",
         "fog": "FOG", "bone": "BONE", "blood": "BLOOD", "candle": "CANDLE", "candle-d": "CANDLE-D"}


def panel_void() -> Cv:
    """Stage panel: the default frame around an INK-0 void (7.3). 16 x 16, border 4."""
    c = Cv(16, 16)
    c.rect(0, 0, 16, 16, "INK-3"); c.rect(3, 3, 10, 10, "INK-0")
    c.rect(3, 3, 10, 1, "INK-4"); c.rect(3, 3, 1, 10, "INK-4")
    return c


def primary(state: str) -> Cv:
    """Primary action button 9-slice (Start, art bible 7.3): the segment frame with a Candle 2 px frame. 24 x 24, body at 4..19."""
    c = Cv(24, 24)
    edge = "INK-3" if state == "disabled" else "CANDLE"
    fill = "CANDLE" if state == "pressed" else "INK-1"
    if state == "hover":
        c.frame(2, 2, 20, 20, 1, "CANDLE")
    if state == "focus":
        c.frame(1, 1, 22, 22, 2, "CANDLE")
    c.rect(4, 4, 16, 16, fill)
    c.frame(4, 4, 16, 16, 2, edge)
    return c


def chipbtn(state: str) -> Cv:
    """Small button on a chip (menu, 12 x 12, border 3). The state shows in the frame, since the top strip leaves no room for rings."""
    c = Cv(12, 12)
    edge = {"default": "INK-4", "hover": "CANDLE", "pressed": "CANDLE", "focus": "CANDLE", "disabled": "INK-3"}[state]
    c.rect(0, 0, 12, 12, "CANDLE" if state == "pressed" else "INK-1")
    c.frame(0, 0, 12, 12, 2 if state == "focus" else 1, edge)
    return c


def dim_mask() -> Cv:
    """One-step dim for the pause overlay (art bible 2.3.1): a 1 px checker of INK-0, so no pixel leaves the palette."""
    c = Cv(4, 4)
    for y in range(4):
        for x in range(4):
            if (x + y) % 2 == 0:
                c.px(x, y, "INK-0")
    return c


def ghost_cells() -> dict[str, Cv]:
    """Ghost footprint cells for dragging an item (art bible 7.11): a solid 1 px outline, Fog-hi when the target is valid and
    Fog when it is not, plus a 5 x 5 cross on each blocking cell. Never red."""
    valid = Cv(20, 20); valid.frame(0, 0, 20, 20, 1, "FOG-HI")
    invalid = Cv(20, 20); invalid.frame(0, 0, 20, 20, 1, "FOG")
    cross = Cv(20, 20)
    for i in range(5):
        cross.px(8 + i, 8 + i, "FOG"); cross.px(12 - i, 8 + i, "FOG")
    return {"ui_ghost_valid_20x20": valid, "ui_ghost_invalid_20x20": invalid, "ui_ghost_cross_20x20": cross}


# ------------------------------------------------------------------ sell zone (user request 2026-11-16)
def sell_scale(state: str) -> Cv:
    """Balance scale above the sell zone, 48 x 28. Default is quiet chrome; active (an item is held) lifts it to Fog."""
    c = Cv(48, 28)
    hi, lo = ("FOG-HI", "FOG") if state == "active" else ("INK-4", "INK-3")
    c.rect(22, 0, 4, 2, hi)                      # knob
    c.rect(23, 2, 2, 22, lo); c.rect(23, 2, 1, 22, hi)   # pillar, lit on the left
    c.rect(4, 4, 40, 2, lo); c.rect(4, 4, 40, 1, hi)     # beam
    c.rect(16, 24, 16, 2, lo); c.rect(14, 26, 20, 2, lo); c.rect(16, 24, 16, 1, hi)  # base
    for cx in (7, 40):
        c.rect(cx - 1, 6, 2, 2, hi)              # hook
        for k in range(9):                       # two strings to the pan rim
            c.px(cx - 1 - k // 2 - (k % 2), 8 + k, lo)
            c.px(cx + k // 2 + (k % 2), 8 + k, lo)
        c.rect(cx - 7, 17, 15, 1, hi)            # pan rim
        c.rect(cx - 6, 18, 13, 1, lo); c.rect(cx - 4, 19, 9, 1, lo)
    return c


def sell_frame(state: str) -> Cv:
    """Sell zone 9-slice, 24 x 24, border 8: a thin frame with bracket corners. Default Blood-d corners; active (an item is
    held) Blood corners and an Ink-4 frame; hover (the held item is over it) a Blood frame and Wine fill."""
    c = Cv(24, 24)
    fill = {"default": "INK-1", "active": "INK-1", "hover": "WINE-1"}[state]
    edge = {"default": "INK-3", "active": "INK-4", "hover": "BLOOD"}[state]
    corner = {"default": "BLOOD-D", "active": "BLOOD", "hover": "RED-GLOW"}[state]
    c.rect(2, 2, 20, 20, fill)
    c.frame(2, 2, 20, 20, 1, edge)
    for x0, y0, sx, sy in ((0, 0, 1, 1), (23, 0, -1, 1), (0, 23, 1, -1), (23, 23, -1, -1)):
        for i in range(6):
            c.px(x0 + sx * i, y0, corner); c.px(x0, y0 + sy * i, corner)
            c.px(x0 + sx * i, y0 + sy, corner); c.px(x0 + sx, y0 + sy * i, corner)
        c.px(x0 + sx * 3, y0 + sy * 3, corner)
    return c


def sell_tile() -> Cv:
    """Floor of the sell zone, tiled at the cell pitch: a small ring at the cell centre and a plus at the corners, Ink-2."""
    c = Cv(20, 20)
    for dx, dy in ((0, -1), (1, 0), (0, 1), (-1, 0)):
        c.px(10 + dx, 10 + dy, "INK-3")
    for x, y in ((0, 0),):
        for k in range(-2, 3):
            c.px((x + k) % 20, y, "INK-2"); c.px(x, (y + k) % 20, "INK-2")
    return c


def start_button(state: str) -> Cv:
    """Start, the one filled button (art bible 7.3, review 2026-11-16): a Candle plate with a Candle-d lip so it reads as
    pressable, dark text on it. 24 x 24, body at 4..19, border 8. Hover adds a 1 px Candle ring with a 1 px gap; focus a
    2 px Bone ring, so focus never looks like the resting plate; pressed sinks 1 px and loses the lip; disabled is Ink."""
    c = Cv(24, 24)
    if state == "disabled":
        c.rect(4, 4, 16, 16, "INK-3"); c.frame(4, 4, 16, 16, 1, "INK-4")
        return c
    if state == "hover":
        c.frame(2, 2, 20, 20, 1, "CANDLE")
    if state == "focus":
        c.frame(0, 0, 24, 24, 2, "BONE")
    if state == "pressed":
        c.rect(4, 5, 16, 15, "CANDLE-D"); c.rect(5, 6, 14, 13, "CANDLE")
        return c
    c.rect(4, 4, 16, 16, "CANDLE-D")              # outline and lip
    c.rect(5, 5, 14, 12, "CANDLE")                # face
    c.rect(5, 5, 14, 1, "BONE")                   # top light
    return c


# ------------------------------------------------------------------ mouse cursors (user request 2026-11-16)
CURSOR_ARROW = [
    "O..........",
    "OO.........",
    "OCO........",
    "OBBO.......",
    "OBBBO......",
    "OBBBBO.....",
    "OBBBBFO....",
    "OBBBBBFO...",
    "OBBBBBBFO..",
    "OBBBBBBBFO.",
    "OBBBBFOOOOO",
    "OBBOBFO....",
    "OBO.OBFO...",
    "OO..OBFO...",
    "O....OBFO..",
    ".....OOO...",
]
CURSOR_GRAB = [
    "..OOOOOOOO....",
    ".OBBOBBOBBOO..",
    ".OBBOBBOBBOBO.",
    "OOBBBBBBBBBBO.",
    "OBOBBBBBBBBBO.",
    "OBBBBBBBBBBFO.",
    "OBBBBBBBBBBFO.",
    ".OBBBBBBBBFO..",
    "..OBBBBBBFO...",
    "...OBBBBBFO...",
    "...OOOOOOO....",
]
CURSOR_INK = {"O": "INK-0", "B": "BONE", "F": "FOG", "C": "CANDLE"}


def cursor(rows: list[str]) -> Cv:
    """A cursor drawn from a character map: Ink-0 outline, Bone body, Fog shade, a Candle tip on the arrow."""
    c = Cv(len(rows[0]), len(rows))
    for y, row in enumerate(rows):
        for x, ch in enumerate(row):
            if ch in CURSOR_INK:
                c.px(x, y, CURSOR_INK[ch])
    return c


def save_cursor(c: Cv, name: str) -> None:
    """Hardware cursors are not scaled by the canvas, so each one is saved pre-scaled for zoom 1 to 4."""
    folder = ROOT / "Assets/_Project/Art/UI/Cursors"
    folder.mkdir(parents=True, exist_ok=True)
    img = Image.fromarray(c.a, "RGBA")
    for z in range(1, 5):
        big = img.resize((c.w * z, c.h * z), Image.NEAREST)
        big.save(folder / f"ui_cursor-{name}_x{z}_{c.w * z}x{c.h * z}.png")


def main() -> int:
    UI.mkdir(parents=True, exist_ok=True)
    for name, c in ghost_cells().items():
        c.save(UI / f"{name}.png")
    for st in ("default", "hover", "pressed", "focus", "disabled"):
        primary(st).save(UI / f"ui_primary_{st}_9s.png")
        chipbtn(st).save(UI / f"ui_chipbtn_{st}_9s.png")
        start_button(st).save(UI / f"ui_start_{st}_9s.png")
    dim_mask().save(UI / "ui_dim-mask_default_4x4.png")
    save_cursor(cursor(CURSOR_ARROW), "arrow")
    save_cursor(cursor(CURSOR_GRAB), "grab")
    for st in ("default", "active"):
        sell_scale(st).save(UI / f"ui_sell-scale_{st}_48x28.png")
    for st in ("default", "active", "hover"):
        sell_frame(st).save(UI / f"ui_sell-frame_{st}_9s.png")
    sell_tile().save(UI / "ui_sell-tile_default_20x20.png")
    panel_void().save(UI / "ui_panel_void_9s.png")
    for name, colour in FILLS.items():  # solid fills as sprites: UI vertex colour is not exact in a linear-colour project
        f = Cv(4, 4); f.rect(0, 0, 4, 4, colour); f.save(UI / f"ui_fill_{name}_4x4.png")
    chip().save(UI / "ui_chip_base_9s.png")
    tab().save(UI / "ui_tab-intent_default_46x24.png")
    for n, c in {**hp_bar(), **gauge()}.items():
        c.save(UI / f"{n}.png")
    for n, c in pedestal().items():
        c.save((STAGE.parent / "Props" if n.startswith("prop_") else UI) / f"{n}.png")
    for st in ("default", "hover", "pressed", "disabled", "focus", "selected"):
        segment(st).save(UI / f"ui_segment_{st}_9s.png")
    count = 0
    for suffix, ramp in RAMPS.items():
        for k in range(3):
            crescent(k, ramp).save(VFX / f"vfx_swing_smear{suffix}_{k + 1:02d}.png"); count += 1
        for k in range(2):
            spark(k, ramp).save(VFX / f"vfx_impact_spark{suffix}_{k + 1:02d}.png"); count += 1
            trail(k, ramp).save(VFX / f"vfx_throw_trail{suffix}_{k + 1:02d}.png"); count += 1
            mote(k, ramp).save(VFX / f"vfx_status_mote{suffix}_{k + 1:02d}.png"); count += 1
        for k in range(4):
            burst(k, ramp, False).save(VFX / f"vfx_detonate_burst{suffix}_{k + 1:02d}.png"); count += 1
        burst(0, ramp, True).save(VFX / f"vfx_detonate_burst{suffix}-calm_01.png"); count += 1
    print(f"wrote UI pieces and {count} effect frames")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
