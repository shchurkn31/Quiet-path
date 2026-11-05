using UnityEngine;

public class ArrowProjectile : MonoBehaviour
{
    private Vector2 direction;
    private float speed;

    public void Launch(Vector2 shootDirection, float arrowSpeed)
    {
        direction = shootDirection.normalized;
        speed = arrowSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, 3f);
    }

    void Update()
    {
        transform.position +=
            (Vector3)(direction * speed * Time.deltaTime);
    }
}