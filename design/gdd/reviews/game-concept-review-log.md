# Review Log: Game Concept (Grimpack)

## Review — 2026-11-14 — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: XL
Specialists: game-designer, economy-designer, systems-designer, qa-lead, creative-director (senior)
Blocking items: 4 | Recommended: 5
Summary: The concept is internally coherent (fantasy, five pillars and anti-pillars agree, registry values match) but contradicts its own specs in two load-bearing places: the Unique Hook claims a single backpack cannot sequence effects (item-trigger Rules 5, 8, 11 allow it), and hero order is promised as a player skill while party-and-heroes removes reordering. The MVP hypothesis ("most players reach a going-off moment") would validate a common spectacle rather than earned rarity, and Pillar 5 has no MVP content. Fixes are text changes plus four owner decisions, not a new direction. The 8-section system-GDD checklist does not apply to a concept document (0/8 by design); the review used concept-level criteria.
Prior verdict resolved: First review
Findings:
- [BLOCKING] Unique Hook: "a single large backpack cannot sequence effects" is contradicted by item-trigger-and-cooldown Rules 5, 8, 11; rewrite around cross-hero timing and add a harness check that split builds beat stacking on one hero
- [BLOCKING] MVP Definition: core hypothesis and go/no-go 1 ("most players reach a going-off moment") contradict Pillar 3 (about 1 in 20); add a payoff ladder, validate the rare rung by harness, add a near-miss readout
- [BLOCKING] Pillar 5 / Key Dynamics / Elevator Pitch: no MVP content (no Gold, Red or curses; Purple's price is only difficulty); add a real cost or defer explicitly and soften the wording
- [BLOCKING] Core Fantasy / Pillar 4 / MVP item 2: "hero order" and the Tanker/Warrior/Support roster conflict with party-and-heroes (pick 3 of 4, no reordering, order only via speed items)
- [RECOMMENDED] Pillar 3: named knobs cannot steer 1-in-20 alone (boss HP and item magnitudes unlisted); no sinks in MVP and about 16 droppables risk convergence
- [RECOMMENDED] Pillar 1: says it owns direction not power, but the lean raises synergy density and is a Pillar 3 knob; restate and add a lean on/off ablation
- [RECOMMENDED] MVP go/no-go: "most" undefined (use at least 6 of 8), personal-best metric undefined, "zero open ordering bugs" unmeasurable, live-play event-log parity not listed, visual tests lack thresholds, only 4 tiers testable in MVP
- [RECOMMENDED] Pillars / Core Loop: design tests rarely name a competing value; fully automatic combat without speed control or skip in MVP is a retention risk
- [RECOMMENDED] Stale or conflicting sections: Next Steps, Open Questions (committed is answered), Technical Risks (several now specified), monster count 4-5 vs 5-6, session length 30-60 vs 25-30, Platform line vs shipping assumptions, "about half" vs exactly 50
Reviewed-Content-Hash: design/gdd/game-concept.md 6b9d38c318360c81f27d5f6e4ccf06ddd622f5f2
Reviewed-Content-Hash: design/registry/entities.yaml 7e57d0c08597fafafe42c4d4f9968b80d51a9165
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/systems-index.md ae1e293ab6286804dfe005f28723a862d761cc72
Reviewed-Content-Hash: design/gdd/turn-order.md 4a292e562356d13db40fa6bf592f29d60f72fa48
Reviewed-Content-Hash: design/gdd/curses-and-trade-offs.md 050b8a1857d7e86a9816d6e7587bf5f651181aa2
Reviewed-Content-Hash: design/gdd/enemy-and-intent.md 813da88ac84fcb62e5df5cacc6bf855a8dd30898
Reviewed-Content-Hash: design/gdd/commitment-and-drop-model.md 25f09d9d4795145a84edd08c3836006d5e60c68b
Reviewed-Content-Hash: design/gdd/party-and-heroes.md 40ef0a45512a8154fdb904b246ca0b65768aa12f
Reviewed-Content-Hash: design/gdd/item-trigger-and-cooldown.md 4843a2ef63c965f705a87bd96520dd4d7599e035
Reviewed-Content-Hash: design/gdd/balance-simulation-harness.md d39644f07bd0a529b348cc2a46e56a883706b979
Reviewed-Content-Hash: design/gdd/door-generation-and-tiers.md 625520891e297560b7c27d41ec32619f607f0b81
Reviewed-Content-Hash: design/gdd/combat-resolution.md b751a6b7e57a2bd5d80c596b5957b3cb154da823

## Review — 2026-11-14 (re-review) — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, producer, unity-specialist, creative-director (senior)
Blocking items: 6 | Recommended: 7
Summary: The direction is sound and no pillar or the core loop changes. The remaining faults are claims the concept made on behalf of GDDs that say otherwise (hook mechanics, Purple's cost, the System 18 sink, the harness ceiling) plus an MVP timeline that no longer matched its scope. The concept was revised the same day after owner decisions D1 to D4. The 8-section checklist does not apply to a concept document.
Prior verdict resolved: Partly (MVP hypothesis resolved; Unique Hook and hero order partly; Pillar 5 text contradicted the door GDD)
Owner decisions (2026-11-14): D1 rewrite the hook around parallel cooldown clocks, innate items and cross-hero Reactions, tested at an equal item or cell budget; D2 state that the MVP does not test Pillar 5 (Purple has no cost, first test is Gold with a curse in Tier 1.5); D3 set a numeric lean ceiling (working value: run-defining rate lean-on / lean-off at most 1.5) and add a lean-off arm to System 35; D4 restate the MVP as about 6 months and add ordered first cuts. The cuts are listed as first cuts, not applied, because the personal best and the speed control were earlier user decisions.
Findings:
- [BLOCKING] Unique Hook: "turn order is the combo" is undercut by item-trigger Rules 5, 8 and 11 and turn-order Rule 4; "no single hero can carry a whole machine" is false in numbers; the split-vs-packed test wins on capacity alone and no harness question measures it (fixed in text; System 35 comparison still to be added)
- [BLOCKING] MVP item 5 / Pillar 5: the Purple "build-shaping cost" does not exist (door Rule 11 and AC 11 give Purple no cost; System 19 covers only Gold and Red) (fixed in text)
- [BLOCKING] Key Dynamics / Motivation / Discovery / Explorers / Retention / Flow: "hero order" still sold as a player skill, against party-and-heroes Rule 2 (fixed in text)
- [BLOCKING] Pillar 1: the lean multiplies the run-defining odds about 3.4 times (0.11 to 0.38 in the System 18 sketch), not "a little"; no numeric ceiling and no lean-off arm exist (ceiling added in text; System 35 arm still to be added)
- [BLOCKING] Scope Tiers / Scope Risks: "4-5 months" and "the prototype may shorten it" contradict the systems index (about 6 months; prototype refactored, not reused); the revision added at least 6 MVP deliverables with no offset (fixed in text)
- [BLOCKING] Design Risks: "the sink is decided in System 18 (copy cap of 1)" is false; System 18 sets a copy cap of 2 and decides no sink (fixed in text)
- [RECOMMENDED] Pillar 3: payoff ladder not mapped to System 29 classes; rung 1 as Chain cannot fail; rung 2 conflicts with the "about one Detonation per run" aim (mapped in text)
- [RECOMMENDED] Pillar 3: the 50 percent share is a movable knob equivalent to boss HP, and boss HP has no value in any GDD (frozen in text; boss HP anchor still open)
- [RECOMMENDED] MVP Definition: go/no-go criteria lacked per-group floors, NOT ASSESSED rules, a peak definition, the "name the contributing items" test and a defined kill/pivot signal with checkpoints and a decider (added in text)
- [RECOMMENDED] Technical Risks: domain reload is off in EditorSettings.asset, so "NOT SOURCEABLE" was wrong; no determinism contract (explicit hash, own RNG, no mutable statics, IL2CPP parity) (added in text; ADR to fix the details)
- [RECOMMENDED] Visual Identity: the squint test still used the "brightest 10% of pixels" wording that art bible 4.10 replaced (synced)
- [RECOMMENDED] Stale text: session length, Platform line, Tier 1.5 and Tier 2 content (Red belongs to Tier 3), settings placement, "6 colour tiers" without "(4 in MVP)" (fixed in text)
- [RECOMMENDED] Other documents: door GDD and systems index (Pillar 5 carrier, 3 heroes and 5-6 monsters), balance-simulation-harness (split-vs-packed and lean-off arms, build diversity, boss HP), post-combat-breakdown (near-miss readout) (follow-ups applied later the same day, 2026-11-14, with owner decisions: Purple restored as the second cut after item rotation, `X` frozen at 50, harness questions 14 to 16 added with provisional margins, near-miss readout specified as a Vertical Slice feature in System 30 Rule 12; monster count corrected to 6 regular types plus a boss in the concept, the systems index and System 14; boss HP anchor is System 14's provisional 300 to 420)
Note: the Gold tier placement (concept Tier 1.5 against registry "Vertical Slice") is not a conflict: the systems index maps Vertical Slice to concept Tier 1.5.
Reviewed-Content-Hash for design/gdd/game-concept.md: not stamped on purpose. The document was revised after this review, so the next run must review it in full.
Reviewed-Content-Hash: design/registry/entities.yaml 7e57d0c08597fafafe42c4d4f9968b80d51a9165
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/systems-index.md ae1e293ab6286804dfe005f28723a862d761cc72
Reviewed-Content-Hash: design/gdd/turn-order.md 4a292e562356d13db40fa6bf592f29d60f72fa48
Reviewed-Content-Hash: design/gdd/curses-and-trade-offs.md 050b8a1857d7e86a9816d6e7587bf5f651181aa2
Reviewed-Content-Hash: design/gdd/enemy-and-intent.md 813da88ac84fcb62e5df5cacc6bf855a8dd30898
Reviewed-Content-Hash: design/gdd/commitment-and-drop-model.md 25f09d9d4795145a84edd08c3836006d5e60c68b
Reviewed-Content-Hash: design/gdd/party-and-heroes.md 40ef0a45512a8154fdb904b246ca0b65768aa12f
Reviewed-Content-Hash: design/gdd/item-trigger-and-cooldown.md 4843a2ef63c965f705a87bd96520dd4d7599e035
Reviewed-Content-Hash: design/gdd/balance-simulation-harness.md d39644f07bd0a529b348cc2a46e56a883706b979
Reviewed-Content-Hash: design/gdd/door-generation-and-tiers.md 625520891e297560b7c27d41ec32619f607f0b81
Reviewed-Content-Hash: design/gdd/combat-resolution.md b751a6b7e57a2bd5d80c596b5957b3cb154da823
Reviewed-Content-Hash: design/gdd/payoff-presentation.md 854095d1e79ae0f2be66318da90a1b8a6e931812
Reviewed-Content-Hash: design/gdd/post-combat-breakdown.md 309a69fce9d8559cc87fbf94920aa15aaeac8bf0
Reviewed-Content-Hash: design/gdd/effect-resolution-model.md 5624328a8e34eccea27d8ed66a4371106ef812d8
Reviewed-Content-Hash: design/gdd/grid-backpack.md 08864f21a84a82b44b593c15b22bea620f8dd69a
Reviewed-Content-Hash: design/art/art-bible.md 9be45eb884f2349bc256f05312d31ddecb87bc8e

## Review — 2026-11-14 (third review) — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, producer, unity-specialist, creative-director (senior)
Blocking items: 6 | Recommended: 12
Summary: The direction is sound; the pattern is that each round's text fixes added numbers to the concept (the 1.5 lean ceiling, 6-of-8 rules, the 6-month figure, ladder rates) that no GDD math backs, and this round mostly found faults in those additions. The creative director's remedy is structural: the concept states intent only, and each number lives in its owning GDD (System 18, System 35, the playtest plan) and is cited. A fourth review that adds new uncited numbers escalates to MAJOR REVISION NEEDED. The 8-section checklist does not apply to a concept document.
Prior verdict resolved: Partly (round-2 text claims fixed; the numbers added to fix them are what failed)
Owner decisions (2026-11-14, third round; a separate set from the earlier D1 to D4): D1 withdraw the 1.5 lean ceiling and set the ceiling and a perceptibility floor only after System 18 computes the exact finite pool for one, two and three lean-dependent key pieces (gate = upper confidence bound); D2 re-estimate the MVP at 8 to 10 months, day 0 = start of the combat core spike, add a week-10 greybox payoff checkpoint, cap each tuning pass at 2 weeks, reorder the first cuts; D3 Surge-class moments and the boss fight auto-slow and cannot be skipped; D4 Q14 (split vs packed) is reported in the MVP and carries a decision trigger that rewrites the "three stages" wording if split does not win.
Specialist disagreements: lean ceiling (game-designer said consistent; systems-designer, economy-designer and the creative director computed it unreachable: ratio 1.56 at B=250, 2.16 at 500, 3.4 at 1000; ruled unreachable, with the finite pool possibly lowering the true ratio); run-defining metric favouring packed builds (downgraded to recommended, Q14 measures it); Hook bullets conflict (rejected, kill trigger kept); Pillar 5 untestable in the MVP (not blocking, earlier decision D2 stands; the Purple cell-lock price remains an open option); playtest plan and sample power (downgraded to a gate line).
Findings:
- [BLOCKING] Pillar 1: the 1.5 lean ceiling is unreachable under the System 18 sketch and the sketch does not fit the finite pool (fixed in text; exact computation still to do)
- [BLOCKING] Pillar 3 / Design Risks: the 1-in-20 rests on a with-replacement model; with 16 droppables and 24 slots the perfect run may be a near-fixed item set (named in text; System 35 metrics still to add)
- [BLOCKING] Core Fantasy / Pitch / Hook / Key Dynamics: "door buffs" and "bet on a bigger payoff" are not machine-relevant in the MVP (fixed in text)
- [BLOCKING] Scope Risks / Scope Tiers / first cuts: 6 months was reworded, not re-estimated; old cuts recover 2 to 3 weeks; kill signal read too late (fixed in text; /estimate still to run)
- [BLOCKING] MVP go/no-go and kill/pivot: criteria that cannot reach a verdict, wrong cut number, live-play parity and IL2CPP missing, no playtest plan (fixed in text; the plan itself is still to write)
- [BLOCKING] Unique Hook: gated only at the Vertical Slice, no MVP decision trigger (trigger added)
- [RECOMMENDED] Speed order claim ("where items sit"), watch beat vs Skip, MVP cannot show the Core Fantasy, Pillar 5 vs Pillar 3 curse question, pick-3-of-4 gives 4 parties, no lean perceptibility floor (applied or listed as open questions)
- [RECOMMENDED] Q15 gate and run count, undefined "drop cap", ladder rungs as per-run probabilities, Q14 budget definition and random-allocation control, droppable minimum has no slack (partly applied; System 35 and the droppable count are follow-ups)
- [RECOMMENDED] "48 firing cells" is not a real limit, "Gold drop rate", meta-unlock and System 20 undefined (applied)
- [RECOMMENDED] Coding rules for judged criteria, classifier config pinned, evidence types for MVP items 1 to 8, six-tier visual test cannot run in the MVP (the playtest plan and test plan own these; not applied here)
- [RECOMMENDED] Determinism contract details, UGUI vs UI Toolkit spike, forced re-pin, harness as plain dotnet project (summarised in Technical Risks; details belong in the determinism ADR)
- [RECOMMENDED] Stale text: 16-20 months, session length headings, header and Next Steps, systems index lines (applied in the concept; the systems index is a follow-up)
Reviewed-Content-Hash for design/gdd/game-concept.md: not stamped on purpose. The document was revised after this review, so the next run must review it in full.
Reviewed-Content-Hash: design/registry/entities.yaml 7e57d0c08597fafafe42c4d4f9968b80d51a9165
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/systems-index.md 58f3d8f606c35c9304c50b0f3685e7bab67f0841
Reviewed-Content-Hash: design/gdd/commitment-and-drop-model.md 25f09d9d4795145a84edd08c3836006d5e60c68b
Reviewed-Content-Hash: design/gdd/door-generation-and-tiers.md b1e60e157db6a19f6e80cbd96e31292ec581e702
Reviewed-Content-Hash: design/gdd/party-and-heroes.md 40ef0a45512a8154fdb904b246ca0b65768aa12f
Reviewed-Content-Hash: design/gdd/item-trigger-and-cooldown.md 4843a2ef63c965f705a87bd96520dd4d7599e035
Reviewed-Content-Hash: design/gdd/balance-simulation-harness.md 2eceb7c008b744ffe68fa17d5868bb4fe5d08e1a
Reviewed-Content-Hash: design/gdd/enemy-and-intent.md b6fcdf5e2bcd2b85d8e40ae932b1fcf3d90c3dfe
Reviewed-Content-Hash: design/gdd/payoff-presentation.md 854095d1e79ae0f2be66318da90a1b8a6e931812
Reviewed-Content-Hash: design/gdd/post-combat-breakdown.md 61a521a85e3c22b2ca886c8c3a412d7ef9389a76
Reviewed-Content-Hash: design/gdd/run-state-and-flow.md c537a9a473db1679fe0f52cd117fcb388c214a64
Reviewed-Content-Hash: design/gdd/turn-order.md 4a292e562356d13db40fa6bf592f29d60f72fa48
Reviewed-Content-Hash: design/gdd/curses-and-trade-offs.md 050b8a1857d7e86a9816d6e7587bf5f651181aa2
Reviewed-Content-Hash: design/gdd/effect-resolution-model.md 5624328a8e34eccea27d8ed66a4371106ef812d8
Reviewed-Content-Hash: design/gdd/combat-resolution.md b751a6b7e57a2bd5d80c596b5957b3cb154da823
Reviewed-Content-Hash: design/gdd/grid-backpack.md 08864f21a84a82b44b593c15b22bea620f8dd69a
Reviewed-Content-Hash: design/art/art-bible.md 9be45eb884f2349bc256f05312d31ddecb87bc8e

## Review — 2026-11-14 (fourth review) — Verdict: MAJOR REVISION NEEDED
Review mode: full · Workflow: full
Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, producer, unity-specialist, creative-director (senior)
Blocking items: 8 | Recommended: 11
Summary: The fantasy, the five pillars and the loop stand. The verdict is about the document type: for four rounds the concept carried operational numbers (tester counts, thresholds, hours, schedules) that no owning document backs, and each round's specialists found faults in exactly those numbers. The creative director ruled the third-round escalation condition met, narrowly (new uncited figures: hours to see a 1-in-20 run, the tester ceiling, the damage range, the scripted-run count). The remedy is structural: the concept states intent, and every count, threshold and denominator moves to the playtest plan, System 35 or the /estimate result. The 8-section checklist does not apply to a concept document.
Prior verdict resolved: Partly (the third-round faults were in the numbers added to fix the second round; this round removed them)
Owner decisions (2026-11-14, fourth round; a separate set from earlier D1 to D4): (1) Skip fast-forwards to the next Detonation, plays it in full, then continues, which withdraws the third-round D3 "Surge and boss cannot be skipped" and needs an amendment of payoff-presentation Rules 11 and 20; (2) the record is relative to the boss, and the run-defining blow gets its own presentation beat in System 29 and System 30; (3) the run-defining share is a definition constant (registry default, effective floor set by the Detonation classifier), changed only with the creative director's sign-off; (4) run /estimate before any MVP figure is cited. Defaults applied without a question: the go/no-go numbers move to a playtest plan, the user decides kill or pivot with the creative director advising, and a rung-3 proxy test goes into the playtest plan.
Specialist disagreements: lean size (the third-round rule "nothing may call the lean small" was withdrawn; System 18's finite-pool correction gives 1.04, 1.10, 1.37 and the concept now claims no size either way); the 50 percent share (systems-designer wanted a lever, economy-designer called boss HP the same relabelling, the creative director kept it a definition constant); Pillar 3's main lever (drop_bias_cap in the concept, tier rows, boss HP and skill in System 18 Rule 11; the concept now cites System 35); whether the MVP not showing the Core Fantasy is a blocker (accepted as scope, with a proxy test); the MVP estimate (8 to 10 months against the producer's roughly 8 to 14; not stated until /estimate runs); D3 (withdrawn); delta review proposed by the producer (rejected, the skill forbids a partial review).
Findings:
- [BLOCKING] MVP Definition / Pillar 3 / Open Questions: operational numbers with no owning document (tester counts, 6 of 8 and 3 of 4, hours to see a rare run, damage range, scripted-run count) (removed; playtest plan not yet written)
- [BLOCKING] Core Loop / MVP item 6 / Design Risks: decision D3 contradicts payoff-presentation Rules 11 and 20 (replaced in the concept by decision 1; payoff spec amendment still to do)
- [BLOCKING] Core Fantasy / Pillar 3 ladder: the run-defining blow looks like any Detonation (fixed in text: relative record and its own beat; System 29 and System 30 still to specify)
- [BLOCKING] Pillar 1 / Pillar 3: lean text contradicts System 18's finite-pool correction; drop_bias_cap named as the main lever; floor and ceiling measured on different quantities; harness question 15 still carries the withdrawn 1.5 ceiling (fixed in the concept; harness and System 18 Rule 11 still to reconcile)
- [BLOCKING] Pillar 3: the 50 percent share described as "equivalent to boss HP" (false) and the run-defining definition omitted shield, clipping and the Detonation condition (fixed in text)
- [BLOCKING] Pillar 3 ladder: rungs written as expected counts and percentiles, so rung 1 cannot fail (fixed in text; System 35 bands still to do)
- [BLOCKING] Scope Risks / systems-index: the index says about 6 months, the concept 8 to 10, and the MVP has no definition of done (concept no longer states a figure; /estimate and index sync still to do)
- [BLOCKING] MVP go/no-go and kill/pivot: no aggregate rule, sample too small to give a verdict, denominators missing, empty-suite and bug-filter loopholes, hash-parity source unnamed (moved to the playtest plan as intent; plan still to write)
- [RECOMMENDED] Pillar 1: "pivoting costs something" is not shown in the MVP, and the lean starts later than "the third room" (reworded; real cost still to name)
- [RECOMMENDED] Design Risks: near-fixed item set is the base case, so "reachable through skill" collapses into "know the recipe"; Pillar 3 against Expression needs an owner ruling (listed; no ruling yet)
- [RECOMMENDED] Design Risks / first cuts: question 16 should not be deferred with question 15; the cuts do not cover a second 2x miss (reworded; milestone file to add a structural cut)
- [RECOMMENDED] Unique Hook: trigger only rewrote wording (design fallback of fewer heroes added; must hold under more than one bot policy)
- [RECOMMENDED] Technical Risks: engine-free core needs its own assembly with no engine references; UI spike sized by the ADR; managed-code variant parity; Dictionary iteration, sort and overflow rules for the determinism ADR (added or pointed to the ADR)
- [RECOMMENDED] MVP item 8: DEVELOPMENT_BUILD wording was an engine claim; it is a design choice (reworded)
- [RECOMMENDED] Door decision: "one more door" is wrong (no cash-out); the door criterion cannot show a trade-off on its own (reworded, evidence tied to the door GDD's metrics)
- [RECOMMENDED] Visual Identity tests: no thresholds in the concept (art bible 4.10 made normative; its numbers are still proposals)
- [RECOMMENDED] Retention: the near-miss readout is a Vertical Slice feature, so the MVP lacks its main retention hook (not changed)
- [RECOMMENDED] Pillar design tests: several name a strawman or a tier absent from the MVP (Gold door frequency fixed; the others unchanged)
- [RECOMMENDED] Kill or pivot decider: the creative director is an agent the user runs (reworded: the user decides, the creative director advises)
Revision note: the revised concept is not shorter than before (about 54 KB against 49 KB); it removed the numbers but added explanation, so the next review should check that it is still within what the document type can carry. The pre-revision text is not in git (the third-round changes were uncommitted); a copy was kept in the session scratchpad.
Reviewed-Content-Hash for design/gdd/game-concept.md: not stamped on purpose. The document was revised after this review, so the next run must review it in full.
Reviewed-Content-Hash: design/registry/entities.yaml 7e57d0c08597fafafe42c4d4f9968b80d51a9165
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/systems-index.md f34d7626ef7634ad503dab6969dec1f2f3489430
Reviewed-Content-Hash: design/gdd/payoff-presentation.md 854095d1e79ae0f2be66318da90a1b8a6e931812
Reviewed-Content-Hash: design/gdd/commitment-and-drop-model.md 3e3c366885382ecd878a538dcaed220bbda220b2
Reviewed-Content-Hash: design/gdd/balance-simulation-harness.md 2eceb7c008b744ffe68fa17d5868bb4fe5d08e1a
Reviewed-Content-Hash: design/gdd/door-generation-and-tiers.md b1e60e157db6a19f6e80cbd96e31292ec581e702
Reviewed-Content-Hash: design/gdd/enemy-and-intent.md b6fcdf5e2bcd2b85d8e40ae932b1fcf3d90c3dfe
Reviewed-Content-Hash: design/gdd/post-combat-breakdown.md 61a521a85e3c22b2ca886c8c3a412d7ef9389a76
Reviewed-Content-Hash: design/gdd/party-and-heroes.md 40ef0a45512a8154fdb904b246ca0b65768aa12f
Reviewed-Content-Hash: design/gdd/item-trigger-and-cooldown.md 4843a2ef63c965f705a87bd96520dd4d7599e035
Reviewed-Content-Hash: design/gdd/turn-order.md 4a292e562356d13db40fa6bf592f29d60f72fa48
Reviewed-Content-Hash: design/gdd/run-state-and-flow.md c537a9a473db1679fe0f52cd117fcb388c214a64
Reviewed-Content-Hash: design/gdd/effect-resolution-model.md 5624328a8e34eccea27d8ed66a4371106ef812d8
Reviewed-Content-Hash: design/gdd/grid-backpack.md 08864f21a84a82b44b593c15b22bea620f8dd69a
Reviewed-Content-Hash: design/gdd/curses-and-trade-offs.md 050b8a1857d7e86a9816d6e7587bf5f651181aa2
Reviewed-Content-Hash: design/gdd/combat-resolution.md b751a6b7e57a2bd5d80c596b5957b3cb154da823
Reviewed-Content-Hash: design/art/art-bible.md 9be45eb884f2349bc256f05312d31ddecb87bc8e

## Review — 2026-11-15 (fifth review) — Verdict: NEEDS REVISION
Review mode: full · Workflow: full
Scope signal: L
Specialists: game-designer, systems-designer, economy-designer, qa-lead, producer, unity-specialist, creative-director (senior)
Blocking items: 8 | Recommended: 8
Summary: The vision, the five pillars and the loop hold, and the round-4 remedy (no new uncited numbers) worked: the creative director ruled the escalation condition NOT met. What remains is one false cross-reference (door buff described as max HP only; door Rule 6 also opens a cell for Purple), contradictions caused by sister GDDs changing after the concept, a document that grew instead of shrinking (about 54.7 KB, with a 37-line review-history preamble and ADR-level technical paragraphs), and owner rulings deferred for several rounds. The next revision must be a cut, not an addition. If a sixth round finds the document larger, or carrying restated schedule or threshold figures, it escalates to MAJOR REVISION NEEDED. The 8-section checklist does not apply to a concept document.
Prior verdict resolved: Partly (round-4 number removal held; hero order, Purple cost, 1.5 ceiling, Skip and schedule figures confirmed resolved; the document-size check requested in round 4 FAILED)
Specialist disagreements: blocking labels from game-designer (G1 Core Fantasy untestable in MVP, G2 surprise vs confirmation, G4 hook levers), systems-designer (S1 ladder bands), qa-lead (Q3 to Q5) and producer (P1, P3) were downgraded or moved to their owning documents (playtest plan, harness, mvp.md, determinism ADR) by the creative director. qa-lead's closed criterion list and the producer/game-designer call for a shorter go/no-go block were reconciled as one line per criterion (name, GATED or REPORTED, owning metric) with mechanics moved to a playtest-plan stub.
Owner decisions still open (only the user can make them): (1) Pillar 3 vs Expression: "rare = executing a known recipe" (re-rank aesthetics) or "several distinct run-defining recipes must exist" plus a named fallback lever; (2) is the payoff a surprise or a confirmation (a pre-combat damage forecast or not); (3) pivot cost: free in the MVP by design, or charge for stash moves; (4) keep or remove Purple's extra cell (door Rule 6); (5) Pillar 5 in the pitch: demote for the MVP or adopt the Purple cell-lock price; (6) before day 0, in mvp.md: confirm the definition of done and pick the structural cut with its milestone-1 trigger.
Findings:
- [BLOCKING] Key Dynamics (line 136): "the buff is a max-HP bonus only (door Rule 6)" is false; Rule 6 gives Purple +12 and one extra open cell; contradicts line 400 on capacity
- [BLOCKING] Pillar 3 (line 269): lean called "a minor modifier", contradicting Pillar 1 (lines 225-227) and System 18's provisional 1.04 to 1.37
- [BLOCKING] First cuts (line 476) vs Design Risks (line 400) vs MVP Definition (line 512): cut 1 defers Q15 and Q16 though both other places need them in the MVP
- [BLOCKING] Checkpoints (line 487): "about ten weeks" and "needs a date" disagree with mvp.md (week 11, week 27); restated schedule figures
- [BLOCKING] MVP item 8 (line 469): a symbol "set by the build script" cannot stop a manual build losing the export; Visual Identity (line 339): art bible 4.10 holds only the squint test, not the tier-separation or freeze-frame metrics; lines 403 and 441 are stale against Next Steps
- [BLOCKING] Pillar 3 vs Expression: owner ruling still open after three rounds; Q16 counts final-build fingerprints, not run-defining item sets; Core Fantasy line 66 "biggest blow ever dealt" contradicts the relative record at line 70
- [BLOCKING] Go/no-go (lines 489-514): the closed criterion set lives in a playtest plan that does not exist; GATED vs REPORTED is ambiguous for "hook is felt" and "fantasy proxy"
- [BLOCKING] Document type: 54.7 KB, 37-line review-history preamble, dated inline review tags, ADR-level determinism and UI paragraphs (lines 410, 418-419), playtest-plan mechanics; target about 35 KB
- [RECOMMENDED] Payoff as surprise or confirmation: combat is a pure function of what the player sees; no forecast decision
- [RECOMMENDED] Unique Hook: real cross-hero levers are the speed sum and Reaction placement; Reactions resolve in the same root action; Q14 can be won by action economy; add ablations
- [RECOMMENDED] Payoff ladder: rungs 1 and 2 cannot fail under System 29's thresholds (Chain = N>=2); harness has no ladder bands; one denominator needed; shield, the "share still left" clause and the boss-alone assumption in the run-defining definition
- [RECOMMENDED] Near-miss claim overstates System 30 Rule 12 (it shows recorded inputs vs thresholds, not what was missing)
- [RECOMMENDED] Technical Risks: IL2CPP asserted as shipped backend without a source; Managed Code Variant of 6.6 not mentioned; Scripts/Core already holds engine-dependent code; Editor-open batchmode note is a user rule
- [RECOMMENDED] Door bet: item-rows-flattened ablation (Q4) missing from evidence; Purple droppable pool may be 0 to 1; "a curse lowers the run-defining rate" unproven; pivot cost still not shown
- [RECOMMENDED] Testability: rule-to-test trace table and golden run set for hash parity; evidence-type mapping for MVP items 1 to 8; tier test says six tiers but the MVP has four; photosensitivity precondition contradicts the reduced-flashes default
- [RECOMMENDED] Scope: no structural cut for a second 2x overrun (cuts recover about 34 days against a 201-day gap); mvp.md definition of done omits Systems 6, 7, 31, 35, 37; uncommitted work (production/milestones untracked; round-4 text already lost from git)
Reviewed-Content-Hash: design/gdd/game-concept.md 759a1e12baa155608682062a360ebe2fb159f734
Reviewed-Content-Hash: design/registry/entities.yaml 881f39cbb8579f2eb1c18c9116794e956bd067fd
Reviewed-Content-Hash: .claude/skills/design-review/SKILL.md a63d8fba9b7142eeb43791f061e2e1533c429b43
Reviewed-Content-Hash: design/gdd/systems-index.md 55ce0b9cf15b977381745aff2fac535aed1ad094
Reviewed-Content-Hash: design/gdd/door-generation-and-tiers.md b1e60e157db6a19f6e80cbd96e31292ec581e702
Reviewed-Content-Hash: design/gdd/payoff-presentation.md 0d91b6571984645c45e372293cd9c4bd3829a719
Reviewed-Content-Hash: design/gdd/post-combat-breakdown.md 61a521a85e3c22b2ca886c8c3a412d7ef9389a76
Reviewed-Content-Hash: design/gdd/commitment-and-drop-model.md a8b87de85eb21027b7eced819154844631310e71
Reviewed-Content-Hash: design/gdd/balance-simulation-harness.md 5ad0a8f7c38f6872db29260f3bd67713ff39ba1b
Reviewed-Content-Hash: design/gdd/party-and-heroes.md 40ef0a45512a8154fdb904b246ca0b65768aa12f
Reviewed-Content-Hash: design/gdd/item-trigger-and-cooldown.md 4843a2ef63c965f705a87bd96520dd4d7599e035
Reviewed-Content-Hash: design/gdd/turn-order.md 4a292e562356d13db40fa6bf592f29d60f72fa48
Reviewed-Content-Hash: design/gdd/enemy-and-intent.md b6fcdf5e2bcd2b85d8e40ae932b1fcf3d90c3dfe
Reviewed-Content-Hash: design/gdd/encounter-generation.md cbcc3ec5bad1665f5d35732eb951ea4ebb0507de
Reviewed-Content-Hash: design/gdd/combat-resolution.md b751a6b7e57a2bd5d80c596b5957b3cb154da823
Reviewed-Content-Hash: design/gdd/grid-backpack.md 08864f21a84a82b44b593c15b22bea620f8dd69a
Reviewed-Content-Hash: design/gdd/run-state-and-flow.md 821afd02a07db5a67d97230c356700f05445211d
Reviewed-Content-Hash: design/gdd/effect-resolution-model.md 5624328a8e34eccea27d8ed66a4371106ef812d8
Reviewed-Content-Hash: design/gdd/curses-and-trade-offs.md 050b8a1857d7e86a9816d6e7587bf5f651181aa2
Reviewed-Content-Hash: design/art/art-bible.md b36a69a3b58f567f6215c57ee4381fa4d0e663fe
Reviewed-Content-Hash: production/milestones/mvp.md 2a344b4016bbf1839fc610112b646cffecceaffe
