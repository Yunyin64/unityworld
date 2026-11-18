# Output Package Reference

Use this reference when generating game design deliverables with `game-design-planner`.

## Folder Structure

```text
game-design-{slug}/
  00_intake.md
  01_gdd.md
  02_core_loop_and_systems.md
  03_numerics.md
  04_content_plan.md
  05_world_and_story.md
  06_art_audio_direction.md
  07_copy_and_localization.md
  08_prototype_review.md
  09_test_plan.md
  tables/
    economy.csv
    progression.csv
    items.csv
    levels.csv
    missions.csv
    enemies.csv
    abilities.csv
    localization_zh-CN.csv
    telemetry_events.csv
  configs/
    economy_config.json
    item_config.json
    level_config.json
    mission_config.json
    telemetry_config.json
```

Omit files that do not apply, but explain omissions in `00_intake.md`.

## 00_intake.md

Include:

- Project name and working slug
- Inputs received: genre, theme, platforms, audience, references, art style, art requirements, prototype materials
- Assumptions made
- Open questions
- Prototype/source inventory
- Output scope and language

## 01_gdd.md

Use these sections:

- One-line concept
- Player fantasy
- Target platform and audience
- Design pillars
- Unique selling points
- Core loop
- Session structure
- Game modes
- Controls and camera
- Progression
- Economy and rewards
- Content structure
- Onboarding
- Monetization, if applicable
- Accessibility
- Risks and mitigations
- MVP definition

## 02_core_loop_and_systems.md

Specify:

- Player verbs
- Moment-to-moment loop
- Short, mid, and long-term goals
- Rule systems
- Input/control mapping
- Feedback and juice requirements
- Difficulty model
- Failure and recovery
- Retention hooks
- Social/competitive/cooperative systems, if applicable

## 03_numerics.md

Include formulas and initial values. Use tables for:

- Player attributes
- Enemy or obstacle attributes
- Level difficulty curve
- Reward curve
- Currency sources and sinks
- Time gates or energy, if applicable
- Upgrade costs
- Drop rates
- Score calculation
- Win/loss thresholds

Label each row with:

- `id`
- `name`
- `value`
- `formula`
- `design_intent`
- `tuning_status` (`initial`, `tested`, `locked`)

## 04_content_plan.md

Define:

- Content categories
- MVP content list
- Post-MVP expansion list
- Level/mission structure
- Item/ability/enemy taxonomy
- Unlock order
- Content production estimates
- Reuse rules and variation rules

## 05_world_and_story.md

For narrative-heavy games, include:

- World premise
- Core conflict
- Factions
- Main characters
- Player role
- Story arc
- Chapter beats
- Quest templates
- Dialogue tone
- Lore rules

For light narrative games, keep this short and focus on naming, setting flavor, and tone.

## 06_art_audio_direction.md

Include:

- Visual pillars
- Camera/framing
- Color and lighting direction
- Character, environment, UI, VFX, and icon requirements
- Asset list by priority
- Animation principles
- Audio pillars
- Music style
- SFX list
- Platform-specific readability constraints

Do not rely on vague labels like "cartoon style" alone. Translate style into shapes, line weight, palette, material, proportions, animation timing, UI density, and readability rules.

## 07_copy_and_localization.md

Include:

- Game title options
- Tagline options
- Store short description
- Store long description
- Tutorial copy
- Button and menu copy
- System messages
- Item/level/mission names
- Error and empty-state copy
- Localization constraints: text expansion, cultural risks, terminology glossary

## 08_prototype_review.md

Include only when prototype material exists:

- Materials reviewed
- What is already implemented
- What the prototype proves
- What remains unproven
- Controls and feel notes
- Code/data/content structure observations
- Design risks found
- Recommended next prototype tasks
- Acceptance criteria for the next build

## 09_test_plan.md

Include:

- QA smoke tests
- Gameplay functional tests
- Balance tests
- UX/onboarding tests
- Compatibility tests by platform
- Accessibility tests
- Monetization/economy tests, if applicable
- Playtest script
- Survey questions
- Telemetry events
- Success metrics and thresholds

## CSV Schemas

### economy.csv

```csv
id,currency,source_or_sink,action,amount,frequency,formula,design_intent,tuning_status
```

### progression.csv

```csv
id,stage,unlock_condition,reward,difficulty_target,expected_session_count,design_intent,tuning_status
```

### items.csv

```csv
id,name,type,rarity,unlock_condition,cost,effect,upgrade_path,design_intent,tuning_status
```

### levels.csv

```csv
id,name,biome,duration_sec,objective,difficulty,spawn_rules,rewards,fail_conditions,design_intent,tuning_status
```

### missions.csv

```csv
id,name,type,trigger,objective,reward,dialogue_key,repeatable,design_intent,tuning_status
```

### enemies.csv

```csv
id,name,role,hp,damage,speed,attack_pattern,counterplay,spawn_stage,design_intent,tuning_status
```

### abilities.csv

```csv
id,name,input,cooldown_sec,cost,effect,upgrade_values,feedback,design_intent,tuning_status
```

### localization_zh-CN.csv

```csv
key,context,zh-CN,max_length,tone,notes
```

### telemetry_events.csv

```csv
event_name,trigger,properties,success_metric,privacy_note
```

## Config Guidelines

Configs should mirror CSV ids and remain easy for engineers to parse. Prefer stable ids, numeric values without units in field names when possible, and explicit version fields.

Example:

```json
{
  "version": "0.1.0",
  "currency": {
    "soft": {
      "initial_balance": 100,
      "sources": [
        {"id": "level_clear", "amount": 25, "daily_cap": 500}
      ],
      "sinks": [
        {"id": "basic_upgrade", "base_cost": 50, "cost_formula": "base_cost * pow(1.35, level)"}
      ]
    }
  }
}
```
