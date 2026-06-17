# Phase 2 — Combat System

**Read `CONTEXT.md` before starting. Phases 0-1 complete — extend, don't refactor.**

## Goal
Implement core combat: player can attack enemies, enemies chase and attack back, damage calculated from STR/DEX/VIG/INT stats. No loot drops (Phase 4), no gear stat modifiers (Phase 3) — just the raw combat loop.

## Tasks

### 1. Character Stats System
- `Scripts/Data/CharacterStats.cs` — Serializable struct: STR, DEX, VIG, INT (int values)
- `Scripts/Combat/StatsComponent.cs` — MonoBehaviour holding CharacterStats, exposes computed properties (MaxHP, PhysicalDamage, AttackSpeed, CritChance, Defense)
- Add `CharacterStats` field to `EnemyDefinitionSO` for base enemy stats

### 2. Health System
- `Scripts/Combat/Health.cs` — MonoBehaviour: currentHP, maxHP, TakeDamage(), OnDeath event
- Derives maxHP from StatsComponent (100 + VIG * 10)
- Player and enemies both use this component

### 3. Player Combat
- `Scripts/Combat/PlayerCombat.cs` — reads Attack action from InputSystem_Actions
- On attack: sphere overlap check (Physics.OverlapSphere) around player for enemies in melee range
- Apply damage using DamageCalculator
- Attack cooldown based on DEX (attack speed)
- Simple attack animation placeholder: brief scale pulse on the player model

### 4. Damage Calculator
- `Scripts/Combat/DamageCalculator.cs` — static utility class
- Physical damage: baseDamage * (1 + STR * 0.02)
- Crit chance: DEX * 0.005 (0.5% per point, capped at 50%)
- Crit multiplier: 1.5x
- Defense reduction: incomingDamage * (100 / (100 + targetVIG))
- Attack interval: 1.0 / (1 + DEX * 0.01) seconds

### 5. Enemy AI
- `Scripts/Combat/EnemyAI.cs` — simple state machine (Idle → Chase → Attack → Dead)
- Idle: stand still until player enters aggro range
- Chase: move toward player (NavMeshAgent or simple transform.Translate)
- Attack: deal damage to player when in melee range, on cooldown
- Dead: disable on HP <= 0

### 6. Flesh Out EnemyDefinitionSO
- Add: baseStats (CharacterStats), baseDamage (int), moveSpeed (float), aggroRange (float), attackRange (float)
- Keep existing fields (enemyId, displayName, tier, visualPrefab)

### 7. Placeholder Enemy Model
- Use Blender MCP: low-poly goblin/creature, shorter than player, reddish color
- Export FBX with bake_space_transform=True per CODING_STANDARDS.md
- Import to Assets/Art/Models/EnemyPlaceholder.fbx

### 8. Scene Setup & Editor Script
- Update Phase1SceneSetup (or create Phase2 addition) to spawn a test enemy in Homestead scene
- Enemy should have: EnemyAI, Health, StatsComponent, CharacterController or simple collider

### 9. Verification
- Compile check — zero errors
- Play Mode self-test per CODING_STANDARDS.md Pre-Report Protocol
- Player can attack enemy, enemy HP decreases, enemy dies
- Enemy chases player, attacks back, player HP decreases
- Commit as: `[Phase 2] Combat system - stats, health, damage calc, player attack, enemy AI`

## Do NOT
- No loot drops (Phase 4)
- No gear stat modifiers applied to combat yet (Phase 3)
- No multiple enemy types/spawning systems yet (Phase 7-8)
- No death/respawn UI yet — just log to console for now
