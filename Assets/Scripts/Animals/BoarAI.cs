
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BoarAI : MonoBehaviour, IDamageable
{
    public enum BoarState
    {
        Calm,
        Alert,
        Attack,
        Dead
    }

    [Header("Здоров'я")]
    public int maxHealth = 4;
    private int currentHealth;

    [Header("ШІ")]
    public Transform player;
    public BoarState currentState = BoarState.Calm;
    public float alertDistance = 4f;
    public float attackDetectionDistance = 3f;
    public float shotAggroRadius = 3f;
    public float shotAlertRadius = 10f;
    public float loseTargetDistance = 12f;
    public float loseTargetTime = 4f;

    [Header("Швидкість")]
    public float calmSpeed = 0.8f;
    public float alertSpeed = 1.4f;
    public float attackSpeed = 3f;

    [Header("Обхід перешкод")]
    public LayerMask obstacleLayer;
    public float obstacleCheckDistance = 0.8f;
    public float obstacleCheckRadius = 0.35f;
    public float avoidanceStrength = 2f;

    [Header("Територія")]
    public float calmRadius = 2f;
    public float alertRadius = 3f;
    public float decisionInterval = 2f;

    [Header("Атака")]
    public float attackRange = 1.8f;
    public float attackDamage = 1f;
    public float attackCooldown = 1.2f;
    public float attackWindup = 0f;
    public float attackAnimDuration = 0.6f;
    public float attackHitDelay = 0.25f;

    [Header("Отримання шкоди")]
    public float hurtDuration = 0.5f;

    [Header("Смерть і здобич")]
    public float deathDuration = 1f;
    public float lootDropDelay = 1f;
    public GameObject meatPrefab;
    public GameObject hidePrefab;
    public float dropSpread = 0.4f;

    [Range(0f, 1f)]
    public float doubleLootChance = 0.2f;

    [Header("Спрайт")]
    public bool spriteFacesRight = true;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Collider2D[] boarColliders;

    private Vector2 homePosition;
    private Vector2 movementDirection;

    private float decisionTimer;
    private float lostTimer;
    private float attackCooldownTimer;
    private float windupTimer;
    private float attackTimer;
    private float hurtTimer;

    private bool attackDamageDealt;
    private string currentAnimation;

    private const string AnimIdle = "Boar_Idle";
    private const string AnimWalk = "Boar_Walk";
    private const string AnimRun = "Boar_Run";
    private const string AnimAttack = "Boar_Attack";
    private const string AnimHurt = "Boar_Hurt";
    private const string AnimDeath = "Boar_Death";

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boarColliders = GetComponents<Collider2D>();

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

        PlayAnimation(AnimIdle);
    }

    private void Update()
    {
        if (currentState == BoarState.Dead)
            return;

        if (hurtTimer > 0f)
        {
            hurtTimer -= Time.deltaTime;
            movementDirection = Vector2.zero;
            return;
        }

        float distance = DistanceToPlayer();

        if (currentState != BoarState.Attack)
        {
            if (distance <= attackDetectionDistance)
            {
                StartAttack();
            }
            else if (distance <= alertDistance)
            {
                if (currentState == BoarState.Calm)
                {
                    currentState = BoarState.Alert;
                    decisionTimer = 0f;
                }
            }
            else if (currentState == BoarState.Alert &&
                     distance > alertDistance * 1.5f)
            {
                currentState = BoarState.Calm;
                decisionTimer = 0f;
            }
        }

        switch (currentState)
        {
            case BoarState.Calm:
                UpdateRoaming(calmRadius);
                break;

            case BoarState.Alert:
                UpdateRoaming(alertRadius);
                break;

            case BoarState.Attack:
                UpdateAttack(distance);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == BoarState.Dead || rb == null)
            return;

        float speed = calmSpeed;

        if (currentState == BoarState.Alert)
            speed = alertSpeed;
        else if (currentState == BoarState.Attack)
            speed = attackSpeed;

        if (movementDirection.sqrMagnitude < 0.01f)
            return;

        Vector2 moveDirection =
            GetAvoidanceDirection(movementDirection.normalized);

        if (moveDirection.sqrMagnitude > 0.01f)
        {
            rb.MovePosition(
                rb.position + moveDirection * speed * Time.fixedDeltaTime
            );

            FaceDirection(moveDirection);
        }
    }

    private Vector2 GetAvoidanceDirection(Vector2 direction)
    {
        if (obstacleLayer.value == 0)
            return direction;

        RaycastHit2D hit = Physics2D.CircleCast(
            rb.position,
            obstacleCheckRadius,
            direction,
            obstacleCheckDistance,
            obstacleLayer
        );

        if (hit.collider == null)
            return direction;

        Vector2 leftDirection =
            Quaternion.Euler(0f, 0f, 45f) * direction;

        Vector2 rightDirection =
            Quaternion.Euler(0f, 0f, -45f) * direction;

        RaycastHit2D leftHit = Physics2D.CircleCast(
            rb.position,
            obstacleCheckRadius,
            leftDirection,
            obstacleCheckDistance,
            obstacleLayer
        );

        RaycastHit2D rightHit = Physics2D.CircleCast(
            rb.position,
            obstacleCheckRadius,
            rightDirection,
            obstacleCheckDistance,
            obstacleLayer
        );

        if (leftHit.collider == null && rightHit.collider != null)
        {
            return (direction + leftDirection * avoidanceStrength).normalized;
        }

        if (rightHit.collider == null && leftHit.collider != null)
        {
            return (direction + rightDirection * avoidanceStrength).normalized;
        }

        if (leftHit.collider == null && rightHit.collider == null)
        {
            if (player != null)
            {
                float leftDistance = Vector2.Distance(
                    rb.position + leftDirection,
                    player.position
                );

                float rightDistance = Vector2.Distance(
                    rb.position + rightDirection,
                    player.position
                );

                Vector2 chosenDirection =
                    leftDistance < rightDistance
                    ? leftDirection
                    : rightDirection;

                return (direction + chosenDirection * avoidanceStrength).normalized;
            }

            return (direction + leftDirection * avoidanceStrength).normalized;
        }

        Vector2 leftSide = new Vector2(-direction.y, direction.x);
        Vector2 rightSide = new Vector2(direction.y, -direction.x);

        RaycastHit2D leftSideHit = Physics2D.CircleCast(
            rb.position,
            obstacleCheckRadius,
            leftSide,
            obstacleCheckDistance,
            obstacleLayer
        );

        RaycastHit2D rightSideHit = Physics2D.CircleCast(
            rb.position,
            obstacleCheckRadius,
            rightSide,
            obstacleCheckDistance,
            obstacleLayer
        );

        if (leftSideHit.collider == null)
            return leftSide;

        if (rightSideHit.collider == null)
            return rightSide;

        return Vector2.zero;
    }

    private float DistanceToPlayer()
    {
        if (player == null)
            return Mathf.Infinity;

        return Vector2.Distance(transform.position, player.position);
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
                Vector2 target =
                    homePosition + Random.insideUnitCircle * radius;

                movementDirection =
                    (target - rb.position).normalized;
            }
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

    private void UpdateAttack(float distance)
    {
        if (player == null || distance > loseTargetDistance)
        {
            lostTimer += Time.deltaTime;

            if (lostTimer >= loseTargetTime)
            {
                EndAttack();
                return;
            }
        }
        else
        {
            lostTimer = 0f;
        }

        if (player == null)
        {
            movementDirection = Vector2.zero;
            return;
        }

        if (attackCooldownTimer > 0f)
            attackCooldownTimer -= Time.deltaTime;

        if (windupTimer > 0f)
        {
            windupTimer -= Time.deltaTime;
            movementDirection = Vector2.zero;

            FaceDirection((Vector2)player.position - rb.position);

            if (windupTimer <= 0f)
                StartStrike();

            return;
        }

        if (attackTimer > 0f)
        {
            attackTimer -= Time.deltaTime;
            movementDirection = Vector2.zero;

            float hitMoment = attackAnimDuration - attackHitDelay;

            if (!attackDamageDealt && attackTimer <= hitMoment)
            {
                attackDamageDealt = true;
                DealDamageToPlayer();
            }

            if (attackTimer <= 0f)
                PlayAnimation(AnimIdle);

            return;
        }

        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (distance <= attackRange)
        {
            movementDirection = Vector2.zero;
            FaceDirection(toPlayer);

            if (attackCooldownTimer <= 0f)
                StartWindup(toPlayer);
            else
                PlayAnimation(AnimIdle);

            return;
        }

        movementDirection = toPlayer.normalized;
        PlayMovementAnimation(movementDirection, AnimRun);
    }

    private void StartWindup(Vector2 direction)
    {
        movementDirection = Vector2.zero;

        FaceDirection(direction);

        windupTimer = Mathf.Max(0f, attackWindup);
        attackCooldownTimer = attackCooldown;

        PlayAnimation(AnimIdle);

        if (windupTimer <= 0f)
            StartStrike();
    }

    private void StartStrike()
    {
        movementDirection = Vector2.zero;

        attackTimer = Mathf.Max(0.01f, attackAnimDuration);
        attackHitDelay = Mathf.Clamp(attackHitDelay, 0f, attackTimer);

        attackDamageDealt = false;

        PlayAnimation(AnimAttack);
    }

    private void DealDamageToPlayer()
    {
        if (player == null)
            return;

        float distance = DistanceToPlayer();

        if (distance > attackRange * 1.5f)
            return;

        IDamageable target = player.GetComponentInParent<IDamageable>();

        if (target == null)
        {
            Debug.LogWarning("На гравці не знайдено IDamageable!", this);
            return;
        }

        Vector2 direction =
            ((Vector2)player.position - rb.position).normalized;

        target.TakeDamage(attackDamage, direction);
    }

    private void StartAttack()
    {
        if (currentState == BoarState.Attack ||
            currentState == BoarState.Dead)
            return;

        currentState = BoarState.Attack;

        lostTimer = 0f;
        windupTimer = 0f;
        attackTimer = 0f;
        attackCooldownTimer = 0f;

        movementDirection = Vector2.zero;
    }

    private void EndAttack()
    {
        currentState = BoarState.Alert;

        movementDirection = Vector2.zero;

        decisionTimer = 0f;
        lostTimer = 0f;
        windupTimer = 0f;
        attackTimer = 0f;
    }

    public void ReactToShot(Vector2 shotPosition)
    {
        if (currentState == BoarState.Dead)
            return;

        float distance = Vector2.Distance(transform.position, shotPosition);

        if (distance <= shotAggroRadius)
        {
            StartAttack();
        }
        else if (distance <= shotAlertRadius &&
                 currentState == BoarState.Calm)
        {
            currentState = BoarState.Alert;
            decisionTimer = 0f;
        }
    }

    public static void NotifyShot(Vector2 shotPosition)
    {
        BoarAI[] boars = FindObjectsOfType<BoarAI>();

        foreach (BoarAI boar in boars)
            boar.ReactToShot(shotPosition);
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        TakeDamage(Mathf.CeilToInt(amount));
    }

    public void TakeDamage(int damage)
    {
        if (currentState == BoarState.Dead || damage <= 0)
            return;

        currentHealth -= damage;

        Debug.Log("Кабан отримав шкоду. Здоров'я: " + currentHealth, this);

        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartHurt();
            StartAttack();
        }
    }

    private void StartHurt()
    {
        hurtTimer = hurtDuration;

        windupTimer = 0f;
        attackTimer = 0f;
        movementDirection = Vector2.zero;

        PlayAnimation(AnimHurt);
    }

    private void Die()
{
    currentState = BoarState.Dead;

    if (QuestManager.Instance != null)
        QuestManager.Instance.RegisterKill(QuestAnimalType.Boar);

    movementDirection = Vector2.zero;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        foreach (Collider2D col in boarColliders)
            col.enabled = false;

        if (animator != null)
        {
            currentAnimation = AnimDeath;
            animator.Play(AnimDeath, 0, 0f);
        }

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        float dropDelay = Mathf.Clamp(lootDropDelay, 0f, deathDuration);

        yield return new WaitForSeconds(dropDelay);

        SpawnLoot();

        float remainingTime = deathDuration - dropDelay;

        if (remainingTime > 0f)
            yield return new WaitForSeconds(remainingTime);

        Destroy(gameObject);
    }

    private void SpawnLoot()
    {
        int meat = 1;
        int hide = 1;

        if (Random.value < doubleLootChance)
        {
            if (Random.value < 0.5f)
                meat = 2;
            else
                hide = 2;
        }

        DropItems(meatPrefab, meat);
        DropItems(hidePrefab, hide);
    }

    private void DropItems(GameObject prefab, int count)
    {
        if (prefab == null)
            return;

        for (int i = 0; i < count; i++)
        {
            Vector2 offset = Random.insideUnitCircle * dropSpread;

            Instantiate(
                prefab,
                (Vector2)transform.position + offset,
                Quaternion.identity
            );
        }
    }

    private void PlayMovementAnimation(Vector2 direction, string animationName)
    {
        FaceDirection(direction);
        PlayAnimation(animationName);
    }

    private void FaceDirection(Vector2 direction)
    {
        if (spriteRenderer == null || Mathf.Abs(direction.x) < 0.01f)
            return;

        bool left = direction.x < 0f;

        spriteRenderer.flipX = spriteFacesRight ? left : !left;
    }

    private void PlayAnimation(string animationName)
    {
        if (animator == null || currentAnimation == animationName)
            return;

        currentAnimation = animationName;
        animator.CrossFade(animationName, 0.1f);
    }
}
