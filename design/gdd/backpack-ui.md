# Backpack UI

> **Status**: Designed — pending independent design review
> **Author**: User and design team
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillars**: 4 — Arrangement Is the Thinking; 2 — The Payoff Is the Product
> **Creative Director Review (CD-GDD-ALIGN)**: REVISED 2026-11-13 — cross-hero readability and loose-layer validation requirements added after CONCERNS; visual evidence not assessed.

## Summary

Backpack UI is the workbench where the player arranges items across three heroes, inspects the connections and expected order, and decides when the party is ready to fight. It presents loose items falling onto a layer, preserves the model's explicit loss confirmation, and shows the arrangement's recorded causes during combat playback. It relies on the owning systems for placement, links, order and run flow; input, cross-hero preview and confirmation atomicity still need their named contracts resolved.

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Grid Backpack (8), Adjacency and Synergy (10), Item Trigger and Cooldown (9), Input and Focus (6)`

## Overview

Backpack UI is the player's workbench for arranging items across three heroes before combat. It presents one hero's grid at a time, offers access to the shared stash and items awaiting storage, and lets the player inspect and change placements while seeing connections, firing information and bound enemy intentions when available. The lower workbench area can show either the stash or the party overview. The UI translates player input into requests to the owning game systems and presents their results; it does not decide placement validity, calculate synergies, resolve combat, or change the run on its own. During combat the workbench displays the frozen arrangement and supports the presentation of recorded causes without turning the fight into an active-control phase.

System 6, Input and Focus, has not yet been designed; input and focus interfaces in this GDD are provisional until reconciled with it. Layout and interaction details will be elaborated in the later screen-specific UX specification. The Unity UI stack is an architecture decision and is not selected here.

## Player Fantasy

The player is the architect of a machine built from three heroes: place a piece, see which real connections it changes, and read how the party is expected to work together before committing to the fight. Switching heroes while holding an item keeps the thought intact. A failed placement returns the item safely and explains what prevented it, so experimentation feels deliberate rather than punishing.

The forecast is a forecast, not a promise; the enemy's bound intentions and the party's order remain visible while the player arranges against a selected room. Before the next room is chosen, the UI does not present the previous room's intent as the next one. When the resulting chain plays out, the player can trace it back to the pieces they arranged. This serves Pillar 4's visible pre-combat decisions and Pillar 2's rule that a clear cause takes precedence over a larger effect. Creative-director consultation recommended this framing; the exact screen layout remains for UX design.

## Detailed Design

### Core Rules

1. **Source of truth.** System 8 owns containers, item positions, placement validity, rotation, undo, and the tray. System 25 renders its read-only arrangement view and sends requests; a held item and its ghost are presentation state, not a model move. System 10 owns adjacency; Systems 9 and 11 own the order and forecast; System 5 owns fight start and Freeze/Thaw.
2. **Workbench.** Show one hero's grid at a time, with a control for each of the three party slots. The lower workbench switches between Stash and Overview without requiring the player to hold the control. In Overview, the party ribbon lists each hero's items and effective cooldowns where applicable, with passive items after a separator; passive and CombatStart items have no cooldown number. First-firing ranks use System 9's separate forecast rules; passive and reaction items, and items whose first firing is beyond the 200-action limit, have no party rank. `Forecast` itself lists cooldown firings only, and must not be treated as a complete list of all trigger kinds. In Arrangement, the bound enemy intent and order strip remain visible. Before the next room is selected and its intents bound, indicate that intent is not yet available rather than showing the previous room's intent as if it were the next one.
3. **Holding and placing.** Pick up an item from a grid, stash or tray without changing its source. While held, the player may switch the visible hero without dropping the item, and may rotate where rotation is enabled. On a free valid target, submit the placement to System 8; show the committed result only after success. Cancel, a completed drag released outside a grid, or an invalid drop leaves the model unchanged and returns the item to its source. In click-to-pick mode, releasing the first click does not drop the item; the next placement action or cancel ends Holding. Occupied cells never swap or displace another item; identify the blockers returned by System 8.
4. **Preview.** Ask System 8 for placement validity and System 10 for synergy preview as the snapped origin, orientation, target hero/container or underlying arrangement changes. Render actual links and the returned gained/lost information, with textual causes and totals. Invalid candidates, `Frozen`, or `BufferTooSmall` have no partial synergy preview: clear stale preview instead of showing zero links as fact. A buffer error is a technical failure, not a player placement error. System 10's cross-hero transfer contract does not yet specify how source losses and destination gains are both returned; do not claim a complete transfer preview until that interface is resolved.
5. **Remove, stash and undo.** Remove places an item in the ordered reward tray. Send to stash requests System 8's deterministic auto-placement and reports `NoPlacement` without changing the item or clearing undo. Expose one-step Undo according to System 8; a successful auto-placement is not undoable and clears the record. Do not offer undo as a way to restore a discarded tray item.
6. **Loose-item layer and loss.** The player sees waiting items as loose objects that fall downward onto a layer in the workbench, following the user's video reference (Backpack Battles clip, 2026-11-13). The layer remains below protected interface content. This is the presentation of System 8's ordered, non-grid reward tray, not a new storage container or a physical simulation that changes gameplay. Keep each item identifiable and retrievable; layout, overflow and overlap handling require UX validation before implementing this screen. Keep a visible count and warning whenever waiting items exist, including in Overview. A nonempty tray at Start causes System 5 to return `NeedsConfirm` with the items to leave behind; present the names with Go back as the default. Only explicit confirmation sends `StartFight(acceptLoss=true)`. System 5/8 performs the loss; the UI never destroys the items itself.
7. **Start and playback.** Block Start while holding an item or while a reward step is pending. The grid remains editable from Thaw to Freeze, including BuffClaim, OfferPending and DoorChoice; Start is legal only after pending rewards are resolved and the next room has bound intents. During trace playback, show the retained pre-fight frozen view and do not send gameplay commands; the flow's `AbandonRun` exception remains available. After victory the authoritative grid can already be thawed before playback begins, so the UI gate applies even when that live model is Editable. Playback cannot change the already-resolved outcome.
8. **Order and causality.** Plate numerals in the grid show the per-grid reading/firing order at the item's anchor only in Overview or on focus. Distinguish the separate party-wide first-firing forecast, label it as a forecast, and state that it assumes no stun or chain limit and omits reactions. Replace the forecast with actual waits in combat; use recorded events to show actual firings and applied modifiers. A bridge never pulses merely because two items are adjacent.
9. **Inputs and access.** Mouse interaction and the R key are the indexed MVP input floor. The art bible additionally specifies click-to-pick/click-to-place as an alternative to dragging and a mouse-accessible rotation control; retain these as design requirements whose mapping and feasibility must be reconciled with the undesigned System 6. Full remapping, focus behavior, gamepad and reduced-motion settings depend on the respective input, settings and accessibility designs; do not claim those contracts exist yet.
10. **Playback grid.** During combat, the workbench follows the acting hero at the start of that hero's Ready phase; no manual hero switch in the MVP. In a Skip fast-forward leg no follow occurs; a Skip Forced span (a Detonation played in full) follows the acting hero again (System 29 Rule 11). After combat it returns to the hero selected before combat. Presentation follows System 29 and the trace and does not drive simulation.

### States and Transitions

These are independent dimensions, not a single state machine: legal actions are the intersection of their guards.

| Dimension | State | Transition and guard |
|---|---|---|
| Arrangement | Editable / Frozen | System 5 freezes on accepted Start and thaws after an eligible victory; only Editable accepts placement requests. |
| Item interaction | Idle / Holding | Pick enters Holding without mutation; successful drop, invalid drop, cancel or completed drag released outside returns to Idle. In click mode the release after pick preserves Holding. Hero switch preserves Holding. Start is blocked in Holding. |
| Lower workbench | Stash / Overview | Toggle between views; picking an item returns the lower section to Stash while keeping the grid lens. Tray count/warning remains visible. |
| Start confirmation | Closed / Awaiting confirmation | `NeedsConfirm` opens the list with Go back focused and UI grid edits blocked; Go back closes without loss. If an authoritative tray change is observed, cancel the old confirmation and require a new one. The atomicity of check and resubmission remains an open System 5 interface question. |
| Combat presentation | Planning / Playback / After playback | Playback renders the retained pre-fight frozen view, including when victory already thawed the live model; input cannot change the outcome. Return the workbench to the pre-fight hero after combat. |

### Interactions with Other Systems

| Owner | Input to System 25 | Request or output from System 25 |
|---|---|---|
| 8 Grid Backpack | Arrangement, tray, preview result, blocker set, undo/result codes | Placement, remove, auto-place, undo and pure preview requests. |
| 10 Adjacency | Current and preview links, modifier totals and causes | Candidate preview request; cross-hero delta interface remains open. |
| 9 Item Trigger and 11 Turn Order | Reading-order data, cooldowns and first-firing forecast with assumptions | Rendered numerals, ribbon and forecast; no local simulation. |
| 5 Run State and Flow | Editable/frozen state, bound intents, `NeedsConfirm`, playback gate | Start with or without explicit loss acceptance; no direct Freeze/Thaw. |
| 6 Input and Focus | Input/focus events and mappings — provisional, GDD not yet written | Interaction intent; concrete key/focus contract awaits System 6. |
| 26 Hover Popup, 27 Combat HUD, 29 Payoff, 30 Breakdown | Explanations, intent/order presentation, recorded causes and source highlights | Selected item/hero and stable item identity; the corresponding systems own their presentation. |

**Known documentation conflict:** System 8's stale Freeze/Thaw wording was synchronized to System 5 during this design; parts of System 10 still attribute those calls to System 12. This GDD follows System 5 pending System 10 synchronization and independent review.

## Formulas

System 25 defines no gameplay formula. It displays the owning systems' results without independently recomputing them: System 8 Formulas 1–5 own footprint, placement, anchors and blockers; System 10 Formulas 4–5 own the link seam and per-grid gained/lost sets; System 9 Formulas 2, 4–5 and 7 own cooldown, order, first-firing forecast and actual wait. The 360-link bound belongs to one 6×6 hero grid under System 10's existing assumptions; it is not a proven aggregate bound for a cross-hero preview. Tray count is the length of System 8's current tray list and is not a capacity limit.

## Edge Cases

- **If an item is held from the tray:** it remains in the tray until a successful placement. Start is blocked; cancel or outside release preserves the item and tray entry.
- **If a moved item overlaps its own old cells:** use System 8's result; its own cells do not by themselves make a placement occupied. If it overlaps another item, show `Occupied` and the returned blocking items in their supplied order.
- **If the hero changes while holding:** preserve the held item and orientation, discard the old target's preview and query the new target. Never reuse the previous hero's ghost or deltas.
- **If a preview is invalid, Frozen or its buffer too small:** remove the previous ghost and deltas. For an ordinary invalid candidate draw the current invalid ghost and blocker crosses with its reason; for Frozen or a buffer failure show an unavailable state without partial synergy data. Do not substitute an empty valid preview.
- **If a link's identity is unchanged but its seam moves:** gained and lost may both be empty; draw the returned new seam rather than preserving the old bridge.
- **If a valid preview becomes stale before commit:** use the actual placement result and authoritative view; a rejected commit does not move the item. Clear the stale preview and explain the returned reason.
- **If rotation is disabled:** keep the valid orientation and explain that rotation is unavailable; do not change the model.
- **If send to stash returns `NoPlacement`, or Undo returns `NothingToUndo`:** leave the model unchanged and expose the result without fabricating a success. A rejected operation retains the existing undo record.
- **If a tray confirmation is open:** UI grid edits are disabled. An observed authoritative change invalidates the displayed list and requires a new confirmation; serialization between the final check and `StartFight` still requires an owning-system contract.
- **If playback is active:** no grid command changes gameplay even when the authoritative grid already thawed after victory; only the run flow's stated Abandon exception may be sent. The retained frozen view cannot be edited.
- **If a Vertical Slice item is pinned by a curse:** do not imply MVP already supports pinning; System 19 requires the later System 8 contract and UI must present its rejection once implemented.

## Dependencies

| System | Direction and contract | Status / risk |
|---|---|---|
| 8 Grid Backpack | Hard upstream: arrangement, tray, placement/preview, rotation, undo and results. | In Review; Freeze/Thaw wording synchronized to System 5, awaiting independent review. |
| 10 Adjacency and Synergy | Hard upstream: pure preview, link seams, gained/lost links and modifier totals. | Designed, pending review; cross-hero preview coverage unresolved. |
| 9 Item Trigger and Cooldown; 11 Turn Order | Hard upstream for item order and party forecast. | Existing GDDs require independent review; forecast is not a guarantee. |
| 6 Input and Focus | Hard upstream: interaction mapping, cancellation and focus. | Not Started; no final focus, remapping or input contract yet. |
| 5 Run State and Flow | Hard upstream: Editable/Frozen, intent readiness, Start and confirmation. | Designed, pending review; confirmation atomicity requires clarification. |
| 2 Game Data; 7 Localization; 37 Settings | Item definitions, display strings and playback/accessibility settings. | System 2 is Designed, pending review; Systems 7 and 37 are not designed. Do not hard-code language or assume unapproved settings. |
| 4 Event Log; 12 Combat Resolution; 29 Payoff Presentation | Recorded causes and playback; System 29 consumes the workbench as a presentation surface. | 4 and 12 need re-review; 29 not designed. Combat is never controlled here. |
| 26 Hover Popup; 27 Combat HUD; 30 Post-combat Breakdown | Downstream presentation: item explanation, intent/order, source highlights. | 26 Designed pending review; 27 and 30 Not Started. Use provisional selection and identity interfaces. |
| Art Bible and later arrangement UX spec | Visual and interaction constraints; screen layout and evidence. | Art Bible is Draft/REVISED, not independently re-reviewed; arrangement UX and accessibility specifications do not yet exist. |

The index lists hard dependencies 8, 10, 9 and 6. System 5 is additionally necessary for the specified Start/Freeze and loss-confirmation interface. Back-references and the stale Freeze/Thaw wording are candidates for synchronization after this design; no upstream verdict is assumed.

## Tuning Knobs

No new gameplay balance knob is owned by this UI. Hero-grid geometry, the 5×5 stash, item orientation, actual link rules, cooldowns and forecasts remain owned by Systems 8, 10 and 9. The loose layer has **no gameplay capacity cap**: representation and overflow must allow all tray entries to be found and selected. Art Bible timings for feedback and hero switching are presentation targets, not measured budgets; change them only in coordination with the art and UX owners. The missing performance target for the whole interface is an open technical decision, not System 10's provisional 0.2 ms per query.

## Visual/Audio Requirements

- Keep the calm cut-stone-on-dark-felt workbench and the protected HP, order and intent layer. The falling loose items remain below that protected layer; the motion cannot hide their eventual identity or make an item inaccessible. Do not introduce a gameplay physics system to realize the fall.
- Use System 8's held, valid and invalid ghost states: invalid is a desaturated outline and crosses at blocking cells, with text naming the cause; locked cells use hatch and lock pip. No state is conveyed by color alone.
- Render only real System 10 links as bridges at returned seams. Resting bridges are static; a candidate/focused link uses the preview state; a trigger bridge reflects a recorded modifier application. Reduced-motion presentation replaces pulses or shudders with static/fade alternatives per the Art Bible.
- System 8 already specifies a soft lift tick, dry placement click, muted invalid-release thud and low tone after confirmed tray loss; hovering is silent. Audio ownership and asset choices remain for downstream audio work.
- The plate rotates but its icon stays upright. Avoid unverified pixel dimensions and clip neither the held object nor protected information. Exact layout must reconcile the Art Bible's conflicting workbench widths and obsolete eight-wide stash with System 8's 5×5 stash.

## Game Feel

- Planning is calm, deliberate and untimed. Picking up, moving, rotating and previewing should feel immediate: the existing System 8 target is within one frame of crossing a cell boundary; this is a target to measure, not evidence already obtained.
- Objects waiting for storage fall down onto the loose-item layer as in the supplied reference. The motion communicates that a piece is waiting; it must not decide ownership, block a later pickup or make the presentation depend on frame-rate-sensitive gameplay physics. Support reduced-motion alternatives.
- Use the Art Bible's existing presentation targets: a 100 ms hero-grid transition; bridge preview 100 ms in and 80 ms out; successful placement's 83 ms slide, 33 ms thunk and 150 ms edge pulse. Respect its reduced-motion treatment. Validate on the actual target rather than assuming these targets are met.

## UI Requirements

- Show the active hero, one grid, access to the 5×5 shared stash and the loose-item layer; switching hero while holding preserves the item. Overview is a toggle, not hold-only, and provides the party ribbon without hiding the persistent loose-item count/warning.
- Show bound enemy intentions and the order strip in Arrangement; indicate unavailable next-room intent in the intervening post-victory phases. Show per-grid plate order only on focus or with the grid lens; label the distinct party-wide first-firing forecast and its assumptions. Popups and full item explanations belong to System 26.
- When an Overview item receives focus, highlight its actual partners in the active grid and party ribbon where the underlying relationship applies. System 10 adjacency never crosses hero grids; cross-hero effect relationships must come from their owning effects and System 26, whose compact popup identifies affected heroes by join numeral. Their data contract remains open; do not manufacture cross-hero adjacency.
- Show placement validity by shape, text and contrast as well as color. Preserve item identity during movement and on rejected placement. A Start confirmation names every item that would be left behind and defaults to Go back.
- Support the Art Bible's click-to-pick/click-to-place and drag interaction requirements and an accessible mouse alternative for rotate; exact bindings, cancellation, focus and remapping must be agreed with System 6. The MVP baseline in the index is mouse plus R; wider platform support is not silently claimed as implemented.
- The arrangement UX specification must test the loose layer at high item counts, item overlap/retrieval, localization expansion, one-grid switching, protected intent/order, and screen sizes. The Art Bible font, Vietnamese rendering and contrast checks remain unverified; no visual evidence is claimed here.

## Cross-References

| Document | Contract |
|---|---|
| [Grid Backpack](grid-backpack.md) | Containers, geometry, tray, results, input and visual expectations. |
| [Adjacency and Synergy](adjacency-and-synergy.md) | Preview and bridge seams, including gained/lost links. |
| [Item Trigger and Cooldown](item-trigger-and-cooldown.md); [Turn Order](turn-order.md) | Per-grid order, forecast, actual waits and party order. |
| [Run State and Flow](run-state-and-flow.md) | Editable interval, bound intents, Start, `NeedsConfirm` and playback gate. |
| [Game Concept](game-concept.md); [Art Bible](../art/art-bible.md) | Pillars 2 and 4, workbench, visual states, motion and accessibility targets. |
| [Systems Index](systems-index.md) | MVP scope, dependencies and the weeks 5–6 combat-core checkpoint before UI implementation. |

## Acceptance Criteria

The following are design criteria, not results of tests run in this session. Criteria marked **BLOCKED** depend on an unresolved owner contract; **NOT ASSESSED** needs later visual or performance evidence. No UI-owned gameplay formula exists: verify owner results are rendered unchanged.

1. **GIVEN** an item in a grid, stash or tray, **WHEN** it is picked up and previewed, **THEN** System 8's snapshot is unchanged; a successful `Place` updates the authoritative arrangement exactly once.
2. **GIVEN** three heroes in Arrangement with bound intent, **WHEN** Stash and Overview are toggled and a hero is selected, **THEN** one hero grid is visible, the party ribbon covers all three in Overview, and the order strip and bound intent remain visible without a held toggle. **GIVEN** DoorChoice before a door is chosen, **THEN** no prior-room or unbound intent is presented as the next fight's intent.
3. **GIVEN** a held item from hero A, **WHEN** the player changes to hero B and drops at a valid target, **THEN** the same instance and selected orientation remain held through the switch and System 8 commits the move to B.
4. **GIVEN** a held item, **WHEN** the player cancels, releases outside a grid, or releases on an occupied or otherwise invalid target, **THEN** the original arrangement remains unchanged; an occupied target names the returned blockers and does not swap items.
5. **GIVEN** a held item, **WHEN** its snapped origin, orientation, target or underlying arrangement changes, **THEN** the previous preview is invalidated and the new valid response supplies the shown seams, changes and totals.
6. **GIVEN** a successful preview, **WHEN** the next response is `Occupied`, `LockedCell`, `Frozen` or `BufferTooSmall`, **THEN** no stale or partial link delta is shown; ordinary invalid targets show the current invalid ghost and reason, while buffer failure is identified as technical unavailability.
7. **GIVEN** a System 10 fixture where a link's seam changes but its identity does not, **WHEN** the candidate moves, **THEN** the bridge moves to the returned seam and no gained/lost link is invented.
8. **BLOCKED — cross-hero preview. GIVEN** a move from hero A to B, **WHEN** previewed, **THEN** both source link losses and destination gains are correctly presented. System 10 must first define the combined result and buffer contract.
9. **GIVEN** a successful Remove, **WHEN** Undo is used, **THEN** System 8 restores the exact prior item placement and removes the newly assigned tray entry. **GIVEN** a successful Place from the tray, **WHEN** Undo is used, **THEN** the item returns with its previous tray-entry number.
10. **GIVEN** a full stash and an existing undo record, **WHEN** auto-placement returns `NoPlacement`, **THEN** the arrangement and undo record remain unchanged. **GIVEN** a successful auto-placement, **THEN** the old undo record is cleared and the auto-placement cannot be undone.
11. **GIVEN** a newly received or removed item, **WHEN** it enters the loose layer, **THEN** its representation falls down, remains associated with the same System 8 instance and can be picked up; holding it alone does not remove it from the tray.
12. **NOT ASSESSED — layout. GIVEN** a tray with more items than fit visibly, **WHEN** the player seeks every entry in Stash and Overview, **THEN** all are identifiable and retrievable and the count and loss warning remain visible without obscuring protected information. The UX spec must define the stress fixture and produce retained evidence.
13. **GIVEN** a nonempty tray and no held item, **WHEN** Start is requested without acceptance, **THEN** `NeedsConfirm` lists the items, no item is lost, Go back is the default and UI edits are blocked. **WHEN** Go back is chosen, **THEN** the arrangement remains unchanged; only explicit confirmation requests acceptance.
14. **BLOCKED — confirmation atomicity. GIVEN** the tray changes while confirmation is shown or between final verification and Start, **WHEN** the player confirms, **THEN** no item absent from the explicitly accepted list is lost. System 5 must first specify the atomic/versioned contract; observing a change in the UI invalidates the old dialog in the meantime.
15. **GIVEN** a held item or pending reward, **WHEN** Start is attempted, **THEN** it does not start. **GIVEN** an editable BuffClaim, OfferPending or DoorChoice state, **WHEN** a valid grid edit is requested, **THEN** System 8 can accept it.
16. **BLOCKED — playback snapshot interface. GIVEN** a victory whose live model has already thawed, **WHEN** the trace is playing and the player attempts a grid edit, **THEN** the UI renders the retained pre-fight view and sends no gameplay edit. Repeat for defeat with the live grid Frozen; the flow's Abandon exception remains available in both. The owning systems must specify how the UI receives and retains that view.
17. **GIVEN** the lens is off and a plate is unfocused, **WHEN** viewed in planning, **THEN** its grid-order numeral is hidden. On focus or with the lens enabled it appears at the anchor; the separate party forecast is labelled with its no-stun, no-chain-limit and no-reactions assumptions, and actual waits replace it in combat.
18. **GIVEN** two touching items without a valid link, **WHEN** rendered, **THEN** there is no bridge. **GIVEN** a valid link, **WHEN** no recorded modifier application occurs, **THEN** it does not pulse as a trigger; an actual recorded application shows the matching cause.
19. **GIVEN** both click-to-pick/click-to-place and drag paths, **WHEN** each moves the same item, **THEN** both obey the same held and commit rules; releasing the initial pick click keeps the item held until a subsequent place or cancel action. Rotation and cancellation follow the resolved System 6 input contract; that integration remains provisional.
20. **GIVEN** hero A was selected before the fight and hero B enters Ready during normal playback, **WHEN** that trace step is shown, **THEN** the workbench follows B; a Skip fast-forward leg does not follow (a Forced span does), manual switching is disabled during combat, and the workbench returns to A after combat.
21. **NOT ASSESSED — visual and responsiveness evidence. GIVEN** valid, locked and occupied fixtures at the target resolution, **WHEN** recorded with a fixed-tick capture, **THEN** retained evidence shows non-color-only states, named blockers, loose-item count and confirmation. Measure the one-frame preview target, reduced-motion behavior, contrast and font rendering separately; do not mark any as passed without evidence.
22. **BLOCKED — party relationship presentation. GIVEN** an Overview item with an actual local link and an effect affecting another hero, **WHEN** the item is focused, **THEN** applicable partners are highlighted in the grid and party ribbon, and the compact popup identifies affected hero join numerals. Cross-hero effects need the owning effects/System 26 data contract; adjacency itself remains within one grid.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| How does a cross-hero preview return both source losses and destination gains without partial or misleading feedback? What buffers does it require? | Systems 10 and 25 | Before integrating cross-hero preview. |
| How are the displayed loss list and `StartFight(acceptLoss=true)` serialized so a changed tray cannot be accepted against an old confirmation? Grid edits do not increment System 5's serial. | Systems 5 and 25 | Before implementing tray-loss confirmation. |
| How does playback obtain and retain the frozen arrangement when System 5 may already thaw the live grid after victory? | Systems 5, 25 and 29 | Before playback integration. |
| How do loose items remain individually discoverable and selectable when they overlap or outgrow the visible layer? Validate the falling treatment from the user's clip without turning the ordered tray into gameplay physics. | UX designer, art director | Arrangement UX spec and visual prototype. |
| What exact cancellation, focus, binding and remapping contract supports both drag and click-to-place? | System 6, UX designer | Before System 25 implementation. |
| What UI stack and integer-pixel strategy work with the pinned Unity version? | Technical director | UI architecture decision and engine spike. |
| Can the workbench, 5×5 stash and loose layer fit without obscuring intent, order or the grid at target sizes? Art Bible column widths and its older stash assumption conflict. | Art director, UX designer | Art Bible revision and retained layout evidence. |
| Which font, contrast, text expansion, reduced-motion and audio choices meet the final accessibility standard? | Art director, UX designer, Systems 7/37 | Accessibility and arrangement UX review. |
| How are cross-hero effect targets exposed to the party ribbon and System 26 without treating them as adjacency links? | Owning effect systems, Systems 25/26 | Before party-relationship presentation implementation. |
| Are cross-hero causal highlights and the acting-hero follow behavior legible at each playback speed, including Skip? | Systems 25 and 29, UX designer | Payoff UX and visual playback test. |
| How will pinned-item rejection be surfaced in the Vertical Slice after the System 8 contract is extended? | Systems 8, 19 and 25 | Before Vertical Slice implementation. |
| Answered by System 6 ([input-and-focus.md](input-and-focus.md), 2026-11-13): click-hold and drag share one held state, Esc and a drag released off every grid cancel, the right click rotates while holding, the keyboard uses a cell cursor with jumps, and remapping is by action with a binding map; reconcile the provisional wording of this GDD. | Systems 6 and 25 | Next revision of this GDD. |
| Answered by System 29 ([payoff-presentation.md](payoff-presentation.md), 2026-11-14): during playback the workbench binds to `IPlaybackView.FrozenArrangement` from the encounter record (placement baseline, items, origins, adjacency links), never to the live grid; the follow starts at the acting hero's Ready phase, none during a Skip fast-forward leg (it follows again in a Forced span), and the workbench returns to the pre-fight hero afterwards. | Systems 5, 25 and 29 | Next revision of this GDD. |
