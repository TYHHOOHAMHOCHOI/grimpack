"""Generated UI kit pipeline (design/art/ui-kit-generated.md).

Turns Pixellab outputs saved under art-source/ai-raw/ui/ into palette-locked Unity sprites under
Assets/_Project/Art/UI/Gen/ (and Art/VFX/Gen/), driven by tools/art/ui_gen_manifest.json. Each entry:

    name      output file name (art bible 8.3 style), without .png
    raw       file under art-source/ai-raw/ui/
    out       "ui" (default) or "vfx"
    crop      [x, y, w, h] in raw pixels, or "first" / "second" (the n-th opaque object, left to right)
    reduce    1 = raw is native, 2 or 4 = raw was drawn at that multiple (exact k-block reduction)
    size      [w, h] final size (nearest-neighbour fit after reduction, used only for small corrections)
    palette   names to snap to (default: the whole master palette), nearest colour in OKLab
    remap     material remap by value instead of nearest colour, for generator colours that are too bright or off-hue:
              {"warm": [["INK-0", 0.15], ["UMBER-1", 0.55], ...], "cool": [[...]], "warmSat": 0.18}
              pixels are split warm (hue red..yellow and saturated) or cool, each class sorted by luminance and cut into
              the listed palette colours by the given shares (bottom to top)
    opaque    true: fill transparent pixels with the darkest ramp colour (backdrops)
    border    [l, t, r, b] 9-slice insets, written to borders.json for the scene builder
    variants  {"suffix": {"from": "to", ...}}: extra files with palette indices swapped (disabled, defeat, pressed)

Prints observations (size, colours, share of each colour); it does not judge.
"""
from __future__ import annotations

import colorsys
import json
import sys
from pathlib import Path

import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
sys.path.insert(0, str(HERE))

import palette as P  # noqa: E402
from quantise import oklab  # noqa: E402

RAW = ROOT / "art-source/ai-raw/ui"
OUT = {"ui": ROOT / "Assets/_Project/Art/UI/Gen", "vfx": ROOT / "Assets/_Project/Art/VFX/Gen"}
MANIFEST = HERE / "ui_gen_manifest.json"


def rgb(name: str) -> tuple[int, int, int]:
    return tuple(int(v) for v in P.rgb(name))


def reduce(a: np.ndarray, k: int) -> np.ndarray:
    """Exact k x reduction: a block is opaque when half its pixels are; its colour is the commonest opaque colour."""
    if k == 1:
        return a
    h, w = a.shape[0] // k, a.shape[1] // k
    out = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        for x in range(w):
            block = a[k * y:k * y + k, k * x:k * x + k].reshape(-1, 4)
            op = block[block[:, 3] >= 128]
            if len(op) * 2 >= k * k:
                cols, counts = np.unique(op[:, :3], axis=0, return_counts=True)
                out[y, x, :3] = cols[counts.argmax()]
                out[y, x, 3] = 255
    return out


def objects(a: np.ndarray) -> list[tuple[int, int, int, int]]:
    """Bounding boxes of the opaque objects, split by fully transparent columns, left to right."""
    cols = (a[..., 3] >= 128).any(axis=0)
    boxes, x = [], 0
    while x < len(cols):
        if not cols[x]:
            x += 1
            continue
        x0 = x
        while x < len(cols) and cols[x]:
            x += 1
        sub = a[:, x0:x, 3] >= 128
        ys = np.nonzero(sub.any(axis=1))[0]
        boxes.append((x0, int(ys.min()), x - x0, int(ys.max() - ys.min() + 1)))
    return boxes


def snap(a: np.ndarray, names: list[str]) -> np.ndarray:
    pal = np.array([rgb(n) for n in names], np.uint8)
    mask = a[..., 3] >= 128
    idx = ((oklab(a[..., :3])[:, :, None, :] - oklab(pal)[None, None, :, :]) ** 2).sum(-1).argmin(-1)
    out = np.zeros_like(a)
    out[mask, :3] = pal[idx][mask]
    out[mask, 3] = 255
    return out


def remap(a: np.ndarray, spec: dict) -> np.ndarray:
    mask = a[..., 3] >= 128
    h, w = mask.shape
    rgbf = a[..., :3].astype(float) / 255.0
    lum = 0.2126 * rgbf[..., 0] + 0.7152 * rgbf[..., 1] + 0.0722 * rgbf[..., 2]
    warm = np.zeros_like(mask)
    sat_min = float(spec.get("warmSat", 0.18))
    for y in range(h):
        for x in range(w):
            if not mask[y, x]:
                continue
            hh, ss, vv = colorsys.rgb_to_hsv(*rgbf[y, x])
            deg = hh * 360.0
            warm[y, x] = ss >= sat_min and vv > 0.08 and (deg <= 65.0 or deg >= 330.0)
    out = np.zeros_like(a)
    for cls, sel in (("warm", mask & warm), ("cool", mask & ~warm)):
        ramp = spec.get(cls) or spec.get("cool") or spec.get("warm")
        ys, xs = np.nonzero(sel)
        if len(ys) == 0:
            continue
        order = np.argsort(lum[ys, xs], kind="stable")
        total = len(order)
        shares = np.array([s for _, s in ramp], float)
        bounds = np.round(np.cumsum(shares / shares.sum()) * total).astype(int)
        start = 0
        for (name, _), end in zip(ramp, bounds):
            pick = order[start:end]
            out[ys[pick], xs[pick], :3] = rgb(name)
            out[ys[pick], xs[pick], 3] = 255
            start = end
    return out


def fill_inside(a: np.ndarray, colour: str) -> np.ndarray:
    """Fills the transparent pixels enclosed by the opaque frame (not reachable from the canvas edge)."""
    h, w = a.shape[:2]
    solid = a[..., 3] >= 128
    outside = np.zeros_like(solid)
    stack = [(y, x) for y in range(h) for x in (0, w - 1)] + [(y, x) for x in range(w) for y in (0, h - 1)]
    while stack:
        y, x = stack.pop()
        if y < 0 or x < 0 or y >= h or x >= w or solid[y, x] or outside[y, x]:
            continue
        outside[y, x] = True
        stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    hole = ~solid & ~outside
    out = a.copy()
    out[hole, :3] = rgb(colour)
    out[hole, 3] = 255
    return out


def hollow(a: np.ndarray) -> np.ndarray:
    """Clears the field: the region of the centre pixel's colour connected to the centre (a frame over the world)."""
    h, w = a.shape[:2]
    cy, cx = h // 2, w // 2
    key = a[cy, cx].copy()
    seen = np.zeros((h, w), bool)
    stack = [(cy, cx)]
    out = a.copy()
    while stack:
        y, x = stack.pop()
        if y < 0 or x < 0 or y >= h or x >= w or seen[y, x] or not np.array_equal(a[y, x], key):
            continue
        seen[y, x] = True
        out[y, x] = 0
        stack += [(y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)]
    return out


def build(entry: dict) -> list[dict]:
    a = np.array(Image.open(RAW / entry["raw"]).convert("RGBA"))
    crop = entry.get("crop")
    if isinstance(crop, str):
        boxes = objects(a)
        x, y, w, h = boxes[{"first": 0, "second": 1, "third": 2}[crop]]
        a = a[y:y + h, x:x + w]
    elif crop:
        x, y, w, h = crop
        a = a[y:y + h, x:x + w]
    a = reduce(a, int(entry.get("reduce", 1)))
    if entry.get("trim"):
        ys, xs = np.nonzero(a[..., 3] >= 128)
        a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    if "size" in entry:
        w, h = entry["size"]
        if a.shape[1] != w or a.shape[0] != h:
            a = np.array(Image.fromarray(a).resize((w, h), Image.NEAREST))
    if "remap" in entry:
        out = remap(a, entry["remap"])
    else:
        out = snap(a, entry.get("palette") or list(P.PALETTE))
    for src, dst in (entry.get("remapAfter") or {}).items():
        m = (out[..., 3] == 255) & np.all(out[..., :3] == rgb(src), axis=-1)
        out[m, :3] = rgb(dst)
    if entry.get("fill"):
        out = fill_inside(out, entry["fill"])
    if entry.get("hollow"):
        out = hollow(out)
    if entry.get("opaque"):
        ramp = entry.get("remap", {}).get("cool") or [["INK-0", 1]]
        hole = out[..., 3] < 128
        out[hole, :3] = rgb(ramp[0][0])
        out[hole, 3] = 255
    folder = OUT[entry.get("out", "ui")]
    folder.mkdir(parents=True, exist_ok=True)
    results = [(entry["name"], out)]
    for suffix, swap in (entry.get("variants") or {}).items():
        v = out.copy()
        ring = swap.get("_outline")
        if ring:
            solid = v[..., 3] == 255
            pad = np.pad(solid, 1)
            edge = solid & ~(pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:])
            v[edge, :3] = rgb(ring)
        for src, dst in swap.items():
            if src.startswith("_"):
                continue
            m = (v[..., 3] == 255) & np.all(v[..., :3] == rgb(src), axis=-1)
            v[m, :3] = rgb(dst)
        results.append((entry["name"].replace("_default", "_" + suffix) if "_default" in entry["name"] else entry["name"] + "_" + suffix, v))
    report = []
    for name, img in results:
        Image.fromarray(img).save(folder / (name + ".png"))
        op = img[..., 3] == 255
        cols, counts = np.unique(img[op][:, :3], axis=0, return_counts=True)
        names = {rgb(n): n for n in P.PALETTE}
        share = ", ".join(f"{names.get(tuple(int(v) for v in c), '?')} {100 * k / op.sum():.0f}%"
                          for c, k in sorted(zip(cols, counts), key=lambda t: -t[1]))
        report.append({"name": name, "w": img.shape[1], "h": img.shape[0], "share": share, "border": entry.get("border"),
                       "out": entry.get("out", "ui")})
    return report


def procedural() -> list[dict]:
    """Plain pieces that need no generation: fills for the strip rails and the Candle highlight rings (art bible 4.9)."""
    folder = OUT["ui"]
    folder.mkdir(parents=True, exist_ok=True)
    made = []
    for name in ("UMBER-1", "UMBER-2", "INK-0", "INK-1", "INK-3"):
        a = np.zeros((4, 4, 4), np.uint8)
        a[..., :3] = rgb(name)
        a[..., 3] = 255
        n = "ui_fill-gen_" + name.lower() + "_4x4"
        Image.fromarray(a).save(folder / (n + ".png"))
        made.append({"name": n})
    for name, width, colour in (("ui_ring-gen_hover_9s", 1, "CANDLE"), ("ui_ring-gen_focus_9s", 2, "CANDLE"), ("ui_ring-gen_fog_9s", 1, "FOG-HI")):
        a = np.zeros((8, 8, 4), np.uint8)
        a[:width, :, :3] = rgb(colour); a[:width, :, 3] = 255
        a[-width:, :, :3] = rgb(colour); a[-width:, :, 3] = 255
        a[:, :width, :3] = rgb(colour); a[:, :width, 3] = 255
        a[:, -width:, :3] = rgb(colour); a[:, -width:, 3] = 255
        Image.fromarray(a).save(folder / (name + ".png"))
        made.append({"name": name, "border": [3, 3, 3, 3]})
    # VFX particles and the coin spin (feedback-motion.md section 6: 1 to 4 px particles; coin 4 frames from the K11 coin)
    vfx = OUT["vfx"]
    vfx.mkdir(parents=True, exist_ok=True)
    def square(n, size, colour):
        a = np.zeros((size + 2, size + 2, 4), np.uint8)
        a[1:1 + size, 1:1 + size, :3] = rgb(colour)
        a[1:1 + size, 1:1 + size, 3] = 255
        Image.fromarray(a).save(vfx / (n + ".png"))
        made.append({"name": n})
    for key, big, small in (("spark", "CANDLE", "BONE"), ("px-fog", "FOG-HI", "FOG"), ("px-bone", "BONE", "FOG-HI"),
                            ("px-moss", "MOSS-HI", "MOSS"), ("px-ink", "INK-4", "INK-3"), ("px-blood", "BLOOD", "BLOOD-D")):
        square("vfx_" + key + "_01", 2, big)
        square("vfx_" + key + "_02", 1, small)
    coin = folder / "ui_glyph-coin_gen_10x10.png"
    if coin.exists():
        c = np.array(Image.open(coin).convert("RGBA"))
        for i, w in enumerate((10, 6, 2, 6)):
            frame = np.zeros((12, 12, 4), np.uint8)
            squashed = np.array(Image.fromarray(c).resize((w, 10), Image.NEAREST))
            x0 = 1 + (10 - w) // 2
            frame[1:11, x0:x0 + w] = squashed
            Image.fromarray(frame).save(vfx / f"vfx_gold-coin_{i + 1:02d}.png")
            made.append({"name": f"vfx_gold-coin_{i + 1:02d}"})
    # a 3 px wooden batten (K13 divider): lit top, face, shadow
    a = np.zeros((3, 8, 4), np.uint8)
    for y, c in enumerate(("UMBER-2", "UMBER-1", "INK-0")):
        a[y, :, :3] = rgb(c)
    a[..., 3] = 255
    Image.fromarray(a).save(folder / "ui_divider-wood_default_9s.png")
    made.append({"name": "ui_divider-wood_default_9s", "border": [2, 0, 2, 0]})
    return made


def main() -> int:
    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    extra = procedural()
    only = set(sys.argv[1:])
    for entry in manifest["items"]:
        if only and entry["name"] not in only:
            continue
        for r in build(entry):
            print(f'{r["name"]:<40} {r["w"]:>4}x{r["h"]:<4} {r["share"]}' + (f'  border {r["border"]}' if r["border"] else ""))
    for key, folder in OUT.items():
        entries = []
        if key == "ui":
            for e in extra:
                if e.get("border"):
                    l, t, r, b = e["border"]
                    entries.append({"name": e["name"], "l": l, "t": t, "r": r, "b": b})
        for e in manifest["items"]:
            if e.get("out", "ui") != key or not e.get("border"):
                continue
            names = [e["name"]] + [e["name"].replace("_default", "_" + s) if "_default" in e["name"] else e["name"] + "_" + s
                                   for s in (e.get("variants") or {})]
            for n in names:
                l, t, r, b = e["border"]
                entries.append({"name": n, "l": l, "t": t, "r": r, "b": b})
        if entries:
            folder.mkdir(parents=True, exist_ok=True)
            (folder / "borders.json").write_text(json.dumps({"entries": entries}, indent=1), encoding="utf-8")
    return 0


if __name__ == "__main__":
    sys.exit(main())
