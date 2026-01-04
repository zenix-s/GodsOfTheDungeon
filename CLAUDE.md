# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GodsOfTheDungeon is a 2D dungeon-crawler action game built with **Godot 4.5** and **C# (.NET 8.0)**.

## Build & Run Commands

```bash
# Build the project (from project directory)
dotnet build

# Run via Godot CLI
godot --path .

# Export (example for Linux)
godot --headless --export-release "Linux/X11" build/game.x86_64
```

No testing framework is currently configured.

## Architecture

### Autoload Singletons
- **GameManager** (`Autoloads/GameManager.cs`) - Global game state, player data, signals for coins and player death
- **SceneManager** (`Autoloads/SceneManager.cs`) - Level and menu transitions via signals

### Core Systems
- **Main** (`Core/Main.cs`) - Main scene controller, handles level loading and camera follow
- **DamageCalculator** (`Core/Systems/DamageCalculator.cs`) - Combat damage formula

### Component Pattern
- **HitBox** (`Core/Components/HitBox.cs`) - Deals damage on collision, stores `AttackData` and owner stats
- **HurtBox** (`Core/Components/HurtBox.cs`) - Detects HitBox collisions, emits `HitReceived` signal with DTOs
- **HealthComponent** (`Core/Components/HealthComponent.cs`) - Manages HP, invincibility frames, death

### Interfaces
- `IGameEntity` - Entity with stats (`EntityStats Stats`)
- `IEnemy` - AI detection (`OnPlayerDetected`, `OnPlayerLost`)
- `IInteractable` - Player interaction
- `ICollectible` - Collectible items

### Entity Classes
Entities extend `CharacterBody2D` directly and implement interfaces as needed:
- **Player** (`Scenes/Prefabs/Entities/Player/Player.cs`) - Implements `IGameEntity`. Has invincibility frames via HealthComponent.
- **Slime** (`Scenes/Prefabs/Entities/Enemies/Slime/Slime.cs`) - Implements `IGameEntity, IEnemy`. No invincibility.

### Signal Flow
GameManager emits: `CoinsChanged`, `PlayerDied`
SceneManager emits: `LevelChangeRequested`, `MenuChangeRequested`

## Physics Collision Layers (2D)

| Layer | Name | Purpose |
|-------|------|---------|
| 1 | World | Platforms, terrain |
| 2 | Player | Player body |
| 3 | Enemies | Enemy bodies |
| 4 | Collectibles | Coins, items |
| 5 | Interactables | Doors, switches |
| 6 | PlayerDamageBox | Player's hurtbox |
| 7 | EnemyDamageBox | Enemy hurtboxes |
| 8 | PlayerAttack | Player's hitbox |
| 9 | EnemyAttack | Enemy hitboxes |
| 10 | Projectiles | Ranged attacks |

## Input Actions

| Action | Keys |
|--------|------|
| move_left | A, Left Arrow |
| move_right | D, Right Arrow |
| ui_accept | Space (jump) |
| attack_1 | J |
| attack_2 | K |
| attack_3 | L |
| interact | E |

## Directory Structure

```
Autoloads/       - Global singletons
Core/
  Components/    - Reusable components (HitBox, HurtBox)
  Data/          - Data structures (EntityStats, AttackData, DamageResult)
  Enums/         - Game enumerations
  Interfaces/    - Contracts (IGameEntity, IEnemy, IInteractable, ICollectible)
  Systems/       - Game systems (DamageCalculator)
Scenes/
  Levels/        - Level scenes
  Prefabs/       - Reusable game objects (Player, Enemies, Interactables)
  Menus/         - Menu scenes
  UI/            - HUD and UI components
```

## Damage Formula

```
rawDamage = Attacker.Attack × BaseDamage × DamageMultiplier
finalDamage = max(1, rawDamage - Defense)
if crit: finalDamage *= CriticalMultiplier
knockback = KnockbackForce × (1 - KnockbackResistance)
```
