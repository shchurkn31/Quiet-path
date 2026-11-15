using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class WagonInteraction : MonoBehaviour
{
    [Header("Налаштування")]
    public TMP_Text interactionText;

    private bool playerNearby;

    private void Start()
    {
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (!playerNearby)
            return;

        if (Keyboard.current != null &&
            Keyboard.current.eKey.wasPressedThisFrame)
        {
            GameManager.Instance.ReturnToCamp();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null)
            return;

        playerNearby = true;

        if (interactionText != null)
            interactionText.gameObject.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null)
            return;

        playerNearby = false;

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }
}