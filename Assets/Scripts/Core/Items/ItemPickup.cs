using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public enum ItemType
    {
        Meat,
        Hide
    }

    public ItemType itemType;
    public int amount = 1;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerInventory inventory =
            other.GetComponentInParent<PlayerInventory>();

        if (inventory == null)
            return;

        if (itemType == ItemType.Meat)
        {
            inventory.AddMeat(amount);
        }
        else if (itemType == ItemType.Hide)
        {
            inventory.AddHide(amount);
        }

        Destroy(gameObject);
    }
}