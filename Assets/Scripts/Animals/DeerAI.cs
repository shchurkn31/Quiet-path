using UnityEngine;
using System.Collections;

public class DeerAI : MonoBehaviour
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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        deerCollider = GetComponent<Collider2D>();

        currentHealth = maxHealth;
        homePosition = transform.position;
    }

    void Update()
    {
        if (isDead)
            return;

        float distanceToPlayer = player != null
            ? Vector2.Distance(transform.position, player.position)
            : Mathf.Infinity;

        // Перевірка відстані до гравця
        if (distanceToPlayer <= fleeDistance)
        {
            StartFlee();
        }
        else if (currentState == DeerState.Calm &&
                 distanceToPlayer <= alertDistance)
        {
            currentState = DeerState.Alert;
            decisionTimer = 0;
        }
        else if (currentState == DeerState.Alert &&
                 distanceToPlayer > alertDistance * 1.5f)
        {
            currentState = DeerState.Calm;
            decisionTimer = 0;
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

    void FixedUpdate()
    {
        if (isDead)
            return;

        float speed = calmSpeed;

        if (currentState == DeerState.Alert)
            speed = alertSpeed;
        else if (currentState == DeerState.Flee)
            speed = fleeSpeed;

        rb.MovePosition(
            rb.position + movementDirection * speed * Time.fixedDeltaTime
        );
    }

    // Спокійне переміщення та настороження
    void UpdateRoaming(float radius)
    {
        decisionTimer -= Time.deltaTime;

        if (decisionTimer <= 0)
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

                movementDirection = (target - rb.position).normalized;
            }
        }

        string action = movementDirection.sqrMagnitude < 0.01f
            ? AnimIdle
            : AnimWalk;

        PlayMovementAnimation(movementDirection, action);
        
    }

    // Втеча
    void UpdateFlee()
{
    fleeTimer -= Time.deltaTime;

    movementDirection = fleeDirection;

    PlayMovementAnimation(movementDirection, AnimRun);

    if (fleeTimer <= 0)
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
            decisionTimer = 0;
        }
        else
        {
            currentState = DeerState.Calm;
            movementDirection = Vector2.zero;
            decisionTimer = 0;
        }
    }
}

    void StartFlee()
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

    // Реакція на постріл
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

    // Повідомлення всім оленям про постріл
    public static void NotifyShot(Vector2 shotPosition)
    {
        DeerAI[] deerList = FindObjectsByType<DeerAI>(FindObjectsSortMode.None);

        foreach (DeerAI deer in deerList)
        {
            float distance = Vector2.Distance(
                deer.transform.position,
                shotPosition
            );

            if (distance <= deer.hearingRadius)
            {
                deer.ReactToShot(shotPosition);
            }
        }
    }

    // Реалізація IDamageable: через цей метод стріла завдає шкоди.
    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        TakeDamage(Mathf.CeilToInt(amount));
    }

    // Отримання шкоди
    public void TakeDamage(int damage)
    {
        if (isDead)
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
    IEnumerator SpawnLootAfterDelay()
{
    yield return new WaitForSeconds(1f);
    SpawnLoot();
}

    // Смерть
    void Die()
{
    isDead = true;
    currentState = DeerState.Dead;

    movementDirection = Vector2.zero;

    rb.linearVelocity = Vector2.zero;
    rb.simulated = false;

    if (deerCollider != null)
        deerCollider.enabled = false;

    currentAnimation = AnimDeath;

    if (animator == null)
    {
        Debug.LogError("DeerAI: на оленеві немає Animator (або він на дочірньому об'єкті).", this);
    }
    else
    {
        animator.enabled = true;
        animator.speed = 1f;

        if (!animator.HasState(0, Animator.StringToHash(AnimDeath)))
        {
            Debug.LogError("DeerAI: у шарі 0 немає стану '" + AnimDeath +
                "'. Перевір назву стану в Animator (регістр, пробіли) і що він у Base Layer.", this);
        }

        animator.Play(AnimDeath, 0, 0f);
        animator.Update(0f);
    }
    if (deerCollider != null)
    deerCollider.enabled = false;

StartCoroutine(SpawnLootAfterDelay());

PlayAnimation("Deer_Death");

    StartCoroutine(DestroyAfterDeath());
}

    IEnumerator DestroyAfterDeath()
{
    yield return null;

    float animationLength =
        animator.GetCurrentAnimatorStateInfo(0).length;

    yield return new WaitForSeconds(animationLength);

    Destroy(gameObject);
}

    void PlayMovementAnimation(Vector2 direction, string action)
    {
        if (direction.sqrMagnitude > 0.01f)
        {
            facingLeft = direction.x < 0;
            spriteRenderer.flipX = facingLeft;
        }

        PlayAnimation(action);
    }

    // Відтворення анімації без постійного перезапуску
    void PlayAnimation(string animationName)
{
    if (currentAnimation == animationName)
        return;

    currentAnimation = animationName;

    animator.CrossFade(animationName, 0.1f);
}
void SpawnLoot()
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
    IEnumerator SpawnLootAfterDelay()
{
    yield return new WaitForSeconds(1f);
    SpawnLoot();
}
}
}