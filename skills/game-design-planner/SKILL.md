---
name: game-design-planner
description: Create complete game design packages from user-provided game concept inputs. Use when the user asks for a game planner/designer skill, GDD, game design document, gameplay systems, balance sheets, configuration tables, narrative/worldbuilding, art direction, platform/audience design, prototype/code review for design implications, localization copy, QA plans, playtest scripts, or a structured game design deliverable based on genre, theme, target platform, audience, reference games, art style, and prototype materials.
---

# Game Design Planner

## Overview

Use this skill to turn an early game concept into a practical design package that can guide prototype, production, art, content, economy, narrative, and QA work.

Default to producing files, not only an answer in chat. Keep the package implementation-ready: concrete mechanics, tables, formulas, config schemas, acceptance criteria, and test cases.

## Intake

Collect or infer these inputs before writing final deliverables:

- Game type/genre and subgenre
- Theme/fantasy and emotional target
- Release platform(s) and device constraints
- Target audience, age range, region, spending model, and session pattern
- Reference games, with what to borrow and what to avoid
- Art style and detailed art requirements
- Prototype status: none, idea only, playable build, code repository, local files, screenshots, video, or external link
- Business model: premium, free-to-play, ads, IAP, subscription, UGC, or mixed
- Team constraints, schedule, target scope, engine, and content budget when available
- Required output language and format

If key inputs are missing, make conservative assumptions and record them in `00_intake.md`. Ask only when a missing answer changes the design direction materially, such as platform, audience, monetization, or whether a prototype must be reviewed.

## Prototype Handling

If the user provides prototype files, repository paths, or links:

1. Inspect the materials before designing around them.
2. Identify implemented mechanics, missing systems, technical constraints visible from the prototype, control feel, content structure, data formats, and production risks.
3. Separate facts observed in the prototype from design recommendations.
4. Include a `08_prototype_review.md` deliverable with findings, design implications, and next prototype tasks.

If the prototype cannot be accessed, state the blocker and continue with assumptions. Do not claim to have reviewed inaccessible code or links.

## Workflow

1. **Frame the pitch**: define the core promise, target player, 30-second loop, 5-minute loop, and long-term motivation.
2. **Analyze references**: map reference games into mechanics, economy, UX, art, narrative, and market positioning. Use current research when the reference is unfamiliar, recently updated, or commercially important.
3. **Design core systems**: specify controls, player verbs, progression, fail states, rewards, content structure, onboarding, retention, and accessibility.
4. **Build numbers**: create first-pass tuning values with formulas and reasons. Mark all speculative values as `initial`.
5. **Create configs**: provide machine-readable tables for gameplay, economy, items, levels, missions, dialogue, and localization.
6. **Write content**: produce UI copy, item names, quest text, narrative beats, world rules, character/setting notes, and tone guidelines.
7. **Plan validation**: define QA cases, playtest tasks, metrics, telemetry events, success thresholds, and iteration priorities.
8. **Package outputs**: write all deliverables into a clear folder structure and summarize assumptions, risks, and next decisions.

## Output Package

Create a project folder named `game-design-{slug}` unless the user names another destination. Use Markdown for documents, CSV for spreadsheet-friendly tables, and JSON/YAML for configs.

Required deliverables:

- `00_intake.md`: user inputs, assumptions, open questions, source/prototype inventory
- `01_gdd.md`: concise but complete game design document
- `02_core_loop_and_systems.md`: mechanics, modes, progression, rewards, UX flow
- `03_numerics.md`: formulas, balance rationale, economy model, tuning notes
- `04_content_plan.md`: levels, missions, items, enemies, abilities, unlocks, content cadence
- `05_world_and_story.md`: world rules, characters, narrative arc, quest/dialogue direction
- `06_art_audio_direction.md`: visual pillars, art references, UI style, asset list, audio direction
- `07_copy_and_localization.md`: UI copy, tutorial text, store copy, naming rules, localization notes
- `08_prototype_review.md`: only when prototype/code/link/files were supplied
- `09_test_plan.md`: QA matrix, playtest script, telemetry, acceptance criteria
- `tables/`: CSV files for numeric and content tables
- `configs/`: JSON or YAML config examples matching the tables

For exact templates and table schemas, read `references/output-package.md`.

## Quality Bar

- Make the first playable experience explicit: objective, controls, win/loss, feedback, and a 3-5 minute demo path.
- Keep the design scoped to the target platform and team constraints.
- Explain important numbers with formulas or intent, not just raw values.
- Include edge cases: failure, abuse prevention, difficulty spikes, low-end device constraints, localization expansion, content exhaustion, and accessibility.
- Mark uncertain claims and assumptions clearly.
- Prefer structured tables over paragraphs for tunable values.
- Avoid copying reference games wholesale; state the unique angle and differentiation.
- End with concrete next steps for prototype, content, art, economy, and QA.
