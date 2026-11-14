
using UnityEngine;
using TMPro;

public class CampUI : MonoBehaviour
{
    [Header("Тексти")]
    public TMP_Text goldText;
    public TMP_Text meatText;
    public TMP_Text hideText;
    public TMP_Text shopStatusText;

    [Header("Панелі")]
    public GameObject shopPanel;

    private void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance == null)
            return;

        goldText.text = "Золото: " + GameManager.Instance.gold;
        meatText.text = "М'ясо: " + GameManager.Instance.meatCount;
        hideText.text = "Шкіра: " + GameManager.Instance.hideCount;
    }

    public void StartHunting()
    {
        GameManager.Instance.StartHunting();
    }

    public void SellResources()
    {
        GameManager.Instance.SellAllResources();
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        shopStatusText.text = "";
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    public void BuyBinoculars()
    {
        if (GameManager.Instance.hasBinoculars)
        {
            shopStatusText.text = "Бінокль вже придбано!";
            return;
        }

        if (GameManager.Instance.gold < 12)
        {
            shopStatusText.text = "Недостатньо золота!";
            return;
        }

        GameManager.Instance.BuyBinoculars();
        shopStatusText.text = "Бінокль придбано!";
    }

    public void BuyArrow()
    {
        if (GameManager.Instance.purchasedArrows >=
            GameManager.Instance.maxExtraArrows)
        {
            shopStatusText.text = "Досягнуто ліміту стріл!";
            return;
        }

        if (GameManager.Instance.BuyArrow())
        {
            shopStatusText.text = "Стрілу придбано!";
        }
        else
        {
            shopStatusText.text = "Недостатньо золота!";
        }
    }
}
