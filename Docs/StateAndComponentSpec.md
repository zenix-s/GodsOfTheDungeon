# State Machine & Component Manager Systems Specification

## Overview

Two Godot node-based systems for managing player behavior:

1. **State Machine** - Manages player states (Idle, Run, Jump, Fall, Attack)
2. **Component Manager** - Organizes logic in reusable, decoupled component nodes

Both use Godot's node architecture for editor visibility and simplicity.

---

## System 1: State Machine

### Architecture

```
Player (CharacterBody2D)
├── StateMachine (Node)
│   ├── Idle (PlayerIdleState)
│   ├── Run (PlayerRunState)
│   ├── Jump (PlayerJumpState)
│   ├── Fall (PlayerFallState)
│   └── Attack (PlayerAttackState)
└── ...
```

### Files

| File | Purpose |
|------|---------|
| `Core/StateMachine/StateMachine.cs` | Parent node that manages state transitions |
| `Core/StateMachine/State.cs` | Base class for all states |
| `Scenes/Prefabs/Entities/Player/States/PlayerIdleState.cs` | Idle behavior |
| `Scenes/Prefabs/Entities/Player/States/PlayerRunState.cs` | Run behavior |
| `Scenes/Prefabs/Entities/Player/States/PlayerJumpState.cs` | Jump behavior |
| `Scenes/Prefabs/Entities/Player/States/PlayerFallState.cs` | Fall behavior |
| `Scenes/Prefabs/Entities/Player/States/PlayerAttackState.cs` | Attack behavior |

---

### StateMachine.cs

Manages the current state and handles transitions.

```csharp
public partial class StateMachine : Node
{
    [Export] public State InitialState { get; set; }

    public State CurrentState { get; private set; }

    // Called by parent entity to inject owner reference
    public void Initialize(CharacterBody2D owner);

    // Transition to a new state
    public void TransitionTo(StringName stateName);
    public void TransitionTo(State state);

    // Delegates processing to current state
    public override void _PhysicsProcess(double delta);
    public override void _Input(InputEvent @event);
}
```

**Behavior:**
- On `Initialize()`: Calls `Initialize()` on all child states, then enters `InitialState`
- On `TransitionTo()`: Calls `Exit()` on current state, updates `CurrentState`, calls `Enter()` on new state
- Delegates `_PhysicsProcess` and `_Input` to `CurrentState`

---

### State.cs (Base Class)

Abstract base for all player states.

```csharp
public abstract partial class State : Node
{
    protected CharacterBody2D Owner { get; private set; }
    protected StateMachine StateMachine { get; private set; }

    // Called once when StateMachine initializes
    public virtual void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        Owner = owner;
        StateMachine = stateMachine;
    }

    // State lifecycle
    public virtual void Enter() { }
    public virtual void Exit() { }

    // Called every physics frame while this state is active
    public virtual void PhysicsUpdate(double delta) { }

    // Called for input events while this state is active
    public virtual void HandleInput(InputEvent @event) { }

    // Helper to request a state transition
    protected void TransitionTo(StringName stateName)
    {
        StateMachine.TransitionTo(stateName);
    }
}
```

---

### State Transition Rules

| From | To | Condition |
|------|-----|-----------|
| Idle | Run | `InputDirection != 0` |
| Idle | Jump | Jump pressed AND on floor |
| Idle | Fall | Not on floor |
| Idle | Attack | Attack input pressed |
| Run | Idle | `InputDirection == 0` AND on floor |
| Run | Jump | Jump pressed AND on floor |
| Run | Fall | Not on floor |
| Run | Attack | Attack input pressed |
| Jump | Fall | `Velocity.Y >= 0` |
| Jump | Attack | Attack input (air attack) |
| Fall | Idle | On floor AND `InputDirection == 0` |
| Fall | Run | On floor AND `InputDirection != 0` |
| Fall | Attack | Attack input (air attack) |
| Attack | Idle | Attack finished AND on floor AND no input |
| Attack | Run | Attack finished AND on floor AND has input |
| Attack | Fall | Attack finished AND not on floor |

---

### Example: PlayerIdleState

```csharp
public partial class PlayerIdleState : State
{
    private MovementComponent _movement;
    private AnimationComponent _animation;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);
        // Get components via ComponentManager or direct reference
        var player = owner as Player;
        _movement = player.Components.Movement;
        _animation = player.Components.Animation;
    }

    public override void Enter()
    {
        _animation.Play("idle");
    }

    public override void PhysicsUpdate(double delta)
    {
        _movement.ApplyGravity((float)delta);
        _movement.ApplyFriction((float)delta);
        _movement.ApplyToOwner(Owner);

        // Check transitions
        if (!Owner.IsOnFloor())
        {
            TransitionTo("Fall");
            return;
        }

        if (_movement.InputDirection != 0)
        {
            TransitionTo("Run");
            return;
        }
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") && Owner.IsOnFloor())
        {
            TransitionTo("Jump");
        }
        else if (@event.IsActionPressed("attack_1"))
        {
            TransitionTo("Attack");
        }
    }
}
```

---

## System 2: Component Manager

### Architecture

```
Player (CharacterBody2D)
├── ComponentManager (Node)
│   ├── MovementComponent
│   └── AnimationComponent
├── HealthComponent (existing - stays at Player level)
├── HurtBox (existing)
└── SlashHitBox (existing)
```

### Files

| File | Purpose |
|------|---------|
| `Core/Components/ComponentManager.cs` | Central access point for all components |
| `Core/Components/MovementComponent.cs` | Velocity, gravity, movement utilities |
| `Core/Components/AnimationComponent.cs` | AnimatedSprite2D wrapper |

---

### ComponentManager.cs

Provides typed access to all entity components.

```csharp
public partial class ComponentManager : Node
{
    // Child components (auto-populated in _Ready)
    public MovementComponent Movement { get; private set; }
    public AnimationComponent Animation { get; private set; }

    // External components (registered by parent)
    public HealthComponent Health { get; private set; }
    public HurtBoxComponent HurtBox { get; private set; }
    public AttackHitBoxComponent AttackHitBox { get; private set; }

    public override void _Ready()
    {
        // Find child components
        Movement = GetNode<MovementComponent>("MovementComponent");
        Animation = GetNode<AnimationComponent>("AnimationComponent");
    }

    // Called by parent to register components that aren't children
    public void RegisterExternalComponents(
        HealthComponent health,
        HurtBoxComponent hurtBox,
        AttackHitBoxComponent attackHitBox)
    {
        Health = health;
        HurtBox = hurtBox;
        AttackHitBox = attackHitBox;
    }

    // Generic getter for flexibility
    public T Get<T>() where T : Node
    {
        if (typeof(T) == typeof(MovementComponent)) return Movement as T;
        if (typeof(T) == typeof(AnimationComponent)) return Animation as T;
        if (typeof(T) == typeof(HealthComponent)) return Health as T;
        // ... etc
        return null;
    }
}
```

---

### MovementComponent.cs

Handles all movement physics. Stateless - doesn't know about game states.

```csharp
public partial class MovementComponent : Node
{
    [Export] public float Speed { get; set; } = 300f;
    [Export] public float Acceleration { get; set; } = 1500f;
    [Export] public float Friction { get; set; } = 1200f;
    [Export] public float JumpVelocity { get; set; } = -400f;
    [Export] public float JumpCutMultiplier { get; set; } = 0.5f;
    [Export] public float FallGravityMultiplier { get; set; } = 2.5f;

    public Vector2 Velocity { get; set; }
    public float InputDirection { get; private set; }
    public bool IsOnFloor { get; private set; }
    public bool FacingRight { get; private set; } = true;

    private float _gravity;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }

    // Call each frame to read input
    public void UpdateInput()
    {
        InputDirection = Input.GetAxis("move_left", "move_right");
        if (InputDirection != 0)
        {
            FacingRight = InputDirection > 0;
        }
    }

    // Sync state from CharacterBody2D
    public void UpdateFromOwner(CharacterBody2D owner)
    {
        Velocity = owner.Velocity;
        IsOnFloor = owner.IsOnFloor();
    }

    // Apply state back to CharacterBody2D
    public void ApplyToOwner(CharacterBody2D owner)
    {
        owner.Velocity = Velocity;
        owner.MoveAndSlide();
        IsOnFloor = owner.IsOnFloor();
    }

    public void ApplyGravity(float delta)
    {
        if (!IsOnFloor)
        {
            float multiplier = Velocity.Y > 0 ? FallGravityMultiplier : 1f;
            Velocity = new Vector2(Velocity.X, Velocity.Y + _gravity * multiplier * delta);
        }
    }

    public void ApplyHorizontalMovement(float delta)
    {
        if (InputDirection != 0)
        {
            float targetVelocity = InputDirection * Speed;
            Velocity = new Vector2(
                Mathf.MoveToward(Velocity.X, targetVelocity, Acceleration * delta),
                Velocity.Y
            );
        }
    }

    public void ApplyFriction(float delta)
    {
        if (InputDirection == 0 && IsOnFloor)
        {
            Velocity = new Vector2(
                Mathf.MoveToward(Velocity.X, 0, Friction * delta),
                Velocity.Y
            );
        }
    }

    public void Jump()
    {
        Velocity = new Vector2(Velocity.X, JumpVelocity);
    }

    public void CutJump()
    {
        if (Velocity.Y < 0)
        {
            Velocity = new Vector2(Velocity.X, Velocity.Y * JumpCutMultiplier);
        }
    }

    public void ApplyKnockback(Vector2 knockback)
    {
        Velocity += knockback;
    }
}
```

---

### AnimationComponent.cs

Wraps AnimatedSprite2D with convenience methods.

```csharp
public partial class AnimationComponent : Node
{
    [Export] public AnimatedSprite2D Sprite { get; set; }

    public StringName CurrentAnimation { get; private set; }

    [Signal]
    public delegate void AnimationFinishedEventHandler(StringName animName);

    public override void _Ready()
    {
        if (Sprite != null)
        {
            Sprite.AnimationFinished += OnAnimationFinished;
        }
    }

    public void Play(StringName animationName)
    {
        if (CurrentAnimation != animationName)
        {
            CurrentAnimation = animationName;
            Sprite?.Play(animationName);
        }
    }

    public void PlayOnce(StringName animationName)
    {
        CurrentAnimation = animationName;
        Sprite?.Play(animationName);
        // AnimationFinished signal will fire when done
    }

    public void SetFlipH(bool flip)
    {
        if (Sprite != null)
        {
            Sprite.FlipH = flip;
        }
    }

    public bool IsPlaying(StringName animationName)
    {
        return CurrentAnimation == animationName && Sprite?.IsPlaying() == true;
    }

    private void OnAnimationFinished()
    {
        EmitSignal(SignalName.AnimationFinished, CurrentAnimation);
    }
}
```

---

## Final Scene Structure

```
Player (CharacterBody2D) [Player.cs]
├── AnimatedSprite2D
├── CollisionShape2D
├── ComponentManager (Node) [ComponentManager.cs]
│   ├── MovementComponent (Node) [MovementComponent.cs]
│   └── AnimationComponent (Node) [AnimationComponent.cs]
├── StateMachine (Node) [StateMachine.cs]
│   ├── Idle (Node) [PlayerIdleState.cs]
│   ├── Run (Node) [PlayerRunState.cs]
│   ├── Jump (Node) [PlayerJumpState.cs]
│   ├── Fall (Node) [PlayerFallState.cs]
│   └── Attack (Node) [PlayerAttackState.cs]
├── HealthComponent (existing)
├── HurtBox (HurtBoxComponent - existing)
├── SlashHitBox (AttackHitBoxComponent - existing)
├── InteractionDetector
├── CollectionArea
├── DebugLabel
└── AttackTimer
```

---

## Refactored Player.cs

The Player script becomes a coordinator that wires systems together:

```csharp
public partial class Player : CharacterBody2D, IGameEntity
{
    [Export] public EntityStats Stats { get; set; }

    // System references
    public ComponentManager Components { get; private set; }
    public StateMachine StateMachine { get; private set; }

    // Direct component references (for external access)
    public HealthComponent HealthComponent { get; private set; }
    public HurtBoxComponent HurtBoxComponent { get; private set; }
    public AttackHitBoxComponent SlashHitBox { get; private set; }

    public override void _Ready()
    {
        // Get systems
        Components = GetNode<ComponentManager>("ComponentManager");
        StateMachine = GetNode<StateMachine>("StateMachine");

        // Get existing components
        HealthComponent = GetNode<HealthComponent>("HealthComponent");
        HurtBoxComponent = GetNode<HurtBoxComponent>("HurtBox");
        SlashHitBox = GetNode<AttackHitBoxComponent>("SlashHitBox");

        // Register external components with manager
        Components.RegisterExternalComponents(HealthComponent, HurtBoxComponent, SlashHitBox);

        // Configure attack hitbox
        SlashHitBox.SetOwnerStats(Stats);
        SlashHitBox.SetActive(false);

        // Initialize state machine
        StateMachine.Initialize(this);

        // Connect signals
        HurtBoxComponent.HitReceived += OnHitReceived;
        HealthComponent.DamageTaken += OnDamageTaken;
        HealthComponent.Died += OnDied;
    }

    private void OnHitReceived(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        var result = DamageCalculator.CalculateDamage(
            attackData, attackerStats, Stats, attackerPosition, GlobalPosition);

        HealthComponent.ApplyDamage(result.FinalDamage, result.WasCritical);
        Components.Movement.ApplyKnockback(result.KnockbackApplied);
    }

    private void OnDamageTaken(int damage, bool wasCritical)
    {
        // Visual feedback (flash, etc.)
    }

    private void OnDied()
    {
        GameManager.Instance.EmitSignal(GameManager.SignalName.PlayerDied);
        QueueFree();
    }
}
```

---

## Key Design Principles

1. **States as Nodes**: Visible in editor, can have [Export] properties, easy to debug
2. **States Own Transitions**: Each state decides when to exit (decentralized logic)
3. **Components are Stateless**: MovementComponent provides utilities, doesn't know about states
4. **Player is Coordinator**: Handles damage, death, and wires systems together
5. **Existing Components Unchanged**: HealthComponent, HurtBoxComponent, AttackHitBoxComponent remain as-is
6. **Reusable for Enemies**: StateMachine and State base classes can be used for Slime or other entities
