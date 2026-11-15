
using UnityEngine;
using System.Collections;

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

    [Header("Обхід перешкод")]
    public LayerMask obstacleLayer;
    public float obstacleCheckDistance = 0.8f;
    public float obstacleCheckRadius = 0.35f;
    public float avoidanceStrength = 2f;

    [Header("Територія")]
    public float calmRadius = 2f;
    public float alertRadius = 5f;
    public float decisionInterval = 2f;

    [Header("Смерть")]
    public float deathDuration = 1f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D deerCollider;

    private Vector2 homePosition;
    private Vector2 movementDirection;
    private Vector2 fleeDirection;

    private float decisionTimer;
    private float fleeTimer;

    private bool isDead;
    private bool facingLeft;

    private const string AnimIdle = "Deer_Idle";
    private const string AnimWalk = "Deer_Walk";
    private const string AnimRun = "Deer_Run";
    private const string AnimDeath = "Deer_Death";

    [Header("Випадання предметів")]
    public GameObject meatPrefab;
    public GameObject hidePrefab;
    public float dropSpread = 0.4f;

    private string currentAnimation;

    public float lootDropDelay = 1f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        deerCollider = GetComponent<Collider2D>();

        currentHealth = maxHealth;
        homePosition = transform.position;
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }
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
        }
        else if (currentState == DeerState.Alert &&
                 distanceToPlayer > alertDistance * 1.5f)
        {
            currentState = DeerState.Calm;
            decisionTimer = 0f;
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

        if (movementDirection.sqrMagnitude < 0.01f)
            return;

        Vector2 actualDirection =
            GetAvoidanceDirection(movementDirection.normalized);

        if (actualDirection.sqrMagnitude > 0.01f)
        {
            rb.MovePosition(
                rb.position + actualDirection * speed * Time.fixedDeltaTime
            );

            FaceDirection(actualDirection);
        }
    }

    private Vector2 GetAvoidanceDirection(Vector2 direction)
    {
        if (obstacleLayer.value == 0)
            return direction;

        if (!HasObstacle(direction))
            return direction;

        Vector2 leftDirection =
            Quaternion.Euler(0f, 0f, 45f) * direction;

        Vector2 rightDirection =
            Quaternion.Euler(0f, 0f, -45f) * direction;

        bool leftBlocked = HasObstacle(leftDirection);
        bool rightBlocked = HasObstacle(rightDirection);

        if (!leftBlocked && rightBlocked)
        {
            return (direction + leftDirection * avoidanceStrength).normalized;
        }

        if (!rightBlocked && leftBlocked)
        {
            return (direction + rightDirection * avoidanceStrength).normalized;
        }

        if (!leftBlocked && !rightBlocked)
        {
            Vector2 chosenDirection = leftDirection;

            if (player != null)
            {
                Vector2 awayFromPlayer =
                    (rb.position - (Vector2)player.position).normalized;

                if (Vector2.Dot(rightDirection, awayFromPlayer) >
                    Vector2.Dot(leftDirection, awayFromPlayer))
                {
                    chosenDirection = rightDirection;
                }
            }

            return (direction + chosenDirection * avoidanceStrength).normalized;
        }

        Vector2 leftSide = new Vector2(-direction.y, direction.x);
        Vector2 rightSide = new Vector2(direction.y, -direction.x);

        bool leftSideBlocked = HasObstacle(leftSide);
        bool rightSideBlocked = HasObstacle(rightSide);

        if (!leftSideBlocked)
            return leftSide;

        if (!rightSideBlocked)
            return rightSide;

        return Vector2.zero;
    }

    private bool HasObstacle(Vector2 direction)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            rb.position,
            obstacleCheckRadius,
            direction,
            obstacleCheckDistance,
            obstacleLayer
        );

        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider == null)
                continue;

            if (hit.collider.transform == transform ||
                hit.collider.transform.IsChildOf(transform))
                continue;

            return true;
        }

        return false;
    }

    private void UpdateRoaming(float radius)
    {
        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval + Random.Range(0f, 2f);

            if (Random.value < 0.4f)
            {
                movementDirection = Vector2.zero;
            }
            else
            {
                Vector2 target = homePosition +
                    Random.insideUnitCircle * radius;

                movementDirection =
                    (target - rb.position).normalized;
            }
        }

        string action = movementDirection.sqrMagnitude < 0.01f
            ? AnimIdle
            : AnimWalk;

        PlayMovementAnimation(movementDirection, action);
    }

    private void UpdateFlee()
    {
        fleeTimer -= Time.deltaTime;

        movementDirection = fleeDirection;

        PlayMovementAnimation(movementDirection, AnimRun);

        if (fleeTimer <= 0f)
        {
            float distanceToPlayer = player != null
                ? Vector2.Distance(transform.position, player.position)
                : Mathf.Infinity;

            if (distanceToPlayer <= fleeDistance)
            {
                fleeDirection =
                    ((Vector2)transform.position -
                     (Vector2)player.position).normalized;

                fleeTimer = fleeDuration;
            }
            else if (distanceToPlayer <= alertDistance)
            {
                currentState = DeerState.Alert;
                movementDirection = Vector2.zero;
                decisionTimer = 0f;
            }
            else
            {
                currentState = DeerState.Calm;
                movementDirection = Vector2.zero;
                decisionTimer = 0f;
            }
        }
    }

    private void StartFlee()
    {
        if (isDead)
            return;

        if (currentState != DeerState.Flee)
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

            fleeTimer = fleeDuration;
        }

        currentState = DeerState.Flee;
    }

    public void ReactToShot(Vector2 shotPosition)
    {
        if (isDead)
            return;

        fleeDirection =
            ((Vector2)transform.position - shotPosition).normalized;

        if (fleeDirection.sqrMagnitude < 0.01f)
            fleeDirection = Vector2.right;

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
            animationLength = animator.GetCurrentAnimatorStateInfo(0).length;

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

        Vector2 meatOffset = Random.insideUnitCircle * dropSpread;
        Vector2 hideOffset = Random.insideUnitCircle * dropSpread;

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

    private void PlayMovementAnimation(Vector2 direction, string action)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            facingLeft = direction.x < 0f;

            if (spriteRenderer != null)
                spriteRenderer.flipX = facingLeft;
        }

        PlayAnimation(action);
    }
    private void FaceDirection(Vector2 direction)
{
    if (spriteRenderer == null || Mathf.Abs(direction.x) < 0.01f)
        return;

    facingLeft = direction.x < 0f;
    spriteRenderer.flipX = facingLeft;
}

    private void PlayAnimation(string animationName)
    {
        if (currentAnimation == animationName || animator == null)
            return;

        currentAnimation = animationName;
        animator.CrossFade(animationName, 0.1f);
    }
}
