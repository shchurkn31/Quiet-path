
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    private int localMeatCount;
    private int localHideCount;

    public int meatCount
    {
        get
        {
            return GameManager.Instance != null
                ? GameManager.Instance.meatCount
                : localMeatCount;
        }
    }

    public int hideCount
    {
        get
        {
            return GameManager.Instance != null
                ? GameManager.Instance.hideCount
                : localHideCount;
        }
    }

    public void AddMeat(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddMeat(amount);
        else
            localMeatCount += amount;

        Debug.Log("М'ясо: " + meatCount);
    }

    public void AddHide(int amount)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.AddHide(amount);
        else
            localHideCount += amount;

        Debug.Log("Шкіра: " + hideCount);
    }
}
