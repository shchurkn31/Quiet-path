using UnityEngine;

public class BackpackLoot : MonoBehaviour
{
    public int meatAmount;
    public int hideAmount;

    private bool collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected)
            return;

        PlayerInventory inventory =
            other.GetComponentInParent<PlayerInventory>();

        if (inventory == null)
            return;

        if (GameManager.Instance == null)
            return;

        if (!GameManager.Instance.TryCollectBackpack(
            out int meat, out int hide))
            return;

        collected = true;

        if (meat > 0)
            inventory.AddMeat(meat);

        if (hide > 0)
            inventory.AddHide(hide);

        Debug.Log("Рюкзак підібрано!");

        Destroy(gameObject);
    }
}