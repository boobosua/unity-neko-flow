# Neko Flow

Lightweight state machine for Unity.

## Installation (Unity Package Manager)

1. Install NekoLib:

```
https://github.com/boobosua/unity-nekolib.git
```

2. Install NekoFlow:

```
https://github.com/boobosua/unity-neko-flow.git
```

## Quick start (state machine)

### 1) Create a controller (the “brain”)

Derive from `StateBehaviour`, create your states, then declare transitions.
Transition predicates usually belong here (using `GetTimeInCurrentState()`, sensors, cooldowns, etc.).

```csharp
using NekoFlow.FSM;
using UnityEngine;

public class EnemyController : StateBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolRadius = 5f;

    [Header("State Durations")]
    [SerializeField] private Vector2 idleDurationRange = new(0.8f, 2.0f);
    [SerializeField] private Vector2 patrolDurationRange = new(2.0f, 5.0f);
    [SerializeField] private float attackDuration = 0.7f;

    public float MoveSpeed => moveSpeed;
    public float PatrolRadius => patrolRadius;
    public Vector3 StartPosition { get; private set; }

    private EnemyIdleState _idle;
    private EnemyPatrolState _patrol;
    private EnemyAttackState _attack;

    private float _nextIdleDuration;
    private float _nextPatrolDuration;

    private void Awake()
    {
        StartPosition = transform.position;

        _idle = new EnemyIdleState(this);
        _patrol = new EnemyPatrolState(this);
        _attack = new EnemyAttackState(this);

        _nextIdleDuration = Random.Range(idleDurationRange.x, idleDurationRange.y);
        _nextPatrolDuration = Random.Range(patrolDurationRange.x, patrolDurationRange.y);

        this.StartWith(_idle)

            // Time-based transitions using built-in TimeInState
            .At(_idle, _patrol, () => GetTimeInCurrentState() >= _nextIdleDuration)
            .At(_patrol, _idle, () => GetTimeInCurrentState() >= _nextPatrolDuration)

            // Global transition that can interrupt from ANY state
            .Any(_attack, () => CanAttackNow())

            // Controller decides when attack finishes
            .At(_attack, _idle, () => GetTimeInCurrentState() >= attackDuration);
    }

    private bool CanAttackNow()
    {
        // Replace with your own logic: target detected, in range, cooldown, etc.
        return HasTargetInRange();
    }

    private bool HasTargetInRange()
    {
        // Demo stub
        return false;
    }
}
```

### 2) Create states (the “workers”)

Implement `IState` directly, or inherit `BaseState<TContext>`. `BaseState<T>` stores three protected fields for convenience: `_context` (the controller), `_gameObject`, and `_transform`.

```csharp
using NekoFlow.FSM;
using UnityEngine;

public sealed class EnemyIdleState : BaseState<EnemyController>
{
    public EnemyIdleState(EnemyController context) : base(context) { }

    public override void OnEnter()
    {
        // e.g. set animation, stop nav, etc.
    }

    public override void OnTick(float deltaTime)
    {
        // Idle behavior only (no transition logic here)
    }
}
```

## Use the StateMachine class directly

If you don’t want a component, use `StateMachine` directly:

```csharp
using NekoFlow.FSM;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    private StateMachine _sm;
    private  IdleState _idle;
    private  PatrolState _patrol;

    private void Awake()
    {
        // Create state machine.
        _sm = new StateMachine();

        // Create states.
        _idle = new IdleState();
        _patrol = new PatrolState();

        // Declare transitions.
        _sm.StartWith(_idle)
            .At(_idle, _patrol, () => _sm.TimeInState >= 1.0f)
            .At(_patrol, _idle, () => _sm.TimeInState >= 3.0f);
    }

    // Remember to tick the state machine in Update.
    private void Update()
    {
        _sm?.Tick(Time.deltaTime);
    }
}
```

## API (quick reference)

### StateBehaviour

- `IsInState<T>()` — check current state type
- `TryGetCurrentState<T>(out T state)` — get current state instance (typed)
- `GetTimeInCurrentState()` — seconds spent in current state

### StateMachine (pure C#)

- `Tick(deltaTime)` — evaluate transitions, tick current state, track `TimeInState`
- `SetState(state)` — immediately switch state (`OnExit` → `OnEnter`)
- `CurrentState` — currently active `IState`
- `TimeInState` — accumulated seconds since entering current state
- Extension — `Is<T>()` — returns `true` when `CurrentState` is of type `T`
- Extension — `Get<T>()` — returns `CurrentState` cast to `T`, or `null`

### Fluent transition extensions

Available on both `StateBehaviour` and `StateMachine`:

- `StartWith(state)` — set initial state
- `At(from, to, condition)` — register a state-specific transition
- `Any(to, condition)` — register an any-state transition (evaluated before state-specific ones)

### IState / BaseState<T>

- `OnEnter()`
- `OnTick(float deltaTime)`
- `OnExit()`
- `BaseState<T>` provides `_context`, `_gameObject`, `_transform` as protected fields

## Requirements

- Unity 6+
- NekoLib
