# Game Data Definitions

> **Status**: Designed
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-10
> **Author**: Codex studio design session, with systems design, engineering and Creative Director consultation
> **Last Updated**: 2026-11-10
> **Last Verified**: 2026-11-10 (design checked against project files)
> **Implements Pillar**: Arrangement Is the Thinking. Supports The Payoff Is the Product and Power Has a Price.

## Summary

Grimpack uses one catalog for what items, heroes, monsters, buffs and doors are meant to do. A player can inspect a build before combat and recognize those same parts when the combo fires. Stable IDs let the game and its headless simulator refer to the same definitions across asset renames and translations.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None`

## Overview

System 2 defines the identity, structure, validation and lookup of static gameplay data. Unity ScriptableObjects are authoring wrappers. Both Unity play and the headless harness receive equivalent immutable plain C# records, validated before a run starts. Authored values remain visible to the systems that interpret them. Active combat values, generated choices and presentation state live in their respective systems.

The MVP catalog supports the existing scope in the game concept and systems index. The prototype's authoring assets are migration inputs that must be checked against those sources. For example, the concept calls for a 4×4 backpack per hero and the index retains White, Green, Blue and Purple doors in MVP. The catalog can describe additional tiers later. Systems 16 and 17 specify the explicit price of taking a higher tier.

## Player Fantasy

The player is the architect of a machine. Before a fight they can read what an item does, place it on a setup hero, and recognize its contribution when another hero cashes in the setup. The tooltip, combat and breakdown can refer to the same definition and authored parameters. This supports Expression and Pillar 4 by making arrangement intelligible, and Pillar 2 by letting the player connect payoff to cause. Combat conditions, random outcomes and which effects actually fire remain the responsibility of the consuming systems.

## Detailed Design

### Core Rules

1. **Stable identity.** Each definition has an explicit ID and a kind. References carry an expected kind. The draft convention uses lowercase ASCII tokens starting with a letter and allowing digits, underscores and hyphens, compared ordinally. This accommodates existing IDs such as `item_iron_sword`. Moving or renaming an asset, changing a translated label or reordering a list preserves its ID. Retired IDs are reserved against reuse. An explicit mapping is needed to migrate a legacy ID whose spelling changes.
2. **Authored records.** Each record has an ID and may expose localization keys and authored parameters. Where text is required, validation rejects a blank key. Labels and visual assets are resolved by the presentation boundary. Downstream GDDs specify the allowed tags, effect kinds, targeting, ordering, units and tuning for their own payloads.

   | Kind | Authored data supplied to consumers |
   |---|---|
   | Item | Footprint as local grid cells, category and tags, and declared triggers, cooldown parameters, effects and synergy data. |
   | Hero | Base HP, speed and initial shield, backpack profile and starting item references. |
   | Monster | Base HP and speed, encounter classification and declared intent or move data. |
   | Buff | Scope, effect declarations and authored magnitudes. Active duration and stack counters belong to the owning runtime system. |
   | Door | Tier and room or encounter eligibility descriptors plus authored reward and cost descriptors. Systems 16–18 own their meaning and selection. |

3. **One validated snapshot.** Each playable content set has one authoritative authoring input. Unity play and the harness read that content set or its equivalent export. The authoring boundary converts fields and nested collections into plain C# values and takes a deep snapshot. Reject missing required fields, malformed or duplicate IDs within a kind, unresolved or wrong-kind references, invalid shapes and out-of-range values. Publish only after all checks pass. Validation errors name the record, field and reason so authors can fix the source.
4. **Deterministic reads.** Lookup by kind and ID returns the same values in live and headless runs given equivalent input. Enumerate unordered records by ordinal ID within kind. Preserve explicit order where the consuming rules use it, such as an authored list of effects. Loading and querying definitions consume no RNG.
5. **Runtime boundary.** Bind a run to its validated catalog when it starts. Mutable HP, gauges, positions, cooldowns, statuses, rolled doors and rewards belong to runtime instances. An authoring edit requires a new validated snapshot for a later run. The simulation assembly uses no `UnityEngine` types and no Unity object identity.

### States and Transitions

| Current state | Trigger | Result |
|---|---|---|
| Authored input | Convert and validate | Publish a read-only catalog on success. On failure, report errors and refuse to start a run with that input. |
| Validated catalog | Start a run | Bind the snapshot to the run. Runtime instances read definitions and own their mutable state. |
| Catalog bound to a run | Edit or reload authoring data | The current run retains its snapshot. A later run can bind a newly validated catalog. |

### Interactions with Other Systems

- A combat or encounter consumer resolves a typed definition reference, then applies its own rules to the authored parameters. It writes changes to its own state.
- A tooltip consumer uses the same definition and parameters as gameplay, resolves text through Localization and Text and adds context through its own rules. Event Log owns how events represent their causes. Stable definition IDs are available to it as inputs.
- Door generation creates a separate candidate from the door definition, current run context and its RNG stream. The selected offer's rolled content and price belong to the generated choice.
- During prototype migration, inventory existing IDs, map legacy references and verify assets against MVP scope. Compare the editor-generated assets with the prototype's runtime catalog before choosing the authoring input, as the two may contain different values. Review generated `DoorCard` fields separately. A prototype value becomes a design value only when its owning system adopts it.

## Formulas

System 2 defines validation predicates. Damage, odds, gauge calculations and difficulty scales are specified in their owning GDDs. Gameplay numeric fields use integers. Units, scaling and rounding for any fractional rule are set by its owner.

| Symbol | Meaning | Validity |
|---|---|---|
| `h`, `s`, `q` | Base HP, base speed and initial shield where present | `h > 0`, `s > 0`, `q ≥ 0` |
| `w`, `h_grid` | Width and height of an authored grid or footprint bound | Both strictly positive. |
| `x`, `y` | Local coordinates of an occupied footprint cell | `0 ≤ x < w` and `0 ≤ y < h_grid`. |
| `C` | Authored sequence of occupied cells | `count(C) > 0` and `count(C) = count(distinct(C))`. |

For example, a footprint in a 2×1 bound may include `(0,0)` and `(1,0)`. Repeating `(1,0)` or using `(2,0)` makes that definition invalid. Grid placement, fit and rotation belong to System 8.

## Edge Cases

| Case | Resolution |
|---|---|
| Duplicate ID, malformed ID or wrong-kind reference | Reject the catalog and identify each offending definition or reference. A matching slug in another kind cannot satisfy the reference. |
| Removed definition still referenced | Reject the candidate catalog. Resolve the reference in authoring or provide an explicit migration where the persistence rules support it. |
| Missing required text key or translation | An empty required key fails definition validation. The localization system owns missing translations and fallbacks for valid keys. |
| Equivalent input assets arrive in different orders | Unordered lookup and enumeration have identical results. Explicit sequences retain their declared order. |
| Author changes a source list after loading | The bound catalog retains its original nested values. |
| Legacy prototype values conflict with MVP rules | Flag the differences for the owning GDD. Do not use prototype defaults as fallback design values. |

## Dependencies

No upstream gameplay GDD is required. The interfaces below are inputs for the named downstream systems. Their payload details remain provisional until those GDDs are written.

| Consumer | Contract |
|---|---|
| 3, 11, 13–15, 36 | Read base stats, effects, moves and stable identity. Effect resolution, turn order, status behavior, intents and mutable combat state stay with the consumers. |
| 5, 8, 10 | Resolve catalog references from run instances and read static backpack, footprint and synergy data. Placement and progression remain runtime concerns. |
| 16–20, 22 | Resolve authored encounter and reward inputs. Eligibility, rolls, costs, economy and unlock policy belong to the respective systems. |
| 26, with 7 | Read definition keys and parameters for tooltip content. System 7 owns localization resolution. |
| 24 | Resolve persisted definition references. Save format, compatibility and migration policy belong to System 24. |
| 35 | Load equivalent definitions through the plain C# boundary and validate them for headless scenarios. |

System 4 owns the event schema and System 1 owns the seeded RNG. Neither is required to load the catalog.

## Tuning Knobs

System 2 has no independent gameplay tuning knobs. The catalog carries authored values such as speed, cooldowns, effect magnitudes and door parameters. Their units, permitted ranges, defaults and balance ownership belong to the consuming GDDs. Validation rejects invalid input rather than adjusting it.

## Visual/Audio Requirements

Definition keys can be mapped to assets in the presentation layer. This system emits no visual or audio cues. The payoff systems select and time cues based on resolved outcomes.

## Game Feel

The authored parameters behind pre-fight inspection and post-fight explanations come from the same catalog. This helps players trace the outcome of an arrangement to the items, heroes and enemy moves involved.

## UI Requirements

No screen is owned by System 2. The tooltip, door panels and breakdown can consume stable IDs, localization keys and authored values through their own interfaces.

## Cross-References

- [Game Concept](game-concept.md) covers Pillars 2, 4 and 5, MVP scope and deterministic simulation risks.
- [Systems Index](systems-index.md) assigns System 2 the Foundation layer and describes the engine-free boundary and downstream systems.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins `6000.6.3f1`. The repository has no `modules/core.md` for Unity. The existing ScriptableObject classes establish the local authoring pattern, while live/headless parity requires verification during implementation.

## Acceptance Criteria

1. **Given** valid definitions of all five kinds, **when** the live and headless paths load equivalent input, **then** lookups return semantically equal records and the core compiles without Unity engine references.
2. **Given** a renamed asset or changed translation for an existing definition, **when** another record resolves its stored typed ID, **then** it resolves the original identity.
3. **Given** a duplicate, malformed ID, missing or wrong-kind reference, invalid stat or invalid footprint, **when** the catalog is validated, **then** the error identifies the record and field and no partial snapshot starts a run.
4. **Given** equivalent input with shuffled unordered records and unchanged explicit sequences, **when** the catalogs are loaded and read, **then** enumeration is identical, declared sequences keep their order and no RNG is consumed.
5. **Given** a bound catalog, **when** the source asset's nested list is edited or the current combatant loses HP, **then** the bound snapshot is unchanged.
6. **Given** an MVP fixture, **when** the authored content is checked against the owning design sources, **then** it uses the MVP backpack and four door tiers, and an offered higher tier carries the price specified by its owner. Generation and pricing behavior are tested in the consumer.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which export or fixture representation lets Unity and the harness share a catalog reproducibly? | Technical Director and engine programmer | Architecture review, before the combat core spike is used as evidence. |
| Which prototype IDs and authored assets can migrate unchanged, and which require a mapping or replacement? | Gameplay programmer and systems designer | Before migrating content for the first headless catalog fixture. |
| Which structured effect payloads and numeric units cover setup and detonation without overbuilding the MVP? | Systems designer, System 3 | Before finalizing the effect payload contract. |
| How should persistent runs handle removed IDs or changed definitions? | System 24 designer | Before implementing save and resume. |
| Add the Grid Backpack contract to this GDD: footprint rules (cells connected by shared edges, inside a 3×3 box, 1 to 9 cells), the hero backpack profile (bound at most 6×6, start block and unlock sequence, validated to lie inside the bound and outside the start block with no repeats and a length cap) and the `rotationEnabled` rules flag. Raising a bound to 7×7 is deferred to Tier 2. System 8 treats these as provisional until they are here. | System 2, System 8 | Next System 2 revision. |
| Add the synergy data for System 10: per-item synergy rules (`targetTags`, `kind` of `DamageFlat`, `DamagePercent`, `CooldownReduction` or `StatusAmount`, a non-zero `amount`, and `statusId` for `StatusAmount`), the tag vocabulary, `MaxRulesPerItem` (provisional 3), the amount ranges, and the "applicable" check that needs each item's effect kinds. Whether an empty `targetTags` needs an explicit flag is open. | System 2, System 10 | Next System 2 revision. |
| Add the trigger data for System 9: one trigger kind per item (`Passive`, `Cooldown`, `Reaction`, `CombatStart`), the cooldown `N` (provisional `1..20`) and start delay `S` (`0..20`, default 0), the reaction pattern vocabulary (event kind, status ID, side filters), `MaxEffectsPerItem` (provisional 4, safe range 1 to 5), and the validation list in System 9's Dependencies section: passive items have no effects and every other item has one to `MaxEffectsPerItem`, a reaction item has a cooldown `N`, and `36 × MaxEffectsPerItem ≤ 256 − 64`. | System 2, System 9 | Next System 2 revision. |
| Add the speed data for System 11: hero and monster base speed as an integer in `5..60` (provisional), a flat `speedBonus` (provisional `−10..10`) on passive items, and the validation of the clamped sums (flat `−50..50`; percent `−1000..1000` permille, unused in the MVP). The content lint `Σ speed ≤ 40 × min speed` per encounter is reported as a content error with System 16. | System 2, System 11 | Next System 2 revision. |
| Add the hero data for System 15: a stable hero ID, name and role keys, base max HP (provisional `40..300`), initial shield (`0..30`), the backpack profile, a list of starting items with exactly one marked signature, exactly one innate item (a trigger of `Cooldown`, `Reaction` or `CombatStart`, effects of the allowed kinds, no cells), and the passive item field `hpBonus` (provisional `−20..40`) next to `speedBonus`; plus the validation of System 15's Rule 1. | System 2, System 15 | Next System 2 revision. |
| Add the monster and move data for System 14: monster ID, name key, classification, size class, base max HP (`1..100,000`), base speed (`5..60`), one to eight moves (at least one with `n ≥ 1`), an optional opener, a boss phase table; and per move an ID unique within the monster, a glyph, a hit count `n` (`0..8`, a windup has none), a base damage `B` (`1..100,000`), a target mode (`RandomHero`, `AllHeroes`, `SameAsMove`), a weight (`0..100`), a `heavy` flag, an optional `linkedNext` and the delivery archetype (Swing or Throw); plus the validation list in System 14's Edge Cases section. | System 2, System 14 | Next System 2 revision. |
