"""Grimpack pixel text face, Body cut (12 px line box): glyph data, Vietnamese
composition, TrueType writer and the Vietnamese render test sheet.

Metrics (art bible 7.7, 8.9): line box 12 rows, baseline under row 9, cap height
6 (rows 4-9), x-height 5 (rows 5-9), ascenders 7 (rows 3-9), descenders rows
10-11, line pitch 14. One design pixel is exactly one output pixel at 12 pt
(UPM 1200, 100 units per pixel), so the face is used only at its design size.

The 146 Vietnamese letters are composed from base letters and shared marks, so
nothing relies on combining marks at run time: every one is a precomposed
code point in the font.

Run:  python -I tools/art/pixel_font.py
Writes: Assets/_Project/Art/Fonts/grimpack-pixel/font_grimpack-pixel_regular_12.ttf
        production/qa/evidence/font_vi_test_<date>.png   (rendered through FreeType)
"""

from __future__ import annotations

import struct
import sys
import unicodedata
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
FONT_DIR = ROOT / "Assets/_Project/Art/Fonts/grimpack-pixel"
FONT_PATH = FONT_DIR / "font_grimpack-pixel_regular_12.ttf"
EVIDENCE = ROOT / "production/qa/evidence"
DATE = "2026-11-15"

UPM, PX = 1200, 100
ASCENT_ROWS, DESCENT_ROWS, GAP_ROWS = 10, 2, 2  # baseline under row 9; pitch 14 = 12 + 2

# ------------------------------------------------------------------ glyph data
# name -> (top_row, rows). '#' is ink. Width is the longest row.
G: dict[str, tuple[int, list[str]]] = {}


def g(ch: str, top: int, *rows: str) -> None:
    G[ch] = (top, list(rows))


# capitals: top 4, 6 rows
g("A", 4, ".###.", "#...#", "#...#", "#####", "#...#", "#...#")
g("B", 4, "####.", "#...#", "####.", "#...#", "#...#", "####.")
g("C", 4, ".####", "#....", "#....", "#....", "#....", ".####")
g("D", 4, "####.", "#...#", "#...#", "#...#", "#...#", "####.")
g("E", 4, "#####", "#....", "####.", "#....", "#....", "#####")
g("F", 4, "#####", "#....", "####.", "#....", "#....", "#....")
g("G", 4, ".####", "#....", "#.###", "#...#", "#...#", ".###.")
g("H", 4, "#...#", "#...#", "#####", "#...#", "#...#", "#...#")
g("I", 4, "#####", "..#..", "..#..", "..#..", "..#..", "#####")
g("J", 4, "..###", "...#.", "...#.", "...#.", "#..#.", ".##..")
g("K", 4, "#...#", "#..#.", "###..", "#..#.", "#...#", "#...#")
g("L", 4, "#....", "#....", "#....", "#....", "#....", "#####")
g("M", 4, "#...#", "##.##", "#.#.#", "#...#", "#...#", "#...#")
g("N", 4, "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#")
g("O", 4, ".###.", "#...#", "#...#", "#...#", "#...#", ".###.")
g("P", 4, "####.", "#...#", "#...#", "####.", "#....", "#....")
g("Q", 4, ".###.", "#...#", "#...#", "#.#.#", "#..#.", ".##.#")
g("R", 4, "####.", "#...#", "#...#", "####.", "#..#.", "#...#")
g("S", 4, ".####", "#....", ".###.", "....#", "....#", "####.")
g("T", 4, "#####", "..#..", "..#..", "..#..", "..#..", "..#..")
g("U", 4, "#...#", "#...#", "#...#", "#...#", "#...#", ".###.")
g("V", 4, "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#..")
g("W", 4, "#...#", "#...#", "#...#", "#.#.#", "##.##", "#...#")
g("X", 4, "#...#", ".#.#.", "..#..", "..#..", ".#.#.", "#...#")
g("Y", 4, "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#..")
g("Z", 4, "#####", "....#", "...#.", "..#..", ".#...", "#####")
# lowercase: x-height 5 (top 5), ascenders top 3, descenders reach row 11
g("a", 5, ".###.", "....#", ".####", "#...#", ".####")
g("b", 3, "#....", "#....", "####.", "#...#", "#...#", "#...#", "####.")
g("c", 5, ".####", "#....", "#....", "#....", ".####")
g("d", 3, "....#", "....#", ".####", "#...#", "#...#", "#...#", ".####")
g("e", 5, ".###.", "#...#", "#####", "#....", ".####")
g("f", 3, "..##.", ".#...", "####.", ".#...", ".#...", ".#...", ".#...")
g("g", 5, ".####", "#...#", "#...#", "#...#", ".####", "....#", ".###.")
g("h", 3, "#....", "#....", "####.", "#...#", "#...#", "#...#", "#...#")
g("i", 3, ".#.", "...", "##.", ".#.", ".#.", ".#.", "###")
g("j", 3, "..#", "...", "..#", "..#", "..#", "..#", "..#", "..#", "##.")
g("k", 3, "#....", "#....", "#..#.", "#.#..", "##...", "#.#..", "#..#.")
g("l", 3, "##.", ".#.", ".#.", ".#.", ".#.", ".#.", "###")
g("m", 5, "####.", "#.#.#", "#.#.#", "#.#.#", "#.#.#")
g("n", 5, "####.", "#...#", "#...#", "#...#", "#...#")
g("o", 5, ".###.", "#...#", "#...#", "#...#", ".###.")
g("p", 5, "####.", "#...#", "#...#", "#...#", "####.", "#....", "#....")
g("q", 5, ".####", "#...#", "#...#", "#...#", ".####", "....#", "....#")
g("r", 5, "#.###", "##...", "#....", "#....", "#....")
g("s", 5, ".####", "#....", ".###.", "....#", "####.")
g("t", 4, ".#...", "####.", ".#...", ".#...", ".#..#", "..##.")
g("u", 5, "#...#", "#...#", "#...#", "#...#", ".####")
g("v", 5, "#...#", "#...#", "#...#", ".#.#.", "..#..")
g("w", 5, "#...#", "#...#", "#.#.#", "#.#.#", ".#.#.")
g("x", 5, "#...#", ".#.#.", "..#..", ".#.#.", "#...#")
g("y", 5, "#...#", "#...#", "#...#", ".####", "....#", ".###.")
g("z", 5, "#####", "...#.", "..#..", ".#...", "#####")
# dotless i for composition (stem only)
g("ı", 5, "##.", ".#.", ".#.", ".#.", "###")
# digits: top 4, 6 rows
g("0", 4, ".###.", "#..##", "#.#.#", "##..#", "#...#", ".###.")
g("1", 4, "..#..", ".##..", "..#..", "..#..", "..#..", ".###.")
g("2", 4, ".###.", "#...#", "...#.", "..#..", ".#...", "#####")
g("3", 4, "####.", "....#", ".###.", "....#", "#...#", ".###.")
g("4", 4, "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.")
g("5", 4, "#####", "#....", "####.", "....#", "#...#", ".###.")
g("6", 4, ".###.", "#....", "####.", "#...#", "#...#", ".###.")
g("7", 4, "#####", "....#", "...#.", "..#..", ".#...", ".#...")
g("8", 4, ".###.", "#...#", ".###.", "#...#", "#...#", ".###.")
g("9", 4, ".###.", "#...#", "#...#", ".####", "....#", ".###.")
# punctuation
g(".", 9, "#")
g(",", 9, "#", "#")
g(":", 6, "#", ".", ".", "#")
g(";", 6, "#", ".", ".", "#", "#")
g("!", 4, "#", "#", "#", "#", ".", "#")
g("?", 4, ".###.", "#...#", "...#.", "..#..", ".....", "..#..")
g("'", 4, "#", "#")
g('"', 4, "#.#", "#.#")
g("-", 7, "###")
g("+", 5, "..#..", "..#..", "#####", "..#..", "..#..")
g("/", 4, "...#", "...#", "..#.", "..#.", ".#..", "#...")
g("(", 3, ".#", "#.", "#.", "#.", "#.", "#.", ".#")
g(")", 3, "#.", ".#", ".#", ".#", ".#", ".#", "#.")
g("%", 4, "##..#", "##.#.", "..#..", "..#..", ".#.##", "#..##")
g("&", 4, ".##..", "#..#.", ".##..", "#..#.", "#..#.", ".##.#")
g("*", 5, "#.#", ".#.", "#.#")
g("=", 6, "#####", ".....", "#####")
g("_", 10, "#####")
g("#", 5, ".#.#.", "#####", ".#.#.", "#####", ".#.#.")
g("<", 5, "..#", ".#.", "#..", ".#.", "..#")
g(">", 5, "#..", ".#.", "..#", ".#.", "#..")
g("[", 3, "##", "#.", "#.", "#.", "#.", "#.", "##")
g("]", 3, "##", ".#", ".#", ".#", ".#", ".#", "##")
g("Đ", 4, "####.", ".#..#", "###.#", ".#..#", ".#..#", "####.")  # Đ placeholder, replaced below
g("×", 6, "#...#", ".#.#.", "..#..", ".#.#.", "#...#")   # multiplication sign, for sizes such as 1×3
g("·", 7, "#")                                           # middle dot, a separator

# ------------------------------------------------------------------ marks
# Single marks, centred on cx, drawn top to bottom, 3 wide (dx -1..1).
SINGLE = {
    "acute": ["..#", ".#."],
    "grave": ["#..", ".#."],
    "hook": [".##", "..#", ".#."],
    "tilde": ["##.", ".##"],
    "circ": [".#.", "#.#"],
    "breve": ["#.#", ".#."],
}
# Tone above a circumflex or breve (stacked): 2 rows, drawn at rows 0-1 and shifted one column right.
STACKED_TONE = {
    "acute": ["..#", ".#."],
    "grave": ["#..", ".#."],
    "hook": [".##", "..#"],
    "tilde": ["##.", ".##"],
}
TONES = {"": None, "̀": "grave", "̉": "hook", "̃": "tilde", "́": "acute", "̣": "dot"}

LOWER_TOP, UPPER_TOP = 5, 4


class Bitmap:
    def __init__(self) -> None:
        self.px: set[tuple[int, int]] = set()  # (col, row), rows 0-11, cols may be -1..7

    def put(self, x: int, y: int) -> None:
        self.px.add((x, y))

    def shape(self, rows: list[str], x0: int, y0: int) -> None:
        for j, r in enumerate(rows):
            for i, c in enumerate(r):
                if c == "#":
                    self.put(x0 + i, y0 + j)


def base_bitmap(ch: str) -> tuple[Bitmap, int, int]:
    top, rows = G[ch]
    bm = Bitmap()
    bm.shape(rows, 0, top)
    width = max(len(r) for r in rows)
    return bm, width, top


def compose(base: str, shape_mark: str | None, tone: str | None, horn: bool) -> tuple[Bitmap, int]:
    """Return (bitmap, advance). base is a key of G ('a', 'o', 'A', ...)."""
    bm, width, top = base_bitmap(base)
    cx = width // 2
    upper = base.isupper()
    if horn:
        bm.put(width, top)
        width += 1
    zone_bottom = top - 2  # one empty row between the letter and a single mark
    if shape_mark:
        rows = SINGLE[shape_mark]
        circ_bottom = top - 1  # a circumflex or breve under a tone sits directly on the letter
        if tone and tone != "dot":
            bm.shape(rows, cx - 1, circ_bottom - len(rows) + 1)
            bm.shape(STACKED_TONE[tone], cx, 0)
        else:
            bm.shape(rows, cx - 1, zone_bottom - len(rows) + 1)
    elif tone and tone != "dot":
        rows = SINGLE[tone]
        bm.shape(rows, cx - 1, zone_bottom - len(rows) + 1)
    if tone == "dot":
        bm.put(cx if base != "y" else cx + 2, 11)
    return bm, width + 1


VOWELS_LOWER = "aeiouy"
VOWELS_UPPER = "AEIOUY"
# modified vowels: (letter, base, mark, horn)
MODIFIED = [("ă", "a", "breve", False), ("â", "a", "circ", False), ("ê", "e", "circ", False),
            ("ô", "o", "circ", False), ("ơ", "o", None, True), ("ư", "u", None, True)]


def build_glyphs() -> dict[int, tuple[Bitmap, int]]:
    """code point -> (bitmap, advance)."""
    out: dict[int, tuple[Bitmap, int]] = {}
    for ch in G:
        if ch == "Đ":
            continue
        bm, w, _ = base_bitmap(ch)
        out[ord(ch)] = (bm, w + 1)
    out[0x20] = (Bitmap(), 4)
    # đ and Đ
    d, w, _ = base_bitmap("d")
    d.put(1, 4); d.put(2, 4); d.put(3, 4)
    out[0x111] = (d, w + 1)
    big_d = Bitmap(); big_d.shape(["####.", "#...#", "#...#", "#...#", "#...#", "####."], 1, 4)
    for x in (0, 1):  # bar through the stem
        big_d.put(x, 6)
    out[0x110] = (big_d, 7)
    # Vietnamese vowels: every base x {no mark, 5 tones}
    bases: list[tuple[str, str, str | None, bool]] = []
    for lo, up in zip(VOWELS_LOWER, VOWELS_UPPER):
        bases.append((lo, lo, None, False)); bases.append((up, up, None, False))
    for letter, b, mark, horn in MODIFIED:
        bases.append((letter, b, mark, horn)); bases.append((letter.upper(), b.upper(), mark, horn))
    for letter, b, mark, horn in bases:
        gkey = "ı" if b == "i" else b
        for tone_cp, tone in TONES.items():
            composed = unicodedata.normalize("NFC", letter + tone_cp)
            if len(composed) != 1:
                continue
            if b == "i" and tone is None:
                bm_adv = compose("i", None, None, False)  # plain i keeps its dot
                out.setdefault(ord("i"), (bm_adv[0], bm_adv[1]))
                continue
            if b == "i":
                base_key = "ı"
                top, rows = G[base_key]
                bm = Bitmap(); bm.shape(rows, 0, top)
                cx, zone_bottom = 1, top - 2
                if tone == "dot":
                    bm.put(1, 11)
                else:
                    sh = SINGLE[tone]
                    bm.shape(sh, cx - 1, zone_bottom - len(sh) + 1)
                out[ord(composed)] = (bm, 4)
                continue
            bm, adv = compose(b, mark, tone, horn)
            out[ord(composed)] = (bm, adv)
    return out


# ------------------------------------------------------------------ TrueType writer
def _runs(px: set[tuple[int, int]]) -> list[tuple[int, int, int]]:
    """Merge pixels into horizontal runs: (row, x0, x1_exclusive)."""
    runs = []
    for row in sorted({y for _, y in px}):
        xs = sorted(x for x, y in px if y == row)
        s = p = xs[0]
        for x in xs[1:]:
            if x == p + 1:
                p = x
            else:
                runs.append((row, s, p + 1)); s = p = x
        runs.append((row, s, p + 1))
    return runs


def _glyf(bm: Bitmap) -> bytes:
    runs = _runs(bm.px) if bm.px else []
    if not runs:
        return b""
    pts: list[tuple[int, int]] = []
    ends: list[int] = []
    for row, x0, x1 in runs:
        y1 = (ASCENT_ROWS - row) * PX  # top of the row
        y0 = y1 - PX
        # clockwise in y-up space
        pts += [(x0 * PX, y0), (x0 * PX, y1), (x1 * PX, y1), (x1 * PX, y0)]
        ends.append(len(pts) - 1)
    xs, ys = [p[0] for p in pts], [p[1] for p in pts]
    d = struct.pack(">hhhhh", len(ends), min(xs), min(ys), max(xs), max(ys))
    d += struct.pack(f">{len(ends)}H", *ends)
    d += struct.pack(">H", 0)  # no instructions
    d += bytes([1] * len(pts))  # on-curve, 16-bit deltas
    px_, py_ = 0, 0
    dx, dy = [], []
    for x, y in pts:
        dx.append(x - px_); dy.append(y - py_); px_, py_ = x, y
    d += struct.pack(f">{len(pts)}h", *dx) + struct.pack(f">{len(pts)}h", *dy)
    return d + b"\0" * (-len(d) % 4)


def _table(tag: str, data: bytes) -> tuple[str, bytes]:
    return tag, data


def _cmap(codes: list[int]) -> bytes:
    """Format 4 over the BMP; glyph ids follow sorted code order (gid = 1 + index)."""
    segs: list[tuple[int, int, int]] = []  # (start, end, gid_of_start)
    for i, cp in enumerate(codes):
        gid = i + 1
        if segs and cp == segs[-1][1] + 1 and gid == segs[-1][2] + (cp - segs[-1][0]):
            segs[-1] = (segs[-1][0], cp, segs[-1][2])
        else:
            segs.append((cp, cp, gid))
    segs.append((0xFFFF, 0xFFFF, 0))
    n = len(segs)
    sr = 2 * (1 << (n.bit_length() - 1))
    body = struct.pack(">HHHH", n * 2, sr, (n.bit_length() - 1), n * 2 - sr)
    body += struct.pack(f">{n}H", *[s[1] for s in segs]) + struct.pack(">H", 0)
    body += struct.pack(f">{n}H", *[s[0] for s in segs])
    body += struct.pack(f">{n}H", *[((s[2] - s[0]) & 0xFFFF) if s[0] != 0xFFFF else 1 for s in segs])
    body += struct.pack(f">{n}H", *([0] * n))
    sub = struct.pack(">HHH", 4, 6 + len(body), 0) + body
    return struct.pack(">HH", 0, 1) + struct.pack(">HHI", 3, 1, 12) + sub


def build_ttf(glyphs: dict[int, tuple[Bitmap, int]], family: str = "Grimpack Pixel") -> bytes:
    codes = sorted(glyphs)
    datas = [b""] + [_glyf(glyphs[c][0]) for c in codes]  # gid 0 = .notdef (empty)
    adv = [500] + [glyphs[c][1] * PX for c in codes]
    lsb = [0] + [(min(x for x, _ in glyphs[c][0].px) * PX if glyphs[c][0].px else 0) for c in codes]
    glyf = b"".join(datas)
    loca, off = [], 0
    for d in datas:
        loca.append(off); off += len(d)
    loca.append(off)
    n = len(datas)
    allpx = [p for c in codes for p in glyphs[c][0].px]
    xmin = min(x for x, _ in allpx) * PX
    xmax = max(x for x, _ in allpx) * PX + PX
    ymin, ymax = -DESCENT_ROWS * PX, ASCENT_ROWS * PX
    head = struct.pack(">IIIIHHqqhhhhHHhhh", 0x00010000, 0x00010000, 0, 0x5F0F3CF5, 0x000B, UPM,
                       0, 0, xmin, ymin, xmax, ymax, 0, 8, 2, 1, 0)
    hhea = struct.pack(">IhhhHhhhhhhhhhhhH", 0x00010000, ASCENT_ROWS * PX, -DESCENT_ROWS * PX, GAP_ROWS * PX,
                       max(adv), 0, 0, xmax, 1, 0, 0, 0, 0, 0, 0, 0, n)
    maxp = struct.pack(">IH13H", 0x00010000, n, 400, 100, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0)
    hmtx = b"".join(struct.pack(">Hh", a, l) for a, l in zip(adv, lsb))
    names = {1: family, 2: "Regular", 3: family + " 12", 4: family + " Regular", 6: family.replace(" ", "") + "-Regular"}
    recs, strs = [], b""
    for nid, s in sorted(names.items()):
        e = s.encode("utf-16-be")
        recs.append(struct.pack(">HHHHHH", 3, 1, 0x409, nid, len(e), len(strs))); strs += e
    name = struct.pack(">HHH", 0, len(recs), 6 + 12 * len(recs)) + b"".join(recs) + strs
    os2 = struct.pack(
        ">HhHHH10hh10s4I4sHHHhhhHHIIhhHHH",
        4, 600, 400, 5, 0,
        0, 0, 0, 0, 0, 0, 0, 0, 0, 0,  # sub/superscript/strikeout metrics
        0, bytes(10),
        1, 0, 0, 0, b"NONE",
        0x40, codes[0], min(codes[-1], 0xFFFF),
        ASCENT_ROWS * PX, -DESCENT_ROWS * PX, GAP_ROWS * PX, ASCENT_ROWS * PX, DESCENT_ROWS * PX,
        1, 0, 500, 600, 0, 32, 0,
    )
    assert len(os2) == 96, len(os2)
    post = struct.pack(">IIhhIIIII", 0x00030000, 0, -100, 50, 1, 0, 0, 0, 0)
    tables = {
        "cmap": _cmap(codes), "glyf": glyf, "head": head, "hhea": hhea, "hmtx": hmtx,
        "loca": struct.pack(f">{n + 1}I", *loca), "maxp": maxp, "name": name, "post": post, "OS/2": os2,
    }
    head_off = None
    nt = len(tables)
    sr = 16 * (1 << (nt.bit_length() - 1))
    out = struct.pack(">IHHHH", 0x00010000, nt, sr, nt.bit_length() - 1, nt * 16 - sr)
    pos = 12 + 16 * nt
    body = b""
    for tag in sorted(tables):
        d = tables[tag]
        cs = sum(struct.unpack(f">{(len(d) + 3) // 4}I", d + b"\0" * (-len(d) % 4))) & 0xFFFFFFFF
        out += tag.encode() + struct.pack(">III", cs, pos + len(body), len(d))
        if tag == "head":
            head_off = pos + len(body)
        body += d + b"\0" * (-len(d) % 4)
    font = bytearray(out + body)
    total = sum(struct.unpack(f">{len(font) // 4}I", bytes(font))) & 0xFFFFFFFF
    struct.pack_into(">I", font, head_off + 8, (0xB1B0AFBA - total) & 0xFFFFFFFF)
    return bytes(font)


# ------------------------------------------------------------------ Vietnamese test
VI_ALL = "".join(chr(c) for c in list(range(0xC0, 0x180)) + list(range(0x1EA0, 0x1EFA)))
STACKED = "Ấ Ầ Ẩ Ẫ Ậ Ắ Ằ Ẳ Ẵ Ặ Ế Ề Ể Ễ Ệ Ố Ồ Ổ Ỗ Ộ Ớ Ờ Ở Ỡ Ợ Ứ Ừ Ử Ữ Ự"
TONE_PAIRS = "ả ã  ẩ ẫ  ể ễ  à á  ề ế  ạ ị . ơ ư đ"
SENTENCE = "Đặng Thị Ánh Nguyệt ưa uống nước ép ổi ở Huế, ăn bánh mì và đọc chữ Việt."
LINES3 = ["Ượ ở ộ ị ạ Ấ Ế Ộ Ự", "Ượ ở ộ ị ạ Ấ Ế Ộ Ự", "Ượ ở ộ ị ạ Ấ Ế Ộ Ự"]
MENU = ["Chơi", "Cài đặt", "Thoát", "Play", "Settings", "Quit"]


def required_vietnamese() -> list[str]:
    """The 146 letters from art bible 7.7: 12 vowels x 6 tone states x 2 cases + Đ đ."""
    out = []
    for letter in "aăâeêioôơuưy":
        for tone in TONES:
            for f in (str.lower, str.upper):
                out.append(unicodedata.normalize("NFC", f(letter) + tone))
    return out + ["Đ", "đ"]


def main() -> int:
    glyphs = build_glyphs()
    FONT_DIR.mkdir(parents=True, exist_ok=True)
    FONT_PATH.write_bytes(build_ttf(glyphs))
    missing = [ch for ch in required_vietnamese() if ord(ch) not in glyphs]
    print(f"{len(glyphs)} glyphs; required Vietnamese letters: {len(required_vietnamese())}, missing: {len(missing)} {''.join(missing)}")
    try:
        from PIL import Image, ImageDraw, ImageFont
    except ImportError:
        return 0
    font = ImageFont.truetype(str(FONT_PATH), 12)
    sheet = Image.new("RGB", (640, 360), (0x15, 0x1B, 0x2E))
    d = ImageDraw.Draw(sheet); d.fontmode = "1"
    bone, fog = (0xE4, 0xDA, 0xC0), (0x87, 0x95, 0xB3)
    y = 6
    d.text((6, y), "Grimpack Pixel 12 - Vietnamese test (1x)", font=font, fill=fog); y += 18
    for line in (STACKED[:60], STACKED[60:], TONE_PAIRS, SENTENCE, "Chơi  Cài đặt  Thoát   Play  Settings  Quit   0123456789 .,:;!?-+/()%"):
        d.text((6, y), line, font=font, fill=bone); y += 16
    for l in LINES3:
        d.text((6, y), l, font=font, fill=bone); y += 14
    sheet.save(EVIDENCE / f"font_vi_test_{DATE}.png")
    big = sheet.crop((0, 0, 320, 180)).resize((960, 540), Image.NEAREST)
    big.save(EVIDENCE / f"font_vi_test_3x_{DATE}.png")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
