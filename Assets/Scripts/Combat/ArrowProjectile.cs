
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Налаштування польоту")]
    public float speed = 15f;
    public float deceleration = 4f;
    public float flightTime = 2f;

    [Header("Шкода")]
    public int damage = 2;

    private Vector2 direction;
    private bool isLanded;

    private CircleCollider2D pickupCollider;
    private Rigidbody2D rb;
    private PlayerShooting owner;
    private float flightTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    }

    public void SetOwner(PlayerShooting player)
    {
        owner = player;
    }

    public void Launch(Vector2 shootDirection, float arrowSpeed)
{
    direction = shootDirection.normalized;
    speed = arrowSpeed;

    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    transform.rotation = Quaternion.Euler(0, 0, angle);

    pickupCollider = gameObject.AddComponent<CircleCollider2D>();
    pickupCollider.isTrigger = true;
    pickupCollider.radius = 0.5f;
    pickupCollider.enabled = true;

    DeerAI.NotifyShot(transform.position);
    BoarAI.NotifyShot(transform.position);
}

    private void FixedUpdate()
    {
        if (isLanded)
            return;

        flightTimer += Time.fixedDeltaTime;

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        speed = Mathf.MoveTowards(
            speed,
            0f,
            deceleration * Time.fixedDeltaTime
        );

        if (speed <= 0.1f || flightTimer >= flightTime)
        {
            Land();
        }
    }

    private void Land()
    {
        isLanded = true;
        speed = 0f;
        pickupCollider.enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
{
    if (isLanded)
    {
        PlayerShooting player = other.GetComponentInParent<PlayerShooting>();

        if (player == owner && owner != null && owner.AddArrow())
        {
            Destroy(gameObject);
        }

        if (other.GetComponentInParent<PlayerShooting>() == owner)
    return;
    }

    BoarAI boar = other.GetComponentInParent<BoarAI>();

    if (boar != null)
    {
        boar.TakeDamage(damage);
        Destroy(gameObject);
        return;
    }

    DeerAI deer = other.GetComponentInParent<DeerAI>();

    if (deer != null)
    {
        deer.TakeDamage(damage);
        Destroy(gameObject);
    }
}

}
