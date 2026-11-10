# Systems Index: Grimpack (working title)

> **Status**: Draft (enumeration, dependencies and priorities approved by the user on 2026-11-10; the CD-SYSTEMS gate result is recorded under Priority Tiers)
> **Created**: 2026-11-10
> **Last Updated**: 2026-11-10
> **Source Concept**: design/gdd/game-concept.md
> **Gates**: TD-SYSTEM-BOUNDARY: CONCERNS (resolved by adding systems 36 and 37; constraints below). PR-SCOPE: OPTIMISTIC (adjustments applied; see Scope Notes). CD-SYSTEMS: CONCERNS (six concerns applied; see the note under Priority Tiers).

---

## Overview

Grimpack is a turn-based, fully automatic roguelike in which the player never acts during combat. All skill happens in three places before a fight: how each of three heroes' grid backpacks is laid out, how items are distributed across the heroes, and which door to bet on (Pillar 4, "Arrangement Is the Thinking"). So the mechanical scope is two big halves: a **deterministic simulation** that resolves a fight from a given arrangement and seed, and a **presentation and arrangement layer** that makes the result readable, dramatic and learnable. The simulation is the product's riskiest and most valuable part. Pillar 2 ("The Payoff Is the Product") only works if the simulation produces a clean, ordered event stream that the presentation can escalate from, and Pillar 3 ("Earned Rarity") can only be verified if that same simulation runs headless, many times, with a seeded RNG.

The 37 systems fall into five groups.

- **Engine-free simulation core (systems 1, 2, 3, 4, 5, 8, 9, 10, 11, 12, 13, 14, 15, 16, 36).** Seeded RNG, plain-C# data definitions, the event log, run state, the grid, adjacency, turn order, effect resolution, status effects, enemy intent and combat. These carry the apply-then-detonate promise: two heroes set up (debuffs, buffs) and a third detonates them, so turn order is the time axis of the build. System 36 (Combat State and Step Model) was added after the technical-director's boundary review. It owns all mutable combat state (HP, shield, statuses, gauges, cooldown counters, intents) behind one mutation API that emits one event per change. System 3 owns the reaction queue, the resolve loop and the damage formulas; system 12 shrinks to the encounter lifecycle (set-up, ask 11 who acts, hand the actor to 9 or 14, check win, loss and stalemate).
- **Run structure and luck (16, 17, 18, 19, 20, 21).** Encounter generation, the three-card door choice over six colour tiers, the commitment-biased and capped drop model, curses on Gold and Red, and the deferred shop, event and recruit rooms. System 18 holds the pillar-defining 1-in-20 target; its definition of "committed" is still an open question (see High-Risk Systems).
- **Arrangement and readout UI (25, 26, 27, 28, 29, 30, 31).** The drag-and-drop backpack, the hover popup (the user rejected a docked inspector), the combat HUD, door panels, the payoff presentation (item avatars, six effect archetypes, escalation by combo size), the post-combat breakdown that lets a player say why they lost, and menus. These are bound by the art bible (`design/art/art-bible.md`), which already fixes layout, the popup rules and the effect vocabulary.
- **Persistence, progression and meta (22, 23, 24, 37, 33, 34, 35).** Run save and resume, settings, meta unlocks, records and replay, the tutorial, accessibility and the headless balance harness. System 35 needs a player-policy component (automatic arrangement and door choice); without it the 1-in-20 target cannot be simulated.
- **Foundations (1, 2, 4, 6, 7, 37).** Small but not optional, because retrofitting them is expensive: input and focus, localisation keys (Vietnamese and English), settings.

**Constraints from the technical-director review (apply to every GDD below).**
1. The simulation core lives in one assembly definition with `noEngineReferences: true`, so the compiler enforces the boundary. The existing prototype's simulation classes import `UnityEngine` and use `ScriptableObject` data; they are to be refactored, not reused as they are.
2. Simulation maths is **integer-only**. Gauges, damage, cooldowns and ties are all integers with a fully specified tie-break rule.
3. System 2's definitions are **plain C# types** loadable without the engine; `ScriptableObject`s are authoring wrappers or an export step.
4. The simulation carries **string keys only**, never display text (systems 6 and 7 stay outside it).
5. **System 5 owns one serialisable snapshot of the whole run**; each system owns a slice that it registers with 5. Save (24), replay (23) and the harness (35) all use that snapshot.
6. **System 18's commitment is a pure function of the run snapshot**, so that 17, 20 and 35 can rely on its interface before the formula is decided.
7. Boundaries: system 11 answers only "who acts next" and exposes a pure, side-effect-free forecast function (the same code drives live combat and the turn-order preview); system 9 never touches a gauge directly; adjacency modifiers (10) are computed once per arrangement before combat and are immutable during it; enemy actions are effect data resolved by 3 (no separate enemy damage path in 12); status ticks and detonations run as effects through 3's queue; intents are chosen from their own RNG substream before combat, so the intents shown are binding.
8. **Playback pacing** (the 1x, 2x, 4x and Skip control) is owned by system 29. The HUD (27) reads the event log plus a state snapshot per event, so it never shows state that is ahead of the animation.
9. **Cause tracking is a requirement of the event schema (4)** (creative-director C1, Pillar 2). Every event that changes state records why: the item that fired, its hero, the adjacency modifiers that applied (10), and, for a detonation, the earlier hero's status that was detonated. The payoff (29) and the breakdown (30) use it to point back at grid cells. Validation at the weeks 5-6 checkpoint: for every damage number the log can name the item and hero that dealt it and the status it detonated.

**Scope notes (PR-SCOPE: OPTIMISTIC, adjustments applied).**
- The MVP estimate is restated as **about 6 months** (best case 4, worst case 8 or more; low confidence), not "a few months". There is a **hard checkpoint at weeks 5-6**: the engine-free combat core (systems 36, 3, 12 with 9, 11, 13, 14) must prove apply-then-detonate across two heroes with identical event logs in tests and in live play. If it does not, re-plan before building any UI on top of it.
- **MVP stubs** (reduced to the minimum that still meets the concept's go/no-go criteria): 7 Localization (one string table with keys, one language, no font or diacritic work); 37 Settings (a playback-speed toggle only); 6 Input and Focus (mouse plus the R key); 31 Menus (an end-of-run screen with Restart); 26 Hover Popup (a tooltip only; the glossary is deferred); 16 Encounter Generation (a hand-authored room table for one floor); 13 Status Effects (only the statuses the 3-4 synergy types need); 30 Post-combat Breakdown (a filtered text view of the event log, no dedicated screen); 25 Backpack UI (item rotation is the first cut); 17 Door Generation (the MVP has **4 tiers: White, Green, Blue, Purple**, as the concept requires; dropping Purple is the second cut after item rotation, and only if the schedule slips).
- A **thin command-line harness** (35) belongs in the MVP, because tuning the drop model and the value of N needs it and the engine-free core makes it nearly free; the full harness is a Vertical Slice item.
- **Creative-director additions (CD-SYSTEMS, applied 2026-11-10).** (C2) System 29 gets a small placeholder sound layer in the MVP: 3-4 cues that rise with combo size from an asset pack, because the payoff kill signal is judged in the MVP and Pillar 2 includes sound; full audio (32) stays Vertical Slice. (C3) A higher door tier must have an explicit MVP price (for example a harder encounter, or something given up), written as a rule in the GDDs of 16 and 17; this keeps Pillar 5 alive before curses exist, without a curse subsystem (anti-pillar), and the go/no-go criterion about door-tier trade-offs depends on it. (C5) System 30 or the end-of-run screen in 31 shows the run's biggest hit, its longest chain and the session best, so escalation is measurable and the core fantasy has a carrier before Records (23) exists. (C6) System 22's GDD must limit unlocks to breadth (new items, heroes, door options), never starting power (Pillar 3: no run starts overpowered).
- **Accessibility (34)** is limited to readability and colour-safe tier cues in the Vertical Slice (it is not in the concept's Tier 1.5 otherwise).
- **Content is not a system** and is estimated separately: the MVP needs about 20 items, 3 heroes and 5-6 monsters (one boss); the concept's Tier 1.5 needs 2 floors and about 30 items, which no system row covers.
- System 28 (Door and Room Panels UI) lists 21 (Room Types) as a dependency, but its MVP slice is door choice only and depends on 17 and 26; the room panels for shop, event and recruit are added when 21 is designed.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Seeded RNG | Core | MVP | Not Started | — | — |
| 2 | Game Data Definitions | Core | MVP | Not Started | — | — |
| 3 | Effect Resolution Model | Gameplay | MVP | Not Started | — | 2, 4, 1, 10, 36 |
| 4 | Event Log (inferred) | Core | MVP | Not Started | — | — |
| 5 | Run State and Flow (inferred) | Core | MVP | Not Started | — | 1, 2 |
| 6 | Input and Focus (inferred) | Core | MVP | Not Started | — | — |
| 7 | Localization and Text | Core | MVP | Not Started | — | — |
| 8 | Grid Backpack | Gameplay | MVP | Not Started | — | 2 |
| 9 | Item Trigger and Cooldown | Gameplay | MVP | Not Started | — | 3, 8, 11, 36 |
| 10 | Adjacency and Synergy | Gameplay | MVP | Not Started | — | 8, 2 |
| 11 | Turn Order (speed gauge) | Gameplay | MVP | Not Started | — | 2, 4, 36 |
| 12 | Combat Resolution | Gameplay | MVP | Not Started | — | 3, 9, 11, 14, 15, 36, 4, 1 |
| 13 | Status Effects (inferred) | Gameplay | MVP | Not Started | — | 3, 2, 36 |
| 14 | Enemy and Intent | Gameplay | MVP | Not Started | — | 2, 1, 3, 36 |
| 15 | Party and Heroes | Gameplay | MVP | Not Started | — | 2, 8 |
| 16 | Encounter Generation (inferred) | Gameplay | MVP | Not Started | — | 2, 1, 14 |
| 17 | Door Generation and Tiers | Gameplay | MVP | Not Started | — | 2, 1, 16, 18 |
| 18 | Commitment and Drop Model | Economy | MVP | Not Started | — | 8, 10, 2, 1 |
| 19 | Curses and Trade-offs | Gameplay | Vertical Slice | Not Started | — | 17, 3, 2 |
| 20 | Currency and Shop | Economy | Alpha | Not Started | — | 2, 18, 8, 5 |
| 21 | Room Types (shop, event, recruit) | Gameplay | Alpha | Not Started | — | 5, 17, 20, 15 |
| 22 | Meta Progression | Progression | Alpha | Not Started | — | 5, 2, 24 |
| 23 | Records, Replay and Difficulty | Progression | Full Vision | Not Started | — | 1, 4, 5, 24 |
| 24 | Run Save and Resume | Persistence | Vertical Slice | Not Started | — | 5, 2, 8, 15, 1, 37 |
| 25 | Backpack UI | UI | MVP | Not Started | — | 8, 10, 9, 6 |
| 26 | Hover Popup and Glossary | UI | MVP | Not Started | — | 2, 7, 6, 8 |
| 27 | Combat HUD | UI | MVP | Not Started | — | 4, 36, 11, 14, 15 |
| 28 | Door and Room Panels UI | UI | MVP | Not Started | — | 17, 26 (21 when designed) |
| 29 | Payoff Presentation | UI | MVP | Not Started | — | 4, 3, 12, 8, 25, 37 |
| 30 | Post-combat Breakdown (inferred) | UI | MVP | Not Started | — | 4, 12, 16, 8, 25 |
| 31 | Menus and Screen Flow (inferred) | UI | MVP | Not Started | — | 5, 6, 37 |
| 32 | Audio | Audio | Vertical Slice | Not Started | — | 4, 29, 37 |
| 33 | Tutorial and Onboarding (inferred) | Meta | Vertical Slice | Not Started | — | 25, 26, 27, 28 |
| 34 | Accessibility (inferred) | Meta | Vertical Slice | Not Started | — | 6, 7, 29, 31, 37 |
| 35 | Balance Simulation Harness | Meta | MVP (thin); Vertical Slice (full) | Not Started | — | 1, 2, 3, 5, 8, 10, 12, 15, 16, 17, 18, 36 |
| 36 | Combat State and Step Model (inferred) | Core | MVP | Not Started | — | 2, 1, 4 |
| 37 | Settings (inferred) | Persistence | MVP | Not Started | — | — |

---

## Categories

| Category | Description | Systems |
|----------|-------------|---------|
| **Core** | Foundation everything depends on | 1, 2, 4, 5, 6, 7, 36 |
| **Gameplay** | The systems that make the game fun | 3, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 19, 21 |
| **Progression** | How the player grows over time | 22, 23 |
| **Economy** | Resource creation and consumption | 18, 20 |
| **Persistence** | Save state and continuity | 24, 37 |
| **UI** | Player-facing information displays | 25, 26, 27, 28, 29, 30, 31 |
| **Audio** | Sound and music | 32 |
| **Meta** | Systems outside the core loop | 33, 34, 35 |

(The Narrative category is not used: the concept has no story budget.)

---

## Priority Tiers

| Tier | Definition | Target Milestone | Design Urgency |
|------|------------|------------------|----------------|
| **MVP** | Required for the core loop to function. Without these, you can't test "is this fun?" | First playable prototype (concept MVP) | Design FIRST |
| **Vertical Slice** | Required for one complete, polished area. Demonstrates the full experience. | Vertical slice / demo (concept Tier 1.5, Minimum Shippable) | Design SECOND |
| **Alpha** | All features present in rough form. Complete mechanical scope, placeholder content OK. | Alpha milestone (concept Tier 2, Core Game) | Design THIRD |
| **Full Vision** | Polish, edge cases, nice-to-haves, and content-complete features. | Beta / Release (concept Tier 3) | Design as needed |

*Tier counts: MVP 28 (system 35 thin), Vertical Slice 5 (plus the full 35), Alpha 3, Full Vision 1; total 37.*

*Why these tiers.* The MVP is "can a committed player reach a visible 'my build is going off' moment by the boss, and want another run?". That needs the whole simulation (a deterministic fight with apply-then-detonate across two heroes: Pillar 4), the bet on doors and the commitment-biased capped drops (Pillars 1 and 3), enough UI to read it (Pillar 2: the payoff and the cause must be visible), and the small foundations that are expensive to retrofit (string keys, input, settings). Curses are Vertical Slice because the concept defers Gold and Red; shop, events and recruiting are Alpha (the concept's Tier 2); records, replay and difficulty are Full Vision (Tier 3).

> **Creative Director Note** (MVP): CD-SYSTEMS returned CONCERNS (2026-11-10): the decomposition is sound, every pillar has a carrier, and no system is clear scope creep. Pillar 5 has no MVP carrier (curses are Vertical Slice), so the price of a door tier must be an explicit MVP rule (concern C3). Legible cause (C1) is now a requirement of the event schema; the payoff gets a placeholder sound layer (C2); the run's records are shown on 30 or 31 (C5). The MVP keeps **4 door tiers** (concern C4, user decision: Purple is the first cut *if the schedule slips*, as the concept says, not a starting reduction). The MVP tests the core hypothesis provided C1 and C3 are met.
>
> **Creative Director Note** (Alpha): concern C6: system 22 (Meta Progression) must unlock breadth, not power.

---

## Dependency Map

Systems are listed in dependency order within each layer. Design and build from top to bottom.

### Foundation Layer (no dependencies)

1. **Seeded RNG (1)**: one run seed with named substreams (doors, drops, intents); runs must be replayable.
2. **Game Data Definitions (2)**: items, heroes, monsters, buffs and doors as plain-C# data with stable IDs; ScriptableObjects are authoring wrappers only.
3. **Event Log (4)**: the single source of truth that VFX, audio, the breakdown, tests, the harness and replay all read; defines the event schema.
4. **Input and Focus (6)**: mouse and keyboard primary, gamepad partial.
5. **Localization and Text (7)**: string tables (Vietnamese and English); the simulation carries keys only.
6. **Settings (37)**: playback speed, volume, text size, key bindings; split out of Save so presentation systems can depend on it.

### Core Layer (depends on foundation)

1. **Run State and Flow (5)** — depends on: 1, 2. Owns the serialisable run snapshot; each system registers a slice.
2. **Grid Backpack (8)** — depends on: 2.
3. **Combat State and Step Model (36)** — depends on: 2, 1, 4. All mutable combat state behind one mutation API.
4. **Turn Order (11)** — depends on: 2, 4, 36. Integer gauges, full tie-break, pure forecast function.
5. **Adjacency and Synergy (10)** — depends on: 8, 2. Read-only modifier query, computed once per arrangement.
6. **Effect Resolution Model (3)** — depends on: 2, 4, 1, 10, 36. Reaction queue, depth cap, damage formulas. Designed together with 12.
7. **Party and Heroes (15)** — depends on: 2, 8.

### Feature Layer (depends on core)

1. **Item Trigger and Cooldown (9)** — depends on: 3, 8, 11, 36.
2. **Status Effects (13)** — depends on: 3, 2, 36.
3. **Enemy and Intent (14)** — depends on: 2, 1, 3, 36.
4. **Combat Resolution (12)** — depends on: 3, 9, 11, 14, 15, 36, 4, 1. Encounter lifecycle.
5. **Encounter Generation (16)** — depends on: 2, 1, 14.
6. **Commitment and Drop Model (18)** — depends on: 8, 10, 2, 1.
7. **Door Generation and Tiers (17)** — depends on: 2, 1, 16, 18.
8. **Curses and Trade-offs (19)** — depends on: 17, 3, 2.
9. **Currency and Shop (20)** — depends on: 2, 18, 8, 5.
10. **Room Types (21)** — depends on: 5, 17, 20, 15.

### Presentation Layer (depends on features)

1. **Backpack UI (25)** — depends on: 8, 10, 9, 6.
2. **Hover Popup and Glossary (26)** — depends on: 2, 7, 6, 8.
3. **Combat HUD (27)** — depends on: 4, 36, 11, 14, 15.
4. **Payoff Presentation (29)** — depends on: 4, 3, 12, 8, 25, 37. Also owns playback pacing and the MVP placeholder sound layer.
5. **Door and Room Panels UI (28)** — depends on: 17, 26 (and 21 when designed).
6. **Post-combat Breakdown (30)** — depends on: 4, 12, 16, 8, 25. Also shows the run's records (biggest hit, longest chain).
7. **Menus and Screen Flow (31)** — depends on: 5, 6, 37. The Continue entry binds to 24 when 24 is designed.
8. **Audio (32)** — depends on: 4, 29, 37.

### Polish Layer (depends on everything)

1. **Run Save and Resume (24)** — depends on: 5, 2, 8, 15, 1, 37.
2. **Meta Progression (22)** — depends on: 5, 2, 24.
3. **Records, Replay and Difficulty (23)** — depends on: 1, 4, 5, 24.
4. **Tutorial and Onboarding (33)** — depends on: 25, 26, 27, 28.
5. **Accessibility (34)** — depends on: 6, 7, 29, 31, 37.
6. **Balance Simulation Harness (35)** — depends on: 1, 2, 3, 5, 8, 10, 12, 15, 16, 17, 18, 36, plus a player-policy component (automatic arrangement and door choice). Its thin form is an MVP item.

---

## Recommended Design Order

Combining the dependency sort with the priority tiers. Systems 3 and 12 are designed back to back (the same session or consecutive ones) because their contract is tightly coupled.

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | 2 Game Data Definitions | MVP | Foundation | lead-programmer, game-designer | M |
| 2 | 4 Event Log | MVP | Foundation | lead-programmer | M |
| 3 | 1 Seeded RNG | MVP | Foundation | lead-programmer | S |
| 4 | 36 Combat State and Step Model | MVP | Core | lead-programmer, systems-designer | M |
| 5 | 8 Grid Backpack | MVP | Core | game-designer, systems-designer | M |
| 6 | 10 Adjacency and Synergy | MVP | Core | systems-designer | M |
| 7 | 11 Turn Order | MVP | Core | systems-designer | M |
| 8 | 3 Effect Resolution Model | MVP | Core | systems-designer, lead-programmer | L |
| 9 | 15 Party and Heroes | MVP | Core | game-designer | M |
| 10 | 9 Item Trigger and Cooldown | MVP | Feature | systems-designer | M |
| 11 | 13 Status Effects | MVP | Feature | systems-designer | M |
| 12 | 14 Enemy and Intent | MVP | Feature | game-designer, ai-programmer | M |
| 13 | 12 Combat Resolution | MVP | Feature | systems-designer, gameplay-programmer | L |
| 14 | 5 Run State and Flow | MVP | Core | lead-programmer, game-designer | M |
| 15 | 16 Encounter Generation | MVP | Feature | level-designer | S |
| 16 | 18 Commitment and Drop Model | MVP | Feature | economy-designer, systems-designer | L |
| 17 | 17 Door Generation and Tiers | MVP | Feature | game-designer, economy-designer | M |
| 18 | 35 Balance Simulation Harness (thin) | MVP | Polish | tools-programmer, economy-designer | M |
| 19 | 6 Input and Focus | MVP | Foundation | ux-designer, ui-programmer | S |
| 20 | 7 Localization and Text | MVP | Foundation | localization-lead | S |
| 21 | 37 Settings | MVP | Foundation | ux-designer | S |
| 22 | 25 Backpack UI | MVP | Presentation | ux-designer, ui-programmer | L |
| 23 | 26 Hover Popup and Glossary | MVP | Presentation | ux-designer | S |
| 24 | 27 Combat HUD | MVP | Presentation | ux-designer, ui-programmer | M |
| 25 | 29 Payoff Presentation | MVP | Presentation | technical-artist, game-designer | L |
| 26 | 28 Door and Room Panels UI | MVP | Presentation | ux-designer | S |
| 27 | 30 Post-combat Breakdown | MVP | Presentation | ux-designer | S |
| 28 | 31 Menus and Screen Flow | MVP | Presentation | ux-designer | S |
| 29 | 24 Run Save and Resume | Vertical Slice | Polish | lead-programmer | M |
| 30 | 32 Audio | Vertical Slice | Presentation | audio-director, sound-designer | M |
| 31 | 33 Tutorial and Onboarding | Vertical Slice | Polish | ux-designer | M |
| 32 | 34 Accessibility | Vertical Slice | Polish | accessibility-specialist | M |
| 33 | 19 Curses and Trade-offs | Vertical Slice | Feature | game-designer | M |
| 34 | 35 Balance Simulation Harness (full) | Vertical Slice | Polish | tools-programmer | M |
| 35 | 20 Currency and Shop | Alpha | Feature | economy-designer | M |
| 36 | 21 Room Types | Alpha | Feature | level-designer, game-designer | L |
| 37 | 22 Meta Progression | Alpha | Polish | game-designer, economy-designer | M |
| 38 | 23 Records, Replay and Difficulty | Full Vision | Polish | game-designer | M |

(Effort: S = 1 session, M = 2-3 sessions, L = 4+ sessions. Row 18 and row 34 are the same system, system 35, in its thin and full forms. Orders 19-21 and 14 are cheap and can be designed in parallel with their neighbours.)

---

## Circular Dependencies

- **None found** in the dependency graph. Four tightly coupled pairs need written contracts so that they do not become cycles:
  - **3 and 12 (and 36):** 36 owns state and its mutation API; 3 resolves effects against that state and owns the formulas and the reaction queue; 12 sequences the encounter. The technical-director found that, without 36, 3 would need to read status stacks (13) that depend on 3, and 12 would apply damage that feeds back into 3's queue.
  - **3 and 10:** 10 exposes a read-only modifier query that 3 consumes; 10 never calls 3.
  - **17 and 18:** 18 supplies weighted selection to 17; 18 does not generate doors.
  - **25, 26 and 28:** share one hover popup (art bible 7.9); the popup component belongs to 26.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| 3, 36, 12 | Technical | Effect resolution, combat state and combat flow are new architecture on the critical path: a reaction queue with a depth cap, apply-then-detonate across two heroes, integer gauges and a full tie-break. Likely 30-35% of the MVP. A wrong boundary here forces rework in 9, 13 and 14. | Design 3 and 12 together; combat-core spike (`/prototype`); the weeks 5-6 checkpoint (identical event logs in tests, simulation and live play); a depth cap and a turn cap against stalemates. |
| 2 | Technical | The prototype's data are `ScriptableObject`s and its simulation imports `UnityEngine`; the headless harness cannot run until definitions are plain C#. | Plain-C# definitions in a `noEngineReferences` assembly; ScriptableObjects only as authoring wrappers or an export step. |
| 18 | Design | The definition of "committed" is undefined; biased drops may lock a run onto the first items found (reduces Expression); the 1-in-20 target is unproven. | Commitment as a pure function of the run snapshot; a hard cap on bias; simulate with the thin harness during the MVP; several tuning passes planned. |
| 29 | Design | The go/no-go depends on the payoff landing; the kill signal is two failed tuning passes. Spectacle must not hide the cause (Pillar 2). | Drive it from the event log; a protected layer for HP, intent and the grid (art bible 4 and 5); cap simultaneous popups; budget tuning loops, not just code. |
| 25 | Scope | Three 4x4 grids plus a stash, drag and drop, invalid-placement feedback, adjacency lines and firing-order numbers: UI of this kind always takes longer than planned. | Rotation is the first cut; follow the art bible's UI rules; build it after the weeks 5-6 checkpoint. |
| 4, 29 | Design | If the event schema does not record cause, the apply-then-detonate chain looks like one big number with no visible author (Pillar 2; the go/no-go criterion that players can say why they lost). The payoff is also judged without sound unless 29 carries placeholder cues. | Cause tracking is a schema requirement (constraint 9); 29 depends on 8 and 25 so it can point at grid cells; 3-4 placeholder sound cues in the MVP. |
| 35 | Technical | Cannot simulate the 1-in-20 target without a player policy (automatic arrangement and door choice). | Name the policy component in 35's GDD; run a thin command-line harness in the MVP. |
| 17, 19 | Design | Gold and Red tiers and curses are deferred but change the door model; adding a fifth and sixth tier later must not break 17. With no curses in the MVP, a higher tier must still cost something or door choice is solved by always picking the highest (Pillar 5 and a go/no-go criterion). | Make 17's tier model data-driven for six tiers from the start; implement four (White, Green, Blue, Purple) in the MVP; give each tier an explicit MVP price (harder encounter or something given up) in the GDDs of 16 and 17. |
| 7 | Technical | Hard-coded Vietnamese strings with emoji sit inside logic in the prototype; pixel fonts need full diacritic coverage (not yet tested). | Keys-only simulation; the Vietnamese render test from the art bible (7.7) before committing to a font. |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 37 |
| Design docs started | 0 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 0/28 |
| Vertical Slice systems designed | 0/5 |

---

## Next Steps

- [x] Review and approve this systems enumeration (user, 2026-11-10)
- [x] CD-SYSTEMS concerns applied (user chose to revise the index, 2026-11-10)
- [ ] Design MVP-tier systems first (use `/design-system [system-name]`), starting with 2 Game Data Definitions, then 4 Event Log and 1 Seeded RNG
- [ ] Combat-core spike (`/prototype`): prove apply-then-detonate across two heroes with identical event logs; this is the weeks 5-6 checkpoint
- [ ] Run `/design-review` on each completed GDD
- [ ] Run `/gate-check technical-setup` when MVP systems are designed
- [ ] Validate the highest-risk systems with `/vertical-slice` before committing to Production
- [ ] Create `design/accessibility-requirements.md` (the art bible's Section 7 is not final without it)
