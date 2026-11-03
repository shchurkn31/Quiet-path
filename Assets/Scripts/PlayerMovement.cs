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
    private Vector3 currentVelocity;

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

        animator.SetBool("IsMoving", move.sqrMagnitude > 0.01f);

        if (move.sqrMagnitude > 0.01f)
            lookDirection = move;

        if (move.x < 0)
            spriteRenderer.flipX = true;
        else if (move.x > 0)
            spriteRenderer.flipX = false;

        Vector3 targetPosition = new Vector3(
            lookDirection.x,
            lookDirection.y,
            0
        ) * lookAheadDistance;

        cameraTarget.localPosition = Vector3.SmoothDamp(
            cameraTarget.localPosition,
            targetPosition,
            ref currentVelocity,
            1f / lookAheadSpeed
        );
    }

    void FixedUpdate()
    {
        rb.MovePosition(
            rb.position + move * moveSpeed * Time.fixedDeltaTime
        );
    }
}