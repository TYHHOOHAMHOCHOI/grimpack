# Game Concept: Grimpack (working title)

*Created: 2026-11-09*
*Last Updated: 2026-11-14*
*Status: Draft*

> **Provenance note.** Built in a `/brainstorm` session from the user's implementation
> plan (`game_implementation_plan.md`, a Unity 6 plan) and their answers. Reviewed by
> three director gates: CD-PILLARS (CONCERNS, addressed in the pillars below),
> AD-CONCEPT-VISUAL (STRONG), TD-FEASIBILITY (CONCERNS), PR-SCOPE (OPTIMISTIC).
> Items marked *(assumption)* were not stated by the user. Items marked
> **NOT SOURCEABLE** have no pinned reference in this repo yet.
>
> **Design review 2026-11-14: NEEDS REVISION, then revised the same day** (Unique Hook,
> hero order, MVP hypothesis and payoff ladder, Pillar 5 in the MVP, roster).
> **Re-review 2026-11-14: NEEDS REVISION again, then revised the same day** (the Unique
> Hook claims, Pillar 5 in the MVP, residual "hero order" text, the lean ceiling, the MVP
> schedule and first cuts, the go/no-go criteria, false source claims). Owner decisions D1
> to D4 of that round are recorded in the review log.
> **Third review 2026-11-14: NEEDS REVISION, then revised the same day** (the lean ceiling
> withdrawn pending an exact pool computation, door-buff wording, an 8 to 10 month MVP
> estimate and reordered first cuts, a go/no-go and kill/pivot rule that can fire, Surge and
> boss moments unskippable, a Hook decision trigger). The decisions of that round (also
> called D1 to D4 in the log, a separate set) are recorded in the review log. The concept
> states intent only: numbers live in their owning GDD (System 18, System 35, the playtest
> plan) and are cited, not restated.
> **Fourth review 2026-11-14: MAJOR REVISION NEEDED, then revised the same day.** The
> verdict was about the document type, not the game: for four rounds the concept carried
> operational numbers (tester counts, thresholds, hours, schedules) that no owning document
> backs. This revision removes them. The concept now states intent; every count, threshold
> and denominator belongs to the owning GDD, the playtest plan (not yet written) or the
> `/estimate` result, and the concept adds no new number. Owner decisions of this round:
> Skip fast-forwards to a Detonation instead of past it (withdraws the earlier "Surge and
> boss moments cannot be skipped" wording); the record is relative to the boss, with its own
> beat for the run-defining run; the run-defining share is a definition constant; the MVP
> estimate is re-run with `/estimate` before it is cited. See
> `design/gdd/reviews/game-concept-review-log.md`.

---

## Elevator Pitch

> It's a turn-based pixel-art roguelike where you lead three heroes through a cursed
> dungeon, arranging each hero's grid backpack so their items chain into combos on
> their own, and every door you pick is a bet on a stronger item row at a higher risk.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Roguelike, turn-based auto-battler, backpack-battler; dark fantasy |
| **Platform** | PC (Windows). No commercial release is planned; a Steam or Epic release is optional and nothing in the scope assumes it. |
| **Target Audience** | Achievers who chase record runs; secondary Creators and Explorers (see Player Profile) |
| **Player Count** | Single-player |
| **Session Length** | The MVP run (one floor: 8 combat rooms and a boss) is estimated at 25 to 30 minutes (see run-state-and-flow.md). A later two-floor run is assumed to take 30 to 60 minutes *(assumption; to be measured)*. |
| **Monetization** | None. The user does not plan a commercial release. No real-money gacha (anti-pillar). |
| **Estimated Scope** | Large (solo, full-time). No full-vision figure is stated until the MVP is re-baselined with `/estimate` (see Scope Tiers). |
| **Comparable Titles** | Backpack Battles, Backpack Hero, Slay the Spire |

---

## Core Fantasy

You are the architect of a perfect machine. After many runs, one run finally lines up:
the right items, the right door choices, the right speed items and placement across the three heroes. Then it fires, and
stacked damage detonates into the biggest blow the player has ever dealt to a boss. The
player is chasing that run. The promise is that the run is rare, that the player's own
arrangement made it possible, and that watching it go off is a thrill. The record is
**relative to the boss** (a blow that takes a large share of its health), not an absolute
number, and the run-defining blow gets a presentation of its own so the rare run is
unmistakable and never looks like an ordinary Detonation (owner decision, fourth review;
System 29 and System 30 carry the beat).

---

## Unique Hook

Like Backpack Battles, **and also** you run a three-hero party that is **one machine in
three stages**: one hero sets the trap (applies debuffs and buffs), another springs it
(detonates them), on clocks of their own. Every door is a bet on the item row and the
danger it brings, and the perfect run is meant to be a rare fireworks show (the rate is
set in System 35 and is not assessed in the MVP).

What the party adds over one big backpack, according to the specs:
- Each hero has its own cooldown clock and its own innate item, so three clocks run in
  parallel, and a setup on one hero and its payoff on another are spread across time with
  enemy actions in between.
- A Reaction item on one hero can fire off another hero's item firing or status
  application (item-trigger Rule 11), so a detonator on one hero can answer a setup on
  another.
- A downed hero's half of the machine goes dark, so keeping the right hero alive is part
  of the build.
- The speed order is shaped by which grid holds the speed items (a speed bonus does not
  depend on cell position; position only changes firing order inside one hero); the party
  slots are fixed once the run starts. *Sequence across heroes is part of the build.*

What the hook does **not** claim: a setup item and its detonator can still sit in one grid
and fire in one action (item-trigger Rules 5, 8 and 11), and a strongly committed
direction can fit in one 16-cell grid. The party is not the only way to combo. The claim
to prove is that the strongest builds use more than one hero. The test must compare
builds with an **equal item or cell budget** (three grids against one would win on
capacity alone), and the split build must win by a set margin. System 35
defines the comparison (question 14, provisional). It is reported in the MVP and becomes
a gate at the Vertical Slice (see the go/no-go criteria).

**Decision trigger (third review, D4; extended in the fourth).** Question 14 is reported in
the MVP and carries a decision: if the split build does not beat the packed build by the
margin System 35 sets, the "three stages" wording of this Hook and of Pillar 4 is rewritten
before the Vertical Slice, and the party structure itself is reconsidered (the design
fallback is fewer, more distinct heroes). The result must hold under more than one bot
policy, because a weak cross-hero search would bias the comparison against the split. This
is a decision point, not a ship block.

---

## Player Experience Analysis (MDA Framework)

### Target Aesthetics (What the player FEELS)

| Aesthetic | Priority | How We Deliver It |
| ---- | ---- | ---- |
| **Sensation** (sensory pleasure) | 2 | The escalating payoff: number pops, light blooming from the grid, rising audio. Small triggers stay quiet so big chains stand out. |
| **Fantasy** (make-believe, role-playing) | 5 | A dark-fantasy party delving into a cursed dungeon; the player as the planner behind the party. |
| **Narrative** (drama, story arc) | N/A | Not a target. No story budget in the core. |
| **Challenge** (obstacle course, mastery) | 3 | Door tiers raise difficulty; run odds improve with commitment and arrangement skill. |
| **Fellowship** (social connection) | N/A | Single-player. |
| **Discovery** (exploration, secrets) | 4 | Finding item synergies and cross-hero setup-and-detonate combos. |
| **Expression** (self-expression, creativity) | 1 | Build variety: how to lay out three grids, which hero carries what, which direction to commit to. |
| **Submission** (relaxation, comfort zone) | N/A | Not a target. |

### Key Dynamics (Emergent player behaviors)
- Players commit to a build direction early, then read doors and drops for what serves it.
- Players experiment with speed items, item placement and which hero carries what to discover setup-and-detonate combos.
- Players weigh high-tier doors as bets: a stronger item row and buff against a harder room. The buff is a max-HP bonus only (door Rule 6) and heroes are restored before every fight, so in the MVP the bet is about the item row and survival, and the price is the danger of the room; the first build-shaping price is the Gold curse (Tier 1.5), and Red follows in Tier 3.
- Players start new runs hoping to reproduce a "perfect" one, and study why a run did or did not go off.

### Core Mechanics (Systems we build)
1. **Grid backpacks with auto-trigger and adjacency synergies.** Each hero has a grid (4x4 start, expandable); shaped items (1x1, 1x2, 2x2, L) can be rotated; a shared stash holds spare items. On a hero's turn, items whose turn-cooldown has elapsed fire automatically; adjacent items modify each other.
2. **Speed-gauge turn order with three heroes.** Heroes and monsters fill speed gauges and act in order. Turn order is the time axis of the build.
3. **Enemy intent.** Monsters show their next action. Intent and turn order must be visible *before* combat, while the player can still rearrange.
4. **Door progression.** After each room the player picks 1 of 3 door cards across 6 colour tiers (White, Green, Blue, Purple, Gold, Red; the MVP has 4: White to Purple). Higher tier: bigger buff, harder room. Gold and Red carry a curse or trade-off.
5. **Commitment-biased, capped drop model.** Once the player has committed to a build direction, reward item offers lean toward it (System 18 sets when the lean can start; door buffs are neutral, certain rewards), with a hard cap on how much.

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | Choosing the build direction, the arrangement, who carries what, and which door to risk. | Core |
| **Competence** (mastery, skill growth) | Reading synergies, shaping the speed order, and understanding why a run did or did not go off. Skill raises the odds of the perfect run. | Core (depends on readable cause and effect) |
| **Relatedness** (connection, belonging) | Each hero has a mechanical identity (its innate item, its speed and its role in the sequence). The player attaches to "my detonator hero". No story budget required. | Minimal |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers**: chase record runs, optimise builds, clear harder difficulties.
- [x] **Explorers**: discover item synergies and cross-hero combos.
- [ ] **Socializers**: not served.
- [ ] **Killers/Competitors**: not served (no PvP). Records are a personal benchmark.

Secondary appeal: Creators (Quantic Foundry), who enjoy assembling a build their own way.

### Flow State Design

- **Onboarding curve**: *(to be designed)* First minutes should teach one idea at a time: place an item, see it fire, then see an adjacency bonus, then the speed order across heroes. Detail belongs in `/ux-design`.
- **Difficulty scaling**: Door tiers set the room's danger; the player chooses how much risk to take, so the player steers the curve.
- **Feedback clarity**: Firing-order numbers on the grid, an escalating payoff that shows which arrangement caused it, and a post-combat breakdown.
- **Recovery from failure**: A fast restart. Losing should be educational: the player can see why via intent and the event log.

---

## Core Loop

### Moment-to-Moment (30 seconds)
The loop is arrange, preview (turn order and enemy intent), then watch: items fire in a
visible order, numbers pop, and setups from earlier heroes pay off. Because combat is
fully automatic, the *watch* beat must be satisfying even without a perfect run (small
triggers quiet, big chains unmistakable). Playback speed and Skip exist, but **Skip
fast-forwards to the next Detonation, plays that beat in full, then continues** (owner
decision, fourth review; it replaces the third-review wording that Surge-class moments and
the boss fight cannot be skipped, which would have forced a very long fight to be watched
every time). The payoff is therefore never skipped past, and `payoff-presentation.md`
carries the rule (an amendment to its Rules 11 and 20 is a follow-up).

### Short-Term (5-15 minutes)
Pick one of three doors, enter the room, win, take the loot and buff, then rearrange
the backpacks. The recurring question is "which direction am I building, and does this
door serve it?". The risk and reward of each door tier drive the next pick; there is no
cash-out, so the stake is the run itself.

### Session-Level (25-30 minutes for the MVP; 30-60 for a later two-floor run)
One run across the floors up to the boss (about 25 to 30 minutes for the MVP's single floor), with a natural stopping point after each
floor and after the boss. The hook between sessions is the memory of a run that
almost went off.

### Long-Term Progression
In a run: items, buffs, extra backpack slots. Across runs: unlocked item pools, heroes
and door types. Long-term goals: harder difficulty levels and record runs. *(The broader
records system, replay and higher difficulty levels are Tier 3; a small cross-launch
personal best is in MVP by user decision.)*

### Retention Hooks
- **Curiosity**: Unseen items, synergies and cross-hero combos; locked content.
- **Investment**: Meta unlocks the player does not want to lose.
- **Social**: None.
- **Mastery**: Reproducing the perfect run more often; harder difficulty; record numbers.

---

## Game Pillars

### Pillar 1: Luck Follows Commitment
Once the player has visibly committed to a build direction, reward item offers lean toward
it (System 18 sets when the lean can start; door buffs are neutral, certain rewards). The
player can still pivot. Whether pivoting costs something real in the MVP is **not yet
shown**: the MVP has no sink, so the only cost is lag in the lean, and System 18 or a
later system must name a real cost or this pillar drops "pivoting costs something" for the
MVP. This pillar owns the *direction* luck leans: the lean changes the composition of what
is offered, never the `drop_bias_cap`, the item count or any item's magnitude.

The lean is a *direction the player can feel*, not a source of power, and **its size is
unmeasured on the real catalog**: this document claims neither that the lean is small nor
that it is large. The earlier sketch (a several-fold effect) is superseded by System 18's
own finite-pool correction, and the earlier ceiling of 1.5 is withdrawn (third review,
decision D1). The two limits measure different things: a *perceptibility floor* (a lean the
player cannot feel is not this pillar) is about what the player sees in an offer, and a
*power ceiling* is about the run-defining rate with the lean off and on. System 18 computes
the exact pool and System 35 measures the lean-off arm (question 15); both limits are set
from those results, each with its own run count. They must be re-derived for each pool
size, because the lean grows with the pool. `drop_bias_cap` is owned by System 18; this
pillar owns the *direction* of the lean, Pillar 3 owns the power it produces. **Trigger:**
if no cap can reach the floor without pushing runs toward one build, Pillar 1 is reworked
rather than the floor lowered.

*Design test*: Debating random drops vs build-weighted drops, we choose build-weighted,
unless the player cannot feel the difference. Debating hard lock-in vs pivoting, we choose
pivoting, and the cost of pivoting must be real before we call it a cost. When the lean
starts to crowd out Expression (runs converging on the same build) or breaks the ceiling once it is set,
the ceiling wins and the lean is cut back, but never below the perceptibility floor. ("Committed" is defined in System 18.)

### Pillar 2: The Payoff Is the Product
Synergy ignitions escalate in sight, sound and feel with combo size. Small triggers
stay quiet, big chains are unmistakable, and the player can always see which
arrangement caused them.

*Design test*: Debating a cleaner UI vs a more dramatic ignition effect, we choose the
dramatic effect, unless it hides information. When a bigger effect and a clearer cause
conflict, the clearer cause wins.

### Pillar 3: Earned Rarity
The perfect run is rare by default and reachable through skill. Commitment and
arrangement raise its odds; nothing guarantees it. This pillar owns the *magnitude and
frequency* of power. The draft target is that a committed, competent player reaches a
run-defining combo only rarely (System 35 owns the target band, the way it is measured and
the list of levers).

A run is **run-defining** when the boss is defeated and one Detonation-class root action
takes a defined share of the boss's maximum HP (the registry entry
`run_defining_damage_share`; shield lost counts, and the blow must land while the boss
still has at least that share left). That share is a **definition constant, not a tuning
lever** (owner decision, fourth review): moving it moves the goalpost for "rare" without
changing anything the player does, so a change needs the creative director's sign-off.
Rarity is tuned through the levers System 35 lists, and the levers the specs say matter
most are the door tier rows, boss HP, item magnitudes and the player's own arrangement
and survival; the lean (Pillar 1) is a minor modifier by System 18's own correction. The
model behind the target must be the real finite pool, not a with-replacement sketch. Until
System 35 reports how often every key item is offered and how many distinct item sets are
run-defining, the target is a hope, and the perfect run may turn out to be a near-fixed
item set. Boss HP has only a provisional range in System 14, so the run-defining rate and
the boss win rate are tuned together.

*Payoff ladder.* Between a quiet trigger and the run-defining peak there are rungs, mapped
onto the four classes System 29 assigns to each root action (Tick, Chain, Surge,
Detonation): (1) a visible chain, regular and easy to read, in most committed runs; (2) at
least one Detonation-class action in a run; (3) the run-defining blow, which is a subset of
(2) and has **its own presentation beat** so that it never looks like an ordinary
Detonation (System 29 and System 30 carry it; the owner decision of the fourth review). The
rungs are per-run probabilities, not per-action percentiles or expected counts: System 35
states them as per-run bands and must show that rung 1 can fail (a rung that cannot fail is
not a test). The post-combat breakdown is meant to show what was missing for the next rung
so that a near miss is readable (System 30 Rule 12, a Vertical Slice feature; the MVP
breakdown shows the biggest hit and the longest chain only). Human playtests judge rung 1;
rung 3 is too rare for them and is judged by the harness, and the playtest plan adds a
cheap proxy: testers watch a scripted run-defining run and say whether it reads as
different and whether they would chase it.

*Design test*: Debating a generous vs a stingy door row for the higher tiers (the MVP's
version of the question; the Gold row applies from Tier 1.5), we choose the rate that keeps
a committed player inside System 35's target band. If a change makes a run-defining combo
independent of player skill, we reject it. The measure is the harness's skill axis: a
change that leaves the run-defining rate the same for the Competent policy and for a
noisier one is skill-independent (the margin is set in System 35). Debating a rarer
run-defining combo vs a build that more players can discover, we choose rarity only while
discovery does not collapse into "know the recipe"; that conflict with Expression is an open
owner ruling (see Design Risks).

### Pillar 4: Arrangement Is the Thinking
All player skill happens before combat: grid layout, distribution of items across three
heroes, and door choice. The three heroes are three stages of one machine; turn order
lets one hero set up (buff, debuff) and another cash in, so sequence is part of the
build. The player shapes that order through speed items (which grid holds them; a speed bonus
does not depend on cell position); the three party slots are fixed once the run starts. Everything the player needs to decide (item effects, turn order, enemy intent)
is visible before the fight starts.

*Design test*: Debating an extra in-combat action vs a deeper pre-combat arrangement, we
choose pre-combat. Debating an effect shared by all heroes vs one whose value depends on
speed order or position, we choose the order- or position-dependent one, unless it makes the first runs unreadable for a new player.

### Pillar 5: Power Has a Price
Great power is always tainted in this world. High-tier doors and items cost the player
something real, and art, audio and text make that cost felt. A drawback that changes how
the player builds counts; one the player can ignore does not. In the MVP the only price is the danger of the room (door Rule 11 gives Purple no extra cost), so **the MVP does not test this pillar**. The first build-shaping price is the Gold curse (Tier 1.5, System 19); Red follows in Tier 3.

*Design test*: Debating a pure upgrade vs an upgrade with a drawback on a Gold or Red
door, we choose the drawback. Debating a numerically trivial drawback vs one that changes
how the player builds, we choose the one that changes the build.

### Anti-Pillars (What This Game Is NOT)

- **NOT active in-combat actions** (no manual skills or targeting): it would weaken Pillar 4 and the watched payoff.
- **NOT uncapped drop rates or "every run is overpowered"**: it would destroy Pillar 3.
- **NOT a second curse or corruption subsystem** (no stress meter, no corruption tree): it would bloat scope and crowd out Pillar 4. Curses stay tied to door cards and a few items.
- **NOT real-money gacha or monetisation hooks**: "gacha" here is the in-game feeling of anticipation only.

---

## Visual Identity Anchor

**Direction: "Ink and Ember", simplified.** (Chosen from three art-director directions;
adapted to the user's two pixel UI references: simple, chunky, easy on the eyes.)

- **Visual rule**: The world is soft ink; only things that matter emit light.
- **Style**: Simple 2D pixel art, chunky shapes, gentle contrast (dim and soft rather than harsh black). UI frames are simple and thick. Layout follows the three-column plan.
- **Supporting principles and tests**:
  1. *Light is information.* Squint test: build a lit mask and a mattering mask of the frame; at least 90% of the lit pixels must fall on items, tier markers, active synergies, the current-turn indicator, intent and HP. The per-state lit-area budgets are in art bible Section 4.10 (proposals until the first full-frame mock-up; this replaces the earlier "brightest 10% of pixels" wording). **Section 4.10 is normative** for the masks, the lit threshold and the frames tested; the list here is a summary, and the tier-separation and freeze-frame tests below get their metrics there, not here.
  2. *Tier is read by form before hue.* Each of the six tiers gets a non-colour cue (frame notches, corner pips or border shape). Test: greyscale and colour-blind simulation must still separate all six.
  3. *Escalate by adding, not covering.* Combo size drives scale, particles and number size, never overlays on the grid. HP, intent and the grid sit in a protected layer. Test: a freeze-frame of the biggest payoff still shows every intent icon, HP value and which items fired.
  4. *Silhouette first.* Each item is identifiable by outline alone at native pixel scale.
- **Colour philosophy**: Low-saturation ink-blue, slate and bone base; warm light marks "you" and your active items, cold light marks the dungeon; saturated colour only on tier-bearing objects, active synergy effects and the Red room (**item icons are the one exemption: they may use any master-palette colour, user decision 2026-11-10, art bible 8.5.3**). Each tier is a small value ramp so it can brighten during a payoff.
- **Colour conflicts to settle in the art bible**: red HP and damage vs the Red tier; decorative gold and currency vs the Gold tier; ambient poison green vs the Green tier; night or curse purple vs the Purple tier; white UI text vs the White tier.
- **Visual references**: two screenshots the user supplied (a bright forest scene with side panels, and a dark dungeon with tier-coloured item frames, three units and a turn counter). They are not stored in the repo yet; add them with the art bible.

---

## Inspiration and References

| Reference | What We Take From It | What We Do Differently | Why It Matters |
| ---- | ---- | ---- | ---- |
| **Backpack Battles** | Auto-combat, items that fire on cooldowns, adjacency synergies | A three-hero party with turn-order combos; a PvE roguelike with risk/reward doors | Closest comparable; proves the core loop and sets what we must differ from |
| **Backpack Hero** | Grid-backpack arranging as the main puzzle | Fully automatic combat and a payoff-first design | Validates grid inventory as a core mechanic |
| **Slay the Spire** | Enemy intent shown before it acts; run structure | Intent is shown for pre-combat planning, since combat is automatic | Validates intent readability and the run loop |
| **Dead Cells**, **Megaloot**, **Overlooting** (the user's most-played) | Build-crafting through loot and the thrill of an aligned run | Build assembly through arrangement rather than action | The audience we are building for plays these |

**Non-game inspirations**: not yet discussed. To be added with the art bible.

---

## Target Player Profile

| Attribute | Detail |
| ---- | ---- |
| **Age range** | *(assumption, unvalidated)* adult players of build-crafting roguelikes |
| **Gaming experience** | Mid-core to hardcore *(assumption)* |
| **Time availability** | MVP runs of 25-30 minutes, later runs of 30-60 *(assumption)* |
| **Platform preference** | PC |
| **Current games they play** | Backpack Battles, Backpack Hero, Slay the Spire, Dead Cells *(from comparable titles)* |
| **What they're looking for** | The thrill of a perfect, rare, self-built run; a build they assembled by thinking |
| **What would turn them away** | Needing reflexes or manual input in combat; story-heavy play; a game where every run is overpowered or where outcomes feel random |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity (the user's stated preference), pinned in `project.yaml` as `6000.6.3f1` (Unity 6.6), 2D URP; the installed editor matches. `docs/engine-reference/unity/` was updated to 6.6 on 2026-11-09 and lists what is **NOT SOURCEABLE**. One case that is sourceable here: `ProjectSettings/EditorSettings.asset` already enables Enter Play Mode options with domain reload off (`m_EnterPlayModeOptions: 1`), so static state survives between Play sessions. Unity 6.6 is not an LTS and is supported only until 6.7 ships (a beta is out); re-pin when 6.7 LTS ships. |
| **Key Technical Challenges** | See Technical Risks. Chief among them: deterministic effect-resolution order within a backpack and across three heroes; simulation separated from presentation; headless balance simulation; drag/drop grid UI. |
| **Art Style** | Simple 2D pixel (see Visual Identity Anchor) |
| **Art Pipeline Complexity** | Medium: AI-assisted pixel art plus hand clean-up; needs a locked master palette, outline rule and light direction. Items and monsters are the labour-heavy part. |
| **Audio Needs** | Moderate: asset-pack music and SFX; payoff audio matters for Pillar 2. |
| **Networking** | None |
| **Content Volume** | Full vision: about 60-80 items, up to 6 heroes, about 15 monster types plus 3 bosses, 6 door tiers, shop, events, recruit rooms, 3 floors. MVP: about 20 items, a roster of 4 heroes (pick 3), 6 regular monster types plus 1 boss (System 16's table; each needs art and intent authoring), 1 floor. |
| **Procedural Systems** | Door generation, commitment-biased capped drop model, monster selection. Seeded, reproducible RNG with named substreams (doors, drops, monster intents). |

---

## Risks and Open Questions

### Design Risks
- The payoff may not land: if the escalation is not exciting by the boss, more content will not fix it.
- Automatic combat may feel random if the player cannot say why they lost (intent and trigger feedback must be readable).
- Biasing drops toward the committed build can lock runs into the first items found, which shrinks Expression. Pivoting must be possible, and its cost must be real (see Pillar 1): in the MVP it is only lag in the lean.
- Players will watch automatic combat hundreds of times; spectacle that does not scale with combo size will tire them (needs speed control).
- Combinatorial balance (items x heroes x placement x order) cannot be hand-tuned.
- Heroes may collapse into "one big backpack split in three" unless position and order give items different value. The harness comparison named in the Unique Hook (System 35 question 14) is the test, and the Hook carries the fallback if it fails.
- Runs may converge: the MVP has no sink (heroes are restored before every fight, skip and stash moves are free) and only a small droppable pool, so a run is offered a large share of the pool. System 18 sets the copy cap and owns the offer rules, `grid-backpack.md` owns the stash size, and "pool pressure" is left to System 35. The real limits are one pick per offer, the number of offers and the copy cap, not capacity. System 35 question 16 reports build diversity (distinct final builds); it should be run together with the lean-off arm, not deferred before it, because a near-fixed item set is the base case, not the exception.
- The run-defining target rests on a bot "competent" policy that is not yet calibrated against human runs (System 35 states how many human runs calibrate it). With a small droppable pool the run-defining combo needs only one or two key items, so it may be a near-fixed item set, the same perfect run every time. Then "reachable through skill" collapses into "know the recipe" and Expression (the top aesthetic) suffers. System 35 should check this, and the conflict between Pillar 3 and Pillar 1/Expression needs an **owner ruling** before the Vertical Slice.
- Pillar 5 has no MVP test (see Pillar 5). The risk is that the build-shaping price is first judged at Tier 1.5.
- The fully automatic combat gives no in-combat input; speed control and skip are needed, not optional. Skip fast-forwards to the next Detonation and plays it in full (fourth-review decision; the payoff spec still needs the amendment).
- Meta-progression that unlocks item pools changes the pool size and so the run-defining target and the lean (both must be re-derived per pool size); keep unlocks sidegrades (a Tier 2 problem; no GDD owns meta-unlocks yet).

### Technical Risks
- **Effect resolution order** (highest): the apply-then-detonate promise needs a defined resolution model (a one-page spec, composable effect data, a reaction queue with a depth cap). The current prototype applies effects immediately in a fixed order and has no "detonate" concept.
- **Simulation coupled to presentation**: the speed gauge uses floats advanced by frame time, so ties can depend on frame rate. Needs integer gauges, a full tie-break rule and one simulation as the source of truth.
- **Reproducibility**: RNG falls back to `Environment.TickCount` when no RNG is passed, so runs cannot be replayed. Needs one seeded run RNG.
- **Determinism contract** (the ADR fixes the details): the simulation core has no mutable static state (the project runs with domain reload off, and the prototype has static counters and caches); the event-log hash uses an explicit algorithm over canonical bytes, never `GetHashCode`; the RNG is self-implemented, not `System.Random`; arithmetic is integer-only with a stated overflow policy; collection iteration and sorting have total-order tie-breaks; and parity is checked on IL2CPP as well as Mono, because players get IL2CPP. The core is a new engine-free assembly (`Project05.Runtime` still references the engine) and targets C# 9.
- **Headless simulation** to verify the 1-in-N target: recommended an engine-free combat and drop core. Do not kill the open Unity Editor for batchmode runs.
- **Drag/drop grid UI** on three grids plus a stash (rotation, invalid-placement feedback, adjacency lines, firing-order numbers, tooltips).
- **Payoff VFX** must not hide information (drive from an event log, cap simultaneous popups).
- Stalemates, unbounded reaction chains, and integer overflow from escalating numbers.
- Fixed-field effect stats will not scale to "consume stacks"-style effects.
- Hard-coded Vietnamese strings with emoji inside logic: move to a string table; pixel fonts need diacritic coverage.
- Stable IDs on data objects are needed for future saves.
- *Status update 2026-11-14:* integer gauges with a full tie-break (turn-order), seeded named substreams (seeded-rng), and reaction depth and effect limits of 8 and 256 (effect-resolution-model) are now specified. Still open until the combat spike runs: identical event logs across the Editor tests, the headless harness and live play, and the overflow boundary at the damage cap.
- *Added in the third review, revised in the fourth (2026-11-14):* the UI stack (UGUI or UI Toolkit) is undecided and the GDDs list pixel-perfect rendering, diacritics and focus behaviour as NOT SOURCEABLE for 6.6, so a short spike precedes the architecture ADR; the ADR sizes it, and it must cover a rotated multi-cell drag across two grids, gamepad focus, Vietnamese combining diacritics at the pixel-font size and pixel-perfect scaling, deciding from the spike and not from the engine reference. The MVP runs past the date 6.6 loses support (when 6.7 ships), so a forced re-pin and a budgeted re-verification belong in the plan, taken at a checkpoint after a playtest. The engine-free core is a separate assembly with no engine references (the current runtime assembly references engine packages, so it cannot be that core), and the harness is a plain dotnet project outside `Assets/` that shares its source (batchmode is blocked while the Editor is open). IL2CPP parity is checked on a Release build, and the build uses the same managed-code variant as the shipped one. The details of the determinism contract (culture and encoding, hashed substream seeds, stable sort, banned APIs including Dictionary or HashSet iteration, overflow enforcement by explicit helpers, the C# 9 limits, a same-seed-twice test in Play Mode that catches leaked statics) belong in the determinism ADR, not here.

### Market Risks
- The backpack-battler space is crowded (Backpack Battles, Backpack Hero). Differentiation rests on turn-order combos and door bets. Lower weight while there is no commercial release.

### Scope Risks
- Estimates were about 2x too tight for a first-time solo developer (producer review). The prototype in `Assets/_Project/` saves little: the systems index requires its simulation to be refactored, not reused. The MVP estimate is **not stated here**: it is owned by `production/milestones/mvp.md` (the `/estimate` of 2026-11-14, low confidence, no sprint history), which withdraws the earlier figures, including decision D2's. `systems-index.md` cites the same file. Day 0 is the start of the combat core spike. The milestone file also proposes the MVP definition of done (every gated criterion assessed, not passed), which the owner still has to confirm, and it holds the recovery of each first cut.
- The 2026-11-14 revisions added MVP deliverables (persistent personal best, command-log export and headless replay with hash parity, speed control and skip, the contribution panel, the harness comparisons) without removing any. The ordered first cuts in the MVP Definition are the contingency.
- No tier as originally drafted included saving, audio, settings or onboarding. Tier 1.5 holds saving, audio from asset packs and onboarding; the MVP holds minimal settings, input and localization (index systems 37, 6, 7) and 3-4 placeholder sound cues.
- The prototype has already drifted into out-of-MVP scope (a shop, and party-recruit parameters in the door generator). Freeze them.
- Curse ownership is decided: the Gold curse ships in Tier 1.5 (System 19, the index's Vertical Slice) and Red with its curse in Tier 3. The number of recruitable heroes in Tier 2 is still open.

### Open Questions
- What counts as "committed" to a build direction (item tags in the backpacks, an explicit choice, something else)? *Resolved 2026-11-14 in commitment-and-drop-model.md (System 18): at least three items in one catalog direction and a clear lead over the next; verify in the combat spike.*
- What is the right target band for the perfect run? Resolve with the simulation harness and playtests; System 35 owns the band and the way it is measured.
- ~~Is the Core Fantasy's "record-breaking number" absolute or relative to the boss?~~ *Resolved 2026-11-14 (fourth review, owner decision): relative to the boss, with a presentation beat of its own for the run-defining blow. System 29 and System 30 carry the beat; System 30 owns the personal-best metric.*
- What is boss HP? System 14 gives only a provisional design-time range, and the run-defining share is a definition constant (Pillar 3), so boss HP is one of the levers System 35 tunes together with the boss win rate. System 35 owns the measurement.
- How far ahead should the turn-order preview promise (draft: next 6 actions assuming no speed change)?
- Does the effect-resolution model support apply-then-detonate across two heroes, with identical event logs in tests, simulation and live play? Resolve with the combat core spike.
- Can the colour conflicts between the tier palette and HP, currency and curse visuals be resolved? Resolve in `/art-bible`.
- Does a Gold curse count toward the run-defining target? A curse lowers the run-defining rate, which Pillar 3 tunes with many other levers. Decide with the first Gold design (System 19), and treat it as a conflict between Pillar 5 and Pillar 3, not as a note.
- Undefined economies the concept implies: currency and shop (System 20, Not Started) and the meta-unlock rule. The rate must be reported at both the initial and the full pool, and unlocks must add recipes without raising any recipe's power.
- What are the lean ceiling and the perceptibility floor, and do they hold at every pool size? Resolve with System 18's exact pool computation and System 35's lean-off arm (decision D1). The floor is measured on what the player sees in an offer and the ceiling on the run-defining rate, so each needs its own run count. System 35 question 15 still carries the withdrawn 1.5 ceiling and must be reconciled with D1, and System 18 Rule 11 ("only the tier row is the rare-frequency lever") should be reconciled with the finite-pool correction.

---

## MVP Definition

**Core hypothesis**, in two parts:
- **H1 (human playtest):** a committed player reaches a visible chain ("my build is
  going off", see the payoff ladder in Pillar 3) by the boss, can see which arrangement
  caused it, and wants to start another run.
- **H2 (harness, a Vertical Slice gate):** the run-defining rung 3 is visibly rarer than
  rung 1, tracks the player's arrangement rather than luck, and a near miss is readable in
  the post-combat breakdown. Human playtests cannot judge a rare event. The MVP reports
  the harness results but does not gate on them, and H2 is NOT ASSESSED until System 35's
  bot is calibrated against human runs (System 35 states how many, and the numeric bands
  and margins). The MVP therefore validates the loop, not the Core Fantasy; the owner
  accepts that as scope (fourth review), and the playtest plan adds a cheap proxy for the
  fantasy: testers watch a scripted run-defining run and say whether it reads as different
  and whether they would chase it.

**Required for MVP** (scope kept as the user chose; see "first cuts" below):
1. One floor: about 8 rooms plus 1 boss.
2. A roster of four heroes (Crusader, Alchemist, Ranger, Cleric, per `party-and-heroes.md`); the player picks three before the run and the party is then fixed. Each hero has a 4x4 backpack; the player gets drag/drop, rotation (R key) and a shared stash.
3. About 20 items with 3-4 synergy types and cooldown triggers; items defined as data.
4. Speed-gauge combat with enemy intent shown before combat; 6 regular monster types plus 1 boss (System 16's table; the earlier 4-5 was too low).
5. Three door cards across 4 tiers (White, Green, Blue, Purple) with a bounded, commitment-biased drop model. The MVP's only price is the danger of the room (door Rule 11 gives Purple no extra cost), so Pillar 5 is **not tested in the MVP**; its first test is Gold with a curse (Tier 1.5).
6. A basic escalating payoff (number pops, rising intensity, screen shake) driven by an event log, with speed control and skip for combat, and 3-4 placeholder sound cues that rise with combo size (systems index, System 29). Skip fast-forwards to the next Detonation and plays it in full, then continues (fourth-review decision, which withdraws D3; the payoff spec needs the amendment).
7. A post-combat contribution panel and a small personal-best record retained across game launches, by user decision on 2026-11-13. The record's exact metrics and storage contract remain to be designed (System 30 criterion 21 is blocked on them); this does not add run save or replay.
8. A dev-only command-log export and headless replay, used to check that one scripted run gives the same event-log hash in the Editor tests, the console harness and a development player build. A tool for developers, not a player-facing replay feature. The command log carries a content-version stamp, because a replay breaks whenever data or rules change. The "development player build" is identified by a project-defined scripting symbol (a design choice: the engine reference lists `DEVELOPMENT_BUILD` as deprecated in Unity 6.6) that the build script sets, so a manual player build cannot silently lose the export.

**Explicitly NOT in MVP** (defer):
- Shop, events, recruiting, Gold and Red tiers, curses.
- Meta progression, the broader records system and replay, higher difficulty levels, and run save/resume. The small persistent personal best above is the MVP exception.

**First cuts if the schedule slips**, in this order (none is cut yet; the personal best and the speed control were user decisions, so promoting a cut to a firm cut is the user's call). How many days each cut recovers is owned by `production/milestones/mvp.md`. The producer judged that the ten cuts together may not cover another estimate that is about twice too tight, so the milestone file should also name a structural cut:
1. Harness questions 15 and 16 (the lean-off arm and build diversity) are deferred to the Vertical Slice. Question 14 stays, because it carries the Hook decision trigger.
2. English-only text, keeping the string keys (the second language and the diacritic pixel font are deferred).
3. Four regular monster types plus the boss instead of six (needs a change to System 14 and System 16's table).
4. The rebind UI (default bindings only).
5. Item rotation.
6. The fourth door tier (Purple), as the systems index and the door GDD already say (user decision C4); the door bet shrinks to three tiers, the ladder must still read with three, and door Rule 12 removes Purple's weight, template cells and buff row together. Purple is no longer a last resort: it was once the MVP's only test of Pillar 5, and the MVP no longer claims that test.
7. Keep the personal best for the session only (drop persistence).
8. Prove hash parity in the Editor tests and the harness only; defer the development player build. This only defers the IL2CPP check, because the outside playtests need a player build anyway, so it saves little time.
9. Ship Skip only, without playback speed levels (Skip still stops at each Detonation).
10. Cut the roster from four heroes to three fixed heroes.

**Checkpoints:** the combat-core checkpoint (systems index): the engine-free combat core must prove apply-then-detonate across two heroes with identical event logs in tests and in live play; if it does not, re-plan before building UI. Two further checkpoints follow. A greybox payoff test (decision D2 of the third review proposed about ten weeks after day 0; the milestone file sets the date, and day 0 is the start of the combat core spike): the combat core with a placeholder backpack, a few new testers and the visible-chain question, so the payoff is tested before any UI exists. The second is the first playable run of the whole loop, which also needs a date. The kill or pivot signal below is read after each tuning pass, and each pass has a cap on its length that the playtest plan sets.

**Go/no-go criteria: intent only** (fourth review, 2026-11-14). The earlier version of this
block carried counts, thresholds and denominators that no owning document backed, and each
review found faults in them. They now belong to the **MVP playtest plan** in
`production/qa/`, which is not yet written and must be committed before the first outside
session. The plan owns: the sample and recruitment budget (including testers who have
already seen the payoff), the aggregate rule that combines the criteria into one verdict,
the denominator and the NOT ASSESSED rule of each criterion, the coding rules for judged
answers, the boss-peak margin, the classifier configuration under test, the loss-cause
rule, and the threshold of the item-cost measurement. It must say plainly that a small
playtest is a qualitative smoke signal, not statistical evidence, and that a criterion
with too few observations is NOT ASSESSED and never passes. Until it exists, no threshold
is stated here. The MVP must show that:
- **The payoff lands.** Testers, both newcomers to the genre and players of comparable titles, reach a visible chain by the boss (System 29 classifier), and the boss fight's biggest blow stands out from early rooms.
- **The cause is readable.** After the first Surge, testers can name the items that contributed (System 30 ranking), asked with the breakdown panel dismissed so the test measures insight and not reading. A tester who skipped the moment is recorded, not counted as a pass.
- **They want another run.** Testers start a second run without being asked.
- **A loss can be explained.** After a loss, players can say why, judged against the event-log cause by one deterministic rule that the plan defines.
- **The door choice is a decision.** Players do not always pick the same tier and can explain the trade-off (the reward against the room's danger, since the MVP has no other price). The evidence is the door GDD's own metrics (tier share per room, win rate by tier, the buff ablation), because a different pick can come from different cards alone.
- **The hook is felt.** Players of comparable titles can name a real difference from a closed list that the plan fixes.
- **The fantasy has a proxy.** The scripted run-defining run reads as different and worth chasing (see H2).
- **Combat ordering is deterministic.** The ordering suite is non-empty and passes, no severe ordering bug is open (counted in any role, not only by the system field), and scripted runs on different seeds, including one that reaches the reaction depth cap, give the same event-log hash in the Editor tests and the console harness. The same runs are compared against live play and checked on the IL2CPP backend. Taking first cut 8 defers only the player-build part; the IL2CPP check must still be done before the Vertical Slice.
- **Adding content is cheap.** The measured effort to add one data-only item is recorded and judged against a threshold the plan sets; art time per item and monster is recorded, and items that need a new effect kind are reported separately.
- **Preconditions of the playtest:** the plan is committed, and the reduced-flashes default has passed the photosensitivity check (payoff-presentation Rule 17), so the payoff is judged on the real bloom.

The Unique Hook and the lean are **reported, not gated, in the MVP**: System 35 reports the run-defining rate with the lean on and off (the ceiling and floor come from System 18's exact pool computation, decision D1) and the best split build against the best one-hero build at an equal item budget. They become Vertical Slice gates (H2).

**Kill or pivot signal (principle only).** If the payoff does not land after repeated tuning passes, stop adding content and return to trigger design. A tuning pass is a documented change batch re-tested with new testers; the plan sets the number of passes, their size and their length, and defines "does not land" so that a small sample's noise does not fire it alone (a failed pass is advisory, and content work stops only on repeated failure or on a written decision). **The user decides; the creative director advises** (fourth-review default, because the creative director is an agent the user runs and adds no independent check). The plan must also confirm before the MVP starts that enough distinct outside testers can be recruited, with a deadline, since a stalled recruitment would turn a capped pass into an open-ended wait.

### Scope Tiers (if budget/time shrinks)

Estimates: producer review, solo, first-time, full-time, cumulative from day 0, low
confidence. Release work is lighter than the producer assumed (no commercial release).

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 1 floor, about 20 items, 4 heroes (pick 3), 6 monster types plus 1 boss | Core loop, minimal settings, input and localization, placeholder sound cues | See `production/milestones/mvp.md` (owner decision, fourth review); day 0 is the combat core spike; the prototype is refactored, not reused |
| **Tier 1.5: Minimum Shippable** (the systems index's Vertical Slice) | 2 floors, about 30 items, Gold tier (ships only with a curse) | Save and resume, audio from asset packs, basic tutorial | To be estimated after the MVP |
| **Tier 2: Core Game** | About 40 items, door tiers up to Gold (Red waits for Tier 3 with its curses), shop, recruiting, events | Initial meta unlocks | To be re-estimated after the MVP |
| **Tier 3: Full Vision** | 3 floors, about 80 items, up to 6 heroes, the Red tier and curses on top tiers | Records and replay, higher difficulty levels, polish | To be re-estimated after the MVP |

Fallback if time runs out: ship Tier 2 without events and meta progression. This has no date yet, so it is not a real fallback until the milestone file ties it to a limit.

---

## Next Steps

- [x] `/setup-engine`: pin Unity and fill the engine reference (and the `project.yaml` engine block) — done 2026-11-09
- [x] `/art-bible`: lock the master palette, tier colours and the colour conflicts above — done (Draft)
- [x] `/design-review design/gdd/game-concept.md`: NEEDS REVISION three times and MAJOR REVISION NEEDED once on 2026-11-14, revised after each. The next review should wait until the follow-ups below are done; it must be a full review, not a delta
- [x] Follow-ups in other documents (found by the re-review; done 2026-11-14): `door-generation-and-tiers.md` and `systems-index.md` (Pillar 5 note, content counts), `balance-simulation-harness.md` (questions 14 to 16, `X` frozen, `CellBudget`), `post-combat-breakdown.md` (Rule 12 readout, Vertical Slice), `enemy-and-intent.md` (monster count). Still open in those documents: the margin interface in System 29, the ratio interval in Formula 4, and the other items in their Open Questions
- [ ] Combat core spike (`/prototype`): prove deterministic apply-and-detonate across two heroes, with identical event logs in tests, simulation and live play
- [x] `/map-systems` (run 2026-11-10). `/design-system` for each system is in progress (most MVP GDDs exist) and is not gated on the spike
- [ ] `/create-architecture` and `/architecture-decision`: record the engine-free simulation core, the 6-action turn preview and refactor-in-place decisions
- [ ] `/playtest-report` against the MVP go/no-go criteria. `/vertical-slice` targets Tier 1.5 (the systems index's Vertical Slice), not the MVP
- [x] `/estimate` to re-baseline the MVP: done 2026-11-14, written to `production/milestones/mvp.md` (low confidence; the concept and `systems-index.md` cite it). Still open: the owner confirms the proposed definition of done and picks a structural cut, and `/estimate` is re-run at the combat core checkpoint with measured velocity; then `/sprint-plan`
- [ ] Playtest plan committed to `production/qa/` before the first outside session, holding every number removed from this concept (the removed text is kept in the review log and in git once committed)
- [x] Amend `payoff-presentation.md` for the Skip decision and the boss blow: done 2026-11-14 (Rules 11 and 21, Formulas 5, 8, 9; CD-GDD-ALIGN CONCERNS applied; dependent GDDs synced). Still open: an independent `/design-review` of System 29 in a fresh session, the art director's mock-up, and System 30 labelling the blow run-defining after a Victory
- [x] Reconciled 2026-11-14: `balance-simulation-harness.md` question 15 (no 1.5 ceiling), `commitment-and-drop-model.md` Rule 11, and the registry note on `run_defining_damage_share`. Still open: the `drop_offer_size` unit text, and the registry names the harness as the source of `run_defining_damage_share`, which the harness text does not contain
- [ ] Owner rulings still open: the conflict between Pillar 3 and Pillar 1/Expression (a near-fixed item set), and what makes pivoting cost something real in the MVP
- [ ] System 18: exact pool computation for one, two and three lean-dependent key pieces; System 35: the finite-pool metrics (chance all key definitions are offered by room 8, distinct run-defining item sets), the split-vs-packed full-capacity comparison and a random-allocation control; set the lean ceiling and floor from the result
- [ ] Sync stale lines in `design/gdd/systems-index.md` (the Purple cut order and the header date; the estimate line is already synced) and the droppable minimum (the validator's floor leaves no slack over the droppable count)
