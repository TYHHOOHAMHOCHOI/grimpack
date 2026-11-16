"""MVP item catalog: the single authored source for stats, skills and footprints.

Writes:
  Assets/_Project/Data/items_mvp.json            what the game and the hover popup read
  design/content/mvp-item-catalog.md             the same data as a reviewable table

Rules it follows (design/gdd): one trigger per item (item-trigger-and-cooldown 1), at most 4 effects and
3 synergy rules, MVP effect kinds Damage, ApplyStatus and ConsumeDetonate only (effect-resolution-model,
Spike Effect Kinds), passive items carry hpBonus and speedBonus (party-and-heroes), footprints are 1 to 9
edge-connected cells in a 3 x 3 box (grid-backpack), tiers White to Purple (Gold and Red need curses and
wait for later tiers). Every number is PROVISIONAL: balance belongs to the balance harness (System 35).

Run:  python -I tools/art/item_catalog.py
"""

from __future__ import annotations

import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))

# footprints as (x, y) cells, y down
FP = {
    "1x1": [(0, 0)],
    "1x2": [(0, 0), (0, 1)],
    "2x1": [(0, 0), (1, 0)],
    "1x3": [(0, 0), (0, 1), (0, 2)],
    "2x2": [(0, 0), (1, 0), (0, 1), (1, 1)],
    "2x3": [(0, 0), (1, 0), (0, 1), (1, 1), (0, 2), (1, 2)],
    "l": [(0, 0), (1, 0), (0, 1)],                  # axe: haft down the left, head across the top
    "l3": [(0, 0), (0, 1), (1, 1)],                 # boots: shaft down the left, foot along the bottom
    "t": [(0, 0), (1, 0), (2, 0), (1, 1), (1, 2)],  # hammer: head across the top, handle down
    "s": [(0, 0), (1, 0), (1, 1), (2, 1)],          # chain links
}
SIZE = {k: (max(x for x, _ in v) + 1, max(y for _, y in v) + 1) for k, v in FP.items()}


def dmg(base, target="FirstEnemy"):
    return {"kind": "Damage", "target": target, "base": base, "status": "", "amount": 0, "units": 0, "perUnit": 0}


def seal(amount, target="FirstEnemy"):
    return {"kind": "ApplyStatus", "target": target, "base": 0, "status": "an", "amount": amount, "units": 0, "perUnit": 0}


def blast(units, per_unit, target="FirstEnemy"):
    return {"kind": "ConsumeDetonate", "target": target, "base": 0, "status": "an", "amount": 0, "units": units, "perUnit": per_unit}


def shield(amount, target="Self"):
    return {"kind": "ShieldGain", "target": target, "base": 0, "status": "", "amount": amount, "units": 0, "perUnit": 0}


def heal(amount, target="Self"):
    return {"kind": "Heal", "target": target, "base": 0, "status": "", "amount": amount, "units": 0, "perUnit": 0}


def rule(kind, amount, tags=(), status=""):
    return {"targetTags": list(tags), "kind": kind, "amount": amount, "statusId": status}


def item(id_, tier, fp, vi, en, flavor_vi, flavor_en, tags, trigger, effects=(), hp=0, speed=0, rules=()):
    return {
        "id": "item_" + id_.replace("-", "_"), "art": id_, "tier": tier, "footprint": fp,
        "cells": [{"x": x, "y": y} for x, y in FP[fp]], "tags": list(tags), "trigger": trigger,
        "effects": list(effects), "hpBonus": hp, "speedBonus": speed, "synergy": list(rules),
        "nameVi": vi, "nameEn": en, "flavorVi": flavor_vi, "flavorEn": flavor_en,
    }


def cd(n, s=0):
    return {"kind": "Cooldown", "n": n, "s": s, "eventKind": "", "eventStatus": "", "eventSide": ""}


PASSIVE = {"kind": "Passive", "n": 0, "s": 0, "eventKind": "", "eventStatus": "", "eventSide": ""}
START = {"kind": "CombatStart", "n": 0, "s": 0, "eventKind": "", "eventStatus": "", "eventSide": ""}
ON_SEAL = {"kind": "Reaction", "n": 2, "s": 0, "eventKind": "StatusApplied", "eventStatus": "an", "eventSide": "enemy"}
ON_HIT_SELF = {"kind": "Reaction", "n": 2, "s": 0, "eventKind": "Damage", "eventStatus": "", "eventSide": "self"}

ITEMS = [
    # ---- White
    item("iron-sword", "White", "1x3", "Kiếm Sắt", "Iron Sword", "Nặng tay, chắc việc.", "Heavy and dependable.",
         ["weapon", "blade"], cd(2), [dmg(9)]),
    item("dagger", "White", "1x1", "Dao Găm", "Dagger", "Nhỏ, nhanh, hay bị quên.", "Small, quick, easily forgotten.",
         ["weapon", "blade"], cd(1), [dmg(4)]),
    item("hand-axe", "White", "l", "Rìu Tay", "Hand Axe", "Cán ngắn, lưỡi dày.", "Short haft, thick edge.",
         ["weapon", "axe"], cd(3), [dmg(12)]),
    item("seal-ink", "White", "1x1", "Mực Ấn", "Seal Ink", "Dấu in chưa khô.", "The mark is not yet dry.",
         ["seal"], cd(2), [seal(2)]),
    item("whetstone", "White", "1x1", "Đá Mài", "Whetstone", "Tiếng mài làm lưỡi dao sáng lên.", "The sound of an edge being made.",
         ["tool"], PASSIVE, rules=[rule("DamageFlat", 3, ["blade"])]),
    item("leather-vest", "White", "2x2", "Áo Da", "Leather Vest", "Đã sờn, vẫn ấm.", "Worn, still warm.",
         ["armour"], PASSIVE, hp=12),
    item("swift-boots", "White", "l3", "Giày Gió", "Swift Boots", "Đế mỏng, bước nhẹ.", "Thin soles, light steps.",
         ["boots"], PASSIVE, speed=3),
    item("iron-helm", "White", "1x1", "Mũ Sắt", "Iron Helm", "Móp ở một bên.", "Dented on one side.",
         ["armour"], PASSIVE, hp=6),
    item("spiked-buckler", "White", "1x2", "Khiên Gai", "Spiked Buckler", "Vừa đỡ đòn vừa húc trả.", "Blocks the blow and strikes back.",
         ["armour", "shield"], cd(2), [shield(6), dmg(3)]),
    item("oak-buckler", "White", "1x1", "Khiên Gỗ Sồi", "Oak Buckler", "Gỗ sồi nẹp sắt, nhẹ tay mà chắc.", "Iron-banded oak, light and steady.",
         ["armour", "shield"], cd(2), [shield(5)]),
    item("field-bandage", "White", "1x1", "Băng Cứu Thương", "Field Bandage", "Vải thô thấm thảo dược, buộc vội vẫn cầm máu.", "Coarse herb-soaked cloth; bound in haste, still stops the bleed.",
         ["tool", "heal"], cd(3), [heal(6)]),
    # ---- Green
    item("crossbow", "Green", "2x1", "Nỏ Gỗ", "Crossbow", "Một phát, một tiếng cạch.", "One shot, one click.",
         ["weapon", "ranged"], cd(3), [dmg(15)]),
    item("seal-stamp", "Green", "1x2", "Ấn Triện", "Seal Stamp", "Đóng xuống là không gỡ ra.", "Press down, and it will not lift.",
         ["seal"], cd(2), [seal(3)]),
    item("breaking-bell", "Green", "1x2", "Chuông Phá Ấn", "Breaking Bell", "Mỗi tiếng ngân làm một dấu ấn vỡ.", "Each ring cracks a seal.",
         ["detonator", "bell"], cd(3), [blast(4, 2)]),
    item("war-drum", "Green", "1x2", "Trống Chiến", "War Drum", "Nhịp trống làm vũ khí nặng thêm.", "The beat lends weight to every weapon.",
         ["instrument"], PASSIVE, rules=[rule("DamagePercent", 250, ["weapon"])]),
    item("ink-pot", "Green", "1x1", "Lọ Mực", "Ink Pot", "Đen đến mức nuốt cả ánh sáng.", "So black it swallows light.",
         ["tool"], PASSIVE, rules=[rule("StatusAmount", 1, ["seal"], "an")]),
    item("heart-charm", "Green", "1x1", "Bùa Tim", "Heart Charm", "Còn ấm như vừa rời tay người.", "Warm, as if just left a hand.",
         ["charm"], PASSIVE, hp=10),
    item("war-banner", "Green", "1x3", "Cờ Chiến", "War Banner", "Kéo lên trước khi bước vào.", "Raised before the first step in.",
         ["banner"], START, [seal(2, "AllEnemies")]),
    item("cleaver-halberd", "Green", "1x3", "Kích Phá Trận", "Sweeping Halberd", "Quét ngang một vòng, không chừa kẻ nào.", "Sweeps across the front line, sparing none.",
         ["weapon", "blade"], cd(3), [dmg(8, "AllEnemies")]),
    item("runed-mace", "Green", "1x2", "Chùy Khắc Ấn", "Runed Mace", "Mỗi cú nện để lại một vết ấn.", "Every blow leaves a burning mark.",
         ["weapon", "blunt", "seal"], cd(2), [dmg(6), seal(2)]),
    item("crimson-vial", "Green", "1x1", "Huyết Dược", "Crimson Vial", "Một ngụm đỏ thẫm, xương thịt liền lại.", "One crimson sip, and flesh knits anew.",
         ["potion", "heal"], cd(3), [heal(10)]),
    item("guard-sigil", "Green", "1x1", "Ấn Hộ Vệ", "Guard Sigil", "Chạm đòn là bừng sáng lớp chắn.", "Struck once, the ward flares to meet the next.",
         ["charm", "shield"], ON_HIT_SELF, [shield(5)]),
    # ---- Blue
    item("war-hammer", "Blue", "t", "Búa Chiến", "War Hammer", "Một nhát, cả hang rung.", "One blow, and the cave shakes.",
         ["weapon", "blunt"], cd(4), [dmg(28)]),
    item("seal-chain", "Blue", "s", "Xích Ấn", "Seal Chain", "Mỗi mắt xích một dấu.", "A mark on every link.",
         ["seal", "chain"], cd(3), [seal(2, "AllEnemies")]),
    item("thunder-gong", "Blue", "2x2", "Chiêng Sấm", "Thunder Gong", "Tiếng chiêng gọi sấm xuống các dấu ấn.",
         "Its voice calls thunder down on the marks.", ["detonator", "bell"], cd(4), [blast(10, 3)]),
    item("hourglass", "Blue", "1x2", "Đồng Hồ Cát", "Hourglass", "Cát chảy ngược khi không ai nhìn.", "The sand runs upward when no one looks.",
         ["relic"], PASSIVE, rules=[rule("CooldownReduction", 1)]),
    item("echo-charm", "Blue", "1x1", "Bùa Vang", "Echo Charm", "Nghe thấy dấu ấn, nó đáp lại.", "It answers when a seal is set.",
         ["charm"], ON_SEAL, [dmg(4)]),
    item("whirlwind-axe", "Blue", "t", "Đại Phủ Cuồng Phong", "Whirlwind Axe", "Lưỡi rìu hai đầu xoay tròn qua cả hàng quái.", "A twin-bit arc that carves through every foe.",
         ["weapon", "axe"], cd(4), [dmg(14, "AllEnemies")]),
    item("oath-blade", "Blue", "1x3", "Thánh Kiếm Lời Thề", "Oath Blade", "Lời thề giữ thành không đổi qua trăm trận.", "An oath that holds the line through a hundred fights.",
         ["weapon", "blade", "shield"], cd(3), [dmg(14), shield(7)]),
    item("sanctuary-relic", "Blue", "1x2", "Thánh Tích Hồi Sinh", "Sanctuary Relic", "Ánh nến trong lồng kính giữ hơi thở không tắt.", "A caged ember that keeps breath and ward alike.",
         ["relic", "heal", "shield"], cd(4), [heal(9), shield(9)]),
    # ---- Purple
    item("void-censer", "Purple", "2x3", "Lư Hương Hư Vô", "Void Censer", "Khói của nó nuốt mọi dấu ấn.", "Its smoke swallows every seal.",
         ["detonator", "incense"], cd(5), [blast(99, 4)]),
    item("scholar-lens", "Purple", "1x1", "Kính Học Giả", "Scholar Lens", "Nhìn thấy chỗ dấu ấn sẽ vỡ.", "It shows where the seal will break.",
         ["relic"], PASSIVE, rules=[rule("StatusAmount", 2, ["seal"], "an"), rule("DamagePercent", 500, ["detonator"])]),
    item("tower-shield", "Purple", "2x3", "Khiên Tháp", "Tower Shield", "Đứng sau nó, bạn chậm hơn và sống lâu hơn.",
         "Behind it you are slower, and last longer.", ["armour"], PASSIVE, hp=30, speed=-2),
    item("resonant-bell", "Purple", "2x2", "Chuông Cộng Hưởng", "Resonant Bell", "Một hồi chuông vang, mọi dấu ấn đều nứt vỡ cùng lúc.", "One toll, and every mark across the room shatters at once.",
         ["detonator", "bell"], cd(4), [blast(8, 3, "AllEnemies")]),
]


def validate() -> list[str]:
    errors = []
    ids = set()
    for it in ITEMS:
        if it["id"] in ids:
            errors.append(f"duplicate id {it['id']}")
        ids.add(it["id"])
        k = it["trigger"]["kind"]
        if (k == "Passive") != (not it["effects"]):
            errors.append(f"{it['id']}: passive items have no effects and every other item has at least one")
        if len(it["effects"]) > 4:
            errors.append(f"{it['id']}: more than 4 effects")
        if len(it["synergy"]) > 3:
            errors.append(f"{it['id']}: more than 3 synergy rules")
        if k in ("Cooldown", "Reaction") and not 1 <= it["trigger"]["n"] <= 20:
            errors.append(f"{it['id']}: cooldown out of 1..20")
        if k != "Passive" and (it["hpBonus"] or it["speedBonus"]):
            errors.append(f"{it['id']}: stat bonuses belong to passive items")
        if not -20 <= it["hpBonus"] <= 40:
            errors.append(f"{it['id']}: hpBonus out of -20..40 (party-and-heroes)")
        for e in it["effects"]:
            if e["kind"] not in ("Damage", "ApplyStatus", "ConsumeDetonate", "ShieldGain", "Heal"):
                errors.append(f"{it['id']}: effect kind {e['kind']} is unsupported")
            if e["kind"] == "Damage" and not 1 <= e["base"] <= 100000:
                errors.append(f"{it['id']}: damage base out of range")
            if e["kind"] in ("ApplyStatus", "ShieldGain", "Heal") and not 1 <= e["amount"] <= 100000:
                errors.append(f"{it['id']}: amount out of range")
        cells = FP[it["footprint"]]
        w, h = SIZE[it["footprint"]]
        if w > 3 or h > 3 or not 1 <= len(cells) <= 9:
            errors.append(f"{it['id']}: footprint outside a 3 x 3 box")
        seen, stack = {cells[0]}, [cells[0]]
        while stack:
            x, y = stack.pop()
            for n in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
                if n in cells and n not in seen:
                    seen.add(n)
                    stack.append(n)
        if len(seen) != len(cells):
            errors.append(f"{it['id']}: footprint is not edge-connected")
    return errors


def md() -> str:
    lines = [
        "# MVP item catalog",
        "",
        "> **Status**: Updated for 1-Hero Crusader Combat, 2026-11-16. Generated from `tools/art/item_catalog.py`; edit the script, not this file.",
        "> **Every number is provisional.** Balance belongs to the balance harness (System 35).",
        "",
        "Follows `item-trigger-and-cooldown.md`, `effect-resolution-model.md` (extended with `ShieldGain`, `Heal`, and `Self` for solo Crusader survival),",
        "`adjacency-and-synergy.md`, `party-and-heroes.md` (passive `hpBonus` and `speedBonus`), and `grid-backpack.md` (footprints).",
        "",
        "| # | Item | Tier | Shape | Trigger | Skill (effects) | Stats | Synergy (adjacent items) | Tags |",
        "|---|------|------|-------|---------|-----------------|-------|--------------------------|------|",
    ]
    for i, it in enumerate(ITEMS, 1):
        t = it["trigger"]
        if t["kind"] == "Reaction":
            if t["eventKind"] == "Damage":
                trig = f"Reaction (Owner takes damage), cooldown {t['n']}"
            else:
                trig = f"Reaction (Ấn applied on an enemy), cooldown {t['n']}"
        else:
            trig = {"Passive": "Passive", "CombatStart": "Combat start", "Cooldown": f"Cooldown {t['n']}"}[t["kind"]]
        fx = []
        for e in it["effects"]:
            if e["kind"] == "Damage":
                fx.append(f"Damage {e['base']} → {e['target']}")
            elif e["kind"] == "ApplyStatus":
                fx.append(f"Apply {e['amount']} Ấn → {e['target']}")
            elif e["kind"] == "ShieldGain":
                fx.append(f"Shield +{e['amount']} → {e['target']}")
            elif e["kind"] == "Heal":
                fx.append(f"Heal +{e['amount']} → {e['target']}")
            else:
                fx.append(f"Detonate up to {e['units']} Ấn × {e['perUnit']} → {e['target']}")
        stats = []
        if it["hpBonus"]:
            stats.append(f"{it['hpBonus']:+d} max HP")
        if it["speedBonus"]:
            stats.append(f"{it['speedBonus']:+d} speed")
        syn = []
        for r in it["synergy"]:
            who = "/".join(r["targetTags"]) or "every neighbour"
            amt = {"DamageFlat": f"{r['amount']:+d} damage", "DamagePercent": f"{r['amount'] / 10:+g}% damage",
                   "CooldownReduction": f"cooldown {-r['amount']:+d}", "StatusAmount": f"{r['amount']:+d} {r['statusId']} applied"}[r["kind"]]
            syn.append(f"{who}: {amt}")
        lines.append(f"| {i} | {it['nameVi']} / {it['nameEn']} (`{it['id']}`) | {it['tier']} | {it['footprint']} ({len(it['cells'])} cells) | "
                     f"{trig} | {'; '.join(fx) or '-'} | {', '.join(stats) or '-'} | {'; '.join(syn) or '-'} | {', '.join(it['tags'])} |")
    counts = {t: sum(1 for i in ITEMS if i["tier"] == t) for t in ("White", "Green", "Blue", "Purple")}
    shapes = sorted({i["footprint"] for i in ITEMS})
    lines += [
        "", f"{len(ITEMS)} items. Per tier: {counts}. Footprints used: {', '.join(shapes)}.",
        "", "## Design intent", "",
        "- **Three jobs, one chain + solo Crusader sustain.** Seals (ApplyStatus Ấn) set up, detonators (ConsumeDetonate) cash in, passive relics",
        "  tune either end, and active Shield/Heal/Cleave gear lets a solo Tank survive and clear 1..6 monsters.",
        "- **Rhythm is the cost.** Strong pieces wait longer (cooldown 4 to 5) and take more cells (T, S, 2×3), so",
        "  the grid pays for power in space and in time.",
        "- **Passive stats are small and honest.** `hpBonus` stays inside the `-20..40` range, and the Tower Shield trades 2 speed for 30 HP.",
        "- **Synergy is local.** Every rule sits on a neighbour link (System 10); none crosses hero grids.",
        "- **Shapes match the object.** A sword/halberd is 1×3, a hammer/whirlwind axe a T, a chain an S, a bell 1×2, a gong/resonant bell 2×2. Art fills the footprint.",
        "",
    ]
    return "\n".join(lines)


def main() -> int:
    errors = validate()
    if errors:
        print("\n".join(errors))
        return 1
    (ROOT / "Assets/_Project/Data").mkdir(parents=True, exist_ok=True)
    (ROOT / "Assets/_Project/Data/items_mvp.json").write_text(json.dumps({"items": ITEMS}, ensure_ascii=False, indent=1), encoding="utf-8")
    (ROOT / "design/content").mkdir(parents=True, exist_ok=True)
    (ROOT / "design/content/mvp-item-catalog.md").write_text(md(), encoding="utf-8")
    print(f"{len(ITEMS)} items validated and written")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
