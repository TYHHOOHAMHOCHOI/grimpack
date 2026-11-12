# Seeded RNG

> **Status**: In Review
> **Design Review**: NEEDS REVISION. Findings addressed 2026-11-10, pending re-review.
> **Creative Director Review (CD-GDD-ALIGN)**: APPROVED 2026-11-10
> **Author**: Codex studio design session, with systems design, engineering and Creative Director consultation
> **Last Updated**: 2026-11-10
> **Last Verified**: 2026-11-10 (design checked against project files)
> **Implements Pillar**: Luck Follows Commitment and Earned Rarity. Supports Arrangement Is the Thinking.

## Summary

Seeded RNG supplies repeatable random draws to the systems that choose doors, drops and enemy intents. A run's seed and decisions can reproduce its results when the same game data and rules are used. Separate named streams keep one kind of draw from advancing another.

> **Quick reference** — Layer: `Foundation` · Priority: `MVP` · Key deps: `None`

## Overview

System 1 owns the run's deterministic random streams and integer sampling contract. At run creation the caller supplies one recorded seed. Gameplay consumers ask their named stream for draws, while System 5 holds their states in the run snapshot. The core is plain C# and runs in Unity and the headless harness. The chosen generator, derivation and reference vectors require a versioned architecture decision before implementation is considered ready.

The existing prototype sometimes seeds `System.Random` from `Environment.TickCount`. Those call sites are migration inputs and must be routed through the recorded run seed and stream contract. System 18 sets commitment bias and rarity caps. RNG supplies draws according to weights, without deciding the weights or promising a particular outcome.

## Player Fantasy

A player weighs a door bet and commits to an arrangement, hoping their choices build toward a rare payoff. Luck creates variety while the player's decisions retain meaning. Enemy intents chosen before combat remain binding so the player can plan around what they saw. The player experiences this infrastructure through the choices and outcomes, with presentation controlled by their owning systems.

## Detailed Design

### Core Rules

1. **One recorded seed.** The run-start boundary obtains or accepts an unsigned 64-bit seed and records it before the first gameplay draw. Tests and the harness may supply a known seed. Core initialization requires an explicit seed, including zero, and fails on absent or malformed input. It never substitutes clock time, Unity random state or a process-global generator. Normal-run seed creation is an outer-boundary responsibility to be specified during architecture.
2. **Named streams.** The MVP exposes `doors`, `drops` and `intents` as exact, case-sensitive identifiers. The versioned initialization contract derives each initial state from the seed and canonical stream name, independent of the order streams are first requested. Looking up a stream again returns the same advancing run-owned state. A draw from one stream does not advance another. Unknown names fail explicitly without creating a stream. Adding a stream requires a versioned contract and its own owner.
3. **Deterministic operations.** Core operations use fixed-width integer arithmetic and a pinned algorithm and stream derivation. The version identifies the complete contract, including generator, derivation, sampling rules, stream names and consumer ownership. The architecture decision must specify state width, bit operations, overflow semantics, seed mapping, stream-name encoding, initial state, output order and published reference vectors. It must assess the selected PRNG's statistical quality, correlations, finite-state and rejection-termination limits, and the derivation's stream-separation properties. It must be the same across supported Unity and headless runtimes. Neither `System.Random` implementation details nor `GetHashCode` define the contract. Only the simulation owner advances the live stream in the order specified by consuming rules.
4. **Bounded and weighted draws.** A bounded draw returns an integer in `[0, b)` using the rejection procedure in Formulas. Its exact uniformity guarantee applies to the ideal model described there. A weighted choice uses a caller-supplied, deterministic ordered list of candidates and nonnegative integer weights. It samples a ticket against cumulative intervals in that order. A zero-weight candidate cannot be selected. The consumer defines the list, weight policy and the meaning of the result. Missing, empty, invalid or overflowing input fails before any draw and does not change stream state.
5. **Preview and intent.** A preview may read a cloned RNG snapshot and discard all changes. It cannot advance the live stream. System 14 chooses the enemy's binding intent from the live `intents` stream before combat and stores that decision in run state. Display and forecast read the stored intent. Presentation, diagnostics, logs and System 2 catalog loading consume no gameplay randomness.
6. **Snapshot and restore.** System 5 owns the whole-run snapshot and stores the seed, complete RNG contract version, stream names and each stream's full state necessary to continue. A checkpoint is taken at an operation boundary, after all rejection draws in that operation complete. Restore validates the complete snapshot and version before exposing it. Duplicate, missing or unexpected stream names, an invalid state or unsupported version fail explicitly without replacing active state or silently reseeding. After a successful restore, every handle obtained beforehand must either resolve to the restored run-owned stream state or fail explicitly as stale before drawing. It must never advance a detached pre-restore state. An unsuccessful restore leaves existing handles and all stream states usable and unchanged. The ADR selects the handle mechanism. System 24 owns later persistent-save compatibility and migration.

### States and Transitions

| State | Trigger | Result |
|---|---|---|
| Uninitialized | Valid run seed and supported RNG version supplied | Create the run's named stream states. |
| Ready | Valid draw request | Advance only the requested stream and return its result. Rejected candidate words also advance that stream. |
| Ready | Invalid request | Return a diagnostic and leave all stream states unchanged. |
| Ready | Snapshot or preview | Copy the state at an operation boundary. Preview draws advance only the copy. |
| Ready | Valid restore | Replace the run's RNG state atomically with the complete restored state. Invalid restore leaves it unchanged. |

### Interactions with Other Systems

System 17 asks `doors` for the draws its door-generation rules specify. Systems 16 and 18 own encounter and drop decisions and use their designated stream according to their GDDs. For MVP, encounter choices tied to doors use `doors`, and reward selection uses `drops`; changes to that ownership require a versioned contract. Encounter and door draws share one advancing stream, so adding an encounter draw can change later door outcomes for the same seed. Systems 16 and 17 must specify the shared call order and treat changes as reproducibility-relevant. System 14 chooses binding intents using `intents`. System 35 supplies seed sets, player policies and scenarios to measure balance. It must report the RNG version and inputs alongside results. System 4 logs committed events without drawing random values.

## Formulas

The API draws unsigned 32-bit words `x` in `[0, 2³²)`. In the ideal model, successive words are independent and uniformly distributed over that range, including after any sequence of rejections. Under that model, rejection sampling terminates with probability 1 and each result in `[0, b)` is exactly equally likely. A deterministic finite-state PRNG does not itself establish that ideal guarantee. The ADR must evaluate its output and stream-derivation quality, correlations, finite period and behavior for rejection loops, and document their practical limits. The algorithm and stream derivation remain a pre-implementation decision.

| Symbol | Meaning | Range |
|---|---|---|
| `b` | Exclusive upper bound | Integer `1 ≤ b ≤ 2³²`. |
| `x` | Next unsigned output word | `0 ≤ x < 2³²`. |
| `L` | Rejection threshold | `2³² − (2³² mod b)`, calculated in at least 64 bits. |
| `wᵢ`, `W` | Weight of candidate `i` and total weight | Each `wᵢ ≥ 0`; `W = Σwᵢ`, checked in at least 64 bits, with `1 ≤ W ≤ 2³²`. |

For a bounded draw, repeatedly draw `x` until `x < L`, then return `x mod b`. Rejected words advance the stream. For `b = 2³²`, `L = 2³²` and every word is accepted. For a weighted choice, draw `t` in `[0, W)` and select the first candidate whose cumulative weight exceeds `t`. Candidate order and associated weights are inputs, not RNG policy. For example, weights `[2, 0, 1]` map tickets `0, 1` to the first candidate and `2` to the third.

## Edge Cases

| Case | Resolution |
|---|---|
| Seed is zero or two runs use the same seed | Zero is valid. Equal seed, rules, version, state and decisions reproduce the same draws. Different decisions or catalog values can change the outcome. |
| Bound is zero or above `2³²`, weight is negative, candidates are empty, total is zero or exceeds `2³²` | Reject before drawing. Preserve every stream state. |
| Rejection sampling rejects one or more words | Consume those words from the requested stream and keep sampling. Snapshot only after the operation completes. |
| Same candidate set arrives in a different order | Results may differ. Consumers must establish a stable order before asking for a weighted choice. |
| Snapshot has an unknown contract version, malformed state, or duplicate, missing or unexpected stream names | Reject the entire restore. Keep the current run state and existing handles unchanged. Do not silently reset a stream. |
| A preview is opened repeatedly | Each preview uses a copy. It leaves the real stream and the already selected binding intent unchanged. |

## Dependencies

There is no upstream gameplay GDD. Interfaces to downstream systems remain subject to their own design reviews.

| System | Contract |
|---|---|
| 2 | Provides equivalent catalog data and deterministic candidate order where requested. Catalog loading consumes no RNG. |
| 5 | Includes seed, version and all stream states in the run snapshot. |
| 14, 16, 17, 18 | Own intent, encounter, door and reward rules, candidate order and draw timing. They use the streams identified above. |
| 3 | Effect Resolution does not draw from any stream in this GDD. A future random combat effect needs a named, versioned combat stream and a coordinated change here first. |
| 4, 23, 24, 35 | Event Log and harness compare resulting events. Replay and save own later persistence and compatibility. The harness records versions and input scenarios. |

## Tuning Knobs

The seed is a run input, not a balance knob. Probability weights, caps and eligible candidates belong to the GDDs that own the corresponding decisions. System 18 owns the commitment and rarity policy. Algorithm choice and state representation are versioned technical decisions rather than gameplay tuning.

## Visual/Audio Requirements

The RNG core owns no visual or audio effects. Door, intent, reward and payoff systems present the resolved results under their own specifications.

## Game Feel

Outcomes should feel varied while the player can understand their choices and the consequences revealed by combat. Separate streams and repeatable inputs support debugging and balance measurement; they do not imply a guaranteed rare result.

## UI Requirements

System 1 owns no screen. System 14 exposes its already selected intent to the preview and HUD. Seed entry or display is not an MVP requirement.

## Cross-References

- [Game Concept](game-concept.md) defines the named streams, binding intents, commitment and rarity goals.
- [Systems Index](systems-index.md) defines the engine-free core, integer simulation and run snapshot ownership.
- [Game Data Definitions](game-data-definitions.md) requires deterministic catalog reads without RNG draws.
- [Event Log](event-log.md) requires semantic event parity given identical seed and inputs. That GDD is currently in review.
- [Unity version reference](../../docs/engine-reference/unity/VERSION.md) pins Unity `6000.6.3f1`. The local Unity Core module reference is absent. Cross-runtime generator behavior needs validation in the pinned editor and headless runtime.

## Acceptance Criteria

1. **Golden fixtures. Given** each published fixture's exact seed, contract version, stream name, initial state and ordered request sequence, **when** it is executed in Unity and headless, **then** each path matches that fixture's independently verified expected outputs and final stream state. **Parity. Given** any identical supported inputs and ordered requests selected for the test, **when** both paths execute them, **then** their outputs and final stream states match each other. **Precondition:** the algorithm, derivation and fixture vectors have been specified in the architecture decision.
2. **Given** equal initialized runs, **when** extra draws are made on `doors` in one run, **then** corresponding `drops` and `intents` sequences remain unchanged. Repeated lookup of `doors` continues its existing sequence.
3. **Given** bounds `1`, a power of two, a non-power of two and `2³²`, **when** bounded draws use controlled words, **then** results follow the formula and the next draw reflects every consumed word. In particular, for `b = 3`, `L = 4294967295`, word `4294967295` is rejected and subsequent word `4` returns `1`. Invalid bounds leave state unchanged.
4. **Given** weights `[2, 0, 1]` and controlled tickets `0`, `1`, `2`, **when** choosing, **then** the first, first and third candidates are selected. A total of exactly `2³²` is accepted and a total above it is rejected. Empty lists, negative weights, zero total and overflow are rejected without consuming a word.
5. **Given** interleaved draws and a retained stream handle, **when** an earlier snapshot is restored into a compatible run, **then** fresh handles continue from the restored state and the retained handle either continues from that state or fails explicitly as stale without drawing. Unknown versions, malformed states and duplicate, missing or unexpected stream names reject the whole restore. In each failure case all stream states and the previously acquired handles remain unchanged and usable.
6. **Given** a binding enemy intent and an RNG snapshot, **when** preview is opened repeatedly, **then** neither the live RNG states nor the stored intent change. **Integration, when System 14 exists:** after the intent is displayed, rearrange the party before combat and verify the same intent remains binding through the enemy's first action.
7. **Integration, once the combat spike exists. Given** the same seed, catalog, rules, initial snapshot and player decisions, **when** Unity and headless execute the scenario, **then** System 4's ordered event records and historical views match. The comparison identifies the first mismatch.
8. **Given** the simulation core assembly, **when** built and its references inspected, **then** it contains no `UnityEngine` or `UnityEngine.*` assembly references.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| Which pinned PRNG and derivation, encoding, overflow rules and independently checked vectors satisfy cross-runtime reproducibility? | Technical Director and engine programmer | Architecture decision, before implementing RNG or treating parity as passed. |
| How does normal run creation obtain and record an unpredictable seed at the platform boundary? | Technical Director and System 5 | Run creation design, before live run implementation. |
| Which candidate ordering, draw count and weight policy apply to each encounter, door, reward and intent choice? (Answered for intents by System 14, [enemy-and-intent.md](enemy-and-intent.md): a sequence of 12 entries per enemy drawn at room entry, enemies in ascending slot, exactly two calls per entry (a move draw then a target draw, bound 1 when forced), candidates in authored move order and party-slot order. Encounters (System 16, [encounter-generation.md](encounter-generation.md)): one weighted draw per door card on `doors` over the cell's templates in ascending template-ID order, a bound-1 draw when the cell has one template, made by the caller after System 17's draws in card order, plus one draw for room 1 at run start and none for the boss: 22 draw calls per run (calls, not words), every weight at least 1. Doors and rewards are still open.) | Systems 16, 17 and 18 | Before implementing each consumer. |
