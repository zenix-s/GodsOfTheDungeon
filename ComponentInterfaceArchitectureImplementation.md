## System Design: Interface Orchestration

The logical flow follows this chain:

1. **Detection:** A physical component (e.g., `HurtBox`) detects an interaction.
2. **Notification:** The component looks for the required interface in its parent (`IDamageable`) and calls the method.
3. **Mediation:** The Entity receives the call and decides which child components should act.
4. **Execution:** Logic components (e.g., `HealthComponent`) perform the heavy lifting.

---

## Component Specification

### 1. Data Layer (Resources)

We create an immutable (or semi-immutable) data object to define the entity's identity.

```csharp
public partial class EntityStats : Resource
{
    [Export] public int MaxHealth = 100;
    [Export] public float MovementSpeed = 300.0f;
    [Export] public int BaseDamage = 10;
}

```

### 2. Contract Layer (Interfaces)

Define "What" the entity can do, hiding the "How".

```csharp
public interface IDamageable
{
    void TakeDamage(AttackData attack);
}

```

### 3. The Mediator (Entity Script)

Implements the interfaces and coordinates child nodes.

```csharp
public partial class Player : CharacterBody2D, IDamageable
{
    [Export] public EntityStats Stats;

    // References to child components
    private HealthComponent _health;
    private AnimationComponent _anim;

    public override void _Ready()
    {
        _health = GetNode<HealthComponent>("HealthComponent");
        _anim = GetNode<AnimationComponent>("AnimationComponent");

        _health.Initialize(Stats.MaxHealth);
    }

    public void TakeDamage(AttackData attack)
    {
        // The mediator coordinates the response
        _health.ApplyDamage(attack.Amount);
        _anim.PlayHurtEffect();

        if (_health.IsDead) Die();
    }
}

```

### 4. Input Components (Detection)

Look for the interface in the parent to report events.

```csharp
public partial class HurtBoxComponent : Area2D
{
    private IDamageable _mediator;

    public override void _Ready()
    {
        // Validate that the system is correct at mount time
        if (GetParent() is IDamageable damageable)
            _mediator = damageable;
        else
            GD.PrintErr($"{Owner.Name}: HurtBox without IDamageable parent");
    }

    // Called by an external AttackComponent
    public void NotifyHit(DamageInfo info)
    {
        _mediator?.TakeDamage(info);
    }
}

```

---

## 📋 Implementation Plan (Step by Step)

### Phase 1: Core Definition

* [ ] Create `Core` folder and define common `Records` (like `AttackData`).
* [ ] Define interfaces `IDamageable`, `IAttacker`, `IMovable` in separate files.
* [ ] Create the `EntityStats` class inheriting from `Resource`.

### Phase 2: Atomic Components

* [ ] Create `HealthComponent` (Node): Only manages a `currentHealth` variable and functions `ApplyDamage(AttackData attack)`/`Heal(...)`.
* [ ] Create `HurtBoxComponent` (Area2D): Configure collision layers (Layer: Hurtboxes) and the logic to find the interface in the parent.
* [ ] Create `HitBoxComponent` (Area2D): For attacks, looks for a `HurtBoxComponent` on overlap.
