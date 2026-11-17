"""Build the key frames for the hero and the two monsters (art bible 5.5, 5.6, 8.4).

Input: the generator's raw idle image per character (art-source/ai-raw), already
chosen. Steps: palette mapping and outline (process_character.py), then every
other key frame is made from the idle frame by whole-pixel edits (no resampling,
no rotation, no squash): a 1 px bob, a recoil, an integer row shear, a drop.
That keeps the ground line, the pivot and the outline weight identical in every
frame (8.4 stability rules).

  hero (tank family, 6 + overlay):  idle 01-02, hurt 01-02, down 01-02, narrow (eyes-narrow overlay)
  monster (6 + overlay):            idle 01-02, act 01, hurt 01, die 01-02, eyes 01-03 (lit sockets for idle1, idle2, act)

Run:  python -I tools/art/build_characters.py
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402
import process_character as PC  # noqa: E402

RAW = ROOT / "art-source/ai-raw"
CHARS = ROOT / "Assets/_Project/Art/Characters"
MONS = ROOT / "Assets/_Project/Art/Monsters"
EVIDENCE = ROOT / "production/qa/evidence"
GROUND_ROW = lambda h: h - 5  # last body row; the pivot is 4 px above the canvas bottom  # noqa: E731

C = lambda n: np.array(P.rgb(n), np.uint8)  # noqa: E731


def opaque(a: np.ndarray) -> np.ndarray:
    return a[..., 3] > 0


def bbox(a: np.ndarray) -> tuple[int, int, int, int]:
    ys, xs = np.where(opaque(a))
    return ys.min(), ys.max(), xs.min(), xs.max()


def shift_upper(a: np.ndarray, hip: int, dy: int, dx: int = 0) -> np.ndarray:
    """Rows above `hip` move down by dy (and sideways by dx); rows from `hip` stay. Upper rows draw over lower ones."""
    h, w = a.shape[:2]
    out = np.zeros_like(a)
    out[hip:] = a[hip:]
    for y in range(hip):
        ny = y + dy
        if 0 <= ny < h:
            row = a[y]
            m = row[:, 3] > 0
            if dx:
                row = np.roll(row, dx, 0)
                m = np.roll(m, dx, 0)
            out[ny][m] = row[m]
    return out


def shear(a: np.ndarray, y_top: int, y_bottom: int, k: int) -> np.ndarray:
    """Integer row shear: rows between y_top and y_bottom move sideways by 0..k px (top moves most)."""
    out = a.copy()
    n = max(1, y_bottom - y_top)
    for y in range(y_top, y_bottom + 1):
        d = round(k * (y_bottom - y) / n)
        if d:
            out[y] = 0
            row = np.roll(a[y], d, 0)
            out[y] = row
    return out


def translate(a: np.ndarray, dx: int, dy: int = 0) -> np.ndarray:
    out = np.zeros_like(a)
    h, w = a.shape[:2]
    ys = slice(max(0, dy), min(h, h + dy))
    yd = slice(max(0, -dy), min(h, h - dy))
    xs = slice(max(0, dx), min(w, w + dx))
    xd = slice(max(0, -dx), min(w, w - dx))
    out[ys, xs] = a[yd, xd]
    return out


def clip_below_ground(a: np.ndarray) -> np.ndarray:
    out = a.copy()
    out[GROUND_ROW(a.shape[0]) + 2:] = 0
    return out


def components(mask: np.ndarray) -> list[list[tuple[int, int]]]:
    seen = np.zeros_like(mask)
    comps = []
    for y, x in zip(*np.where(mask)):
        if seen[y, x]:
            continue
        stack, comp = [(y, x)], []
        seen[y, x] = True
        while stack:
            cy, cx = stack.pop()
            comp.append((cy, cx))
            for dy, dx in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                ny, nx = cy + dy, cx + dx
                if 0 <= ny < mask.shape[0] and 0 <= nx < mask.shape[1] and mask[ny, nx] and not seen[ny, nx]:
                    seen[ny, nx] = True
                    stack.append((ny, nx))
        comps.append(comp)
    return comps


def hero_eyes(a: np.ndarray) -> np.ndarray:
    """INK-0 pixels that sit inside the Candle-D face patch."""
    face = (a[..., :3] == C("CANDLE-D")).all(-1) & opaque(a)
    ys, xs = np.where(face)
    m = np.zeros(face.shape, bool)
    if not len(ys):
        return m
    y0, y1, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    dark = (a[..., :3] == C("INK-0")).all(-1) & opaque(a)
    m[y0:y1 + 1, x0:x1 + 1] = dark[y0:y1 + 1, x0:x1 + 1]
    return m


def monster_eyes(a: np.ndarray, min_size: int = 2, left_frac: float = 1.0) -> np.ndarray:
    """Dark sockets: INK-0 blobs of at least 2 px in the upper half of the body (the outline is INK-4)."""
    y0, y1, _, _ = bbox(a)
    dark = (a[..., :3] == C("INK-0")).all(-1) & opaque(a)
    dark[y0 + round((y1 - y0) * 0.5):] = False
    _, _, x0, x1 = bbox(a)
    dark[:, x0 + round((x1 - x0) * left_frac):] = False  # the head sits in this part of the body
    out = np.zeros_like(dark)
    for comp in components(dark):
        if len(comp) >= min_size:
            for y, x in comp:
                out[y, x] = True
    return out


def shut_eyes(a: np.ndarray, eyes: np.ndarray) -> np.ndarray:
    out = a.copy()
    for comp in components(eyes):
        ys = [y for y, _ in comp]
        low = max(ys)
        for y, x in comp:
            if y != low:
                out[y, x] = (*P.rgb("CANDLE-D"), 255)
    return out


def dim_down(a: np.ndarray) -> np.ndarray:
    """Fallen hero: the ramp drops one step and the rim dims to Candle-D (5.6)."""
    out = a.copy()
    for src, dst in (("INK-2", "INK-1"), ("INK-3", "INK-2"), ("INK-4", "INK-3"), ("CANDLE", "CANDLE-D")):
        out[(a[..., :3] == C(src)).all(-1) & opaque(a)] = (*P.rgb(dst), 255)
    return out


def save(a: np.ndarray, path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(a, "RGBA").save(path)


def build_hero(frames: dict[str, np.ndarray], name: str = "tank") -> None:
    """One hero family from its raw idle and attack images (tank; alchemist added 2026-11-17)."""
    idle = PC.process(RAW / f"char_{name}_idle_01_raw.png", "hero", 48)
    y0, y1, x0, x1 = bbox(idle)
    hip = y0 + round((y1 - y0) * 0.62)
    eyes = hero_eyes(idle)
    frames[f"char_{name}_idle_01"] = idle
    frames[f"char_{name}_idle_02"] = shift_upper(idle, hip, 1)
    hurt1 = shut_eyes(translate(idle, -2), translate(eyes, -2))
    hurt2 = shut_eyes(shear(translate(shift_upper(idle, hip, 1), -1), y0, hip, -1), translate(eyes, -1) if False else np.roll(np.roll(eyes, 1, 0), -1, 1))
    frames[f"char_{name}_hurt_01"] = hurt1
    frames[f"char_{name}_hurt_02"] = hurt2
    d1 = shut_eyes(shift_upper(idle, hip, 6), np.roll(eyes, 6, 0))
    d2 = dim_down(shut_eyes(shift_upper(idle, hip, 12), np.roll(eyes, 12, 0)))
    frames[f"char_{name}_down_01"] = d1
    frames[f"char_{name}_down_02"] = clip_below_ground(d2)
    # narrow overlay (ready state): each eye loses its top row; the freed pixel returns to face colour
    ov = np.zeros_like(idle)
    for comp in components(eyes):
        ys = [y for y, _ in comp]
        if max(ys) == min(ys):
            continue
        top = min(ys)
        for y, x in comp:
            ov[y, x] = (*P.rgb("CANDLE-D" if y == top else "INK-0"), 255)
    frames[f"char_{name}_narrow_01"] = ov
    # attack key frames (user request 2026-11-16): wind-up and strike, generated from the idle with an edit, then
    # processed by the same rule as the idle so palette, outline and rim match
    frames[f"char_{name}_attack_01"] = PC.process(RAW / f"char_{name}_attack_01_raw.png", "hero", 48)
    frames[f"char_{name}_attack_02"] = PC.process(RAW / f"char_{name}_attack_02_raw.png", "hero", 48)


def build_monster(name: str, raw: str, canvas: int, arm_lift: bool, min_eye: int, left_frac: float, frames: dict[str, np.ndarray]) -> None:
    idle = PC.process(RAW / raw, "enemy", canvas)
    y0, y1, x0, x1 = bbox(idle)
    hip = y0 + round((y1 - y0) * 0.55)
    idle2 = shift_upper(idle, hip, 0, 1)
    if arm_lift:  # the oversized arm rises: the right third lifts 4 px
        w = x1 - x0 + 1
        cut = x0 + round(w * 0.68)
        act = idle.copy()
        act[:, cut:] = 0
        act[:y1 + 1 - 0, cut:][:-4] = idle[4:y1 + 1, cut:] if False else act[:y1 + 1, cut:][:-4]
        arm = idle[:, cut:].copy()
        act[:, cut:] = 0
        for y in range(canvas):
            if y - 4 >= 0:
                m = arm[y, :, 3] > 0
                act[y - 4, cut:][m] = arm[y][m]
    else:
        act = shear(idle, y0, hip, -2)  # the monster leans further toward the party (5.5)
    frames[f"mon_{name}_idle_01"] = idle
    frames[f"mon_{name}_idle_02"] = idle2
    frames[f"mon_{name}_act_01"] = act
    frames[f"mon_{name}_hurt_01"] = translate(idle, 2)
    frames[f"mon_{name}_die_01"] = translate(shift_upper(idle, hip, 2), 3)
    frames[f"mon_{name}_die_02"] = clip_below_ground(translate(shift_upper(idle, hip, 9), 4))
    for i, base in enumerate((idle, idle2, act), start=1):
        lit = np.zeros_like(idle)
        e = monster_eyes(base, min_eye, left_frac)
        lit[e] = (*P.rgb("FOG-HI"), 255)
        frames[f"mon_{name}_eyes_{i:02d}"] = lit


def main() -> int:
    frames: dict[str, np.ndarray] = {}
    build_hero(frames, "tank")
    if (RAW / "char_alchemist_idle_01_raw.png").exists():
        build_hero(frames, "alchemist")
    build_monster("swarmer", "mon_swarmer_idle_01_raw.png", 48, False, 1, 1.0, frames)
    build_monster("brute", "mon_brute_idle_01_raw.png", 64, True, 3, 0.42, frames)
    for name, a in frames.items():
        save(a, (CHARS if name.startswith("char_") else MONS) / f"{name}.png")
    print(f"wrote {len(frames)} frames")
    # evidence sheet: every frame at 4x on its camp's ground (INK-1 wall for heroes, INK-0 void for enemies)
    groups = [[n for n in frames if n.startswith(p)] for p in ("char_tank", "char_alchemist", "mon_swarmer", "mon_brute")]
    groups = [g for g in groups if g]
    S = 3
    rows = []
    for g in groups:
        canvas = frames[g[0]].shape[0]
        bg = P.rgb("INK-1" if g[0].startswith("char") else "INK-0")
        row = Image.new("RGB", (len(g) * (canvas * S + 4), canvas * S), P.rgb("INK-2"))
        for i, n in enumerate(g):
            tile = Image.new("RGBA", (canvas * S, canvas * S), bg + (255,))
            a = Image.fromarray(frames[n], "RGBA").resize((canvas * S, canvas * S), Image.NEAREST)
            tile.alpha_composite(a)
            row.paste(tile.convert("RGB"), (i * (canvas * S + 4), 0))
        rows.append(row)
    sheet = Image.new("RGB", (max(r.width for r in rows), sum(r.height + 4 for r in rows)), P.rgb("INK-0"))
    y = 0
    for r in rows:
        sheet.paste(r, (0, y))
        y += r.height + 4
    sheet.save(EVIDENCE / "characters_frames_2026-11-15.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
