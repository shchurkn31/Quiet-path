using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public InputAction MoveAction;
    public float moveSpeed = 3f;

    [Header("Camera")]
    public Transform cameraTarget;
    public float lookAheadDistance = 2f;
    public float lookAheadSpeed = 3f;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 move;
    private Vector2 lookDirection = Vector2.right;

    public Vector2 LookDirection => lookDirection;
    public bool IsMoving => move.sqrMagnitude > 0.01f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void OnEnable()
    {
        MoveAction.Enable();
    }

    void OnDisable()
    {
        MoveAction.Disable();
    }

    void Update()
    {
        move = MoveAction.ReadValue<Vector2>().normalized;

        animator.SetBool("IsMoving", IsMoving);

        if (IsMoving)
            lookDirection = move;

        if (move.x < 0)
            spriteRenderer.flipX = true;
        else if (move.x > 0)
            spriteRenderer.flipX = false;
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + move * moveSpeed * Time.fixedDeltaTime
        );
    }

    void LateUpdate()
    {
        Vector3 targetPosition = transform.position +
            (Vector3)(lookDirection * lookAheadDistance);

        cameraTarget.position = Vector3.Lerp(
            cameraTarget.position,
            targetPosition,
            lookAheadSpeed * Time.deltaTime
        );
    }
}