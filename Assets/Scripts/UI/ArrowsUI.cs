using UnityEngine;
using TMPro;

public class ArrowsUI : MonoBehaviour
{
    public PlayerShooting playerShooting;
    public TextMeshProUGUI arrowsText;

    void Update()
    {
        arrowsText.text = "Стріли: " +
            playerShooting.currentArrows + "/" +
            playerShooting.maxArrows;
    }
}