﻿# Unity C# Coding Standards

## Naming Conventions

### Fields
- **Private fields**: Use underscore prefix (`_fieldName`)
  ```csharp
  private int _health;
  private NavMeshAgent _navMeshAgent;
  private GameObject _rockPrefab;
  ```

- **Protected fields**: No underscore prefix (camelCase)
  ```csharp
  protected Transform player;
  protected Animator anim;
  protected bool isPerformingAttack;
  ```

- **Public fields**: PascalCase
  ```csharp
  public int meleesDamage;
  public float fastRunSpeed;
  ```

- **Serialized fields**: Use `[SerializeField]` with underscore prefix for private
  ```csharp
  [SerializeField] private float _dashDistance;
  [SerializeField] private GameObject _rockPrefab;
  ```

### Static Fields
- **Static readonly (Animator hashes)**: PascalCase with underscores
  ```csharp
  private static readonly int Jump_Attack = Animator.StringToHash("JumpAttack");
  private static readonly int Enter_Second_Phase = Animator.StringToHash("EnterSecondPhase");
  private static readonly int Dizzy_Loop = Animator.StringToHash("DizzyLoop");
  ```

- **Other static readonly**: PascalCase
  ```csharp
  private static readonly int ColorPropertyId = Shader.PropertyToID("_Color");
  ```

### Methods
- **Public methods**: PascalCase
  ```csharp
  public void TriggerCounter()
  public void StartExplosion()
  ```

- **Private methods**: PascalCase
  ```csharp
  private void IdleMovement()
  private IEnumerator RevealStone()
  ```

- **Protected methods**: PascalCase
  ```csharp
  protected virtual void Start()
  protected override void PerformAttack()
  ```

### Properties
- **Public properties**: PascalCase
  ```csharp
  public bool HasSlime => slimeTransform != null;
  public bool IsInSecondPhase => hasEnteredSecondPhase;
  ```

### Expression-Bodied Members
- **Single-line methods**: Use expression-bodied syntax (`=>`) instead of curly braces
  ```csharp
  // ✅ GOOD
  public void StartAttacking() => _isAttacking = true;
  private void StartFlip() => _isKickFlipping = true;
  public void DamageAnimStopped() => isAnimationLocked = false;
  
  // ❌ BAD
  public void StartAttacking()
  {
      _isAttacking = true;
  }
  ```

- **Multi-line methods**: Use curly braces as normal
  ```csharp
  public void StopClone()
  {
      _isAttacking = false;
      Destroy(gameObject);
  }
  ```

### Control Flow Statements
- **Single-line if/else**: Omit curly braces when the body is a single statement
  ```csharp
  // ✅ GOOD
  if (GetMouseWorldPoint(out Vector3 mouseWorldPoint))
      _storedShootTargetPoint = mouseWorldPoint;
  else
      _storedShootTargetPoint = transform.position + transform.forward * 10f;
  
  // ❌ BAD
  if (GetMouseWorldPoint(out Vector3 mouseWorldPoint))
  {
      _storedShootTargetPoint = mouseWorldPoint;
  }
  else
  {
      _storedShootTargetPoint = transform.position + transform.forward * 10f;
  }
  ```

- **Multi-line if/else**: Use curly braces as normal
  ```csharp
  if (condition)
  {
      DoSomething();
      DoAnotherThing();
  }
  ```

## Code Organization

### Header Attributes
Use `[Header("")]` to organize serialized fields in Inspector:
```csharp
[Header("Jump Attack")]
public int earthShatterDamage;
[SerializeField] private GameObject _jumpingAttackParticlesPrefab;

[Header("Movement")]
[SerializeField] private float roamRadius;
[SerializeField] private float minMoveDistance;
```

### Section Comments
Use triple-slash comments to separate major sections:
```csharp
/// *** Unity Events *** ///

/// *** Base Methods *** ///

/// *** Skill 1-1: Jump Attack *** ///
```

Use double-slash for minor separators:
```csharp
///// ******* Skills ******* /////
```

### Method Comments
- **DO NOT** add XML documentation summaries (`/// <summary>`)
- Only add inline comments for complex logic when needed
- Method names should be self-explanatory

**❌ BAD:**
```csharp
/// <summary>
/// Lifts all remaining stones to reveal slimes
/// </summary>
public void LiftRemainingStones()
```

**✅ GOOD:**
```csharp
public void LiftRemainingStones()
```

### Inline Comments
Use inline comments sparingly for clarification:
```csharp
// Wait until boss reaches the center using manual distance check
while (Vector3.Distance(transform.position, center) > 0.1f)

// Flatten to horizontal plane
direction.y = 0;
```

## Unity-Specific Patterns

### Coroutines
- Name pattern: Verb + noun (e.g., `MoveToIntermissionPosition`, `RevealStone`)
- Start with `StartCoroutine()`
- Yield patterns:
  ```csharp
  yield return null; // Wait one frame
  yield return new WaitForSeconds(2f); // Wait time
  yield return new WaitUntil(() => condition); // Wait for condition
  yield return coroutine; // Wait for another coroutine
  ```

### Animation Events
- Use public methods that can be called from Unity animation events
- Clear, action-based names (e.g., `ActivateJumpingAttackParticles`, `JumpAttackStart`)

### GetComponent Pattern
- Cache component references in `Awake()` or `Start()`
  ```csharp
  void Start()
  {
      navMeshAgent = GetComponent<NavMeshAgent>();
      animator = GetComponent<Animator>();
      meshRenderer = GetComponentInChildren<Renderer>();
  }
  ```

## File Structure

### Typical Class Organization
1. Serialized fields (grouped by `[Header]`)
2. Public fields
3. Protected fields
4. Private fields
5. Static readonly fields
6. Public properties
7. Unity lifecycle methods (`Awake`, `Start`, `Update`, etc.)
8. Public methods
9. Protected methods
10. Private methods
11. Nested classes/structs

### Namespaces
- Use namespace hierarchy matching folder structure:
  ```csharp
  namespace Bosses.First_Boss
  namespace Bosses.First_Boss.Slime
  namespace Characters
  namespace Manager.GameManager
  ```

## Best Practices

### Variables
- **Always use explicit types**, avoid `var`:
  ```csharp
  Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
  StoneProjectile bulletScript = rock.GetComponentInChildren<StoneProjectile>();
  List<Coroutine> movementCoroutines = new List<Coroutine>();
  Vector3 explosionPosition = transform.position + Vector3.up * 1f;
  ```

### Null Checks
- Check component references before use:
  ```csharp
  if (animator == null)
      return;
  ```

### Debugging
- **DO NOT** add `Debug.Log` statements unless explicitly requested by the user
- If debugging is needed, wait for user instruction

### Performance
- Cache expensive lookups
- Reuse objects when possible (e.g., `NavMeshPath`, `MaterialPropertyBlock`)
- Use `sqrMagnitude` instead of `Distance` when only comparing distances

### Spawned Object Lifetime Management
- **Spawned objects should manage their own lifetime** instead of relying on the spawner's coroutines
- Use `Update()` with timers instead of coroutines when the spawner might call `StopAllCoroutines()`
- This makes objects resilient to interruptions and keeps behavior self-contained

  ```csharp
  // ✅ GOOD - Clone manages its own lifetime
  public class NinjaClone : MonoBehaviour
  {
      private float _lifetime;
      
      private void Update()
      {
          _lifetime -= Time.deltaTime;
          if (_lifetime <= 0)
              DestroySelf();
      }
      
      public void SetLifetime(float duration) => _lifetime = duration;
  }
  
  // ❌ BAD - Spawner manages lifetime with coroutine (vulnerable to StopAllCoroutines)
  private IEnumerator CloneLifetime()
  {
      yield return new WaitForSeconds(duration);
      clone.DestroySelf();
  }
  ```

## Animation Controller Patterns

### Trigger Parameters
- Store as static readonly int fields
- Use `Animator.StringToHash()` for performance
- Match Unity Animator parameter names exactly

### Setting Values
```csharp
anim.SetTrigger(Jump_Attack);
anim.SetBool(Walking, true);
anim.ResetTrigger(Fast_Run);
```

## Summary

**Key Rules:**
1. Private fields: `_underscore` prefix
2. Protected/public fields: `camelCase` or `PascalCase`
3. Methods: `PascalCase` (all visibility levels)
4. Animator hashes: `PascalCase_With_Underscores`
5. NO XML documentation summaries (`/// <summary>`)
6. Use `[Header]` to organize Inspector fields
7. Cache component references in `Start()`/`Awake()`
8. Clear section separators with `///` comments
9. **Always use explicit types**, avoid `var`
10. **NO `Debug.Log`** unless explicitly requested
11. **Single-line methods**: Use expression-bodied syntax (`=>`) instead of curly braces
12. **Single-line if/else**: Omit curly braces for single statements
13. **Spawned objects manage their own lifetime** with `Update()` timers, not spawner coroutines
