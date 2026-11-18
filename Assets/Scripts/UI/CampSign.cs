using System.Collections.Generic;
using UnityEngine;

public class CampSign : MonoBehaviour
{
    [SerializeField] private GameObject directionHint;

    private readonly HashSet<Collider2D> nearbyPlayerColliders = new HashSet<Collider2D>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerHealth>() == null)
            return;

        nearbyPlayerColliders.Add(other);
        directionHint.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (nearbyPlayerColliders.Remove(other) && nearbyPlayerColliders.Count == 0)
            directionHint.SetActive(false);
    }

    private void OnDisable()
    {
        nearbyPlayerColliders.Clear();
        directionHint.SetActive(false);
    }
}
