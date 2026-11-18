
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HuntingPause : MonoBehaviour
{
    [Header("Панель паузи")]
    public GameObject pausePanel;

    [Header("Налаштування звуку")]
    public AudioSettingsUI audioSettingsUI;

    [SerializeField] private GameObject progressWarning;

    private bool isPaused;
    private bool isLeaving;

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
        if (isLeaving)
            return;

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
        HideProgressWarning();

        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    public void ShowProgressWarning()
    {
        progressWarning.SetActive(true);
    }

    public void HideProgressWarning()
    {
        if (progressWarning != null)
            progressWarning.SetActive(false);
    }

    public void ReturnToCamp()
    {
        if (isLeaving)
            return;

        isLeaving = true;
        HideProgressWarning();

        if (GameManager.Instance != null)
            GameManager.Instance.AbandonHunt();
        else
            SceneManager.LoadScene("Camp");
    }

    public void QuitGame()
    {
        GameManager.QuitGame();
    }

    private void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
