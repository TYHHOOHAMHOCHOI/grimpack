# Art Bible: Grimpack (working title)

> **Status**: Draft (all nine sections written and approved section by section; open items below)
> **Owned By**: art-director
> **Last Updated**: 2026-11-10
> **Art Director Sign-Off (AD-ART-BIBLE)**: REVISED 2026-11-10 — the first review returned CONCERNS; the flagged consistency items were corrected in this file, and the revised file has **not** been re-reviewed by the art-director. Still open because they need outside input: `design/accessibility-requirements.md` (does not exist; Section 7 is not final and the 4.12 limits are an unadopted proposal); tool-verified contrast and colour-blind figures (every figure in Section 4 onward is hand-computed, NOT VERIFIED); the hero, monster, item and glyph rosters; a stored master palette file; the two reference screenshots (no storage path decided); the engine spikes (S1 to S6, the icon spike, the Vietnamese font test); and the provisional cell edge C, snapping mode and canvas architecture.

`/art-bible` writes these nine sections in this order and replaces each
section's `[To be designed]` line as that section is approved. A section still
holding its placeholder is incomplete; a later run fills only those.

## 1. Visual Identity Statement

### 1.1 The Visual Rule

> **The world is soft ink; only things that matter emit light.**

**What "matters" means.** The anchor chosen in brainstorm left this word undefined, and a rule is only as good as its weakest term. A thing matters if either of these is true:

- **(a) The player can change it.** Examples: items, grid cells, hero order, door cards.
- **(b) It tells the player what their arrangement did or will do.** Examples: tier markers, active synergies, the current-turn indicator, enemy intent, HP.

Everything else is ink: walls, floor, props, backdrops, panel chrome, ambient particles. HP and intent count as things that matter, so they may be lit, but they are kept low in saturation (Section 4) so they do not compete with the tier hues.

**What "emit light" means.** Light is brightness first and saturation second. A lit element is the brightest or most saturated thing in its neighbourhood. An element that is merely coloured but sits at the same value as its surroundings is not lit.

**What "soft" means.** The ink has a floor and the light has a ceiling.

- The darkest pixels are a deep ink-blue, never pure black.
- The brightest pixels are a warm bone or a tier highlight, never pure white.
- These are testable limits. Section 4 sets the exact values, and mock-ups are checked against them.

**How to use it.** When any visual choice is ambiguous, ask: *does the player decide or read anything from this?*

- If no, push it toward ink: dimmer, lower saturation, fewer value steps.
- If yes, it may emit light. How much is set by how much it matters, and Section 4 ranks that.

### 1.2 Rationale

**Contrast is the scarce resource, and this game spends it on data.**

- Six tier colours are gameplay information.
- The player reads three backpack grids, enemy intent, and turn order before every fight (Pillar 4).
- Colour only carries meaning against a quiet field. A world that is already bright and busy cannot make a Gold item mean "Gold".

**Escalation needs headroom (Pillar 2).**

- Pillar 2 says small triggers stay quiet and big chains are unmistakable.
- That only works if the baseline is calm. In a soft-ink world, a glow is an event.
- Stillness becomes the setup for the payoff instead of competing with it.

**It is easy on the eyes by construction.** A 30-60 minute session spent studying dense grids is better served by low ambient luminance and gentle contrast. The user chose the dark, soft, low-saturation tone of reference 2 for this reason. The rule makes that tone enforceable instead of a mood.

**It is one question, not a style guide.** A solo developer with AI-assisted art will face hundreds of small calls: a sprite's rim light, a panel's bevel, a particle's colour. "Does the player read or decide anything from this?" settles most of them in seconds.

**What we take from the references** (the two screenshots the user supplied):

- **Reference 2 (dark dungeon, tier-coloured item grid, three units with HP bars).**
  - Keep: the dark stone dungeon, and the units lit on pedestals. That is the rule working.
  - Warning: its item grid lights every cell at full saturation, so nothing outranks anything. We keep tier-coloured frames but control how much of each cell is lit, so the tier reads as a thin lit edge on a mid-value cell.
- **Reference 1 (bright forest, three-panel layout).**
  - Keep: chunky outlines, thick plain panel frames, simple readable shapes. We adopt this form language.
  - Skip: its bright tone.
  - Note: its glowing items on the tree pop out clearly even in a bright scene, which shows light-as-information holds up outside the dark palette.

**Simplicity guard (user direction: simple, easy on the eyes).**

- Frames are thick and plain.
- Each material gets few value steps.
- No filigree, no texture noise, no decorative detail.
- If a detail helps neither silhouette nor light-as-information, cut it.
- This also keeps the palette small and consistent for AI-assisted generation plus hand clean-up.

### 1.3 Supporting Principles

**Principle 1: Light is information.**
- *Serves:* Pillar 4 (Arrangement Is the Thinking), primary. Pillar 2 (The Payoff Is the Product), secondary.
- *Design test:* When it is ambiguous whether an element should be brighter or more saturated, choose dimmer, unless the player reads or decides something from it this turn.
- *Screenshot check:* Build a lit mask and a mattering mask of the frame (method and per-state budgets in Section 4.10). At least 90% of the lit pixels must fall inside the things that matter: items, tier markers, active synergies, the current-turn indicator, intent and HP. Any lit decoration, chrome or ambience gets demoted. (This replaces the earlier "brightest 10% of pixels" wording, which could not catch decoration in a dense frame; the numbers are proposals until the first full-frame mock-up.)
- *Why it serves the pillars:* Pillar 4 requires everything needed to decide to be findable before the fight. Pillar 2 requires a quiet baseline for the payoff to stand out against.

**Principle 2: Form before hue.**
- *Serves:* Pillar 4. Items and tiers must be identifiable while arranging three grids and choosing among three door cards. Colour-blind safety follows from the same test.
- *Design test:* When shape and colour are both candidates for distinguishing items or tiers, choose the one that survives desaturation. Colour reinforces form and never replaces it.
- *Screenshot check:*
  - Convert to greyscale and run colour-blind simulation (deuteranopia, protanopia, tritanopia).
  - All six tiers must still be separable by frame notch, corner pip or border shape.
  - Each item must be identifiable by outline alone at 1:1 native pixel scale. (See open item 2: the native scale is not yet set.)
- *Note:* This merges the brainstorm anchor's "Tier is read by form before hue" and "Silhouette first" into one principle. Both ask the same question: does the shape still tell you before colour does?

**Principle 3: Escalate by adding, never covering.**
- *Serves:* Pillar 2 (The Payoff Is the Product). It restates Pillar 2's own tiebreak: when a bigger effect and a clearer cause conflict, the clearer cause wins.
- *Design test:* When a bigger payoff effect conflicts with a clearer cause, choose the clearer cause. Escalation lives in the scene column and in number size, never on top of the grid, intent icons or HP.
- *Screenshot check:* Freeze the biggest payoff. Every intent icon, every HP value and the set of items that fired must still be legible.

**Principle 4: Cost is visible, and it subtracts.** *(Added 2026-11-10 at the user's decision, after Section 4 drafted its colour rules.)*
- *Serves:* Pillar 5 (Power Has a Price).
- *Design test:* When a cost and its benefit could be shown apart or together, show them together on the same plate or card, and show the cost as light taken away (darker, desaturated plum), never as more light.
- *Screenshot check:* On any Gold or Red door card or item, the benefit and the curse are readable in the same glance, and the curse region is darker than the plate around it.
- *Detail:* The shape (a hanging tag) is in Section 3.4.4 and the colour (Plum) is in Sections 4.6 and 4.14.

### 1.4 Pillar Coverage

| Pillar | Visual expression |
| ---- | ---- |
| P1 Luck Follows Commitment | No direct visual rule. It is a systems pillar. |
| P2 The Payoff Is the Product | Principles 1 and 3 |
| P3 Earned Rarity | No direct visual rule. It is a systems pillar. |
| P4 Arrangement Is the Thinking | Principles 1 and 2 |
| P5 Power Has a Price | Principle 4, with the colour rules in Section 4 (Gold, Red, the curse tag, and "Red is a room": the middle and right columns shift palette in boss rooms, not just an accent). |

### 1.5 Alternatives Considered

**For the one-liner:**

| Alternative | Why rejected |
| ---- | ---- |
| "Quiet world, loud payoff" | It tracks Pillar 2 but only resolves payoff decisions. It says nothing about item frames, HP bars or panel chrome. Kept as the mantra behind Principle 3. |
| "Readability above all" | It is true of every game and gives no identity. It would also excuse a bright, busy approach. It is hard to test against a screenshot. |
| "Dim, soft, cursed" (mood adjectives) | It describes a feeling, not a decision. It cannot say which of two sprites is right. |
| "Colour is data; everything else is grey" | It protects tier colours but ignores brightness, which is the stronger channel. It also kills the warm "you" light and cold dungeon light the anchor relies on. A flat grey world is not easy on the eyes. |
| "Everything is torch-lit" (realistic warm lighting) | Realism spawns ambient light sources that do not matter, which breaks Principle 1. |
| The anchor rule, unchanged | **Adopted**, with "matters" defined so it can be tested. |

**For the principles:**

- **Keeping all four anchor principles.** Rejected because the brief asks for 2-3 and silhouette and tier-form share one test. Merging loses nothing.
- **A "strict palette discipline" principle.** Rejected because it is an asset specification, not identity. It belongs in the asset and palette sections.
- **A "cost is visible" principle for Pillar 5.** First deferred to Section 4 because its wording depends on the colour rules; adopted as Principle 4 on 2026-11-10.

### 1.6 Open Items Carried Forward

1. **The 10% squint threshold may be too tight.** A full grid of tier-framed items could exceed 10% of pixels on its own, as in reference 2. *Resolved in Section 4.10:* replaced by a precision-plus-budget method; the budget numbers still need confirming on the first full-frame mock-up.
2. **Native resolution and grid cell size are not set.** The silhouette test needs a defined 1:1 pixel scale and cell size. Resolve in Section 8 (Asset Standards); until then Principle 2's silhouette test cannot be run.
3. **Photosensitivity.** Escalating payoffs with particles and glow can conflict with "easy on the eyes". *Proposed in Section 4.12* as a flash and strobe limit for the biggest payoff; it still has to be written into `design/accessibility-requirements.md` and checked against the current standard.
4. **Colour conflicts:** red HP and damage vs the Red tier; decorative gold and currency vs the Gold tier; ambient poison green vs the Green tier; curse or night purple vs the Purple tier; white UI text vs the White tier. *Resolved in Section 4.7.*

## 2. Mood & Atmosphere

### 2.1 The Mood in One Line

**A lamplit study inside a cold dungeon: calm hands, high stakes.**

The player is a careful architect working by a small warm light. Around them is a vast, quiet, indifferent dark. This is the mood version of the Section 1 rule. Warm light is "you" and what you built. Cold light is the dungeon. Everything else is ink.

Three axes separate one state from another. Every state below is placed on them so that no two states can be confused at a glance.

| Axis | Low end | High end |
| ---- | ---- | ---- |
| **Temperature** | Cold: Fog wash on ink, cold rim on monsters | Warm: Candle wash, warm rim on heroes |
| **Lit-area budget** | Few lit pixels (menus, defeat) | Many lit pixels (planning, payoff peak) |
| **Motion** | Still | Pulsing, rising, swelling |

The lit-area budgets and average-luminance caps quoted below are defined once, in Section 4.10. They are repeated here so a state can be read on its own. **All budget and cap numbers are proposals** and are re-measured on the first full-frame mock-up (see Section 4.10).

### 2.2 Eyes-First Rules (apply to every state)

The target is a 30-60 minute session of studying grids without fatigue. These are mood constraints, not polish, and they apply even where a state below asks for drama.

1. **Average picture level (APL) is capped per state.** APL is the mean relative luminance of the composited frame. Caps are listed per state. Ordinary states sit at or below 0.08.
2. **No persistent flicker.** Ambient light may "breathe" at 0.5 Hz or slower, with amplitude of 6% luminance or less. Nothing ambient pulses faster.
3. **Ambient motion is limited to two elements per screen** (for example drifting motes and a banner sway). Anything that moves must belong to a thing that matters, or be one of those two.
4. **Saturated area is capped.** Colour at saturation 0.60 or higher is tier colour (and Red-room light, Section 4.8) only, **except on item icons, which may use any master-palette colour (user decision, Section 8.5.3)**. The 12% cap counts tier colour and Red-room light only; icon pixels are measured separately in the lit-mask test (4.10) and must not hide a tier edge. It never exceeds 12% of the frame outside the payoff peak.
5. **Transitions between states are eased over at least 250 ms.** There are no hard cuts of the whole palette, with one exception: the defeat extinguish (2.3.9), which is slower still.
6. **The soft limits from Section 1 hold in every state.** Floor is `#0B0F1C`, ceiling is `#F2E8CE` (Section 4.3). Even the payoff peak does not leave this range.

### 2.3 State Profiles

Nine states are covered. Non-combat rooms are one state with three variants, because shop, event and recruit are each a different feeling but share the same layout logic.

#### 2.3.1 Menus (title, run setup, settings, pause)

- **Emotional target:** Invitation without pressure. The feeling of standing at the threshold of the dungeon with a lantern, before committing. Calm and slightly curious.
- **Lighting:** One warm point of light (a lantern) in a large cold ink field. Low contrast everywhere else. Light direction top-left, as everywhere.
- **Descriptors:** Still, spacious, hushed, inviting, unhurried.
- **Energy:** 1 of 5.
- **Mood carrier:** The single Candle lantern and its small light pool on stone. It is the only lit object, so it is also the "start" affordance's neighbour. The title lettering is Bone on ink and does not glow.
- **Ink versus light:** Maximum ink. Menus are the reference baseline against which every other state is judged. Lit budget 6%, APL 0.06 or less.
- **Pause overlay:** Drops the scene one INK step and desaturates it. It must not hide the grid, because a paused player may be thinking, and Pillar 4 says that is when the thinking happens.

#### 2.3.2 Arrangement / Planning (backpacks and stash, before combat)

- **Emotional target:** Calm, absorbed focus with a pulse of anticipation. The "tinkerer at the workbench" feeling. The player should feel clever, not rushed.
- **Lighting:** Even, soft and low in contrast. Cool-neutral ink with no area tint. The only warm light is the Candle selection halo on the selected hero and the item being held. The left column (grid and stash) is the brightest region of the screen. The scene columns are held dim, as a stage waiting for the show.
- **Descriptors:** Tactile, orderly, deliberate, lamplit, quiet.
- **Energy:** 2 of 5.
- **Mood carrier:** Tier-framed items resting on a darker "tray" of cells. Items read like cut stones laid on dark felt. The thin lit tier edge is what makes the grid feel precious rather than busy.
- **Ink versus light:** The light is in the grid. This is the one state where the player's hands are where the light is, which is the Section 1 rule: the player can change items, so they are lit. The scene is almost pure ink. Planning-only affordances (placement hints, empty-cell dots, drag handles) are the dimmest lit things in the state, so they never compete with tiers.
- **Budget:** lit 12%, APL 0.08 or less. This is the highest ordinary budget, because a full grid is the densest lit content in the game.

#### 2.3.3 Door Choice (three cards)

- **Emotional target:** A hushed, weighing wager. Temptation with a shadow in it: the player sees what each door promises and feels what it might cost (Pillar 5). Heart rate up, body still.
- **Lighting:** A dark corridor. Each card is a lit doorway that emits its own tier light. Each card throws a faint tier-coloured wash (10-15% alpha) onto the floor beneath it, so three doors cast three pools of colour. Warm Candle light touches the party from behind. Gold and Red cards also cast a dark-plum edge shadow on one side, so the cost is visible in the light itself.
- **Descriptors:** Hushed, weighty, tempting, threshold-like, charged.
- **Energy:** 2 of 5 for motion, but high tension. This is a deliberate mismatch.
- **Mood carrier:** Three vertical lit rectangles in a dark room, side by side. The cards are the only lit things. Their tier form cues (Section 3.4) and rank pips carry the tier. The curse tag carries the cost.
- **Ink versus light:** Total ink except the cards and the party's rim. Doors that carry curses add a subtractive element (the plum tag and shadow), so they take light away from the card's own glow. This is the one place "power has a price" is shown as light.
- **Budget:** lit 12%, APL 0.07 or less. Cards breathe at 0.5 Hz or slower, amplitude 6% or less.

#### 2.3.4 Combat (automatic, ordinary rooms)

- **Emotional target:** Steady, watchful suspense, like watching a machine you built run. Confidence with a small tremor. It is not frantic, because the player has no actions.
- **Lighting:** A cool dungeon key from top-left (Fog wash, about 10% alpha). Heroes carry a warm Candle rim on the key side. Monsters carry a cold Fog rim. Low-to-mid contrast. Because the player cannot act, the lighting does nothing urgent.
- **Descriptors:** Measured, rhythmic, watchful, clinical, steady.
- **Energy:** 3 of 5.
- **Mood carrier:** The turn marker (a chevron under the acting unit) and the speed gauge ticks give a heartbeat. Each item that fires shows as a plate edge pulse in the grid (one to two frames, then an echo that holds while its avatar lives) and as a **quiet avatar** floating above the acting hero for a few hundred milliseconds (Section 5.10): a tier edge at its rest value, a 1 px ring and a small chip, no particles. These are the "small triggers stay quiet" of Pillar 2.
- **Ink versus light:** Light is rationed to the information layer: intent tabs, HP, turn marker, tier edges, and trigger pulses. The planning affordances switch off. Monster bodies are ink with a cold rim. Lit budget 8% (below planning, because the grid goes still), APL 0.07 or less. (Re-measure with avatars, rings and chips counted as information; the hand estimate is about 0.1% for a Tick and about 0.4% for a Chain, Section 5.13.)

#### 2.3.5 Payoff Peak (a big chain going off)

- **Emotional target:** Awe, vindication, release. "It all lined up." The player's own arrangement did this, and they should be able to read why (Pillar 2: the clearer cause wins).
- **Lighting:** The only moment the scene reaches the light ceiling at any real area. Each fired item sends a thread of light in its own tier highlight from its source plate to its avatar (Section 5.10.9). In a Detonation the last up to three avatars form a fan and release together into one warm bone-white bloom (core up to `#F2E8CE`). Enemy bodies are backlit against the bloom and stay readable as silhouettes. Warm Candle wash rises over the right column to a maximum of 30% alpha. Contrast peaks here and nowhere else.
- **Descriptors:** Swelling, radiant, resonant, inevitable, cathartic.
- **Energy:** 5 of 5, but bounded by the photosensitivity limits in Section 4.12.
- **Mood carrier:** The convergence: many tier colours pouring into one warm bloom, plus damage numerals scaled by size. "All coloured light becomes warm light when the machine fires" is the visual statement of the fantasy.
- **Ink versus light:** The scene column becomes a light source and the world briefly stops being ink, but only in the scene. The protected layer (grid, HP, intent, turn marker) stays at its normal values and sits above the bloom in draw order. This is Principle 3 in practice: escalation by adding. Budget: lit up to 25% for a transient of 1.5 seconds or less, APL up to 0.20 at the instant of peak.
- **Escalation ladder (Pillar 2):**

| Level | Trigger size | What appears |
| ---- | ---- | ---- |
| Tick | Single trigger | One-to-two frame pulse on the item edge only |
| Chain | Several triggers | Light threads from source items to target, number size steps up |
| Surge | Large chain | Candle wash over the right column, particles |
| Detonation | Run-defining chain | Full bloom to the ceiling, long afterglow, largest numerals |

- **Before and after the peak:** Ambient scene motion pauses for the last beat before detonation. This is a stillness cue and does not dim anything. Afterglow is embers and a fading wash over 1 to 2 seconds. The ink returns on its own.

#### 2.3.6 Non-Combat Rooms (shop, event, recruit)

The right column becomes a shop or event panel but the three-column layout stays. All three share a "no threat" calm and a lower energy than combat. They differ in temperature and in the number of lit things.

**Shop**
- **Target:** Relaxed, acquisitive curiosity. The safe room where the player spends.
- **Lighting:** The warmest ordinary state. A Candle wash of about 14% across the scene, plus lantern pools on the counter. Low contrast.
- **Descriptors:** Warm, cosy, orderly, abundant, unhurried.
- **Energy:** 1-2 of 5.
- **Carrier:** Rows of wares, each a small lit tier edge on a shelf of ink, and round coins. The warm wash and horizontal shelf rhythm tell the player "trade", not "fight".

**Event**
- **Target:** Uneasy curiosity, a hush before a bargain. Pillar 5 lives here: the offer has a price.
- **Lighting:** Cold Fog wash. A single off-centre lit object (altar, well, stranger) in a large area of negative space. High local contrast, low global contrast.
- **Descriptors:** Eerie, hushed, expectant, lonely, ambiguous.
- **Energy:** 1-2 of 5.
- **Carrier:** One lit object in a very dark room. The emptiness around it does the work.

**Recruit**
- **Target:** Warmth, relief and kinship. A new light joins the party.
- **Lighting:** A warm Candle pool (about 18% alpha) centred on the recruit. The recruit is the single lit figure. The party's pedestal pools visibly reach toward it.
- **Descriptors:** Warm, hopeful, intimate, gentle, welcoming.
- **Energy:** 2 of 5.
- **Carrier:** Two warm pools merging. It is the only state where the player's warm light and another figure's light overlap on purpose.

- **Ink versus light (all three):** Only the thing the player can interact with is lit: wares, the offer, the recruit. Everything else is ink. Lit budget 10%, APL 0.08 or less.

#### 2.3.7 Boss / Red Room

- **Emotional target:** Dread and grandeur. "This is the cost, and it is big." Cornered, committed, and a little exhilarated.
- **Lighting:** Low-key. The ink floor shifts to wine (Section 4.8, Wine ramp). The cold Fog wash is replaced by a Red-glow wash of 25-30% alpha behind the boss. The boss is a silhouette against that glow with a hot rim. Contrast is the highest of any non-payoff state. Large saturated-red areas are capped at 8% of the frame and do not flash.
- **Descriptors:** Oppressive, monumental, ominous, hot, cornered.
- **Energy:** 4 of 5.
- **Mood carrier:** The palette shift itself ("Red is a room"). A player who looks up from the grid sees the whole stage has changed temperature before reading any text. The environment also loses its calm geometry (Section 3.5): the arch is broken and diagonals appear.
- **Ink versus light:** The room is information (it says "this is the boss"), so it is allowed to be tinted, but only as tinted ink, not lit. The protected layer keeps its normal base palette, so tier colours stay readable and Red-tier items do not vanish into a red room. Only things that matter are lit; the boss matters, so it gets the glow. Lit budget 10%, APL 0.09 or less. *("Scene" means the middle and right columns; the left UI column is untouched. Mythic rooms use the same shift until that is decided; see Section 4.8.)*

#### 2.3.8 Run Victory

- **Emotional target:** Quiet pride and exhale. Earned, not fireworks. The payoff peak already supplied the thrill; this is the afterglow of having built something.
- **Lighting:** The ink lifts by one step and a warm Candle wash (about 22%) fills the scene, like a first light of day coming through the arch. The cold dungeon light is gone. The heroes stand in warm light. Nothing uses the Gold tier hue; the warmth is Candle, not Gold, so victory does not borrow a tier's meaning.
- **Descriptors:** Warm, still, proud, exhaled, golden-hour-soft.
- **Energy:** 2 of 5.
- **Mood carrier:** The lifted ink and the heroes in warm light, with the winning build shown at its highest highlights as a trophy. This is the only state where the world gets lighter than the ordinary baseline.
- **Ink versus light:** The rule relaxes slightly: the world is allowed to be less inky, because the run is over and the player has nothing left to decide except reading their build. Budget 15% lit, APL 0.13 or less.

#### 2.3.9 Defeat

- **Emotional target:** Sombre acceptance, with a pull to try again. It must not feel punishing or shaming. The player should read what happened (the arrangement they made) and want another run.
- **Lighting:** The warm pools on the pedestals go out, one hero at a time. What is left is cold Fog light and desaturated ink. Non-tier colour drops to about 60% saturation. Tier edges dim to about 55% brightness but stay readable, so the grid can be studied like an autopsy.
- **Descriptors:** Hushed, cold, ashen, reflective, still.
- **Energy:** 1 of 5.
- **Mood carrier:** The extinguishing of the warm "you" light. The player's light was the thing that mattered, so its absence is the loss. This extinguish is the one slow palette change in the game and runs slower than the 250 ms minimum.
- **Ink versus light:** Near-maximum ink. Only the build is still faintly lit. Budget 5%, APL 0.05 or less.

### 2.4 Payoff Peak Versus Combat

These two states share a screen layout and must never be mistaken for each other. This is where Pillar 2's "small stays quiet, big is unmistakable" becomes a visual contract.

| Property | Combat (ordinary) | Payoff peak |
| ---- | ---- | ---- |
| Who emits light | Information only (intent, HP, turn marker, tier edges, quiet item avatars, small rings and chips) | The scene column itself, driven by the items that fired |
| Light colour | Cool wash, warm hero rim, tier-coloured edge pulses | Tier threads converging into one warm bone bloom |
| Area lit | 8% of frame | Up to 25%, transient (1.5 s or less) |
| Reaches the light ceiling | Never at area; only single-pixel highlights | Yes, at the bloom core |
| Duration of a beat | About 260-400 ms per item (a quiet avatar), with a 1-2 frame plate pulse in the grid | 1.5 s peak plus 1-2 s afterglow |
| Motion | Steady pulses on a rhythm | Stillness, then a build, then a swell, then embers |
| Protected layer (grid, HP, intent) | Normal | Normal. The bloom sits beneath it in draw order. |
| What the player should feel | "It's running." | "It all lined up." |

### 2.5 State Distinctness (thumbnail test)

Shrink any frame to thumbnail size or squint. The player should be able to name the state from one cue, with no text.

| State | The one-glance cue |
| ---- | ---- |
| Menus | One warm lantern in a big cold field |
| Planning | Left column is the brightest region; scene is dim |
| Door choice | Three tall lit rectangles in the dark |
| Combat | Evenly low light; small pulses; turn chevron; one floating plate above the acting hero |
| Payoff peak | The right column blooms warm and bright |
| Shop | The whole scene tinted warm, rows of small lit edges |
| Event | One lit object, large dark negative space, cold |
| Recruit | One warm pool with a figure in it |
| Boss / Red room | The scene column is wine-coloured with a hot backlight |
| Victory | The lightest frame of the game, warm, heroes front-lit |
| Defeat | Desaturated, no warm pools, dimmed tier edges |

Shop and Recruit are the closest pair (both warm). They differ in lit-object count and composition: the shop is many small lit items in rows, the recruit is one lit figure in a single pool. If the first mock-ups fail this test, the shop wash is reduced before anything else changes.

### 2.6 Transitions

- **Planning to combat:** The grid's planning affordances fade, the scene lights rise from dim to the combat key over 250 ms or more, and the first turn marker appears.
- **Combat to payoff:** There is no cut. The payoff is the existing combat state with extra light added, which keeps cause and effect continuous.
- **Combat to door choice:** The scene dims to ink and the cards rise lit, one at a time, left to right.
- **Any room to boss:** The wine shift is visible on the door card before the room opens (the card's tier and Red form cue), then the palette change is eased over 250 ms or more once inside.
- **Boss to victory:** The wine ink lifts to normal, then to the lifted victory ink, with the warm wash rising last.

### 2.7 Alternatives Considered

| Alternative | Why rejected |
| ---- | ---- |
| One global dark mood for the whole game | Fails the "each must look distinct" brief. The player would learn nothing about state from the palette. |
| A different hue family per state (blue combat, green shop, and so on) | Spends the hue channel on mood and collides with the tier colours, which are the scarce data. Section 1 reserves saturation for tier. State differences are carried by temperature, lit area and motion instead. |
| A bright, saturated payoff that fills the screen | Breaks Principle 3 (covers the grid) and the photosensitivity limits. The payoff reaches the ceiling only in the scene column. |
| A dimming "hush" before the payoff | A dim followed by a bright creates a luminance pair, which is a photosensitivity risk and weakens the "adding, not covering" rule. Replaced by an ambient stillness beat. |
| A dramatic, saturated victory screen using Gold | Borrows a tier hue for a non-tier meaning. Victory uses Candle warmth instead. |

### 2.8 Pillar Coverage

| Pillar | How Section 2 serves it |
| ---- | ---- |
| P2 The Payoff Is the Product | The escalation ladder, the only-at-peak light ceiling, and the protected layer |
| P4 Arrangement Is the Thinking | Planning is the lit-grid state. Combat lowers the planning affordances. Defeat shows the build as an autopsy. |
| P5 Power Has a Price | Door choice shows cost as subtractive light (plum tag, dark shadow). The Red room is a palette shift, not an accent. |
| P1, P3 | No direct mood rule, as in Section 1. |

## 3. Shape Language

### 3.1 The Grammar in One Paragraph

Grimpack's shapes follow one idea: **shape carries identity before colour does** (Section 1, Principle 2). Characters and items are built from a small set of stable primitives. Rarity, danger and cost are shown by cutting or adding to those primitives in a fixed vocabulary. Shape also has to survive greyscale, colour-blind simulation and a small native resolution, which keeps the vocabulary small and rigid. The vocabulary is reserved: once a form means something, nothing else may use it.

### 3.2 Reserved Shape Vocabulary

| Form | Reserved meaning | Where it is allowed | Where it is banned |
| ---- | ---- | ---- | ---- |
| Square or rectangle | Containers, cells, White-tier plates | Everywhere | None |
| Chamfered (cut) corner | **Tier** (1, 2 or all corners, Section 3.4.4) | Item plates, door cards, tooltips | UI chrome, buttons, environment |
| Inner bracket line (double frame) | Gold tier | Gold plates and cards | Everywhere else |
| Serrated edge (equal-spaced teeth) | Red tier and the Red room | Red plates and cards, boss room frame | Other tiers, UI chrome. Monsters may have spikes, but not a regular tooth row |
| Hanging tag (additive, dark) | **Curse** | Gold and Red plates and cards | Everywhere else |
| Downward pennant tab | **Enemy intent** | Above enemies | Everywhere else |
| Circle or ellipse | Currency, pedestals, the Candle lantern | As listed | Item plates, frames |
| Square lattice | The backpack grid only | Left column | Environment (bricks use running bond, Section 3.5) |
| Diamond | Rank pips on cards and tooltips | Cards, tooltips | Items at rest |
| Chevron | Turn marker, buff and debuff direction (also on the stat chip, Section 5.11) | As listed | Elsewhere |
| Expanding ring | **Area of effect.** A transient effect: outline only, never filled, never static, lives under 600 ms. | Item-avatar effects and enemy intent-glyph effects (Section 5.11) | Anywhere static, or as decoration |
| Floating tier plate (item avatar) | **An item acting.** A 1x1 plate of the item's tier floating above the acting hero's head. | Above the acting hero only (Section 5.10) | Above enemies (an enemy's action is a bare glyph with no plate), or at rest |
| Stat chip | **A stat change.** A plain square-cornered chip holding a stat glyph, a chevron and a signed number, all in Bone. | At the target's body band (Section 5.11) | Item plates, door cards |

This table is the single source of truth for "can I use this shape here?". It also makes colour a backup: a Gold plate is Gold because it has an inner bracket line, then because it is yellow.

*Serves the Section 1 rule:* a reserved form is information, so it may be lit. A shape used decoratively would be lit without mattering, which is exactly what the rule forbids. *Pillar:* Pillar 4 (Arrangement Is the Thinking).

### 3.3 Character and Monster Silhouettes

#### 3.3.1 Philosophy

- **Silhouette first.** Every character must be identifiable as a solid dark shape. Test: fill the sprite with INK-0 on INK-2 at native scale, then again at half size. The archetype must still be named correctly. A second run uses the figure-ground contract values (INK-3 on INK-1 for heroes and NPCs, INK-2 on INK-0 for enemies with their INK-4 outline, Section 5.9).
- **One distinguishing trait per archetype.** If two things need two traits to tell apart, one of them is redesigned. One trait is a single protruding shape (a horn, a hat, a long arm), not a texture.
- **Plumb heroes, leaning monsters.** Heroes stand on a vertical axis of symmetry. Monsters do not: whichever wrongness device they use (3.3.3), the silhouette as a whole is not mirror-symmetric, so even a "wrong count" or "misplaced feature" breaks the mirror. This single rule is the main way "slightly wrong" is read.
- **Compact, closed, convex** shapes read as objects. Long, repeated, linear shapes read as scenery (Section 3.7).

*Serves the Section 1 rule:* a monster is ink until it acts, so its silhouette must carry the read, not its colour. *Pillar:* Pillar 4 (the player reads intent and targets before the fight).

#### 3.3.2 The Heroes (one column, one pedestal size)

**Layout decision (user, 2026-11-10):** the heroes stand in **one vertical column** in the middle column of the screen, all on the **same pedestal size**. There is no front, middle or back depth slot. The top-to-bottom order is simply **the order in which heroes joined the party**: the run starts with one character and recruits more. Position therefore carries no mechanical meaning and **must not be encoded by silhouette, pedestal size or any other form cue.**

What identifies a hero instead:

- **Role family.** Silhouette is varied by role so the three starting archetypes read apart at thumbnail size. These families are provisional and are reconciled with the hero GDD. **All three share an equal body height H (within 10%)**, so the ratios below differ in width and height never reads as rank; the Support is tallest only by its prop (Section 5.2.3). Heroes never attack or cast: their body shows only idle, ready, hurt and down (Section 5.1).

| Role | Silhouette family | Read as |
| ---- | ---- | ---- |
| Tanker | Block: wide, low centre of mass, squared shoulders (about 1:1.1 width to height) | Stability, takes the hit |
| Warrior | Round: oval mass, balanced (about 1:1.4) | Direct, balanced |
| Support | Tall: narrow vertical, protruding prop above the head (about 1:2) | Reach, set-up |

- **One identifying prop** (hat, hood, lantern as a rectangular cage, staff) that sticks out of the silhouette. Recruited heroes keep the same rule: a role family plus one prop, so any hero is distinguishable from the others at a glance.
- **The party can be one, two or three heroes.** A single hero stands alone on the same pedestal. The column must look complete at every count, and an empty slot is shown as an empty pedestal outline, never as a gap in the layout.
- **Order display.** If the join order needs to be shown, a small numeral on the pedestal does it. It is not a pip (pips are reserved for tier, Section 3.2) and it is not lit unless the player can act on it.

Pedestals are ellipses of one shared size, and each carries the warm Candle pool (Section 4). Warm light says "you". This ties the shape to the colour rule.

#### 3.3.3 Monster Archetypes (provisional, to be reconciled with the enemy GDD)

Each archetype has one trait and one wrongness device.

| Archetype | One distinguishing trait | Typical "wrongness" |
| ---- | ---- | ---- |
| Swarm | Small, appears in identical groups of 3 or more | One member is slightly off-scale |
| Bruiser | Wide block with one oversized limb | Asymmetric arms |
| Skirmisher | Leans forward, long thin limb or tail | Leans off the vertical axis |
| Hexer / Caster | Hovers, with a gap beneath the body, and a hood or halo shape | No ground contact, hollow where a face should be |
| Warden | Flat plated top, shell-like | Wrong count of plates |
| Elite | Any archetype plus one added element (ring, crown, extra head) | The addition breaks the silhouette |
| Boss | At least twice hero height, composite body of several parts | Parts that do not match each other |

**How a monster reads as "slightly wrong".** Use exactly one of these devices per monster, never more:

1. Broken symmetry (one oversized or undersized limb).
2. Wrong count (an extra or missing eye, limb or plate).
3. Misplaced feature (eyes too low, mouth to one side).
4. Lean off the vertical axis.
5. A hole in the silhouette (negative space where mass should be).

"Slightly" is the limit. Two devices read as a different creature; zero reads as a hero. Monster eyes are dark sockets at rest and light for one to two pixels when that monster is acting or targeted. That is the monster "mattering" under rule (b) of Section 1.1. Monsters do not lunge: they hold one act pose while their intent glyph performs the action (Section 5.10.8).

#### 3.3.4 Figure-Ground

Because both the world and the monsters are dark, silhouette can fail by value, not by shape. Two rules fix this:

- The average value of a character's body must differ from the backdrop directly behind it by **at least two steps** on the INK ramp (Section 4.3). For enemies the INK-4 outline supplies the luminance separation (Section 5.9), because the ramp is nearly flat at the dark end (INK-2 on INK-0 is only about 1.36:1).
- Characters carry a 1 px rim on the key side (top-left): Candle for heroes, Fog for monsters (Section 4.13). The rim separates the silhouette from the backdrop even when the body is nearly the same value.

### 3.4 Item Shape Grammar

#### 3.4.1 Plate Construction

Every item is an **icon on a plate**.

- The **plate** is the footprint. It covers exactly the occupied cells (1x1, 1x2, 2x2 or L) as a single contour, not as separate cells. Its outline is a 1 px tier-edge outline, with the lit tier base as the thin frame inside it.
- The **icon** is a compact glyph, centred on the plate, readable by outline alone.
- The **cell bed** underneath is a mid-value ink (INK-2). Only the thin frame is lit, so the tier reads as an edge on a quiet cell, never a filled cell (the Section 1 warning from reference 2).

#### 3.4.2 The Four Footprints

| Footprint | Shape character | Readability rule |
| ---- | ---- | ---- |
| 1x1 | Square | Icon fills about 60% of the cell, leaving the frame clear |
| 1x2 | Rectangle, 1:2 | Icon drawn on a diagonal or as a compact glyph so it works in both orientations |
| 2x2 | Large square | The icon may be more detailed, but keeps one dominant silhouette |
| L | Five convex corners, one concave (a rectilinear L has six corners in all; the earlier wording "six convex" was a counting error found while drafting Section 8) | The concave corner stays square (never chamfered), so the L reads as one piece |

**Every multi-cell item (1x2, 2x2, L) also supplies a compact 1x1 avatar glyph** (Section 5.10.2). It is authored at the 1x1 cell edge and passes the same outline-only test.

#### 3.4.3 Rotation

**Decision (user, 2026-11-10): the plate rotates; the icon does not.**

- Icons stay upright in their footprint. This keeps the single light direction (top-left) intact, since a rotated pixel icon would light from the wrong side.
- 1x1 and 2x2 are rotation-invariant, so only 1x2 and L items show orientation, and their footprint shows it.
- Tier cues are re-derived from the rotated footprint, always relative to the screen, never to the item's own orientation. After a rotation the plate is redrawn.
- Elongated icons are authored as a diagonal glyph or a compact glyph so they sit well both upright and sideways. That is an authoring rule for the item artist (Section 8).
- **Avatar exception (Section 5.12, approved).** Icons stay upright in the grid and on plates. As a combat avatar only, an icon may translate on a stepped arc and may lean by integer row shear (at most 2 px at the top row), baked at import. No non-90-degree rotation, no resampling.

#### 3.4.4 Tier Cues (satisfying "form before hue")

All six tiers have a form cue that survives greyscale. The cue lives on the **plate silhouette** so it is visible at thumbnail size. The "anchor corner" is the top-left convex corner of the footprint in reading order.

| Tier | Plate form | Door-card extra |
| ---- | ---- | ---- |
| White | Square corners, plain 1 px frame. No mark. | 1 rank pip |
| Green | **One** chamfered corner (anchor, top-left) | 2 rank pips |
| Blue | **Two** chamfered corners, diagonal pair (anchor and last convex corner) | 3 rank pips |
| Purple | **All** convex corners chamfered | 4 rank pips |
| Gold | All convex corners chamfered **plus an inner bracket line** (a short inner line at each convex corner) | 5 rank pips |
| Red | All convex corners chamfered **plus a serrated top edge**: teeth 2 px tall and 2 px wide on a 4 px pitch, **drawn in Red-hi `#FF8A8F`, hanging inward from the lit 1 px top ring** (so the plate footprint never grows) | 6 rank pips |

Notes:

- **Red serration (decided 2026-11-10, from the planning mock-up).** In the first mock-up the teeth were drawn in the dark Red edge colour and were almost invisible, in colour and in greyscale. Teeth in Red base were still faint in greyscale (Red base luminance 0.18). **Red-hi teeth, inward from the ring, read clearly in the greyscale tier strip** (`production/qa/evidence/tier-strip-c24-grey-3x.png`). A 1x1 plate shows only three teeth (the corner chamfers take the first and last 6 px), so on a 1x1 plate this is still the weakest form cue; the icon spike (8.5.6) must confirm Red against Purple at native 1x and 2x. Fallback: 3 px teeth.
- The cues escalate in silhouette complexity as rarity rises. This mirrors Pillar 3: the rarer the thing, the more complicated its outline.
- **The weakest form pair is Green versus Blue** (1 cut versus 2 cuts). It is made robust by using a diagonal pair for Blue instead of an adjacent pair, so the two are different patterns, not different counts. The first silhouette test in the mock-up checks this pair first.
- **Curses are separate.** Gold and Red items and cards carry a curse tag, an additive dark plate hanging from the bottom edge (Section 4.7, conflict 4). Tier cues are cuts at the top and sides. The curse is an addition at the bottom. They cannot be confused.
- Rank pips are diamonds and appear on door cards and tooltips only, where there is room. Items at rest rely on plate form.
- **Assumption for Section 8:** this grammar needs a 1x1 cell of at least 16 native pixels per edge (20-24 preferred, which is what reference 2 appears to use). Chamfers are 2-3 px, teeth 2 px tall. If the native resolution forces smaller cells, the cue set is reduced for items (and kept in full on cards).

*Serves the Section 1 rule:* a tier is something that tells the player what their arrangement is, so it is lit, but as a thin edge and a silhouette cue, not as a flood of colour. *Pillar:* Pillar 4 (arrangement across three grids and three door cards must be legible), with Pillar 3 (earned rarity is visibly more complex).

### 3.5 Environment Geometry

- **Orthogonal and quiet.** Walls, floors and arches are flat planes. The one large curve is the round-headed niche that frames the first enemy line (a semicircular arch about 140 px wide against the party column, Sections 6.6 and 7.3). The second line stands in a square-headed bay beside it. There is only one arch on a combat screen. Diagonals and teeth are rare, so that when they appear they mean something.
- **Bricks use running bond (offset rows), never a square lattice.** The square lattice is reserved for the backpack grid (Section 3.2). Brick contrast is one value step, so the environment never competes with the grid for the "this is a cell" read.
- **Per-unit ground (amended by Section 6.6).** The stage is a flat frontal or side-on view with no perspective floor. The heroes stand in one vertical column, so there is no shared floor line: each hero and recruit has a pedestal, each enemy a contact shadow, each NPC its furniture. The wall plane is continuous. This keeps pedestals, shadows and intent tabs simple.
- **Area geometry:**

| Area | Geometry |
| ---- | ---- |
| Ordinary dungeon | Rectilinear arches and pillars. Large, flat and repeated. |
| Shop | Long horizontal counter and shelves (rows) |
| Event | A single tall central object with a lot of empty space around it |
| Recruit | A low **arch-shaped alcove** (a recess, never a ring or ellipse, which are reserved for pedestals and coins, Section 6.7). The round arch is the only curved environment form. |
| Boss / Red room | The arch is broken. A diagonal intrudes and a serrated edge appears on the frame. The room is "wrong" in the same way monsters are. |
| Menus | A wide arch, a lantern and plenty of empty space |

*Serves the Section 1 rule:* environment is ink: low contrast, few value steps, no lit detail. *Pillar:* Pillar 4 (a quiet field lets the grid and tiers carry the data) and Pillar 5 (the boss room's broken geometry is the shape of the cost).

### 3.6 UI Shape Grammar

- **Thick, plain frames.** Panel frames are two to three native pixels thick in INK-3, with a one-pixel INK-4 inner highlight on the key side. No rivets, bolts, filigree or ornament. This is the form language adopted from reference 1.
- **Hierarchy of containers:** panel (thick frame), then section (thin line), then cell (square). Each level is a step quieter than the one above.
- **Square corners on all chrome.** Chamfers and teeth are reserved for tier (Section 3.2). Buttons are plain rectangles.
- **The world sits inside the UI, not behind it.** The scene is a window held in the panel frame, as in reference 1. The UI frame is ink: it is cut from the same dark stone as the world and never lit. That keeps the world and the UI one material.
- **Lit UI is information only:** tier plates, HP bars, intent tabs, the turn chevron, the current selection. Selection is drawn outside the plate with a 1 px gap, so it can never merge with a tier frame.
- **Reserved UI shapes:** intent is a downward pennant tab above an enemy. The curse is a hanging tag. Currency is a round coin. Turn is a chevron. Buff and debuff are shown by chevron direction (up or down) on a plain plate, not by hue.

*Serves the Section 1 rule:* chrome is ink by definition; thick plain frames leave all the light for data. *Pillar:* Pillar 4.

### 3.7 Lead Shapes Versus Supporting Shapes

To avoid confusion with the heroes, this section calls the brief's "hero shapes" **lead shapes**.

| | Lead shapes | Supporting shapes |
| ---- | ---- | ---- |
| What they are | Things that matter under the Section 1 test: items, item avatars, stat chips, heroes, monsters, interactable environment objects, door cards, intent tabs, turn marker, HP, synergy bridges | Walls, floors, props, panel chrome, ambience |
| Form | Closed, compact, convex | Long, linear, repeated |
| Outline | 1 px outline plus a 1 px key-side rim | Flat fill, no outline |
| Value steps | Three or more per material | One or two |
| Relation to its container | May break it (an item lifts when dragged, a boss overlaps its frame) | Always contained |
| Detail | Allowed if it helps silhouette or information | Forbidden beyond a single repeated module |

Gestalt principle: **closure** makes compact shapes read as objects. **Continuity** makes long repeated shapes read as scenery. The player's eye goes to closed shapes first without being told.

**Synergy bridges.** Where two adjacent items fire together, a short bridge crosses their shared seam and never covers the plates. **The bridge has three states (amended by Section 7.6):** *resting* in planning (Fog, 1 px, static), *preview* while a held item hovers a position or a plate has focus (Fog-hi, 2 px, no pulse), and *trigger* in combat (tier highlight with a 1 to 2 frame pulse).

### 3.8 Native Resolution Assumptions (not fixed here)

Section 8 owns the native resolution. These shape rules assume the following minimums, and Section 8 must meet them or revise Section 3:

- A 1x1 cell edge of 16 px or more (20-24 preferred).
- Characters at least 24 px tall for readability, bosses much larger.
- 1 px outlines and rims at native scale, and integer scaling only.
- Frame thickness of 2-3 px.

### 3.9 Alternatives Considered

| Alternative | Why rejected |
| ---- | ---- |
| Tier shown by a pip count (0 to 5 pips) on every item | Counting four or five pips on a 20 px cell is unreliable. Pips are kept for door cards and tooltips, where there is room. |
| Tier shown by frame thickness | Frame thickness is nearly invisible to colour-blind players at small sizes and costs lit area. |
| Rotating the icon with the plate | Breaks the single light direction and makes pixel art look wrong. |
| Dashed or dotted frames for a tier | Reads as "disabled" or "placement ghost". |
| Encoding hero position by pedestal size or silhouette (front, middle, back slots) | Rejected by the user's layout: heroes share one column and one pedestal size, and top-to-bottom order is only join order. |
| Decorative frames from reference 1 (bolts, vines) | Violates the Section 1 simplicity guard. |

### 3.10 Rule and Pillar Coverage

| Principle | Section 1 rule or principle | Pillar |
| ---- | ---- | ---- |
| Reserved vocabulary (3.2) | Matters means lit | P4 |
| Silhouette and one trait (3.3) | Principle 2, Form before hue | P4 |
| Wrong monsters (3.3.3) | Only things that matter emit light (monsters are ink until they act) | P2 (the player reads what they fight) |
| Plate and tier cues (3.4) | Principle 2 | P4, P3 |
| Curse tag (3.4.4) | Principle 2 | P5 |
| Environment (3.5) | Soft ink | P4, P5 |
| UI grammar (3.6) | Principle 1, Light is information | P4 |
| Lead versus supporting (3.7) | The "matters" test | P4 |

## 4. Color System

> **Verification status.** Every luminance, contrast and colour-blind figure in this section was **hand-computed** by the art-director and has **not** been checked with a contrast checker or a colour-vision-deficiency simulator. The hex values are proposals until swatches are viewed on a real monitor. Treat the palette as locked in structure (roles, rules, limits) and provisional in exact values. See 4.17.

### 4.1 Philosophy

Colour is spent in three tiers.

1. **Value first.** Most of the screen is a narrow ramp of ink. Brightness ranks importance.
2. **Saturation second.** Saturated colour (HSV saturation of 0.60 or more) is reserved for tier colours and the Red room's light. Everything else is capped below that.
3. **Hue last.** Hue is the least reliable channel (colour-blindness, monitors), so it only ever repeats information that a shape already carries (Section 3.4.4).

**Saturation budget.** Every non-tier colour is held at saturation 0.60 or lower. This is the rule that makes a saturated colour mean "tier". (Item icons are the one exemption: Section 4.5, item 5.) **Dark-value exemption (amended 2026-11-10, after `tools/art/palette.py` flagged INK-0):** the 0.60 cap applies only to colours with HSV value of 0.25 or more. HSV saturation is not meaningful that dark, and INK-0 `#0B0F1C` has saturation 0.607 at value 0.11, which is no more colourful to the eye than INK-1. The palette script enforces the cap at value 0.25 or more and prints the exempt colours as notes. The lit mask (4.10) is unaffected, because it needs value 0.55 or more.

### 4.2 Primary Palette

Seven base colours. The six tier ramps (4.4) are separate and are not counted here.

| # | Name | Hex | Role |
| ---- | ---- | ---- | ---- |
| 1 | **Ink** (INK-0 to INK-2) | `#0B0F1C`, `#151B2E`, `#222B44` | The world and UI field. INK-0 is the floor, INK-1 the default panel and backdrop, INK-2 the cell bed. |
| 2 | **Slate** (INK-3, INK-4) | `#36415F`, `#5A6785` | Structure: frames, lines, interactive boundaries. INK-4 is the brightest chrome allowed. |
| 3 | **Fog** (with Fog-hi) | `#8795B3`, `#AAB8D4` | Cold light: dungeon rim, secondary text, speed gauge, armour. Fog-hi is the intent colour. |
| 4 | **Bone** | `#E4DAC0` | Primary text and neutral light. Damage numerals. |
| 5 | **Candle** (with Candle-D) | `#EFC186`, `#7C5C3C` | The warm "you" light: hero rim, pedestal pools, selection, currency tint. Candle-D is the falloff. |
| 6 | **Blood** (with Blood-D) | `#B0505A`, `#4E2229` | HP only. Muted by design (conflict 1). Blood-D is the lost-HP trail; the bar track is INK-0 (4.6, amended by Section 7.4). |
| 7 | **Plum** (with Plum-E) | `#2A1830`, `#8A5C96` | The curse only. Dark and subtractive. Plum-E is the tag outline. |

Room accents (not base colours): **Moss** `#2E3820`, `#4A5E34` for ambient poison and damp; the **Wine** ramp and **Red-glow** for the Red room (4.8).

The master palette is 41 colours (ink, slate, Fog, Bone, the light ceiling, Candle, Blood, Plum, Moss, the Wine ramp, Red-glow and 18 tier colours) with 7 slots reserved, for a cap of 48.

### 4.3 The Ink Floor and the Light Ceiling

These are the testable "soft" limits promised in Section 1.1.

| Limit | Value | Rule |
| ---- | ---- | ---- |
| **Ink floor** | `#0B0F1C` (relative luminance about 0.005) | No pixel darker. Any pixel whose three channels are all 0x0A or below is a violation. Pure black is banned. |
| **Light ceiling** | `#F2E8CE` (relative luminance about 0.81) | No pixel brighter. Any pixel whose three channels are all 0xF6 or above is a violation. Pure white is banned. |
| **Text** | Bone `#E4DAC0` | Contrast on INK-2 is 10.08:1 (12.29:1 on INK-1), and Fog on INK-2 is 4.66:1 (5.68:1 on INK-1); tool-computed. Primary text must be 7:1 or better, secondary 4.5:1 or better. |
| **Chrome ceiling** | INK-4 `#5A6785` (luminance about 0.14) | No chrome (frame, line, panel) may be brighter than this. |
| **Interactive boundaries** | INK-4 or brighter | A control edge must be at least 3:1 against its field. **INK-4 meets this only on INK-0 and INK-1 fields** (tool-computed, WCAG relative luminance: 3.38:1 on INK-0, 3.02:1 on INK-1 (a margin of 0.02), 2.48:1 on INK-2). Buttons and ghost outlines must not rely on INK-4 against the INK-2 cell bed: use Fog there (Section 7.11). |

The ceiling `#F2E8CE` is reserved for the payoff bloom core. At rest, nothing is brighter than Bone.

### 4.4 Tier Palette

Each tier is a small value ramp.

- **Base:** the thin lit frame at rest, and pips.
- **Highlight:** the payoff glow and trigger pulse. It is brighter and slightly less saturated, as real light is, and it stays at or under the ceiling's luminance.
- **Edge:** the dark 1 px outer outline and shade side, which separates one plate from the next.

| Tier | Base | Highlight | Edge | Form backup (Section 3.4.4) | Card pips |
| ---- | ---- | ---- | ---- | ---- | ---- |
| White (Chalk) | `#C0C8D8` | `#DDE3EE` | `#4A5470` | Square plain frame | 1 |
| Green | `#3CB878` | `#8CF0B4` | `#14502F` | One chamfer | 2 |
| Blue | `#3E9BE8` | `#9CCBFF` | `#1A3F7A` | Two diagonal chamfers | 3 |
| Purple | `#9A5CEB` | `#CFA8FF` | `#43257A` | All corners chamfered | 4 |
| Gold | `#E6B422` | `#FFE58A` | `#7A5410` | Chamfers plus inner brackets, plus curse tag | 5 |
| Red | `#E5263F` | `#FF8A8F` | `#6E1020` | Chamfers plus serrated top edge, plus curse tag | 6 |

- **Saturation rule:** every tier base other than White has saturation of 0.60 or more. White is the intentional exception: it is the baseline, the absence of a lit hue.
- **Luminance order** (designed as a secondary cue, unverified): Gold (about 0.50), White (about 0.50, a tie broken by form), Green (about 0.37), Blue (about 0.30), Purple (about 0.21), Red (about 0.18). All tier bases keep at least 3:1 contrast against INK-2.
- **Tier name text** appears on tooltips and door cards in Bone. Colour is never the only carrier of the tier name.
- **Door cards** use the same ramps at larger scale, with the rank pips.

### 4.5 Importance Ladder and the Candidate Rule

**The candidate rule: "brightness marks importance, saturated hue marks tier."** It holds as a default, with five amendments, so the adopted wording is:

> Brightness ranks importance within the information layer. Saturation (0.60 or more) is reserved for tier and for the Red room's light, **with item icons the one exemption** (amendment 5). Tier is carried by hue and form together, and on item art by form first.

The five places the simple version breaks:

1. **White tier has no saturation.** It is identified by form and by being a cool neutral brighter than chrome.
2. **Payoff highlights get brighter and less saturated** (saturation about 0.35 to 0.50). The hue persists, but saturation is a rest-state property.
3. **Text is exempt from the ladder.** Bone text is bright but thin-stroked and small in area, and it needs 7:1 contrast.
4. **The Red room uses saturation to mark a room**, not a tier (small area only).
5. **Item icons** may use any colour of the master palette (user decision, Section 8.5.3), so hue on an icon does not mean tier. The tier is read from the plate's form first and from its thin lit edge second.

Attention ladder (what draws the eye first, with luminance bands):

| Rank | Element | Luminance band |
| ---- | ---- | ---- |
| 1 | Payoff highlights and the bloom | up to the ceiling (about 0.81), transient |
| 2 | Selection (Candle) and trigger pulses | about 0.4 to 0.6 |
| 3 | Tier base edges | about 0.18 to 0.50 |
| 4 | Intent (Fog-hi), speed gauge, HP | about 0.13 to 0.48 |
| 5 | Chrome (frames, lines) | 0.14 or less |
| 6 | Ink field | 0.03 or less |

Item avatars above the acting hero sit at rank 3 at rest (the same lit value as their grid plate) and rise to rank 2 only on pop-in (Section 5.13).

### 4.6 Semantic Colour Use

| Meaning | Colour and carrier | Notes |
| ---- | ---- | ---- |
| **HP** | Blood `#B0505A` on an **INK-0 track** (amended by Section 7.4: Blood on Blood-D was only about 2.6-2.8:1), INK-4 border, heart glyph, numeric label in Bone beside the bar. Blood-D `#4E2229` is the **lost-HP trail** (200 ms, then INK-0). | Muted on purpose (conflict 1). A bar, never a frame. Low HP: heart glyph beats and the bar gets a 1 px Bone outline (no hue change). Contrast figures are hand-computed, NOT VERIFIED. |
| **Damage dealt** | Bone numerals with INK-0 outline, rising from the target. Size scales with magnitude. | Not red. |
| **Damage taken** | Bone numerals with a Blood "minus", falling from the unit | Position and motion carry direction. |
| **Heal** | Bone numerals with a "plus", rising | |
| **Armour** | Fog `#8795B3` shield glyph | |
| **Speed gauge** | Fog fill, Bone ticks | |
| **Enemy intent** | Fog-hi `#AAB8D4` glyph on a downward pennant tab | Shape per intent type (attack, guard, buff, debuff). Low saturation. |
| **Buff / debuff** | Bone chevron on a plain plate, up for buff and down for debuff | Not hue-coded. |
| **Curse** | Plum `#2A1830` tag, Plum-E `#8A5C96` outline, Bone glyph | The only subtractive colour (conflict 4). |
| **Currency** | Candle `#EFC186` round coin with INK-0 edge, Bone numerals | Never framed, never saturated (conflict 2). A second currency (reference 2 shows a gem-like one) is not assigned yet: it needs a colour and a shape that avoid the Blue and Green tiers. |
| **"You" (warm light)** | Candle: hero rim, pedestal pools, selection outline, hover | Everything the player controls. |
| **Dungeon (cold light)** | Fog: wash at 10% alpha, rim on monsters and **interactable props only** (Section 6.3; supporting props have no rim) | Everything hostile or neutral. |
| **Turn marker** | Candle chevron under the acting hero; **Fog-hi chevron under the acting enemy** (keeps "warm means you", Section 7.4a) | |
| **Poison / damp** | Moss ramp, droplet glyph | Ink-class only (conflict 3). |
| **Disabled** | INK-3 text and icon | |

### 4.7 The Five Colour Conflicts: Decisions

**1. Red HP and damage versus the Red tier.**
- **Decision (user, 2026-11-10):** HP is a muted brick (Blood, saturation 0.55) and is always a horizontal bar with a heart glyph. Damage numerals are Bone, not red. The Red hue (saturation 0.83) is reserved for the Red tier and the Red room.
- **Why:** HP is on screen constantly, so a bright red HP bar would dull the Red tier. A rare, costly tier must own its hue.
- **Backup:** form (a bar versus a chamfered, toothed plate) and context (the middle column versus the grid).
- **Risk:** Blood and Red base have similar luminance (about 0.16 and 0.18). The test is a swatch check on the target monitor. **Fallback:** HP in Bone with a heart glyph, no hue.

**2. Decorative gold and currency versus the Gold tier.**
- **Decision:** No decorative gold. Trim and hardware are INK-3 or INK-4. Currency is a Candle round coin. The Gold tier hue is the only saturated yellow (saturation 0.80 or more) allowed on **plates, cards, chrome and effects**; item icons are exempt (Section 8.5.3), so a gold-coin icon may use a yellow, and the tier is then read from the plate's form.
- **Why:** if gold trim exists, the Gold tier stops meaning "Gold".
- **Backup:** currency is round and appears on chrome and prices. Gold is a plate with brackets. Selection is a separate Candle outline with a 1 px gap.

**3. Ambient poison green versus the Green tier.**
- **Decision:** Poison and damp use Moss: hue 80 to 100 (yellow-leaning), saturation 0.45 or lower, value 0.40 or lower. The Green tier is hue about 148 with saturation 0.60 or more and value 0.70 or more. Moss never appears on a plate or card and is never lit.
- **Why:** a dark yellow-olive and a bright emerald differ in hue, value and saturation at once.
- **Backup:** a droplet glyph on a Moss plate for the poison status.

**4. Curse or night purple versus the Purple tier.**
- **Decision:** Night is ink-blue, never purple. The curse is Plum: dark (value 0.35 or lower, luminance about 0.01), and always a tag shape. The Purple tier is bright (value 0.85 or more, luminance about 0.21).
- **Why:** a curse takes light away. The Purple tier adds it.
- **Backup:** the tag form (Section 3.4.4) and a Bone glyph. Plum-E outlines the tag so it stays legible on a dark cell.

**5. White UI text versus the White tier.**
- **Decision:** UI text is warm Bone (hue about 44). The White tier is cool Chalk (hue about 220). Text is thin glyph strokes; a tier is an enclosed frame. Item names are in Bone, not in a tier colour.
- **Why:** temperature and form separate them. Text is never drawn in a frame colour.

### 4.8 Colour Temperature by Area and the Red Room

**Scope decision (user, 2026-11-10):** "the scene" means the **middle column (heroes) and the right column (enemies, shop or event panel)**. World-layer washes and the Red-room shift are alpha overlays on those two columns only. The **left UI column** (selected hero's backpack and the shared stash) keeps the normal palette in every state, so tier colours stay consistent where the player arranges. **Sanctioned exceptions** are the whole-screen states that change the left column too: pause (the scene drops one INK step and desaturates, 2.3.1), victory (the ink lifts one step, 2.3.8) and defeat (tier edges dim to about 55% and non-tier colour desaturates, 2.3.9). Every other state leaves the left column untouched. The thresholds in 4.10 are applied to the composited frame.

| State / area | Wash | Alpha | Notes |
| ---- | ---- | ---- | ---- |
| Menus | Single Candle lantern pool, Fog elsewhere | 6% | |
| Planning | None. Candle halo on the selected hero only. | 0% | Neutral cool ink |
| Door choice | Tier colour under each card | 10-15% | Plum edge shadow on Gold and Red |
| Combat | Fog, top-left | 10% | Hero rim Candle, monster rim Fog |
| Payoff peak | Candle over the **right column** (both enemy lines and the margin) | up to 30% | Raises luminance by about 0.07 (hand estimate; re-measure now that each enemy line is 140 px wide), under the 0.10 flash threshold. Core up to the ceiling. |
| Shop | Candle | 14% | |
| Event | Fog, one lit object | 8% | |
| Recruit | Candle, centred pool | 18% | |
| Boss / Red room | Red-glow behind the boss, on a Wine field | 25-30% | See below |
| Victory | Candle, ink lifted one step | 22% | |
| Defeat | Fog, non-tier saturation about 60% | 8% | Pools off. Tier edges at about 55%. |

**The Red room palette shift.** "Red is a room" means every ink-class colour in the middle and right columns shifts, not an accent.

| Step | Normal ink | Red-room Wine |
| ---- | ---- | ---- |
| Floor | INK-0 `#0B0F1C` | W0 `#14080F` |
| Field | INK-1 `#151B2E` | W1 `#200F1A` |
| Raised | INK-2 `#222B44` | W2 `#311826` |
| Structure | INK-3 `#36415F` | W3 `#4B2638` |
| Chrome ceiling | INK-4 `#5A6785` | W4 `#75405A` |
| Key light | Fog wash | Red-glow `#B02A40` at 25-30% alpha |

- The Wine ramp is ink-class: low value and low saturation.
- Red base `#E5263F` appears only on small elements and tier plates, not as a large area.
- Saturated-red area is capped at 8% of the frame, and it never flashes (4.12).
- The protected layer keeps its normal palette, so a Red-tier item never disappears into a Red room.
- Boss rooms use the shift. **Open item:** whether Mythic rooms use the same shift or a distinct variant is not decided; until it is, Mythic rooms use the same shift.

### 4.9 UI Palette and Where It Diverges from the World

The UI uses the same ink, slate, Fog, Bone and Candle colours as the world, and tier colours mean the same thing in both. It diverges in exactly five places:

1. **No area washes on the left UI column.** It never takes a tint, so tier colours stay consistent. The frames of the middle and right columns take Wine (W3 and W4) in the Red room only.
2. **Chrome is capped at INK-4.** The world may use Fog and Bone for lit things, chrome may not.
3. **Interaction states** are UI-only. Hover is a Candle 1 px ring; **focus is a Candle 2 px ring drawn above neighbouring plates**; selection is a ring with a 1 px gap **plus a 4 x 4 Candle square at its top-left** (amended by Section 7.11). Primary actions use a Candle 2 px frame with Bone text, and a Candle fill only while pressed. Disabled is INK-3, **but never for text the player must read**: an unaffordable price uses Fog numerals plus a cross glyph (Section 7.10).
4. **Text colours** are Bone (primary), Fog (secondary) and INK-3 (disabled). The world has no text.
5. **Moss, Wine and Plum** appear in the UI only as the curse tag and plate (Plum) and in the Red room frame (Wine).

### 4.10 Lit-Area Budgets and the Squint Test (Section 1 open item 1)

**Finding: the 10% squint threshold is realistic only for the thin-edge frame rule, and as written it is the wrong test.**

Estimate: a left column of about 32% of the screen, a grid filling about 60% of it, about 60% of cells occupied, and a thin lit edge plus icon highlights covering about 25-35% of an occupied cell. That is about 3-4% of the frame for the grid. Add the stash, HP, intent, text and tier pulses and planning lands at about 7-10%. If plates are filled as in reference 2, the figure rises to 40% or more. So 10% is achievable, but only if frames stay thin.

**Measured (2026-11-10, `tools/art/mockup_planning.py`, greybox in the exact master palette).** Worst-case planning frame: 30 items in an 8 x 6 grid (91.7% occupied), 10 bridges, a focused plate, a stash, a compact hover popup open, three heroes, and five enemies in two lines. **Lit area 3.59% at C = 24 and 3.24% at C = 20** (budget 12%); **APL 0.038 and 0.035** (cap 0.08); saturated area 1.00% and 0.86%; 0 off-palette pixels. The hand estimate above (7 to 10%) was about three times too high. The text is a pattern at about 40% density and the mattering mask is true by construction, so these are lower bounds: real text could roughly double the lit area and still stay under 12%. A pinned expanded popup was not measured (7.16). Detail: `production/qa/evidence/mockup-planning-findings.md`.

"The brightest 10% of pixels must belong to things that matter" is a weak test, because in a dense frame the top 10% is trivially all items. The test that catches decoration is how many lit pixels are not information.

**Adopted method (replaces the 10% wording in Section 1, Principle 1):**

1. Build a **lit mask**: pixels in the composited frame with relative luminance of 0.30 or more, or with saturation of 0.60 or more and value of 0.55 or more.
2. Build a **mattering mask** by rendering only the item, protected-layer and tier layers (plates, HP, intent, turn marker, text, active synergy bridges, hero and recruit rims, pedestal pools, lit monster eyes, item avatars, effect rings, stat chips, threads and impacts).
3. **Precision check:** at least 90% of lit pixels must fall inside the mattering mask.
4. **Budget check:** lit area must be at or below the state's budget below.

| State | Lit-area budget | APL cap |
| ---- | ---- | ---- |
| Menus | 6% | 0.06 |
| Planning | 12% | 0.08 |
| Door choice | 12% | 0.07 |
| Combat | 8% | 0.07 |
| Payoff peak | 25%, 1.5 s or less | 0.20 at peak |
| Non-combat | 10% | 0.08 |
| Boss / Red room | 10% | 0.09 |
| Victory | 15% | 0.13 |
| Defeat | 5% | 0.05 |

These are **proposed numbers** and must be re-measured on the first full-frame mock-up. The method is what is locked, and the numbers are tuned once. Frames are checked with a script over exported screenshots, not by eye.

### 4.11 Colour-Blind Safety

Check by running every tier strip and the HUD through simulation (Machado or Brettel) for protanopia, deuteranopia and tritanopia, and in greyscale.

| Type | Pairs that fail on hue | What backs them up |
| ---- | ---- | ---- |
| Protanopia, Deuteranopia | Green and Red. Green and Gold. Blue and Purple (milder). Blood HP and Red tier (milder). | Plate form (1 cut versus all cuts plus teeth), luminance order, rank pips and tier text on cards, context (HP is a bar in the middle column) |
| Tritanopia | Blue and Green. Purple and Red. Gold and White. | Plate form, luminance order, rank pips and tier text |
| All | Candle versus Gold | Round coin versus plate, Candle selection drawn with a gap |
| All | Fog-hi intent versus White tier | A pennant tab versus an enclosed plate |
| All | An item icon whose hue matches its plate's tier hue (free palette, 8.5.3), for example a red icon on a Red plate | Plate form first (3.4.4), the tier name in words in the popup, and the optional tier badge (7.13). The icon spike (8.5.6) checks every tier with a same-hue icon; the first remedy if a tier stops reading is the 0.60 saturation cap on icon colour. |

- **Risk to verify:** Red base `#E5263F` is the weakest on dark under protanopia, where red darkens. If its contrast against INK-2 drops below 3:1, raise it to `#F04A5A`. The serrated top edge means the form cue still works either way.
- All the luminance figures in this section are hand-computed and need a tooling pass (4.17).

### 4.12 Photosensitivity: Proposed Flash Limit (Section 1 open item 3)

This is a **proposal for the accessibility requirements** (`design/accessibility-requirements.md`, which does not exist yet), not an adopted requirement. It follows WCAG 2.3.1 and the Harding and PEAT conventions, and it must be verified against the current standard before adoption.

For the largest payoff (Detonation) and for the Red room:

1. **No more than 2 flashes in any one second** (stricter than the 3-per-second general limit). One flash is a rise and fall in luminance of 10% or more of the maximum.
2. **No concurrent flash over a large area.** A single flashing region must not exceed about 25% of a 10-degree visual field, roughly 341 by 256 px at a 1024 by 768 reference. A full enemy line (or both lines) is larger than that, so the bloom is built from rings, particles, thread lines and number scale, not a full-column flash.
3. **Column washes are swells, not flashes.** The Candle wash ramps over 300 ms or more and changes the scene's relative luminance by no more than about 0.08 (the 30% wash in 4.8 gives about 0.07), which is under the 0.10 flash threshold.
4. **No saturated-red flashing.** Red-tier payoffs and the Red room never alternate to and from saturated red. The Red room's glow is a steady wash.
5. **Rises are eased** over at least 6 frames at 60 fps, with no single-frame full-intensity cuts.
6. **A "Reduced flashes" setting** replaces the bloom with a steady glow and removes particle bursts. Recommended on by default until the first player-testing pass.
7. **Verification:** the largest payoff is captured and run through a photosensitivity analyser before any build ships.
8. **Avatar chains (Section 5.13).** Avatars pop in over 6 frames or more, at a cadence of 260-400 ms in an area of about 24 x 24 px each. Rings grow steadily and never alternate to and from a colour. The "Reduced flashes" setting fades avatars in with no tier pulse and shows a ring at its final radius in 3 steps. A full avatar chain is part of the verification capture. At 2x and 4x the cadence can fall to the 120 ms floor (5.10.6), which is more than 3 pops per second; the rule then relies on each avatar's small area (about 24 x 24 px, far below the large-area threshold of item 2). This is a proposal and NOT VERIFIED; the verification capture must include 4x.

### 4.13 Pipeline Note for AI-Assisted Generation (LOCKED)

These rules are fixed so AI-generated art and hand clean-up converge on one look.

1. **One master palette.** The 41 colours of 4.2 to 4.8 (cap 48) are the only colours in the game. Every generated image is quantised to it (nearest colour in a perceptual space such as OKLab) before it is accepted. No off-palette pixel survives. Suggested file: a `.gpl` plus a swatch strip PNG; the path and name are decided in Section 8.
2. **Outline rule.** Every lead shape (Section 3.7) has a 1 px outer outline in INK-0 (heroes, recruits, NPCs, props), **INK-4 (enemies, which stand on the INK-0 void, Section 5.9)**, or the tier edge colour (item plates). The interior uses selective outlining, one ramp step darker than the local fill. Never pure black. No anti-aliasing, no sub-pixel offsets.
3. **One light direction: top-left.** Key light comes from the upper left on every sprite. Shadows fall to the lower right. Heroes get a 1 px Candle rim on the key side, monsters a 1 px Fog rim. Item icons are never rotated, so the direction cannot flip.
4. **Value steps.** Three steps per material for lead shapes, one or two for supporting shapes. No gradients, no dithering (except authorised VFX), and no texture noise.
5. **Rejection test for a generated sprite:** off-palette pixels, more than three steps on a supporting shape, a rim on the wrong side, a lit pixel on a thing that does not matter, or a form that breaks the vocabulary in 3.2.

### 4.14 Cost Is Visible (Pillar 5)

The colour rule: **a cost is drawn in the same plane as its benefit, adjacent to it, and it subtracts light.** The curse is a dark plum tag on the same plate or card as the tier, never a separate panel. It darkens the card's edge on that side. A player can read the benefit and the price in one glance.

**Decision (user, 2026-11-10):** this is added to Section 1 as Principle 4, "Cost is visible, and it subtracts". This section carries its colour rules. The curse tag does **not** appear on item avatars (Section 5.10.2): the avatar is a 1x1 plate and the tag would hang over the hero's head, so a curse shows through its effects instead.

### 4.15 Alternatives Considered

| Alternative | Why rejected |
| ---- | ---- |
| Red HP at full saturation | Dilutes the Red tier (conflict 1). Kept as a fallback only if the muted brick tests poorly. |
| A different hue family for each tier chosen for maximum distinction | The six hues are already the player's mental model from the references. The cost of changing them is greater than the gain. |
| Curse as a glowing purple | Collides with the Purple tier and adds light instead of taking it. |
| The Red room as a Red-tinted full screen | Hides Red-tier items and the protected layer. Only the middle and right columns shift. |
| Brightness-only ranking of importance | Ignores saturation, which is the cheapest channel for tier. The amended rule uses both. |

### 4.16 Open Items from Section 1

| Item | Status |
| ---- | ---- |
| 1. 10% squint threshold | Replaced by the precision-plus-budget method in 4.10, with numbers to be confirmed on the first mock-up |
| 2. Native resolution | Not decided here. Section 3.8 states the minimums Section 8 must meet |
| 3. Photosensitivity | Proposed in 4.12, for the accessibility requirements |
| 4. Colour conflicts | All five resolved in 4.7 |

### 4.17 Verification Status and Open Questions

- **VERIFIED by tool (2026-11-10, `tools/art/palette.py`, WCAG relative luminance):** the contrast ratios listed in `production/qa/evidence/mockup-planning-findings.md` section 1 (INK-4, Fog, Bone, Blood, Plum-E, Fog-hi pairs) all matched the hand figures to the second decimal, and the 41 colours are within the cap of 48. **Failed or borderline:** INK-4 on INK-2 is 2.48:1 (already excluded in 4.3); INK-4 on INK-1 passes by 0.02; Red base on INK-2 passes 3:1 by 0.13.
- **NOT VERIFIED:** colour-blind pair analyses are still hand-computed and need a colour-vision-deficiency simulator. Every other figure in this section that is not in the findings file is still hand-computed. Hex values are proposals until viewed on a real monitor (the swatch check is still open).
- **Open:** the figure-ground contract (5.9) is fixed at the edge by the enemy outline only; the heroes read mainly through the Candle rim and face patch. A stronger body step (INK-3 enemy body, a 2 px rim, or the interior highlight row) is not applied and stays a fallback.
- **NOT VERIFIED:** the photosensitivity thresholds in 4.12 against the current WCAG text.
- **Open:** whether Mythic rooms use a distinct variant of the Wine shift (4.8).
- **Open:** a second currency, if the game has one (4.6).
- **Open:** whether adjacency synergies are shown as a bridge across the shared seam (Section 3.7 assumes so); confirm against the item and synergy GDD.
- **Open:** the exact monster and hero rosters that Section 3 archetypes assume.

## 5. Character Design Direction

> **Status.** Approved by the user on 2026-11-10 (revision 2). It reflects the user's decision that **characters do not attack**: each firing item appears as an avatar above the acting hero's head and performs its own effect. Also decided by the user: stiff-and-snappy pose style; the figure-ground contract in 5.9 and its five refinements; monsters act through their intent glyph (5.10.8); the avatar is a 1x1 tier plate with a glyph (5.10.2); swings and turns use a stepped arc plus baked integer row shear (5.12). The "Delvers" reading (5.2.1) is a **proposal** about lore: the user has given none.

### 5.1 Scope and the Test

This section governs every figure on the stage: the party's heroes, recruit candidates, enemies (ordinary, elite, boss) and NPCs (shopkeeper, event figures). It also governs the **item avatars** that carry all action (5.10 to 5.13). Items in the grid are Section 3.4, the environment is Section 6, and native pixel sizes are Section 8.

Apply the Section 1 question: *does the player decide or read anything from this?*
- Heroes and enemies pass rule (b). Interactable NPCs pass rule (a).
- Item avatars pass rule (b) strongly. They are how the player reads what their arrangement did.
- Passing the test does not mean being lit all over. A character is a lead shape (3.7) with light rationed to a 1 px rim, a pedestal pool and, for monsters, eyes that light while acting.
- The action itself is information, so it lives on the avatars and not on the bodies.

**What changed.** Characters never lunge, cast or swing. A hero's body shows only four things: it is idle, it is **ready** (its turn), it is hurt, or it is down. An enemy's body shows idle, an **act pose**, hurt and death. Everything that moves toward a target is an avatar or an effect.

The layout goals still hold (one column, one pedestal size, no depth slots, position carries no meaning):
1. The party reads as **one body** and as **three individuals**.
2. Hero, recruit, enemy and NPC are told apart in one glance.
3. No visual or motion cue may imply a position, rank or priority among heroes.
4. Every rule is cheap for one person to produce with AI generation plus hand clean-up.

### 5.2 The Party: One Body, Three Individuals

#### 5.2.1 Archetype (proposal): the Delvers

The party is three **delvers**. They are plain working people who go down carrying one tool of their trade each, not chosen heroes. They have no heraldry, no armour finery, no capes and no glowing weapons.

Why this reading:
- **Pillar 5.** The power comes from the items and the doors, not from who they are. A plain hero makes the borrowed power and its cost the story.
- **Simplicity guard (Section 1.2).** Plain clothes and one tool is the cheapest design that still has character.
- **Section 6.** The dungeon is institutional and plain, so plain delvers belong to the same world.
- **Warmth.** They are the only warm things in a cold dungeon. That is the visual meaning of the Candle rim ("you").
- **The action model.** Delvers do not fight with their tools; the items do the work. The prop is a trade mark, not a weapon, which fits.

#### 5.2.2 What makes them one party

The party is bound by **shared construction**, not a shared costume.

| Shared element | Rule |
| ---- | ---- |
| Axis | Heroes stand plumb on a vertical axis (3.3.1). Never a lean. |
| Pedestal | One ellipse size, one Candle pool each (3.3.2). |
| Cloth ramp | Slate ramp only: INK-2 shade, INK-3 base, INK-4 highlight (5.9). |
| Face | A flat Candle-D patch with two INK-0 dot eyes. Warm skin is the "alive and yours" cue. |
| Rim | 1 px Candle on the key side (5.4). |
| Outline | 1 px INK-0, the same weight on every hero (4.13). |
| Idle clock | All heroes breathe on one shared clock (5.6). |

The Gestalt principles doing the work:
- **Similarity:** one rim, one face, one ramp.
- **Proximity and common region:** one column.
- **Common fate:** one idle clock, so the three move together.

**Rejected: a shared uniform** (a tabard, sash or cord on every hero). It costs detail on every sprite and competes with the prop for the "one trait" slot that identification depends on. Similarity of construction gives the same cohesion for free.

#### 5.2.3 What makes them three individuals

A hero is **a role family plus one identifying prop**.

**Role families** (ratios from 3.3.2):

| Role | Body width : height | Body width as a fraction of H | Read as |
| ---- | ---- | ---- | ---- |
| Tanker (block) | 1 : 1.1 | about 0.9 H | Wide, low, squared shoulders |
| Warrior (round) | 1 : 1.4 | about 0.7 H | Oval mass, balanced |
| Support (tall) | 1 : 2 | 0.5 H | Narrow vertical, tall prop above the head |

Here **H** is the body height excluding the prop. The three families have **equal H, within 10%**.

> **Refines 3.3.2 (approved).** Holding H equal makes the ratios differ in **width**, so the families read apart by mass and height never reads as rank. The Support is tallest only because of its prop, which extends about 0.25 H above the body. All heroes stand feet-aligned on the pedestal baseline.

**Mid-action envelope.** Because heroes no longer attack or cast, their body never leaves the rest envelope except for the 2 px recoil when hurt and the 1 px lift of the ready state. The role family can never be disguised by a pose.

**The prop is an identity cue only.** It does nothing in combat and has no lit variant.
- **Zones.** A prop sits in one of three zones: **Crown** (above the head), **Hand** (held), or **Back** (carried).
- **Protrusion.** It sticks out of the body envelope by at least 0.2 H, so it survives the half-size silhouette test (3.3.1).
- **Colour.** The Slate ramp plus Candle-D accents only. **No tier hue ever appears on a character.** Tier colour belongs to items and their avatars.
- **Height cap.** A Crown prop rises at most **0.25 H** above the body envelope. The avatar anchor (5.10.3) sits at 1.25 H plus 4 px, so a taller prop would collide with the avatar zone.
- **Uniqueness.** Two heroes in the same party must not share both role family and prop zone. The real test is the half-size silhouette test run on the pair.
- **Attached, never floating.** A prop is part of the body (worn, held or carried). **The only objects that float above a hero's head are item avatars**, and an avatar always sits on a tier plate. A prop never sits on a plate. This keeps "prop" and "item" from being confused.
- **Recruits** follow the same rule. The number of heroes the art can support is families times prop zones times distinct prop forms, with no new bodies.

**Reserved-shape audit for props and costume** (3.2's bans applied to characters):
- No downward pennants, swallowtails or flags above a head. They read as an enemy intent tab.
- No chamfered-corner shields or plates. They read as a tier plate. Shields are square-cornered or round-topped.
- No row of equal teeth: saw blades, crown points, comb edges. Crowns have three points of unequal height.
- No dark plates hanging from a lower edge. They read as the curse tag.
- No floating diamond or chevron badges (rank pip, turn marker, buff direction).
- No large circles or ellipses (round shields, discs, halos). They read as pedestal, coin, lantern or the effect ring (5.11).
- No crossed grids (chain-mail hatching, woven patterns). They read as the backpack lattice.
- **Lantern exception.** A lantern prop is allowed only as a **rectangular cage**. Its flame is Candle-D at rest, and it never throws a pool of its own. The pool belongs to the pedestal.

**Party size.** The column must look complete at one, two or three heroes. The empty pedestal outline and the join-order numeral on the pedestal (3.3.2) stand as written.

### 5.3 Telling Hero, Recruit, Enemy and NPC Apart

Four camps are separated on two independent axes: **value** (how light the body is) and **temperature** (the colour of the rim and face). Shape from the 3.2 vocabulary backs up both, so the read survives greyscale.

| | Warm: Candle rim, Candle-D face | Cold: Fog rim, no warm pixel anywhere |
| ---- | ---- | ---- |
| **Light body (INK-3 base)** | Hero, recruit candidate | NPC (shopkeeper, event figure) |
| **Dark body (INK-2 base)** | Unused (see below) | Enemy |

The unused quadrant (dark and warm) is where a **fallen hero** lands: the body drops a step and the rim dims to Candle-D (5.6). "Dimmed ally" then has a place in the system.

**Shape cues from the reserved vocabulary:**

| Camp | Ground contact | Reserved shape that marks it | What floats when it acts | Posture and silhouette |
| ---- | ---- | ---- | ---- | ---- |
| Hero, recruit | **Ellipse pedestal** with Candle pool | Ellipse (pedestal); Candle turn chevron when acting | **Item avatars on tier plates** above the head (5.10) | Plumb, closed, convex, one prop |
| Enemy | None: flat INK-0 contact shadow, or a visible gap if hovering | **Downward pennant tab** (intent) above it | **Its intent glyph, bare (Fog-hi, no plate)**, lifting off the tab (5.10.8) | Leaning or wrong by exactly one device (3.3.3); eyes are sockets, not dots |
| NPC | Furniture: **a rectangle** (counter, altar, well, plinth) hides the lower body | Rectangle (container) | Nothing (NPCs never act in combat) | Plumb or still, hooded or partly hidden, never wrong, no tab, no pedestal |

**The three-question read** sorts any figure in under a second:
1. Is it on an ellipse pedestal? Then it is the party or a recruit.
2. Does it carry an intent tab, or lean wrong? Then it is an enemy.
3. Neither, and it sits behind furniture? Then it is an NPC.

A fourth cue is free: **a plate means ally, a bare glyph means enemy.** Anything floating on a tier plate came from the party's backpacks. Anything floating as a cold bare glyph came from an enemy's tab.

**Rules:**
- **An enemy never wears an ally or NPC cue.** A deceiver (a mimic posing as an NPC) would be a deliberate rule break, approved case by case.
- **An NPC is the only camp with a cool face** (INK-4 patch with the same dot eyes).
- **No camp may encode position.** No cue (pedestal size, lean, height, lane line, motion amplitude, avatar anchor) may vary with a hero's or an enemy's place in its column. Enemies stand in one or two vertical lines; line 1 stands against the party column. Their order is spawn order only, with no front or back meaning, and acting order is given by the order strip and the turn marker (Section 7.4a).

### 5.4 Light on Characters: Rims, Eyes and Pools

One light direction applies: key light from the **top-left** (4.13). The rim is the 1 px lit edge on the top and left outer edges of the body and prop, sitting just **inside** the 1 px outline (INK-0 on heroes, recruits and NPCs; INK-4 on enemies, Section 5.9).

| Type | Ground / pool | Rim (1 px, key side) | Eyes | Action tell |
| ---- | ---- | ---- | ---- | ---- |
| Hero | Pedestal ellipse, Candle pool | Candle, body and prop, always on | INK-0 dots on a Candle-D face. Never lit. **Narrow while it is the acting hero.** | **The item avatars above its head (5.10)**, plus the Candle turn chevron under its feet |
| Recruit candidate | Same pedestal and pool shape; pool at 18% (2.3.6) | Candle | As hero | None until it joins |
| Ordinary enemy | None. Contact shadow only. | Fog, inside a 1 px INK-4 outline | INK-0 sockets at rest. **Fog-hi, 1-2 px, while this enemy is acting or targeted.** | Eyes light, and **its intent glyph lifts off the tab** (5.10.8) |
| Elite | As ordinary | Fog on the body and on the added element | As ordinary | As ordinary. The added element has no light of its own. |
| Boss | As ordinary. May overlap its frame (3.7). | Fog (INK-4 outline, W4 in the Red room). **Red-glow** in the Red room. | Every eye lights, 1-2 px each, Fog-hi | As ordinary |
| Shopkeeper, event figure | Furniture, no pedestal | Fog | INK-0 dots on an INK-4 face. Never lit. | None. Its interactable state is the Fog wash and rim (4.8). |

Notes:
- **The parallel tell.** The thing that is acting is marked at its tell: monsters by eye light and the lifted glyph, heroes by their avatars.
- **Eyes.** The monster eye-light rule (3.3.3) stands: sockets are dark at rest and light only while that monster is acting or targeted. "Targeted" is a state flag that swaps the eye pixels, not an animation frame. Heroes and NPCs have dot eyes with three states (5.5) and never emit light from the eyes.
- **Rim implementation.** Author the rim as its own colour index or mask so the engine can modulate it per state (defeat dims it to Candle-D). The mechanism goes to the technical-artist.
- **Pedestal pools** follow 4.8. This section adds nothing to them.

> **Refines 2.3.7 / 3.3.4 (approved).** In the Red room the boss rim is Red-glow. Everywhere else the monster rim is Fog.
>
> **Flag against 4.10 (resolved).** The Candle rim has a luminance of about 0.58 and Fog about 0.30 (on the 0.30 threshold, hand-computed, NOT VERIFIED). 4.10's mattering mask now lists hero and recruit rims, pedestal pools and lit monster eyes. **Monster and NPC Fog rims** are still borderline on the lit threshold, and are treated as part of the character they outline (a lead shape, so they count as mattering); re-measure on the first mock-up.

### 5.5 Pose and Expression Style

**Style: stiff and snappy** (approved). Few frames, hard cuts, small amplitude, held key poses. With the avatar model, most of the motion budget lives in shared effects, so the characters themselves can be very still.

Rules:
- **Whole-pixel movement only.** No sub-pixel offsets, no smear frames on bodies, no squash and stretch.
- **Heroes do not move toward anything.** Their only motions are the idle shift, the 1 px ready lift, the hurt recoil and the down collapse. The motion version of the no-position-encoding rule is therefore met by construction.
- **Monsters have one act pose,** held while their glyph performs. The pose is the monster's wrongness device pushed one step further (a leaning monster leans more; an oversized limb rises). There is no lunge.
- **Expression is eyes only.** Hero and NPC faces have three eye states: **open** (idle), **narrow** (ready, 1 px shorter) and **shut** (hurt, down). No mouths, except where a monster's wrongness device is a misplaced mouth.
- **No whole-sprite flash on a hit.** Damage is shown by a 2 px recoil, the shut-eyes state and the Bone damage numerals (4.6). This stays inside the flash limit referenced in 4.12.

### 5.6 Minimum Animation Set

Hero attack and cast frames do not exist. Monster attack and gesture frames are replaced by one act pose. Key frames per type; everything else is code.

| Type | Idle | Ready / act pose | Hurt | Down / death | Total key frames |
| ---- | ---- | ---- | ---- | ---- | ---- |
| Hero (per role family) | 2 | **0 new** (idle frame + narrow eyes + 1 px lift, by code) | 2 | 2 | **6** |
| Recruit candidate | 0 | 0 | 0 | 0 | **0** |
| Ordinary monster | 2 | 1 (act pose) | 1 | 2 | **6** |
| Elite | 0 new | 0 new | 0 new | 0 new | **0** |
| Boss | 2 | 2 (act poses) | 1 | 4 | **9** |
| Shopkeeper | 2 | none | none | none | **2 + 2 react** |
| Event figure | 2 | none | none | none | **2** |

**Frame definitions:**
- **Idle:** 2 frames, a 1 px body shift, cycle of **2 s or slower** (the 0.5 Hz ceiling in 2.2). Rim and eye pixels do not change between idle frames.
- **Ready (hero):** a state, not a drawn pose. When a hero's turn starts it holds idle frame 1 with narrow eyes and a 1 px lift for the whole avatar sequence, then returns to idle 150 ms after the last avatar exits. A hero with no items that fire does the ready state and passes (an empty turn reads as an empty turn).
- **Hurt:** hero 2 frames (recoil, brace); monster 1 frame plus a code shake.
- **Down (hero):** drop, down. The last frame is held, with a code shake on the first.
- **Death (monster):** stagger, slump with the eyes dark, then a code-driven sink into the ground 1 px per step. No dissolve and no dithering (4.13).
- **No walk cycle, anywhere.** A recruit joins by sliding (5.7).

**Timing and data.**
- Hold times live in data, not in sprites. Combat speed stretches or shrinks them and **never skips a key frame** (5.10.7). No key frame is shorter than 80 ms at 2x.
- **The party shares one idle clock** (common fate). Monsters each get a random phase offset.

**Props as overlays.** A prop is a static image with a per-frame anchor table (head, hand, back) for the six hero frames. A new recruit then costs one prop image and its anchors, and no new body frames. The technical-artist confirms the anchor approach.

**Fallen heroes (default, pending the hero GDD).** A fallen hero stays in the column on its pedestal in the held last frame, drops one step toward the dark-warm quadrant, dims its rim to Candle-D and loses its pedestal pool. Its turn is skipped. Join order and column length do not change.

**Naming.** Keep the project convention: `char_tank_idle_01.png`, `char_tank_hurt_02.png`, `char_tank_down_02.png`. Section 8 owns the final names.

**Swarm.** The "slightly off-scale" swarm member (3.3.3) is **redrawn**, never resized (non-integer scaling is banned, 3.8).

### 5.7 Introducing a Recruit

The recruit must be fully readable before the player decides (Pillar 4). No silhouette reveal.

1. **Arrival.** The recruit panel replaces the enemy lines (2.3.6): the candidate stands in line 1's niche, and its items and stats take the area to its right. The recruit stands in its alcove on the same ellipse pedestal as the party, idling, with the Candle rim on. A warm pool of about 18% sits under it, and the party's pools visibly reach toward it. Role family and prop are fully visible and no UI covers them.
2. **Accept.** The two pools merge (the one deliberate overlap of warm lights, 2.3.6). The recruit slides, by code, to the next **empty pedestal outline** in the party column over about 500 ms with an ease. It uses the idle sprite, so it needs no art. The outline fills and the pool comes on.
3. **Settle.** The new hero joins the shared idle clock at the next cycle, and the join-order numeral appears on its pedestal. Nothing pulses (small events stay quiet, Pillar 2). Its backpack appears in the left column (Section 7). **Its first avatars appear on its first turn**; until then it is just a hero standing there. A recruit with an empty backpack passes its turns (5.6).
4. **Decline.** The pools separate and the recruit fades over at least 250 ms (2.2, rule 5).

**Multiple candidates.** All stand on identical pedestals, and nothing in the staging ranks them.

### 5.8 Detail Budget and LOD

The camera is fixed, with integer scaling and one pixel size on screen. LOD here means the **authored detail budget**: what survives at game camera distance, in a stated order.

**Always survives** (a sprite that fails any of these is redrawn):
1. The outer silhouette, including the prop.
2. The one distinguishing trait and the one wrongness device (3.3.3).
3. The 1 px outline (INK-0, or INK-4 on enemies) and the key-side rim.
4. The eyes: dots or sockets, and their states.
5. Three value steps on the major masses (INK ramp, 5.9).

**Dropped, in this order, when something has to give:**
1. Surface texture and noise (already banned).
2. Folds, stitching and trim lines.
3. Secondary costume layers (belts, straps, buckles).
4. Facial features beyond the eyes.
5. Interior selective outlining (4.13, rule 2).
6. Shading on minor masses, from three steps to two. Never below two on a lead shape.

**Limits:**
- **No detail smaller than 2 x 2 px**, except the outline, the rim and the eyes.
- **Internal colour regions:** at most 6 per hero and 5 per monster, outline and rim excluded.
- **One pixel size on screen.** No 2x recruit portrait ("mixels").
- **Half-size test** (3.3.1): fill with solid ink at native scale, then at half size. The camp (5.3), the role family and the monster archetype must still be named correctly.

**The avatar glyph detail budget.** An avatar glyph is drawn at the 1x1 cell edge **C** (16 px minimum, 20-24 preferred). It follows the same drop order: texture, then trim, then secondary parts, then interior outlines. The glyph must still pass the outline-only test (Principle 2). That is also the test for every multi-cell item's extra avatar glyph (5.10.2).

**Inputs to Section 8** (native resolution is not fixed here):
- Smallest character (swarm member) about 24 px, which puts heroes at **H of about 32 px or more** and bosses at **64 px or more**.
- Each hero slot needs an **avatar zone** above the head (5.10.3). Slot pitch is roughly **C + 4 (avatar zone) + 1.25 H (body and prop) + 8 (pedestal) + 10 (HP row) + 12 (stat row, Section 7.4)**, about 94 px at C of 20 and 98 px at C of 24 (H of 32). Three slots want roughly 282-294 px of a 314 px stage, so **native height 360 is required** (amended by Section 7.4: the first estimate of about 82 px had no stat row, and 270 is no longer workable).
- Each enemy slot is **26 (intent tab zone) + body + 4 (act-pose headroom) + 4 (shadow and turn row) + 10 (HP row) + 12 (stat row)**, so a pitch of **body + 56**: 80 to 84 (small), 92 to 100 (medium), 108 to 116 (large), 152 to 171 (boss). At most three enemies per line, two lines, the sum of pitches at most about 300 per line (Section 7.4a).

### 5.9 Figure-Ground Contract with Section 6 (approved)

Section 3.3.4 requires the character body to differ from the backdrop directly behind it by **at least two steps** on the INK ramp. The contract is written in **ramp indices**, so the Red room swaps by index (INK-n becomes Wn, 4.8) and the contract holds in both palettes.

| Camp | Body ramp (shade / base / highlight) | Face | Rim | Outline | Must sit on |
| ---- | ---- | ---- | ---- | ---- | ---- |
| Hero, recruit | INK-2 / INK-3 / INK-4 | Candle-D | Candle | INK-0 | Wall plane L1 (INK-1) |
| NPC | INK-2 / INK-3 / INK-4 | INK-4 | Fog | INK-0 | L1 (INK-1); furniture overlapping it also INK-1 |
| Enemy | INK-1 / INK-2 / INK-3 | none (sockets) | Fog | **INK-4** | Void L0 (INK-0) |

- Enemies are the heroes' ramp shifted **one step darker**, with no warm pixel.
- The ramp steps are close by design. The mock-up measured **1.36:1** for enemy base on void and **1.69:1** for hero base on wall (tool-computed). The INK-0 outline is invisible against the INK-0 void, so **enemies carry a 1 px INK-4 outline on all sides (user decision)**, **3.38:1** against the void and **3.02:1** against the INK-1 wall (tool-computed; the wall margin over 3:1 is only 0.02, so the outline must never be dropped, thinned or animated away). In the planning mock-up the enemies read as clear silhouettes on both grounds; their interior stays at 1.36:1 and carries no information, so the silhouette depends entirely on the outline. INK-4 is below the 0.30 lit threshold, so it costs no lit area. Every enemy in every line stands against the same void, so one rule covers all of them. **Fallback:** a one-step interior highlight row under the rim on the key side.
- **Red-room boss exception.** The boss is a dark silhouette against a lit glow (2.3.7), a deliberate polarity inversion. The Red-glow rim separates it.

### 5.10 The Item Avatar System

#### 5.10.1 What it is

When a hero's turn starts, each item that fires appears **in firing order, one after another, above that hero's head** as an **avatar** and plays its effect on the targets. The avatar is the scene-side expression of the Tick step of the escalation ladder (5.10.9). It is a lead shape and information under rule (b): it tells the player what the arrangement did.

#### 5.10.2 Anatomy

| Part | Rule |
| ---- | ---- |
| Plate | The **1x1 plate of the item's tier** (3.4.4) at native cell size C, no scaling. Same chamfers, bracket line or serrations as in the grid. INK-2 cell bed, thin lit tier edge. **Zero new art.** |
| Glyph | 1x1 items: the item's own icon, unchanged. **Multi-cell items (1x2, 2x2, L): one extra compact glyph authored at 1x1 size.** It follows the same outline-only test. Rejected alternative: scaling a large icon down (non-integer scaling is banned). |
| Outline | 1 px tier-edge outline (plate rule). |
| Shadow | A 1 px INK-0 offset toward the lower right. It lifts the plate off the head and backdrop. |
| Light | Key light top-left, as authored. No rim of its own: the lit tier edge is the avatar's light. |
| Omitted | **Rank pips** (cards and tooltips only). **The curse tag** (it hangs below the plate and would cover the head; a curse shows through its own effects and through Section 4.14, not through the avatar). |

**How tier stays readable.** The cue lives in the plate silhouette (3.4.4), so it works in greyscale at the avatar's size exactly as in the grid. The weakest pair (Green versus Blue) is checked again at avatar size in the first mock-up. Hue repeats what the form says.

#### 5.10.3 Anchoring and the slot anatomy

- **One fixed anchor for every hero.** The avatar's centre sits on the pedestal's centre line, and its bottom edge sits at a fixed height above the pedestal baseline: the tallest hero-plus-prop (1.25 H) plus a 4 px gap. It does not follow the prop, so the anchor cannot vary by role or position.
- **Slot stack, top to bottom:** avatar zone (C + 4) / body and prop (1.25 H) / pedestal / HP row (Section 7). In a vertical column the avatar zone of slot 2 and slot 3 sits between the heroes, so **each slot must reserve it**. Nothing else may use that zone: no HP, no turn marker, no readouts, no ring centre except the avatar itself.
- **A second avatar** (the previous one, still finishing its effect) slides **one avatar width toward the enemy lines** and finishes there. Avatars never stack vertically, because vertical stacking would climb into the slot above.
- **Nothing else floats above a head.** The turn chevron is under the feet. Props are attached.

#### 5.10.4 Protected layer and draw order

Principle 3 holds: every freeze frame still shows every intent icon, HP value and the turn marker.

Draw order, back to front:
1. Environment L0 to L4 (Section 6).
2. Characters and pedestals.
3. Effects: rings, smears, trails, impacts, threads (Chain and above).
4. Avatars and readout chips.
5. **Protected layer:** HP, intent tabs, turn marker and the left grid column. Always on top.
6. The payoff bloom sits **beneath** the protected layer (2.3.5).

Placement rules: effects aim at the target's **body centre**, never at its tab, HP row or turn marker. Readout chips stay inside the target's **body band** (head-top to pedestal) and never enter the avatar zone or the HP row.

#### 5.10.5 The queue

- **Order** is firing order, one avatar per fired item.
- **Concurrency:** at most **2 avatars** per acting hero at once at 1x and 2x (the arriving one and the finishing one), 3 at 4x (5.10.7). Only the acting character has avatars, so no more than 3 on screen at any time.
- **Silent triggers** (a passive whose effect is invisible this turn) show **only** the grid plate pulse, with no avatar. This keeps the sequence short.
- **Repeats compress.** The second and later firings of the same item or archetype with the same targets within one turn play at about 60% duration. Chips to the same target and stat within 300 ms **aggregate** into one sum.
- **Progress** is read from the grid: plates pulse in firing order and the sweep is the progress indicator. No counter in the scene.

#### 5.10.6 Timing (1x speed)

| Phase | Time | Note |
| ---- | ---- | ---- |
| Ready | 150 ms | Eyes narrow, lift 1 px, before the first avatar |
| Pop-in | **100 ms** | The lit edge rises over 6 frames at 60 fps (4.12, rule 5). The grid plate pulses 1-2 frames at the start. |
| Effect | Pulse 160, Swing 240, Status 240, Throw 280, Detonate 320, Ring 360 | Per archetype (5.11) |
| Exit | 80 ms | The edge steps down two ramp steps, then removed |
| **Cadence** (start to start) | 260 ms Pulse, Swing, Status; 320 ms Throw, Detonate; 400 ms Ring | The next pop-in overlaps the previous effect's last 40% |

- A hero with 8 items runs about 2.5 to 3 s at 1x, including ready and tail.
- **Cap:** the whole queue is at most about **4 s at 1x** for any item count. Cadence is `min(base, 3500 / N)`, with a floor of 120 ms.

#### 5.10.7 Speed control

| Setting | What changes |
| ---- | ---- |
| 1x | As above |
| 2x | Holds and cadence halved; luminance rises keep their 100 ms floor. Key frames kept; none shorter than 80 ms. |
| 4x | Avatars appear in groups of up to 3 side by side, each group held 160 ms. Chips aggregate per target. |
| Skip | The turn resolves instantly. One aggregated chip per target per stat. The grid plates still sweep, one plate starting per frame, each easing over 6 frames (see the envelope rule below). |

- **Speed never removes a key frame, a chip or the Detonation level.** The Detonation peak plays at 1x on any setting except Skip (Pillar 2: the payoff is the product). This is a proposal for the UX designer.
- Speed is a data multiplier on hold times, not a different animation set.
- **Speed scales holds and cadence only, never rise and fall envelopes (amended by Section 7.5).** Every luminance rise, including the avatar pop-in, stays at least 100 ms (6 frames) at any speed, so 2x and 4x cannot break the 4.12 rule. At Skip each plate in the grid sweep still eases over 6 frames.

#### 5.10.8 Enemies: the intent glyph acts (approved)

Each enemy already shows a Fog-hi intent glyph on its pennant tab. When the enemy acts:
1. The enemy takes its **act pose** and its eyes light (Fog-hi).
2. The **glyph lifts off the tab** and performs the matching archetype. The **tab stays in place, and its glyph slot goes dark** for the duration.
3. The effect lands on the heroes: a hurt frame and Bone numerals falling from the unit (4.6).

The glyph is a **bare Fog-hi glyph with no plate**, and it travels from the tab along a straight stepped path to the target hero's **body centre**, across the gutter between the lines and the party column (from line 2 it first crosses line 1, over its bodies and behind its tabs and HP rows; diagonally when the target is higher or lower). It is drawn beneath the protected layer, so it needs no avatar zone above the enemy.

Mapping: attack becomes Swing or Throw (per monster data); guard becomes Ring on itself; buff becomes Ring (area or point); debuff becomes Ring (point) on the hero.

The "what you saw is what happens" continuity (the glyph you read before the fight is the glyph that acts) is a direct Pillar 4 gain.

#### 5.10.9 Relation to the escalation ladder (2.4)

The ladder's levels survive; each is expressed on the avatars.

| Level | Trigger size | Grid | Scene |
| ---- | ---- | ---- | ---- |
| Tick | Single trigger | Plate edge pulse 1-2 frames, then an **echo** (held one step brighter) for the avatar's lifetime | **Quiet avatar:** tier edge at its rest value, 1 px ring, small chip, no particles |
| Chain | Several triggers | Light threads run from each source plate to **its avatar** (short), number size steps up | Avatar edge +1 step, ring 2 px, chips +1 size |
| Surge | Large chain | Candle wash over the right column | Particles on impacts; rings stay 2 px |
| Detonation | Run-defining chain | Full bloom | The sequence accelerates ("build"), and the last up-to-3 avatars form a fan and release together into the bloom |

- **Cause link (Pillar 2, "the clearer cause wins").** The avatar and its source plate share form, icon and pulse timing, and the plate echo holds while the avatar lives. The player can find the cause in the grid at a glance.
- Threads now end at the avatar instead of crossing to the target (the avatar relays). This is a refinement of 2.3.5.

### 5.11 Effect Archetypes and Readouts

With 60-80 items, cost is per **archetype**, not per item.

| Archetype | Used by | Avatar motion | Shared art | Duration |
| ---- | ---- | ---- | ---- | ---- |
| **Pulse** | Self buffs, armour on self, gold, passives | Bobs 2 px up and down (stepped) | None (readout only) | 160 ms |
| **Swing** | Melee damage | Stepped arc about 20 px toward the target, with a lean (5.12) | Smear crescent (3 frames), impact spark (2) | 240 ms |
| **Throw** | Ranged damage | Leaves the head and hops 4 to 8 whole-pixel steps to the target (about one hop per 48 px of path), ending there | Trail (2), impact spark (shared) | 280 ms |
| **Ring** | Buff, debuff, heal, shield (area or point) | Pulses once; the ring grows from it (area) or a thread runs to the target and the ring blooms there (point) | Ring is **code-drawn**, no art | 360 ms |
| **Status** | Apply poison, burn and so on | Tips toward the target; a mote travels 3 to 6 hops | Mote (shared) | 240 ms |
| **Detonate** | Consume stacks for a burst | Pulses; the target's status chips pulse | Burst (4 frames) | 320 ms |

A new archetype needs the user's approval. An item that fits none of the six does not get a custom effect by default.

**Authored per item:** the icon (already exists), the 1x1 avatar glyph for multi-cell items, and **one data row**: archetype, target rule, stat glyph id, readout value, lean on or off. **Optional:** one hand-drawn "struck" pose for at most about 10% of items (signature items).

**Shared effect art in total:** about 25 frames plus about 25 to 30 small glyph drawings (the count depends on the roster; Section 8.7). For example `vfx_swing_smear_01.png`, `vfx_impact_spark_01.png`. It does not grow with the item count.

**Ring behaviour.**
- Outline only, never filled. Integer radii, stepped growth in about 6 steps over 360 ms.
- Area mode is centred on the avatar and grows until it has touched every ally target (allies in the column above and below). Each ally's chip appears at contact, so readouts arrive in order of distance.
- It is drawn **beneath** the protected layer, so it passes behind HP rows and tabs.
- At most 1 ring per avatar and 2 on screen.

**Readout: the stat chip, drawn without hue.**
- **Anatomy:** a plain square-cornered chip (INK-1 bed, INK-4 frame, 3.6) holding a **stat glyph**, a **chevron** and a **signed number**. Everything in the chip is Bone.
- **Direction by three redundant non-hue cues:** the chevron points up for a buff and down for a debuff (this is the 3.2 and 4.6 vocabulary; the "up-arrow" in the user's description is the chevron), the chip **rises** 6 px for a buff and **sinks** 6 px for a debuff, and the number carries its sign.
- **Other readouts** follow 4.6: damage and heal are Bone numerals (heal with a plus, rising); armour uses the Fog shield glyph; **status uses a Bone droplet glyph with a stack number** (Moss stays ink-class and is never on a chip, 4.7).
- **Timing:** appears at contact, holds about 350 ms, steps out. Maximum 2 chips per target at once.
- **Possible confusion to test:** the Candle turn chevron (static, under the feet) versus the Bone buff chevron (on a chip, moving). Colour, container and motion separate them.

### 5.12 Stepped Motion Without Rotation

The avatar must "swing" and "turn a little". Section 3.4.3 says icons stay upright, and the asset standards (Section 8) allow only 90-degree rotation. Non-90-degree rotation of pixel art resamples pixels and breaks the palette and the outline.

**Technique (approved): three layers of stepped motion, none of them rotation.**
1. **Stepped arc by position offset.** The avatar moves in whole pixels along an arc, with 4-6 positions and hard cuts between them. This carries most of the "swing".
2. **Lean by integer row shear.** Rows above the icon's pivot shift 1 px sideways and the top quarter shifts 2 px, mirrored for the other direction. The look is a lean of about 10 degrees. It is **not resampled**: every pixel stays on the grid and keeps its palette colour. The lean frames (centre, left, right) are **baked at import** by a pipeline script, so the engine never resamples at runtime. This gives the flask its small wobble and the sword its lean into the swing. Icons where shear looks wrong simply skip the lean.
3. **Shared smear.** The Swing archetype's crescent and the impact spark carry the rest of the swing.

**Light direction.** Row shear does not change which side is lit, so the single top-left light direction holds. A 90-degree turn would flip it, so **no 90-degree turns are used for swings or wobbles**. (90-degree steps remain for plate rotation in the grid, unchanged.)

**Amendment to 3.4.3 (approved).** *Icons stay upright in the grid and on plates. As a combat avatar only, an icon may translate on a stepped arc and may lean by integer row shear (at most 2 px at the top row), baked at import. No non-90-degree rotation, no resampling.*

**Line needed in Section 8:** integer row shear is a pixel-integer transform, not a rotation, and is allowed for avatar lean.

**Fallback** if shear is rejected later: arc plus smear only. The flask's "blowing" becomes a 1 px side-to-side wobble by position.

### 5.13 Light, Colour and Budgets for Avatars

**Where the avatar's light comes from.** The avatar's light is its **tier edge**, not Candle. Candle is the warm "you" light for the hero's body. The avatar is the item and carries the item's tier. The tier edge stays at the same lit value as its grid plate at rest (ladder rank 3), and pulses to rank 2 only on pop-in.

**The ring's colour (proposal, adopted with this section).**
- The item's **tier highlight at reduced saturation (about 0.35 to 0.50)**, as 4.5 amendment 2 sets for payoff highlights. White tier rings are Chalk.
- 1 px at Tick and 2 px at Chain and above.
- It **steps down the ramp** as it grows (highlight, base, INK-4). No alpha blending and no dithering.
- **Enemy effects** use Fog-hi (cold), so the camp of the effect reads by temperature too.
- The ring does not say buff or debuff. The chevron does (5.11).

**Soft-ink rule.** The only light added to the scene is a thin tier edge, a thin ring, small chips and short impacts, all information. The world stays ink.

**Lit-area estimate (hand-computed at the native 640 x 360 frame, which is 230,400 px; NOT VERIFIED).** One avatar edge plus glyph highlights is about 120 lit pixels (about 0.05%), a 1-2 px ring of radius 30 is about 190-380 pixels (about 0.08-0.16%), and a chip is about 100 lit pixels (about 0.04%). A Tick is about 0.1%, and a Chain with 2 avatars, a ring and 2 chips is about 0.4%, well inside the Combat budget of 8% (4.10). The Planning and Payoff budgets are unaffected. These figures need re-measuring on the first mock-up.

**Photosensitivity (4.12).**
- Avatar pop-in rises over 6 frames, never as a single-frame cut (rule 5).
- Rings grow steadily and never alternate to and from a colour; the Red-tier ring uses the reduced-saturation highlight, not saturated red (rule 4).
- A chain of avatars pops at a cadence of 260-400 ms in a **small area** (about 24 x 24 px each, far under the 25% of a 10-degree field in rule 2). The chain is still included in the verification capture (rule 7).
- **"Reduced flashes" setting:** avatars fade in over 6 frames with no tier-pulse, and the ring appears at final radius in 3 steps without growth sparkle.

### 5.14 Alternatives Considered

| Alternative | Why rejected |
| ---- | ---- |
| Hero attack and cast animations | Removed by the user's decision. Also cost 13 frames per family and tied action to the body. |
| A custom effect animation per item | Unaffordable at 60-80 items. Six archetypes replace it (5.11). |
| Avatar as a bare icon with no plate | Loses the form-based tier cue (Principle 2) and the link to the grid plate |
| Avatar as an outline-only ghost plate | Icon values blend into the dark backdrop without the INK-2 bed |
| Scaling large icons down to avatar size | Non-integer scaling is banned (3.8). A per-item compact glyph costs one image. |
| Stacking avatars vertically | Climbs into the slot above and covers its HP row |
| Hand-drawing 2-3 tilt poses per item | Cost scales with the item count |
| 90-degree turns for swings | Too large a move, and it flips the light direction |
| Ring in Bone for every item | Loses the tier link in a chain and flattens the ladder; tier highlight at reduced saturation keeps both |
| Monster body lunges and gestures | Two action languages on one screen; costs 4 more frames per monster; breaks the "what you saw is what happens" continuity with the intent tab |
| A prop that lights up when the hero acts | Redundant with the avatars; removed |
| Enemies lit warm to look dangerous | Warm means "you" (Section 4) |

### 5.15 Rule and Pillar Coverage

| Rule in this section | Section 1 rule or principle | Pillar |
| ---- | ---- | ---- |
| Camps by shape and value, 5.3 | Principle 2, Form before hue | P4 |
| Rim, eyes and action tells, 5.4 | Only things that matter emit light | P4, P2 |
| Stiff and snappy, still heroes, 5.5 | Quiet baseline for the payoff | P2 |
| Minimum animation set, 5.6 | Simplicity guard | P4 (scope) |
| Avatar anatomy, anchor and protected layer, 5.10 | Principle 1, Principle 3 | P4, P2 |
| Queue, timing and speed, 5.10 | Principle 3 | P2 |
| Effect archetypes and chips, 5.11 | Principle 2 (chevron and motion, not hue) | P4, P2 |
| Stepped motion, 5.12 | Simplicity guard | P2 (swing reads) |
| Light and budgets, 5.13 | Soft ink, Principle 1 | P2 |
| Plain delvers, 5.2.1 | The world is soft ink | P5 |

### 5.16 Amendments to Locked Sections and Open Items

**Approved with this section (applied to Sections 2, 3, 4 and 6 in the same pass):**
1. **3.3.2:** equal H, widths follow the ratios.
2. **2.3.7 / 3.3.4:** boss rim is Red-glow in the Red room.
3. **4.10:** the mattering mask includes hero and recruit rims, pedestal pools, lit monster eyes, avatars, rings, chips, threads and impacts.
4. **3.2 / 3.3.2:** a lantern prop is allowed only as a rectangular cage.
5. **3.3.1:** keep the INK-0-on-INK-2 silhouette test and add a second run at the contract values.
6. **3.2:** add *"Expanding ring: transient effect, outline only, never filled, never static, lives under 600 ms; meaning: area of effect."*
7. **3.2 / 4.6:** the buff-and-debuff chip (plain plate, Bone chevron) appears in the scene, and an item avatar (a tier plate) floats over the heroes. Both are stated in the vocabulary.
8. **3.4.2:** every multi-cell item also supplies a 1x1 avatar glyph.
9. **3.4.3:** the avatar exception in 5.12.
10. **2.3.4, 2.3.5, 2.4, 2.5:** see the impact list below.

**Open items:**
- Roster and role families (three body families supported). Whether a fallen hero stays (5.6). Full-party recruit behaviour. First-hero selection.
- ~~Enemy layout in the right column.~~ **Closed:** one or two vertical lines of at most three enemies each, 140 px wide, line 1 against the party column (Section 7.4a). Open: the swarm and boss-companion rules, and whether the combat GDD gives a line a meaning.
- Item glyph, stat glyph and status glyph sets (counts depend on the item and stat roster).
- Whether the left grid follows the acting hero during combat (Section 7).
- Prop overlay and rim-mask implementation, and the baked-shear pipeline script: technical-artist.
- Native resolution and cell size C: Section 8.
- The photosensitivity limits in 4.12 apply to avatar chains and are NOT VERIFIED.

**Impact on other sections:**
- **Section 2:** 2.3.4 Combat (triggers are no longer only 1-2 frame edge pulses; the quiet avatar replaces them); 2.3.5 Payoff (threads end at avatars; Detonation uses the fan release); 2.4 (duration of a beat becomes about 300-400 ms per item with a 1-2 frame plate pulse; who emits light and motion rows add the avatars); 2.5 (the Combat cue gains "one floating plate above the acting hero").
- **Section 3:** 3.2, 3.3.2, 3.3.3, 3.4.2, 3.4.3, 3.7 as listed above (avatars and chips are lead shapes).
- **Section 4:** 4.5 (avatar edges at ladder ranks 3 and 2), 4.10, 4.12 (avatar cadence and ring rules; extend Reduced flashes), 4.14 (the curse tag does not reach the avatar).
- **Section 6:** an effects layer between characters and the protected layer; character zones include the avatar zone above each hero and the glyph path from enemy tabs across the gutter; the top-band environment detail clears the first slot's avatar zone under the lintel.
- **Sections 7 and 8:** reconciled when those sections are drafted (slot stack, speed control, stat chip and glyph sets, native height about 360, integer row shear allowed, asset counts).

## 6. Environment Design Language

> **Status.** Approved by the user on 2026-11-10 with the "Keepers' vault" reading in 6.2 chosen. That reading is a **proposal about lore**: the user has said nothing else about the world. It is written so it can be replaced without touching any rule from 6.3 onward. Cross-references to Section 5 use its topics; Section 5 was revised (the item-avatar system) before this section was written and its numbering (5.1 to 5.16) is now stable.

### 6.1 Scope and the Test

This section governs everything that is not a character, an item or UI: walls, floors, arches, piers, furniture, props and backdrops, across ordinary rooms, shop, event, recruit, boss and Red rooms, and menus.

Apply the Section 1 question to every environment element: *does the player decide or read anything from this?* Mostly the answer is no, so the environment is **ink**: dimmer, lower in saturation, fewer value steps. The exceptions are the few **interactable objects** (an altar, a well, wares on a counter). The player can act on them, so under rule (a) they are lead shapes and may be lit.

The binding constraint comes from Section 5. **A character silhouette must read against the texture density this section defines.** That is why 6.3 defines texture as a measurable density and why 6.6 fixes which layers may sit behind which column.

### 6.2 Architectural Style and the World It Implies (PROPOSAL, chosen)

**The reading: the Vault.** The dungeon is a strongroom built by **keepers** to hold something dangerous and to put a price on it. The keepers are gone. What remains is their plain, heavy architecture, their counting marks, and the effects of the thing they kept. The heroes are delvers who borrow from it. It names no culture, script, deity or heraldry.

Why this reading:
- **It is Pillar 5 in architecture.** A place built to weigh and price power explains why all power here has a cost. Shops and events become the keepers' counting rooms and bargaining places.
- **It matches Section 3.5.** Rectilinear, thick, flat and quiet is how a strongroom is built.
- **Plainness is in character.** Institutions do not ornament, so the simplicity guard is satisfied by the setting instead of fighting it.
- **It is flexible.** Nothing visual needs a name. A tomb, a mine or a prison would all fit under the same rules.

**The grammar of the Vault:**

| Element | Rule |
| ---- | ---- |
| Wall | Thick, large blocks in running bond (3.5). Never a square lattice. |
| Opening | A round (semicircular) arch or a flat lintel. **Never a pointed arch**, whose apex reads as a chevron. |
| Support | Square-section piers. No fluting, no chamfered capitals, no octagonal columns. |
| Floor | Flat slabs with joints one step apart. No checkerboard, no diamond pattern, no herringbone. |
| Door | A single slab. No inset panels (a nested frame reads as the Gold bracket). |
| Wood | Counters and shelves: long horizontal planks. |
| Metal | Never visible as bright metal. Hardware is ink. |
| Marks | Tally strokes, chisel cuts, wear and scorch. No text, no alphabet, no emblem. |

**Three conditions of the same stone:**
- **Kept:** neat, tended, joints clean. Used for the shop and the recruit alcove (someone tends these places).
- **Worn:** the default. Joints soft, a few blocks out of true. Ordinary rooms.
- **Breached:** the arch is broken, a diagonal intrudes and a serrated edge appears on the frame. The boss and Red room (3.5).

An optional tie, not required: the shopkeeper may be the last keeper, and the Warden monster archetype (3.3.3) may be what the keepers left to guard the place.

**If the lore changes,** only this reading and the story table in 6.5 change. The grammar, the texture classes, the density caps, the layering and the shape bans do not.

**Alternatives for the reading:**

| Reading | Verdict |
| ---- | ---- |
| **Keepers' vault** | **Chosen.** Ties directly to Pillar 5 and to the existing geometry. |
| Abandoned mine or quarry | Plain and cheap, but "cursed" has no architectural logic |
| Gothic crypt or cathedral | Instantly "dark fantasy", but ornament-heavy, with pointed arches and tracery that collide with the chevron and lattice reservations |
| Prison or oubliette | Bars and cells are square lattices (reserved for the backpack grid) |
| No stated culture (anonymous stone) | The fallback. Cheapest, but leaves storytelling without a vocabulary. Adopt it if the lore is dropped: keep 6.3 to 6.7 and drop the keeper marks from 6.5. |

### 6.3 Texture Philosophy: Flat Stylised Pixel

**Decision: flat, stylised pixel, one or two value steps per material, one repeating module, no texture noise.**

Why:
1. **Texture is unread information.** The Section 1 rule says that if the player reads nothing from a detail, it moves toward ink. Texture in a backdrop is the clearest case.
2. **Figure-ground.** A character's strongest edges must be the strongest edges in the frame. Fine environmental texture creates competing edges right where the characters stand.
3. **Eyes.** A 30-60 minute session of studying grids is easier on the eyes over a calm field (Section 2.2).
4. **Pipeline.** Flat tilable modules quantise to the master palette consistently (4.13). Painted or noisy scenes drift between AI generations.
5. **Solo production.** A room is built from modules, not painted as a scene.

**Materials are told apart by pattern, not by colour.** Everything is ink, so stone is running bond, wood is long horizontal plank lines, cloth is vertical folds at least 2 px wide, earth is flat. This is "form before hue" (Principle 2) applied to the environment.

**Density classes.** Value steps are ramp indices (the Red room swaps by index).

| Class | Where | Value steps | Strongest edge | Feature size |
| ---- | ---- | ---- | ---- | ---- |
| **D0 Void** | The arch opening, deep recesses | INK-0 only | None | n/a |
| **D1 Plane** | Wall behind heroes, floor, panel backdrops | INK-1 face, INK-0 joint | **1 step** | Module at least 8 x 4 px (assuming a hero body height H of 32 px; scale with H) |
| **D2 Structure and decor** | Piers, arch ring, lintel, counter body, alcove, decor props | INK-0 to INK-2, flat, no outline, no rim | Up to 2 steps against D0 or D1 | Masses at least half an H wide |
| **D3 Interactable** | Altar, well, wares, the event object | 3 or more steps, outline, rim | Any | Any (a lead shape under 3.7) |

**No environment pixel is brighter than INK-2** in the stage bands (luminance about 0.025, under the 0.03 ink-field ceiling). Decor never has a rim or an outline.

> **Refines 4.6 and 3.7 (approved).** Section 4.6 says Fog rims "monsters and props". Read that as **interactable props only** (D3). Supporting props are flat fills with no rim (3.7). Section 3.7's list of lead shapes does not name interactable environment objects. Add them, since rule (a) of Section 1.1 already says they matter.

**Character zones.** The hero strip and each enemy line are vertical bands of the stage. Each is the slot width (120 px) plus a clearance of **0.25 H on each side** (8 px): 136 px. Line 1 sits against the party strip and line 2 against line 1, so the gutters are about 16 px of clearance (party to line 1) and a 4 px gap holding the pier (line 1 to line 2). The gutters, the **intent tab zone** above each enemy (26 px, Section 7.4a), the **avatar zone** above each hero (C + 4 px, Section 5.10.3) and the **straight paths** that avatars and intent glyphs travel to a target's body centre (diagonal, and from line 2 crossing line 1) are zones as well. The margin to the right of the second bay is D0 or D1 only.
- In a zone the environment is **D0 or D1 only**: no props, no structure, no strong edges.
- Environmental detail therefore lives in the **top band** (lintel, arch crown, which must clear the first slot's avatar zone), the **outer margins** (piers) and the foreground edge.
- The art is mostly frame, and that is the point.
- **One exception: the arch crown.** The round-headed niche's crown may enter line 1's top corners, in the top 53 px, where the first slot holds only its 40 px tab. It never touches a tab, a body or an HP block. The first mock-up checks that the top 44 px of the first enemy's silhouette is at most about 100 px wide.

**Test (the edge-dominance check).** Convert an exported frame to greyscale. A strong edge is a neighbouring-pixel step of 2 or more ramp indices. Inside the character zones, at least 95% of strong-edge pixels must belong to lead shapes. The 95% is a proposal and is re-measured on the first mock-up.

**Alternatives considered for texture:**

| Alternative | Why rejected |
| ---- | ---- |
| Painterly or textured backdrops | Noise competes with silhouettes and drifts between AI generations |
| Dithered gradients and vignettes | Banned outside authorised VFX (4.13). The vignette is done in stepped values instead. |
| Parallax layers | Moving layers cost the two-element ambient motion limit (2.2) for little information |
| Per-area bespoke backdrops | Not affordable for one person. Modules and palette-index swaps are. |

### 6.4 Prop Density Rules per Area Type

A prop earns its place by doing one of two things: framing the stage, or telling one story beat (6.5). Otherwise it is cut. Decor is counted outside the character zones.

| Area | Decor instances | Interactable (D3) | Negative space | Notes |
| ---- | ---- | ---- | ---- | ---- |
| Ordinary room | 0-3, from at most 2 types | None | 70% or more of the scene is D0/D1 | Piers and arch are structure, not counted. Worn condition. |
| Shop | 0-3 | Wares (count is gameplay's). The shopkeeper. | 50% or more | Long counter and horizontal shelf rows (3.5). Kept condition. Furniture that overlaps the NPC uses INK-1 face and INK-0 joint, so the NPC keeps its two-step separation. |
| Event | 0-2 supporting | **Exactly one** central object | 60% or more | A single tall object with a lot of empty space (3.5) |
| Recruit | 0-2 | The recruit, in its alcove | 60% or more | The alcove is an **arch-shaped recess**, never a ring or ellipse (3.5, 6.7, 6.8). Kept condition. |
| Boss / Red room | 0-2 (rubble, debris) | The boss | 60% or more | Monumental, not cluttered. Debris obeys the Wine ink-class limits and the 8% saturated-red cap (4.8). |
| Menus | 0 | None | 60% or more | A wide arch and one Candle lantern, plenty of empty space |

**Global rules:**
- **One-of rule.** A scene never repeats the same decor prop more than twice.
- **No prop in a character zone, and none overlapping a character**, with one exception: furniture in front of an NPC (see the shop row).
- **Prop library cap (a proposal for the producer).** About 16 decor props plus the structure kits for the whole game, so the environment budget is known up front.

### 6.5 Environmental Storytelling Without Text

1. **One fact per room.** A room tells one thing. Two facts compete, the way two wrongness devices make a different creature (3.3.3).
2. **Tell with absence and damage before objects.** An empty plinth, a sealed lintel, a cut chain, a breach. What is missing reads more strongly than what is added, and costs less to draw.
3. **Story reads in silhouette.** A story prop must pass the half-size silhouette test. If it needs detail to be read, it is cut.
4. **Story props are ink and never light.** If a prop needs light to be understood, it either becomes an interactable (D3) or is removed.
5. **No text and no alphabets.** Marks only (tally strokes, chisel cuts, scorch) at one step of contrast. Text is the UI's job (Bone, Section 7).
6. **One mark vocabulary, repeated.** The player learns the keepers' marks, and an unfamiliar object then means something is wrong, the way monsters do.
7. **Cost shows as damage to architecture, not as added ornament.** This follows Principle 4 and 3.5. More cost means more breach.
8. **No storytelling in the character zones.**
9. **Stable across runs.** Which fact a room tells may vary; what a mark means does not.

**Story table** (replaceable with the lore):

| Area | The one fact | How it is shown |
| ---- | ---- | ---- |
| Ordinary | Someone counted here | A short run of tally strokes on one pier, in groups of four with a gap |
| Shop | The books are still kept | A long counter, a single shelf row, one empty peg |
| Event | A price was set here | One central object (altar, well, or plinth) with an empty seat for an offering |
| Recruit | A fire was kept | An arched alcove with cold ash |
| Boss / Red | The thing broke out | The arch broken, a diagonal intruding, the serrated frame |

### 6.6 Backdrop Layering Behind the Columns

The environment is authored once in the neutral INK ramp. State washes are applied by the engine (4.8), and the Red room is an index swap (INK-n becomes Wn), not a second art set. That swap needs a palette-index mechanism, which is a technical-artist task.

| Layer | Content | Values | Class | May sit in a character zone? |
| ---- | ---- | ---- | ---- | ---- |
| L0 Void | The **round-headed niche** behind enemy line 1 (140 px wide, full stage height, semicircular crown of radius 70, left jamb the stage divider) and the **square-headed void bay** behind line 2 (140 px wide, flat top at the stage frame), separated by a 4 px pier. Both are authored in every enemy room; an unused bay stays empty void. | INK-0 | D0 | Yes (it is the backdrop) |
| L1 Wall plane | Running-bond wall behind the **hero strip** and in the margin to the right of the second bay; the floor plane | INK-1 face, INK-0 joint | D1 | Yes |
| L2 Structure | Piers, arch ring, lintel, counter, alcove | INK-0 to INK-2 | D2 | **No** (0.25 H clearance), except furniture on an NPC (INK-1) |
| L3 Decor | Story props | INK-1 to INK-2, flat | D2 | **No** |
| L4 Foreground frame | The outermost piers and stage-edge darkness | INK-0, stepped (no gradient) | D0/D2 | Only at the outer margins |
| **Effects layer** | Rings, smears, trails, impacts, threads (Section 5.10.4) | Section 4 | | Above characters, below the protected layer. Aimed at the target's body centre, never at its tab, HP row or turn marker. |
| **Lit information (scene side)** | Characters, interactables, item avatars, stat chips | Section 4 | | Above L0-L4 and the effects layer. Nothing from L0-L4 draws over a lead shape. |
| **Protected layer** | HP, intent tabs, turn marker, order strip, workbench, action strip (Sections 5.10.4 and 7.2). The hover popup (Section 7.9) draws above every layer except the held item and cursor, under placement rules that keep it off all of these | Section 4 | | Above every scene layer, including avatars and chips. It never takes a Wine shift or a column wash. |

- **Behind the heroes** is L1, because the heroes sit at INK-3 and need two steps of separation.
- **Behind the enemies** is always L0, in every room type that has enemies, because enemies sit at INK-2.
- **Bosses and frames.** A boss may overlap L2 and L4 (3.7). It draws above them.
- **No lane lines and no slot dividers**, in either strip. They would encode position (Section 3.9).
- **Ambient motion.** No parallax. The two ambient elements allowed by 2.2 (for example drifting motes and one narrow banner) live in L3/L4 and never in a zone.

> **Refines 3.5 "one ground band" (approved).** Section 3.5 assumes a single horizontal ground band. The column layout stacks the heroes and the enemies vertically, so there is no shared floor line. The ground becomes **per-unit**: a pedestal for each hero and recruit, a contact shadow for each enemy, furniture for each NPC. The wall plane is continuous across the whole strip. The "no perspective floor" part of 3.5 stands.

### 6.7 Keeping the Environment Off the Reserved Shapes

| Reserved form (3.2) | Environment hazards | Substitute |
| ---- | ---- | ---- |
| Square lattice | Checkerboard floors, tiled walls, portcullis grids, window mullions, cubby shelves, any 2 x 2 or larger array of equal rectangles | Running bond, vertical-only bars, plain horizontal shelf boards |
| Chamfered corner | Bevelled plinths, chamfered capitals, octagonal piers, cut-corner doors | Square corners, or the one round arch |
| Inner bracket (double frame) | Picture frames, inset door panels, nested rectangles | Single-plane slabs |
| Serrated edge (equal teeth) | Crenellations, dentil moulding, rows of stalactites, saw-tooth rims, spike rows | Crenellations are banned outright. A lone irregular spike is allowed. Teeth appear **only** on the Red-room frame. |
| Hanging tag | Hanging signs, plaques, banners with a dark flat end, hanging weights | Banners only as tall narrow strips (1:4 or taller), square-ended, in L3/L4 outside any zone. No hanging signs. |
| Downward pennant | Pennants, triangular flags, swallowtail banners | Square-ended strips only, and never in the clearance above an enemy (the intent tab sits there) |
| Circle / ellipse | Round windows, floor medallions, round plinths, coin-like discs | The recruit alcove is an arch-shaped recess. The Candle lantern is allowed in menus and nowhere else. |
| Diamond | Lozenge windows, diamond floor patterns | Rectangles only |
| Chevron | Herringbone floors, V-braces, arrow marks, pointed arches | Round arches and flat lintels |

**Rhythm rule.** No more than **3** equal-spaced repeated vertical elements in view. Beyond three they read as teeth. Vary the spacing by 25% or more.

**Test.** Squint at an exported frame and look for any 2D array of equal rectangles, any row of equal points and any downward-pointing small shape above an enemy. Any found is a defect.

### 6.8 Alternatives Considered

| Alternative | Why rejected |
| ---- | ---- |
| Hearth **ring** for the recruit room | Reads as the pedestal ellipse or the coin circle. The alcove is also an arch, which keeps one curved vocabulary. 3.5 permits either. |
| Per-area bespoke environments | Cost. The kit-and-swap approach gives each area its look from a wash plus module swaps. |
| Props drawn with a rim to "ground" them | They would count as lit pixels with no information (Section 1, Principle 1) |
| Environment text for story | Competes with the UI's Bone text, and forces localisation of art |
| Backdrop at INK-2 behind everyone | Leaves the enemy only one step of separation and breaks the figure-ground contract |

### 6.9 Rule and Pillar Coverage

| Rule | Section 1 rule or principle | Pillar |
| ---- | ---- | ---- |
| Vault reading and grammar, 6.2 | Soft ink | P5, P4 |
| Flat stylised pixel and density classes, 6.3 | Principle 1, Light is information | P4 |
| Prop density caps, 6.4 | Simplicity guard | P4 |
| Storytelling rules, 6.5 | Soft ink (story props are ink) | P5 |
| Layering and the figure-ground contract, 6.6 | Principle 1, Principle 2 | P4 |
| Reserved-shape bans, 6.7 | Principle 2, Form before hue | P4 |

### 6.10 Refinements and Open Items

**Refinements (approved with this section):**
1. **3.5:** the ground band becomes per-unit (6.6).
2. **4.6 / 3.7:** Fog rim only on interactable props, and add interactable environment objects to the lead shapes (6.3).

**Open items:**
- The lore reading itself (6.2) is a proposal.
- Mythic rooms: whether they share the Red-room index swap (still open from 4.8).
- ~~The number and arrangement of enemies in the right column.~~ Closed: one or two vertical lines of at most three, 140 px wide (Section 7.4a). Open: the swarm and boss-companion rules.
- Palette-index swap mechanism for the Red room: technical-artist.
- The prop library cap: producer.
- Edge-dominance threshold and the stage luminance figures: re-measure on the first full-frame mock-up.
- **Figure-ground contract** (heroes and NPCs at INK-3 on a wall of INK-1, enemies at INK-2 on a void of INK-0) was accepted by the user and is written in Section 5; this section applies it.

## 7. UI/HUD Visual Direction

> **Status.** Approved by the user on 2026-11-10 (art-direction half, revised after the ux-designer's alignment report). User decisions: native 640x360 with integer scale; native pixel font gated by a Vietnamese render test, with "extend a Latin pixel font with the Vietnamese marks" tried before any hi-res overlay; Vietnamese and English; cross-hero visibility is **switch-only**; a **hover popup** instead of a docked inspector (the user rejected the dock: it held too much information); the enemies stand in **one or two vertical lines**, like the heroes' column (Section 7.4a). Interaction behaviour (focus order, bindings, flows) belongs to the ux-designer's half; where a visual rule forces an interaction consequence it is stated as a constraint. **Pixel measures assume 640x360 and a 1x1 cell edge C = 24; Section 8 owns C and proposes C = 20 provisionally.** The formulas are given so a change of C only changes a number: at C = 20 the party-slot pitch is 94 px (not 98) and the left-panel width for 8 columns is 174 px (not 206). Layout figures marked *provisional* are re-checked on the first full-frame mock-up.
>
> **Dependency not yet met.** The settings in 7.13 must be recorded in `design/accessibility-requirements.md`, which **does not exist yet**. This section is written, but it is not final until that file exists.

### 7.1 The UI rule

The UI is **ink that holds light**. Panels, frames, buttons and popup bodies are INK-class and never lit (3.6). The only lit things are information: tier plates, HP, intent, turn markers, selection and focus, Bone text. Where the Section 1 test ("does the player read or decide anything from this?") answers no, the element is chrome: dim, flat, plain.

Two additions to the rule, both consequences of the decisions above:
- **Form carries every state.** Hover, focus, selected, held, valid, invalid, disabled and pressed each differ by shape or stroke, never by hue alone (7.11).
- **Every screen is operable and legible without a mouse.** Focus is a first-class visual state, not a hover substitute.

### 7.2 Diegetic versus screen-space, element by element

The scene is a window held in the UI frame (3.6). The answer is **screen-space panels around a diegetic stage**, plus **anchored** elements: drawn in a fixed layer, positioned by a scene object, and never inheriting that object's animation.

**UI draw order, back to front** (extends 5.10.4):

1. Environment L0 to L4.
2. Characters and pedestals.
3. Effects (rings, smears, trails, impacts, threads).
4. **Item avatars, stat chips and damage numerals.** These are scene-side and UI-adjacent (7.5).
5. **Protected layer:** HP block, intent tabs, turn marker, order strip, the left workbench panel and the action strip. Always above 1 to 4.
6. **The hover popup (7.9), the held item and the cursor.** The popup draws above every scene layer but its placement rules keep it off HP blocks, intent tabs, the order strip and the action strip. The held plate may cross anything because the player is moving it.

The payoff bloom sits below layer 5 and below layer 4's numerals (2.3.5), so the bloom core, which equals Bone, never hides a number.

| Element | Answer | Visual treatment |
| ---- | ---- | ---- |
| **Backpack grid, stash** | Screen-space | Left workbench panel. Never takes a state wash (4.9). |
| **Hero sprite, pedestal** | Diegetic | Scene art (Section 5). |
| **HP block** (HP row, stat row) | Anchored, screen-space | Under each hero's pedestal and each enemy's contact shadow (7.4a), fixed in the slot. It does not move when the unit recoils (7.4, 7.4a). |
| **Item avatar** | Hybrid (scene object, UI grammar) | A 1x1 tier plate above the acting hero (5.10). Uses the item plate, so it obeys 3.4. Layer 4. |
| **Stat chip** | Hybrid | Plain square-cornered chip in the target's body band (5.11). Layer 4. |
| **Enemy intent tab** | Anchored | Pennant tab in the 26 px tab zone above the enemy's head (7.4a): type glyph, magnitude, target (7.5). Layer 5. The glyph lifts off the tab when the enemy acts and crosses the gutter to its target (5.10.8, 7.5). |
| **Turn marker** | Anchored | The Candle chevron under the acting unit's feet. |
| **Turn order** | Screen-space | Order strip in the top strip (7.5). |
| **Popup** (the tooltip) | Screen-space, floating, transient | One popup at a time, placed once beside the hovered object by the rules in 7.9. Inside the workbench it may cover empty cells and plates by the cost order in 7.9; on the stage it is only a chip in the object's own body band; it never covers an HP block, intent tab, order strip or action strip. |
| **Door cards** | Hybrid | Tier-lit card frames (scene light), glyph rows (screen-space). Right column, upper part; the popup opens beneath them (7.10). |
| **Shop** | Hybrid | Counter and shelf boards are backdrop (3.5). Wares are plates on shelf lines at true footprint, prices below. |
| **Recruit** | Hybrid | Pedestal in the alcove (diegetic) with the same HP block as a hero. |
| **Event, menus** | Screen-space | Plain rectangles on ink. |
| **Cursor** | Screen-space | 12 px Bone pointer with INK-0 outline, integer-positioned. |

**Rule: nothing floats over information.** Damage numerals draw above effects but beneath layer 5 and spawn at the target's **body centre** and rise inside its body band (7.5), never into the tab zone or HP row. Chips stay in the body band (5.10.4). The one thing that may float over the stage is the hover popup (7.9), which is transient, is placed once, and never covers an HP block, an intent tab, the order strip or the action strip.

### 7.3 Frame and layout

**Structure: two panels and one divider.** A thick-framed **workbench** (left) and a thick-framed **stage** (middle and right, one backdrop and one arch, split by a thin section line). Common region makes the screen read "tools on the left, theatre on the right", and the left-to-right order matches *my items, my heroes, what I fight*.

| Region | Width | At 640 | Contents |
| ---- | ---- | ---- | ---- |
| Left: workbench | 32% | about 206 | Hero name plate, grid, lower section (Stash or Overview) |
| Middle: party | 22% | about 140 | Three slots (7.4) |
| Right: enemies and room panel | 46% | about 294 (326 at C = 20) | The whole column, about 300 px tall (provisional): up to **two enemy lines** (140 px each, line 1 against the party column, 7.4a), or the room's panel (7.10). There is no reserved band. |

- **Left width is derived:** `n_columns x C + 14` (two 3 px frames and two 4 px paddings). At C = 24 and 8 columns that is 206. At C = 20 it is 174.
- **Right column composition.** The column is 294 px wide at C = 24 (326 at C = 20) and about 300 px tall. It holds:

| Room | Contents |
| ---- | ---- |
| Combat, boss | **Enemy line 1** (140 px, against the party column) and, if the encounter has one, **line 2** (140 px) to its right; with two lines and the 4 px pier between them, a 10 px (C = 24) or 42 px (C = 20) margin remains. With one line the second line's place is an empty void bay. |
| Door choice | Three 84 x 140 cards across the top; the popup beneath them (7.10) |
| Shop | Shelves in the upper part, counter and shopkeeper below |
| Event | The text panel and option buttons across the width |
| Recruit | The candidate in line 1's niche (against the party column); items and stats in the area to its right (154 or 186 px), which has no enemies in this room |
| Post-combat | The result panel across the whole column (7.5) |

- **Why not centred, why not right-aligned.** With one line, centred leaves two narrow strips that fit nothing and lengthens every crossing; right-aligned leaves a gutter of 154 px (C = 24). Adjacent to the party column keeps the crossings shortest for both lines.
- **Vertical budget at 360:** top strip 16, stage 314 (after frames), action strip 24. Left panel: name plate 16, grid (rows x C plus padding), section line, lower section at least 140 px. *Provisional.*
- **Top strip (16 px, full width):** the left part (over the workbench) holds currency (Candle coin plus numerals) and the room counter. The right part (over the stage) holds the **order strip**, then the settings button at the far right.
- **Action strip (24 px, stage panel):** stateful.
  - *Planning:* the **primary action** (start the fight) at the right, a Candle 2 px frame, Bone text; a prompt chip shows its binding.
  - *Combat:* the **speed control**, four plain rectangular segments (1x, 2x, 4x, Skip), with the current one selected (7.11) and one prompt chip showing the cycle binding for keyboard and gamepad.
  - *Door choice, shop, recruit:* the **Confirm** button (7.10).
- **Gestalt reading:** the thick frame separates "what I change" from "what plays out"; the thin line separates "mine" from "theirs". Square corners everywhere (3.6).
- **16:9 and scaling.** Integer scale only (3x at 1080p, 4x at 1440p, 6x at 4K, 2x at 720p). The frame is never stretched. Non-multiple windows show extra backdrop (bleed), not black bars, because crop frames clear to pure black and break the ink floor (Section 8); ultrawide shows more surround, never extended UI.
- **Red room.** The stage frame takes Wine (4.8). The protected layer keeps its normal palette, so HP and intent never shift. The order strip belongs to the protected layer and is exempt from the Wine shift and from every column wash. The popup keeps its own INK-1 and INK-3 palette in every room.

### 7.4 The party slot: stack, HP block and what lives where

Each of up to three slots is identical (one pedestal size, join order only, no position meaning). Top to bottom, at C = 24, H = 32:

| Zone | Height | Content |
| ---- | ---- | ---- |
| **Avatar zone** | C + 4 = 28 | **Empty at rest. Nothing else may use it** (5.10.3). Backdrop shows through. |
| Body and prop | 1.25 H = 40 | The hero (Section 5). |
| Pedestal | 8 | Pedestal, Candle pool, join-order numeral. |
| **HP row** | 10 | Heart glyph (8), HP bar (outlined, about 36 wide, 8 high), and **current / max numerals beside the bar** in the 8 px numeral cut. |
| **Stat row** | 12 | Armour (Fog shield glyph and number), speed gauge (Fog fill, Bone ticks, about 20 wide), up to three status chips (glyph and stack number), and an overflow count "+n". The overflow opens in the popup (7.9). |

- **Slot pitch: 98 px** at C = 24 (94 at C = 20). Three slots are 294 px in a 314 px stage. This adds 12 px to the pitch that 5.8 first estimated, because persistent statuses, armour and speed need a stat row as well as an HP row. **Native height 360 is therefore required, not just comfortable.** If the stat row has to go, the fallback is to fold statuses into the popup and keep armour and speed only.
- **HP bar contrast fix (amends 4.2 and 4.6).** Blood `#B0505A` on Blood-D `#4E2229` is only about 2.6-2.8:1 by hand computation, below 3:1. Fix: the **track becomes INK-0**. Blood on INK-0 is about 3.8:1 (hand-computed, NOT VERIFIED). The border stays INK-4. Blood-D is kept as the **lost-HP trail**: on damage the lost segment shows Blood-D for 200 ms, then steps to INK-0. No hue change, no flash.
- **Numerals never sit on the fill.** Bone on Blood is about 3.6:1, which fails the 7:1 rule for primary text.
- **Names are not in the slot.** The hero's name sits in the left workbench panel's name plate (for the shown grid) and in the popup on focus. The pedestal carries the join numeral.
- **Selected hero (planning).** The Candle halo on the pedestal plus the Candle name plate over the grid. In combat the halo is replaced by the turn chevron (7.5). They never coexist.
- **Fallen hero.** The HP row reads 0, the bar is empty, and the stat row clears. The slot keeps its place (5.6).

### 7.4a The enemy lines and the enemy slot

**Decision (user, final): the enemies stand in one or two vertical lines side by side.** Each line is a column read top to bottom, exactly like the heroes' column. **No line may be horizontal, and no third line fits at any C** (three lines need at least 360 px, against a right column of 294 px at C = 24 and 326 px at C = 20).

**Where the lines sit.** Each line is **140 px wide** (a 120 px slot plus 8 px of clearance each side). **Line 1 stands against the party column**, at the left edge of the right column; its left jamb is the stage divider. **Line 2 stands to its right.** An encounter always fills line 1 first, so line 2 is never occupied while line 1 is empty. Two lines and the 4 px pier between them are 284 px, of a right column of 294 px (C = 24) or 326 px (C = 20), leaving a margin of 10 px or 42 px (the planning mock-up measures 7 px and 39 px after the 3 px stage frame). With one line the second line's place is an empty void bay (Section 6.6).
- **Why line 1 is against the party:** avatars and intent glyphs cross a short gutter, and the party and the first line read as facing each other (Gestalt proximity).
- **No popup lane.** With two lines occupied there is no free strip, so the popup does not depend on the enemy lines (7.9).

**Slot stack, top to bottom, per slot in each line.**

| Zone | Height | Content |
| ---- | ---- | ---- |
| **Intent tab zone** | 26 | The tab (about 24 including its point) and a 2 px gap, centred above the head. Nothing else may use it. |
| Body | body + 4 | The enemy, plus 4 px of act-pose headroom (8.6). |
| Shadow and turn-marker row | 4 | The flat contact shadow (or the visible gap for a hovering monster); the turn chevron when this enemy acts. |
| **HP row** | 10 | The same component as a hero's (7.4). |
| **Stat row** | 12 | The same component as a hero's: armour, speed, up to three status chips, overflow "+n". Statuses on enemies are what the build detonates, so the row stays. |

**Slot pitch = body + 56.**

| Class (8.6) | Body | Pitch |
| ---- | ---- | ---- |
| Small | 24 to 28 | 80 to 84 |
| Medium | 36 to 44 | 92 to 100 |
| Large | 52 to 60 | 108 to 116 |
| Boss | 96 to 115 | 152 to 171 (a 120 px body is 176) |

**How many fit.** Per line the column budget is **about 300 px** (stage 314 less margins; provisional). **At most 3 enemies per line, the sum of their pitches at most 300 per line, so at most 6 enemies in all.** Examples per line: 3 small (252); 2 medium and 1 small (284); large, medium and small (300). The two lines are budgeted independently; they never share a budget. A swarm of more than six is met by **reinforcement**: when one dies, the next fills the first empty slot of line 1, then line 2. Capacity is not a target: the combat GDD should cap encounters below six.

**Reading order.** Line 1 first, then line 2, each top to bottom: **spawn order only, with no meaning**, as for the heroes' join order (5.3). Acting order is given by the order strip and the turn marker. The encounter data names the line and slot; by default it fills line 1 first. **Two lines will read as "front" and "back".** No art or gameplay rule may depend on the line: no archetype is assigned to a line by type, and no size, lean or tab varies with the line. The combat GDD must not give a line a targeting meaning unless the art is changed first.

**Vertical placement.** **Each line is centred in the column independently.** Slots in different lines are never aligned on purpose, because alignment would read as rows or ranks. No lane lines and no slot dividers (6.6). A defeated enemy's slot stays empty; **neither line reflows mid-fight**, so tabs, HP blocks and aim points do not move.

**Boss.** A boss occupies **one line**; its canvas (128 px, 8.6) fits the 140 px line, and **a boss never spans two lines**. It stands in line 1 (facing the heroes). That line holds the boss and **at most one companion** (small or medium; 171 + 84 = 255). Line 2 may hold up to 3 more enemies if the encounter data asks. A composite boss's arm may extend sideways into **free width only**: up to 40 px to the right when line 2 is empty, none when line 2 is occupied (7 px usable at C = 24). Its tab and HP block are centred on the body, not on the overflow.

**Outline.** Every enemy has a 1 px **INK-4** outline on all sides (user decision, Section 5.9), because the INK-0 outline is invisible against the INK-0 void.

### 7.5 Combat-time UI

**Avatars and chips (UI-adjacent).** Motion, timing and anatomy belong to Section 5 (5.10 to 5.12). Section 7 adds only:
- They are **layer 4**: above effects, below layer 5. A freeze-frame must still show every intent tab, HP block and the order strip (Principle 3).
- They follow UI text rules: numbers in the numeral face (chip digits in the 8 px cut), no all-caps, Bone only.
- A chip must stay in the body band and never enter the avatar zone or the HP block.

**Intent tab: type, magnitude, target.** The tab is about 40 x 24 (including the point). It holds, in Fog-hi (low saturation):
1. the **intent glyph** (12 x 12, the shared keyword drawing, 7.8);
2. **magnitude** in 8 px numerals, to the glyph's right ("12", "12x3" for multi-hit, "1.2K");
3. the **target chip**, a small square plain chip with the **target hero's join numeral**; self-targeting has no chip; all-heroes shows a row of three small squares.

Focusing or hovering a tab lifts the target hero's join numeral on the pedestal to a 1 px Fog-hi frame (no new shape). When the enemy acts, the glyph lifts off and the glyph slot goes dark (5.10.8).

**Crossing the gutter.** Both sides stand in vertical lines, so a target is rarely at the source's height.
- **Aim:** every effect aims at the target's **body centre**, never at its tab, HP row, stat row or turn marker (5.10.4).
- **Paths:** an avatar (Swing, Throw, Status) and an enemy's lifted intent glyph travel a **straight stepped line in whole pixels** from their source to the target's body centre. The line is **diagonal** when the target is higher or lower. It is drawn in layer 3 or 4, **beneath the protected layer**, so it passes behind any HP row, stat row or tab on the way. The protected layer rule is unchanged.
- **Distance:** hero centre to **line 1** centre is about 140 px across; to **line 2** centre about **280 px across** (C = 24; 248 at C = 20), and up to about **375 px on the diagonal** (352 at C = 20). Section 5.11's hop counts are raised accordingly (about one hop per 48 px of path, 4 to 8 hops for a Throw).
- **Paths from line 2 cross line 1.** An avatar or lifted glyph passes **over line 1's bodies** (layers 2 and 4) and **behind line 1's tabs, HP rows and stat rows** (layer 5), so nothing protected is ever hidden. The target is identified by its lit eyes (5.4) and by the chip at contact, not by the path, so crossing a body does not misattribute the hit.
- **Rings and threads:** a point ring blooms at the target's body centre; an area ring on allies is centred on the avatar and grows up and down the party column (5.11). **Threads never cross the gutter**: they end at the avatar (5.10.9), so only avatars and lifted glyphs cross it.
- **Numerals** spawn at the target's **body centre** and rise inside its body band, never into the tab zone or HP row. They are drawn beneath layer 5, so a wide numeral may overlap a neighbouring line's body but never its tab or HP block.
- **Popups for stage objects** are chips in the object's own body band (7.9).

**Turn order: the order strip, not a badge on the pedestal.** Pedestals already carry the join numeral (identity); adding a second numeral for turn order would collide. The **order strip** (top strip, right part) is a row of 14 px square plain chips: hero chips show the **join numeral**, enemy chips show the **intent glyph**. The active chip carries the **Candle chevron** beneath it (the same turn-marker meaning as under the unit's feet). The strip is visible in planning (the order the fight will play) and in combat. Focusing a hero's HP block or a chip names the order in words in the popup ("acts 2nd"). *This depends on the combat GDD: if turn order is a fixed hero sequence, the strip shows only that and the layout is unchanged.*

**The left grid follows the acting hero in combat.** At the start of each hero's Ready phase the workbench swaps to that hero's grid, so the plate echo (5.10.9) and the firing-order pulses are visible.
- **Swap envelope:** a 100 ms (6-frame) fade-in. It is **not speed-scaled**, so it also meets 4.12 rule 5 at 2x and 4x. At **Skip** there is no follow; the grid stays where it was.
- **Marker:** the name plate shows the Candle chevron beside the join numeral, so the player knows the grid belongs to the acting hero. The Candle halo is planning-only.
- **After combat** the workbench returns to the hero the player had selected.
- **Manual hero switching in combat is off for the MVP** (revisit with a "pin to hero" action later).

**Speed control.** Lives in the action strip during combat (7.3). Speed scales *holds and cadence only, never rise and fall envelopes*. Every luminance rise stays at least 100 ms at any speed (this refines 5.10.7, amended below). At Skip the plate sweep is one plate per frame but each plate still eases over 6 frames (about 0.45 s total for 20 plates). The Detonation peak plays at 1x on any setting except Skip (Pillar 2).

**Post-combat breakdown.** Because the 1.5 s peak is too short to read which items fired, a **result panel** replaces the enemy zone when combat ends (the whole right column, about 9 rows of 28 px, because both enemy lines are gone; popups open over it by the rules in 7.9).
- One row per contributing item, **ranked by contribution**: rank numeral (8 px), the item's **avatar plate** (zero new art), name (Body, Bone), and the contribution in the 12 px numeral face. A Bone bar on an INK-2 track shows the share. **No hue gradient.**
- A row has a hero filter (the join numeral chips) and an "all" view.
- Focusing a row **highlights the source plate in the workbench** (the cause link, Pillar 2).
- It is dismissible, and re-openable from the planning screen as "last fight".

### 7.6 Planning-time UI and Pillar 4 under switch-only

The workbench shows one hero's grid at a time. **There is no per-hero compact readout in the party column** (user decision). Pillar 4 ("everything the player decides is visible before the fight") is therefore protected by five measures:
1. **Cheap switching.** Key, gamepad bumper, click on a pedestal, and switching *mid-drag* (the held plate stays in the hand while the workbench cross-fades over 100 ms).
2. **Persistent resting bridges** in the grid (below), so adjacency synergies are readable at a glance.
3. **The planning overview** (below), a toggle that shows all three heroes' items on demand.
4. **The popup** shows an "affects" row of the **join numerals of the heroes affected** (reusing the pedestal numeral) in its compact view whenever an item acts on another hero, so cross-hero effects are visible without opening anything.
5. **The order strip** and intent tabs stay visible in every planning state.

**Synergy bridge: three states (amends 3.7).** One form (the short bridge across the shared seam, never covering plates), three states:

| State | When | Look |
| ---- | ---- | ---- |
| **Resting** | Planning, always | Fog, 1 px, static. Area about 0.1% of the frame for a full grid, so it does not strain the 4.10 budget (hand estimate, to be measured). |
| **Preview** | While a held item hovers a position, and on focus of a plate (partners' edges lift to their tier highlight) | Fog-hi, 2 px, no pulse. 100 ms in, 80 ms out. |
| **Trigger** | Combat, when it fires | Tier highlight with a 1 to 2 frame pulse (3.7, 5.10.9). |

**Planning overview (toggle, not hold-only).** One control, in the workbench's lower-section header as a tab (Stash | Overview), with a prompt chip. It does two things:
- **Grid lens:** firing-order numerals appear on plates, and bridges rise to the preview state.
- **Party ribbon** (replaces the lower section): three rows, one per hero (join numeral chip, then that hero's items **as 1x1 avatar plates in firing order**, with the **cooldown number** under each plate; passives after a thin separator). Focusing a plate highlights its partners across all three rows and in the grid. The ribbon lists every item of every hero compactly.
- **Numerals on plates** appear only in overview and on focus, never by default (7.11). They sit on the plate's bottom edge lane at the anchor cell, in 8 px digits with a 1 px INK-0 outline.
- Picking an item up switches the lower section back to Stash automatically; the grid lens stays.

### 7.7 Typography

**Personality.** Sturdy, plain, a little warm: monoline, square-ish rounds, open counters. Letters cut into stone by someone who wanted them read. No gothic, no blackletter, no decorative display face.

**One native pixel text face (Vietnamese-gated) plus one custom numeral face.**

**Gate: no font is adopted until it passes the Vietnamese render test.** Test on INK-1 and INK-2 at 1:1 and at 3x:

| Test | Pass |
| ---- | ---- |
| **Coverage** | All **146** glyphs present as **precomposed** code points: 12 vowels (a ă â e ê i o ô ơ u ư y) x 6 tone states (none, grave, hook above, tilde, acute, dot below) x 2 cases = 144, plus Đ and đ. No fallback tofu, no reliance on combining marks. Verify code points (Ă U+0102, Đ U+0110, Ơ U+01A0, Ư U+01AF, and U+1EA0 to U+1EF9) against the font. |
| **Stacked capitals** | Every capital with a circumflex plus a tone (Ấ Ầ Ẩ Ẫ Ậ Ế Ề Ể Ễ Ệ Ố Ồ Ổ Ỗ Ộ) is legible at the smallest size, and the tone mark is distinguishable. |
| **Tone pairs** | Hook above versus tilde (ả ã, ẩ ẫ, ể ễ) and grave versus acute (à á, ề ế) are distinguishable in **1:1 greyscale** at 12 px. Dot below is not confused with a full stop. |
| **Horns and bars** | ơ and ư show their horn; đ shows its bar, at 12 px. |
| **Line collision** | Three stacked lines at the **14 px** pitch (capitals with marks over lines with descenders and dot-below, for example "Ượ ở ộ ị ạ") do not touch. |
| **Real strings** | Ten longest real strings from the prototype in Vietnamese, plus their English versions, set in the popup width (144 px at C = 24, 176 px at C = 20). |
| **+30% expansion** | The longest English string grown by 30% still fits its container (7.13). |
| **Licence** | Allows embedding in a shipped game. |
| **Digits** | Tabular digits, or the numeral face substitutes. |

**Size hierarchy (native px, at 640x360):**

| Role | Line box | Pitch | Colour | Use |
| ---- | ---- | ---- | ---- | ---- |
| Display | 24 | 28 | Bone | Title, victory and defeat banners only |
| Heading | 16 | 18 | Bone | Panel titles, item names in the popup |
| Body | 12 | 14 | Bone | Effects, buttons, menus, event text, card names |
| Secondary | 12 | 14 | Fog | Flavour text, labels |
| Digit micro | 8 | n/a | Bone | **Digits only** (HP, tab magnitude, chip numbers, prices) |

- **Minimum readable size:** a **12 px line box** for any text that can contain Vietnamese. A stacked capital needs about 6 px cap height plus about 4 px of mark zone, with about 2 px below for descenders and dots, so 12 px is the least that leaves the marks legible. Digits have no marks and drop to 8 px.
- **At 3x** that is 36 px (Body) and 24 px (digits), above the 16 px floor the ux-designer asked for. At 2x it is 24 and 16.
- **Weight:** a 1 px stem for Body, 2 px for Heading and Display; no bold at 12 px, no italics, no letter-spacing tricks. Hierarchy is size, then colour.
- **Case:** sentence case; **no all-caps** in either language (the worst case for stacked marks).
- **Colour:** Bone, Fog, INK-3 for disabled controls only (4.9). Never a tier colour (4.7). Over scene art: 1 px INK-0 outline. On panels: none.
- **Rendering:** no anti-aliasing, no sub-pixel positioning, integer positions only. Export and atlas details are Section 8.
- **Bilingual layout rule.** Vietnamese is about 30% longer. Every container is laid out for the Vietnamese length and tested in English (so English never looks broken), not the reverse. Buttons size to content with a minimum and a maximum width. Labels that can be a glyph are a glyph.
- **Fallback ladder (decided by the user), in order:**
  1. Another native font that passes the test.
  2. **Hand-extending a good Latin pixel font with the Vietnamese marks** (the marks are shared components, so the cost is moderate). This needs a licence that allows modification.
  3. **Hi-res text overlay**, only if 1 and 2 fail: text is drawn at screen resolution on top of the pixel-scaled game, positions snapped to the native grid, same sizes in native-px-times-scale, flat colours. It trades pixel integrity for legibility, and the lit-mask check (4.10) is run on it.

**Numeral face (custom, digits and a few symbols).** About 20 glyphs: 0-9 `.` `,` `K` `M` `+` `-` `x` `%` `/`. Tabular.

| Size | Digit height | Outline (baked) | Use |
| ---- | ---- | ---- | ---- |
| 8 | 7 | none | HP, tab magnitude, chip numbers, prices, ribbon cooldowns |
| 12 | 10 | 1 px INK-0 | Smallest damage, stat lines |
| 16 | 14 | 1 px | Damage step |
| 24 | 21 | 2 px | Damage step, result panel |
| 32 | 28 | 2 px | Damage step |
| 48 | 42 | 3 px | **Payoff peak only** |

- **The outline scales with size** because the bloom core equals Bone. A Bone numeral on a Bone-bright core must keep a dark edge; the outline is 1/1/2/2/3 px by size.
- Damage and heal are Bone with an INK-0 outline; damage taken carries a Blood minus. No numeral is ever red (4.6).
- The magnitude bands that pick a size are a balance value.
- **Thousands separator and K/M versus Vietnamese abbreviations** are a localisation decision. The face contains both `.` and `,`.

### 7.8 Iconography

**Style: flat, filled, outlined silhouettes.** One light direction (top-left).

| Family | Style | Grid | Colours |
| ---- | ---- | ---- | ---- |
| **Item icons** | Filled object, 3 value steps plus 1 px outline (4.13), upright | 16 x 16 canvas, 14 x 14 live for 1x1, 1x2, L (compact glyph); 32 x 32 for 2x2; **plus one 1x1 avatar glyph per multi-cell item** (3.4.2). Canvas sizes follow C (Section 8). | Any colour of the master palette (free palette, decided in Section 8.5.3) |
| **Keyword glyphs** (status, stat, synergy keywords) | Solid silhouette, fill plus 1 px shade plus 1 px outline, at most one carved interior line | **12 x 12 master, hand-reduced to 8 x 8** | Bone |
| **Intent glyphs** | The same keyword drawing, flat skin | 12 x 12 | Fog-hi, no shade step |
| **UI symbols** (heart, shield, coin, cross, settings, rotate, pin) | As keyword glyphs | 8 x 8 or 12 x 12 | Per 4.6 |
| **Input prompts** | **Plain square-cornered keycaps** (12 px), including gamepad buttons (drawn as squares with the button letter; **not circles**, which are reserved) | 12 px | Bone on INK-2 |

**One verb, one drawing, three skins.** A keyword is drawn once at 12 px. It appears as an **inline glyph** in popup text (Bone, 8 px), a **status chip** (Bone on a plain plate, a 3 x 2 chevron in the corner for buff or debuff direction), and the **intent glyph** (Fog-hi). Skins are recolours, not redraws. Item icons are objects, never reduced to pictograms. The generic droplet in 5.11 is the *poison* keyword glyph; every other status has its own keyword glyph.

**Glyph sets (counts depend on the roster, Section 5 open item):** intent (attack, guard, buff, debuff, and others per the enemy GDD), stat (damage, armour, speed, and the rest), status (about 10 to 12). Section 7 fixes the grid, skins and constraints, not the roster.

**Reserved-shape audit (3.2).** No glyph silhouette may be a bare circle, a bare diamond, a chevron (other than the buff/debuff corner mark), a pennant, a toothed row, a hanging tag or a lattice. A "ring" or "orb" gets a distinguishing feature (a notch or a stem).

**Item-icon colour (decided by the user in Section 8.5.3: free palette).** Item icons may use any colour of the master palette, so a red potion or a green gem is allowed. **The tier is therefore read from the plate's form first and from its thin lit edge second**, never from the hue of the icon. The risk is that icon hue competes with the tier edge: the icon spike (8.5.6) checks every tier at 3x and 2x, in greyscale and in colour, including an icon that shares its plate's hue. If a tier stops reading, the first remedy is to cap icon saturation at 0.60 (the 4.1 rule for non-tier colour), not to restrict hue.

**Rejection test:** off-palette pixel, a rim on the wrong side, a reserved shape used decoratively, or an 8 px glyph not distinguishable from every other 8 px glyph on a 1:1 greyscale sheet.

### 7.9 The hover popup and keyword glossary

**Decision (user, final): there is no fixed inspector. An object's details appear in a single popup on hover.** The user rejected the dock because it held too much information. The popup therefore carries essentials only, and everything else is one deliberate action away.

**Rules that hold for every object (item plate, door card, ware, candidate, event option, intent tab, HP block, status chip, order chip):**
- **One popup at a time.** It opens after a **300 ms** hover, or **instantly on keyboard and gamepad focus**. Hover and focus show the same content in the same place, so nothing is hover-only.
- **Moving the pointer to an adjacent object swaps the content** without closing and reopening. Leaving closes it after a 60 ms grace.
- **It is placed once, when it opens, and never moves** while it is open. It closes if its object moves (a drag starts, the hero switches, combat starts).
- **Suppressed while dragging**, except the held item's own readout (below).
- **No nested hover.** A popup is not interactive.

**Size classes.**

| Class | Width | Height | When |
| ---- | ---- | ---- | ---- |
| **Compact** | **152 px** at every C (it must fit the workbench's inner width: 192 at C = 24, 160 at C = 20) | up to 140 | The default |
| **Expanded** | 152 | up to 232 | After the Details action |
| **Chip** | to content, up to 152 (120 on the stage) | 18 | Stage objects, and the last-resort fallback |

The frame is the standard panel frame (3 px INK-3 plus a 1 px INK-4 highlight, Section 8.8), square corners, INK-1 bed. It never takes a wash in any room.

**Writer budgets** (about 6 px per character, 136 px of text width, about **22 characters per line**; measure with the chosen font): names up to 17 characters; effect text 3 lines (about 66 characters); curse 2 lines (about 44); keyword entry 2 lines including its name (about 44).

**Compact anatomy, top to bottom (essentials only).**
1. **Name** (Heading), one line.
2. **Tier name in words** (Body, Bone) with the rank diamonds. Colour is never the only carrier of the tier (4.4). Door cards and wares also carry the word.
3. **Numbers:** up to three stat glyph and numeral pairs (numeral face, 12 px).
4. **Effect text (Body):** at most **3 wrapped lines**, with at most **2** when the item also has a curse and an affects row. A keyword is Bone text with a leading 8 px glyph and a 1 px Fog-hi underline (glyph and underline, never hue).
5. **Affects (only when the item acts on another hero):** one row of join-numeral chips (reusing the pedestal numeral). It keeps cross-hero effects visible without opening anything (Pillar 4, switch-only).
6. **Curse (Gold and Red):** the hanging Plum tag with at most 2 lines on Bone. Cost and benefit are in the same popup (Principle 4).

**Not in the compact popup:** flavour text, keyword definitions, the "works with" chips, the mini plate (the hovered plate is already beside it).

**Expanded view (the Details action).** One action both **expands and pins**. The popup grows in place and stays until Details is pressed again, Cancel is pressed, an item is picked up, or the hero switches. While pinned, hover popups for other objects are suppressed. The expanded view adds, below a thin line:
- **Keywords:** each keyword the object uses, as *glyph, name, definition*, at most 3 entries of at most 2 lines each (the **auto-glossary**, but only on request).
- **Works with:** a row of keyword glyph chips for the synergies the object wants.
- **Affects** (if not already shown) and the full curse text.

Bindings (the ux-designer owns them; recommended): keyboard, a remappable key; gamepad, a face button; **mouse, right-click on an object when nothing is held** (right-click rotates while an item is held). If the largest legal rectangle is shorter than the content, the expanded view **pages**: Details advances, the wheel and direction keys scroll, the last page returns to compact.

**Armed objects hold their popup.** A selected door card, ware, candidate or event option (7.10) keeps its compact popup open until it is confirmed or disarmed, so the player can re-read it while moving to Confirm.

**Held item.** While dragging, the held item's readout is **one line in the workbench's name plate** (its name and tier word, plus a number pair if it fits), which returns to the hero's name on drop. It covers nothing, never follows the cursor, and cannot hide a drop target.

**Popup content by object kind.**

| Object | Compact shows |
| ---- | ---- |
| Item plate, avatar plate, ware | Items 1 to 6 above |
| Door card, event option | Effect prose and the curse (the name and tier are already on the card or button, but the popup repeats the name for consistency) |
| Intent tab | Action name, type glyph, magnitude, **target in words** ("Hero 2"), one short line, **as a chip** |
| Status chip or overflow "+n" | Status name, stack, one line. Overflow lists the hidden statuses, one per line. |
| Hero or enemy HP block | Name, HP and armour in words, speed, status list |
| Order chip | Name and "acts 2nd" |

**Placement (computed once when the popup opens; it never moves).**

*Never covered (hard exclusions; a position that touches one is rejected):* the hovered object and its focus ring; every **HP block** (HP row and stat row, heroes, enemies, recruit); every **intent tab** and its target chip; the **order strip, top strip and action strip** and the Confirm button; the **turn marker**; any **avatar, chip, damage numeral or lifted glyph** while combat plays; a pinned popup.

*Why not on the stage.* The enemies stand in one or two independently centred vertical lines (7.4a). The free band between a slot's tab and the next slot's HP row is only the body band (36 px for a small enemy, 52 for a medium, 68 for a large), and the two lines' bands never line up, so no rectangle of the popup's size avoids every tab and HP block. The popup therefore never opens over the enemy lines.

*Workbench objects (grid, stash and ribbon plates):* the popup opens **inside the workbench** and never on the stage. Candidates are the four sides of the object (4 px gap, slid in 8 px steps) and, for a grid object, the **lower section** (stash or overview). The cheapest candidate wins; within a cost, the nearest.
- Cost 0: empty cells and margins.
- Cost 1: the stash's or ribbon's plates.
- Cost 2: grid plates that are not partners of the object.
- Cost 3: partners or bridges of the object, or the name plate. Only if nothing cheaper exists; otherwise the popup becomes a **chip**.
- A grid object prefers the lower section (cost 1) over covering the grid (cost 2); a stash object prefers its own region.

*Stage objects (intent tab, HP block, status chip, order chip, enemy or hero body):* the compact popup is a **chip** (name, magnitude or value, target in words; up to 120 px wide) in the object's **own body band** (the band between its tab zone and its HP row), which is free by construction. The expanded view (Details) **lands in the workbench lower section** (costs as above), pinned. **In combat, stage objects get no popup.**

*Room panels with no enemy lines (door choice, shop, event, recruit):* the popup opens in the room's **free lower band** (7.10), beneath or beside the object, over backdrop only.

*Fallback:* a chip beside the object on its cheapest side. It is 18 px tall and can always slide to a free place.

**Where it opens in practice:**

| Origin | Result |
| ---- | ---- |
| Plate in a sparse grid | Beside the plate over empty cells (cost 0) |
| Plate in a dense grid | The lower section (cost 1) |
| Stash plate | Over its own empty cells, or other stash plates |
| Ribbon plate | The lower section's free area, else a chip |
| Intent tab, HP block, status chip | A chip in the object's own body band |
| Order chip | A chip below the strip, over the first slot's tab-free margin |
| Door card, ware, event option, candidate | The room's free lower band |

**Costs the user accepted by choosing a popup.** A popup can hide non-partner plates and, as a last resort, partners (then a chip is used instead); stage objects get chips, not full popups, and the full text needs Details; the width is 152 px (about 22 characters per line); keyword definitions are no longer listed automatically, so a first-time player may never find them. Mitigations: keywords are visibly underlined, the Glossary page exists, and a one-time hint appears on first use. **Fallbacks if testing shows harm, in order:** (1) always land workbench objects in the lower section and drop the near placements, so the position is predictable; (2) *Compact Lite*: name, tier, numbers and one effect line on hover, the full compact popup only on focus or Details.

**Glossary.** A **Glossary** page in the menu lists every keyword with its glyph. It is for reading between runs, and the popup never needs it.

**Lit-area note.** A compact popup at 152 x 140 is about 21,000 px², about **9% of the frame**; its lit pixels (Bone strokes) are about 1% to 1.5%. An expanded popup is about 15% of the frame, about 2.5% lit. Both are inside the mattering mask (text). Measure planning with a compact popup open; treat a pinned expanded popup as a reading state (APL capped, lit budget decided separately, 7.16).

### 7.10 Room panels: door choice, shop, recruit, event

The right column holds no reserved panel. In every room the **upper part holds the room's objects** and the **lower part is free backdrop where the popup opens** (7.9). Cards, wares and options carry **glyphs, numerals and short labels only**. Prose lives in the popup.

**Door choice.**
- The workbench stays visible and operable (4.8 keeps it unwashed).
- **Three glyph-first cards** side by side at the top of the right column: each **84 x 140** at C = 24 (up to 90 x 140 at C = 20), with 12 px gaps. Three cards are 276 px of about 280 usable (4 px spare) at C = 24, and 276 to 294 of about 312 at C = 20.
- **Card anatomy, top to bottom:** the tier form on the silhouette (3.4.4), rank diamonds, the **name** (Body 12, up to two lines of about 12 characters; Vietnamese names under about 16 characters), the **tier name in words** (Secondary, one line), **up to three benefit rows** (a 12 px keyword glyph and a numeral), and the hanging **curse tag** (a glyph and numeral). About 130 px of the 140.
- **Prose goes in the popup, not on the card.** An 84 px card holds about 13 Vietnamese characters per line, and a taller card would not fix the width. The popup opens **beneath the card row**, left-aligned with the focused card and clamped to the column, over empty floor: 140 + 4 + 140 is 284 px of about 290. Cards stay 140 tall, so the compact popup has room.
- **Select, then confirm.** Hover or focus shows the popup. Click or press select **arms** the card (ring plus the corner square, 7.11); the armed card holds its popup, and the action strip shows **Confirm**, bound for every input method. Double-click may be a shortcut but never the only path.
- The 2.3.3 mood is kept: the cards are the lit doorways, with the plum cost taking light from the card's own glow.

**Shop.**
- Wares sit on **shelf lines** in the **upper part** (at most three rows, about 150 px; plates at true footprint, in rows, not a lattice, 3.2). The counter and the shopkeeper fill the lower part, which stays free of wares so the popup has somewhere to open.
- Footprints and **prices are visible without hover**. A fixed **price row** (about 10 px) sits below the tag zone (about 8 px), so Gold and Red wares do not shift.
- **An unaffordable ware keeps its full tier edge.** Only the price changes: Fog numerals plus the 5 x 5 cross glyph (INK-3 is only about 1.6:1 against INK-1, so it would make the price unreadable; this amends 4.9).
- Buying is irreversible and uses select, then confirm. An armed ware holds its popup.

**Recruit.** The candidate stands in **line 1's niche** (Section 6), against the party column, so the party's pools visibly reach toward it (5.7). It has the **same HP block** as a hero. Its **stats** are a row of stat chips under the HP block. Its **items** are plates at true footprint on a shelf row in the area to the right of the niche (154 or 186 px, empty of enemies in this room), with the popup opening below them. **Where it falls:** the next empty pedestal outline previews the join numeral, and the order strip shows the new chip inserted and highlighted. Accept uses select, then confirm.

**Event.** A plain INK-1 text panel across the full width of the upper part (about 110 px) holds the event's prose in Body. Plain rectangular **option buttons** (up to three, about 30 px each) sit beneath it. An option's **consequences** and **costs** appear in the popup (compact, at most 2 effect lines plus the cost, so it fits the free band of about 90 px beneath the options). Irreversible choices are confirmed. An armed option holds its popup.

### 7.11 State table

Each state differs by form. Hover, focus and selected all use the Candle ring (4.9) drawn **outside** the element with a 1 px gap, so it can never merge with a tier frame.

| State | Form (all interactive elements) |
| ---- | ---- |
| **Default** | Element at rest. Buttons: INK-4 frame, Bone text. |
| **Hover** | **1 px** Candle ring, 1 px gap. |
| **Focus** | **2 px** Candle ring, 1 px gap, **drawn above neighbouring plates** (z-lifted, so flush neighbours do not hide it). No brackets (Gold owns them). The prompt chip with the current binding appears. |
| **Selected** | A ring (1 px, or 2 px if also focused) **plus a 4 x 4 Candle square at the ring's top-left**. Hero: also the pedestal halo and name plate. Card: armed, with Confirm. Segment: the active speed. |
| **Held** | Plate lifted 2 px with a 2 px INK-0 shadow lower-right and a 2 px Candle ring. Origin cells show an empty bed. |
| **Valid target** | Ghost footprint, solid 1 px Fog-hi outline; covered cell beds one step lighter. |
| **Invalid target** | Ghost footprint, 1 px **Fog** outline (not INK-4, which is only about 2.5:1 on the INK-2 cell bed, 4.3), **desaturated**, and a **5 x 5 cross on each blocking cell**. No red. |
| **Disabled** | INK-3 frame and text, no hover ring, but **still focusable** (the focus ring shows) so the popup can explain why. Never used for text the player must read. |
| **Pressed** | Candle fill with INK-0 text (buttons only), 50 ms. |

Element notes:
- **Plates** (grid, stash, ware, ribbon): hover, focus, held, valid, invalid apply. Locked or unaffordable states never change the plate's tier edge.
- **Empty cells:** hover, valid and invalid only.
- **Intent tabs:** focusable (inspect); hover and focus rings as above.
- **Buttons, toggles, segments:** all states.
- **Selected versus hover** differ by the corner square. **Focus versus hover** differ by stroke width.
- **Audio** cues for invalid, confirm and speed belong to the audio direction; the visual spec only requires that a non-visual cue exists.

### 7.12 UI animation feel

**Character:** crisp, tactile, a little heavy. Whole-pixel positions, no tilting, no smooth scaling, no overshoot. All timings at 60 fps. **Rises of luminance are at least 100 ms (6 frames) at any speed setting** (4.12 rule 5).

**Avatars, chips, intent glyph and queue timings live in 5.10.6 and 5.11. This table does not repeat them.**

| Event | Visual | Duration | Notes |
| ---- | ---- | ---- | ---- |
| Drag lift | Rises 2 px, shadow, ring fades in | 100 ms ease-out | |
| Drag follow | Integer positions at the cursor | one frame | Responsiveness first. |
| Valid ghost | Solid outline | 0 ms on cell change | |
| Rotate | 3-step redraw of the plate; the icon does not move | 66 ms, no easing | Snap, not spin (3.4.3). |
| Snap | Slide to origin, 1 px thunk, tier-edge pulse | 83 ms, 33 ms, 150 ms | Small triggers stay quiet. |
| Invalid release | Straight return to the origin; 2 px shudder (two cycles) | 150 ms return, 160 ms shudder | The item returns; it is never destroyed. |
| Cancel | Same return path as invalid | 150 ms | |
| Bridge preview | Rest to preview | 100 ms in, 80 ms out | 7.6 |
| Bridge on drop | Preview holds, then fades | 400 ms hold, 200 ms fade | |
| Popup | Appears after delay; swaps content instantly when the pointer moves to an adjacent object; expands in place on Details | 300 ms delay (instant on focus), 100 ms fade in, 60 ms grace, 60 ms fade out, 100 ms expand | Placed once when it opens; it never moves while open (7.9). |
| Hero switch (planning) | Workbench cross-fade; held plate stays in the hand | 100 ms | |
| Grid follow (combat) | Fade-in of the acting hero's grid | 100 ms, not speed-scaled | 7.5 |
| Door-card reveal | Cards rise 8 px with tier light; pips tick on; the tag drops last | 240 ms per card, 80 ms stagger, 300 ms light swell, 40 ms per pip, 120 ms tag; about 640 ms | Input unlocks when the tag lands; any click skips. |
| Card arm | Ring and corner square | 100 ms | |
| Card confirm | The other two fall to ink, then the eased state change | 250 ms or more (2.2) | |
| Result panel | Rows step in, top rank first | 40 ms stagger, bars grow over 200 ms | Skippable. |
| Button press | Candle fill | 50 ms | |
| Number pop (dealt) | One numeral size up for 2 frames, settle, rise 12 px | 33 ms pop, 600 ms rise, 200 ms hold, 150 ms fade | Stagger 100 ms and 4 px laterally. |
| Payoff number | The 48 px numeral lands at the flank, steps up through three sizes | 100 ms steps, 600 ms hold | Inside the 1.5 s peak (4.10). |
| HP change | Fill steps down, Blood-D lost segment holds then clears | 200 ms | No hue change. |

### 7.13 Accessibility visuals and settings

These must be recorded in `design/accessibility-requirements.md`, which does not exist yet.

| Setting | What it does |
| ---- | ---- |
| **Text scale** | Two **authored size sets**, no scaling: *Standard* (Body 12, Heading 16, Display 24) and *Large* (Body 16, Heading 20, Display 24). Applies to prose and labels (popups, panels, buttons, menus, result list), not to fixed-geometry numerals (8 px digits are 24 px at 3x, above the floor). Standard ships first; Large is authored before release and is only offered if the chosen font supplies or can be given a 16 and 20 cut. Layouts keep 30% spare height for Vietnamese and for Large. If the hi-res overlay is the fallback, this setting maps to its scale instead. |
| **Tier badge** (off by default) | An 8 px digit (1 to 6) in a chip on the plate's bottom edge lane, in addition to form. The **tier name is always in words** in the popup and the focus readout regardless (luminance order is not a usable backup). |
| **Reduced flashes** | Per 4.12: bloom becomes a steady glow, avatars fade in without the tier pulse, rings appear at final radius in 3 steps. Recommended on by default until the first player-testing pass. |
| **Reduced motion** | Removes shudder, thunk, number pop and card rises (replaced by straight fades of the same duration), idle breathing and chip rise/sink (the chevron and sign still carry direction), and replaces avatar travel (Swing, Throw, Status hops) with a 100 ms cross-fade at the target. Snap still snaps. Section 5 owns the avatar-motion wording. |
| **Remapping and prompts** | Full remapping of every action, with prompt chips showing the *current* binding for keyboard and gamepad. A rebind row in capture mode shows a Candle 2 px frame and a "..." label. |
| **Input rules visible in art** | No timed input in planning, so no countdown visuals and no urgency pulses. Pick-up and place work without holding a button (click to pick, click to place, or drag), with the same held state. R-rotate has a mouse alternative and a gamepad button, and the rotate prompt chip appears next to the held plate. |

### 7.14 Alternatives considered

| Topic | Alternative | Why rejected |
| ---- | ---- | ---- |
| Tooltip | **Docked inspector** (user rejected: too much information) | A fixed 286 x 160 band held every keyword and every definition at once, took about half the right column, and forced the enemy zone to about 154 px. Replaced by the hover popup with essentials only (7.9). |
| Tooltip | Cursor-following popup | It covers the cell the player is about to read next and has no stable place to pin. The popup is placed once, beside the object, and stays still. |
| Popup | Auto-expanding after a dwell | Brings back the information load the user rejected and changes the popup's size under the pointer. Expansion is an explicit Details action. |
| Popup | Popup on the stage over the enemy lines | The tabs and HP rows of two independently centred lines leave no rectangle for it; stage objects get a chip and the full text lands in the workbench (7.9). |
| Popup | Dimming enemy tabs and HP under a popup | Binary alpha means covering; it breaks the protected layer (Principle 3). |
| Enemies | Standing in horizontal rows or scattered | Rejected: each line is a vertical column read top to bottom, like the heroes' column. |
| Enemies | One line centred in the right column | With one line it leaves two narrow strips that fit nothing and lengthens every crossing; adjacent to the party column keeps both lines' crossings shortest. |
| Enemies | One line flush right | A 154 px gutter at C = 24 and longer crossings; the lines stop reading as facing the party column. |
| Enemies | A third line | Needs at least 360 px; no C fits it (right column 294 / 326). |
| Enemies | A sub-column inside a line | Breaks "vertical column". Reinforcement fills empty slots instead (7.4a). |
| Tooltip | Nested hover for keyword definitions | Cannot be reached by keyboard or gamepad, and violates "no nested hover". |
| Cards | Cards spanning both stage columns | Moves the party or hides HP. Glyph-first cards in the right column, with the popup beneath them, fit. |
| Cards | Prose on 84 px cards | About 13 characters per line, unreadable in Vietnamese. |
| Turn order | A second numeral on each pedestal | Collides with the join numeral. |
| Turn order | The Candle chevron on the pedestal pre-combat | The chevron means "acting now", and pre-combat nobody is acting. |
| Overview | Hold-only overview | Hard to read while also dragging, and not keyboard-friendly. |
| Overview | Per-hero compact readouts in the party column | Rejected by the user (switch-only). |
| Overview | A full-screen modal overlay | Hides the grid and enemy intent. |
| Text | A single typeface with 2x to 4x scaled numerals | Strokes thicken in integer steps and outlines go inconsistent. |
| Text | A vector sans for Vietnamese | Breaks pixel integrity. Last resort only: the hi-res overlay (7.7). |
| Icons | Outline-only icons | Hollow at 8 px, ambiguous in greyscale. |
| Icons | Coloured item icons (reference 2) | Argued against here for competing with the tier signal, but **adopted by the user in Section 8.5.3** (free palette), with the tier read from plate form first and a saturation cap as the first remedy if tiers stop reading. |
| States | Hover and focus identical | Fails keyboard and gamepad users and breaks "each state differs by form". |
| HP | Numerals inside the bar | Bone on Blood is about 3.6:1. |
| Invalid | A red ghost or flash | Red is the Red tier's hue. |
| Grid follow | A manual-only grid in combat | The plate echo (5.10.9) would be invisible. |

### 7.15 Rule and pillar coverage

| Decision | Section 1 rule or principle | Pillar |
| ---- | ---- | ---- |
| Ink chrome, lit information, stateful action strip (7.1, 7.3) | The rule; Principle 1 | P4 |
| Draw order and anchored HP block (7.2, 7.4) | Principle 3 | P2, P4 |
| Slot stack with empty avatar zone (7.4) | Principle 3 | P2 |
| Intent type, magnitude, target (7.5) | Principle 1 | P4 |
| Order strip, grid follow, result panel (7.5) | Principle 3 | P2, P4 |
| Bridge states, overview, cross-hero cues (7.6) | Principle 1 | P4 |
| Vietnamese-gated type and numeral face (7.7) | Principle 1 (text must be read) | P2 (numerals escalate), P4 |
| Glyph system (7.8) | Principle 2 | P4 |
| Hover popup and its placement rules (7.9) | Principle 3 (nothing covers HP, intent or order) | P4, P5 (cost in the same popup) |
| Enemy lines, slot stack and gutter crossing (7.4a, 7.5) | Principle 3 | P2, P4 |
| Cards, shop, recruit (7.10) | Principle 4 | P5, P4 |
| State table (7.11) | Principle 2 | P4 |
| Animation (7.12) | Eyes-first rules (2.2); 4.12 | P2 |

### 7.16 Dependencies, open items and verification status

**Depends on Section 8:** cell size C (24 assumed here; 20 is Section 8's provisional value); native 640x360 and integer scale; the 8 px digit cut; font size cuts (8, 12, 16, 20 for Large, 24); line pitches; the numeral face sizes; the keyword, stat, status and intent glyph counts; the prompt keycap set; 1x1 avatar glyphs; integer row shear (5.12). The free width beside line 1 (154 or 186 with one line) and the hero slot pitch follow C; the popup width (152), the enemy line width (140) and the enemy slot pitch (body + 56) do not.

**Amendments approved with this section (applied in the same pass):**
- 3.7: three bridge states (7.6).
- 4.2 and 4.6: HP track becomes INK-0, Blood-D becomes the lost-HP trail.
- 4.9: focus is 2 px (hover stays 1 px), the selected corner square, and the unaffordable-price form.
- 5.8: slot pitch gains a 12 px stat row.
- 5.10.7: speed scales holds and cadence only; luminance envelopes keep the 100 ms floor.

**VERIFIED by tool:** the contrast figures in 4.17 (Blood on INK-0 3.77:1, Fog on INK-2 4.66:1, and so on). **Layout (planning mock-up, 2026-11-10):** the 7.3 layout, the two enemy lines, the popup and the hero slot pitches fit at 640 x 360 at both C = 24 and C = 20; the compact popup lands in the workbench without covering any protected element. The mock-up is a greybox, so the price numerals, the real text and the combat and door states are still unverified, and the paths from line 2 across line 1 are not drawn. The Vietnamese render test has not been run on any font. The photosensitivity thresholds (4.12) are not verified against the current WCAG text.

**Open:**
- **Closed:** the enemy layout. One or two vertical lines, at most three enemies each, 140 px wide, line 1 against the party column (7.4a). **Still open:** the swarm and boss-companion rules, the combat GDD's use of the two lines (no gameplay or art meaning is allowed until the art is changed), and the hop counts for paths of about 375 px (Section 5.11 raised to 4 to 8).
- The magnitude bands for numeral size; the second currency glyph; the thousands separator convention.
- Whether order is dynamic by speed or a fixed hero sequence (changes the order strip's content, not its layout).
- Resting-bridge colour (Fog) is borderline on the 4.10 lit threshold: **measured, Fog luminance is 0.2990 against a threshold of 0.30**, so resting bridges are not counted as lit. Any palette change toward a brighter Fog moves them into the budget.
- The planning lit budget: **measured with a compact popup open at 3.59% (C = 24) and 3.24% (C = 20)**, far under 12%, so **the budget stays 12% and is not raised**. A pinned expanded popup (about 2.5% lit by estimate) is still to be measured.
- Font candidates, unverified and to be tested: FatPix (itch.io, ngndang), Tonemark Pixel (paid; vendor claims full Vietnamese coverage), and a Vietnamese 8x24 pixel font pack on itch.io (licence not stated).
- `design/accessibility-requirements.md` must be created and carry 7.13.

## 8. Asset Standards

> **Status.** Approved by the user on 2026-11-10, in two parts: **Part A** is the art-director's half (preferences and pipeline) and **Part B** is the technical-artist's half (engine hard limits). Where an art preference and a technical limit disagree, both are recorded with the trade-off chosen (8.16).
>
> **User decisions:** native resolution 640x360, integer scale; native pixel font gated by a Vietnamese render test; Vietnamese and English; the icon on every 1x2 and L plate is **one compact 16x16 glyph** (option A, 8.5.2); item icons may use a **free palette** (8.5.3).
>
> **Defaults adopted unless the user changes them:** the L item is three cells in a 2x2 box; no non-integer "fill window" option; windows of 1280x720 or larger with bleed; snapping mode UpscaleRenderTexture (gated on a spike); world stage plus an Overlay-canvas protected layer (gated on spike S1); effect rings drawn into runtime textures; sprites loaded by serialized references plus atlases; effect colour sets Bone for the party and Fog-hi for enemies.
>
> **Still provisional until a spike passes:** the cell edge C (20, fallback 24), the snapping mode, the canvas architecture, every draw-call and fill estimate, and every number marked PROVISIONAL.
>
> **Source tags in Part B.** **[REF]** = stated in `docs/engine-reference/unity/`. **[PKG]** = read from the installed package source or docs in `Library/PackageCache` (URP 17.6.0, ugui 2.6.0), used where [REF] is silent; downgrade these to NOT SOURCEABLE if only [REF] is accepted. **[PROJ]** = a project file. **[CALC]** = arithmetic. **[PROPOSED]** = a suggestion, not a sourced fact. The project states only 60 fps, 16.6 ms, about 500 draw calls and about 1 GB. Every other limit below is derived or proposed.

# Part A: Art direction

Notation: **[TA]** means Part B owns or must confirm it. **[PROVISIONAL]** means the number is tuned on the first full-frame mock-up. **[DECISION PENDING]** means the user decides, and a safe default is stated.

### 8.0 Numbers at a glance

| Item | Value |
| ---- | ---- |
| Native resolution | **640 x 360**, integer zoom only (user decision, final) |
| 1x1 cell edge C | **20 px** (provisional until the icon spike in 8.5.6 passes; fallback 24) |
| Plate interior | C - 4 = **16 px** (1 px tier edge plus 1 px lit frame per side) |
| Compact glyph `c` | **16 x 16** (the glyph fills 12 to 14 px of it) |
| Avatar | 1x1 plate at C, canvas 22 x 22 with its shadow; avatar zone above a hero is C + 4 = **24 px** |
| Hero body H / canvas | **32 / 48**; hero plus prop at most 1.25 H = 40 px |
| Pixels per unit | One global constant. I propose **20** (1 cell = 1 unit). **[TA]** |
| Runtime image | PNG, RGBA 8-bit, **binary alpha**, Point filter, no compression, no mips, Full Rect |
| Animation files | **One PNG per frame** (no sheets) |
| Text | Native pixel font, Vietnamese and English, gated by the 8.9 test |
| 2D polygons, LOD, rigs, texture tiers | Not applicable |

### 8.1 Native resolution, camera and scaling

**Decision (user, final):** the internal resolution is 640 x 360, shown at an integer zoom `z = max(1, min(floor(W/640), floor(H/360)))`.

| Display | z | Visible world |
| ---- | ---- | ---- |
| 1280 x 720 (minimum) | 2 | 640 x 360 |
| 1920 x 1080 | 3 | 640 x 360 |
| 2560 x 1440 | 4 | 640 x 360 |
| 3840 x 2160 | 6 | 640 x 360 |
| Non-multiple window or display | floor | More than 640 x 360, up to 1.5x per axis at the 720p minimum |

**No letterbox, so every edge needs bleed.** The camera uses `CropFrame.None`, because any other mode clears the back buffer to pure black, which breaks the ink floor in 4.3. The camera background colour is INK-0 (`#0B0F1C`). The 640 x 360 area is the **safe frame**. Everything outside it, up to 960 x 540 centred, is the **bleed frame**.
- Layout and UI are authored in the safe frame only.
- The bleed frame shows a `surround`, one authored asset (8.8).
- Panel frames do not extend outward.
- **Beyond the bleed frame** (for example a 32:9 window at z = 4 shows 1280 px of width): the camera clear colour INK-0 shows. The surround is a flat INK-0 field, so nothing else is needed, no UI is extended, and the ink floor (4.3) holds because INK-0 is the floor itself, not pure black.

**Implications at C = 20**

| Quantity | Native px | At 1080p (3x) | At 720p (2x) |
| ---- | ---- | ---- | ---- |
| 1x1 cell pitch | 20 | 60 | 40 |
| 1 px outline | 1 | 3 | 2 |
| 4x4 backpack | 80 x 80 | 240 | 160 |
| 6x6 expanded backpack | 120 x 120 | 360 | 240 |
| Stash, 8 cells wide | 160 | 480 | 320 |
| Hero body / canvas | 32 / 48 | 96 / 144 | 64 / 96 |
| Avatar plate | 20 | 60 | 40 |
| Panel frame | 3 px + 1 px highlight | 12 | 8 |

**Hero slot stack** (5.8, Section 7.4): C + 4 (avatar zone) + 1.25 H (40) + 8 (pedestal) + 10 (HP row) + 12 (stat row) is **94 px per slot** at C = 20. Three slots are 282 px of the 314 px stage, so they fit with little margin. **Enemy slot stack** (Section 7.4a): 26 (intent tab zone) + body + 4 + 4 (shadow and turn row) + 10 (HP row) + 12 (stat row) is **body + 56 px**, independent of C: 80 to 84 (small), 92 to 100 (medium), 108 to 116 (large), 152 to 171 (boss). At most three enemies per line, two lines, the sum of pitches at most about 300 per line.

**Columns.** Section 7.3 owns the layout and its formula is the **single source** (left workbench `n_columns x C + 14`; the stage is split about 22% party and 46% enemies and room panel; 206 / 140 / 294 at C = 24 and 8 columns, and 174 / 140 / 326 at C = 20; each enemy line is 140 px, line 1 against the party column, so two lines are 280 px of the 294 or 326 px right column). The numbers in this section are not independent. Cross-hero visibility is switch-only, so the left column shows one grid at a time plus the stash. The middle column must hold a 48 px hero, its pedestal, the avatar and a second avatar one avatar width toward the enemy lines, HP and the turn chevron; the right column (about 300 px tall, no reserved band) must hold three door cards with the popup beneath them (Section 7.10: 140 + 4 + 140 = 284 px), or two 140 px enemy lines side by side (a 128 px boss canvas fits one line, about 171 px with its tab and HP block).

**Runtime rules for pixel art**
1. All motion is in whole native pixels, including particles, avatars and screen shake (1 to 2 px steps). How this is enforced depends on the snapping mode in 8.1.1.
2. The only runtime rotation is by multiples of 90 degrees, and only for plates in the grid. Avatars never rotate (they lean by baked shear, 8.5.5). VFX use authored frames, never rotation.
3. No runtime sprite scaling. Text and numerals follow 8.9.
4. One material for all pixel art: Sprite-Unlit, a white Global Light 2D, no tonemapping, no vignette, no bloom. Lighting is baked into the sprites (4.13). **[TA]**
5. The master palette source of truth is the `.gpl` (8.2). The prototype `Palette.cs` is replaced by a file generated from the `.gpl`.

#### 8.1.1 Snapping mode: PixelSnapping or UpscaleRenderTexture

| | PixelSnapping | UpscaleRenderTexture |
| ---- | ---- | ---- |
| What it does | Sprite renderers snap to the pixel grid. The scene renders at screen resolution. | The scene renders on the native grid and is point-upscaled. |
| Particles, lines, shake | Can move in sub-pixel steps unless every system is snapped by hand | Quantised to whole native pixels automatically |
| Bloom, if it existed | Smooth and high-resolution | Chunky (every blur step is a block) |
| Glow and ladder | Stepped shells identical in both modes | Identical in both modes |
| Escalation ladder | Tick and Chain (1 to 2 px edges and rings) identical | Same; Surge and Detonation gain the whole-pixel guarantee for free |

The ladder is built without post-processing, so the mode changes nothing the ladder depends on, except that sub-pixel particle motion is a risk in PixelSnapping. **Adopted default: UpscaleRenderTexture**, gated on the UI-alignment spike (S1). It makes 5.5 (whole-pixel motion), 5.8 (one pixel size on screen) and 5.12 true by construction. The cost is that bloom, which is rejected anyway, would be chunky. Fallback: PixelSnapping with manual snapping through `PixelPerfectCamera.RoundToPixel` (Part B 8.B.1).

#### 8.1.2 Effects over the grid

The UI uses an Overlay canvas, drawn after the final blit. World-space particles and sprites draw behind it, which matches 5.10.4 (rings pass behind HP rows, the bloom is beneath the protected layer).
- The only art that must appear over the grid is the plate pulse, the echo, and the Chain threads from source plates (5.10.9). The pulse and echo are canvas sprite Images.
- **Threads are built in two legs** (Part B 8.B.4): a canvas-native leg inside the grid column, and a world leg to the avatar. The legs meet on a whole native pixel and leave through the gutters, never covering neighbouring plates (3.7).
- Avatars, rings, smears, impacts and chips remain world-space sprites beneath the canvas.
- The canvas scale is locked to z. Screen Space - Camera is a fallback only if spike S1 shows an alignment problem.

### 8.2 Formats, colour mode, palette

| Stage | Rule |
| ---- | ---- |
| Source | `.aseprite`, indexed colour mode, locked to the master palette |
| Export | PNG, RGBA 8-bit, straight alpha, sRGB. **Alpha is 0 or 255 only.** |
| Palette files | `pal_master_v01_48.gpl` (source of truth) and `pal_master_v01_strip.png`. 41 colours in use, 7 reserved, cap 48. |
| Import preset | Filter Point, compression None, mips off, Read/Write off, Mesh Type Full Rect, one global PPU, maxTextureSize at least the longest edge. **[TA]** enforces it with an AssetPostprocessor. |

- **Palette changes are breaking.** Any change bumps `v01` to `v02`, and every asset is re-quantised and re-reviewed.
- **Runtime soft alpha is whitelisted.** The sanctioned cases are every wash and pool listed in 4.8 (the Candle column wash and its payoff swell up to 30%, the Fog 10% cold wash, the door-card tier floor wash of 10 to 15%, the event Fog wash of 8%, the shop Candle wash of 14%, the recruit pool of 18%, the victory Candle wash of 22%, the defeat Fog wash of 8%, and the Red-room Wine shift, or a palette-remap LUT if **[TA]** prefers), the card and avatar breathing of at most 6% amplitude at 0.5 Hz or slower (2.2 rule 2), and the fades and transitions defined in 2.6, 5.7 and 7.12. Every other effect uses ramp steps, not alpha. If a state is added to Section 2 or 4, it is added to this list in the same change.
- **On-screen palette audit.** A capture of a washless test scene is point-downsampled and checked: every pixel must be in the master palette. This catches stray tonemapping, vignette, tint or semi-transparent colours. The capture is retained in `production/qa/evidence/`.
- **2D, so none of the following apply:** polygon budgets, LOD levels, texture resolution tiers, material slots. A sprite is one Full Rect quad. There is one resolution and no `@2x`.

### 8.3 Naming convention

Pattern: **`{cat}_{name}_{variant}_{size}.{ext}`**. This is exactly four underscore-separated fields, lowercase `a-z0-9`, with hyphens allowed inside a field and no other punctuation.

Master regex:
`^(char|mon|boss|npc|item|plate|card|prop|env|ui|vfx|font|pal|anch)_[a-z0-9]+(-[a-z0-9]+)*_[a-z0-9]+(-[a-z0-9]+)*_[a-z0-9]+(-[a-z0-9]+)*\.(png|gpl|json|ttf|otf)$`

A script also checks field 3 and field 4 against the category vocabulary:

| cat | name | variant | size |
| ---- | ---- | ---- | ---- |
| `char` (hero) | role family id | `idle hurt down narrow` | frame `01`, `02` |
| `mon` | monster id | `idle act hurt die` | frame `01`, `02` |
| `boss` | boss id | `idle act hurt die` | frame `01` to `04` |
| `npc` | npc id | `idle react` | frame |
| `item` | item id | `c d struck lean-l lean-r` | `1x1 1x2 2x2 l` |
| `plate` | `white green blue purple gold red` | `rest hi lift lifthi liftdim` | `1x1 1x2 2x1 2x2 l0 l1 l2 l3` |
| `card` | tier | `frame pip tag` | `9s` or pixel size |
| `prop` | prop id | `base` | `WxH` |
| `env` | room id or `surround` | `bg mid fg tile prop ink` | `WxH` |
| `ui` | element (`btn panel chip stat-atk chevron-buff`) | variant | `default hover pressed disabled focus 9s` or `WxH` |
| `vfx` | `swing throw status detonate impact` | `smear spark trail mote burst` (add `-foe` for the cold set, `-calm` for the reduced-flash set) | frame `01` and up |
| `font` | family | `regular num` | pixel height (`8 12 16 20 24 32 48`, from the font table in 8.9) |
| `pal` | `master` | `v01` | `48` or `strip` |
| `anch` | role family | `props` | `v01` (prop anchor table, `.json`) |

Examples: `char_tank_idle_01.png`, `mon_ghoul_act_01.png`, `item_iron-sword_c_1x2.png`, `plate_green_lift_1x1.png`, `vfx_swing_smear_02.png`, `font_pixelname_regular_8.ttf`.

Rules:
- **Tier is never in an icon's name.** The plate carries tier. An item whose art changes at a higher tier is a different `name`.
- **Avatar glyph:** `item_{id}_c_{fp}`. For a 1x1 item this is the icon itself. For a multi-cell item it is the compact glyph, which also serves as the grid icon on its plate (8.5.2, option A). The `fp` token is the item's footprint, and the `c` variant fixes the canvas at 16 x 16.
- **Banned in names:** `final`, `new`, `copy`, `v2` (except the palette version), spaces, and capitals.
- **Grid and avatar states:** `hi` is the plate with its frame at the Highlight value, used for the pulse and echo. `lift` is the plate with the 1 px lower-right shadow, used for dragging and for avatars. `lifthi` and `liftdim` are 1x1 avatar-only states for pop-in and exit.
- **Unity scripts, prefabs and scenes** keep PascalCase from the technical preferences.

### 8.4 Canvases, pivots, padding, animation

- **Dimensions** are even by default. Exceptions are listed with a whole-pixel pivot in texel coordinates: lift plates (footprint + 2), selection overlays (footprint + 4), chevrons.
- **Margin.** At least 1 px of transparent pixels on every side of every frame, except plates, 9-slices and tiles, which are full-bleed.
- **Canvas classes for characters:** 32, 48, 64, 96, 128 (square), declared per asset id in the manifest. All frames of an asset share the same canvas, and the validator checks it.
- **Pivots**

| Asset | Pivot |
| ---- | ---- |
| Hero, monster | Bottom-centre at the ground line, 4 px above the canvas bottom, the same row in every frame |
| Boss | Same, 8 px above the bottom |
| Plate, glyph, card | Canvas centre |
| Lift plate | Centre of the plate (not the canvas), whole-pixel, e.g. (10, 10) on 22 x 22 |
| Tile | Bottom-left |

- **Key frame counts** (5.6, locked): per role family 6 (idle 2, hurt 2, down 2); ordinary monster 6 (idle 2, act 1, hurt 1, die 2); boss 9 (idle 2, act 2, hurt 1, die 4); shopkeeper 2 plus 2 react; event figure 2; elite and recruit 0 new.
- **Ready is a code state** (idle frame 1 with narrow eyes and a 1 px lift). The narrow eyes are three tiny overlay images (`char_{family}_narrow_01`, transparent except the eye pixels), not key frames. **[TA]** to confirm the overlay or an equivalent.
- **Per-frame PNGs, no sheets.** With at most 4 frames per state this adds roughly 250 small files and removes slicing and grid validation. It matches the examples in 5.6 and 5.11, and the prototype importer's Single mode. The Sprite Atlas packs them. Hold times live in data (5.6). Every duration is a multiple of 1/60 s and no key frame is shorter than 80 ms at 2x.
- **Stability rules:** ground line, pivot and outline thickness never change between frames. The idle loop is seamless. The party shares one idle clock (5.6).

### 8.5 Items: icons, glyphs, plates, avatars, lean

#### 8.5.1 Icon canvases

| Variant | Canvas | Use |
| ---- | ---- | ---- |
| `c` compact | 16 x 16 | 1x1 icon, **the avatar glyph for every item**, and the grid icon on 1x2 and L plates |
| `d` detailed | 32 x 32 | 2x2 grid icon only. It is rotation-invariant, so it does not touch 3.4.3. |
| `struck` | 16 x 16 | Optional hand-drawn pose for at most about 10% of items |

Formulas, so a change to C is mechanical: `c` = (C - 4)^2, `d` = (2C - 8)^2, lift plate = (C + 2)^2, selection = (C + 4)^2.

The `c` glyph and the `d` icon of one item depict the same subject and silhouette family. The avatar uses `c`, so the cause link in 5.10.9 holds.

#### 8.5.2 The icon on 1x2 and L plates (decided: option A)

An upright icon on a rotating 1x2 plate must fit both interiors (16 x 36 and 36 x 16), so one image is at most 16 x 16. For an L the four rotations share no common area.

**Decision (user): option A.** One compact `c` glyph (16 x 16) sits centred on every 1x2 plate and on the elbow cell of every L plate. Section 3.4.3 is unchanged. The grid icon equals the avatar glyph, so the cause link is exact and no extra art is needed. The cost is that a 1x2 plate looks under-filled (about 10 px blank above and below). A `c` that fails the outline-only test is redrawn, not replaced by a bigger icon. Option B (redrawn 16 x 32 and 32 x 16 icons per orientation) is reserved for a need shown by the mock-up, and would need an amendment to 3.4.3.

#### 8.5.3 Item icon colours (decided: free palette)

**Decision (user): item icons may use any colour of the master palette.** Section 7.8 and Section 4.5 are amended accordingly.

| | Consequence |
| ---- | ---- |
| Rule 4.5 | "Saturated hue means tier" **no longer holds on item art**: a red potion or green gem is allowed. The tier is read from the plate's **form first** (3.4.4, Principle 2) and from the thin lit edge second. |
| Quantise target | The full 41 colours. AI drift is wider than it would be with a restricted subset. |
| Risk | Hue competes with tier edges on every plate and avatar, and Candle ("you") may appear in an icon. |
| Check | The first mock-up and the icon spike (8.5.6) must show that each of the six tiers still reads at 3x and 2x, in greyscale and with an icon that shares its hue (for example a red icon on a Red plate). If they do not, the first remedy is to cap icon saturation at 0.60 (the 4.1 rule for non-tier colours), not to restrict hue. |

#### 8.5.4 Plate and avatar authoring: generated offline

**Decision:** a deterministic script generates plates from the 3.4.4 cue rules and the master palette. The PNGs are committed. Hand overrides are flagged in the manifest. No plate is composed at runtime.

| Set | Contents | Count |
| ---- | ---- | ---- |
| Grid plates | 8 shapes x 6 tiers x {`rest`, `hi`} | 96 |
| Lift plates | 8 shapes x 6 tiers (dragging, 1 px lower-right INK-0 shadow) | 48 |
| Avatar states | 1x1 x 6 tiers x {`lifthi`, `liftdim`} | 12 |
| Selection and ghost overlays | 8 shapes x a state count owned by Section 7 | about 16 to 24 |
| **Total** | | **about 170 to 180, each under 2 KB** |

- **Avatar composition:** an avatar is `plate_{tier}_lift_1x1` (or its `hi`/`dim` states) plus `item_{id}_c_{fp}`. Zero new art. The avatar is therefore two sprites: the plate (with its shadow baked in) and the glyph (which alone is sheared, 8.5.5).
- **Pop-in** is 100 ms (6 frames): `liftdim` for 2 frames, `lift` for 2, `lifthi` for 2. **The avatar then settles to `lift` (the rest value, ladder rank 3, 5.13) for the rest of its life**, so it does not end on its brightest state. Exit steps down two ramp steps. Reduced flashes skips `lifthi`.
- **Omitted on avatars:** rank pips and the curse tag (5.10.2).
- **Shapes** are `1x1`, `1x2`, `2x1`, `2x2`, `l0` to `l3`. The generator takes the shape from a cell list, so a different L costs a script run (8.5.7).
- **Cards** reuse the same generator for corner pieces (8.8).
- **Tooling** lives in `tools/art/`. **[TA]**

#### 8.5.5 Lean by integer row shear (art-pipeline requirement)

A script bakes the lean frames from the `c` glyph. Input is `item_{id}_c_{fp}`. Output is `item_{id}_lean-l_{fp}` and `item_{id}_lean-r_{fp}`, committed with a `generated` marker. **Only the glyph is sheared, never the plate**, because a 2 px shear would distort the 2 to 3 px chamfers that carry the tier cue (Part B 8.B.3).

Algorithm, with no resampling:
1. The pivot row defaults to the bottom row of the glyph's bounding box. A data row may override it.
2. Rows above the pivot shift 1 px sideways. Rows in the top quarter of the bounding box shift 2 px. The direction is mirrored for `lean-r`.
3. Each row keeps its pixel sequence unchanged, only offset.
4. The output canvas is widened by 2 px on each side, which Part B 8.B.3 sets out as the formula.

The script rejects a lean frame, and sets lean to off for that item with a log line, if any of these fail:
- Row sequences differ from the source (a resample).
- A pixel is outside the master palette.
- An opaque pixel leaves the canvas.
- The 8-connected component count changes (the outline fragmented).

Light direction holds because shear does not change which side is lit. The fallback is arc plus smear only (5.12).

#### 8.5.6 Icon spike (gate before C is final)

Generate and clean 10 varied 1x1 `c` glyphs through the AI pipeline. Include a weapon, potion, tome, armour and trinket, **at least three using hues that match a tier edge**, and run the lean bake on at least 3. Place them on all six plates at 3x and 2x, as avatars and in the grid, in greyscale and in colour. If four or more fail the outline-only read, move to C = 24 and revise 8.1 (glyph 14 to 16, 4x4 grid of 96 px, an 8-wide stash of 192 px, which just fits the 196 px column).

#### 8.5.7 L footprint

Default: a **three-cell L in a 2x2 box**. A four-cell L (2 x 3, possibly with a mirrored J) changes plate shapes and the avatar placement rule. The generator makes the wrong guess cheap to fix. Confirm against the item GDD.

### 8.6 Characters, monsters, bosses, props

| Class | Body height | Canvas |
| ---- | ---- | ---- |
| Hero | 32 (30 to 34); body plus prop at most 40 | 48 |
| Monster, small | 24 to 28 | 48 |
| Monster, medium | 36 to 44 | 64 |
| Monster, large | 52 to 60 | 96 |
| Boss | 64 or more (96 to 115 preferred) | 128 (96 only for a body of 83 px or less) |

These are provisional until the enemy roster is reconciled with 3.3.3.

**Canvas headroom rule (PROVISIONAL).** A canvas must hold the body height, the pivot offset (4 px; 8 px for a boss), at least 1 px of transparent margin at the top, and the highest point of any act pose (a raised limb adds about 4 px). So a monster canvas is **at least body + 9 px** (a boss **body + 13 px**). A hero (which has no act pose) needs body plus prop (up to 40 px) + 4 + 1 = 45, which fits the 48 canvas. The canvas classes in the table above follow this rule, so a small monster uses the 48 class, a medium the 64 class, a large the 96 class and a boss the 128 class (a boss body of 83 px or less could use 96). The manifest records the canvas class per asset id and the validator checks it. The larger classes add only a few hundred KB to the memory estimate (8.B.9), which is not binding.
- **Rims, outline and steps:** 1 px Candle rim on heroes and 1 px Fog rim on monsters, on the key side. 1 px outline: INK-0 on heroes, recruits and NPCs, INK-4 on monsters and bosses (5.9). Three value steps per material. Internal colour regions at most 6 per hero and 5 per monster (5.8).
- **No detail smaller than 2 x 2 px**, except outline, rim and eyes. A swarm member is redrawn, never resized.
- **Props** are static overlays: `prop_{id}_base_{WxH}`, canvas at most 24 x 24. A per-frame anchor table `anch_{family}_props_v01.json` gives head, hand and back as integer canvas coordinates for the six hero frames. A new recruit costs one prop image and its anchors, and no new body frames. The validator checks that every anchor is inside the canvas. **[TA]** confirms the overlay and rim-mask method.
- **Recruits** reuse the idle sprite and need no portrait.

### 8.7 Effects, chips and glyphs

Shared effect art, from 5.11:

| Art | Frames | Canvas |
| ---- | ---- | ---- |
| `vfx_swing_smear` crescent | 3 | 32 |
| `vfx_impact_spark` | 2 | 16 |
| `vfx_throw_trail` | 2 | 16 |
| `vfx_status_mote` | 2 | 8 |
| `vfx_detonate_burst` | 4 (plus a `-calm` variant of every payoff effect, about 5 more frames in all) | 48 |
| Total authored | 13 | |
| **With the cold `-foe` set** | **about 26** | |

- **Colour sets (adopted default).** Frames are authored on a 3-colour mask ramp. A script recolours them offline, which avoids runtime tint (an off-palette product). The set is Bone for the party's effects and Fog-hi for enemies' (5.13). Tier colour appears on the avatar edge and the ring only. Tier-coloured smears and sparks would multiply the frames by six and are not adopted.
- **Ring:** code-drawn, outline only, integer radii, about 6 steps over 360 ms, 1 px at Tick and 2 px at Chain and above. It steps down the ramp (Highlight, Base, INK-4) with no alpha. It is drawn per step by an integer rasteriser (Part B 8.B.5) into a small runtime texture with Point filter. Maximum two rings live. **[TA]**
- **Small glyphs** (about 25 to 30 drawings, each with a 12 px master and an 8 px reduction; the count depends on the roster: at least 4 intent, 3 or more stat, about 10 to 12 status and about 7 UI symbols, Section 7.8):
  - Stat glyphs, 8 x 8: `ui_stat-{id}_base_8x8`.
  - The Bone droplet, the Fog shield, and similar.
  - Intent glyphs: attack, guard, buff, debuff, 12 x 12, Fog-hi, no plate (5.10.8). Provisional, to match the pennant tab.
- **Stat chip:** `ui_chip_base_9s` (INK-1 bed, 1 px INK-4 frame, 12 px tall, 3-slice horizontally). Contents are all Bone: an 8 x 8 stat glyph, a 6 x 4 chevron (`ui_chevron-buff_up_6x4`, `..._down_6x4`) and a signed number in the small numeral font. Two chips stack to 26 px, which fits the body band. The chip rises or sinks 6 px in code.
- **Lit area and photosensitivity:** these effects stay in small areas (an avatar is about 24 x 24 px). Chains are included in the verification capture (4.12 item 7).

### 8.8 Backdrops, UI frames, 9-slice, cards

- **Backdrops.** Layers follow Section 6 (L0 to L4). `bg` is authored at **960 x 540** (the bleed frame). `mid` and `fg` are authored at 640 x 360, or smaller if they sit in the stage window. The `surround` (`env_surround_ink_960x540`) is a plain INK-0 field with an optional INK-1 running-bond course, one value step. Repeating pieces use a 20 px module, seamless in both axes, and are checked in a 3 x 3 arrangement.
- **Panel frame.** 3 px INK-3 plus a 1 px INK-4 highlight on the top-left. The 9-slice border is 4 px and the source is 16 x 16. Edges are uniform along their length, or tile with a 4 px period. Draw mode is Sliced (or Tiled). Panels sit on whole native pixels.
- **Buttons.** Plain rectangles with states `default hover pressed disabled focus`. Focus is the selection outline (Section 7.11).
- **Door cards.** Each tier has its own corner pieces (chamfers, Gold brackets) from the plate generator. Edges tile, and the Red serration sits on a 4 px pitch. Pips are 1 to 6 diamonds on a 6 px grid. The curse tag hangs from the bottom edge. Card size follows Section 7.10 (about 84 x 140).
- **Popup.** The hover popup (Section 7.9) uses the panel frame above (no second frame asset). It is 152 px wide at every C, 9-sliced to any height up to 232 px. The chip fallback uses the same frame at 18 px tall.

### 8.9 Native pixel font (Vietnamese and English)

**Decision (user):** text is a native pixel font. The fallback ladder is: another native font; a Latin pixel font hand-extended with the Vietnamese marks (user-approved in Section 7); and only then a hi-res font.

File and use rules:
- Fonts live in `Art/Fonts/{family}/`, with a `LICENSE.txt` per family (exempt from the 8.3 regex) and the manifest entry. Fonts are used only at their design pixel height. **Scaling a font by an integer multiple is banned** because it creates mixels (a bigger size is a separate authored cut, never a scaled one).
- **The font table (single source; **[PROVISIONAL]**, matching Section 7.7).** Text sizes and numeral sizes are authored cuts, not scales:

| Family | Cut (line box in native px) | Used for |
| ---- | ---- | ---- |
| Text face (Vietnamese-gated) | **12** (Body, Secondary; Standard set) | Effects, buttons, menus, event text, labels |
| Text face | **16** (Heading; also the Large Body cut) | Panel titles, item and card names |
| Text face | **20** (Large Heading cut, accessibility set only) | Text scale "Large" (7.13) |
| Text face | **24** (Display) | Title, victory and defeat banners |
| Numeral face (digits and a few symbols) | **8, 12, 16, 24, 32, 48** | 8: HP, tab magnitude, chip numbers, prices, ribbon cooldowns; 12 to 32: damage steps and result panel; 48: payoff peak only |

  The 8.3 `font` size token takes the values 8, 12, 16, 20, 24, 32 and 48. Numerals grow by changing cut, never by scaling. Chip numbers use the 8 cut.
- Point-sampled atlases only, with no SDF smoothing. The build method is in Part B 8.B.8.
- Allow about 30% more label width for Vietnamese strings **[PROVISIONAL]**.

**Vietnamese render test (gate).** Section 7.7 holds the full pass criteria (146 precomposed glyphs, stacked capitals, tone pairs, horns and bars, line collision at 14 px pitch, real strings, +30% expansion, licence, digits). Render the sheet at native size and at 2x on a 1280 x 720 capture, in each candidate font. The worst-case stacked capital row is `Ấ Ầ Ẩ Ẫ Ậ Ắ Ằ Ẳ Ẵ Ặ Ế Ề Ể Ễ Ệ Ố Ồ Ổ Ỗ Ộ Ớ Ờ Ở Ỡ Ợ Ứ Ừ Ử Ữ Ự`, and a context sentence is `Đặng Thị Ánh Nguyệt ưa uống nước ép ổi ở Huế, ăn bánh mì và đọc chữ Việt.` Line pitch must be at least cap height + 5 px **[PROVISIONAL]**.

### 8.10 VFX limits, glow, photosensitivity

- **Glossary.** In Sections 2, 4 and 5 "bloom" means the authored **stepped-shell light of a payoff**, built from sprites. A **post-process bloom** (a URP Volume effect) is rejected in 8.1 rule 4, 8.10 and 8.16, and appears only in spike S1, to test layering.
- **Sprite sizes.** Particles are 1 x 1 to 4 x 4. Rings are code-drawn. Threads are 1 to 2 px wide, tiled with a 4 px pitch, at 0 or 90 degrees. Flipbooks run at 15 fps (4 display frames each).
- **Limits** (shared with Part B 8.B.6 and 8.B.7): at most 500 live particles and F at most 1.0 full-screen-equivalents at the Detonation peak; at most 8 particle renderers plus 8 sprite-VFX batches. The art target is **200 or fewer live VFX sprites**, and it can trade down to 100.
- **Glow** is stepped shells (two to three 1 to 2 px shells in Highlight, Base, Edge, opaque). An ordered 2 x 2 dither is allowed on the outermost shell (the authorised VFX exception in 4.13). Rejected: post-process bloom, additive blending, soft gradients.
- **Photosensitivity** (4.12, **NOT VERIFIED**):
  - At most 2 flashes per second for Detonation.
  - Rises eased over at least 6 display frames.
  - A single flashing region of at most 0.111 full-screen-equivalents (about 25,600 native px2, roughly 160 x 160).
  - No alternation to or from saturated red.
  - Every payoff effect ships a `-calm` variant.
  - The Detonation peak and the avatar chain are captured and analysed before any build ships.

### 8.11 AI generation workflow

1. **Brief.** The manifest entry (name, category, canvas, palette) plus the 4.13 rules go into every prompt.
2. **Generate at native canvas size** if the tool supports it. Otherwise generate at an exact integer multiple (2x to 8x), detect the pixel grid, and reduce by **nearest neighbour only**. Smooth resampling is banned.
3. **Key out the background** to binary alpha (threshold 128).
4. **Quantise** to the master palette in OKLab: nearest colour, ties to the lower index, no dithering.
5. **Outline pass** (1 px INK-0; INK-4 on monsters and bosses; tier edge on plates) and the key-side rim.
6. **Hand clean-up** in indexed mode, then review.
7. **Retry limit** of three regenerations per sprite, then hand-draw.
8. **Provenance.** Tool, model and version, prompt, seed and date go in the manifest, which supports store-page AI disclosure and a licence audit.
9. **Animation.** AI output is key poses only, and the frames are checked for ground-line and outline stability. The locked counts in 5.6 are small enough to hand-finish.

**What a rejected sprite looks like:**
- Off-palette intent: more than **15%** of opaque pixels move by more than **0.10** in OKLab on quantisation **[PROVISIONAL]**.
- Anti-aliased halos, alpha other than 0 or 255, mixels, dithering, gradients or noise.
- An outline that is not 1 px, or not INK-0 (heroes, recruits, NPCs, props), INK-4 (monsters and bosses) or tier edge (plates). Pure black or white.
- A rim on the wrong side, light from anywhere but the top-left.
- More than three steps on a lead shape, or more than two on a supporting shape.
- A lit pixel on something that does not matter.
- A form that breaks the 3.2 vocabulary.
- Orphan pixels, art touching the canvas edge, wrong canvas size.
- A `c` glyph that fails the outline-only test at 16 x 16.

### 8.12 Review checklist

**A. Automated (script, blocking)**
1. Name matches the regex and the category vocabulary.
2. Canvas matches the declared size. All frames of an asset share one canvas.
3. Every pixel is in the master palette. Alpha is 0 or 255.
4. Import preset matches 8.2.
5. Manifest entry and provenance exist.
6. Generated files carry the `generated` marker and match a fresh regeneration.
7. Lean frames pass the 8.5.5 checks. Anchors lie inside the canvas.
8. The on-screen palette audit passes on the washless test scene.

**B. Human (art director, in this order)**
1. Silhouette fill test at 1x.
2. Greyscale value test.
3. Light direction and rim side.
4. Vocabulary (3.2).
5. In context: on its plate or pedestal, at 3x and 2x, beside three neighbours. Items are checked on all six tiers, in the grid and as avatars.
6. Tier and cost: tier reads by form first, and a curse is an addition at the bottom.
7. Animation: ground line, loop, outline drift.
8. Squint test of the whole frame against the 4.10 budgets.
9. Sign-off recorded in the manifest as `approved`.

### 8.13 Folders, atlases, versioning, source

Unity compiles only `Assets/`, and `Assets/_Project` already exists. The template's `assets/` is the same folder as `Assets/` on case-insensitive Windows, so do not create both.

```
Assets/_Project/Art/
  Palette/  Fonts/{family}/  Characters/  Monsters/  Items/{Icons,Plates,Baked}/
  UI/  Environment/  VFX/  Atlases/
art-source/   aseprite/  ai-raw/  anchors/  art-manifest.csv
tools/art/    plate and recolour generator, shear bake, validator, quantiser
```

- **Atlases group by co-occurrence on screen**, not by category. The first proposal (**[TA]** sets sizes and padding):

| Page | Contents |
| ---- | ---- |
| `atlas_ui_main` | Panel frames, buttons, chips, glyphs, cards, plates, icons, lean frames (shared by canvas and world avatars) |
| `atlas_stage_core` | Heroes, props, monsters, NPCs, pedestals |
| `atlas_boss_{id}` | One boss, loaded in its room |
| `atlas_vfx_shared` | Effect frames |
| `atlas_env_{room}` | Backdrops |

  A combat screen touches about 4 pages plus the font pages. The prototype's `Resources.Load` plus `Sprite.Create` path bypasses atlases and is prototype-only. Art assets must come through the normal importer.
- **Memory.** About 30 to 45 MB for the full vision (Part B 8.B.9), which is 3 to 4% of 1 GB. It is not binding, so there is no compression.
- **Versioning.** Git history is the version. Only the palette carries a version in its name. The manifest `status` (`gen`, `clean`, `approved`) is the workflow state.
- **Source policy.** Every shipped PNG has an `.aseprite` master or a generator rule. Files over about 5 MB use Git LFS. Rejected generations are not kept, but a reason is logged. Raw output for accepted sprites is kept. **The project is not a git repository yet**, so this policy has no home.
- **Editor assumption.** Sources are exported to PNG. The Aseprite importer package (installed at 6.0.0) is not relied on.

### 8.14 Inventory at full vision (rough)

| Asset | Count |
| ---- | ---- |
| Hero key frames | 3 families x 6 = 18, plus 3 narrow-eye overlays and about 10 props |
| Monster frames | about 15 x 6 = 90 |
| Boss frames | 3 x 9 = 27 |
| NPC frames | about 8 |
| Item icons (`c`) | about 70, plus about 25 `d`, plus lean frames (2 per lean-enabled item) |
| Plates | about 170 to 180, generated |
| Effect frames | about 26, with 13 hand-authored |
| Small glyphs | about 25 to 30 drawings (12 px master plus 8 px reduction) |
| Backdrops | 5 rooms x 3 layers, plus the surround |
| UI frames, cards, chips | tens of small images |

### 8.15 Ideal standard versus likely constraints

| Topic | Ideal | If limits push back |
| ---- | ---- | ---- |
| Cell size | 24 px | 20 px chosen for stage room and larger expandable grids |
| Display edge | Letterbox in INK-0 | Bleed plus a 960 x 540 surround (CropFrame.None) |
| Plates | Hand-polished per tier | Generated, with hand overrides |
| Icons on 1x2 and L | Redrawn per orientation | Compact `c` glyph (chosen) |
| Icon colour | Restricted | Free palette (chosen), with a saturation cap as the first remedy |
| Glow | Soft bloom | Stepped shells (palette lock, Part B agrees) |
| Animation files | Sheets | Per-frame PNGs (prototype importer) |
| Pixel snapping | UpscaleRenderTexture | PixelSnapping plus manual snapping if the UI alignment spike fails |
| Draw calls | 500 verified | Unverified until a stress scene runs |

### 8.16 Conflicts between art preference and engine limit, and how they were resolved

| Art preference | Engine constraint | Conflict? | Position and trade-off |
| ---- | ---- | ---- | ---- |
| Integer scale with an INK-0 letterbox on odd displays | `CropFrame` other than None clears the back buffer to pure black, which breaks the ink floor | **Yes** | Letterbox withdrawn. `CropFrame.None` plus bleed. Cost: every edge needs bleed art (a 960 x 540 `bg` plus a surround) and UI anchors to the safe frame. |
| Show exactly 640 x 360 | At the 720p minimum a non-multiple window shows up to 1.5x per axis | **Yes** (cost) | The bleed frame is 960 x 540. `bg` layers cost 2.25x the pixels; the 12 backdrops are about 25 MB at 960 x 540 (Part B 8.B.9), inside the memory estimate. A window-size dropdown of exact multiples would remove most of the cost; it is the user's call. |
| PPU 20, C = 20 | The prototype importer hard-codes PPU 16 and forces Single mode | **Yes** with the prototype only | One global PPU. The prototype importer is rewritten to per-folder presets. C stays 20 or goes to 24, not 16: a 12 px plate interior fails the icon spike. |
| Animation sheets | The importer blocks sheets | **Yes** | Per-frame PNGs. Cost: about 250 files instead of about 60. |
| Even canvas dimensions | Even dimensions or whole-pixel pivots | Minor | Even by default; whole-pixel exceptions listed (lift plates, overlays, chevrons). |
| Binary alpha, Point, no compression, no mips, Full Rect | Same | **No** | Agreed. |
| Palette lock at runtime | The prototype has ACES tonemapping, vignette, a tinted Global Light 2D and semi-transparent colours | **No** (agreed) | Whitelist of sanctioned soft alpha plus an on-screen palette audit. `Palette.cs` is replaced by a file generated from the `.gpl`. |
| Effects over the grid and beneath the protected layer | The Overlay canvas draws after the final blit; bloom, vignette, tonemapping and lighting never touch it; world particles draw behind it | **Partial** | Overlay matches 5.10.4 except Chain threads, which start inside the grid. Two-leg threads (canvas leg plus world leg). Screen Space - Camera only as a fallback, after a spike. |
| Item icons in colour | Not an engine matter | n/a | Free palette (user decision). Tier reads by form first; the icon spike checks colour against tier edges. |
| A row-shear lean of 2 px | A 2 px shear on a 2 to 3 px chamfer would distort the tier cue | **Yes** | Shear the glyph layer only. The avatar is two sprites. |
| Atlas per category | Group by co-occurrence; the prototype uses `Resources.Load` plus `Sprite.Create` with no atlases | **Yes**, mild | Co-occurrence groups (8.13). `Sprite.Create` is prototype-only. A sprite is occasionally duplicated across pages; memory absorbs it. |
| About 500 draw calls | SRP Batcher on; dynamic batching removed in 6.6 with no replacement stated; batching of SpriteRenderers under the 6.6 2D Renderer is NOT SOURCEABLE | **Open** | A dependency: art supplies the worst-case content list for a stress scene (8.17), and the budget is unverified until it runs. |
| Texture memory | About 30 to 45 MB (3 to 4% of 1 GB) | **No** | Not binding. |
| 200 or fewer live VFX sprites | At most 500 live particles, F at most 1.0 FSE, at most 8 particle renderers plus 8 sprite-VFX batches | **No** | 200 is an art target under the engine ceilings. |
| Flash region of about 25,600 native px2 | 0.111 FSE (about 25,600 native px2) | **No** | The figures agree. Both rest on 4.12, which is unverified. |
| Stepped-shell glow, no bloom | HDR is on in the URP asset, but the HDR emission path and bloom thresholds are NOT SOURCEABLE | **No** | Stepped-shell glow stands. HDR emission is not used. |
| Native pixel text | The prototype uses legacy `Text` with an OS dynamic font (anti-aliased, off-palette) | **Yes** with the prototype | Replace with a static TextMeshPro bitmap font, after spike S3. |
| Whole-UI integer scaling | The prototype canvas scales at 1600 x 900, match 0.5 (1.2x at 1080p, 0.8x at 720p) | **Yes** with the prototype | `ConstantPixelSize` with an integer `scaleFactor` set from z (8.B.8). |

### 8.17 Alternatives considered and open spikes

| Alternative | Why rejected |
| ---- | ---- |
| 960 x 540, 480 x 270, 384 x 216, 320 x 180 | None gives an integer fit at both 1080p and the 720p minimum with a 20 px cell |
| INK-0 letterbox | CropFrame other than None clears to pure black, which breaks the ink floor |
| Animation sheets | Blocked by the prototype importer's Single mode, and needless at 2 to 4 frames |
| Atlas per category | Costs extra texture binds when screens combine categories |
| Runtime-composed plates | Sub-pixel risk and per-drag cost |
| Per-tier icons | The plate already carries tier |
| Runtime tint of effects | Off-palette products |
| Post-process bloom | Off-palette, chunky under UpscaleRenderTexture, unverified in this URP version |
| Scaling the numerals | Creates mixels. Use hand-sized numeral fonts instead. |

**Open spikes (carried into pre-production):**
1. Icon legibility at 12 to 14 px, with colour (8.5.6).
2. S1: canvas and draw order, UI alignment with UpscaleRenderTexture, two-leg threads (Part B 8.B.14).
3. S2: draw-call stress scene (worst case: a full grid with items, 3 heroes, 4 monsters, 3 avatars, 2 rings, 200 VFX sprites, 24 chips).
4. S3: the Vietnamese font test (8.9).
5. S4: the on-screen palette readback (8.2).
6. S5: odd-window parity.
7. S6: the shear bake on 10 icons (8.5.6).

# Part B: Engine hard limits

**FIRM** means the engine source, project config or a locked art rule makes a violation visibly wrong. **ADVISORY** means a planning convention or proposal that needs profiling or ratification. Polygon, vertex and mesh budgets do not apply: all content is 2D sprites on quads. The binding limits are draw calls, fill rate, palette fidelity, pixel-grid alignment and layering.

**Engine baseline [PROJ]:** Unity 6000.6.3f1, URP 17.6.0, 2D Renderer, ugui 2.6.0, 2D Animation 16.0.1, Aseprite importer 6.0.0, Input System 1.20.0. Colour space value is 1 (Linear by the usual enum; confirm in Player Settings). The URP asset has HDR on, MSAA off, Render Scale 1, SRP Batcher on and dynamic batching off. Default window 1920x1080. Not installed: `com.unity.2d.pixel-perfect`, Addressables, VFX Graph.

### 8.B.1 Native resolution and integer scaling

**Pixel Perfect Camera.** The component is `UnityEngine.Rendering.Universal.PixelPerfectCamera`, shipped in URP 17.6.0 [PKG]. The prototype scene already uses it [PROJ] (Assets PPU 16, reference 640x360, `PixelSnapping`, `CropFrame.None`). Change `assetsPPU` to the final PPU (an `int` [PKG]). Do not add the legacy `com.unity.2d.pixel-perfect` package: URP carries a converter from it [PKG], and whether the two would clash is NOT SOURCEABLE.

**Zoom [PKG].**
```
z = max(1, min( floor(W/640), floor(H/360) ))
visible native area (CropFrame.None) = (W/z) x (H/z)
```

| Window | z | Visible native area |
|---|---|---|
| 1920x1080 | 3 | 640x360 |
| 1280x720 | 2 | 640x360 |
| 2560x1440 | 4 | 640x360 |
| 3840x2160 | 6 | 640x360 |
| 1600x900 | 2 | 800x450 |
| 1366x768 | 2 | 683x384 |
| 1900x1000 | 2 | 950x500 |
| 3440x1440 | 4 | 860x360 |

**FIRM rules**
- **Bleed.** Worst case at the 720p minimum is 1.5x per axis (959x539 native), so backdrops need at least 160 px bleed per side and 90 px top and bottom (a 960x540 backdrop) [CALC].
- **No crop frames.** With `CropFrame` other than None the back buffer clears to `Color.black` [PKG]. That violates the ink floor (4.3).
- **Odd leftover.** If a window leaves an odd number of screen pixels, the N frame sits on a half native pixel. Whether PPC compensates is NOT SOURCEABLE. Spike S5 covers it.
- **One quantiser.** All code-driven motion (avatar arcs, hops, the recruit slide, chips, pops) is quantised with `PixelPerfectCamera.RoundToPixel`, which is public and rounds to `unitsPerPixel` (1/PPU under both snapping modes) [PKG].

**Snapping modes (sourced behaviour):**

| Mode | Sourced behaviour | Consequence |
|---|---|---|
| `PixelSnapping` (prototype) | Snaps the camera matrix and sets `PixelPerfectRendering.pixelSnapSpacing` [PKG] | Renders at screen resolution. Bloom is smooth. Whether renderers or particles honour the snap spacing is NOT SOURCEABLE. |
| `UpscaleRenderTexture` | The whole scene and post-processing render in a low-res RT near N, then upscale. Final post-processing (FXAA) does not run [PKG] | Nothing can be sub-pixel in the world. Bloom is chunky. |
| Crop modes | Black bars, optional RetroAA bilinear [PKG] | Not used (see above) |

### 8.B.2 Sprite and texture import constraints

**FIRM**
- **One global PPU** for all world sprites, equal to PPC `assetsPPU` [PKG]. Prototype 16 becomes 20 if C = 20. N in world units is 32 x 18 at PPU 20 [CALC]; only C = 16 (40 x 22.5) and C = 20 give a whole-unit width, and C = 24 gives 26.67 x 15.
- **Render and post settings.** Render Scale 1.0, MSAA off, no FXAA.
- **No palette-altering effects on sprites.** Tonemapping, a tinted Global Light 2D and vignette change source pixels. The prototype has ACES, vignette 0.28, bloom threshold 0.9 and a (0.95, 0.92, 0.88) light [PROJ]. Required: white light at intensity 1 or Sprite-Unlit materials (Sprite-Unlit-Default exists in URP 17.6 [PKG]), no tonemapping, and vignette only as an art-director exception.
- **Point filter, no mipmaps, uncompressed.** Block compression is lossy and can create off-palette pixels. The prototype metas show these settings [PROJ].
- **No silent downscale.** `maxTextureSize` is at least the longest edge. The prototype default is 2048 [PROJ]. The hardware maximum is NOT SOURCEABLE.
- **Even dimensions** or a pivot on a whole pixel. Prototype UI parts are 65x65, 69x69, 91x39 and 37x37 [PROJ].
- **Binary alpha** (0 or 255) outside VFX. Prototype colours include alphas 0.85 to 0.98 [PROJ `Palette.cs`], which blend off-palette.
- **No bone deformation and no non-90-degree rotation of pixel sprites.** Row shear and stepped position are the allowed transforms (8.B.3).

**Importer checklist (every pixel sprite).** "Now" is what the prototype shows.

| Setting | Required | Status | Now |
|---|---|---|---|
| Texture Type | Sprite (2D and UI) | FIRM | OK |
| Sprite Mode | Single (static art), Multiple (sheets) | FIRM | forced Single on everything |
| Pixels Per Unit | the global constant | FIRM | hard-coded 16 |
| Filter Mode | Point | FIRM | OK |
| Generate Mip Maps | off | FIRM | OK |
| Compression (Default platform) | None, and no platform override | FIRM | OK. The Standalone entry has `textureCompression: 1` but `overridden: 0`. |
| sRGB | on | FIRM | OK |
| Alpha Is Transparency | on | FIRM | OK |
| Wrap | Clamp | FIRM | OK |
| Max Size | at least the longest edge | FIRM | 2048 default |
| NPOT | None | FIRM | OK |
| Pivot | on a whole pixel | FIRM | 0.5 |
| Read/Write | off | ADVISORY | OK |
| Mesh Type | Full Rect | ADVISORY | Tight (`spriteMeshType: 1`) |
| Generate Physics Shape | off unless a collider is needed | ADVISORY | on |

Mesh Type and Physics Shape effects in 6.6 are NOT SOURCEABLE.

**Problems in `PixelArtImportPostprocessor` [PROJ]**
- It rewrites every texture under `Assets/_Project/` on every import, including the 688x384 mockup and any future LUT or mask.
- It forces Single mode, which blocks sheets, and forces PPU 16. It cannot be exempted per asset.
- [REF] (6.4): dependents reimport only when a dependency's import artifact changes, and `AssetPostprocessor` code may need `DependsOnSourceAsset` or `DependsOnArtifact`. How a rule change in the script forces a reimport is NOT SOURCEABLE.

### 8.B.3 Baked row-shear import step (avatar lean)

**Rule (5.12).** Rows above the glyph's pivot row shift 1 px sideways and the top quarter shifts 2 px, mirrored for the other direction. No resampling. Three variants per glyph: centre (unchanged), left, right.

**Specification (deterministic).** Let the glyph be H rows tall, y = 0 at the bottom, pivot row P, direction d = +1 or -1. Row y shifts by:
```
s(y) = 0            for y < P
       d * 1        for P <= y < floor(3H/4)
       d * 2        for y >= floor(3H/4)
```
The canvas is widened by 2 px on each side, giving (c + 4) x c for a c x c glyph, where c = C - 4 is the compact glyph edge (20 x 16 at C = 20, since the glyph is 16 x 16). Rows move as whole rows, so each row's pixel colours are unchanged.

**Validator-checkable properties [CALC].**
1. The per-row pixel multiset equals the source row.
2. `|s| <= 2`.
3. `s` is non-decreasing with y and adjacent rows differ by at most 1 px, so a 1 px outline stays 8-connected.
4. No clipping.
5. Palette equals the source palette and alpha stays binary.
6. Byte-identical output on a rerun (hash).
7. Canvas dimensions even.

**Scope.** Shear the **glyph layer only**. The plate (with its shadow baked in) is a separate sprite, so an avatar is two sprites (8.5.4).

**Mechanism.** Recommended: an Editor menu script writes `_lean_l` and `_lean_r` PNGs into a generated folder, and the same checks then apply to them, which also sidesteps the 6.4 reimport-dependency question (8.B.2). Alternatives (an `AssetPostprocessor` that adds sprites, or a `ScriptedImporter`): [REF] names `ScriptedImporter` only in the dependency note, and their APIs and creating extra sub-sprites from one source are NOT SOURCEABLE. The project already has an Editor assembly with an `AssetPostprocessor` [PROJ].

**Memory.** At most 80 glyphs x 3 variants = 240 sprites of (c+4) x c x 4 B with c = C - 4 [CALC]: 184,320 B (C = 16), 307,200 B (C = 20), 460,800 B (C = 24).

### 8.B.4 Draw order, Overlay versus Screen Space - Camera, and the Section 5 layers

**Required order (5.10.4), back to front:** environment, characters, effects (rings, smears, trails, impacts, threads), avatars and chips, then the protected layer (HP, intent tabs, turn marker, left grid column). The bloom sits beneath the protected layer.

**What could be sourced**
- URP 17.6's 2D Renderer schedules the Overlay UI pass (`DrawScreenSpaceUIPass`, for uGUI and UI Toolkit) after the final blit (`AfterRendering` + offset) [PKG].
- Overlay content is therefore always above every camera-rendered object. Bloom, tonemapping, vignette, lighting and PPC never touch it.
- The prototype's canvas is Overlay [PROJ], so its Volume (bloom, ACES, vignette) probably affects only the camera background. Confirm with a screenshot.
- A second UI pass runs before post-processing ("offscreen UI") [PKG]. What selects it is NOT SOURCEABLE.

**What could not be sourced**
- How Screen Space - Camera canvases behave with PPC, `UpscaleRenderTexture` or post-processing.
- How sorting layers or orders interleave canvases with `SpriteRenderer`s.
- Whether Particle Systems render inside Overlay canvases.
- How a World Space canvas behaves in the 2D Renderer.

**Consequences**
- The protected layer must be Overlay, because that is the only mechanism that could be sourced to guarantee "always on top" and "bloom beneath".
- The stage (environment, characters, effects, avatars, chips) is camera-rendered `SpriteRenderer` content. It sits below every Overlay element, so anything Section 7 draws as chrome in Overlay will sit above effects and avatars. Section 7 classifies chrome accordingly (protected by definition).
- **Threads** cross the layer boundary: source plates are in the Overlay grid column, and avatars are in the world. Draw each in two legs, an Overlay leg inside the column bounds and a world leg outside, meeting on a whole native pixel. Alternative: start the thread at the column edge.
- **Coordinate mapping.** Overlay canvas scale = z and PPC zoom = z, so 1 native pixel = z screen pixels in both. Use one function from native (x, y) to world (x/PPU, y/PPU) and to canvas anchored position (x, y), both centred. Per-unit HP rows and tabs sit on fixed slots (fixed camera, no walk cycle), so the mapping is computed once.
- Alternatives: *all Overlay* (simplest order; the bloom must be built from rings, particles and threads, which 4.12 says it is; particles need UI-pooled quads) and *all Screen Space - Camera* (the protected layer can no longer be above the bloom, because post-processing applies to it; feasibility NOT SOURCEABLE).
- **Adopted:** world stage plus Overlay protected layer, subject to spike S1.

### 8.B.5 Code-drawn integer-radius rings and thread lines

**Integer rasteriser rule (palette-safe by construction).**
- For a ring of radius r and thickness t (1 or 2 px) centred on the avatar centre, set a pixel when `4(r-t)^2 < (2x+1)^2 + (2y+1)^2 <= 4r^2` (pixel-centre offsets from the centre, integers only).
- An even C puts the avatar centre on a pixel corner, so the ring texture is 2r x 2r. C = 16, 20 and 24 are all even.
- Colours are written as exact palette `Color32` values per step (highlight, base, INK-4). No tint multiply, no alpha blending.
- Lines use Bresenham, 1 px thick.
- Cap: at most 1 ring per avatar and 2 on screen (5.11). The radius cap is about 200 px (about two slot pitches plus a body half-width [CALC, assumption]).

| Option | Palette and snapping | Draw calls | Cost |
|---|---|---|---|
| R1: runtime `Texture2D` + `Sprite.Create` per ring or thread, regenerated per step | Exact. Position via `RoundToPixel` | 1 per ring or thread (at most 2 + 3) | Up to (2*200)^2 x 4 = 640,000 B per ring; 6 regenerations per ring |
| R2: one shared stage-sized effects buffer (640x360x4 = 921,600 B) drawn on the CPU | Exact and grid-aligned by construction | 1 | A full-frame quad is 1 FSE while visible |
| R3: procedural mesh (Mesh2D-Unlit shaders exist [PKG]) | Exact | 1 per mesh | Usage in the 2D Renderer is NOT SOURCEABLE |
| R4: shader with a white mask tinted by a palette colour | Sprite-Unlit multiplies vertex colour x `_Color` x `unity_SpriteColor` [PKG]. Exact only if the mask texel is exactly 1.0; verify by readback | 1 per ring | Needs a custom shader; usage NOT SOURCEABLE |

R1 has project precedent (`Texture2D` and `Sprite.Create` at fixed PPU in `SpriteFactory` [PROJ]). **Adopted: R1**, with R2 as the fallback if spike S2 shows draw calls or batch breaks matter. CPU cost is bounded by arithmetic (at most 160,000 pixel tests per step) but is unmeasured. Thread textures are at most 640x360x4 = 921,600 B each, 3 on screen = 2.76 MB [CALC].

### 8.B.6 Draw-call and particle estimates

**Counting model.** `D = D_world + D_overlay + D_vfx + D_post`.
- The mechanism that batches `SpriteRenderer`s in the 6.6 2D Renderer is NOT SOURCEABLE.
- Dynamic batching is removed with no replacement named [REF].
- `Graphics.RenderSprite` and `RenderSpriteInstanced` work with 2D lights, sprite masks and the SRP Batcher (6.5) [REF]; their details are NOT SOURCEABLE.
- The estimates below are unmeasured and must be confirmed in the Frame Debugger or the 2D Profiler module [REF].

**Worst case of three avatars on screen** (assumptions: at most 8 chip targets, at most 2 chips each):

| Element | Count | Notes |
|---|---|---|
| Avatars | 3 x 2 sprites (plate with baked shadow, glyph) = 6 | |
| Chips | at most 16 x about 6 elements (bed, glyph, chevron, digits) = about 96 | digits as sprites from the same atlas |
| Rings | 2 | R1 textures |
| Threads | 3 | one world leg each |
| Impacts and smears | about 6 | shared frames |
| Characters | about 25 | heroes, enemies, prop overlays |
| Backdrops | 5 (L0 to L4) | one per layer |

- A **fully unbatched** bound is about 140 world calls. The Overlay side is about 192 + 3S + 30 text, where S is the stash cell count (stash size is not yet known). Total is about 360 + 3S, so S under about 45 keeps an unbatched worst case under 500 [CALC, assumed element counts].
- **Disciplined** (per-layer atlas batches, digits as sprites): about 15 to 35 world and Overlay calls plus post-processing passes (unmeasured, NOT SOURCEABLE), plus particle renderers.
- **Particles.** Proposed Detonation cap (not project-stated): 500 live particles, at most 8 particle renderers and 8 sprite-VFX batches. Typical pixel particles of 1 to 4 px use 0.01 to 0.04 full-screen equivalents (FSE) at N, so count, CPU and draw calls bind, not fill. Which particle shader renders in the 2D Renderer, and VFX Graph (not installed) with it, are NOT SOURCEABLE. Pooled 1x1 sprites or `RenderSpriteInstanced` are options.

### 8.B.7 VFX, fill and post-processing

- **Fill metric:** `F = sum(particle native px^2) / (640 x 360)`. Peak F at most 1.0 is PROPOSED. A single flashing region of at most 0.111 FSE (about 25,600 native px, such as 160x160) follows from 4.12 (341 x 256 of 1024 x 768) [CALC]; 4.12 itself is an unadopted proposal. An avatar (about 24 x 24 px) is far below it.
- **Bloom.** HDR is on in the URP asset [PROJ], and bloom only affects the camera-rendered stage (8.B.4). The light ceiling `#F2E8CE` should stay below the threshold. Bloom threshold units, per-pass cost and the way to push a sprite above 1.0 are NOT SOURCEABLE. Test with a captured frame. Under `UpscaleRenderTexture`, bloom runs at native resolution [PKG].
- **Reduced flashes** needs runtime control of Volume weight and a no-burst variant of every payoff prefab. Rings in that mode appear at final radius in 3 steps (4.12).
- **Timing:** author in milliseconds (6 frames at 60 fps = 100 ms). VSync is 0 in the first two quality levels and 1 in the others [PROJ].
- **Minimum hardware** is not stated in the project files, so fill limits stay unratified.

### 8.B.8 UI: scaling, canvas and native pixel font

**Integer scaling of the whole UI at 640x360 (FIRM).** Replace the prototype's `ScaleWithScreenSize` 1600x900, match 0.5 (1.2x at 1080p, 0.8x at 720p [CALC]).

| Setting | Required |
|---|---|
| Canvas Render Mode | Screen Space - Overlay (protected layer) |
| CanvasScaler Ui Scale Mode | `ConstantPixelSize` |
| `scaleFactor` | integer z, set by a script on resolution or fullscreen change, using the formula in 8.B.1. The setter exists [PKG] (clamped at 0.01) |
| `referencePixelsPerUnit` | equal to the global PPU (default 100 [PKG]), so a sprite's 1 source px = 1 canvas unit. `UISkin` currently divides by 16 and scales [PROJ] |
| Root | a RectTransform of exactly 640x360, centred |
| Layout | integer positions and sizes only. No fractional anchors (the prototype uses 0.333 splits [PROJ]). Column widths follow the Section 7.3 formula, in whole native pixels |
| TMP | auto-sizing off. `fontSize` equal to the atlas sampling size of the authored cut (no scaling; the cuts are in the 8.9 font table) |
| Drag ghosts | snap to whole native pixels (the prototype ghost follows the pointer [PROJ]) |

`Canvas.pixelPerfect` semantics and enforcing a minimum window size are NOT SOURCEABLE.

**UI Toolkit [REF].** Panel Renderer replaces UI Document for scene UI, and Advanced Text Generator is the default text system (6.5); `backdrop-filter`, `drop-shadow` and `VisualElement.AddMeshModifier` arrive in 6.6; legacy UXML Factory/Traits are removed. Pixel-art behaviour (nearest sampling, 9-slice, integer scale) is NOT SOURCEABLE. Keep uGUI: it is installed and all prototype UI uses it [PROJ].

**Native pixel font.**
- TextMeshPro ships in the installed ugui 2.6.0 [PKG], with static atlases, `RASTER` and `RASTER_HINTED` (non-antialiased) render modes, a Bitmap shader and a `Custom Size` option for "pixel-accurate control over bitmap-only fonts" [PKG docs]. Both asmdefs reference TMP [PROJ].
- Use a Static atlas (`AtlasPopulationMode.Static`) with `RASTER`. Do not use Dynamic or DynamicOS. Atlas texture filter mode and shader details beyond the TMP docs are NOT SOURCEABLE; verify in the font test.
- The prototype uses legacy `Text` with an OS dynamic font [PROJ], which is anti-aliased and off-palette.

**Vietnamese coverage [CALC from Unicode blocks, not engine facts]:** Basic Latin 95, Latin-1 letters used about 34, Ă ă Đ đ Ơ ơ Ư ư 8, U+1EA0 to U+1EF9 90, punctuation and the dong sign a few: **about 240 glyphs**. A cell of 8 x 14 plus 1 px padding is 135 px², so 240 cells are 32,400 px² and fit 256x256 (about 49% used; 262,144 B at RGBA32; the actual atlas format is NOT SOURCEABLE). Line pitch is about 14 px at 12 px (stacked marks). Larger sizes are separately authored cuts (8.9 font table), not scaled copies.

**The validator can check:** TMP `HasCharacters(text, out missing)` over the entire English and Vietnamese string tables [PKG]; NFC-normalised text; glyph vertical extents within the line height; atlas Point, uncompressed, no mips, with padding; integer metrics; and a render test capturing worst-case Vietnamese strings at z = 1, 2 and 3 for clipping.

### 8.B.9 Atlases and memory

**Atlas groups (ADVISORY).** `ui_main` (chrome, beds, plates with shadow, pips, chips, digits, avatar glyph sets), `chars_heroes`, `chars_monsters`, one page per boss, `vfx`. Backdrops use one atlas page per room (`atlas_env_{room}`, 8.13) loaded with the room. Page settings: Point, no mips, no compression, Read/Write off. Sprite Atlas V2 settings in 6.6 and `m_SpritePackerMode: 5` are NOT SOURCEABLE. Runtime `Sprite.Create` sprites (as used by `UISkin` and `SpriteFactory` [PROJ]) are by definition outside any atlas, and whether `Resources` and atlases interact is NOT SOURCEABLE.

**Size limits.** No edge above 2048 (FIRM, project importer default). A 2048x2048 RGBA32 page is 16 MiB [CALC]. `ui_main` should fit one page.

**Illustrative total at C = 20 [CALC].** Frame counts and canvas sizes are assumptions.

| Item | Assumption | Bytes |
|---|---|---|
| C-dependent set (icons, plates, avatar glyphs) | see 8.B.10 | 1,210,368 |
| Heroes | 36 frames, 48x48 | 331,776 |
| Monsters | 90 frames, 64x64 | 1,474,560 |
| Bosses | 27 frames, 128x128 | 1,769,472 |
| Shared effects | 25 frames, 48x48 | 230,400 |
| Glyphs | 16 x C x C | 25,600 |
| Backdrops | 12 at 960x540 | 24,883,200 |
| Effects buffer (R2) | 640x360 | 921,600 |
| Font | 256x256 RGBA32 | 262,144 |
| **Total** | | **31,109,120 B = about 29.7 MiB (about 3% of 1 GB; the larger monster canvases of 8.6 add a few hundred KB, not binding)** |

Backdrops dominate. If each of the 12 had five separate 1.5N layers, the total is 130,795,520 B (124.7 MiB, about 12%). Doubling N multiplies N-derived figures by 4.

### 8.B.10 Numbers for 640x360 at C = 16, 20 and 24

| | C = 16 | C = 20 | C = 24 |
|---|---|---|---|
| PPU if PPU = C | 16 | 20 | 24 |
| N in world units | 40 x 22.5 | 32 x 18 | 26.67 x 15 |
| Cell on screen (3x / 2x) | 48 / 32 | 60 / 40 | 72 / 48 |
| 4x4 backpack edge | 64 | 80 | 96 |
| Avatar zone C + 4 | 20 | 24 | 28 |
| Hero slot pitch (with the 12 px stat row, H = 32) | 90 | 94 | 98 |
| Three slots, of a 314 px stage | 270 | 282 | 294 |
| Right column width (640 - left - 140) | 358 | 326 | 294 |
| Margin right of two enemy lines (2 x 140) | 78 | 46 | 14 |
| Popup width | 152 | 152 | 152 |
| Enemy lines that fit | 2 | 2 | 2 |
| Enemy slot pitch, small / medium / large / boss (body + 56) | 80-84 / 92-100 / 108-116 / 152-171 | same | same |
| C-dependent texture memory | 776,192 B (0.74 MiB) | 1,210,368 B (1.15 MiB) | 1,740,800 B (1.66 MiB) |

Column widths and the number of grid columns follow the Section 7.3 formula, and only one grid is visible at a time (switch-only). The stash size is not known, so stash capacity is not tabulated. The memory row is 80 icons at (2C+2)² x 4 B, plus 48 plates at the same size, plus the 240 avatar glyph variants of (c+4) x c x 4 B (8.B.3). Tier chamfers (2 to 3 px) are 4 to 6 screen px at the minimum 2x.

### 8.B.11 Play Mode and pipeline

- **Domain reload.** [REF]: 6.6 new projects default to "Reload Scene only", so statics persist between Play sessions; reset them with `[OnEnteringPlayMode]`, `[OnExitingPlayMode]` or `[AutoStaticsCleanup]`. This project has `m_EnterPlayModeOptionsEnabled: 1` and `m_EnterPlayModeOptions: 1` [PROJ]; value 1 is presumably "disable domain reload" (not in [REF]); the upgrade treatment of an existing project is NOT SOURCEABLE. `UISkin.Cache` and `SpriteFactory` hold static caches [PROJ], so a re-imported PNG can show stale in Play until the cache is cleared.
- **Validator delivery.** EditMode tests or a menu item. Do not close the Editor to run batchmode (project memory). `-quit` cannot be combined with `-runTests` [REF]. Editor.log is project-local, `Logs/Editor.log` [REF].
- **Build.** Everything under `Resources/` ships, and the 688x384 mockup sits there [PROJ]. 6.6 has a Build Analysis window [REF] whose usage is NOT SOURCEABLE. `ProjectAuditorSettings.asset` has a `SpriteAtlasEmptySpaceLimit` key [PROJ] whose effect is NOT SOURCEABLE.
- **Aseprite importer.** Installed. Its PPU and filter settings and interplay with the postprocessor are NOT SOURCEABLE. The 6.6 Mesh Read/Write requirement's scope is NOT SOURCEABLE.

### 8.B.12 Validator rules

| ID | Check | Strength |
|---|---|---|
| V-01 | PNG, 8-bit RGBA | FIRM |
| V-02 | Every opaque pixel in the master palette; no triple at or below 0x0A or at or above 0xF6 | FIRM |
| V-03 | Alpha 0 or 255 outside VFX | FIRM |
| V-04 | Importer values match the 8.B.2 checklist | FIRM |
| V-05 | `maxTextureSize` is at least the longest edge; no edge above 2048 | FIRM |
| V-06 | Even dimensions or whole-pixel pivot | FIRM |
| V-07 | PPU equals the global constant | FIRM |
| V-08 | No platform compression override | FIRM |
| V-09 | Same-screen sprites share an atlas page | ADVISORY |
| V-10 | Naming regex (8.3) | open |
| V-11 | Scene: white light, no tonemapping, Render Scale 1, PPC present, `CropFrame.None`, Overlay canvas `ConstantPixelSize` with integer scale | FIRM |
| V-12 | Shear properties 1 to 7 in 8.B.3 | FIRM |
| V-13 | Font: `HasCharacters` on all strings, NFC, metrics, atlas Point and uncompressed | FIRM |
| V-14 | Runtime readback: every opaque pixel of a captured frame region without blending is in the palette | ADVISORY |

The palette check applies to source PNGs. Rendered frames contain blended colours wherever alpha, bloom or a lit pass is used.

### 8.B.13 Prototype remediation, in priority order

1. **Integer UI scaling and layout.** Replace the 1600x900 scaler. Remove fractional anchors. Snap drag ghosts. Nothing else is evidence-worthy until this is done.
2. **Scene render settings.** Set `assetsPPU` to the final PPU. Set the Global Light 2D to white. Remove ACES. Remove vignette (or get an art-director exception). Do not use a post-process bloom in the shipped look (Part A rejects it; see the 8.10 glossary). A Bloom volume appears only in spike S1, to test layering, where Overlay UI is immune to it.
3. **Importer postprocessor.** PPU from one constant. Scope to art folders. Mode by path. Mesh Type Full Rect. Physics shape off. Exclude mockups. Handle reimport dependency (the mechanism is NOT SOURCEABLE).
4. **Sprite loading.** Replace `Resources.Load` plus `Sprite.Create` with serialized Sprite references and Sprite Atlases. Remove runtime `filterMode` patches.
5. **Palette.** Generate `Palette.cs` from the master `.gpl`. Remove alpha below 1. Prototype values such as `#0F0D14` and `#2C3344` are off palette.
6. **Text.** Replace legacy `Text` and the OS font with a TMP bitmap pixel font, after spike S3 passes.
7. **Housekeeping.** Odd-sized sprites, mockup out of `Resources`, static-cache reset hooks, platform-override audit.

### 8.B.14 Spikes, in order

- **S1: canvas and draw order.** One scene at N, PPC, a world avatar plus an R1 ring plus a Bloom volume, and an Overlay panel overlapping them. Capture at 1920x1080, 1280x720, 1600x900 and 1366x768. Check ring and avatar pixel alignment, that the bloom halo ends beneath the Overlay panel, a two-leg thread join, and the world-to-canvas mapping. Repeat with a Screen Space - Camera canvas to fill the NOT SOURCEABLE answers.
- **S2: stress scene.** Detonation worst case: 3 avatars, 16 chips, 2 rings, 3 threads, 500 particles, the full grid and stash. Read the Frame Debugger and the 2D Profiler module. Run it once with the prototype loader and once with atlases.
- **S3: font render test.** TMP static `RASTER` Bitmap-shader font, worst-case Vietnamese strings at z = 1, 2 and 3, `HasCharacters`, clipping check at 14 px pitch, integer metrics.
- **S4: palette readback.** Plate, ring, and white-mask-times-tint versus direct colour; compare against palette values with and without HDR and tonemap.
- **S5: odd-window parity.** Windows whose leftover is odd screen pixels.
- **S6: shear script.** Round-trip, hash determinism and the validator.

### 8.B.15 NOT SOURCEABLE (consolidated)

- Hardware maximum texture size, and the minimum-spec GPU.
- The 6.6 2D Renderer `SpriteRenderer` batching mechanism, and `RenderSpriteInstanced` details.
- Sprite Atlas V2 behaviour in 6.6, `m_SpritePackerMode: 5`, and `Resources`/atlas interaction.
- Whether pixel snapping applies to individual renderers or particles.
- Screen Space - Camera and World Space canvas behaviour with PPC, UpscaleRT, post-processing and sorting.
- Whether Particle Systems render inside Overlay.
- Which particle shader works under the 2D Renderer, and VFX Graph with it.
- The "offscreen UI" pass trigger.
- Bloom threshold units, per-pass cost, and an HDR emission method for sprites.
- The R3 and R4 usage details, and the CPU cost of ring generation.
- `AssetPostprocessor` and `ScriptedImporter` mechanics for adding sprites and for reimport on rule change.
- `Canvas.pixelPerfect` semantics, and minimum window size enforcement.
- UI Toolkit pixel-art features.
- TMP atlas texture format and filter defaults.
- The meaning of `m_EnterPlayModeOptions: 1`, and the colour-space enum mapping.
- Aseprite importer settings, Mesh Read/Write scope, Build Analysis usage, and the `SpriteAtlasEmptySpaceLimit` effect.
- Windows HDR output state.

### 8.B.16 Open items and decisions still to make

- **Stash size** and the **minimum-spec GPU** are needed before layout and fill limits are ratified.
- Whether `Resources` stays (recommended: no; serialized references plus atlases; no Addressables at this memory scale).
- The window policy (adopted default: any size of 1280x720 or larger with bleed; alternative: exact multiples only).
- The accessibility photosensitivity figures (4.12) remain NOT VERIFIED.
- `design/accessibility-requirements.md` must exist before Section 7 is final.
- Re-measure all Section 4 hand-computed luminance figures with a tool.

## 9. Reference Direction

> **Status.** Approved by the user on 2026-11-10 as a set of **four verified references**. The two screenshots (Reference 1 and Reference 2) were supplied by the user and their lessons are recorded in Section 1.2. The user named no film, book, music, artist or other non-game inspiration, so this section invents none (9.6 asks instead). Visual claims about other games were checked on 2026-11-10 and only what could be confirmed is written down; what could not be confirmed is marked in 9.8.

### 9.1 Scope and the test

A reference is not a mood board. Each one here is **one specific element** taken, **one divergence** that keeps the result from reading as a copy, and the **section of this bible** it supports. "The general aesthetic of Game X" is not an entry.

Apply the Section 1 question to the references themselves: *does this reference tell us something we must decide or read?* A reference earns its place only if it settles a call the bible would otherwise settle by guesswork, and only if it points in a direction no other reference does (9.4).

**Number of references: four.** Two are the user's screenshots. One is a favourite game whose method could be verified (Dead Cells). One is a comparable whose single visual convention could be verified (Slay the Spire). Backpack Battles, Backpack Hero, Overlooting and Megaloot are not entries, because their visuals could not be confirmed (9.5).

### 9.2 Summary

| # | Source | Type | The one element taken | Supports | Verified how |
| ---- | ---- | ---- | ---- | ---- | ---- |
| 1 | Screenshot 1: bright forest, chunky pixel art, three panels | User-supplied | Uniform dark outline weight on every figure and mass, and thick frames that read as a border | 1.2 (simplicity guard), 3.6, 4.13 rule 2, 7.1, 7.3 | Viewed 2026-11-10 |
| 2 | Screenshot 2: dark soft dungeon, tier-framed item grid, three units with HP bars | User-supplied | Dark stone field, units standing lit on dark pedestals, quiet thin chrome, left-grid / right-scene split | 1.1, 3.4.1, 5.4, 6.3, 7.3 | Viewed 2026-11-10 |
| 3 | **Dead Cells** (Motion Twin) | Game, user favourite | Figure-ground rule: backgrounds blend, interactive and collidable things contrast. Plus warm light on the player against cold surroundings | 1.1, 4.8, 5.4, 6.3 (edge-dominance check) | Developer art-design post, read through search summaries (9.8) |
| 4 | **Slay the Spire** | Game, comparable | Intent as one glyph plus one number, above the enemy, readable before it acts | 3.2, 5.10.8, 7.5 | Secondary guides only (9.8) |

### 9.3 The entries

#### 9.3.1 Reference 1: the bright forest screenshot

**What is visible.** A bright forest scene in chunky pixel art. A thick grey frame divides the screen into three panels. The left panel holds an HP bar and an item grid. The middle panel is a tall scene with a hero on a dirt path. The right panel holds slots and a stat list. Dark outlines surround the hero, the tree and the large foliage masses. Glowing items in the tree stand out clearly from the bright scene.

**Take.**
- **Outline consistency.** Every figure and large mass has the same dark outline weight, and the shapes are big and simple. The chunky read comes from consistent weight and few shapes, not from detail. This is the form behind our 1 px INK-0 outline on lead shapes (4.13 rule 2). The reference's own pixel scale was not measured (9.8), so we take the principle and not a number.
- **Frame mass.** The frames are heavy relative to the content, so the eye stops at them and the scene reads as a window. This supports the 3 px frames and the "thick and plain" rule (7.3, 1.2).

**Diverge so it does not read as a copy.**
- **Bright tone.** The bright, saturated greens and sky are not adopted (user decision, 1.2). Our floor is INK-0 and our light ceiling is `#F2E8CE` (4.3).
- **Frame ornament.** The frames carry small decorative details (foliage at the corners). We have none: no filigree, no ornament (1.2). The frame is a plain mass. (Section 1.2 describes this reference's frames as "thick plain"; the screenshot's frames are thick but not wholly plain, so we take the mass and not the ornament.)
- **Panel count.** It has three panels of similar weight. We have two panels and one thin divider, "tools on the left, theatre on the right" (7.3).
- **Setting.** An open outdoor path. Ours is the Vault: rectilinear, plain, enclosed (6.2).

**Supports:** 3.6, 4.13, 7.1, 7.3.

**Check at the first mock-up.** Place the mock-up beside this screenshot at equal width. Frames must read as heavy as this one's and carry less decoration. If a viewer would call our frames "decorated", remove detail.

#### 9.3.2 Reference 2: the dark soft dungeon screenshot

**What is visible.** A dark stone dungeon wall with an arch. Three units stand on dark ellipse pedestals, each with an HP bar above it and a turn counter at the top. On the left is a grid of items whose frames use many saturated hues. The chrome (panel edges, lines) is thin and dim. A few small sparkle marks near the units appear to be ambient decoration.

**Take.**
- **Figure-ground by value.** The wall and floor sit close in value, and the units are the lit things. This is the Section 1.1 rule working on screen, and it is the main reason the user chose this tone. It maps to the D1 plane rule in 6.3 and the pedestal Candle pool in 5.4.
- **Quiet chrome.** Thin, dim panel edges mean the data (items, HP) wins attention. This maps to the chrome ceiling INK-4 (4.3) and 7.1.
- **Layout split.** A left item panel and a right scene panel. This is the structure of 7.3.
- **Tier frames identify items fast.** A coloured frame on a dark cell is a quick read. We keep that idea, with form first (3.4.4).

**Diverge so it does not read as a copy.**
- **Every cell lit at full saturation.** The grid lights nearly every cell in a saturated hue, so nothing outranks anything (the Section 1.2 warning). Ours: a thin lit tier edge on a mid-value INK-2 bed, and the lit area budgeted per state (3.4.1, 4.10).
- **HP above the heads.** The zone above a hero's head is the avatar zone in our design (5.10.3). HP sits under the pedestal (7.4).
- **Sparkle marks.** Ambient decoration is ink under the Section 1 rule. We do not light ambience.
- **Saturated bars and currency icons.** The HP bars and currency icons are fully saturated. Ours: Blood `#B0505A`, muted by design (4.2), and a Candle-tinted coin (4.6).
- **Tier cues by hue alone.** Its tier read leans on hue. Ours leans on plate form first (3.4.4).

**Supports:** 1.1, 3.4.1, 4.3, 5.4, 6.3, 7.3.

**Check at the first mock-up.** Run the lit-mask test (4.10) on both frames at equal scale. Ours must put at least 90% of lit pixels inside things that matter. If it does not, we have drifted toward this reference's weakness.

#### 9.3.3 Reference 3: Dead Cells

**What was confirmed.** Motion Twin's artist Gwenael Masse wrote a developer post on the art direction. As summarised by the search results, it says:
- background colours should blend smoothly, while collisions and interactive elements contrast strongly, so the player reads the action quickly;
- the opening scene puts warm light on the player character against a giant's corpse in cold shadow;
- complementary palettes make interiors feel confined, and analogous palettes give outdoor levels more nuance;
- a violent, cryptic atmosphere does not require desaturated colours.

The character pipeline (3D models rendered to 2D sprites by an in-house tool) is also confirmed, in separate Motion Twin write-ups. It is noted only so it is not mistaken for ours (see Diverge).

**Take (primary): the figure-ground rule.** The things the player acts on or reads contrast strongly. Everything else blends. This is a compositional rule, and it is the same question as Section 1.1, from a game the user plays. We use it as a stated test: before accepting a room, ask whether the interactables are the strongest edges and the backdrop the weakest. It supports the edge-dominance check in 6.3 (at least 95% of strong-edge pixels inside the character zones belong to lead shapes).

**Take (secondary): a warm "you" against a cold world.** One warm light on the player's side, cold everywhere else. This matches our Candle rim and pedestal pools on heroes against a Fog rim on monsters (4.8, 5.4). It confirms that the temperature split is a recognised, working choice. It adds no new colour.

**Diverge so it does not read as a copy.**
- **Saturation.** The developer's own account is a saturated palette. Ours holds every non-tier colour at HSV saturation 0.60 or lower, and saturation is reserved for tier colours and the Red room (4.1; item icons are the one exemption, 4.5).
- **Per-area palettes.** Complementary and analogous palette schemes per area are not adopted. Our areas change by a low-alpha temperature wash on the middle and right columns only (4.8). Hue families stay inside the 41-colour master palette (cap 48).
- **Pipeline and lighting.** No 3D-to-sprite pipeline and no normal-map lighting on sprites. Ours is flat stylised pixel, AI generation plus hand clean-up (6.3, 4.13, 8.11).
- **Motion.** Fluid action-platformer animation is not our target. Characters do not attack here. Items act as avatars, with stiff-and-snappy poses (5.1, 5.5).
- **Setting.** Its architecture and themes are not ours (the Vault, 6.2).

**Supports:** 1.1, 4.8, 5.4, 6.3.

**Check at the first mock-up.** Greyscale the frame and apply the 6.3 edge-dominance test. Then ask: would a viewer say this looks like Dead Cells? The saturation cap and the single-light-per-state rule are the two things that should answer no.

#### 9.3.4 Reference 4: Slay the Spire

**What was confirmed.** Secondary guides and wiki data state that each enemy shows its upcoming action above its head before the player's turn, and that an attack's icon carries its damage number. Icon graphics, per-tier variants and the art style were **not** confirmed (9.8). The concept document already lists this game as the validation of intent readability.

**Take: one glyph plus one number.** The player reads what is coming from a small type glyph and a magnitude, without opening a tooltip. This is the information grammar of our intent tab (glyph, magnitude, target, 7.5). It also supports the rule that intent is visible during planning, because combat is automatic in our game and planning is where the player decides.

**Diverge so it does not read as a copy.**
- **Container.** Ours sits on a downward pennant tab, a reserved shape meaning "enemy intent" (3.2). It is not a free-floating icon.
- **Target.** Ours adds a target chip for the heroes (7.5), because the player must know which hero is threatened.
- **Action.** Our glyph lifts off the tab and performs the action when the enemy acts (5.10.8). Enemies do not lunge.
- **Colour.** Ours is Fog-hi, low saturation (7.5).
- **Rendering.** Chunky pixel art at 640x360, not painted art (8.0).

**Supports:** 3.2, 5.10.8, 7.5.

**Check at the first mock-up.** Show the intent tabs alone, at 1:1, to someone who has not seen the game. They should name the type and the number without a tooltip. If they cannot, enlarge the glyph before adding anything else.

### 9.4 Additivity check

No two references point the same way. Each answers a different question.

| Reference | The question it answers | Section home |
| ---- | ---- | ---- |
| 1: bright forest | How heavy are outlines and frames, and how simple are the shapes? | 3.6, 4.13, 7.3 |
| 2: dark dungeon | How dark is the field, and where does the light sit? | 1.1, 4.3, 5.4, 6.3 |
| 3: Dead Cells | What must stand out from the field, and in which temperature? | 6.3, 4.8, 5.4 |
| 4: Slay the Spire | How is incoming information written? | 5.10.8, 7.5 |

**One deliberate near-overlap, and its boundary.** References 2 and 3 both concern figure-ground. Reference 2 answers *how dark the world is*. Reference 3 answers *what must stand out against it*. If a call can be settled by Reference 2 alone, do not cite Reference 3 for it.

**Pillar coverage.** References support Pillars 2 and 4 (Principles 1 to 3). No reference covers Pillar 5 (cost as subtractive light, the Plum curse tag, 4.14). That is deliberate: Section 4 already specifies it, and no verified reference for it exists. 9.6 asks whether one is wanted.

### 9.5 What is NOT being referenced

| Not referenced | Why |
| ---- | ---- |
| **Overlooting, Megaloot** | The user's favourite roguelikes. Web checks confirmed only that both are pixel-art inventory-management roguelikes on Steam (Steam tags, release details). **No specific visual element could be verified**, so no entry is written and none is invented. If the user supplies screenshots or says which element they like, an entry is added (9.8). |
| **Backpack Battles, Backpack Hero** | Comparables for the mechanic. No source describing their grid, frame or colour treatment was found. They also sit closest to our genre, so copying risk is highest. They stay mechanical comparables, as in the game concept. |
| **Dead Cells' 3D-to-sprite pipeline and normal-map lighting** | Confirmed, but it is a production method we do not use (6.3, 8.11). |
| **Painterly or high-ornament dark fantasy** | Ornament, texture noise and painted backdrops fight silhouettes and drift between AI generations (6.3 alternatives). |
| **Gothic cathedral or crypt architecture** | Pointed arches read as chevrons and tracery reads as lattice, both reserved shapes (3.2, 6.2 alternatives). |
| **Bright, saturated casual palettes** | The user chose the dark, soft tone (1.2). Reference 1's bright tone is explicitly not adopted. |
| **Glow-everywhere neon fantasy** | The "every cell lit" failure that Reference 2 warns about. Light is information only (1.1). |
| **HD-2D-style blur, depth-of-field and parallax on pixel sprites** | Conflicts with binary alpha, point filtering and the two-element ambient motion limit (6.3 alternatives, 8.0). |
| **Fluid action animation as a goal** | Characters do not attack. The action lives on item avatars (5.1). |
| **Living artists, by name** | None named by the user. None used in prompts (9.7, rule 2). |

### 9.6 Non-game inspirations: not yet discussed

The user named no film, book, music, artist or other non-game source. None is recorded here. The questions below are for the user to answer or skip. They are not statements about the user's taste.

1. **Real spaces for the Vault.** Section 6.2 reads the dungeon as a keepers' strongroom, but only as a proposal about lore. Is there a real kind of building (a strongroom, a salt mine, a cistern, a customs house) whose block proportions and arch shapes you would want as the reference for 6.2 and 6.3? *Would give: a proportion reference for walls and arches.*
2. **A flat-value ink or print tradition.** The core rule is "soft ink" with few flat value steps per material. Is there a print or ink-wash tradition, or a particular artwork, that matches the look you imagine for the ink field? We would study it for value structure, never trace it. *Would give: a reference for how few steps a calm surface needs (4.1, 6.3).*
3. **Candle-lit darkness, and cost as taken-away light.** The Candle rim and pool (hero warmth) and the Plum curse (light subtracted, Pillar 5) are the two lighting ideas with no outside reference yet. Is there a film still, painting or photograph of a single warm light in a dark room that feels right for the Candle, or a dim, heavy image that feels right for the curse? *Would give: a reference for 4.8, 4.14 and 5.4.*

### 9.7 Reference rules (solo developer, AI-assisted pixel art)

1. **Study the principle, never the pixels.** Record a reference as one element, one divergence and one section. Do not trace, recreate or screenshot-match any sprite, icon or layout.
2. **AI prompts must not name living artists, and must not ask to copy a game's art, a studio's style or a specific game's characters or UI.** Prompts are built from the manifest entry plus the 4.13 rules and the palette, outline, light and shape vocabulary of this bible (8.11 step 1). A prompt says "1 px INK-0 outline, top-left key light, three value steps, Section 3.2 vocabulary", not "like Game X".
3. **No reference image is ever an input to a generator.** This includes the two user screenshots, which are of games not named in the project and are study material only. Do not place them in `assets/` or use them for image-to-image or style-reference features.
4. **Never sample colour from a reference.** Colour comes from the master palette (4.2 to 4.8). Every generated image is quantised to it (4.13).
5. **The bible outranks a reference.** If a reference contradicts a locked rule, the rule wins and the reference note is revised, not the rule. Conflicts go to the creative-director.
6. **One divergence per entry is a tested requirement.** At each full-frame mock-up, view it at equal scale beside each reference and run the "Check at the first mock-up" test in 9.3. If a viewer could name the source game, a divergence has failed.
7. **A reference must add a direction.** The cap is five. A sixth requires removing one or stating what new question it answers (9.4).
8. **Record provenance and uncertainty.** An entry states how it was checked and what was not. An unverified claim is flagged, not written as fact (9.8). This also feeds the AI provenance record in 8.11 step 8.
9. **Favourite does not mean visual source.** That the user plays a game does not make its visuals a reference. An entry exists only when an element is named and verified.

### 9.8 Verification status and open questions

**Verified (2026-11-10).**
- The two screenshots were viewed directly. Fine text and the games' names could not be read, and their native pixel scales were not measured.
- Dead Cells: the 3D-to-sprite character pipeline and the art-direction post, via search summaries of Game Developer ("Art design deep dive: giving back colors to cryptic worlds in Dead Cells"), JikGuard ("Art design deep dive: using a 3D pipeline for 2D animation in Dead Cells") and 80.lv ("Case study: Dead Cells character art pipeline").

**NOT VERIFIED.**
- **Dead Cells.** The full art post was read only as search summaries, not page by page. The quoted principles (background blend, interactive contrast, warm player light) should be confirmed against the full post before being quoted in any external document.
- **Slay the Spire.** Only that intent icons appear above enemies and that an attack icon carries a damage number, from secondary guides and the community wiki's enemy data. Icon graphics, variants and rendering were not confirmed.
- **Overlooting and Megaloot.** Only genre and tags, via the Steam store page and an aggregator. **No visual claim is made.**
- **Backpack Battles and Backpack Hero.** No source describing their visuals was found.
- **Both screenshots.** Whether "chunky" corresponds to a 1 px outline at a 640x360 native scale is not measured.

**Open items.**
1. The user supplies screenshots of Overlooting or Megaloot, or names a specific element, to add an entry.
2. The user answers any of the three questions in 9.6.
3. Whether any reference notes or images are stored in the project, and where. Rule 3 forbids storing the screenshots in `assets/`. A path under `design/art/` has not been decided.
4. Confirm the Dead Cells principles against the full developer post before external quotation.

### 9.9 Alternatives considered

| Alternative | Verdict |
| ---- | ---- |
| **Four verified references, as above** | **Chosen.** Each is additive and each divergence is stated. |
| Add Overlooting and Megaloot as entries now | Rejected. Their visuals are unverified, and inventing a lesson would be fabrication. They are added when the user supplies material. |
| Add Backpack Battles and Backpack Hero for item-grid look | Rejected. Unverified, and the closest comparables carry the most copy risk. |
| Invent non-game references (a film, an artist) to fill the five slots | Rejected. The user named none. 9.6 asks instead. |
| A reference image folder fed to the generator as a style reference | Rejected. Conflicts with rules 2 and 3 (copy risk, provenance). |

### 9.10 Rule and pillar coverage

| Rule or pillar | How Section 9 serves it |
| ---- | ---- |
| Section 1.1 (light is information) | References 2 and 3 are chosen for it. Both divergences guard against decorative or over-saturated light. |
| Principle 2 (form before hue) | The Reference 2 divergence (hue-only tier read) and Reference 4 (glyph plus number, not colour) reinforce it. |
| Simplicity guard (1.2) | Reference 1 divergence: no frame ornament. |
| Principle 3 (escalate by adding) | Reference 4: intent stays readable and never gets covered. |
| 4.13 (AI pipeline, LOCKED) | Rules 2 to 4 restate that prompts and colour come from the bible. |
| P4 Arrangement Is the Thinking | References 1, 2 and 4. |
| P2 The Payoff Is the Product | References 2 and 3 (a quiet field gives headroom). |
| P5 Power Has a Price | No reference. A gap, raised in 9.6 question 3. |
| P1, P3 | Systems pillars with no visual rule (1.4). |
