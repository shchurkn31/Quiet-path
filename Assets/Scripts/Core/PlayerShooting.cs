using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerShooting : MonoBehaviour
{
    [Header("Shooting")]
    public InputAction ShootAction;
    public float cooldown = 0.7f;
    public float shotDelay = 0.3f;

    [Header("Arrows count")]
    public int maxArrows = 4;
    public int currentArrows = 4;

    [Header("Arrow")]
    public Sprite arrowSprite;
    public float arrowSpeed = 8f;
    public float arrowSize = 0.5f;
    public float spawnDistance = 0.5f;
    public float spawnOffsetY = 0.3f;

    private Animator animator;
    private Camera mainCamera;
    private PlayerController player;

    private float cooldownTimer;
    private bool isShooting;
    private SpriteRenderer spriteRenderer;

    private void Start()
{
    if (GameManager.Instance != null)
    {
        maxArrows += GameManager.Instance.purchasedArrows;
        currentArrows = maxArrows;
    }
}
    void Awake()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;
        player = GetComponent<PlayerController>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        ShootAction.Enable();
    }

    void OnDisable()
    {
        ShootAction.Disable();
    }

    void Update()
    {
        if (cooldownTimer > 0)
        {
            cooldownTimer -= Time.deltaTime;
        }

        if (ShootAction.WasPressedThisFrame() &&
    cooldownTimer <= 0 &&
    !isShooting &&
    currentArrows > 0)
{
    StartCoroutine(Shoot());
}
        if (isShooting && player.IsMoving)
        
{
    animator.ResetTrigger("Shoot");
    animator.SetBool("IsMoving", true);
    isShooting = false;
    StopAllCoroutines();
}
    if (ShootAction.WasPressedThisFrame() &&
        cooldownTimer <= 0 &&
        !isShooting &&
        currentArrows > 0)
{
    StartCoroutine(Shoot());
}
    }

    IEnumerator Shoot()
{
    isShooting = true;
    cooldownTimer = cooldown;

    animator.SetTrigger("Shoot");

    float timer = 0f;

    while (timer < shotDelay)
    {
        if (player.IsMoving)
        {
            animator.ResetTrigger("Shoot");
            animator.SetBool("IsMoving", true);

            isShooting = false;
            yield break;
        }

        timer += Time.deltaTime;
        yield return null;
    }

    SpawnArrow();

    isShooting = false;
}

    void SpawnArrow()
    {
        if (Mouse.current == null || mainCamera == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector3 mouseWorldPosition = mainCamera.ScreenToWorldPoint(
            new Vector3(
                mousePosition.x,
                mousePosition.y,
                -mainCamera.transform.position.z
            )
        );

        Vector2 direction = (
            (Vector2)mouseWorldPosition - (Vector2)transform.position
        ).normalized;
        if (direction.x < 0)
    spriteRenderer.flipX = true;
else if (direction.x > 0)
    spriteRenderer.flipX = false;

        GameObject arrow = new GameObject("Arrow");

        arrow.transform.position = transform.position +
            (Vector3)(direction * spawnDistance) +
            Vector3.up * spawnOffsetY;

        arrow.transform.localScale = Vector3.one * arrowSize;

        SpriteRenderer sr = arrow.AddComponent<SpriteRenderer>();
        sr.sprite = arrowSprite;
        sr.sortingOrder = 5;

        ArrowProjectile projectile = arrow.AddComponent<ArrowProjectile>();
        projectile.Launch(direction, arrowSpeed);
        projectile.SetOwner(this);
        currentArrows--;
    }
    public bool AddArrow()
{
    if (currentArrows >= maxArrows)
        return false;

    currentArrows++;
    return true;
} 
}