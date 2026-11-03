using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 3f;

    void Update()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        Vector2 movement = new Vector2(x, y).normalized;

        transform.position += (Vector3)(movement * speed * Time.deltaTime);
    }
}