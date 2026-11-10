
using System.Collections;
using UnityEngine;

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
    public float shotAggroRadius = 3f;
    public float shotAlertRadius = 10f;
    public float loseTargetDistance = 12f;
    public float loseTargetTime = 4f;

    [Header("Швидкість")]
    public float calmSpeed = 0.8f;
    public float alertSpeed = 1.4f;
    public float attackSpeed = 4.5f;

    [Header("Територія")]
    public float calmRadius = 2f;
    public float alertRadius = 3f;
    public float decisionInterval = 2f;

    [Header("Удар")]
    public float attackRange = 1.2f;
    public float attackDamage = 1f;
    public float attackCooldown = 1.2f;
    public float attackAnimDuration = 0.6f;
    public float attackHitDelay = 0.25f;
    public float attackWindup = 0.5f;
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
    private Collider2D boarCollider;

    private Vector2 homePosition;
    private Vector2 movementDirection;

    private float decisionTimer;
    private float lostTimer;
    private float attackCooldownTimer;
    private float attackLockTimer;
    private float windupTimer;
    private float hurtTimer;

    private bool attackDamageDealt;

    private const string AnimIdle = "Boar_Idle";
    private const string AnimWalk = "Boar_Walk";
    private const string AnimRun = "Boar_Run";
    private const string AnimAttack = "Boar_Attack";
    private const string AnimHurt = "Boar_Hurt";
    private const string AnimDeath = "Boar_Death";

    private string currentAnimation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boarCollider = GetComponent<Collider2D>();

        currentHealth = maxHealth;
        homePosition = transform.position;
    }

    private void Start()
    {
        ValidateAnimator();
    }

    private void ValidateAnimator()
    {
        string[] names =
        {
            AnimIdle,
            AnimWalk,
            AnimRun,
            AnimAttack,
            AnimHurt,
            AnimDeath
        };

        foreach (string stateName in names)
        {
            if (!animator.HasState(0, Animator.StringToHash(stateName)))
            {
                Debug.LogError("BoarAI: не знайдено стан Animator: " + stateName, this);
            }
        }
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

        float distanceToPlayer = DistanceToPlayer();

        if (currentState == BoarState.Calm && distanceToPlayer <= alertDistance)
        {
            currentState = BoarState.Alert;
            decisionTimer = 0f;
        }
        else if (currentState == BoarState.Alert && distanceToPlayer > alertDistance * 1.5f)
        {
            currentState = BoarState.Calm;
            decisionTimer = 0f;
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
                UpdateAttack(distanceToPlayer);
                break;
        }
    }

    private void FixedUpdate()
    {
        if (currentState == BoarState.Dead)
            return;

        float speed = calmSpeed;

        if (currentState == BoarState.Alert)
            speed = alertSpeed;
        else if (currentState == BoarState.Attack)
            speed = attackSpeed;

        rb.MovePosition(rb.position + movementDirection * speed * Time.fixedDeltaTime);
    }

    private float DistanceToPlayer()
    {
        return player != null
            ? Vector2.Distance(transform.position, player.position)
            : Mathf.Infinity;
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
                Vector2 target = homePosition + Random.insideUnitCircle * radius;
                movementDirection = (target - rb.position).normalized;
            }
        }

        string action = movementDirection.sqrMagnitude < 0.01f
            ? AnimIdle
            : AnimWalk;

        PlayMovementAnimation(movementDirection, action);
    }

    private void UpdateAttack(float distanceToPlayer)
    {
        attackCooldownTimer -= Time.deltaTime;

        if (windupTimer > 0f)
        {
            windupTimer -= Time.deltaTime;
            movementDirection = Vector2.zero;

            if (player != null)
                FaceDirection((Vector2)player.position - rb.position);

            if (windupTimer <= 0f)
                StartStrike();

            return;
        }

        if (attackLockTimer > 0f)
        {
            attackLockTimer -= Time.deltaTime;
            movementDirection = Vector2.zero;

            if (!attackDamageDealt &&
                attackLockTimer <= attackAnimDuration - attackHitDelay)
            {
                attackDamageDealt = true;
                DealDamageToPlayer();
            }

            return;
        }

        if (player == null || distanceToPlayer > loseTargetDistance)
        {
            lostTimer += Time.deltaTime;

            if (player == null || lostTimer >= loseTargetTime)
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
            return;

        Vector2 toPlayer = (Vector2)player.position - rb.position;

        if (distanceToPlayer <= attackRange)
        {
            if (attackCooldownTimer <= 0f)
            {
                StartWindup(toPlayer);
            }
            else
            {
                movementDirection = Vector2.zero;
                FaceDirection(toPlayer);
                PlayAnimation(AnimIdle);
            }

            return;
        }

        movementDirection = toPlayer.normalized;
        PlayMovementAnimation(movementDirection, AnimRun);
    }

    private void StartWindup(Vector2 toPlayer)
    {
        movementDirection = Vector2.zero;
        FaceDirection(toPlayer);

        attackCooldownTimer = attackCooldown;
        windupTimer = attackWindup;

        PlayAnimation(AnimIdle);

        if (windupTimer <= 0f)
            StartStrike();
    }

    private void StartStrike()
    {
        movementDirection = Vector2.zero;

        attackLockTimer = attackAnimDuration;
        attackDamageDealt = false;

        currentAnimation = AnimAttack;
        animator.Play(AnimAttack, 0, 0f);
    }

    private void StartHurt()
    {
        hurtTimer = hurtDuration;
        windupTimer = 0f;
        attackLockTimer = 0f;
        movementDirection = Vector2.zero;

        attackCooldownTimer = Mathf.Max(attackCooldownTimer, 0.3f);

        currentAnimation = AnimHurt;
        animator.Play(AnimHurt, 0, 0f);
    }

    private void DealDamageToPlayer()
    {
        if (player == null)
            return;

        if (Vector2.Distance(transform.position, player.position) > attackRange * 1.5f)
            return;

        IDamageable target = player.GetComponentInParent<IDamageable>();

        if (target == null)
            return;

        Vector2 direction = ((Vector2)player.position - rb.position).normalized;

        target.TakeDamage(attackDamage, direction);
    }

    private void StartAttack()
    {
        if (currentState == BoarState.Attack || currentState == BoarState.Dead)
            return;

        currentState = BoarState.Attack;
        lostTimer = 0f;
        attackLockTimer = 0f;
        attackCooldownTimer = 0.3f;
    }

    private void EndAttack()
    {
        currentState = BoarState.Alert;
        movementDirection = Vector2.zero;
        decisionTimer = 0f;
        lostTimer = 0f;
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
        else if (distance <= shotAlertRadius && currentState == BoarState.Calm)
        {
            currentState = BoarState.Alert;
            decisionTimer = 0f;
        }
    }

    public static void NotifyShot(Vector2 shotPosition)
    {
        BoarAI[] boars = FindObjectsByType<BoarAI>(FindObjectsSortMode.None);

        foreach (BoarAI boar in boars)
        {
            boar.ReactToShot(shotPosition);
        }
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        TakeDamage(Mathf.CeilToInt(amount));
    }

    public void TakeDamage(int damage)
    {
        if (currentState == BoarState.Dead)
            return;

        currentHealth -= damage;

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

    private void Die()
    {
        currentState = BoarState.Dead;
        movementDirection = Vector2.zero;

        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;

        boarCollider.enabled = false;

        currentAnimation = AnimDeath;
        animator.Play(AnimDeath, 0, 0f);
        animator.Update(0f);

        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        yield return null;

        float length = animator.GetCurrentAnimatorStateInfo(0).length;

        if (length <= 0.01f)
            length = deathDuration;

        float lootAt = Mathf.Min(lootDropDelay, length);

        yield return new WaitForSeconds(lootAt);

        SpawnLoot();

        float rest = length - lootAt;

        if (rest > 0f)
            yield return new WaitForSeconds(rest);

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

    private void PlayMovementAnimation(Vector2 direction, string action)
    {
        FaceDirection(direction);
        PlayAnimation(action);
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
        if (currentAnimation == animationName)
            return;

        currentAnimation = animationName;
        animator.CrossFade(animationName, 0.1f);
    }
}
