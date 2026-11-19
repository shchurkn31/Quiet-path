
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(GridPathfinder2D))]
public class DeerAI : MonoBehaviour, IDamageable
{
    public enum DeerState
    {
        Calm,
        Alert,
        Flee,
        Dead
    }

    [Header("Здоров'я")]
    public int maxHealth = 2;
    private int currentHealth;

    [Header("ШІ")]
    public Transform player;
    public DeerState currentState = DeerState.Calm;

    public float alertDistance = 7f;
    public float fleeDistance = 3f;
    public float hearingRadius = 8f;
    public float fleeDuration = 4f;

    [Header("Швидкість")]
    public float calmSpeed = 1f;
    public float alertSpeed = 1.8f;
    public float fleeSpeed = 4f;

    [Header("Маршрутизація")]
    public GridPathfinder2D pathfinder;
    public float fleePathDistance = 8f;

    [Header("Територія")]
    public float calmRadius = 2f;
    public float alertRadius = 5f;
    public float decisionInterval = 2f;

    [Header("Смерть")]
    public float deathDuration = 1f;

    [Header("Випадання предметів")]
    public GameObject meatPrefab;
    public GameObject hidePrefab;
    public float dropSpread = 0.4f;
    public float lootDropDelay = 1f;

    [Header("Спрайт")]
    public bool spriteFacesRight = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D deerCollider;

    private Vector2 homePosition;
    private Vector2 movementDirection;
    private Vector2 fleeDirection;
    private Vector2 fleeTarget;
    private Vector2 roamTarget;

    private float decisionTimer;
    private float fleeTimer;
    private float nextFleeRetry;
    private int fleeTurnIndex;

    private static readonly float[] FleeTurns = { 0f, 55f, -55f, 100f, -100f };

    private bool isDead;
    private bool hasRoamTarget;

    private string currentAnimation;

    private const string AnimIdle = "Deer_Idle";
    private const string AnimWalk = "Deer_Walk";
    private const string AnimRun = "Deer_Run";
    private const string AnimDeath = "Deer_Death";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        deerCollider = GetComponent<Collider2D>();

        if (pathfinder == null)
            pathfinder = GetComponent<GridPathfinder2D>();

        currentHealth = maxHealth;
        homePosition = transform.position;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        PlayAnimation(AnimIdle);
    }

    private void Update()
    {
        if (isDead)
            return;

        float distanceToPlayer = player != null
            ? Vector2.Distance(transform.position, player.position)
            : Mathf.Infinity;

        if (distanceToPlayer <= fleeDistance)
        {
            StartFlee();
        }
        else if (currentState == DeerState.Calm &&
                 distanceToPlayer <= alertDistance)
        {
            currentState = DeerState.Alert;
            decisionTimer = 0f;
            hasRoamTarget = false;

            if (pathfinder != null)
                pathfinder.ClearPath();
        }
        else if (currentState == DeerState.Alert &&
                 distanceToPlayer > alertDistance * 1.5f)
        {
            currentState = DeerState.Calm;
            decisionTimer = 0f;
            hasRoamTarget = false;

            if (pathfinder != null)
                pathfinder.ClearPath();
        }

        switch (currentState)
        {
            case DeerState.Calm:
                UpdateRoaming(calmRadius);
                break;

            case DeerState.Alert:
                UpdateRoaming(alertRadius);
                break;

            case DeerState.Flee:
                UpdateFlee();
                break;
        }
    }

    private void FixedUpdate()
    {
        if (isDead || rb == null)
            return;

        float speed = calmSpeed;

        if (currentState == DeerState.Alert)
            speed = alertSpeed;
        else if (currentState == DeerState.Flee)
            speed = fleeSpeed;

        if (movementDirection.sqrMagnitude <= 0.000001f)
            return;

        Vector2 step = Vector2.ClampMagnitude(movementDirection, speed * Time.fixedDeltaTime);
        rb.MovePosition(rb.position + step);

        movementDirection -= step;
        FaceDirection(step);
    }

    private void UpdateRoaming(float radius)
    {
        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0f)
        {
            decisionTimer =
                decisionInterval + Random.Range(0f, 2f);

            if (Random.value < 0.4f)
            {
                hasRoamTarget = false;
                movementDirection = Vector2.zero;

                if (pathfinder != null)
                    pathfinder.ClearPath();
            }
            else
            {
                roamTarget =
                    homePosition + Random.insideUnitCircle * radius;

                hasRoamTarget = true;

                if (pathfinder != null)
                    pathfinder.ClearPath();
            }
        }

        if (!hasRoamTarget)
        {
            movementDirection = Vector2.zero;
            PlayAnimation(AnimIdle);
            return;
        }

        Vector2 toTarget = roamTarget - rb.position;

        if (toTarget.magnitude <= 0.25f)
        {
            hasRoamTarget = false;
            movementDirection = Vector2.zero;

            if (pathfinder != null)
                pathfinder.ClearPath();

            PlayAnimation(AnimIdle);
            return;
        }

        if (pathfinder != null)
        {
            movementDirection =
                pathfinder.GetMovementToTarget(roamTarget);
        }
        else
        {
            movementDirection = toTarget;
        }

        if (movementDirection.sqrMagnitude < 0.01f)
        {
            PlayAnimation(AnimIdle);
        }
        else
        {
            PlayMovementAnimation(movementDirection, AnimWalk);
        }
    }

    private void UpdateFlee()
    {
        fleeTimer -= Time.deltaTime;

        if (fleeTimer <= 0f)
        {
            float distanceToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            if (distanceToPlayer <= fleeDistance)
            {
                CalculateFleeTarget();
                fleeTimer = fleeDuration;
            }
            else if (distanceToPlayer <= alertDistance)
            {
                currentState = DeerState.Alert;
                movementDirection = Vector2.zero;
                decisionTimer = 0f;

                if (pathfinder != null)
                    pathfinder.ClearPath();

                return;
            }
            else
            {
                currentState = DeerState.Calm;
                movementDirection = Vector2.zero;
                decisionTimer = 0f;

                if (pathfinder != null)
                    pathfinder.ClearPath();

                return;
            }
        }

        if ((fleeTarget - rb.position).sqrMagnitude <= 0.01f)
        {
            fleeTurnIndex = 0;
            SetFleeTarget();
        }

        movementDirection = pathfinder != null
            ? pathfinder.GetMovementToTarget(fleeTarget)
            : fleeTarget - rb.position;

        if (movementDirection.sqrMagnitude <= 0.000001f && Time.time >= nextFleeRetry)
        {
            fleeTurnIndex = (fleeTurnIndex + 1) % FleeTurns.Length;
            SetFleeTarget();
        }

        if (movementDirection.sqrMagnitude > 0.01f)
            PlayMovementAnimation(movementDirection, AnimRun);
        else
            PlayAnimation(AnimIdle);
    }

    private void StartFlee()
    {
        if (isDead)
            return;

        if (currentState != DeerState.Flee)
        {
            CalculateFleeTarget();
            fleeTimer = fleeDuration;
        }

        currentState = DeerState.Flee;
    }

    private void CalculateFleeTarget()
    {
        if (player != null)
        {
            fleeDirection =
                ((Vector2)transform.position -
                 (Vector2)player.position).normalized;
        }
        else
        {
            fleeDirection = Vector2.right;
        }

        if (fleeDirection.sqrMagnitude < 0.01f)
            fleeDirection = Vector2.right;

        fleeTurnIndex = 0;
        SetFleeTarget();
    }

    private void SetFleeTarget()
    {
        Vector2 offset = Quaternion.Euler(0f, 0f, FleeTurns[fleeTurnIndex]) *
                         (fleeDirection * fleePathDistance);
        fleeTarget = rb.position + offset;
        movementDirection = Vector2.zero;
        nextFleeRetry = Time.time + 0.5f;

        if (pathfinder != null)
            pathfinder.ClearPath();
    }

    public void ReactToShot(Vector2 shotPosition)
    {
        if (isDead)
            return;

        fleeDirection =
            ((Vector2)transform.position - shotPosition).normalized;

        if (fleeDirection.sqrMagnitude < 0.01f)
            fleeDirection = Vector2.right;

        fleeTurnIndex = 0;
        SetFleeTarget();

        currentState = DeerState.Flee;
        fleeTimer = fleeDuration;
    }

    public static void NotifyShot(Vector2 shotPosition)
    {
        DeerAI[] deerList =
            FindObjectsByType<DeerAI>(FindObjectsSortMode.None);

        foreach (DeerAI deer in deerList)
        {
            float distance = Vector2.Distance(
                deer.transform.position,
                shotPosition
            );

            if (distance <= deer.hearingRadius)
                deer.ReactToShot(shotPosition);
        }
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        TakeDamage(Mathf.CeilToInt(amount));
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0)
            return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartFlee();
        }
    }

    private void Die()
    {
        isDead = true;
        currentState = DeerState.Dead;

        if (QuestManager.Instance != null)
            QuestManager.Instance.RegisterKill(QuestAnimalType.Deer);

        movementDirection = Vector2.zero;
        hasRoamTarget = false;

        if (pathfinder != null)
            pathfinder.ClearPath();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (deerCollider != null)
            deerCollider.enabled = false;

        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;

            currentAnimation = AnimDeath;
            animator.Play(AnimDeath, 0, 0f);
            animator.Update(0f);
        }
        else
        {
            Debug.LogError("DeerAI: відсутній Animator!", this);
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return null;

        float animationLength = deathDuration;

        if (animator != null)
        {
            animationLength =
                animator.GetCurrentAnimatorStateInfo(0).length;

            if (animationLength <= 0.01f)
                animationLength = deathDuration;
        }

        float dropDelay = Mathf.Clamp(
            lootDropDelay,
            0f,
            animationLength
        );

        yield return new WaitForSeconds(dropDelay);

        SpawnLoot();

        float remainingTime = animationLength - dropDelay;

        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        Vector2 dropPosition = transform.position;

        Vector2 meatOffset =
            Random.insideUnitCircle * dropSpread;

        Vector2 hideOffset =
            Random.insideUnitCircle * dropSpread;

        if (meatPrefab != null)
        {
            Instantiate(
                meatPrefab,
                dropPosition + meatOffset,
                Quaternion.identity
            );
        }

        if (hidePrefab != null)
        {
            Instantiate(
                hidePrefab,
                dropPosition + hideOffset,
                Quaternion.identity
            );
        }
    }

    private void PlayMovementAnimation(
        Vector2 direction,
        string action)
    {
        FaceDirection(direction);
        PlayAnimation(action);
    }

    private void FaceDirection(Vector2 direction)
    {
        if (spriteRenderer == null ||
            Mathf.Abs(direction.x) < 0.01f)
            return;

        bool left = direction.x < 0f;

        spriteRenderer.flipX =
            spriteFacesRight ? left : !left;
    }

    private void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName ||
            animator == null)
            return;

        currentAnimation = animationName;
        animator.CrossFade(animationName, 0.1f);
    }
}
