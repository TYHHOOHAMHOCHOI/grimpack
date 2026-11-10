# Game Concept: Grimpack (working title)

*Created: 2026-11-09*
*Status: Draft*

> **Provenance note.** Built in a `/brainstorm` session from the user's implementation
> plan (`game_implementation_plan.md`, a Unity 6 plan) and their answers. Reviewed by
> three director gates: CD-PILLARS (CONCERNS, addressed in the pillars below),
> AD-CONCEPT-VISUAL (STRONG), TD-FEASIBILITY (CONCERNS), PR-SCOPE (OPTIMISTIC).
> Items marked *(assumption)* were not stated by the user. Items marked
> **NOT SOURCEABLE** have no pinned reference in this repo yet.

---

## Elevator Pitch

> It's a turn-based pixel-art roguelike where you lead three heroes through a cursed
> dungeon, arranging each hero's grid backpack so their items chain into combos on
> their own, and every door you pick is a bet on a bigger payoff.

---

## Core Identity

| Aspect | Detail |
| ---- | ---- |
| **Genre** | Roguelike, turn-based auto-battler, backpack-battler; dark fantasy |
| **Platform** | PC (Windows). The user chose "PC (Steam / Epic)"; they do not plan a commercial release, so a store release is optional. |
| **Target Audience** | Achievers who chase record runs; secondary Creators and Explorers (see Player Profile) |
| **Player Count** | Single-player |
| **Session Length** | One run is about 30-60 minutes *(assumption; to be measured in the MVP)* |
| **Monetization** | None. The user does not plan a commercial release. No real-money gacha (anti-pillar). |
| **Estimated Scope** | Large (16-20 months, solo, full-time; producer estimate, low confidence; re-estimate after the MVP) |
| **Comparable Titles** | Backpack Battles, Backpack Hero, Slay the Spire |

---

## Core Fantasy

You are the architect of a perfect machine. After many runs, one run finally lines up:
the right items, the right door buffs, the right hero order. Then it fires, and
stacked damage detonates into a record-breaking number. The player is chasing that run.
The promise is that the run is rare, that the player's own arrangement made it
possible, and that watching it go off is a thrill.

---

## Unique Hook

Like Backpack Battles, **and also** you run a three-hero party where **turn order is
the combo**: two heroes set the trap (apply debuffs, buffs), the third springs it
(detonates them). Every door is a bet, and the perfect run is a once-in-twenty-runs
fireworks show.

Why it is not just "three smaller backpacks": a single large backpack cannot sequence
effects over time. An item could detonate debuffs before they were applied, and a
heavy-hit build would simply fire every big item individually. With three heroes, the
turn order creates a setup phase and a payoff phase, so *sequence is part of the build*.

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
| **Discovery** (exploration, secrets) | 4 | Finding item synergies and hero-order combos. |
| **Expression** (self-expression, creativity) | 1 | Build variety: how to lay out three grids, which hero carries what, which direction to commit to. |
| **Submission** (relaxation, comfort zone) | N/A | Not a target. |

### Key Dynamics (Emergent player behaviors)
- Players commit to a build direction early, then read doors and drops for what serves it.
- Players experiment with hero order and item placement to discover setup-and-detonate combos.
- Players weigh high-tier doors as bets: bigger buff and harder room, with a price at the top tiers.
- Players retry runs hoping to reproduce a "perfect" one, and study why a run did or did not go off.

### Core Mechanics (Systems we build)
1. **Grid backpacks with auto-trigger and adjacency synergies.** Each hero has a grid (4x4 start, expandable); shaped items (1x1, 1x2, 2x2, L) can be rotated; a shared stash holds spare items. On a hero's turn, items whose turn-cooldown has elapsed fire automatically; adjacent items modify each other.
2. **Speed-gauge turn order with three heroes.** Heroes and monsters fill speed gauges and act in order. Turn order is the time axis of the build.
3. **Enemy intent.** Monsters show their next action. Intent and turn order must be visible *before* combat, while the player can still rearrange.
4. **Door progression.** After each room the player picks 1 of 3 door cards across 6 colour tiers (White, Green, Blue, Purple, Gold, Red). Higher tier: bigger buff, harder room. Gold and Red carry a curse or trade-off.
5. **Commitment-biased, capped drop model.** Once the player has committed to a build direction, drops and door buffs lean toward it, with a hard cap on how much.

---

## Player Motivation Profile

### Primary Psychological Needs Served

| Need | How This Game Satisfies It | Strength |
| ---- | ---- | ---- |
| **Autonomy** (freedom, meaningful choice) | Choosing the build direction, the arrangement, who carries what, and which door to risk. | Core |
| **Competence** (mastery, skill growth) | Reading synergies, ordering heroes, and understanding why a run did or did not go off. Skill raises the odds of the perfect run. | Core (depends on readable cause and effect) |
| **Relatedness** (connection, belonging) | Each hero has a mechanical identity (position and role in the sequence). The player attaches to "my back-row detonator". No story budget required. | Minimal |

### Player Type Appeal (Bartle Taxonomy)

- [x] **Achievers**: chase record runs, optimise builds, clear harder difficulties.
- [x] **Explorers**: discover item synergies and hero-order combos.
- [ ] **Socializers**: not served.
- [ ] **Killers/Competitors**: not served (no PvP). Records are a personal benchmark.

Secondary appeal: Creators (Quantic Foundry), who enjoy assembling a build their own way.

### Flow State Design

- **Onboarding curve**: *(to be designed)* First minutes should teach one idea at a time: place an item, see it fire, then see an adjacency bonus, then hero order. Detail belongs in `/ux-design`.
- **Difficulty scaling**: Door tiers set the room's danger; the player chooses how much risk to take, so the player steers the curve.
- **Feedback clarity**: Firing-order numbers on the grid, an escalating payoff that shows which arrangement caused it, and a post-combat breakdown.
- **Recovery from failure**: A fast restart. Losing should be educational: the player can see why via intent and the event log.

---

## Core Loop

### Moment-to-Moment (30 seconds)
Watching one hero's turn resolve: items fire in a visible order, numbers pop, and
setups from earlier heroes pay off. Because combat is fully automatic, this is the
*watch* beat, and it must be satisfying even without a perfect run (small triggers
quiet, big chains unmistakable).

### Short-Term (5-15 minutes)
Pick one of three doors, enter the room, win, take the loot and buff, then rearrange
the backpacks. The recurring question is "which direction am I building, and does this
door serve it?". Risk and reward on each door tier drives "one more door".

### Session-Level (30-120 minutes)
One run across the floors up to the boss, with a natural stopping point after each
floor and after the boss. The hook between sessions is the memory of a run that
almost went off.

### Long-Term Progression
In a run: items, buffs, extra backpack slots. Across runs: unlocked item pools, heroes
and door types. Long-term goals: harder difficulty levels and record runs. *(Records
and replay, and higher difficulty levels, are Tier 3.)*

### Retention Hooks
- **Curiosity**: Unseen items, synergies and hero-order combos; locked content.
- **Investment**: Meta unlocks the player does not want to lose.
- **Social**: None.
- **Mastery**: Reproducing the perfect run more often; harder difficulty; record numbers.

---

## Game Pillars

### Pillar 1: Luck Follows Commitment
Once the player has visibly committed to a build direction, drops and door buffs lean
toward it. The player can still pivot, but pivoting costs something. This pillar owns
the *direction* luck leans, not how much power the player gets.

*Design test*: Debating random drops vs build-weighted drops, we choose build-weighted.
Debating hard lock-in vs pivoting at a cost, we choose pivoting at a cost. (The
definition of "committed" is an open question; see Risks.)

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
frequency* of power, and the drop cap lives here. Draft target: a committed, competent
player reaches a run-defining combo in roughly 1 run in 20 (tunable by playtest and
simulation). No run starts overpowered.

*Design test*: Debating a generous vs a stingy Gold drop rate, we choose the rate that
keeps a committed player near the 1-in-N target. If a change makes a run-defining combo
independent of player skill, we reject it.

### Pillar 4: Arrangement Is the Thinking
All player skill happens before combat: grid layout, distribution of items across three
heroes, and door choice. The three heroes are three stages of one machine; turn order
lets one hero set up (buff, debuff) and another cash in, so sequence is part of the
build. Everything the player needs to decide (item effects, turn order, enemy intent)
is visible before the fight starts.

*Design test*: Debating an extra in-combat action vs a deeper pre-combat arrangement, we
choose pre-combat. Debating an effect shared by all heroes vs one whose value depends on
hero order or position, we choose the order- or position-dependent one.

### Pillar 5: Power Has a Price
Great power is always tainted in this world. High-tier doors and items cost the player
something real, and art, audio and text make that cost felt. A drawback that changes how
the player builds counts; one the player can ignore does not.

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
  1. *Light is information.* Squint test: the brightest and most saturated 10% of pixels must be items, tier markers, active synergies or current-turn indicators.
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
| **Time availability** | Runs of 30-60 minutes *(assumption)* |
| **Platform preference** | PC |
| **Current games they play** | Backpack Battles, Backpack Hero, Slay the Spire, Dead Cells *(from comparable titles)* |
| **What they're looking for** | The thrill of a perfect, rare, self-built run; a build they assembled by thinking |
| **What would turn them away** | Needing reflexes or manual input in combat; story-heavy play; a game where every run is overpowered or where outcomes feel random |

---

## Technical Considerations

| Consideration | Assessment |
| ---- | ---- |
| **Recommended Engine** | Unity (the user's stated preference), pinned in `project.yaml` as `6000.6.3f1` (Unity 6.6), 2D URP; the installed editor matches. `docs/engine-reference/unity/` was updated to 6.6 on 2026-11-09 and lists what is **NOT SOURCEABLE** (for example how existing projects treat domain-reload settings after upgrading to 6.6). Unity 6.6 is not an LTS; re-pin when 6.7 LTS ships. |
| **Key Technical Challenges** | See Technical Risks. Chief among them: deterministic effect-resolution order within a backpack and across three heroes; simulation separated from presentation; headless balance simulation; drag/drop grid UI. |
| **Art Style** | Simple 2D pixel (see Visual Identity Anchor) |
| **Art Pipeline Complexity** | Medium: AI-assisted pixel art plus hand clean-up; needs a locked master palette, outline rule and light direction. Items and monsters are the labour-heavy part. |
| **Audio Needs** | Moderate: asset-pack music and SFX; payoff audio matters for Pillar 2. |
| **Networking** | None |
| **Content Volume** | Full vision: about 60-80 items, up to 6 heroes, about 15 monster types plus 3 bosses, 6 door tiers, shop, events, recruit rooms, 3 floors. MVP: about 20 items, 3 heroes, 4-5 monsters plus 1 boss, 1 floor. |
| **Procedural Systems** | Door generation, commitment-biased capped drop model, monster selection. Seeded, reproducible RNG with named substreams (doors, drops, monster intents). |

---

## Risks and Open Questions

### Design Risks
- The payoff may not land: if the escalation is not exciting by the boss, more content will not fix it.
- Automatic combat may feel random if the player cannot say why they lost (intent and trigger feedback must be readable).
- Biasing drops toward the committed build can lock runs into the first items found, which shrinks Expression. Pivoting must be possible at a cost.
- Players will watch automatic combat hundreds of times; spectacle that does not scale with combo size will tire them (needs speed control).
- Combinatorial balance (items x heroes x placement x order) cannot be hand-tuned.
- Heroes may collapse into "one big backpack split in three" unless position and order give items different value.

### Technical Risks
- **Effect resolution order** (highest): the apply-then-detonate promise needs a defined resolution model (a one-page spec, composable effect data, a reaction queue with a depth cap). The current prototype applies effects immediately in a fixed order and has no "detonate" concept.
- **Simulation coupled to presentation**: the speed gauge uses floats advanced by frame time, so ties can depend on frame rate. Needs integer gauges, a full tie-break rule and one simulation as the source of truth.
- **Reproducibility**: RNG falls back to `Environment.TickCount`, so runs cannot be replayed. Needs one seeded run RNG.
- **Headless simulation** to verify the 1-in-N target: recommended an engine-free combat and drop core. Do not kill the open Unity Editor for batchmode runs.
- **Drag/drop grid UI** on three grids plus a stash (rotation, invalid-placement feedback, adjacency lines, firing-order numbers, tooltips).
- **Payoff VFX** must not hide information (drive from an event log, cap simultaneous popups).
- Stalemates, unbounded reaction chains, and integer overflow from escalating numbers.
- Fixed-field effect stats will not scale to "consume stacks"-style effects.
- Hard-coded Vietnamese strings with emoji inside logic: move to a string table; pixel fonts need diacritic coverage.
- Stable IDs on data objects are needed for future saves.

### Market Risks
- The backpack-battler space is crowded (Backpack Battles, Backpack Hero). Differentiation rests on turn-order combos and door bets. Lower weight while there is no commercial release.

### Scope Risks
- Estimates were about 2x too tight for a first-time solo developer (producer review). The producer estimated before knowing an existing prototype is in `Assets/_Project/`, so the real MVP duration may be shorter; re-baseline after the MVP.
- No tier as originally drafted included saving, audio, settings or onboarding; Tier 1.5 now holds them.
- The prototype has already drifted into out-of-MVP scope (a shop, and party-recruit parameters in the door generator). Freeze them.
- Tier ownership of curses (Gold and Red tiers) and the number of recruitable heroes in Tier 2 must be decided.

### Open Questions
- What counts as "committed" to a build direction (item tags in the backpacks, an explicit choice, something else)? Resolve in the drop-model design and test in the combat spike.
- What is the right value of N for the perfect-run target? Resolve with the simulation harness and playtests.
- How far ahead should the turn-order preview promise (draft: next 6 actions assuming no speed change)?
- Does the effect-resolution model support apply-then-detonate across two heroes, with identical event logs in tests, simulation and live play? Resolve with the combat core spike.
- Can the colour conflicts between the tier palette and HP, currency and curse visuals be resolved? Resolve in `/art-bible`.

---

## MVP Definition

**Core hypothesis**: A committed player reaches a visible "my build is going off" moment
by the boss, and wants to start another run.

**Required for MVP** (scope kept as the user chose; see "first cuts" below):
1. One floor: about 8 rooms plus 1 boss.
2. Three fixed heroes (Tanker, Warrior, Support), each with a 4x4 backpack, drag/drop, rotation (R key) and a shared stash.
3. About 20 items with 3-4 synergy types and cooldown triggers; items defined as data.
4. Speed-gauge combat with enemy intent shown before combat; 4-5 monster types plus 1 boss.
5. Three door cards across 4 tiers (White, Green, Blue, Purple) with a bounded, commitment-biased drop model.
6. A basic escalating payoff (number pops, rising intensity, screen shake) driven by an event log.

**Explicitly NOT in MVP** (defer):
- Shop, events, recruiting, Gold and Red tiers, curses.
- Meta progression, records and replay, higher difficulty levels, saving.

**First cuts if the schedule slips**: item rotation and the fourth door tier (Purple).

**Go/no-go criteria, set before the MVP starts** (from the producer review):
- In 5 or more outside playtests, most players reach a visible "build is going off" moment by the boss and numbers clearly escalate from room 1 to the boss.
- Most testers start a second run without being asked.
- After a loss, players can say *why* they lost.
- Players do not always pick the same door tier and can explain the trade-off.
- Testers who know Backpack Battles or Backpack Hero can name what is different.
- Combat ordering is deterministic and covered by automated tests, with zero open ordering bugs.
- The measured time to add one item is under about half a day; art time per item and monster is recorded.
- **Kill or pivot signal**: if the payoff does not land after two tuning passes, stop adding content and return to trigger design.

### Scope Tiers (if budget/time shrinks)

Estimates: producer review, solo, first-time, full-time, cumulative from day 0, low
confidence. Release work is lighter than the producer assumed (no commercial release).

| Tier | Content | Features | Timeline |
| ---- | ---- | ---- | ---- |
| **MVP** | 1 floor, about 20 items, 3 heroes, 4-5 monsters plus 1 boss | Core loop only | 4-5 months (before accounting for the existing prototype) |
| **Tier 1.5: Minimum Shippable** | 2 floors, about 30 items, Gold tier (curses optional) | Save and resume, audio from asset packs, settings, basic tutorial | To be estimated after the MVP |
| **Tier 2: Core Game** | About 40 items, all 6 door tiers, shop, recruiting, events | Initial meta unlocks | 10-12 months |
| **Tier 3: Full Vision** | 3 floors, about 80 items, up to 6 heroes, curses on top tiers | Records and replay, higher difficulty levels, polish | 16-20 months |

Fallback if time runs out: ship Tier 2 without events and meta progression.

---

## Next Steps

- [x] `/setup-engine`: pin Unity and fill the engine reference (and the `project.yaml` engine block) — done 2026-11-09
- [ ] `/art-bible`: lock the master palette, tier colours and the colour conflicts above
- [ ] `/design-review design/gdd/game-concept.md`: check the concept is complete
- [ ] Combat core spike (`/prototype`): prove deterministic apply-and-detonate across two heroes, with identical event logs in tests, simulation and live play
- [ ] If the prototype PROCEEDS: `/map-systems`, then `/design-system` for each system
- [ ] `/create-architecture` and `/architecture-decision`: record the engine-free simulation core, the 6-action turn preview and refactor-in-place decisions
- [ ] `/vertical-slice` and `/playtest-report` against the MVP go/no-go criteria
