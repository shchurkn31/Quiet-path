using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    [Header("Налаштування польоту")]
    public float speed = 15f;
    public float deceleration = 4f;
    public float flightTime = 2f;

    private Vector2 direction;
    private bool isLanded;

    private CircleCollider2D pickupCollider;
    private PlayerShooting owner;
    private float flightTimer;

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
        pickupCollider.enabled = false;
    }

    void Update()
    {
        if (isLanded)
            return;

        flightTimer += Time.deltaTime;

        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);

        speed = Mathf.MoveTowards(
            speed,
            0,
            deceleration * Time.deltaTime
        );

        if (speed <= 0.1f || flightTimer >= flightTime)
        {
            Land();
        }
    }

    void Land()
    {
        isLanded = true;
        speed = 0;
        pickupCollider.enabled = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isLanded)
            return;

        PlayerShooting player = other.GetComponent<PlayerShooting>();

        if (player == owner && owner != null)
        {
            owner.AddArrow();
            Destroy(gameObject);
        }
    }
}