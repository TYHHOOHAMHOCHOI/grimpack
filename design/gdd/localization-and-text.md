# Localization and Text

> **Status**: Designed (CD-GDD-ALIGN CONCERNS applied 2026-11-13; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Arrangement Is the Thinking (the player must be able to read every effect, intent and price before the fight); Power Has a Price (cost text must be readable in both languages); The Payoff Is the Product (text can never change a number, because numbers are parameters, so the payoff the player reads is the payoff they get)

## Summary

Localization and Text is how every word and number reaches the player. The simulation, the catalog, the snapshot and the event log carry text keys and integer arguments only; this system owns the Vietnamese and English string tables, derives keys from definition IDs, formats parameters, integers and plurals with the invariant culture, allows a small closed markup, shows a visible and flagged marker when a string is missing (the chain is the requested language, then English, then Vietnamese, then the marker), and runs a blocking lint before a build ships. It is an engine-free table, formatter and linter with a thin Unity binding. Numbers always come from the owning system, never from a translator, and layouts are sized for the longer Vietnamese text.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `Game Data Definitions (2)` (IDs and slot sets)

## Overview

System 7 is how every word and number the player reads gets from the game's data to the screen. The simulation, the data catalog, the run snapshot and the event log carry **keys and integer arguments only**; this system holds the **string tables** (Vietnamese is the authoring source, English the translation), resolves a key and its arguments into a finished string for the current language, and defines the rules that make that safe: how keys are named and derived from definition IDs, how parameters, numbers and plurals are formatted, which small set of markup a string may use, what is shown when a string is missing, and which checks run before a build ships. It is an engine-free table, formatter and linter, with a thin Unity layer that loads the tables and binds labels. Numbers in text always come from the owning system's parameters and never from a translator, so a translation can never change a rule. Vietnamese and English both ship in the MVP; layouts are sized for the longer Vietnamese text, and no text work counts as done before the font passes the Vietnamese render gate (art bible 7.7). Without this system the prototype's habit of building Vietnamese strings inside game logic would spread; with it, a player reads exactly what the game will do, in their language (Pillars 4 and 5).

> *Specialists consulted for this section: systems-designer, localization-lead, writer, gameplay-programmer, engine-programmer, art-director, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player never notices this system, and that is its success. **Every number, rule and price the player reads is exactly what the game will do, in their own language, and nothing they read ever needs a second guess.**

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: everything the player decides on must be visible, and "visible" means readable. Pillar 5, Power Has a Price: a cost is felt only if its text is clear in both languages. Pillar 2, The Payoff Is the Product: text can never change a number, because numbers are parameters.

*Guardrails* (binding rules): the simulation carries keys and integer arguments only, with no player-facing string or emoji in logic; a number in text is a placeholder filled from the owning system, never typed by a translator; a missing string is visible and flagged, never blank and never an invented number; containers are laid out for Vietnamese first and English is tested in them so it never looks broken; one term names one mechanic in both languages, from a locked glossary, and a synonym is a bug; no all-caps, no bold or italics, no text below the 12 px line box.

*Risks the design guards against:* translation drift that quietly changes a rule (a glossary lock and a placeholder-parity check), a tone split between the languages (a tone note per category), unreadable stacked diacritics (the font gate first), and English treated as an afterthought.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **Keys only below presentation.** The simulation, System 2's catalog, the run snapshot and System 4's event log hold text keys and integer arguments; presentation resolves them (`EventTextFormatter` in the presentation layer turns a record into a key plus arguments). No player-facing string, number format or emoji lives in logic. Text and the language never enter the run snapshot or its checksum; a language switch (allowed at any time from Settings) changes presentation only.
2. **The key scheme.** A key is `<ns>.<id>.<slot>`: lowercase ASCII `[a-z0-9_]`, dot-separated, at most 64 characters (for example `item.ember_blade.name`, `item.ember_blade.effect`, `status.seal.desc`, `move.gnaw.name`, `door.crypt.name`, `ui.combat.turn_label`, `term.shield.name`, `term.shield.def`, `err.choose_buff_hero.text`; every key has exactly three dot-separated parts). The namespaces are `item`, `hero`, `enemy`, `move`, `status`, `door`, `curse`, `term`, `ui`, `intent`, `log`, `err`, and `a11y` for accessibility text; the namespace names the owning system so a lint can check who may reference it. **Keys are derived** from a definition's stable ID and a fixed slot set per record kind (System 2 stores no hand-written key; the blank-key validation becomes "this slot is required for this kind and the table lacks it"). Keys are immutable once shipped; a renamed definition ID is a table migration recorded in `renames.tsv` (old, new), and the lint fails if an old key is still referenced.
3. **The source files.** One file per language per namespace, `loc/<lang>/<ns>.tsv`, with the four columns of Rule 20: `key`, `params`, `text`, `note` (where it appears, who speaks, what each parameter means). Rows are sorted by ordinal key comparison (never by culture), the text is UTF-8 in Unicode normalization form C with no byte-order mark and LF line endings, a newline inside a string is the escape `\n` (allowed only in dialog-prose slots), a literal backslash is `\\`, and a tab is forbidden in text, so a diff is one line per string. `.gitattributes` forces LF for these files. The **canonical build** is a sorted array of `(key, parsed template)` with a header `{formatVersion, lang, count}` and an FNV-1a 64 checksum over the canonical bytes (the same hash as System 5's snapshot); recording it in the run header is **[VS]** (see Scope tiers), not an MVP requirement.
4. **Languages.** (The per-string status and source hash are **[VS]**, see Scope tiers.) Vietnamese (`vi`) is the authoring source and English (`en`) the translation; both ship in the MVP. Each string carries a status in the build report (draft, reviewed, locked) and a source hash, so a change to the Vietnamese flags the stale English row. English is reviewed by a fluent reader (the reviewer is an Open Question).
5. **Parameters.** Placeholders are **named in the source** (`{damage}`, `{cells}`) and compiled to indices at load, so a translator sees meaning and the runtime never allocates a dictionary. Each has a type: `int` (an integer), `sint` (always signed), `count` (a plural selector) or `id` (a key reference). Arguments are 64-bit integers or key references; floats are banned. `sint` prints `+n` when positive, `0` for zero and `-n` when negative. A number and its unit are joined by a non-breaking space (U+00A0, in the allowed glyph set, rendered as a space width) so the wrap never separates them. The set of names and types must be equal in `vi` and `en` (order in the string may differ). Literal braces and brackets are escaped as `{{`, `}}`, `[[` and `]]`; a lone brace or bracket is a lint error.
6. **Numbers.** Only integers are shown. Vietnamese groups with `.` (1.250) and English with `,` (1,250), grouping from four digits; the minus sign is always the ASCII `-`; there are no decimals and no K or M abbreviations in the MVP (a K or M is added only if the lint finds a slot that overflows, and then as a decision of this system). Grouping is done by an integer loop with the invariant culture and not by any culture-sensitive API. The product of hits is written with the glyph `×` (U+00D7, in the lint's allowed set; the art bible's "x" is amended to it) and no translated word (for example `{r}×{h}`); a total is a separate string whose number the owner supplies, never computed in presentation.
7. **Plurals.** Vietnamese has one form. English has `one` (when the integer is exactly 1) and `other` (every other value, including 0). The syntax is inline: `{n} {n:count|one=cell|other=cells}`; the selector is integer only and a negative count is rejected by the lint. Grammatical gender is not supported (the format version reserves the slot).
8. **Markup (a closed set).** `[term:id]` links a glossary term (it renders as the term's name with the keyword treatment of the art bible, a 1 px underline and a Bone tone against the Fog body, never a tier colour and never colour alone, and carries the link ID for System 26's inline explanation); `[g:id]` is a glyph token that resolves to a 12 px glyph with a reserved width and an accessible text name. There is no bold, italic, size or colour tag, no all-caps transform, and no hard line break (the renderer wraps). A tag that is unknown or unbalanced is a lint error. The tokenizer is a single left-to-right pass with no culture API.
9. **Resolution and the fallback chain.** `ITextSource.Resolve(key, args)` returns a `TextResult {Text, Segments, Status, LangUsed}` with `Status` one of `Ok`, `FellBack`, `Missing`, `BadArgs`, `BadMarkup`. The chain is the requested language, then `en`, then `vi`, then the marker (the user's decision: English is the fallback base, and the Vietnamese source is the last text before the marker so that a number is never lost). A missing `vi` string therefore shows the English text with the status `FellBack`, flagged, and a missing `en` string shows the Vietnamese text, also flagged. A missing key in both languages, a wrong argument count or an unparsable template shows the **marker**: the literal key in the Fog body cut inside a plain bracket frame (for example `[ui.shop.buy]`), never empty, never a number, never carrying an argument value. A missing glyph is a fixed 5 × 7 hollow box in Fog, never a blank or a "?". Development builds log an error and the lint fails; shipping builds show the fallback or marker and log once per key; a missing string never blocks a mandatory fact in a popup.
10. **Slots and overflow.** Every text slot has a descriptor in data: `slot_id`, `role` (Display, Heading, Body, Secondary or a numeral cut), `digit_cap` (the worst-case digit count the validator uses for numbers), `max_lines`, the budget in native pixels of width (the character count is only a coarse first pass), the font cut per text scale set (Standard 12/16/24; Large 16/20/24), alignment (left or centre), colour role and `has_vi_marks`. Keys attach to slots by pattern (for example `item.*.name`). The overflow policy, in order, is: wrap, paginate (popup Details only), grow the container within the 30 percent spare height, otherwise **fail validation at build time**; the text is never shrunk, scaled or truncated with an ellipsis, because that would hide a mandatory fact. Large text has its own budgets and is offered only where the budget validator passes for every slot, or the slot goes glyph-only or paginates. The system returns the measured width and line count so the presentation can remeasure after a language, scale or content change.
11. **The lint (blocking at content time and in CI).** Fails on: a key referenced or derived but missing in either language; a duplicate key; a placeholder name or type mismatch between languages; a malformed plural block; an unknown or unbalanced markup tag; text that is not NFC or that contains a combining mark (U+0300 to U+036F); a glyph outside the font's 146 precomposed Vietnamese glyphs plus ASCII and the numeral set; a BOM, CRLF or tab; a key over 64 characters; a length budget exceeded (Vietnamese measured in pixels against the font's advance widths; English measured as itself, and a +30 percent growth test as a warning); a hard-coded diacritic or a string interpolation in runtime code (a source scan). An unused key is a warning.
12. **Invariant culture.** No culture-sensitive API is used: number formatting and parsing use the invariant culture, comparison and sorting are ordinal, keys are compared ordinally, and a list's display order is designer-defined and never alphabetical. A test sets the current culture to `vi-VN` and `de-DE` and asserts identical output.
13. **The glossary.** Every mechanic word is a `term.*` entry in both languages with a one-line definition; item and keyword text reference terms by `[term:id]`, never by retyping, so a rename propagates and a synonym is a bug. The starter set (Vietnamese / English) is: Ấn / Seal, Kích nổ Ấn / Detonate, Hồi chiêu / Cooldown, Tốc độ / Speed, Giáp / Shield, Lượt / Turn, Cửa / Door, Chào mời / Offer, Hướng / Direction, Trắng / White, Lục / Green, Lam / Blue, Tím / Purple, Vàng / Gold, Đỏ / Red, Lời nguyền / Curse, Neo / Anchor, Suy kiệt / Wither, Chậm chạp / Sluggish, Hero buff / Buff hero. The Vietnamese picks are proposals for a native reviewer; "Seal" is the user's choice for Ấn; the quality words on door cards (standard, better, strong, strongest) are a second vocabulary mapped in the glossary and are never item rarity names.
14. **Voice and budgets (writer).** Present tense; rules text has no "you"; order is trigger, number, effect ("Mỗi 3 lượt: +4 giáp"); one word per mechanic; no idioms, puns, hedges or flavour in rules text (flavour is Secondary/Fog only); a rejection reason is written as the fix and never blames the player ("Chọn hero buff trước" / "Choose a buff hero first"). Budgets (characters, a coarse guide at about 6 px per character and 22 per line): item name 17; effect text 3 lines (about 66); card name two lines of about 12 (Vietnamese under 16); keyword term 12 and definition 2 lines in Compact; hero and monster name 12; intent line a glyph, a number and a verb of at most 10; door promise popup line 44; curse line 44; rejection reason 30; dialog prose 4 lines of about 40; option button 20; end-of-run banner 14. The card-name limit (17 or two lines of about 12) is settled as the two-line form.
15. **What is localised and what is not.** Not localised: IDs, seeds, save and snapshot data, event-log records, debug logs, analytics, file names, config keys. Localised: all player-visible names, effect text, tier names (always in words, with the form cue), glyph labels and their text equivalents, error and validation messages shown to players, key prompts (taken from the live binding of System 6), settings labels.
16. **Pseudo-localisation (debug builds only; [VS], see Scope tiers).** A third locale wraps each string in `[ ]` with a Candle frame, expands it by 40 percent (`⌈len × 1.4⌉ + 2`) and substitutes the hardest stacked capitals (Ấ Ầ Ể Ệ Ố Ợ Ữ Ự); numbers and tokens pass through untouched.
17. **The font gate and the build order.** No font is adopted before the art bible's Vietnamese render test (146 glyphs plus digits, punctuation, the numeral set, `Đ` against `D` at 12 px, the real strings); long strings are not written before it passes; the test also covers the Large cuts.
18. **Architecture.** An engine-free assembly (the name is NOT SOURCEABLE until the architecture ADR) holds `TextKey`, an immutable `TextTable` built once by a builder from `(key, lang, template)` rows with templates pre-parsed into segments (literals, argument slots, plural blocks), a pure `Formatter` that writes into a caller-supplied buffer, the `TextLinter` and the `ITextSource` port. A plain `TextService` built with `(TextTable, language)` is injected through the composition root; there are no statics (the domain reload is off); a language change is an event whose subscribers unsubscribe in `OnDisable`. The Unity layer loads a `TextAsset` (UTF-8, NFC, LF) through a pure `Parse(string) -> TextTable`, and binds labels with a component that holds a key, resolves on enable and on language change, caches the last result and allocates a string only when an argument changes; nothing resolves per frame. The package Unity Localization is not used (it is not installed, it needs Addressables and asynchronous loading, and its locale selector is static state).
19. **Migration.** The prototype's roughly 266 lines of Vietnamese text and 139 lines of string interpolation (the catalog, `ThreeColumnLayoutManager`, `RunManager`, `CombatManager`, the status and intent classes) move to keys and tables; combat and run messages become structured records plus templates; palette emoji become glyph IDs; a regex test fails any diacritic or interpolation in non-test, non-editor runtime code.

20. **File format, grammar and results (fixed here).** A source row is `key<TAB>params<TAB>text<TAB>note`: `params` declares the arguments as `name:type` pairs separated by commas (`damage:int,n:count`; empty if none), so the types live in one place; a build report file `loc/status.tsv` holds `(key, status, sourceHash)` for the draft, reviewed and locked flags. At run time arguments are passed as an array in the order of the declared `params`, and `vi` and `en` must declare the same names and types (the order in the string may differ). The grammar is: `text := (literal | arg | plural | term | glyph)*`; `arg := "{" name "}"`; `plural := "{" name ":count|one=" text "|other=" text "}"` (a `one` form is optional for `vi`; `other` is required; no nesting; `|` and `=` inside a branch are escaped `||` and `==`); `term := "[term:" id "]"`; `glyph := "[g:" id "]"`; the escapes are `{{ }} [[ ]]` and `\`. The canonical bytes of a table are the header line `LOCTABLE v1 <lang> <count>` followed by one line per row `key<TAB>params<TAB>text` in ordinal key order, each ending in LF; the checksum covers all of them. `Parse` returns the table or a list of errors; **a table with any error is not built** (so the build fails), and at run time `Resolve` returns only `Ok`, `FellBack`, `Missing`, `BadArgs` (wrong count or type, a negative count, `long.MinValue`) or `BadMarkup` (an unresolvable `[term:]` or `[g:]` id, shown as the id in a marker frame inside the string). The glyph set the lint allows is the font's 146 precomposed Vietnamese glyphs, ASCII and the numeral set, plus U+00A0 (the non-breaking space, authored by the writer), U+00D7 (`×`) and the punctuation the font test approves. The source scan for hard-coded text allows development logs through an allowlist attribute in test, editor and diagnostic code. The pseudo-locale's `len` is counted in UTF-16 code units of the NFC `vi` row, padded with the letter `ế`, and is selectable only through an injected flag (not `DEVELOPMENT_BUILD`, which is deprecated in 6.6). The log-once set is an instance owned by `TextService`, and it resets when the language changes.

### States and Transitions

| Subject | State | Trigger | Result |
|---|---|---|---|
| Table | Unloaded | The composition root loads the tables | Parsed and checked for the format version; a bad table is a build failure (shipping: the language is unavailable). |
| Table | Loaded(lang) | `SetLanguage` | The current language changes; the event is raised; labels re-resolve and popups remeasure. |
| Key | Resolved | `Resolve` | `Ok`, `FellBack`, `Missing`, `BadArgs` or `BadMarkup`. |

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 2 Game Data Definitions | The definition IDs and the slot set per kind | Derived keys; the lint's missing-key report | System 2 owns the IDs; System 7 owns the key scheme and the translations. |
| 4 Event Log | Records as a kind plus integer arguments | Text for the log and the breakdown | System 4 never calls `Resolve`; presentation does. |
| 26 Hover Popup and Glossary | `term.*` entries, the markup, the missing-key policy | Resolved text, measured width and lines, the first-use Details hint wording | System 26 owns the popup; System 7 owns the text. |
| 25, 27, 28, 29, 30, 31 UI | Keys and arguments | Resolved strings and slot budgets | Those systems own layout. |
| 6 Input and Focus | Live binding names | Key prompt labels | System 6 supplies bindings; System 7 labels them. |
| 37 Settings | The language choice, the text scale set | None | System 37 stores. |
| 34 Accessibility | `a11y.*` keys and text alternatives | | Vertical Slice. |
| 5 Run State and Flow | The table checksum | | Recorded in the run header; text never in the snapshot. |

*Provisional:* the assembly name, the file format details (TSV against JSON) and the budgets are proposals for the architecture ADR and the first spike.

> *Specialists consulted: systems-designer, localization-lead, writer, gameplay-programmer, engine-programmer, art-director (2026-11-13).*

## Formulas

All arithmetic is on integers; no floats, no culture-sensitive API. All budgets are provisional until the font is chosen.

**Formula 1: digit grouping.** For an integer `n` and a grouping threshold `T` of digits (default 4, safe range 4 to 5), write `|n|` in decimal, split it into groups of three digits from the right, join with the language's separator (`.` for vi, `,` for en), and prefix `-` if `n < 0`; a number below 1000 in absolute value has no separator. The groups are produced by repeated `/ 1000` and `% 1000`, the leading group unpadded and the others padded to three digits. *Examples:* `999` → `999`; `1000` → vi `1.000`, en `1,000`; `1234567` → groups 1, 234, 567 → vi `1.234.567`, en `1,234,567`; `-4` → `-4`; `-12345` → vi `-12.345`. `long.MinValue` is rejected before negation.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `n` | long | `−99,999..99,999` expected; any `long` except `long.MinValue` | owning system | The integer argument. |
| separator | char | `.` or `,` | language table | Thousands separator. |

**Formula 2: plural selection.** `form(n) = one` if `lang = en` and `n = 1`; otherwise `other`; for `vi` the form is always `other`; `n < 0` is a lint error. *Examples:* `{n} {n:count|one=cell|other=cells}` with `n = 1` → `1 cell`, `n = 2` → `2 cells`, `n = 0` → `0 cells`; the Vietnamese template `{n} ô` gives `1 ô`, `2 ô`, `0 ô`.

**Formula 3: a coarse character budget and the fit test.** `maxChars ≈ ⌊textWidthPx / 6⌋` at about 6 px per character for the Body 12 cut; the real check is the pixel width `Σ advance(glyph)` (letter-spacing is 0; the art bible forbids spacing tricks) against the slot, wrapped greedily at spaces (never inside a `[term:…]` span; a number and its unit are bound by a non-breaking space). *Example:* a popup text width of 136 px gives `⌊136 / 6⌋ = 22` characters per line, so the effect-text budget of 3 lines is about 66 characters. A text fits a slot iff its wrapped height is at most `lines × pitch + ⌊heightPx × 30 / 100⌋` (the spare height, integer floor) and the width of every line is at most the slot width (`≤`, boundary inclusive).

**Formula 4: the pseudo-locale.** `len' = ⌈len × 1.4⌉ + 2` (in general `(len × (100 + p) + 99) / 100 + 2` for an expansion of `p` percent) (the +2 is the brackets), with numbers and tokens unchanged (they are excluded from `len`). *Example:* a string of 20 characters becomes `⌈28⌉ + 2 = 30`; a string of 13 becomes `⌈18.2⌉ → 19, + 2 = 21` (integer ceiling: `(len × 14 + 9) / 10`).

**Formula 5: key length.** `len(key) ≤ 64`, characters in `[a-z0-9_.]`, exactly three dot-separated parts. *Example:* `item.ember_blade.effect` (23) is valid; `Item.EmberBlade.Effect` is not.

**Formula 6: the table checksum.** FNV-1a 64 over the canonical bytes (UTF-8, LF, rows in ordinal key order, header first), written as 16 lowercase hexadecimal digits; the same function as System 5's snapshot (the empty input hashes to `cbf29ce484222325`).

**Worked example (a parameterised effect text).** `item.ember_blade.effect`: vi `Gây {damage} sát thương cho {n} {n:count|other=kẻ địch}`, en `Deal {damage} damage to {n} {n:count|one=enemy|other=enemies}`; the names `damage` (`int`) and `n` (`count`) match in both. With `damage = 8` and `n = 2` the English result is `Deal 8 damage to 2 enemies` and the Vietnamese `Gây 8 sát thương cho 2 kẻ địch`; with `n = 1` the English is `… to 1 enemy`. A curse line `{speed:sint}` with `speed = -4` gives `-4`. If the English row is missing, the Vietnamese-language player still sees the Vietnamese row; an English-language player with a missing English row sees the Vietnamese row, flagged `FellBack`; only if both are missing is the marker `[item.ember_blade.effect]` shown (the chain is the requested language, English, Vietnamese, then the marker).

> *`systems-designer`, `localization-lead` and `art-director` consulted (2026-11-13); the grouping and plural rules are theirs.*

## Edge Cases

Format: *if condition: exact outcome* (the detector: `TextLinter`, `Formatter`, `TextService`, the builder, or the named system).

**Keys and tables**

- *A derived key has no row in either language:* the lint fails CI; at run time the chain applies (requested, en, vi, marker).
- *A key appears twice, in one file or across namespace files:* the builder fails the build; last-wins is forbidden.
- *A key exists in one language only:* the lint reports an error; in shipping `Resolve` returns `FellBack` when the requested language lacks the row, and `Ok` otherwise.
- *A key is unused:* a warning, exempt while a `renames.tsv` pair is inside its window.
- *A definition ID is renamed:* the lint fails until `renames.tsv` lists the pair and nothing references the old key; shipped saves hold IDs, so the migration belongs to System 2's ID table.
- *A System 2 ID has characters outside `[a-z0-9_]`, or ns + id + slot exceeds 64:* key derivation fails validation in System 2; this system never slugs or normalises.
- *One language file fails to load:* development builds fail; shipping marks the language unavailable, Settings greys it out and keeps the current language (if the current language failed it switches to `en`, or `vi` if `en` also failed; if both fail every `Resolve` returns the marker and a fixed built-in boot-error string shows); logged once.
- *Two tables have different `formatVersion` values:* the newer or unknown version is rejected whole, never partially parsed.
- *The CI checkout has CRLF:* `.gitattributes` forces LF; the lint reports CRLF, a BOM, a tab, non-NFC text or a combining mark U+0300 to U+036F as hard errors and never normalises silently; the checksum is over the canonical bytes, so line endings cannot change it.
- *A shipping table has no rows:* a load error (an empty table is valid only for the checksum vector).

**Templates and arguments**

- *Placeholder names or types differ across languages:* lint error.
- *The formatter gets too many or too few arguments, or the wrong type:* `BadArgs` and the marker; development builds throw.
- *An argument is `long.MinValue`:* `BadArgs`; it is never negated (the negative remainder is handled directly).
- *A count is negative at run time:* `BadArgs`; the lint catches only constants.
- *A count is 0, 1 or large:* English `one` only at 1; every other value is `other`; Vietnamese is always `other`.
- *A plural block lacks `other`, has an unknown form or is nested:* lint error.
- *A lone brace or bracket:* lint error; `{{ }} [[ ]]` give literals.
- *`[term:x]` inside a plural branch:* allowed and expanded per branch; `{...}` inside a tag is a lint error; the term set must be equal across branches and languages.
- *A `[term:id]` or `[g:id]` has no definition:* lint error; at run time `BadMarkup`, with the id in a marker frame and the rest of the text kept.
- *An unbalanced or unknown tag:* lint error.
- *An integer is formatted in two places differently:* forbidden; the one `Formatter.Int` is the only path and the source scan flags any other.
- *`sint` is zero or positive:* `0` and `+n`.

**Fonts, layout and runtime**

- *A glyph is outside the font:* the lint fails and at run time the 5 × 7 hollow box is drawn; a marker key uses only `[a-z0-9_.]`, so it is always drawable.
- *A string is over budget in one language only:* the build fails for that language (budgets are per slot and per language); the Large set is offered only where every slot passes.
- *A marker frame overflows its slot:* it wraps like text; if it still does not fit it is a validation failure for a fallback case; shipping shows it anyway and never truncates.
- *A string mixes digits and Vietnamese:* the digits use the text face inside running text and a number is never split across a wrap (a non-breaking space binds it to its unit).
- *The language changes while a popup or dialog is open, or mid-playback:* `TextService` raises the event; System 26 remeasures and re-resolves; playback is untouched because records hold keys; a popup with no legal route closes; a dialog keeps its state and re-lays out.
- *The language changes with the run snapshot or a replay involved:* nothing text-related is in either; only the table checksum is in the run header, so a replay in another language is bit-identical.
- *The culture is vi-VN, de-DE, sv-SE or tr-TR:* the output is the same; no culture-sensitive call is allowed without an explicit invariant or ordinal argument.
- *The pseudo-locale is requested in shipping:* it is not selectable; the build strips it and the lint checks no pseudo table is in the package.
- *An English-language player's English row is missing:* the Vietnamese row is shown, flagged `FellBack`; only if both are missing is the marker shown.
- *A `FellBack` or `Missing` result:* it must never change layout rules or any gameplay decision.

> *`systems-designer` consulted (2026-11-13); its contradictions are applied (derived keys with three parts, the newline escape only in dialog prose, the fallback chain with `vi` before the marker, `sint`, the escapes, the pseudo-locale unit).*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 2 Game Data Definitions | Depends on (hard) | Definition IDs and the slot set per record kind | Keys are derived; System 2's "localization key" field becomes the slot list. |
| 4 Event Log | Related | Records are kind plus integer arguments | System 4 never resolves text. |
| 26 Hover Popup and Glossary | Depended on by | `term.*`, markup, missing-key policy, measured width and lines, the first-use hint wording | Closes System 26's Rule 10 and its open questions on missing text. |
| 25, 27, 28, 29, 30, 31 UI | Depended on by | Resolved strings and slot budgets | |
| 6 Input and Focus | Related | Live binding names for key prompts | |
| 37 Settings | Depended on by | Stores the language and the text scale set | |
| 34 Accessibility | Depended on by | `a11y.*` text alternatives; Large budgets | Vertical Slice. |
| 5 Run State and Flow | Related | None in the MVP; the optional table checksum in the run header is **[VS]** | Text never in the snapshot; System 5 Rule 16 does not currently log it. |

The systems index lists System 7 with no dependencies, layer Core and effort S; this GDD's header says Foundation, depends on System 2, and the effort is M (the glossary, the formatter, the missing-key policy, the number formats and the font gate). There is no cycle.

## Tuning Knobs

All values are provisional. They live in data, never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| Grouping threshold | 4 digits | 4 to 5 | When a separator appears | Too high: long unseparated numbers. |
| Pseudo-locale expansion **[VS]** | 40 percent | 30 to 60 | Stress on layouts | Low: misses overflow. |
| Characters per line (coarse guide) | about 22 at Body 12 | measure per font | Writer budgets | Wrong guide: false passes before the pixel check. |
| Spare height | 30 percent | 20 to 40 | Growth room for Vietnamese and Large | Low: overflow; high: wasted space. |
| Key length limit | 64 | 48 to 96 | Key hygiene | Long keys hurt diffs. |
| Fallback chain | requested, en, vi, marker | fixed | Missing strings | Changing it changes what a player sees on a gap. |
| Text scale sets | Standard 12/16/24, Large 16/20/24 | fixed by the art bible | Slot budgets | Large only where budgets pass. |

Interactions: the budgets depend on the chosen font's advance widths, so the lint is re-run when the font changes; the Large set doubles the budgets to maintain.

### Scope tiers (CD-GDD-ALIGN, 2026-11-13)

The rules above are the full design. For a solo first-time developer the MVP builds only the tier-1 core; items marked **[VS]** stay specified but are built at Vertical Slice or later, and their acceptance criteria are not MVP gates. This keeps System 7 at effort M.

| Tier | Items |
|---|---|
| **MVP (build now)** | Keys-only rule (1); key scheme and derivation (2); TSV source files with four columns (3, without the checksum); `vi` and `en` with the fallback chain and marker (9); named typed placeholders and the invariant-culture integer formatter (5, 6); English plurals (7); `[term:]` and `[g:]` markup (8); the glossary (13); voice and coarse character budgets (10 coarse pass, 14); culture-independence (12); a **minimal lint**: missing key, duplicate key, placeholder and plural parity, markup syntax, NFC and combining mark, glyph set, BOM/CRLF/tab, hard-coded text scan (11, 19). |
| **[VS] deferred** | (a) Pseudo-locale (16, Formula 4): Vietnamese is already the longer authoring language and acts as the stress test. (b) Per-string status and source hash, `loc/status.tsv` (4): use git diff until English exists in bulk. (c) `renames.tsv` (2): keys are immutable only once shipped. (d) The table checksum in the run header (3, Formula 6): System 5 logs only the catalog fingerprint; add it to System 5 Rule 16 if a replay ever needs to name its text table. (e) Pixel-accurate budget validator and the Large-set budgets (10, 17): keep the coarse character check until a font is chosen. (f) Engine-free IL scan, allocation probe and operation-counter criteria (AC 28 to 30). (g) The watched-failures mutant list (advisory). |
| **Later / Tier 2** | Plural forms beyond `one` and `other`, gender, a third language, a K/M abbreviation. |

## Visual/Audio Requirements

System 7 produces no art or sound; it fixes the visible forms of text states (art bible 7.7 and 8.9).

- **Missing string:** the literal key in Fog in the Body cut inside a plain bracket frame; in shipping builds a fallback row (English, or Vietnamese for an English-language player) is shown without a frame, `FellBack` is logged once per key; never blank, never a number.
- **Missing glyph:** a fixed 5 × 7 hollow box in Fog (it should be unreachable once the 146-glyph gate passes).
- **Pseudo-locale (debug):** each string in `[ ]` with a Candle frame (not Blood, which means damage or cost).
- **Keyword:** a 1 px underline plus a Bone tone against the Fog body; never a tier colour; glyph tokens reserve 12 px plus a 1 px gap and carry an accessible text name.
- **Font test beyond the 146 glyphs:** digits 0 to 9 at each cut (tabular, and the 8 px cut), `. , K M + - × % /`, curly quotes, the ellipsis, the en dash, 1 against l against I, 0 against O, `Đ` against `D` at 12 px, a mixed digit and Vietnamese string, and the Heading, Display and Large cuts.
- **Art bible gaps:** no missing-string or missing-glyph spec (add one under 7.7), no line pitch for the Large cuts, no 20 numeral cut, 7.13 still says `accessibility-requirements.md` does not exist (it now exists as a draft), and the hi-res overlay fallback breaks the pixel rules (the slot descriptor works in native pixels so it stays swappable).

*`art-director` consulted (2026-11-13). Asset specs belong to `/asset-spec`.*

## Game Feel

Text is invisible when it works.

- **Instant.** A label resolves on enable and on a language change, never per frame; a changed number costs one string.
- **Stable.** A language switch re-resolves every label and the open popup remeasures at once; nothing flickers.
- **Honest on failure.** A gap is visible and calm (a bracketed key in Fog), never a crash, a blank or an invented number.
- **No latency from this system.** Resolution is a lookup and a small formatting pass.

## UI Requirements

Layout belongs to Systems 25 to 31 and their UX specs; this system fixes the contract.

| Information | Where | Rule |
|---|---|---|
| The language choice and the text scale set | Settings (System 37) | Both change presentation only and apply at once. |
| Key prompts | Prompt chips | Text from the live binding (System 6), localised key names. |
| Glossary terms | Inline in popups | `[term:id]` with the keyword treatment and a link to the explanation. |
| Slot budgets | Every text container | A build-time check; overflow is a failure, never a runtime shrink. |
| Rejection reasons | Disabled controls and dialogs | Written as the fix, within 30 characters. |

Accessibility: no all-caps, no text below the 12 px line box, tier names in words, text alternatives for glyphs (`a11y.*`).

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| Keys, blank-key validation, parameters | `design/gdd/game-data-definitions.md` Rule 2 | Keys become derived. |
| Records to text | `design/gdd/event-log.md` | Presentation derives text. |
| Missing-key policy, remeasure, inline terms, the first-use hint | `design/gdd/hover-popup-and-glossary.md` Rules 6 and 10 | Answered here. |
| The snapshot and the checksum function | `design/gdd/run-state-and-flow.md` Rule 16 | Text never in the snapshot. |
| Key prompts and bindings | `design/gdd/input-and-focus.md` | |
| Typography, the Vietnamese gate, budgets, text scale | `design/art/art-bible.md` 7.7, 7.10, 7.13, 8.9 | |
| The pillars | `design/gdd/game-concept.md` | |

## Acceptance Criteria

**Numbering and scope.** Rule-level ACs use the number of the rule they test, except: AC 15 tests Rule 1 and Rule 18 (layering), AC 22 covers Rule 7's plural formatting through Formula 2, AC 18 covers Rule 18's language-change event, and Rule 15 (what is localised) is a review item with no automated test: a reviewer confirms that every player-visible string class is on the localised list and none of the excluded classes is. Under the Scope tiers table the following are **[VS]** and not MVP gates: AC 4 (status), AC 16 and 24 (pseudo-locale), AC 2c (renames), AC 26 and the checksum parts of AC 3b/20/27 (table checksum), AC 10a pixel budgets and Large budgets, AC 28 to 30, and the watched-failures list (advisory).

Evidence type: **Logic**, automated EditMode tests in `Assets/_Project/Tests/EditMode/Text/` (class `TextTests`, assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. The Vietnamese render gate is **Visual** evidence (retained screenshots in `production/qa/evidence/` with lead sign-off); the label binder's lifetime is Play Mode evidence. Each row is its own `[TestCase]` with a passing control. Tests use test-local fixtures only.

**Fixtures.** `TinyTables`: vi and en tables built in the test with unicode escapes, one clean pair and one table per single fault (duplicate key, type mismatch, bad plural, unbalanced tag, decomposed `ế`, a NFC-stable `x` + U+0300, BOM, CRLF, tab, a 65-character key, a glyph outside the set, a key missing in en, a key missing in both). `FakeTextSource` (records calls; for binder tests only). `StubMetrics`: literal advances (letter 5, digit 5, space 3, non-breaking space 3, `×` 5), a glyph set and line height 16. `RefFormatter` and `RefFnv`: written from the rule text, used for sweeps only (`n = −2000..2000`); `RefFnv` is shared with System 5. `CultureScope`: sets and restores the current culture, with a control that `1234.5.ToString()` is `1234,5` under de-DE so the probe is not vacuous. `AllocProbe` (with an allocating control loop) and `OpCounter` (a seam, not a timer).

**Rules**

| # | Given / When / Then |
|---|---|
| 1 | **Keys only.** Switching vi to en mid-run on a fake snapshot writer gives identical snapshot bytes and checksum; no simulation assembly references the text assembly. |
| 2a | **Key scheme.** `item.ember_blade.name` and a 64-character key are valid; `Item.x.name`, a 65-character key, `item.a-b.name`, `item..name`, `foo.x.name` and a two-part key are rejected. |
| 2b | `Derive(item, ember_blade, effect)` gives `item.ember_blade.effect`; an uppercase ID is an error. |
| 2c | A reference to a key listed in `renames.tsv` fails the lint; the new key passes. |
| 3a | **Ordering.** Rows `item.a_b.x` and `item.a.x` sort to `item.a.x` first (`.` 46 before `_` 95), identically under de-DE and vi-VN (control: a culture compare disagrees). |
| 3b | Shuffled input gives the same checksum; a BOM, CRLF, a tab and non-NFC text are each rejected. |
| 4 | **Status.** Changing the Vietnamese text marks the English row stale through the source hash in `loc/status.tsv`. |
| 5a | **Parameters.** vi `{n}..{damage}` and en `{damage}..{n}` called with the same arguments give the same numbers. |
| 5b | A name mismatch, a type mismatch, `{n:N0}` and `{a+b}` are each rejected. |
| 5c | `{{` gives `{`, `}}` gives `}`, `[[` gives `[`, `]]` gives `]`; a lone one is a lint error. |
| 5d | Reflection finds no float, double or decimal parameter on the public API. |
| 6 | **Numbers.** `{r}×{h}` with `r = 3`, `h = 2` gives `3×2`; `-4` under sv-SE gives U+002D and not U+2212; `sint` gives `+4`, `0` and `-4`. |
| 8a | **Markup.** `a [term:shield] b` gives Literal "a ", Term (link `shield`), Literal " b", and the characters read equal the length (a single pass). |
| 8b | `[g:seal]` gives a Glyph segment. |
| 8c | `[b]x[/b]`, `[term:x` and an unknown tag each give `BadMarkup` (lint error; run time shows the id in a marker frame). |
| 9a | **Fallback.** vi missing, en present, language vi: the English text, `FellBack`, `LangUsed = en`. |
| 9b | Missing in both: the text is `[ui.shop.buy]`, `Missing`, and does not contain the sentinel 987654. |
| 9c | A wrong argument count: `BadArgs`, the marker, and no argument value in it. |
| 9d | English requested, en missing, vi present: the Vietnamese text, `FellBack`, `LangUsed = vi`. |
| 9e | Three resolves of one missing key log once; two keys log twice; a language change resets the set. |
| 9f | Over a status sweep the text is never empty. |
| 10a | **Slots.** A word "abcd" is 20 px and a line is 136 px: 6 words fit (135 px), 7 do not (158 px); 18 words give 3 lines; 19 words in a slot of 3 lines and 48 px need 64 px, the spare is `⌊48 × 30 / 100⌋ = 14`, `62 < 64` so it fails, and at 50 px the spare is 15, `65 ≥ 64`, so it grows; a Details slot paginates; the text is unchanged (no ellipsis). |
| 10b | A non-breaking space between "8" and "damage", and a `[term:shield]` span, never break; a trailing space is not counted. |
| 11 | **Lint.** One fixture per lint code gives exactly that code; the clean table gives 0 findings; an unused key is a warning; with no metrics the budget check is `NotAssessed`, never Pass; the +30 percent English growth is a warning. |
| 12 | **Culture.** All outputs are equal under vi-VN, de-DE, sv-SE and the invariant culture, for both languages. |
| 13 | **Glossary.** The 20 starter terms each have `.name` and `.def` in vi and en; names are unique per language; synonyms and the Vietnamese picks are review items (not automatable). |
| 14 | **Budgets.** An `err.*` slot of 30: vi "Chọn hero buff trước" (20) and en "Choose a buff hero first" (24) pass; the voice rules are review items. |
| 15 | **Layering.** `Resolve` is referenced only from presentation assemblies. [BLOCKED: the architecture ADR (assembly names)] |
| 16 | **Pseudo-locale.** Output for the Formula 4 inputs; `[term:shield]` and `{damage}` pass through; a release configuration refuses the locale (injected flag). |
| 17 | **Font gate (Visual).** Retained screenshots: the 146 glyphs, a stacked-capital row, `Đ` against `D` at 12 px, digits, `×`, the Large cuts, the missing-glyph box, the missing-string marker and a pseudo-locale pass; lead sign-off. [BLOCKED: the chosen font] |
| 18 | **Language change.** `SetLanguage` fires once; to the same language fires 0; to an unloaded language is rejected; the binder's subscriber count returns to 0 after disable. |
| 19 | **Hard-coded text.** A Vietnamese literal and an interpolated string in runtime code are flagged; comment-only text, test and editor paths and allowlisted development logs are not. |
| 20 | **Format.** The grammar of Rule 20 parses the examples and rejects an unescaped bar or equals sign in a branch; the canonical bytes of a two-row table are exactly the header `LOCTABLE v1 en 2` then the rows in key order; a table with any parse error is not built. |

**Formulas**

| # | Given / When / Then |
|---|---|
| 21 | **F1.** Threshold 4: `999` → `999`; `1000` → `1.000` and `1,000`; `1001` → `1.001`; `1000000` → `1.000.000`; `1234567` → `1.234.567` and `1,234,567`; `-4` → `-4`; `-12345` → `-12.345`; `long.MaxValue` → `9.223.372.036.854.775.807`; `long.MinValue` gives `BadArgs` without throwing; threshold 5: `9999` → `9999` and `10000` → `10.000`. |
| 22 | **F2.** en `n = 1` → `1 cell`, `2` → `2 cells`, `0` → `0 cells`; vi `{n} ô` → `1 ô`, `2 ô`, `0 ô`; `n = −1` → `BadArgs`; the worked example gives `Deal 8 damage to 2 enemies`, `Gây 8 sát thương cho 2 kẻ địch` and `… to 1 enemy`. |
| 23 | **F3.** Width 136 → 22; 3 lines → 66. |
| 24 | **F4.** 40 percent: 20 → `(20 × 140 + 99) / 100 = 28`, + 2 = 30; 13 → `(13 × 140 + 99) / 100 = 19`, + 2 = 21; 0 → 2; 1 → 4; 5 → 9. 30 percent: 20 → 26 + 2 = 28, 13 → 17 + 2 = 19. 60 percent: 20 → 32 + 2 = 34, 13 → 21 + 2 = 23. A truncating ceiling gives 13 → 18 + 2 = 20 (mutant). |
| 25 | **F5.** `item.ember_blade.effect` (23 characters) is valid; `Item.EmberBlade.Effect`, `a.b` and `a.b.c.d` are not. |
| 26 | **F6.** FNV-1a 64: `""` → `cbf29ce484222325`, `"a"` → `af63dc4c8601ec8c`, `"foobar"` → `85944171f73967e8`, `"x1"` → `08f10707b58d70c2`; a checked multiply that throws on overflow is rejected. |

**Non-functional**

| # | Criterion |
|---|---|
| 27 | **Determinism.** The same table and arguments give the same output and checksum over 1,000 runs; row order does not change the checksum. |
| 28 | **Engine-free.** `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static`, float, double, `DateTime`, `Random`, `GetHashCode`, `CultureInfo.CurrentCulture`, `string.Format`, parameterless `ToString`, or culture-based compare, case or normalize call without an explicit invariant or ordinal argument. The assembly name is NOT SOURCEABLE until the ADR. |
| 29 | **Allocation.** The formatter into a buffer: delta 0 over 1,000 warmed calls; the binder: unchanged arguments give 0 bytes and 0 `Resolve` calls, a changed argument gives exactly 1 `Resolve` and no more allocation than the control `new string`. |
| 30 | **Operation budget (counted, not timed).** Grouping iterations at most 7; key lookup comparisons at most `1 + ⌊log2 N⌋`; 0 `Resolve` calls over 600 idle frames. |
| 31 | **Play Mode.** Subscription pairing across two enter and exit cycles with the domain reload off. |

**Blocked criteria by system.** Systems 2 and 4: the real slot sets per kind and the record types. System 5: the real snapshot. Systems 26 and 37: popup remeasure and the stored language (the stubbed `SetLanguage` is testable now). System 6: key-label text. The font and slot table: all real budgets and criterion 17. The architecture ADR: assembly names.

**Watched failures.** Each mutant must make a named criterion fail: grouping from 5 digits, or no padding of the middle group (21); plural of 0 treated as `one` (22); culture formatting such as `ToString("N0")` (12, 6); the fallback order swapped (9a, 9d); a blank for a missing key (9b, 9f); an argument leaking into the marker (9c); a number typed inside a translation (5b, 19); decomposed text accepted, or an NFC check without the combining-mark check (11); arguments indexed by order of appearance (5a); a truncating ceiling in Formula 4 (24); `<` against `<=` at the 136 px boundary, an ellipsis or a shrink on overflow, or a break at the non-breaking space (10a, 10b); a culture sort for ordinal (3a); a two-pass tokenizer (8a); a static log-once set (9e, 28); a missing unsubscribe (18); a checked multiply (26).

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed Formulas 1 to 6; its rule-level defects are fixed in the Detailed Design (Rule 20, the key grammar, the escapes, `sint`, the allowed glyph extras).*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| **Who reviews the English** (a friend, a paid pass on the highest-visibility strings, an LLM draft plus a check). | user | Before the first string freeze. |
| **Native review of the Vietnamese glossary picks** (Hướng, Chào mời, Neo, Suy kiệt, Chậm chạp, Hero buff). | user, writer | Before the glossary is locked. |
| **The font and the Vietnamese render gate;** no font file exists in the project; the candidates are in the art bible; the gate also covers the Large cuts, the digits, `×` and the approved punctuation. | art-director | First spike. |
| **Architecture ADR:** the assembly name, the TSV against JSON choice, the loader (a `TextAsset` under `Assets/`), custom table against the Unity Localization package (not installed; Addressables not installed). | technical-director, lead-programmer | Before implementation. |
| **Engine facts to verify:** TextMeshPro availability in 6.6 with ugui 2.6, a static raster atlas with the 146 glyphs, `HasCharacters`, a `.tsv` importing as a `TextAsset`, culture behaviour in the player, Pixel Perfect with a UGUI canvas. | engine-programmer | First spike. |
| **Sync with other GDDs:** System 2's "localization key" field becomes a slot list (keys derived); System 26's missing-key policy, first-use hint wording and `term.*` are answered here; the index says System 7 is MVP Core with effort S and a "one language, no font" stub (it is Foundation, effort M, two languages); the art bible 7.5 shows "1.2K" and 7.7 lists K and M and writes "12x3" (amend to no K or M and `×`), has no missing-string or missing-glyph form, no Large line pitch and no 20 numeral cut, and 7.13 still says `accessibility-requirements.md` does not exist. | Systems 2, 26 and the index, art-director | Sync after this GDD. |
| **The deprecated build define:** the pseudo-locale gate uses an injected flag instead of `DEVELOPMENT_BUILD`. | engine-programmer | First spike. |
| **Plural and gender** stay at the minimum; Vietnamese needs none; adding a language later may need more forms (the format version reserves the slot). | localization-lead | Tier 2. |
| **Event log text:** who owns keys for dynamic kinds (the 12-entry intent list, cooldown breakdown with sources). | Systems 4, 26, 30 | Those GDDs. |
| **Sorting lists:** designer-defined order is used; a Vietnamese collation is needed only if a list is ever alphabetised. | localization-lead | If needed. |

> *Specialists consulted for these questions: systems-designer, localization-lead, writer, gameplay-programmer, engine-programmer, art-director, creative-director, qa-lead (2026-11-13).*
