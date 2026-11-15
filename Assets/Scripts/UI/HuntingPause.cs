
using UnityEngine;
using UnityEngine.InputSystem;

public class HuntingPause : MonoBehaviour
{
    [Header("Панель паузи")]
    public GameObject pausePanel;

    [Header("Налаштування звуку")]
    public AudioSettingsUI audioSettingsUI;

    private bool isPaused;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        Time.timeScale = 1f;
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        if (audioSettingsUI != null)
            audioSettingsUI.RefreshSettings();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
