# NekoFlow

Simple finite state machine for Unity with fluent API.

## Installation

Via Git URL in Unity Package Manager:

```
https://github.com/boobosua/unity-neko-flow.git
```

## Usage

### 1. Create Enemy Controller

```csharp
using NekoFlow;

public class EnemyController : FlowBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolRadius = 5f;
    [SerializeField] private float jumpChance = 0.3f;

    public float MoveSpeed => moveSpeed;
    public float PatrolRadius => patrolRadius;
    public Vector3 StartPosition { get; private set; }

    private EnemyIdleState _idleState;
    private EnemyPatrolState _patrolState;
    private EnemyJumpState _jumpState;

    private void Awake()
    {
        StartPosition = transform.position;

        _idleState = new EnemyIdleState(this);
        _patrolState = new EnemyPatrolState(this);
        _jumpState = new EnemyJumpState(this);

        MainFlow.StartWith(_idleState)
            .At(_idleState, _patrolState, () => _idleState.IsComplete)
            .At(_patrolState, _idleState, () => _patrolState.IsComplete)
            .At(_patrolState, _jumpState, () => ShouldJump())
            .At(_jumpState, _idleState, () => _jumpState.IsComplete);
    }

    private bool ShouldJump()
    {
        return _patrolState.IsMoving && Random.Range(0f, 1f) < jumpChance * Time.deltaTime;
    }
}
```

### 2. Create States

**States/EnemyIdleState.cs**

```csharp
using NekoFlow;

public class EnemyIdleState : BaseState<EnemyController>
{
    private float _idleTimer;
    private float _idleDuration;

    public bool IsComplete => _idleTimer >= _idleDuration;

    public EnemyIdleState(EnemyController context) : base(context) { }

    public override void OnEnter()
    {
        _idleTimer = 0f;
        _idleDuration = Random.Range(1f, 3f);
    }

    public override void OnTick()
    {
        _idleTimer += Time.deltaTime;
    }
}
```

**States/EnemyPatrolState.cs**

```csharp
using NekoFlow;
using UnityEngine;

public class EnemyPatrolState : BaseState<EnemyController>
{
    private Vector3 _targetPosition;
    private float _patrolTimer;
    private float _patrolDuration;

    public bool IsComplete => _patrolTimer >= _patrolDuration;
    public bool IsMoving => Vector3.Distance(_transform.position, _targetPosition) > 0.1f;

    public EnemyPatrolState(EnemyController context) : base(context) { }

    public override void OnEnter()
    {
        _patrolTimer = 0f;
        _patrolDuration = Random.Range(3f, 6f);

        // Set random patrol target
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        _targetPosition = _context.StartPosition + (Vector3)randomDir * _context.PatrolRadius;
    }

    public override void OnTick()
    {
        _patrolTimer += Time.deltaTime;

        // Move to target
        _transform.position = Vector3.MoveTowards(
            _transform.position,
            _targetPosition,
            _context.MoveSpeed * Time.deltaTime
        );
    }
}
```

## API

### FlowBehaviour

- `MainFlow` - Access the state machine
- `IsInState<T>()` - Check current state type
- `GetCurrentState()` - Get current state instance

### FlowMachine

- `StartWith(state)` - Set initial state
- `At(from, to, condition)` - Add transition
- `Any(to, condition)` - Add global transition

### BaseState<T>

- `OnEnter()` - Called when entering state
- `OnTick()` - Called every frame
- `OnExit()` - Called when leaving state
