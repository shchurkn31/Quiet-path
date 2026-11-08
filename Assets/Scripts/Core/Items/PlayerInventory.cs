using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int meatCount = 0;
    public int hideCount = 0;

    public void AddMeat(int amount)
    {
        meatCount += amount;
        Debug.Log("М'ясо: " + meatCount);
    }

    public void AddHide(int amount)
    {
        hideCount += amount;
        Debug.Log("Шкіра: " + hideCount);
    }
}