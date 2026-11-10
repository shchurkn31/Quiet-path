using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Здоров'я")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Захист")]
    public float invulnerabilityTime = 1f;

    [Header("Смерть")]
    public string deathAnimation = "Die";
    public float deathAnimationDuration = 1.5f;

    [Header("Компоненти")]
    public Animator animator;
    public Rigidbody2D rb;
    public Collider2D[] playerColliders;
    public Behaviour[] scriptsToDisable;

    [Header("Події")]
    public UnityEvent onDeath;

    private float nextDamageTime;
    private bool isDead;

    private void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (rb == null)
            rb = GetComponent<Rigidbody2D>();
    }

    public void TakeDamage(float amount, Vector2 hitDirection)
    {
        if (isDead || Time.time < nextDamageTime)
            return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0);

        nextDamageTime = Time.time + invulnerabilityTime;

        Debug.Log("Здоров'я гравця: " + currentHealth);

        if (currentHealth <= 0)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead)
            return;

        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }

    private void Die()
    {
        isDead = true;

        if (animator != null)
            animator.SetTrigger(deathAnimation);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        foreach (Collider2D col in playerColliders)
        {
            if (col != null)
                col.enabled = false;
        }

        foreach (Behaviour script in scriptsToDisable)
        {
            if (script != null)
                script.enabled = false;
        }

        onDeath?.Invoke();

        StartCoroutine(DestroyAfterDeath());
    }

    private IEnumerator DestroyAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }
}