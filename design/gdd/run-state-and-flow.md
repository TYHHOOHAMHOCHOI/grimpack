# Run State and Flow

> **Status**: Designed (CD-GDD-ALIGN CONCERNS, concerns applied; independent `/design-review` not yet run)
> **Author**: user + Claude design session, with specialist consultation
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillar**: Arrangement Is the Thinking (supports: the flow shows what the player needs before each decision); Earned Rarity (reproducible runs); Power Has a Price (the order of the door bet)

## Summary

Run State and Flow is the engine-free command machine that moves one climb of the dungeon from the party pick to the end screen. It owns the floor pointer (rooms 1 to 8, then the boss), the order of every step in a room (the win, the buff, the item, the door choice, room entry where the intents are bound, the decisive arrangement against those intents, Start), the one victory transaction that builds the reward offer and the next door row together, and the whole-run snapshot that every other system's state is stored in. Each draw happens at one fixed point, so a run is reproducible from its seed and commands. A defeat or a Draw ends the run with a summary and a Restart on a new seed; there is no retry, no reroll and no resume in the MVP.

> **Quick reference** — Layer: `Core` · Priority: `MVP` · Key deps: `Seeded RNG, Game Data Definitions`

## Overview

System 5 owns the run: the state machine that moves the player through one climb of the dungeon, the floor pointer (rooms 1 to 8, then the boss), the order of every step inside a room, and the whole-run snapshot that every other system's state is stored in. It is an engine-free **command machine**: the player's choices (pick the party, start the fight, claim the buff, take or skip an item, choose a door) arrive as commands from the live UI and from the headless harness alike, and each command is accepted, rejected with a reason, or stops the run. System 5 does not decide any rule of another system. It calls them in a fixed order: System 15 forms the party, System 8 holds the grid, System 14 binds the enemies' intents, System 12 runs the fight, and Systems 18 and 17 build the reward offer and the next door row, with each draw happening at one documented point so that a run is reproducible from its seed and its commands. A defeat ends the run with a summary and a Restart on a new seed; there is no retry of a room. Without this system the other systems would each own a piece of a run and nobody would own the order; with it, the order of the bet is written once (Pillars 3, 4 and 5).

> *Specialists consulted for this section: systems-designer, gameplay-programmer, engine-programmer, game-designer, ux-designer, creative-director (2026-11-13). Their findings are applied below.*

## Player Fantasy

The player never sees this system; what they feel is the rhythm it fixes. After a win, the dungeon pays out, the player chooses a door knowing its price and its promise, steps into the room, and only then sees exactly what the enemies will do, and arranges the backpacks against it. Then they press Start and watch. If it goes wrong they know why, and the next run is a click and a confirm away. **Every room is one bet, laid out in full before you place it. You lose only what you staked, and the next run is a click and a confirm away.**

*The beats of a room, in order:* (1) the win and the reward: the buff, then one item or none; (2) the door choice, with the grid still open; (3) room entry, where the intents are bound; (4) the decisive arrangement against the visible intents; (5) Start, after which nothing the player does can change the fight.

*Pillar anchors.* Pillar 4, Arrangement Is the Thinking: everything the player needs to decide is visible before the fight, and the decisive arrangement happens after the intents are known. Pillar 3, Earned Rarity: no run starts overpowered; the bet cannot be undone, so a harder room is a real bet, and every run is reproducible from its seed and commands. Pillar 5, Power Has a Price: no reroll, no retry and no retreat after the door is chosen.

*Guardrails* (binding rules): the snapshot is the only gameplay state, and anything derived is recomputed from it (the grid's undo record, the `runId` and the flow log live outside it and never affect gameplay); no input changes the fight once it starts (playback speed and skip only change presentation); every draw happens at a fixed, documented point, and redoing or reloading a step never redraws; abandoning a run is allowed and counts as an abandoned run, not a retry.

*Risks the design guards against:* too many confirms per room (the player should make about three discrete decisions per room plus any number of placements), a long post-mortem standing between the player and the next run, a run status shown as a table of stats, and a flow that hides what a blocked control is waiting for.

> *`creative-director` consulted (2026-11-13); the spine of the fantasy is theirs.*

## Detailed Design

### Core Rules

1. **A command machine, engine-free.** `RunFlow` (name proposed) takes one command at a time and returns a result. A command is a plain value (a kind and integers; the party command carries three hero IDs); a result is `Accepted`, `NoEffect(AlreadyHandled)`, `Rejected(reason)` (wrong state, stale serial, bad index, confirmation needed; the state is unchanged and the run continues) or `TechnicalStop` (a callee's `ContractViolation`, a failed invariant or a failed System 12 setup; the run is abandoned and the diagnostic kept). Tests assert result codes and states, never messages. Every flow command carries the `serial` it expects (the first is 0); an accepted flow command increments it, while grid pass-through commands, `NoEffect`, `Rejected` and `TechnicalStop` do not. A command whose serial differs from the current one is `Rejected(Stale)`, so a double click cannot act twice. The checks run in this order: stale serial, wrong state (every command except `StartNewRun` and `ReturnToMenu` is a wrong state in Ended, Stopped and Abandoned), invalid argument or bad index, confirmation needed. A `WrongState` result names the step the run is waiting for, so a blocked control can say why. The session also carries a `runId` (a monotonic counter held by the Unity layer, never drawn from the RNG), so a queued command of an old session cannot match the serial of a new one after Restart. The machine reads no clock, holds no random generator and has no mutable static state (as Systems 3, 11, 14, 16, 17 and 18).
2. **One session object.** A `RunSession` owns everything mutable in a run: the three RNG streams (mutated in place), the item-ID counter, the Grid Backpack model, the party, the run state and the flow log. It borrows an immutable, plain-data catalog (System 2) and owns no statics, so two sessions run back to back in one process share nothing. The Unity layer holds exactly one session and drops it when a new run starts or the scene is destroyed. Because Unity 6.6 skips the domain reload on entering Play Mode (and this project has it switched off), the core never relies on static initialisation (checked by a reflection test); in the Unity layer, which is Play Mode evidence and not an EditMode test, the bootstrapper subscribes and unsubscribes in `OnEnable` and `OnDisable` and keeps no static instance.
3. **The run starts with the party.** The command `PickParty` carries the three chosen hero IDs (the pick screen's own choices before confirming are UI state, not run state); duplicate, unknown or fewer than three IDs are `Rejected(InvalidArgument)` with no effect. The counter `nextItemId` starts at 1. It is one transaction: System 15 forms the party in pick order; the starting items are created, with item IDs from the counter in this order per slot: the innate item first, then the starting items in authored order; the room 1 spec is built with its single variant draw on `doors` (System 16 Rule 7); System 14 binds the room 1 intents on a clone of `intents`; the run enters Arrangement. Room 1 has no door and no buff.
4. **The floor pointer.** `room` runs 1 to 8, then 9 for the boss. Rooms are never revisited. The key of an encounter is its room index.
5. **Arrangement.** In Arrangement the grid commands (System 8's: pick, place, rotate, send to stash, remove to tray, undo, switch hero) pass through to the grid, and `StartFight` is legal only when no reward step is pending. The grid is editable continuously from Thaw to Freeze: there is no separate rearrangement phase before the door choice, and the decisive arrangement is the one made after room entry, against the bound intents (Pillar 4).
6. **Starting a fight.** `StartFight` freezes the grid. A non-empty tray needs the command to carry `acceptLoss`; without it the result is `Rejected(NeedsConfirm)` listing the items the player would leave behind. System 12 then runs the encounter to a terminal outcome in one synchronous call, with no RNG draw (System 12 Rule 6 and System 14 Rule 5), and returns the outcome and a handle to the trace. The state `Fighting` exists only inside this call and is never visible at a command boundary. The live UI plays the trace back afterwards; playback can never schedule work, and no input during playback changes the result. Until playback ends or is skipped the presentation shows no reward state and sends no flow command except `AbandonRun` (a binding rule, so that the outcome is never spoiled).
7. **Outcomes.** Victory in rooms 1 to 8 runs the victory transaction (Rule 8) and then thaws. Victory against the boss ends the run as **Won** (no offer, no row). Defeat, and a Draw (both sides downed, System 12), end the run as **Lost** with a reason (defeat, draw, action limit, no eligible actor) kept for the summary. A technical stop ends the run as **Stopped**. After Lost the grid stays frozen and the trace stays in System 4 for the breakdown. `StartFight` hands the outcome to an internal, idempotent `ApplyOutcome(room, outcome)`; a late or duplicate delivery of the same room's outcome (for example from an asynchronous runner or a skipped playback) goes through the same entry and returns `NoEffect(AlreadyHandled)` when `room ≤ lastResolvedRoom`. The defeat reasons ActionLimit and NoEligibleActor are Lost, as System 12 defines.
8. **The victory transaction.** On a victory in rooms 1 to 8, System 5 takes the by-value snapshot from the frozen view (items, links and the per-hero counts), builds System 18's offer on a clone of `drops` (rooms 1 to 8) and System 17's next row on a clone of `doors` (rooms 1 to 7; its tier draws first, then System 16's variant draws), and commits both clones, the stored offer and row, and `lastResolvedRoom` together. If any of them fails, both clones are discarded, no stream advances and the run stops. Only then does System 5 thaw the grid. The preparation record of the finished room is discarded.
9. **Reward steps, in order.** After the thaw: (a) **BuffClaim**, for rooms 2 to 8 (room 1 has no door): the player chooses a hero (System 17 Rule 7), skipped without a prompt when no hero can receive anything (a hero can receive when `hpGranted > 0` or `cellsGranted > 0` under System 17's caps; the player may still choose a hero who receives less than another); (b) **OfferPending**: the player takes one card or skips (System 18 Rule 8), skipped when the offer has no cards (an auto-skipped step is consumed inside the same command, with no prompt, no draw, no extra serial and no skip metric; a restore that finds BuffClaim or OfferPending in a state that would have been skipped is rejected); (c) after rooms 1 to 7, **DoorChoice**; after room 8 the boss is entered at once (Rule 11), inside the last reward command (or inside the victory transaction when both reward steps are skipped). The grid remains editable throughout. A pending buff or offer blocks `ChooseDoor` and `StartFight`.
10. **Choosing a door.** `ChooseDoor(i)` is one transaction: System 5 stores the chosen card (tier, buff by value, spec) as the room record, discards the row, has System 14 bind the new room's intents on a clone of `intents` from the stored spec, commits, and enters Arrangement. The choice is final: no reroll, no retreat and no retry of the room.
11. **The boss.** After room 8's reward steps the run enters the boss room without a door: the room record is the floor table's boss spec (identity multipliers, System 16 Rule 2), its intents are bound, and the run enters Arrangement. The boss makes no `doors` draw and gives no offer.
12. **Defeat, quit and crash.** A defeat ends the run: the summary screen shows the outcome, the room reached and the cause, and one Restart command starts a new run on a new seed, with the previous party pre-selected on the pick screen, so Restart then Confirm is the whole path. Abandoned runs are counted in the denominator of every rate System 35 reports for the 1-in-20 target (or both rates are reported), so that abandoning early cannot work as an unmeasured reroll. There is no retry and no checkpoint resume in the MVP (save and resume is System 24, Vertical Slice). Leaving mid-run is the command `AbandonRun` (the UI asks for confirmation and sets the flag): the run ends as **Abandoned**, logged as `abandoned at room N` with the seed, and not counted as a defeat; in Ended or Stopped the same action only returns to the menu and logs nothing. A crash loses the run. The snapshot is still built and restored in memory, and round-trip tested every build, so System 24 only adds persistence.
13. **The seed.** The seed is obtained at the platform boundary outside the core, through a seed port (the live game uses an operating-system entropy source assembled into a 64-bit value; the harness and tests supply a fixed seed), and recorded in the snapshot. Restart takes a new seed. The seed is shown read-only and copyable on the end screen and hidden during the run; there is no seed entry in the MVP. A seed alone does not replay a run (drops depend on the door tier chosen and on the arrangement), so the flow log of commands is kept as a bug-report artefact.
14. **Draw points.** Every draw happens at one fixed point: `PickParty` (1 call on `doors` for room 1, then the room 1 intents on `intents`); each victory in rooms 1 to 7 (`drops` up to 6 calls, `doors` 6 calls); a victory in room 8 (`drops` up to 6 calls only); each `ChooseDoor` and the boss entry (`intents`, 2 calls per entry, 12 entries per enemy, enemies in ascending slot). A full run makes 43 calls on `doors` and at most 48 on `drops`. Previews and restores never redraw.
15. **Idempotency and validation.** A duplicate victory hand-off (the same room already resolved) returns `NoEffect(AlreadyHandled)` and draws nothing. `ChooseDoor`, `ClaimBuff` and the item commands are not idempotent: a second call is `Rejected(WrongState)`. A command legal in no state is rejected; `AbandonRun` is legal in every state except Ended, Stopped and Abandoned.
16. **The snapshot.** The run snapshot is canonical text: UTF-8 without a byte-order mark, LF line endings, the first line `GRIMPACK-RUN v1`, fixed section order, invariant decimal integers, and a last line `CHECKSUM fnv1a64 <16 hex>` over every byte from the start through the LF that ends the line before it (never `GetHashCode`; the multiplication is `unchecked` modulo 2^64, the one exception to the checked context). An embedded slice is length-prefixed (`BEGIN GRID <n lines>`) and the parser reads exactly `n` lines, so the grid slice's own checksum line is data. The flow log of commands is not in the snapshot; the replay oracle is a fixed seed plus the command script. Its sections, in order: the versions (run format, RNG contract, door, encounter, drop and intent content, catalog); the seed; the three stream states; the flow (phase, `room`, `serial`, `lastResolvedRoom`, result); the party (hero IDs by slot, `B_perm[3]`, `doorHp[3]`, `doorCells`, and the innate item instance IDs); `nextItemId`; the Grid Backpack slice embedded verbatim (System 8's own snapshot); the room record and the pending-buff record (tier, hp, cells and the per-hero counts for the default hero); the preparation record (the bound sequences only; the `intents` state lives in the streams section, and a preparation record present means the stream is the post-binding state); the stored row; the stored offer with its reading and labels; the per-room aggregates of the run summary (Rule 19); and the run metrics (call counts per stream, rooms cleared, takes and skips; never read by gameplay; saturating, never throwing). The combat slice and the event log are not in it. A checkpoint is taken only at a command boundary, never during `Fighting`, and only in Arrangement, BuffClaim, OfferPending and DoorChoice (Created, Ended, Stopped and Abandoned are not snapshotable). The room 8 to boss transition runs inside one command, so there is no checkpoint between them.
17. **Restore.** Restore checks in this order and rejects atomically: the version line, the checksum over the raw bytes, the parse (non-canonical forms such as CRLF, a byte-order mark, uppercase hexadecimal, a leading plus sign or trailing text are rejected), the versions and the catalog fingerprint, then the phase matrix, then the invariants. It parses into a staging object, validates the phase-to-record matrix (for example Arrangement needs a room record and a preparation record and forbids a row) and the invariants (`doorHp ≤ B_perm`, every item ID, including the innate items', in `1..nextItemId − 1`, the cells opened by doors equal `doorCells`, the `doors` call count equals `1 + 6 × min(lastResolvedRoom, 7)`, every ID exists in the catalog, the row's target room equals `room + 1`, stream consistency (each stream's recorded position matches its call count in the metrics), every version equal), and only then swaps the whole objects. Any failure rejects the whole restore. The phase-to-record matrix is: Created (nothing stored); Arrangement (room record and preparation record, no row); BuffClaim (room record of the room just won with the pending buff, offer and row stored); OfferPending (offer and row stored, no pending buff); DoorChoice (row stored, no offer, no pending buff); Ended and Stopped (the result only); Abandoned (the result only). Nothing is rebuilt or redrawn on restore: a stored row, offer or sequence is used as stored. The undo record of the grid is not in the snapshot and is lost on restore.
18. **The item-ID counter.** System 5 implements System 8's item-ID source as a run-scoped `long` counter whose `Next()` is called last in a creation; the counter is never recomputed from the largest existing ID, because Freeze destroys the tray's IDs.
19. **The run summary.** The tracer System 4 seals each encounter's trace, so System 5 keeps per-room aggregates in its own slice for the summary: the door tier taken, the danger, the actions used and the peak hit, plus the committed direction and the outcome with its reason. The decisive-intent line of a defeat is read from the last trace (System 30). The exact fields are System 4's and System 30's decision (undecided, so the criterion for them is BLOCKED).
20. **Order of ties and determinism.** Every collection that decides an order is sorted by a unique ordinal key (never by a dictionary, a set or an unstable sort), all arithmetic is integer in a checked context (except the checksum's modular multiplication), and the core uses no `Guid`, thread or clock.

### States and Transitions

| State | Legal commands | Entered by |
|---|---|---|
| Created | `PickParty(heroIds)` | A new run (seed taken) |
| Arrangement | grid commands, `StartFight(acceptLoss)` | `PickParty`, `ChooseDoor`, boss entry |
| BuffClaim | `ClaimBuff(slot)`, grid commands | Thaw after a victory in rooms 2 to 8 (skipped when nothing is grantable) |
| OfferPending | `TakeOffer(i)`, `SkipOffer`, grid commands | After BuffClaim (skipped when 0 cards) |
| DoorChoice | `ChooseDoor(i)`, grid commands | After rooms 1 to 7's reward steps |
| Ended(Won) | `StartNewRun` | Victory over the boss |
| Ended(Lost, reason) | `StartNewRun` | Defeat or Draw |
| Stopped | `ReturnToMenu` | A technical stop |
| Abandoned | `ReturnToMenu`, `StartNewRun` | `AbandonRun` (confirmed) |

`QuitToMenu` is legal in every state. Transitions: Created → (PickParty) → Arrangement(1); Arrangement → (StartFight) → [Fighting, internal] → Victory: BuffClaim → OfferPending → DoorChoice → (ChooseDoor) → Arrangement(next); Victory in room 8: BuffClaim → OfferPending → boss Arrangement; Victory over the boss → Ended(Won); Defeat or Draw → Ended(Lost); any callee failure → Stopped.

### Interactions with Other Systems

| System | Data in | Data out | Interface owner |
|---|---|---|---|
| 1 Seeded RNG | Stream clones, fork and commit, stream states | Seed, stream states in the snapshot | System 1 owns the streams; System 5 holds their states and the order of calls. |
| 2 Game Data Definitions | The immutable catalog and its version | None | System 2. |
| 8 Grid Backpack | The grid model, its snapshot slice, Freeze, Thaw, Create, Unlock | The item-ID source; grid commands pass through | System 8 owns placement; System 5 owns when Freeze and Thaw happen. |
| 12 Combat Resolution | The outcome and the trace handle | Frozen arrangement, baseline, bound sequences | System 12 owns the fight and returns one outcome. |
| 14 Enemy and Intent | Bound sequences | Roster, multipliers, stream clone | System 14 is the callee at party formation, door choice and boss entry. |
| 15 Party and Heroes | Party formation, `B_perm`, restored heroes | Three hero IDs | System 15. |
| 16, 17, 18 | Spec, row, offer, buff | Snapshot (by value), room, door tier, stream clones | Those systems own their rules; System 5 stores and orders. |
| 4 Event Log | Trace, metrics source | Run summary data | System 4 owns the trace; System 5 keeps aggregates. |
| 24 Run Save and Resume | None | The snapshot text and restore | Vertical Slice; System 24 adds persistence only. |
| 31 Menus and Screen Flow, 25 to 30 UI | State and legal commands | Commands | Those systems present the state and send commands. |
| 35 Balance Simulation | A policy that issues commands | Results | System 35 drives the same machine headlessly. |

*Provisional:* the command names, the assembly layout and the snapshot section names are proposals for the architecture ADR; the engine-free assembly name is NOT SOURCEABLE until that ADR.

> *Specialists consulted: systems-designer, gameplay-programmer, engine-programmer, game-designer, ux-designer (2026-11-13).*

## Formulas

All arithmetic is on integers (the seed is a 64-bit unsigned value, item IDs are 64-bit signed, everything else 32-bit) in a checked context. System 5 defines few numeric rules; these formulas fix the draw-call budget, the snapshot checksum and the counters, so that a run is checkable.

**Formula 1: calls per stream in a full run.**

| Stream | Calls | Derivation |
|---|---|---|
| `doors` | `1 + 7 × 6 = 43` | 1 for the room 1 variant at `PickParty`; at each victory in rooms 1 to 7, 3 tier draws (System 17) and 3 variant draws (System 16). |
| `drops` | at most `8 × 6 = 48` | At each victory in rooms 1 to 8, at most 3 slots × 2 calls (System 18); fewer when candidates run out. |
| `intents` | `Σ over the 9 bindings of 2 × 12 × E` | Bindings: room 1 at `PickParty`, rooms 2 to 8 at `ChooseDoor`, the boss at entry; `E` enemies, 12 entries each, 2 calls per entry (System 14). At most `9 × 24 × 4 = 864` with 4 enemies per roster (1,296 at the cap of 6). |

**Formula 2: the room cycle.** After the victory in room `w` the stored items are, by value, `offer(w)` (rooms 1 to 8) and `row(w + 1)` (rooms 1 to 7); `lastResolvedRoom = w`; a command whose room is at most `lastResolvedRoom` is `NoEffect(AlreadyHandled)`.

**Formula 3: the checksum.** FNV-1a 64 over the snapshot body bytes (UTF-8, LF): `h0 = 14695981039346656037`; for each byte `b`: `h = (h XOR b) × 1099511628211 mod 2^64`; the last line is `CHECKSUM fnv1a64 ` followed by `h` as 16 lowercase hexadecimal digits. *Example:* the empty body hashes to `cbf29ce484222325`.

| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `serial` | int | `0..2^31 − 1` | flow state | Starts at 0; increments on each accepted flow command; a command carrying another value is rejected. |
| `room` | int | `1..9` | flow state | Floor pointer (9 is the boss). |
| `lastResolvedRoom` | int | `0..9` | flow state | Highest room whose victory was resolved. |
| `nextItemId` | long | `≥ 1` | run state | Next item instance ID; never recomputed from the maximum. |
| `doorHp[h]` | int | `0..DoorHpCap` | party slice | Max HP granted to hero `h` by doors (System 17). |
| `doorCells` | int | `0..DoorCellCap` | party slice | Cells granted by doors this run (System 17). |

**Formula 4: snapshot budget (design check).** A snapshot of the worst-case fixture is at most 16,384 bytes (mostly the grid slice and the stored sequences), checked by a test; no checkpoint is taken during `Fighting`.

**Worked example (room 3 through a Green door with three enemies; rooms 1 and 2 had one and two enemies).** After `PickParty`: `doors` 1, `drops` 0, `intents` 24 (the room 1 binding: 2 × 12 × 1). Victory in room 1: `drops` 6, `doors` 7. `ChooseDoor` into room 2: `intents` +48, total 72. Victory in room 2: `drops` 12, `doors` 13. `ChooseDoor` into room 3 (three enemies): `intents` +72, total 144. Arrangement edits: 0 draws. Victory in room 3: `drops` 18, `doors` 19 (`1 + 3 × 6`). Thaw. `ClaimBuff(slot 1)`: `B_perm` +8 and `doorHp` +8, no draw. `TakeOffer(2)`: `nextItemId` +1 (the counter is called last in the creation). Then `ChooseDoor` into room 4.

> *`systems-designer` consulted (2026-11-13); the call budget and the checksum are theirs.*

## Edge Cases

Format: *if condition: exact outcome* (the party that detects it). "Rejected" means the state is unchanged, no draw happens, no item ID is consumed and the serial is unchanged.

**Commands**

- *A command in the wrong state:* `Rejected(WrongState)` naming the awaited step (System 5), so a blocked control can say why. Precedence: stale serial, wrong state, bad argument or index, confirmation needed.
- *A stale serial, or a double click:* `Rejected(Stale)`; a re-read serial then gives `WrongState`. A command of an old session cannot match a new session's serial because of the `runId`.
- *Grid pass-through:* grid commands do not change the serial; a grid `NoEffect` stays `NoEffect`, and any other non-Ok grid code becomes `Rejected` with the grid's code (Systems 5 and 8).
- *`StartFight` with a non-empty tray and no `acceptLoss`:* `Rejected(NeedsConfirm)` listing the items, checked before Freeze so nothing changes. With `acceptLoss` and an empty tray the flag is ignored.
- *A held item at Start:* the grid model has no held state (System 8); the UI blocks Start while an item is held. If the command arrives anyway, an item that came from a container stays in place, one from the tray is destroyed with the tray, and the confirmation list includes it.
- *A bad index (`ChooseDoor`, `TakeOffer`, `ClaimBuff` slot):* `Rejected(BadIndex)`.
- *`PickParty` with a duplicate, unknown or locked hero or a count other than three:* `Rejected(InvalidArgument)` before any ID or draw; a content or ID-source failure after partial creation is `TechnicalStop`; a second `PickParty` is `WrongState`.
- *An unexpected exception anywhere:* `TechnicalStop`; the session then rejects everything except `ReturnToMenu`. Stopped is not a defeat and not abandoned.

**The victory transaction**

- *The offer builds and the row fails, or the reverse:* both clones are discarded, no stream advances, nothing is stored, `lastResolvedRoom` is unchanged, the grid stays Frozen, and the run is Stopped (Systems 17 and 18 detect).
- *Clone chaining:* at most one clone per stream per transaction; System 16's variant draws continue System 17's already advanced `doors` clone and never take a fresh fork. Committing a clone whose parent position differs from the live stream is a `ContractViolation`: nothing is committed and the run is Stopped. Commit is "build everything, then assign all parts without throwing".
- *A duplicate or late hand-off for the same or a lower room:* `NoEffect(AlreadyHandled)`, zero draws. A hand-off for a room beyond the one being fought, or outside `StartFight`, or a second different outcome for the same room, is `TechnicalStop`.
- *Draw (both sides downed), ActionLimit, NoEligibleActor:* Ended(Lost) with that reason, no offer, no row, the grid stays Frozen. A scheduler error is Stopped (System 12).
- *A technical stop by phase:* before Freeze, the tray is intact and the grid Editable; during `Fighting`, the valid trace prefix is kept and the grid stays Frozen with no outcome; in the victory transaction nothing is committed and the rooms cleared are unchanged; after the commit, a failure at Thaw is Stopped and is not snapshotable.
- *`ClaimBuff`:* `Unlock` is the only fallible step and runs first; `Frozen` at that point is Stopped (unreachable after Thaw).

**Quit, crash and Restart**

- *`AbandonRun`:* in Created nothing is logged; in Arrangement, BuffClaim, OfferPending and DoorChoice the run is logged `abandoned at room N` and needs confirmation; in Ended, Stopped and Abandoned it only returns to the menu. During playback the machine is already past `StartFight`, so a Won or Lost keeps its result and a reward state counts as abandoned at `room`. A command is never interrupted, so there is no open transaction; the presentation gates gameplay and flow commands except `AbandonRun`. Presentation-only speed/Skip and the user's read-only intent Details are allowed without changing the result or exposing reward early; their interfaces are owned by Systems 26/27/29. Whether to add a `ContinueFromPlayback` command remains open.
- *A crash between commands:* the run is lost; a flow log with no terminal line means crashed, not abandoned.
- *Restart:* a new session with a new seed from the port, fresh streams, serial 0, `nextItemId` 1 and metrics zero; only the immutable catalog is shared (copied from the authoring assets at session creation, because the domain reload is off); lazy caches in the catalog are built eagerly.
- *The seed port returns 0:* valid (System 1). A fixed seed is allowed and `seedSource` is logged. A port failure gives `Rejected(SeedUnavailable)` and the old session is untouched.
- *Previews:* System 5 hands out no stream clone outside a transaction; System 35 what-ifs use Restore of a snapshot.
- *The catalog changes between runs:* an FNV-1a fingerprint of the canonical catalog is stored next to the manual versions; a mismatch on restore rejects, and the fingerprint is logged in the run header.
- *Metrics:* they saturate and never throw; a restored metric above the Formula 1 maxima is rejected; a serial overflow is Stopped.

**Rooms and the boss**

- *Room 8 to the boss:* the transition runs inside the last reward command (or inside `StartFight` when both steps auto-skip), so no checkpoint lies between them; a boss binding failure is Stopped.
- *Boss victory:* Ended(Won), `lastResolvedRoom` 9, rooms cleared 9, no offer and no row (index 9 is a `ContractViolation` for both builders), the grid stays Frozen, takes and skips unchanged.
- *Room 1:* no door and no buff; `PickParty` validates, forms the party, creates IDs, makes the `doors` variant draw (the roster is needed for the next step), binds the intents on a clone (the party is needed), and commits both together.

**Restore**

- *Any corruption* (version, checksum, non-canonical text, phase matrix, invariants): rejected atomically; the live session is untouched. Created, Ended, Stopped and Abandoned are not snapshotable or restorable.
- *Phase matrix:* Arrangement(room `r`) needs the room record and the preparation record, nothing else, and `lastResolvedRoom = r − 1`; BuffClaim needs `room ≥ 2`, the pending-buff record and the offer, and a row only if `room ≤ 7`, with `lastResolvedRoom = room`; OfferPending needs at least one card; DoorChoice needs the row only, with `room ≤ 7`.
- *A missing preparation record in Arrangement:* rejected and never re-bound.
- *Restore then continue equals the uninterrupted run,* compared as snapshot text after each command, except the undo record (`NothingToUndo` after a restore), the trace and the flow log; also `Snapshot(Restore(t)) = t`.

> *`systems-designer` consulted (2026-11-13); its 29 cases are merged. The top determinism hazards it named (the `doors` clone chain, anything rebuilt on restore, the snapshot taken before Thaw, the item-ID counter, and the canonical text with invariant culture and no hash ordering) are covered by Rules 8, 16, 17 and 18 and by the criteria above.*

## Dependencies

| System | Direction | Nature | Notes |
|---|---|---|---|
| 1 Seeded RNG | Depends on (hard) | Streams, fork and commit, stream states, the seed | The RNG algorithm and vectors are an open ADR; no golden RNG file can be authored before it. |
| 2 Game Data Definitions | Depends on (hard) | The immutable catalog and its fingerprint | Plain-C# records; ScriptableObjects only as authoring wrappers converted before the run. |
| 8 Grid Backpack | Depends on (hard) | Grid model, Freeze, Thaw, Create, Unlock, snapshot slice, item-ID source | System 5 implements the ID source and calls Freeze and Thaw. |
| 12 Combat Resolution | Depends on (hard) | One synchronous fight returning an outcome and a trace handle | Single terminal hand-off; technical stop. |
| 14 Enemy and Intent | Depends on (hard) | Binding at party formation, door choice and boss entry | Bound sequences are stored in the preparation record. |
| 15 Party and Heroes | Depends on (hard) | Party formation, `B_perm`, restoration | |
| 16, 17, 18 | Depends on (hard) | The spec, the door row and the buff, the offer | Built in one victory transaction, stored by value. |
| 4 Event Log | Depends on (soft) | Trace and the data for the run summary | System 5 keeps per-room aggregates itself. |
| 24 Run Save and Resume | Depended on by | Persists the snapshot | Vertical Slice; adds persistence only. |
| 31 Menus and Screen Flow, 25 to 30 UI | Depended on by | Present the state and send commands | |
| 35 Balance Simulation Harness | Depended on by | Drives the same machine with a policy | The command interface is the harness's interface. |
| 37 Settings, 6 Input and Focus | Related | Playback speed, input | No dependency in either direction beyond the UI. |

The systems index lists only Systems 1 and 2 as dependencies of System 5; it orchestrates the systems above through narrow ports, with each system owning its own snapshot slice. There is no cycle: System 5 calls the systems above and they never call System 5.

## Tuning Knobs

System 5 has almost no balance values; these are the values that shape the feel and the checks. They live in data (System 2) or the UI settings, never in code.

| Knob | Default | Safe range | Affects | Failure at the extremes |
|---|---|---|---|---|
| Rooms before the boss | 8 | 6 to 10 (owned by System 16's floor table) | Run length | Short: no arc; long: the 30-minute run grows. |
| Discrete decisions per room | about 3 (door, buff hero, item) | 2 to 4 | Confirm fatigue | High: a chore; low: little to decide. |
| Restart time | under 5 s | 2 to 8 s | The "one more run" feel | High: players stop after a loss. |
| Confirms to restart | at most 2 | 1 to 3 | Same | High: friction. |
| Snapshot size limit (worst-case fixture) | 16,384 bytes | 8 to 64 KB | Test speed | Large: slow tests. |
| Abandon confirmation | on | on or off | Accidental loss of a run | Off: a misclick loses 30 minutes. |
| Seed display on the end screen | on, read-only | on or off | Bug reports | Off: harder playtest reports. |

Interactions: the number of rooms (System 16), the decisions per room (Systems 17 and 18) and the playback speed (Systems 29 and 37) together decide the session length; the target is about 3 minutes per room and about 25 to 30 minutes per run (an estimate, to be measured; the concept's session target of 30 to 120 minutes is wider).

## Visual/Audio Requirements

System 5 produces no visuals or sound; it fixes the moments at which the presentation changes.

- **Transitions the presentation must mark:** the fight starts (the arrangement freezes and the tray is cleared), the win (the reward steps), the door row rising lit one card at a time left to right (art bible 7.12), room entry (intents appear), the boss entry, the end of run.
- **The boss entry has no door card.** The art bible gives the wine-shift cue on the door card before the boss room; the MVP has no boss door, so a short "the boss awaits" beat after room 8's reward steps carries it (art director to define).
- **Audio:** no cue is owned here; the cues of the door row, reward shelf and payoff belong to Systems 17, 18 and 29. A non-visual cue is required for rejected commands and for the end of run.
- **State legibility:** a blocked control stays focusable and says why (art bible 7.11); the run status is shown in the world (HP on the heroes, the floor's progress as a corridor of doors), not as a table of stats.

*`art-director` was not consulted for this section; the transitions are those of the art bible's existing state table. Asset specs belong to `/asset-spec`.*

## Game Feel

The run is a rhythm of short, deliberate decisions followed by a fight watched at the player's chosen speed.

- **Few, clear decisions.** About three discrete decisions per room (the door, the buff hero, the item) plus any number of placements; a prompt that has nothing to decide (no grantable buff, an empty offer) never appears.
- **The fight cannot be influenced.** After Start no input changes the result; playback speed and skip only change presentation.
- **A loss is quick to leave.** The end screen shows the cause (the decisive intent) and the near miss (how far, the biggest combo), and one action starts the next run in under about five seconds.
- **No input latency from this system.** Each command costs tens of integer operations; combat itself resolves in one synchronous call, with playback handled by the presentation.

## UI Requirements

Layout and interaction belong to Systems 25 to 31 and their UX specs (`/ux-design`); this system fixes the screens and the commands they must offer. Most "screens" are steps of one persistent planning workbench.

| State | Must show | Commands | Disabled-with-reason |
|---|---|---|---|
| Hero pick | All four heroes with HP, speed, shield, starting and signature items, the innate item in words, the grid shape; pick order numerals | Pick, unpick, Confirm | Confirm until exactly three are picked |
| Arrangement | Grid, stash, tray chip with the loss warning, order strip, intent tabs, forecast | Grid commands, Start | Start while an item is held or a reward step is pending |
| BuffClaim | The exact buff, each hero's locked cells, the suggested hero, any cap shortfall | Confirm the suggested hero (the default path, one Confirm) or choose another hero and Confirm | No skip |
| OfferPending | The reading, the labels, the cards | Select, Confirm (take), Skip | None |
| DoorChoice | The cards (tier, promise, danger, counts, quality word), the popup roster | Arm, Confirm | Confirm while a buff or offer is pending |
| Start with a non-empty tray | A dialog "Leave behind: X, Y?" naming the items, Go back focused by default | Confirm, Go back | None |
| End of run | Outcome, room reached, the cause (decisive intent), the per-room rows, the committed direction, the seed (read-only, copyable) | Restart, menu | None |
| Stopped | A message that the run ended without a result or reward | Return to menu | None |
| Abandoned | The run was abandoned (confirmed) | Return to menu, new run | None |

Leaving mid-run (`AbandonRun`) asks "Abandon run? It cannot be resumed." and returns to the title; it is logged as abandoned, not as a defeat. Accessibility: nothing is timed; tier, danger and lean are never carried by colour alone; the MVP input stub (mouse plus the R key) conflicts with the art bible's keyboard and gamepad parity, so a waiver must be recorded in `design/accessibility-requirements.md` and a focus order authored now (workbench grid, stash and tray, the right-column objects left to right, then Confirm).

## Cross-References

| This GDD says | Defined in | Notes |
|---|---|---|
| Snapshot, streams, fork and commit | `design/gdd/seeded-rng.md` Rule 6 | System 5 holds the states. |
| Freeze, Thaw, Create, Unlock, the item-ID source, the snapshot slice | `design/gdd/grid-backpack.md` Rules 10 to 12 and 19 | The tray is destroyed at Freeze. |
| One synchronous fight, outcomes, technical stop | `design/gdd/combat-resolution.md` | Draw is treated as a loss here. |
| Binding and the preparation record | `design/gdd/enemy-and-intent.md` Rules 7 and 8 | The `intents` state lives in the streams section. |
| Floor, room 1 and the boss | `design/gdd/encounter-generation.md` Rule 2 | System 16 owns the floor table. |
| The row, the buff and the claim | `design/gdd/door-generation-and-tiers.md` Rules 2 to 10 | Its Rule 8 order is corrected here. |
| The offer and the reading | `design/gdd/commitment-and-drop-model.md` Rules 3 and 8 | |
| Party formation, `B_perm`, restoration | `design/gdd/party-and-heroes.md` | |
| Pillars 3, 4 and 5 | `design/gdd/game-concept.md` | Player Fantasy anchors. |
| Reveal, states and door cards | `design/art/art-bible.md` 7.10 to 7.12 | The boss entry has no door card. |

## Acceptance Criteria

Evidence type: **Logic** with integration seams, automated EditMode tests in `Assets/_Project/Tests/EditMode/RunFlow/` (class `RunFlowTests`, assembly `Project05.EditModeTests`), named `[Scenario]_[Expected]`; gate level BLOCKING. UI criteria (the summary screen, the abandon and tray confirmations) are **UI** evidence (retained screenshots in `production/qa/evidence/`) and are BLOCKED on Systems 27, 28 and 31; the Unity-layer lifetime checks are Play Mode evidence. Each row is its own `[TestCase]`; each rejection case has a passing control. Tests use test-local fixtures only.

**Fixtures.** `SCRIPT`: one ticket queue per stream; each call asserts the weights array or bound and fails on a missing call, an extra call or a ticket at or above the total; it counts committed calls and discarded fork calls separately; a non-asserting `COUNT` twin serves the allocation probe. `FakeItemIdSource` logs when `Next()` is called relative to the success of a creation. `FakeGrid` (System 8 port): a scripted view, Freeze, Thaw, Create, Unlock, a tray loss list and a snapshot slice that contains the literal line `CHECKSUM x`. `FakeCombatRunner`: a per-room script of Victory, Defeat, Draw, ActionLimit, NoEligibleActor, setup failure and a duplicate delivery; it records the key it receives. `STUB14/16/17/18`: scripted outputs, with rosters of 1, 2 and 3 enemies, an offer of 3 cards and one of 0 cards, and hero sets where a buff is or is not grantable. A command builder (`Cmd(kind, serial, …)`, scripts such as `FullRun(win8)`). A seed-port fake that returns a queue `[S1, S2]`. `REF-SNAP` (a reference snapshot writer of about 40 lines) and `REF-FNV` are written from the rule text and used as the oracle only for sweeps, never for a criterion with explicit values.

**Rules**

| # | Given / When / Then |
|---|---|
| 1a | **Serial.** `PickParty` with serial 0 is Accepted and the serial becomes 1; the same command with serial 0 again is `Rejected(Stale)` with state and streams unchanged; control: serial 1 is accepted for the next command. Grid pass-through commands, `NoEffect` and `Rejected` do not change the serial. |
| 1b | **Result codes and precedence.** A bad index gives `Rejected(BadIndex)`, a wrong state gives `Rejected(WrongState)`; with a stale serial and a wrong state together the result is `Rejected(Stale)`; a callee `ContractViolation` gives `TechnicalStop` with the diagnostic kept. |
| 2 | **One session.** Two sessions with seeds A and B, commands alternated, each end up byte-equal to its solo run; the catalog hash is unchanged after a run. |
| 3a | **Party and item IDs.** `PickParty` with 3 heroes of (1 innate + 2 starting items) assigns IDs 1 to 9 in slot order, innate first (slot 0: 1, 2, 3; slot 1: 4, 5, 6; slot 2: 7, 8, 9); `nextItemId` becomes 10. |
| 3b | **Party formation draws.** `PickParty` makes 1 call on `doors`, 24 on `intents` (one enemy) and 0 on `drops`; the state is Arrangement(1) with no buff pending; duplicate, unknown or fewer than three hero IDs give `Rejected(InvalidArgument)` with no effect. |
| 4 | **Key.** The fake runner receives the room index as the key for rooms 1 to 9. |
| 5 | **Start gate.** `StartFight` while a reward step is pending gives `Rejected(WrongState)`; control: with none pending it is Accepted. |
| 6a | **Tray loss.** A non-empty tray with `acceptLoss = false` gives `Rejected(NeedsConfirm)` listing the items; the grid is not frozen and the serial is unchanged; control: `acceptLoss = true` is Accepted. |
| 6b | **No draws in the fight.** The fight makes 0 draws (`SCRIPT` fails on any); the outcome is the runner's. |
| 7a | **Won.** A victory in rooms 1 to 8 enters the reward flow; a victory in room 9 gives Ended(Won) with 0 draws on every stream, no offer and no row. |
| 7b | **Lost.** Defeat, Draw, ActionLimit and NoEligibleActor each give Ended(Lost) with a distinct reason and the grid stays frozen. |
| 8a | **Victory transaction.** A victory in room 3 adds 6 calls to `drops` and 6 to `doors`, stores `offer(3)` and `row(4)`, sets `lastResolvedRoom` to 3, and only then thaws; the call order on the fakes is: view snapshot, offer, row, commit, Thaw. |
| 8b | **Atomic.** If the row build throws, no stream advances, nothing is stored, `lastResolvedRoom` is unchanged and the state is Stopped; control: with no throw everything advances. |
| 9a | **Reward steps.** BuffClaim only for rooms 2 to 8 and skipped when no hero can receive anything; the offer is skipped when it has 0 cards; room 1 goes to DoorChoice with neither. |
| 9b | **Blocking.** A pending buff or offer gives `Rejected(WrongState)` for `ChooseDoor` and `StartFight`; grid commands still pass through. |
| 10a | **Choosing a door.** `ChooseDoor(1)` stores the room record of card 1, discards the row, adds `2 × 12 × E` calls on `intents` and enters Arrangement(next); `ChooseDoor(3)` gives `Rejected(BadIndex)` and the row is kept. |
| 10b | **Not idempotent.** A second `ChooseDoor` gives `Rejected(WrongState)`. |
| 11 | **Boss entry.** After room 8's reward steps the room record is the boss spec, `intents` gain `24 × E` calls, and `doors` and `drops` gain 0. |
| 12a | **Abandon.** `AbandonRun` with confirmation in any non-terminal state gives Abandoned(room N, seed) and the `defeats` metric stays 0; in Ended or Stopped it only returns to the menu and logs nothing. |
| 12b | **Restart.** After Lost, `StartNewRun` takes the seed port exactly once (S2); the new session is fresh and the old one is untouched. |
| 13 | **Seed.** A fixed seed is recorded in the snapshot; Restart's seed differs from the previous one; the core has no seed-entry command. |
| 14 | **Draw points.** A scripted full run with 3 enemies per roster makes 43 calls on `doors`, 48 on `drops` and `Σ 24 × E` on `intents` per the roster script; previews, getters and restore make 0 calls; discarded forks are not counted. |
| 15a | **Duplicate outcome.** The outcome of the same room delivered twice through `ApplyOutcome` gives `NoEffect(AlreadyHandled)` the second time, with 0 draws and nothing stored twice. |
| 15b | **Illegal everywhere.** A command legal in no state gives `Rejected(WrongState)`. |
| 16a | **Snapshot format.** The text starts with `GRIMPACK-RUN v1` and LF, has LF only and no byte-order mark, follows the section order of Rule 16, and ends with `CHECKSUM fnv1a64 <16 hex>`; the embedded grid slice is length-prefixed and its own `CHECKSUM x` line is read as data. |
| 16b | **Round trip.** `serialize(restore(text)) = text` byte for byte at every command boundary of a scripted run. |
| 17a | **Corrupt restores.** A bad checksum, a wrong version, `doorHp > B_perm`, an item ID at or above `nextItemId`, a row target different from `room + 1`, a stream position that disagrees with its call count, and a phase-to-record mismatch (one test per row of the matrix) are each rejected and leave the live session byte-unchanged. |
| 17b | **Restore draws nothing.** Restore makes 0 calls and the stored sequences, offer and row are used as stored. |
| 18 | **Item IDs.** With a tray item ID 12 and `nextItemId = 13`: Freeze, Thaw, `TakeOffer` creates ID 13 (not 12); a failed Create does not consume the counter. |
| 19 | **Run summary.** The per-room aggregates are stored by value (a later change of a trace does not alter them); System 5's own fields (tier taken, outcome and reason, committed direction) are testable now. [BLOCKED: Systems 4 and 30 for the trace-derived fields] |
| 20 | **Order and overflow.** Permuting the insertion order of the fake grid's items and links gives an identical snapshot; an overflow of a counter throws (checked context). |

**Formulas**

| # | Given / When / Then |
|---|---|
| 21 | **F1.** The scripted full run totals 43 on `doors`, 48 on `drops` and the roster-script sum on `intents`; the Formulas worked example is reproduced as asserted checkpoints: `intents` 24, 72, 144; `drops` 6, 12, 18; `doors` 7, 13, 19. |
| 22 | **F2.** After a victory in room `w`, `offer(w)` and `row(w + 1)` are stored and `lastResolvedRoom = w`. |
| 23 | **F3.** FNV-1a 64 vectors: `""` → `cbf29ce484222325`, `"a"` → `af63dc4c8601ec8c`, `"foobar"` → `85944171f73967e8`, `"x1"` → `08f10707b58d70c2` (the leading zero catches a missing 16-digit pad); one body hashed by `REF-FNV` and by production gives the same value, and one flipped byte changes it; the multiplication wraps and does not throw. |
| 24 | **F4.** The worst-case fixture snapshot is at most 16,384 bytes. |

**Non-functional**

| # | Criterion |
|---|---|
| 25 | **Engine-free.** The core assembly sets `noEngineReferences`; an IL scan finds no `UnityEngine`, mutable `static` field, `float`, `double`, `DateTime`, `Random`, `Guid`, `GetHashCode` or thread use. The core assembly name is NOT SOURCEABLE until the architecture ADR. |
| 26 | **Replay and restore-and-continue.** A fixed seed plus a command script gives an identical snapshot at every boundary; restoring at every boundary and continuing gives the same remaining snapshots as the uninterrupted run. |
| 27 | **Allocation.** Over 1,000 warmed-up commands on the `COUNT` twin the managed allocation delta is 0, with an allocating control loop proving the probe works. |
| 28 | **Operation budget (counted by a seam, not timed).** At most 12 draws on `doors`, 6 on `drops` and `24 × E` on `intents` per command; one pass over the frozen view per victory. Elapsed time is logged only. |
| 29 | **UI (retained screenshots) and Play Mode.** The end screen shows the outcome, the room reached, the cause and the read-only seed; the abandon and tray-loss confirmations show; entering and exiting Play twice shows no residual statics or duplicate subscriptions. [BLOCKED: Systems 27, 28 and 31] |

**Blocked criteria by system.** System 1 (the ADR): the real streams, fork and commit, stream consistency, the assembly name. System 2: the catalog and the versions. System 4: the trace (19). System 8: real Freeze, Thaw, Create, Unlock and the snapshot slice (the fakes hold the logic until then). System 12: setup failure and outcomes. Systems 14 to 18: the real builders. System 24: nothing needed here.

**Watched failures.** Each mutant must make a named criterion fail: a stale serial accepted or an off-by-one on the serial (1a); a non-atomic victory transaction (8b); thaw before building the offer and row (8a); a row or offer built from the live grid (8a); Freeze before the confirmation check (6a); tray loss without confirmation (6a); `nextItemId` recomputed from the largest ID or `Next()` called before a failing creation (18); restore that redraws (17b) or swaps in pieces and leaves half a state (17a); an abandon counted as a defeat (12a); a Draw counted as a win, or ActionLimit as a victory (7b); a boss victory that draws (7a); Restart that reuses the seed (12b); a static counter shared between sessions (2); dictionary or hash ordering in the snapshot (20); a checked context on the FNV multiplication (23); a missing 16-digit pad (23); a draw in a preview (14).

> *`qa-lead` consulted (2026-11-13). Its recomputation confirmed Formula 1, the worked example and the FNV vector of the empty body; its sixteen ambiguities are resolved in the Detailed Design above.*

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| System 31 needs one coherent return/abandon contract: Rule 1 allows `StartNewRun` in Stopped, while the state table and unexpected-exception edge allow only `ReturnToMenu`; `QuitToMenu` is also stated legal in every state although active-run exit needs confirmed `AbandonRun`. Resolve command names, legal states and playback outcome handoff without bypassing confirmation. | Systems 5/6/12/29/31 | Before menu/run command integration. |
| Does a Restart require only a fresh seed-port request, or must the numeric seed be different? AC 13 says different, but a fresh 64-bit entropy draw may equal the prior value; seed-source failure must preserve the old session. | Systems 1/5/31 | Before Restart implementation and AC reconciliation. |
| How is the tray list accepted by the player bound atomically to `StartFight(acceptLoss=true)` when grid commands do not advance the flow serial? System 25 must not confirm one list and lose unconfirmed items from a changed list. | Systems 5 and 25 | Before tray-loss confirmation integration. |
| How does System 25 retain the pre-fight frozen arrangement for trace playback when a victory may have thawed the live grid before playback starts? | Systems 5, 25 and 29 | Before playback integration. |
| System 30 proposes retaining the latest completed fight's trace, baseline and frozen arrangement until the next accepted `StartFight` or run exit; a rejected start must not release it. How are ownership, atomic replacement and the review handle defined, including an in-memory restore when Rule 16's snapshot excludes the event log? If no matching retained fight is available, the review must not claim an old trace belongs to the restored state. | Systems 4, 5, 25, 29 and 30 | Before last-fight review and restore integration. |
| The player chose record statistics that persist across game launches. Which system owns, saves, loads, updates and resets these records independently of the current run snapshot, and how are incomplete or invalid fight results excluded? This decision does not itself add persistent full-fight history or run resume. | Systems 5, 24, 30, 31 and 35; architecture | Before implementing durable records. |
| **Who calls Freeze and Thaw.** This GDD gives them to System 5 (Freeze at `StartFight`, Thaw after the victory transaction), with System 12 delivering the by-value frozen view; System 8 (Rules 11 and 12 and its Interactions) and System 12 (Rule 2) give them to System 12. A ruling and amendments are needed, which also closes System 12's open question on the terminal hand-off. | Systems 5, 8 and 12 | Sync after this GDD. |
| **Playback after the thaw.** The fight resolves synchronously, the grid is thawed at the victory transaction and the presentation plays the trace afterwards; Rule 6 makes the gate binding (no reward state and no flow command except abandon until playback ends or is skipped); whether to add an explicit `ContinueFromPlayback` command is open. | Systems 5, 29 and 31 | System 29's GDD. |
| **Item-ID order.** This GDD orders IDs per slot, innate item first and then the starting items; System 15 Rule 4 does not specify it. | Systems 5 and 15 | Sync after this GDD. |
| **Stored specs and catalog changes.** System 16 says stored specs stay valid when the catalog changes; this GDD and System 17 reject a restore on a version or fingerprint mismatch. | Systems 5, 16 and 24 | System 24's GDD. |
| **Retry.** The "if System 5 offers one" lines of Systems 14, 16 and 17 are closed here: there is no retry; a resume (System 24) restores the stored record. | Systems 14, 16 and 17 | Sync after this GDD. |
| **The architecture ADR.** The engine-free assembly name and layout (two proposals exist), the RNG algorithm and vectors, the seed port, and the persistence of a debug snapshot are undecided; the engine facts not covered by `docs/engine-reference` (persistent paths, file encoding, the operating-system entropy API) are NOT SOURCEABLE and are checked in the first spike. The UI toolkit choice (UI Toolkit or UGUI) is also open. | technical-director, lead-programmer | Architecture ADR before implementation. |
| **`.gitattributes`.** None exists and `core.autocrlf` is true, so Grid Backpack's criterion that golden files use LF "enforced by `.gitattributes`" is not true yet. | devops-engineer, lead-programmer | Before the first golden file. |
| **Run summary fields** (peak hit, actions used, longest chain, biggest ignition) depend on System 4's trace schema and System 30's breakdown. The player chose longest chain as the count of effects that actually mutate state, not maximum depth or ordinal; confirm completed-root and unique-effect identity. Largest hit also needs an occurrence key distinguishing repeated hits on one target. The decisive-intent line of a defeat is MVP-required, because the go/no-go criterion "players can say why they lost" rests on it; use an enemy intent only when the recorded cause supports it. | Systems 3, 4, 12, 29 and 30 | Before run-summary and breakdown integration. |
| **A Draw counts as a loss;** System 12 defines the outcome but not the hand-off, and Party Rule 9 says the run ends only on a defeat. | Systems 12 and 15 | Sync after this GDD. |
| **Does a skipped action count toward the 200-action limit?** | System 12 | System 12 revision. |
| **Input waiver (superseded by [input-and-focus.md](input-and-focus.md), 2026-11-13: the MVP is mouse and keyboard in full with a gamepad contract only).** The earlier MVP input stub (mouse plus the R key) conflicted with the art bible's keyboard and gamepad parity; a waiver and a focus order belong in `design/accessibility-requirements.md`, which does not exist yet. | ux-designer, accessibility-specialist | Before the UX specs. |
| **Session length.** One floor of nine rooms is estimated at 25 to 30 minutes (35 to 45 on a first run), below the concept's 30 to 60 minutes per run and 30 to 120 minutes per session; the concept's wording is updated, and above about 40 minutes the lack of resume stops being acceptable, so it is measured. | game-designer, System 35 | First playtests. |
| **The "boss awaits" beat** that replaces the boss door card (art bible), and the concept's three fixed heroes against System 15's pick of three from four. | art-director, System 15 | Art bible and concept follow-up. |
| **System 17 Rule 8** ("then rearranges, then chooses the next door") is corrected by Rule 5 and Rule 9 here; System 18's state "Spent when the next encounter starts" becomes "Spent when taken or skipped". | Systems 17 and 18 | Sync after this GDD. |
| System 19 adds a curse list to the snapshot (at most 2 entries, a section and a format bump), a curse in the claim step (`HeroHasCurse`, `NeedsSpace`, atomic `Unlock`, `CreatePinned`, then the additions), restore checks for the list and the pinned item, and a row-build mask that counts the pending curse ([curses-and-trade-offs.md](curses-and-trade-offs.md)). | Systems 5 and 19 | Before the Vertical Slice. |
| Answered by System 29 ([payoff-presentation.md](payoff-presentation.md), 2026-11-14): the by-value frozen arrangement (placement baseline, item instances, origins and the adjacency links) travels in the encounter record System 12 hands to System 5 with the trace; System 25 binds to it during playback and never reads the live grid; System 5 releases the record after the playback gate and the breakdown; System 29 sends one idempotent `PlaybackFinished(encounterId)` (natural end or the final Skip leg; a Skip stop never sends it) and `ViewChanged(t)`; a technical stop ends in `Stopped` with no outcome. | Systems 5, 12, 25 and 29 | Sync in System 5's next revision. |
| Required by System 35 ([balance-simulation-harness.md](balance-simulation-harness.md), 2026-11-14): a read-only, by-value `RunView` for policies (the phase, the legal-command list from the machine's own legality query, the player-visible fields, `Preview` and `Forecast` handles), a typed immutable `RunConfig` (knobs and catalog) with a catalog-snapshot export, a `FightSummary` per fight, and a harness-supplied `runId`; the harness drives the same command facade and never `Restore` from a policy. | Systems 5 and 35 | Before the combat core spike. |

> *Specialists consulted for these questions: systems-designer, gameplay-programmer, engine-programmer, game-designer, ux-designer, creative-director, qa-lead (2026-11-13).*
