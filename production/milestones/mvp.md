# Milestone: MVP (one floor, 28 systems)

*Created: 2026-11-14 from `/estimate`*
*Status: Draft. Confidence: **Low**. Re-run `/estimate` at the combat core checkpoint.*

This file **owns the MVP schedule figures**. `design/gdd/game-concept.md` and
`design/gdd/systems-index.md` cite it and do not restate the numbers (concept
fourth-review ruling: the concept states intent only).

Day 0 is the start of the combat core spike. Unit: working days for one full-time
person, 21.7 working days per month, no leave or holidays.

## Basis and the assumption that decides the answer

There is no sprint history (`production/sprints/` does not exist), so this estimate is
**not calibrated to measured velocity**. The AI-assisted columns assume Claude writes much
of the code and the user reviews it, at about 2.5x the unassisted speed for the engine-free
core, data and harness, about 1.5x for UI and setup, and no speed-up for content, tuning
and playtests. These multipliers are assumptions, not measurements. The cheapest place to
replace them with a measurement is the combat core checkpoint.

## Estimate by work package

| # | Work package | Unassisted | Optimistic | Expected | Pessimistic |
|---|---|---|---|---|---|
| 1 | Architecture ADRs, asmdef split, dotnet harness shell, UI spike | 14 | 7 | 10 | 18 |
| 2 | Engine-free core (systems 1, 2, 4, 36, 3, 13, 9, 10, 8, 11, 12, 14, 15) | 98 | 28 | 40 | 75 |
| 3 | Run structure (5, 16, 17, 18) | 26 | 8 | 11 | 20 |
| 4 | Content (20 items, 4 heroes, 7 monsters, tier art) | 34 | 20 | 28 | 48 |
| 5 | Input, localization, settings, menus (6, 7, 37, 31) | 25 | 12 | 17 | 30 |
| 6 | Backpack UI, popup, HUD, door panels (25, 26, 27, 28) | 39 | 18 | 26 | 48 |
| 7 | Payoff and breakdown with 4 placeholder cues (29, 30) | 23 | 10 | 15 | 30 |
| 8 | Thin harness and Competent bot (35) | 15 | 5 | 7 | 16 |
| 9 | Hash parity, command-log export and replay, IL2CPP build | 11 | 5 | 8 | 16 |
| 10 | Remaining design work (re-reviews, 6 UX specs, ADRs, Skip redesign, playtest plan) | 25 | 12 | 18 | 28 |
| 11 | Integration, defects, retained evidence (15% of packages 1 to 9) | 43 | 17 | 24 | 45 |
| 12 | Balance, two tuning passes, outside playtests | 40 | 26 | 40 | 66 |
| 13 | Forced re-pin to 6.7 and re-verification | 7 | 4 | 7 | 12 |
| | **Total (days)** | **400** | **172** | **251** | **452** |
| | **Calendar months** | **18.5** | **8** | **11.5** | **21** |

**Recommended budget: 251 working days (about 11.5 months), AI-assisted.** The earlier
figures it replaces: "about 6 months" in the systems index (below even the optimistic
case here) and "8 to 10 months" in the concept (decision D2 of the third review, which
matches the optimistic case here). Both are withdrawn.

## Milestones (AI-assisted, expected, from day 0)

| # | Milestone | Cumulative days | About week |
|---|---|---|---|
| 1 | Setup plus combat core checkpoint (36, 3, 12, 9, 11, 13, 14 across two heroes, identical event logs in tests and live play) | 28 | 6 (pessimistic: 12) |
| 2 | Greybox payoff test with a placeholder backpack | 55 | 11 |
| 3 | Full engine-free core, run structure and thin harness | 68 | 14 |
| 4 | First playable run of the whole loop | 135 | 27 |
| 5 | Tuning passes and outside playtests complete | 251 | 50 |

## First cuts: what each recovers (AI-assisted days, rough)

Order and conditions are in `design/gdd/game-concept.md` (MVP Definition). The recovery
figures are owned here.

| Cut | Days |
|---|---|
| 1. Defer harness questions 15 and 16 | 3 |
| 2. English-only text, keys kept | 4 |
| 3. Four regular monsters instead of six (needs System 14 and 16 changes) | 3 net |
| 4. No rebind UI | 3 |
| 5. No item rotation | 3 to 4 |
| 6. Drop the Purple tier | 6 |
| 7. Personal best for the session only | 2 |
| 8. Defer the development player build | 2 |
| 9. Skip only, no speed levels | 2 |
| 10. Three fixed heroes | 5 |
| **All ten together** | **about 34** |

All ten recover about 14% of the budget and cannot absorb the gap between the expected
and pessimistic cases. The milestone needs a **structural cut** as well. One unverified
candidate: a "cheap MVP" of the core, run structure, harness, greybox UI, about 12 items
and the tuning passes, roughly 150 to 170 days. It answers H1 sooner but gives up the
readable UI and persistence. Choosing one is the owner's call.

## Definition of done (proposed; the owner confirms)

The MVP is done when all of these hold:
1. The eight "Required for MVP" items in the concept are built.
2. Every gated criterion in the playtest plan (`production/qa/`, not yet written) has been
   **assessed**. A NOT ASSESSED reading means the MVP is not done, because it never
   passes. H2, the Unique Hook comparison and the lean ceiling are reported, not gated.
3. The user has recorded a go, pivot or kill decision, with the creative director advising
   (concept fourth-review default).

## Risks that move the date

| Risk | Impact |
|---|---|
| AI-assisted speed-up is smaller than assumed (for example 1.5x on the core) | +25 to 45 days |
| Core checkpoint fails or needs a spec rework | +15 to 30 days, UI work blocked |
| Determinism parity (Mono, dotnet, IL2CPP) breaks late | +8 to 15 days |
| UI stack decided wrongly after a short spike | +10 to 20 days |
| Recruiting outside testers stalls | calendar delay, not effort |

## Open items before day 0

- Re-review of Systems 3, 12, 13, 36 and the Event Log contract.
- Redesign of Skip in `payoff-presentation.md` (concept fourth-review decision).
- Determinism ADR and UI stack ADR (the UI spike is sized by the ADR).
- Playtest plan in `production/qa/`, with a recruiting deadline.
- Photosensitivity check of the reduced-flashes default (payoff Rule 17).

## Re-estimate triggers

Re-run `/estimate` at the combat core checkpoint (replace the AI-assisted multipliers with
the measured velocity) and after each tuning pass.
