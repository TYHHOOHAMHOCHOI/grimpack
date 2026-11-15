"""Full-frame mock-ups of the COMBAT screen at 640x360 in the master palette (art bible 7.3 to 7.5, 5.10, 5.11).

Everything is composed from the shipped asset files (characters, plates, glyphs, numerals,
HUD pieces, effects), so the mock-ups double as an integration check of the art set.

  combat_party_acting   a hero acts: two avatars above its head, readout chips and damage numerals on the targets,
                        its plates pulse in the workbench, the targeted enemy has lit eyes
  combat_enemy_acting   an enemy acts: act pose, lit eyes, Fog-hi turn chevron, its glyph lifted off the tab and
                        crossing the gutter, a hurt hero with a damage-taken numeral, a fallen hero
  combat_result         the result panel that replaces the enemy zone after the fight (7.5)

Plate icons are placeholders (no item art exists yet).
Run:  python -I tools/art/build_combat_mock.py [OUT_DIR]
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402
import build_menu_kit as K  # noqa: E402
import numerals as N  # noqa: E402

ART = ROOT / "Assets/_Project/Art"
Img = K.Img


def load(rel: str) -> Img:
    a = np.array(Image.open(ART / rel).convert("RGBA"))
    o = Img(a.shape[1], a.shape[0])
    o.a[:] = a
    return o


def recolour(o: Img, src: str, dst: str) -> Img:
    m = (o.a[..., :3] == np.array(P.rgb(src))).all(-1) & (o.a[..., 3] > 0)
    out = Img(o.w, o.h)
    out.a[:] = o.a
    out.a[m, :3] = P.rgb(dst)
    return out


def num(im: Img, text: str, x: int, y: int, cut: int = 8, colour: str | None = None) -> int:
    a = N.render(text, cut)
    o = Img(a.shape[1], a.shape[0])
    o.a[:] = a
    if colour:
        o = recolour(o, "BONE", colour)
    im.paste(o, x, y)
    return x + o.w


def num_w(text: str, cut: int = 8) -> int:
    return N.render(text, cut).shape[1]


def glyph(im: Img, name: str, skin: str, size: int, x: int, y: int) -> None:
    im.paste(load(f"UI/ui_glyph-{name}_{skin}_{size}x{size}.png"), x, y)


def ellipse_pool(im: Img, cx: int, cy: int, rx: int, ry: int, colour: str) -> None:
    for y in range(cy - ry, cy + ry + 1):
        for x in range(cx - rx, cx + rx + 1):
            if ((x - cx) / (rx + .5)) ** 2 + ((y - cy) / (ry + .5)) ** 2 <= 1:
                im.px(x, y, colour)


def panel(im: Img, x: int, y: int, w: int, h: int, fill: str = "INK-1") -> None:
    im.rect(x, y, w, h, "INK-3"); im.rect(x + 3, y + 3, w - 6, h - 6, fill)
    im.rect(x + 3, y + 3, w - 6, 1, "INK-4"); im.rect(x + 3, y + 3, 1, h - 6, "INK-4")


def chip(im: Img, x: int, y: int, w: int, h: int) -> None:
    im.rect(x, y, w, h, "INK-1"); im.frame(x, y, w, h, 1, "INK-4")


def plate(im: Img, tier: str, shape: str, x: int, y: int, state: str = "rest", icon: str | None = None) -> None:
    p = load(f"Items/Plates/plate_{tier}_{state}_{shape}.png")
    im.paste(p, x, y)
    if icon:
        cx, cy = x + (p.w - (2 if state.startswith("lift") else 0)) // 2, y + (p.h - (2 if state.startswith("lift") else 0)) // 2
        glyph(im, icon, "bone", 12, cx - 6, cy - 6)


def hp_row(im: Img, cx: int, y: int, cur: int, mx: int) -> None:
    text = f"{cur}/{mx}"
    total = 8 + 2 + 36 + 3 + num_w(text)
    x = cx - total // 2
    low = cur / mx <= 0.3
    glyph(im, "heart", "blood", 8, x - 1, y)
    im.paste(load("UI/ui_hp-bar_frame-low_36x8.png" if low and cur > 0 else "UI/ui_hp-bar_frame_36x8.png"), x + 10, y + 1)
    if cur:
        fill = load("UI/ui_hp-bar_fill_34x6.png")
        fill.w = max(1, round(34 * cur / mx))
        fill.a = fill.a[:, :fill.w]
        im.paste(fill, x + 11, y + 2)
    num(im, text, x + 49, y + 1, 8)


def stat_row(im: Img, cx: int, y: int, armour: int, gauge: int, statuses: list[tuple[str, int, str]]) -> None:
    width = 10 + 8 + 4 + 20 + 3 + 21 * len(statuses)
    x = cx - width // 2
    glyph(im, "shield", "fog", 8, x - 1, y + 1)
    num(im, str(armour), x + 10, y + 2, 8)
    im.paste(load("UI/ui_speed-gauge_frame_20x6.png"), x + 20, y + 3)
    im.rect(x + 21, y + 6, gauge, 2, "FOG")
    x += 44
    for name, n, direction in statuses:
        chip(im, x, y, 20, 12)
        glyph(im, name, "bone", 8, x + 1, y + 2)
        num(im, str(n), x + 11, y + 2, 8)
        if direction:
            im.paste(load(f"UI/ui_chevron-{direction[0]}_{direction[1]}_6x4.png"), x + 13, y + 8) if False else None
        x += 22


def intent_tab(im: Img, cx: int, y: int, intent: str | None, mag: str, target: int) -> None:
    im.paste(load("UI/ui_tab-intent_default_46x24.png"), cx - 23, y)
    if intent:  # the glyph slot goes dark while the glyph is away
        glyph(im, intent, "fog-hi", 12, cx - 20, y + 3)
    num(im, mag, cx - 6, y + 5, 8, "FOG-HI")
    chip(im, cx + 11, y + 3, 8, 11)
    num(im, str(target), cx + 12, y + 5, 8, "FOG-HI")


def readout_chip(im: Img, cx: int, y: int, glyph_name: str, direction: str, value: str) -> None:
    """Stat chip: stat glyph, chevron, signed number, all Bone (5.11)."""
    w = 8 + 2 + 6 + 2 + num_w(value) + 4
    x = cx - w // 2
    chip(im, x, y, w, 12)
    glyph(im, glyph_name, "bone", 8, x + 1, y + 2)
    im.paste(load(f"UI/ui_chevron-{'buff_up' if direction == 'up' else 'debuff_down'}_6x4.png"), x + 11, y + 4)
    num(im, value, x + 19, y + 2, 8)


def damage_numeral(im: Img, cx: int, y: int, value: str, cut: int, taken: bool) -> None:
    w = num_w(value, cut) + (num_w("-", cut) + 1 if taken else 0)
    x = cx - w // 2
    if taken:  # a Blood minus, then the Bone numerals (4.6)
        m = N.render("-", cut)
        o = Img(m.shape[1], m.shape[0]); o.a[:] = m
        im.paste(recolour(o, "BONE", "BLOOD"), x, y)
        x += num_w("-", cut) + 1
    num(im, value, x, y, cut)


def sprite(rel: str) -> Img:
    return load(rel)


def frame_base() -> Img:
    im = Img(640, 360, "INK-0")
    im.rect(0, 0, 640, 16, "INK-1")
    im.paste(load("UI/ui_glyph-coin_default_12x12.png"), 3, 2)
    num(im, "120", 18, 4, 8)
    num(im, "3/8", 52, 4, 8, "FOG")
    im.paste(load("UI/ui_glyph-menu_bone_8x8.png"), 624, 4)
    panel(im, 0, 16, 174, 344)
    panel(im, 174, 16, 466, 344, "INK-0")
    im.rect(177, 19, 137, 314, "INK-1")
    for yy in range(33, 333, 20):
        im.rect(177, yy, 137, 1, "INK-2")
    im.rect(314, 19, 1, 314, "INK-3")
    im.rect(177, 333, 460, 24, "INK-1"); im.rect(177, 333, 460, 1, "INK-3")
    return im


def speed_strip(im: Img, current: int = 0) -> None:
    x = 189
    for i, (label, w) in enumerate([("1x", 28), ("2x", 28), ("4x", 28), ("Bỏ qua", 52)]):
        im.rect(x, 338, w, 14, "INK-1")
        im.frame(x, 338, w, 14, 1, "FOG" if i == current else "INK-4")
        tw = sum(K._GLYPHS[ord(c)][1] for c in label) - 1
        K.text(im, label, x + (w - tw) // 2, 337, "BONE")
        if i == current:
            im.frame(x - 2, 336, w + 4, 18, 1, "CANDLE"); im.rect(x - 2, 336, 4, 4, "CANDLE")
        x += w + 8
    chip(im, 598, 338, 30, 14); K.text(im, "Tab", 605, 337, "FOG")


def order_strip(im: Img, seq: list[tuple[str, str]]) -> None:
    x = 182
    for i, (kind, v) in enumerate(seq):
        chip(im, x, 2, 12, 12)
        if kind == "h":
            num(im, v, x + 4, 4, 8)
        else:
            glyph(im, v, "fog-hi", 8, x + 2, 3)
        if i == 0:
            im.rect(x + 3, 14, 6, 2, "CANDLE")
        x += 15
    K.text(im, "nếu không có gì đổi", 276, 2, "FOG")


def workbench(im: Img, firing: list[int]) -> None:
    chip(im, 7, 22, 14, 12); num(im, "2", 12, 24, 8)
    im.paste(load("UI/ui_chevron-turn_hero_7x4.png"), 24, 26)
    K.text(im, "Alchemist", 34, 22, "BONE")
    im.rect(7, 40, 80, 80, "INK-2")
    for i in range(1, 4):
        im.rect(7 + 20 * i, 40, 1, 80, "INK-1"); im.rect(7, 40 + 20 * i, 80, 1, "INK-1")
    plate(im, "green", "1x1", 7, 40, "hi" if 0 in firing else "rest", "attack")
    plate(im, "blue", "1x2", 27, 40, "hi" if 1 in firing else "rest", "speed")
    plate(im, "purple", "2x2", 47, 60, "hi" if 2 in firing else "rest", "damage")
    im.rect(7, 128, 160, 1, "INK-3")
    K.text(im, "Kho đồ", 7, 134, "BONE")
    im.rect(7, 152, 160, 40, "INK-2")
    for i in range(1, 8):
        im.rect(7 + 20 * i, 152, 1, 40, "INK-1")
    im.rect(7, 172, 160, 1, "INK-1")
    plate(im, "gold", "1x1", 7, 152, "rest", "shield")
    plate(im, "white", "1x1", 47, 172, "rest", "droplet")
    plate(im, "red", "1x1", 87, 152, "rest", "seal")


def hero_slot(im: Img, k: int, frame: str, hp: tuple[int, int], pedestal: str = "default", extra: Img | None = None,
              statuses: list | None = None, armour: int = 0, gauge: int = 14, lift: int = 0) -> tuple[int, int]:
    cx = 177 + 70
    top = 35 + 94 * k
    ground = top + 64
    im.paste(load(f"Props/prop_pedestal_{pedestal}_52x12.png"), cx - 26, ground - 2)
    spr = load(f"Characters/{frame}.png")
    im.paste(spr, cx - 24, ground - 44 - lift)
    if extra:
        im.paste(extra, cx - 24, ground - 44 - lift)
    if pedestal != "dim":
        num(im, str(k + 1), cx - 2, ground, 8)
    else:
        num(im, str(k + 1), cx - 2, ground, 8, "INK-4")
    hp_row(im, cx, top + 72, *hp)
    if hp[0]:
        stat_row(im, cx, top + 82, armour, gauge, statuses or [])
    return cx, top


def enemy_slot(im: Img, top: int, body: int, frame: str, hp: tuple[int, int], intent: str | None, mag: str, target: int,
               eyes: str | None = None, acting: bool = False, statuses: list | None = None, line: int = 0) -> tuple[int, int]:
    ex = 314 + 70 + 140 * line
    ground = top + 26 + body + 4
    intent_tab(im, ex, top, intent, mag, target)
    im.paste(load("Props/prop_shadow-contact_default_30x4.png"), ex - 15, ground)
    spr = load(f"Monsters/{frame}.png")
    im.paste(spr, ex - spr.w // 2, ground - (spr.h - 4))
    if eyes:
        im.paste(load(f"Monsters/{eyes}.png"), ex - spr.w // 2, ground - (spr.h - 4))
    if acting:
        im.paste(load("UI/ui_chevron-turn_foe_7x4.png"), ex - 3, ground)
    hp_row(im, ex, ground + 4, *hp)
    stat_row(im, ex, ground + 14, 0, 12, statuses or [])
    return ex, ground


def party_acting() -> Img:
    im = frame_base()
    workbench(im, firing=[0, 1])
    order_strip(im, [("h", "2"), ("e", "multi"), ("h", "3"), ("e", "attack"), ("h", "1"), ("e", "multi")])
    speed_strip(im, 0)
    h1 = hero_slot(im, 0, "char_tank_idle_01", (86, 110), armour=4)
    cx, top = hero_slot(im, 1, "char_tank_idle_02", (64, 92), extra=load("Characters/char_tank_narrow_01.png"), lift=1, statuses=[("droplet", 3, "")])
    hero_slot(im, 2, "char_tank_idle_01", (82, 82))
    # two avatars above the acting hero: the finishing one slides one avatar width toward the enemy lines
    plate(im, "blue", "1x1", cx - 11 + 22, top + 1, "lift", "speed")
    plate(im, "green", "1x1", cx - 11, top + 1, "lifthi", "attack")
    # targets: swarmer takes damage, brute is targeted (lit eyes)
    sy = 19 + 66
    ex, ground = enemy_slot(im, sy, 26, "mon_swarmer_idle_01", (40, 52), "multi", "4x3", 1)
    damage_numeral(im, ex, ground - 26, "12", 16, False)
    im.paste(load("VFX/vfx_impact_spark_02.png"), ex - 8, ground - 22)
    ex2, g2 = enemy_slot(im, sy + 82, 44, "mon_brute_idle_01", (88, 88), "attack", "18", 2, eyes="mon_brute_eyes_01", statuses=[("seal", 2, "")])
    readout_chip(im, ex2 + 6, g2 - 30, "speed", "down", "-2")
    return im


def enemy_acting() -> Img:
    im = frame_base()
    workbench(im, firing=[])
    order_strip(im, [("e", "attack"), ("h", "1"), ("e", "multi"), ("h", "2"), ("e", "attack"), ("h", "3")])
    speed_strip(im, 1)
    cx, top = hero_slot(im, 0, "char_tank_hurt_01", (31, 110), armour=0)
    damage_numeral(im, cx - 4, top + 44, "18", 16, True)
    hero_slot(im, 1, "char_tank_idle_01", (64, 92), statuses=[("droplet", 3, "")])
    hero_slot(im, 2, "char_tank_down_02", (0, 82), pedestal="dim")
    sy = 19 + 66
    enemy_slot(im, sy, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 1)
    ex, g = enemy_slot(im, sy + 82, 44, "mon_brute_act_01", (88, 88), None, "18", 2, eyes="mon_brute_eyes_03", acting=True,
                       statuses=[("seal", 2, "")])
    # the bare Fog-hi glyph lifts off the tab and crosses the gutter on a stepped diagonal toward the target's body centre
    for i, (x, y) in enumerate([(360, 215), (338, 190), (316, 164), (294, 138)]):
        glyph(im, "attack", "fog-hi", 12, x, y)
    return im


def result() -> Img:
    im = frame_base()
    workbench(im, firing=[])
    order_strip(im, [("h", "1"), ("e", "multi"), ("h", "2"), ("e", "attack"), ("h", "3"), ("e", "multi")])
    im.rect(177, 333, 460, 24, "INK-1")
    chip(im, 189, 338, 60, 14); K.text(im, "Đóng", 205, 337, "BONE")
    hero_slot(im, 0, "char_tank_idle_01", (86, 110), armour=4)
    hero_slot(im, 1, "char_tank_idle_02", (64, 92))
    hero_slot(im, 2, "char_tank_idle_01", (82, 82))
    # result panel across the right column
    x0, y0 = 322, 27
    K.text(im, "Kết quả", x0, y0, "BONE")
    for i, lab in enumerate(["1", "2", "3", "Tất cả"]):
        w = 14 if lab != "Tất cả" else 38
        chip(im, 560 + (i * 18 if i < 3 else 54), y0 + 1, w, 12)
        if lab != "Tất cả":
            num(im, lab, 560 + i * 18 + 5, y0 + 3, 8)
        else:
            K.text(im, lab, 560 + 54 + 3, y0, "BONE")
    rows = [("green", "attack", "Dao găm", 214), ("blue", "speed", "Giày gió", 188), ("purple", "damage", "Búa nổ", 156),
            ("gold", "shield", "Khiên sắt", 94), ("white", "droplet", "Bình độc", 61), ("red", "seal", "Ấn đỏ", 33)]
    total = max(v for *_, v in rows)
    for r, (tier, icon, name, v) in enumerate(rows):
        y = y0 + 22 + r * 28
        num(im, str(r + 1), x0 + 2, y + 7, 8)
        plate(im, tier, "1x1", x0 + 16, y + 3, "rest", icon)
        K.text(im, name, x0 + 42, y + 5, "BONE")
        im.rect(x0 + 130, y + 8, 120, 6, "INK-2")
        im.rect(x0 + 130, y + 8, round(120 * v / total), 6, "BONE")
        num(im, str(v), x0 + 258, y + 3, 12)
    return im


def planning() -> Img:
    im = frame_base()
    workbench(im, firing=[])
    order_strip(im, [("h", "1"), ("e", "multi"), ("h", "2"), ("e", "attack"), ("h", "3"), ("e", "multi")])
    hero_slot(im, 0, "char_tank_idle_01", (110, 110), armour=4)
    hero_slot(im, 1, "char_tank_idle_01", (92, 92))
    hero_slot(im, 2, "char_tank_idle_01", (82, 82))
    sy = 19 + 66
    enemy_slot(im, sy, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 1)
    enemy_slot(im, sy + 82, 44, "mon_brute_idle_01", (88, 88), "attack", "18", 2)
    # the primary action: Candle 2 px frame, Bone text, with its prompt chip (7.3)
    im.rect(177, 336, 460, 18, "INK-1")
    im.frame(500, 336, 130, 18, 2, "CANDLE")
    K.text(im, "Bắt đầu trận", 535, 339, "BONE")
    chip(im, 478, 338, 18, 14); K.text(im, "Ent", 479, 337, "FOG")
    return im


def details() -> Img:
    im = party_acting_base()
    # intent Details open in the workbench lower section (read-only; the stage keeps no popup)
    im.rect(4, 128, 166, 228, "INK-1")
    K.text(im, "Ý định: Brute", 7, 134, "BONE")
    im.rect(7, 150, 160, 1, "INK-3")
    seq = [("attack", "18", 2), ("windup", "", 0), ("attack", "26", 1), ("multi", "6x2", 3), ("attack", "18", 2), ("sweep", "9", 0),
           ("attack", "18", 1), ("windup", "", 0), ("attack", "26", 3), ("multi", "6x2", 2), ("attack", "18", 1), ("sweep", "9", 0)]
    for i, (g, mag, tgt) in enumerate(seq):
        col, row = i // 6, i % 6
        x, y = 7 + col * 82, 158 + row * 22
        chip(im, x, y, 78, 18)
        num(im, str(i + 1), x + 3, y + 5, 8, "FOG")
        glyph(im, g, "fog-hi", 12, x + 14, y + 3)
        if mag:
            num(im, mag, x + 30, y + 5, 8, "FOG-HI")
        if tgt:
            chip(im, x + 64, y + 3, 10, 12); num(im, str(tgt), x + 66, y + 5, 8, "FOG-HI")
        if i == 3:
            im.frame(x - 1, y - 1, 80, 20, 1, "CANDLE")   # the entry the tab shows now
    K.text(im, "lặp lại sau mục 12", 7, 296, "FOG")
    return im


def party_acting_base() -> Img:
    im = frame_base()
    workbench(im, firing=[])
    order_strip(im, [("e", "attack"), ("h", "1"), ("e", "multi"), ("h", "2"), ("e", "attack"), ("h", "3")])
    speed_strip(im, 1)
    hero_slot(im, 0, "char_tank_idle_01", (86, 110), armour=4)
    hero_slot(im, 1, "char_tank_idle_02", (64, 92), statuses=[("droplet", 3, "")])
    hero_slot(im, 2, "char_tank_idle_01", (82, 82))
    sy = 19 + 66
    enemy_slot(im, sy, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 1)
    enemy_slot(im, sy + 82, 44, "mon_brute_idle_01", (88, 88), "attack", "18", 2)
    return im


def dense() -> Img:
    """Six enemies in two lines (the maximum): the fixture for acceptance criterion 16 (dense layout)."""
    im = frame_base()
    workbench(im, firing=[])
    order_strip(im, [("h", "1"), ("e", "multi"), ("h", "2"), ("e", "attack"), ("h", "3"), ("e", "multi")])
    speed_strip(im, 0)
    hero_slot(im, 0, "char_tank_idle_01", (86, 110), armour=4)
    hero_slot(im, 1, "char_tank_idle_01", (64, 92), statuses=[("droplet", 3, ""), ("seal", 12, "")])
    hero_slot(im, 2, "char_tank_idle_01", (82, 82))
    line1 = [(82, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 1, []),
             (100, 44, "mon_brute_idle_01", (88, 88), "attack", "18", 2, [("seal", 2, ""), ("droplet", 5, "")]),
             (82, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 3, [])]
    line2 = [(82, 26, "mon_swarmer_idle_01", (31, 52), "multi", "4x3", 2, [("seal", 4, "")]),
             (82, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 1, []),
             (82, 26, "mon_swarmer_idle_01", (52, 52), "multi", "4x3", 3, [])]
    for line, group in enumerate((line1, line2)):
        y = 19 + (314 - sum(g[0] for g in group)) // 2
        for pitch, body, frame, hp, intent, mag, tgt, st in group:
            enemy_slot(im, y, body, frame, hp, intent, mag, tgt, statuses=st, line=line)
            y += pitch
    return im


def audit(im: Img) -> str:
    pal = {tuple(P.rgb(n)) for n in P.PALETTE}
    a = im.a.reshape(-1, 4)
    off = {tuple(p) for p in np.unique(a[:, :3], axis=0)} - pal
    return f"off-palette colours: {len(off)}, alpha {sorted(set(a[:, 3].tolist()))}"


def main() -> int:
    out = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "production/qa/evidence"
    out.mkdir(parents=True, exist_ok=True)
    for name, fn in (("combat_party_acting", party_acting), ("combat_enemy_acting", enemy_acting), ("combat_result", result),
                     ("combat_planning", planning), ("combat_details", details), ("combat_dense", dense)):
        im = fn()
        im.save(out / f"{name}_2026-11-15.png")
        print(name, audit(im))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
