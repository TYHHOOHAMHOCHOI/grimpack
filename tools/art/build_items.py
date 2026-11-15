"""Turn the generated item images into palette-exact item art that fills each item's footprint.

Every item has a real shape and a real size (user decision, 2026-11-15, replacing the single compact glyph on a plate
of art bible 8.5.2 option A): the art is drawn to the live area of its footprint, which is (cells x 20 - 4) px per
side (a 1 px tier edge and a 1 px lit frame on each side). A sword is 1 x 3 cells and 16 x 56 px; a hammer is a T.
Multi-cell items also get a 16 x 16 compact glyph for the avatar (art bible 5.10.2, variant "c").

Pipeline (art bible 8.11): raw image from art-source/ai-raw, reduce by exactly 2 where it was generated at 2x
(block vote, no smoothing), key to binary alpha, drop the generator outline, quantise to the master palette in OKLab,
limit the colour count, redo a 1 px INK-0 outline, clip to the plate bed.

Run:  python -I tools/art/build_items.py
Writes Assets/_Project/Art/Items/Icons/item_{id}_d_{fp}.png (the art), item_{id}_c_{fp}.png (avatar glyph, multi-cell only)
and production/qa/evidence/items_sheet_2026-11-15.png.
"""

from __future__ import annotations

import sys
from pathlib import Path

import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[2]
sys.path.insert(0, str(Path(__file__).resolve().parent))
import palette as P  # noqa: E402
import item_catalog as IC  # noqa: E402
import plates as PL  # noqa: E402
from quantise import oklab  # noqa: E402

RAW = ROOT / "art-source/ai-raw"
OUT = ROOT / "Assets/_Project/Art/Items/Icons"
EVIDENCE = ROOT / "production/qa/evidence"
C = 20
# free palette for item art (user decision 8.5.3), minus the Red-room colours and the ceiling
EXCLUDE = {"CEILING", "RED-GLOW"}
PALETTE = [n for n in P.PALETTE if n not in EXCLUDE and not n.startswith("WINE")]
MAX_COLOURS = 9


def interior(fp: str) -> tuple[int, int]:
    w, h = IC.SIZE[fp]
    return w * C - 4, h * C - 4


def reduce2(a: np.ndarray) -> np.ndarray:
    """Exact 2x reduction: a block is opaque when at least 2 of 4 pixels are; its colour is the commonest opaque colour."""
    h, w = a.shape[0] // 2, a.shape[1] // 2
    out = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        for x in range(w):
            block = a[2 * y:2 * y + 2, 2 * x:2 * x + 2].reshape(-1, 4)
            op = block[block[:, 3] >= 128]
            if len(op) >= 2:
                cols, counts = np.unique(op[:, :3], axis=0, return_counts=True)
                out[y, x, :3] = cols[counts.argmax()]
                out[y, x, 3] = 255
    return out


def erode4(m: np.ndarray) -> np.ndarray:
    pad = np.pad(m, 1)
    return m & pad[:-2, 1:-1] & pad[2:, 1:-1] & pad[1:-1, :-2] & pad[1:-1, 2:]


def dilate4(m: np.ndarray) -> np.ndarray:
    pad = np.pad(m, 1)
    return m | pad[:-2, 1:-1] | pad[2:, 1:-1] | pad[1:-1, :-2] | pad[1:-1, 2:]


def quantise(rgb: np.ndarray, mask: np.ndarray) -> np.ndarray:
    pal = np.array([P.rgb(n) for n in PALETTE], np.uint8)
    lab = oklab(rgb)
    palab = oklab(pal)
    idx = ((lab[:, :, None, :] - palab[None, None, :, :]) ** 2).sum(-1).argmin(-1)
    # limit the colour count: fold the rarest colours into their nearest kept neighbour
    while True:
        used, counts = np.unique(idx[mask], return_counts=True)
        if len(used) <= MAX_COLOURS:
            break
        rare = used[counts.argmin()]
        others = [u for u in used if u != rare]
        d = ((palab[others] - palab[rare]) ** 2).sum(-1)
        idx[idx == rare] = others[int(d.argmin())]
    out = np.zeros_like(rgb)
    out[mask] = pal[idx][mask]
    return out


def make_art(raw: Path, w: int, h: int) -> np.ndarray:
    """RGBA art of exactly w x h: the live area of a plate, outline included."""
    a = np.array(Image.open(raw).convert("RGBA"))
    if a.shape[1] == 2 * w and a.shape[0] == 2 * h:
        a = reduce2(a)
    if a.shape[:2] != (h, w):
        raise ValueError(f"{raw.name}: raw is {a.shape[1]}x{a.shape[0]}, expected {w}x{h} or double")
    solid = a[..., 3] >= 128
    body = erode4(solid)                       # the generator's own outline is replaced below
    if not body.any():
        body = solid
    rgb = quantise(a[..., :3], body)
    sil = body
    outline = dilate4(sil) & ~sil
    out = np.zeros((h, w, 4), np.uint8)
    out[sil, :3] = rgb[sil]
    out[sil, 3] = 255
    out[outline, :3] = P.rgb("INK-0")
    out[outline, 3] = 255
    return out


def clip_to_bed(art: np.ndarray, fp: str) -> np.ndarray:
    """For L, T and S shapes the art may spill outside the plate; keep only what lies on the bed (inside the frame)."""
    cells = set(IC.FP[fp])
    corners = PL.convex_corners(cells)
    mask = PL.build_mask(cells, corners, list(range(len(corners))))
    bed = PL.erode4(PL.erode4(PL.erode4(mask)))   # ring edge, ring frame, then the bed
    h, w = art.shape[:2]
    full = np.zeros(mask.shape, bool)
    full[2:2 + h, 2:2 + w] = art[..., 3] > 0
    keep = full & bed
    out = np.zeros_like(art)
    sub = keep[2:2 + h, 2:2 + w]
    out[sub] = art[sub]
    return out


def main() -> int:
    OUT.mkdir(parents=True, exist_ok=True)
    made = 0
    missing = []
    sheet_tiles = []
    for it in IC.ITEMS:
        art_id, fp = it["art"], it["footprint"]
        w, h = interior(fp)
        raw = RAW / f"item_{art_id}_raw.png"
        if not raw.exists():
            missing.append(raw.name)
            continue
        art = clip_to_bed(make_art(raw, w, h), fp) if len(IC.FP[fp]) > 1 and fp not in ("1x2", "2x1", "1x3", "2x2", "2x3") else make_art(raw, w, h)
        Image.fromarray(art, "RGBA").save(OUT / f"item_{art_id}_d_{fp}.png")
        made += 1
        if len(IC.FP[fp]) > 1:
            craw = RAW / f"item_{art_id}_c_raw.png"
            if craw.exists():
                Image.fromarray(make_art(craw, 16, 16), "RGBA").save(OUT / f"item_{art_id}_c_{fp}.png")
                made += 1
            else:
                missing.append(craw.name)
        # preview: plate with the art on its bed
        plate = Image.fromarray(PL.draw(it["tier"].lower(), fp, "rest"), "RGBA")
        tile = plate.copy()
        tile.alpha_composite(Image.fromarray(art, "RGBA"), (2, 2))
        sheet_tiles.append((it, tile))
    print(f"wrote {made} files; missing raw: {missing}")
    if sheet_tiles:
        S = 3
        col_w, row_h, per_row = 64 * S + 6, 64 * S + 6, 6
        rows = (len(sheet_tiles) + per_row - 1) // per_row
        sheet = Image.new("RGB", (per_row * col_w, rows * row_h), P.rgb("INK-1"))
        for i, (it, tile) in enumerate(sheet_tiles):
            big = tile.resize((tile.width * S, tile.height * S), Image.NEAREST)
            sheet.paste(big, ((i % per_row) * col_w + 3, (i // per_row) * row_h + 3), big)
        sheet.save(EVIDENCE / "items_sheet_2026-11-15.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
