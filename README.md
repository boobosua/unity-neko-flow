# NekoFlow

Lightweight state machine + small conditional flow helpers for Unity.

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
using NekoFlow;
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

        this
            .StartWith(_idle)
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

Implement `IState` directly, or inherit `BaseState<TContext>`.

```csharp
using NekoFlow;
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

## Pure C# usage (no MonoBehaviour)

If you don’t want a component, use `StateMachine` directly:

```csharp
using NekoFlow;
using UnityEngine;

public class EnemyBrain : MonoBehaviour
{
    private StateMachine _sm;
    private  IdleState _idle;
    private  PatrolState _patrol;

    private void Awake()
    {
        _sm = new StateMachine();

        _idle = new IdleState();
        _patrol = new PatrolState();

        _sm
            .StartWith(_idle)
            .At(_idle, _patrol, () => _sm.TimeInState >= 1.0f)
            .At(_patrol, _idle, () => _sm.TimeInState >= 3.0f);
    }

    private void Update()
    {
        _sm?.Tick(Time.deltaTime);
    }
}
```

## Conditional flows

These are standalone helpers (not tied to the state machine).

### SimpleFlow

Run one action when a predicate is true; optionally run another action when it’s false.

```csharp
using NekoFlow;
using UnityEngine;

var flow = new SimpleFlow(
    predicate: () => Time.timeScale > 0,
    onSuccess: () => Debug.Log("Running"),
    onFailure: () => Debug.Log("Paused")
);

bool didRun = flow.Execute();
```

### BranchFlow

Try branches in order; execute the first match; optionally execute a fallback.

```csharp
using NekoFlow;
using UnityEngine;

var flow = new BranchFlow()
    .When(() => Input.GetKey(KeyCode.Space), () => Debug.Log("Jump"))
    .When(() => Input.GetKey(KeyCode.LeftArrow), () => Debug.Log("Left"))
    .Otherwise(() => Debug.Log("Idle"));

flow.Execute();
```

## API (quick reference)

### StateBehaviour

- `IsInState<T>()` — check current state type
- `TryGetCurrentState<T>(out T state)` — get current state instance (typed)
- `GetTimeInCurrentState()` — seconds spent in current state

### StateMachine (pure C#)

- `Tick(deltaTime)` — evaluate transitions, tick current state, track `TimeInState`
- `SetState(state)` — immediately switch state (`OnExit` → `OnEnter`)
- `TimeInState` — accumulated seconds since entering current state

### Fluent transition extensions

Available on both `StateBehaviour` and `StateMachine`:

- `StartWith(state)`
- `At(from, to, condition)`
- `Any(to, condition)` (checked before state-specific transitions)

### IState / BaseState<T>

- `OnEnter()`
- `OnTick(float deltaTime)`
- `OnExit()`

## Requirements

- Unity 2020.3+
- NekoLib
