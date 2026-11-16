# Gate Check: Concept → Systems Design

**Date**: 2026-11-13
**Checked by**: gate-check skill
**Tier**: workflow full · review_mode full · qa.level full · team.size studio
**Stage at check**: Concept (project.yaml and production/stage.txt)

> Retroactive gate: the project is already in the middle of Systems Design
> (systems index written, 23+ GDDs started, 0 approved). This gate was run late.

## Required Artifacts: 3/3 present
- [x] `design/gdd/game-concept.md` — exists with content (Status: Draft)
- [x] Game pillars — 5 pillars + 4 anti-pillars (`## Game Pillars`, `### Anti-Pillars`)
- [x] Visual Identity Anchor — `## Visual Identity Anchor`

## Recommended (not blocking)
- [ ] Concept prototype with REPORT.md (PROCEED) — **none**. `prototypes/` is empty; the user confirmed the core loop has not been playtested.

## Quality Checks: 3/4 passing
- [x] Core loop described (`## Core Loop`)
- [x] Target audience identified (`## Target Player Profile`; several fields marked assumption)
- [x] Visual Identity Anchor has a one-line rule and 4 supporting principles
- [~] Concept `/design-review` — user-reported verdict **NEEDS REVISION** (run outside the repo; points sent back to the authoring session). Not MAJOR REVISION, so the gate threshold is met, but no report is retained in the repo and no re-review has happened.

## Director Panel Assessment (4 of 4 ran)
- Creative Director: CONCERNS — pillars strong; no playtest evidence, concept re-review pending, Pillar 5 not testable in MVP, "committed" (Pillar 1) still undefined, Pillar 3 N≈20 provisional.
- Technical Director: CONCERNS — nothing blocks entry; combat-core spike not done; legacy prototype (float gauges, `TickCount` seed, hardcoded strings) is not evidence; Unity 6.6 is not LTS.
- Producer: CONCERNS — gate is retroactive; 23 GDDs started, 0 approved; concept sequencing ("only if the prototype PROCEEDS") skipped; concept timeline (4-5 months) disagrees with PR-SCOPE (~6 months).
- Art Director: READY — advisories: anchor has drifted from art bible §1.3; reference screenshots not in repo; art bible header stale about accessibility doc; cell size provisional.

## Blockers
None. No check was NOT ASSESSED.

## Recommendations
1. Re-run `/design-review design/gdd/game-concept.md` after the revisions and keep the report in the repo.
2. Run the combat-core spike (`/prototype`: apply status then detonate across two heroes) before approving more core GDDs (3, 12, 36).
3. Clear the review backlog (Systems 1, 2, 3, 4, 8, 12, 13, 36) and run `/review-all-gdds`.
4. Update the concept: Status, unchecked `/art-bible` item, MVP timeline to match PR-SCOPE, pointer to art bible §1.3.
5. Store the reference screenshots under `design/art/references/`; record that Pillar 5 first becomes testable at Tier 1.5.

## Verdict: CONCERNS

Stage unchanged (Concept). No stage write was requested or made.

Chain-of-Verification: 5 questions checked (2 by tool action: heading re-scan of game-concept.md, search for REPORT.md and gate-checks directory) — verdict unchanged.
