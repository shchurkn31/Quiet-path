
using UnityEngine;
using TMPro;

public class PlayerHealthUI : MonoBehaviour
{
    public PlayerHealth playerHealth;
    public TextMeshProUGUI healthText;

    void Update()
    {
        healthText.text = 
            playerHealth.currentHealth.ToString("F0");
            
}}
