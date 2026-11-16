# Hover Popup and Glossary

> **Status**: Designed — pending independent design review
> **Author**: User and design team
> **Last Updated**: 2026-11-13
> **Last Verified**: 2026-11-13
> **Implements Pillars**: 4 — Arrangement Is the Thinking; 2 — The Payoff Is the Product; 5 — Power Has a Price
> **Creative Director Review (CD-GDD-ALIGN)**: REVISED 2026-11-13 — first-use Details hint and discoverability criterion added after CONCERNS; safe placement and actual usability remain unassessed.

## Summary

This shared popup lets players inspect items, heroes, bound enemy intents and doors without a permanently docked inspector. Compact information leads to paged Details, including the user's chosen inline keyword explanations and all mandatory owner-provided facts; the separate glossary is deferred beyond the MVP. Input, localization, safe placement, cross-hero targets and combat-playback inspection still depend on unresolved contracts or visual validation.

> **Quick reference** — Layer: `Presentation` · Priority: `MVP` · Key deps: `Game Data Definitions (2), Localization and Text (7), Input and Focus (6), Grid Backpack (8)`

## Overview

System 26 presents the contextual information a player needs to understand an item, an enemy intent, a hero or a door while planning. One shared popup serves the arrangement and room interfaces; it reads authoritative data and localized text from their owners rather than calculating gameplay values. The player previously chose a popup over a permanently docked inspector. The MVP includes explanations of keywords within Details (user decision, 2026-11-13); the standalone glossary page remains deferred. Details also gives access to longer required game information, including a selected enemy's full bound intent sequence.

Input and Focus (6) and Localization and Text (7) are Designed pending independent review. System 6 specifies mouse/keyboard Details and read-only intent-tab access in playback; System 7 specifies text resolution and missing-key policy. Screen layout and integration still need verification. The chosen Unity UI stack belongs to architecture; the local UI engine reference describes Unity 6.3 and has not been reverified for the project's pinned Unity 6.6.

## Player Fantasy

The player is the architect of a three-hero machine. Looking at an item, a threat or a door reveals the essential fact at once; asking for Details reveals the meaning of its keywords, its sources and its consequences before the player commits. The interface rewards curiosity and makes it possible to connect a result to the arrangement that caused it without turning a forecast into a promise.

This supports Pillar 4: the information needed for a decision is reachable before combat, including the bound intents and the meaning of a build's effects. It also supports Pillar 2: explanations cannot cover the cause or the protected combat information. In later tiers, benefits and curse costs must be readable together (Pillar 5). The creative-director consultation chose this direct player-facing framing; no usability evidence is claimed yet.

## Detailed Design

### Core Rules

1. **Scope and ownership.** System 26 owns one contextual popup shared by the backpack, door and relevant HUD clients. It presents owner-provided definitions and state, with text resolved by System 7; it does not change the run, select targets, draw RNG, simulate combat or replace the owner's rules. The MVP has compact information and reachable Details, including inline keyword explanations; a separate menu glossary is deferred.
2. **Open and change target.** A continuous 300 ms hover opens the compact popup; keyboard or gamepad focus opens it immediately (Art Bible 7.9 targets; exact bindings belong to System 6). A pending hover is cancelled if its source changes, disappears or becomes ineligible. While a compact popup is open, moving to an adjacent eligible object swaps its content without a fade-close/reopen if its existing rectangle is still legal. When neither hover nor eligible focus retains the source, allow 60 ms grace; hover or focus returning to that source within the grace keeps it open. Pointer departure alone cannot close a popup whose source retains eligible keyboard/gamepad focus. These are presentation timers, not simulation clocks.
3. **One surface and pin.** Only one popup is active. Details opens the expanded reading state and pins that source; unrelated hovers do not replace it. The popup is not a nested-hover interface. As a provisional interpretation of the Art Bible's competing Details rules, Details advances to the next page when there is more content; in planning, the final or only page returns to compact/unpinned if its source remains eligible, otherwise it enters grace or closes under the final System 6 contract. During combat, final-page exit closes the workbench Details rather than creating a forbidden compact stage popup. Cancel closes. System 6 and the UX spec must resolve actual commands, focus and paging and confirm this interpretation before implementation.
4. **Close and suppress.** Drag/pickup, manual hero switch, combat start, source removal or physical movement of the source object closes the popup and cancels pending opens, even when pinned. In playback, System 25's automatic follow of the acting hero is not a manual hero switch: keep an open enemy-intent Details if its source stays valid and its reading area stays safe, revalidating the layout; unsafe-layout fallback remains open. A pointer moving between stationary adjacent objects is a target change, not movement of the object. While an item is held, no normal popup appears; the workbench name plate shows the held item's name and tier, with a number pair only if it fits. Selection/arming of a door holds its compact popup until confirm or disarm, unless a higher-priority close event occurs. How an armed card interacts with an already pinned unrelated popup requires UX resolution.
5. **Safe placement.** At open, choose a legal rectangle by the Art Bible 7.9 candidates and costs: a workbench object uses the workbench, a room card the free room band, and a stage object's compact view an appropriate chip; the order-chip exception uses the tab-free margin below the strip. Expanded Details for a stage object lands in the workbench lower section, pinned, not in the chip's body band. Reject any rectangle touching the source/focus ring or other protected HP/stat blocks, intent and target, strips, Confirm, turn marker and combat presentation. The position remains fixed while the same object and protected geometry remain unchanged. If a new target or layout invalidates it, never cover protected content: recalculate a legal alternative or close; exact tie-breaking and fallback need UX validation. A chip is not proven always to fit, and a non-obscuring alternate reading route remains **BLOCKED** until designed and tested.
6. **Compact vs Details.** Compact uses a short name, tier in words and shape, relevant numbers and essential effect text, plus affected-hero join numerals when applicable. It is an entry point, not permission to lose required facts. Details provides complete required explanations and pages when needed; keyword definitions appear here in the MVP. A one-time first-use hint points to Details for keyword explanations, as the Art Bible specifies; its wording, trigger, persistence scope and binding display belong to Systems 6/7 and the UX spec. The Art Bible's 152 px widths, line budgets and three-keyword target are provisional layout/content targets, not a license to silently truncate required content or conceal further terms. A visible Details affordance must be reachable with mouse and focus inputs.
7. **Items and heroes.** Item text and authored parameters come from System 2; trigger and effective cooldown with base, applicable modifiers/sources and floor come from System 9/10. Detail gives actual local receives/gives and applicable Works With tags. System 10 adjacency does not cross hero grids: cross-hero affected-hero chips require the separate effect-target data contract, still unresolved. A hero's detail provides max HP and its sources, speed and its sources, and its innate item (Systems 15/11); a turn-order chip gives position, speed sources and an applicable tie-break.
8. **Enemy intent.** After room entry, compact identifies the current bound move, target and damage as before-shield where applicable. Details or pages expose all `H` bound entries in order (`H = 12` by default, safe tuning range 6–20), each move, and its hit count, damage per hit before shield and target when applicable, plus the fixed sequence and wrap after entry `H`. A windup without damage or target does not acquire invented fields. Timing/order is a labelled forecast, not a guaranteed schedule. Before ChooseDoor binds the next room, do not present the prior room's intent or invent future intent. The user chose read-only intent Details during continued playback (2026-11-13): open it on demand in the workbench lower section, with no stage popup, no gameplay/flow command and no pause implied by opening. Exact input, safe placement, source lifecycle and cursor-aligned playback data are blocked pending Systems 5/6/27/29 and UX.
9. **Doors.** A door's compact and Details together expose the generated encounter roster with monster names, small portraits, counts and role glyphs; no scaled HP or damage and no unbound intent sequence are exposed. Portrait assets and fit require UX validation. System 16 owns the roster, System 28 the card/panel, and System 26 the shared popup. A higher-tier cost and its reward must be understandable together; Gold/Red curse-specific content remains Vertical Slice or later, not MVP.
10. **Freshness and incomplete data.** Read a coherent owner-provided state for the target and update or close when the owner invalidates it; playback must use the presentation-aligned snapshot rather than state ahead of the trace. If a definition, localised text, target mapping, or legal placement is unavailable, never invent a value or expose hidden data. System 7's missing-key policy is now defined (its Rule 9: requested language, then en, then vi, then a bracketed key marker, never a number); incomplete owner contracts and layout of the marker inside a popup remain integration blockers.

The main interaction rules were informed by game-designer, UX designer, art-director, UI programmer and technical-artist consultations. The inline keyword decision was made by the user on 2026-11-13.

### States and Transitions

| State | Entry | Exit / guard |
|---|---|---|
| Closed | No eligible source, cancelled or suppressed | Hover starts Pending; eligible focus opens Compact. |
| Pending | Pointer enters an eligible source | After 300 ms continuous hover opens Compact; source loss/change or suppression cancels the timer. |
| Compact | Hover delay elapsed, focus, or pinned Details returned to compact | Adjacent target may swap only after geometry revalidation; losing both hover and eligible focus enters Grace; Details opens Pinned; arm holds until confirm/disarm. |
| Grace | Neither hover nor eligible focus retains an unarmed compact source | Hover or eligible focus returning within 60 ms preserves it; expiry closes. Higher-priority close events bypass grace. |
| Pinned / page `p` | Details on a source | Other hovers suppressed; Details advances pages, then final/only page returns Compact in eligible planning or closes in combat (provisional); Cancel or a higher-priority close event closes. |
| Suppressed | Held item, combat restriction for stage object, invalid source or other close condition | Ordinary stage hover remains suppressed in combat. A deliberate presentation-only intent Details action may open Pinned in the workbench without opening a stage popup; input and safe-layout contracts remain blocked. |

Source identity, active page and geometry belong to presentation state, not the saved run. Modal, arm and focus precedence are provisional until System 6 and the screen UX resolve them.

### Interactions with Other Systems

| Owner/client | Input to popup | Interface / ownership |
|---|---|---|
| 2 Game Data, 7 Localization | Stable IDs, authored parameters, string keys and resolved text | 2 owns data; 7 defines text lookup/fallback. Missing data fields and integration remain open. |
| 6 Input and Focus | Hover, focus, Details, page and Cancel intents | System 6 owns mappings and focus; its GDD is Designed pending review and integration. |
| 8 Grid Backpack; 25 Backpack UI | Item identity, grid arrangement, held/drag/hero switch | System 25 is popup client and supplies current source/lifecycle; no placement mutation by 26. |
| 9/10/11/15 | Trigger and cooldown breakdown, local links, turn order, hero stats | Each value and source comes from its owning system; cross-hero effect target mapping is open. |
| 14 Enemy and Intent; 27 Combat HUD | Bound sequence and its displayed target/value | 14 owns bound intent; 27 owns HUD. Read-only Details during playback is user-selected; its input, placement and view contracts remain unresolved. |
| 16 Encounter; 17 Door Tiers; 28 Door UI | Stored door roster, role glyph, tier and reward | 16/17 own content, 28 card layout, 26 popup; never redraw the encounter. |
| 5 Run Flow; 29 Payoff | Room/phase state and presentation-aligned playback view | No unbound next-room intent or future simulation state is shown. |

## Formulas

System 26 owns no gameplay or balance formula. It reads values and forecasts from their owners. The following timing rules describe presentation only and use a monotonic presentation clock, not game simulation time.

The `hover_open` formula is defined as:

`hover_open = eligible AND elapsed_hover_ms >= hover_delay_ms`

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `eligible` | bool | false / true | System 6 and current presentation state, provisional | Source is still valid and no suppression applies. |
| `elapsed_hover_ms` | nonnegative duration | 0 or more | presentation clock | Continuous time on the same eligible target; resets when eligibility ends. |
| `hover_delay_ms` | duration | 300 ms target | Art Bible 7.9 | Continuous hover delay; focus does not wait for this timer. |

**Output Range:** false or true. No opening occurs if the source becomes ineligible.

**Example:** 299 ms continuous hover does not open; at 300 ms it can open if still eligible.

The `leave_close` formula is defined as:

`leave_close = unarmed_compact AND NOT retained_by_focus AND elapsed_outside_ms >= leave_grace_ms`

**Variables:**
| Variable | Type | Range | Source | Description |
|---|---|---|---|---|
| `unarmed_compact` | bool | false / true | popup and client state | Compact source is not held open by an armed selection or a pin. |
| `retained_by_focus` | bool | false / true | System 6, provisional | Eligible keyboard or gamepad focus still holds this source. |
| `elapsed_outside_ms` | nonnegative duration | 0 or more | presentation clock | Continuous time while neither hover nor eligible focus retains the source; resets on return. |
| `leave_grace_ms` | duration | 60 ms target | Art Bible 7.9 | Allowed leave interval before normal closing. |

**Output Range:** false or true; true requests closure, followed by the Art Bible's 60 ms fade-out target when safe. A higher-priority close event requests closure without waiting for grace and must not leave a protected target obscured.

**Example:** returning after 50 ms cancels the pending close; remaining outside for 60 ms starts closing the unarmed compact popup.

Placement uses the Art Bible's exclusion and cost ordering, not a new gameplay formula. Candidate ties and safe fallback remain unresolved UX contracts. The owner's gameplay calculations—such as cooldown, threat and damage—are neither copied nor reimplemented here.

## Edge Cases

- **If hover target changes during the delay:** cancel the old timer and start eligibility for the new target; no content from the old target may open late.
- **If hover and keyboard/gamepad focus select different sources:** System 6's Pointer/Nav mode chooses hover or focus and prioritizes pinned, then armed, then the active hover/focus reading. No more than one popup may be shown; integration remains unverified.
- **If an adjacent target makes the current rectangle cover that target or a protected area:** revalidate and choose a legal position or close, even though the normal same-target popup does not move. Exact transition and tie-break are an open UX decision.
- **If no compact or fallback chip rectangle is legal:** do not cover protected content or assert that a chip always fits; the accessible alternative is blocked pending the UX spec.
- **If text, locale, resolution, target geometry or content changes while open:** remeasure and revalidate before showing the changed content; if no legal route exists, apply the unresolved safe-fallback behavior, never clip a mandatory fact silently.
- **If Details content exceeds one page or the authored keyword count target:** keep all mandatory content available via pages; do not silently discard a keyword, an intent entry or a source. Paging/close key mapping is provisional.
- **If an item is held via click-to-pick or drag:** suppress ordinary popups for either interaction until Holding ends; show only the held readout defined by System 25. Right-click rotation while held must not also open Details.
- **If the source moves, is destroyed, the hero switches manually or combat starts:** close even a pinned popup and cancel pending timers; do not retarget a pin to a different item or hero. An automatic playback follow of the acting hero instead revalidates an open enemy-intent Details and retains it if safe; fallback when unsafe remains blocked.
- **If a door is armed and another popup is pinned:** the active-popup priority is unresolved; do not render two popups. An armed popup still closes on higher-priority source loss, drag or combat start.
- **If the next room has not been selected:** no bound intent sequence for it exists; show an unavailable/not-yet-selected state, not the previous room's sequence.
- **If a localization key, owner value or effect-target mapping is missing:** do not substitute a fabricated number, silently drop a required source, or claim success. The final error and fallback presentation belongs to Systems 2/7 and UX.
- **If playback has already thawed the authoritative model:** use only the state made available for the currently displayed trace step. Stage-object popups remain suppressed in combat; read-only enemy-intent Details opens in the workbench by user decision, but its playback snapshot and input/layout interfaces are unresolved. System 14's combat-HUD popup evidence criterion 45 and System 9's in-combat reaction readiness explanation remain blocked until their respective surface and state contracts are settled; pre-fight evidence alone cannot pass them.

## Dependencies

| Dependency / client | Direction | Contract and status |
|---|---|---|
| 2 Game Data Definitions | Hard upstream | IDs, authored parameters and text keys; Designed, pending independent review. Trigger/synergy/enemy content fields are not all finalized. |
| 7 Localization and Text | Hard upstream | Resolve text and missing-key policy (Designed, `localization-and-text.md`): Vietnamese and English ship in the MVP; real font/diacritic validation remains a separate gate. |
| 6 Input and Focus | Hard upstream | Designed pending review: hover, focus, Details, paging, cancel, arbitration and bindings. Screen/toolkit integration remains open. |
| 8 Grid Backpack; 25 Backpack UI | Hard client interface | Stable item identity and placement/held/hero-switch lifecycle; both GDDs await independent review. |
| 9 Item Trigger; 10 Adjacency | Hard for item information | Trigger, cooldown and local source/target breakdown; cross-hero effect mapping is not adjacency and is not yet specified. |
| 11 Turn Order; 15 Party and Heroes | Hard for order/hero information | Forecast labels, tie-break and source breakdown for speed and max HP. |
| 14 Enemy and Intent; 27 Combat HUD | Hard for intent information | Full bound sequence before combat and read-only workbench Details during playback; System 27 Designed pending review. Input, placement and playback-state integration remain unresolved. |
| 16 Encounter; 17 Door Tiers; 28 Door UI | Hard for door information | Actual roster/count/role and reward information; 28 Designed pending review. |
| 5 Run Flow; 29 Payoff | Presentation state and playback gating | System 5 Designed pending review; 29 (revised 2026-11-14, CD-GDD-ALIGN CONCERNS applied, independent review pending) proposes playback snapshots and Skip closing Details at every fast-forward leg, pending its review and integration. |
| Art Bible; accessibility and UX specifications | Visual and interaction requirements | Art Bible and accessibility requirements are Draft; screen UX specs are missing. |

The indexed dependencies are 2, 7, 6 and 8. The other rows expose consumers and data already required by their GDDs. The prototype's fixed inspector footer is not evidence that this popup has been implemented.

## Tuning Knobs

The Art Bible owns the provisional 300 ms hover delay and 60 ms leave grace, as well as popup frame sizes and motion targets; System 26 does not create new balance knobs. Any changes to those values require UX/art review and testing across dense layouts and input modes. System 6 owns bindings; System 7 owns localization and fallback. Character counts and the three-keyword Details authoring budget are targets to test with real content, not permission to omit essential definitions or the `H`-entry sequence (`H=12` by default). No measured frame-time or memory budget for this popup has been established.

## Visual/Audio Requirements

- One non-docked, non-nested popup with Art Bible square frame and text hierarchy; tier information has a word and shape, not color alone. A pinned expanded view is a reading state and does not follow the cursor.
- The Art Bible proposes 152 px compact width (up to 140 px high), 152 px expanded width (up to 232 px high) and a stage chip up to 120×18 px. These are draft native-pixel targets, not verified guarantees: final resolution, legibility, font coverage and protected-region fit require retained evidence.
- A stage object uses its own safe body band except for the order-chip exception; stage popups are absent during combat. Room panels use the free room band. A full or fallback surface never covers the source, HP, target, intent, strips, Confirm or turn marker.
- Existing Art Bible motion gives a 100 ms fade-in/expand and 60 ms fade-out; respect the final reduced-motion design. This system does not invent an audio cue. The chosen font, Vietnamese glyph/render test and contrast evidence remain pending.

## Game Feel

- Quick to inspect without interrupting planning: hover is deliberate (300 ms target), focus responds immediately, and a brief 60 ms pointer exit does not flicker the popup. Planning has no time pressure.
- Details is deliberate and discoverable; pagination preserves the player's place and never changes gameplay. Close/suppress promptly when the source moves or the player picks up an item, keeping the cursor and placement targets clear.
- Readability wins over decorative motion. Timings are targets from the draft Art Bible and must be measured in a real UI; no test or playtest has yet established they are met.

## UI Requirements

- A compact item popup names its tier in words, gives essential effect information and shows affected-hero chips only from real effect-target data. Details exposes trigger wording, base and effective cooldown with individual modifiers/floor where applicable, local synergy sources/targets, Works With tags, and inline keyword explanations. Passive or combat-start items do not acquire fabricated cooldown values.
- An intent popup gives the current bound intent in compact form and all `H` entries via Details/pages before combat and in read-only workbench Details during playback, including target, hit count and damage before shield when applicable, plus fixed sequence and wrap. `H=12` is the default, not a hard cap. Before binding, there is no next-room sequence to display.
- Door content exposes the actual generated monster roster, small portraits, counts and role glyphs, with no scaled stats or future intent. Hero/order Details exposes the owner-supplied stats, sources, innate item and relevant tie-break. System 9's in-combat reaction-readiness readout remains blocked on the playback inspection/state contract.
- Hover and focus need equivalent information and a discoverable Details action; no mapping is final before System 6. Popup and content must remain readable without relying only on color, with text alternatives and a layout that does not hide protected information.
- The UX specification must demonstrate that full intent, long localized text, dense grids, armed doors and larger text have legal routes to all required content. The planned fallback when no legal rectangle exists is blocked until designed and demonstrated.

## Cross-References

| Document | Relevance |
|---|---|
| [Game Concept](game-concept.md) and [Art Bible](../art/art-bible.md), especially 7.9 | Pillars, user choice of popup, placement, visual and timing constraints. |
| [Game Data Definitions](game-data-definitions.md); [Grid Backpack](grid-backpack.md); [Backpack UI](backpack-ui.md) | Authored data, item identity, lifecycle, held readout and affected-hero presentation. |
| [Item Trigger and Cooldown](item-trigger-and-cooldown.md); [Adjacency and Synergy](adjacency-and-synergy.md) | Trigger/cooldown details and local link explanations. |
| [Turn Order](turn-order.md); [Party and Heroes](party-and-heroes.md); [Enemy and Intent](enemy-and-intent.md) | Forecast, sourced hero stats and complete bound intent. |
| [Encounter Generation](encounter-generation.md); [Door Generation and Tiers](door-generation-and-tiers.md); [Run State and Flow](run-state-and-flow.md) | Door roster and rewards, timing of binding, and playback gate. |
| [Systems Index](systems-index.md) | MVP scope, deferred standalone glossary and shared popup ownership. |

## Acceptance Criteria

These are design criteria, not tests run in this session. **BLOCKED** names an unresolved dependency; **NOT ASSESSED** requires real input, timing or visual evidence.

1. **GIVEN** an eligible stationary source, **WHEN** presentation hover time reaches 299 ms then 300 ms, **THEN** compact opening has not begun at 299 ms and begins at 300 ms; its separate fade-in target is 100 ms. Focus begins opening without the hover delay.
2. **GIVEN** hover is pending on A, **WHEN** the pointer moves to B or A disappears before the delay, **THEN** A's timer cannot open a late popup and B requires its own continuous eligible hover.
3. **GIVEN** an unarmed compact popup with neither hover nor eligible focus retaining it, **WHEN** hover or focus returns within 59 ms, **THEN** the pending close is cancelled; remaining without either for 60 ms starts the closing transition. **GIVEN** eligible focus stays on its source, **WHEN** the pointer leaves for over 60 ms, **THEN** the popup remains open. An armed door holds compact through ordinary leave until confirm or disarm unless a higher-priority close occurs.
4. **GIVEN** Details pinned on A, **WHEN** B is hovered, **THEN** no second popup or replacement appears. **WHEN** the item is picked up, the hero switches manually, the source moves/disappears or combat starts, **THEN** the popup closes logically and pending timers are cancelled; while holding, only the held name/tier readout is shown and rotate does not open Details. Automatic playback follow preserves open enemy-intent Details while its source and layout remain valid.
5. **GIVEN** compact A and an adjacent stationary B, **WHEN** the target changes and the existing popup rectangle is legal for B, **THEN** content swaps without close/reopen. **WHEN** it would cover B or another protected region, **THEN** it is revalidated and moved to a legal candidate or closed without obscuring protected content.
6. **GIVEN** competing hover and focus, or an armed card while a different popup is pinned, **WHEN** both request a popup, **THEN** at most one is displayed using System 6's Pointer/Nav mode and priority pinned, then armed, then the active hover/focus reading. Actual integration and visual evidence remain unverified.
7. **BLOCKED — paging input. GIVEN** a Details view with multiple pages, **WHEN** the player navigates, **THEN** every mandatory fact and keyword definition remains readable and Cancel closes. The provisional Details/next/final-page behavior needs System 6 and UX confirmation; a stage object's expanded view is in the workbench lower section, not its compact body-band chip.
8. **GIVEN** an item with base cooldown 3 and a Whetstone modifier reducing it by 1, **WHEN** its Details opens, **THEN** the owner-provided trigger, base 3, modifier source/amount and effective 2 appear. A floor case explains when further reduction has no effect; passive and CombatStart items do not receive fabricated cooldowns.
9. **BLOCKED — cross-hero targets. GIVEN** a cross-hero effect, **WHEN** its item is inspected, **THEN** the affected-hero join numerals match the owning effect's target data. System 10 adjacency stays within a single hero grid; the effect-target data contract is still missing.
10. **GIVEN** an enemy with 12 already-bound entries including a three-hit strike, a targetless windup and an all-hero move, **WHEN** Details is read before combat, **THEN** all 12 appear in order with each applicable move, hit count, per-hit damage before shield and target, the fixed sequence and wrap after entry 12. The forecast's timing is labelled; absent windup fields are not invented.
11. **GIVEN** DoorChoice before a door has been selected, **WHEN** the next-room information is inspected, **THEN** the previous room's intents and any unbound future sequence are not shown as the next room's intent.
12. **GIVEN** a Blue door with the stored roster Elite ×1 and Hunter ×1, **WHEN** the popup is read, **THEN** both names, small portraits, counts and role glyphs are available with the price/reward, with no scaled HP/damage or unbound sequence. Inspection causes no encounter redraw or RNG draw.
13. **GIVEN** owner data for a hero with base max HP 110 plus 30 from items and speed 10 plus 2 from a Ring, **WHEN** Details is read, **THEN** it presents HP 140 and its sources, speed 12 and its sources, the innate item, and an applicable order tie-break from its owner.
14. **BLOCKED — missing data. GIVEN** an absent definition, owner value or localization key, **WHEN** the popup is requested, **THEN** it never silently fabricates a zero or exposes hidden values. System 7's Rule 9 supplies the text fallback (a bracketed key in Fog, never a number); data owners and UX still define the safe reading route.
15. **BLOCKED — safe fallback. GIVEN** a layout or locale change while a popup is open, **WHEN** content or geometry changes, **THEN** it is remeasured and checked against protected regions. If neither the popup nor a chip has a legal location, no protected region is covered; an accessible alternative remains to be designed and tested.
16. **BLOCKED — combat inspection. GIVEN** playback after a victory has already thawed the live state, **WHEN** the user opens read-only enemy-intent Details in the workbench, **THEN** playback continues, the presentation-aligned state is used, no prohibited gameplay command is sent, and no stage popup appears. Systems 5/6/14/27/29 must define retained view, input, lifecycle and safe layout before System 14 AC 45 can pass. System 9 reaction-ready inspection still needs its own combat surface contract.
17. **NOT ASSESSED — visual/accessibility evidence. GIVEN** target-resolution fixtures with dense grids, real Vietnamese terms, long roster and full 12-entry Details, armed card, focus and reduced motion, **WHEN** recorded with a fixed-tick capture and interaction trace, **THEN** every required fact is discoverable and legible and protected UI remains uncovered. Final font, text expansion, contrast and accessible input require actual retained evidence.
18. **NOT ASSESSED — interaction timing. GIVEN** an implemented popup, **WHEN** hover, focus, leave, fade and expansion are measured, **THEN** the 300/60 ms trigger targets and separate 100/60/100 ms fade-in/fade-out/expand targets are evaluated independently on the target device. No measured result is claimed here.
19. **BLOCKED / NOT ASSESSED — first-use discovery. GIVEN** the first eligible keyword encounter and the resolved hint trigger, **WHEN** a player uses mouse or focus input, **THEN** a one-time hint points to Details and its inline definitions. System 6 supplies the bindings and System 7 the wording slot (`ui.hint.details`); UX must still specify its scope; playtest discoverability rather than claiming this criterion has passed.

## Open Questions

| Question | Owner | Resolve by |
|---|---|---|
| What non-obscuring, accessible reading surface works when no legal popup or chip rectangle exists? How are candidate ties handled? | UX designer, art director | Before popup implementation; validate dense layouts. |
| How do Details, paging and close interact across mouse and focus inputs, including armed cards and a prior pin? | System 6, UX designer | Before interaction implementation. |
| What exactly triggers the one-time Details hint, how long is "once" remembered, and what localized binding appears? | Systems 6/7, UX designer | Before first-use hint implementation. |
| System 6 defines Pointer/Nav arbitration and pinned/armed/active-reading precedence; validate it with this screen and toolkit. | Systems 6/26, UX designer | Before input integration. |
| What contract supplies cross-hero effect targets and accurate affected-hero chips, without cross-grid adjacency? | Effect owners, Systems 25/26 | Before affected-hero presentation. |
| How does the user-selected read-only intent Details remain safely usable during ongoing playback, including input, source-down and auto-follow cases, without a stage popup or flow command? | Systems 5/6/14/27/29, UX designer | Before combat HUD implementation. |
| How are missing definitions, missing localized strings and text expansion handled without invented data or inaccessible required content? | Systems 2/7, UX designer | Before content integration. |
| How is presentation-aligned state supplied during playback after the authoritative model may have advanced? | Systems 5/26/27/29 | Before playback integration. |
| Can the proposed frame sizes and line budgets contain real Vietnamese text, all `H` intents (12 by default), roster and source breakdown with safe pagination? | UX designer, art director | Font and screen-layout validation. |
| Which Unity UI technology and pixel strategy are appropriate for the pinned editor? | Technical director | UI architecture decision and engine spike. |
| When does the standalone menu glossary ship, and how are later-tier curse keywords and cross-references added? | Game designer | Post-MVP planning. |
| Answered by System 6 ([input-and-focus.md](input-and-focus.md), 2026-11-13): Details is F or a right click with nothing held, arrows never scroll the popup, Cancel (Esc) closes it in the priority order rebind capture, dialog, popup, held item, armed card, menu; a sibling card's popup temporarily replaces the armed one; reconcile the provisional wording of this GDD. | Systems 6 and 26 | Next revision of this GDD. |
| Answered by System 7 ([localization-and-text.md](localization-and-text.md), 2026-11-13): missing strings follow Rule 9 (fallback chain, bracketed key marker, no invented number); popups never shrink or truncate text (wrap, paginate under Details, grow within 30 percent spare height); `term.*` entries and `[term:id]` markup are owned by System 7; the first-use Details hint wording lives in the `ui.hint.details` slot; reconcile the provisional wording here. | Systems 7 and 26 | Next revision of this GDD. |
