
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    [Header("Музика")]
    public AudioSource musicSource;
    public AudioClip campMusic;
    public AudioClip huntingMusic;

    [Header("Назви сцен")]
    public string campSceneName = "Camp";
    public string huntingSceneName = "Hunting";

    private const string MusicKey = "MusicVolume";
    private const string SFXKey = "SFXVolume";

    private const string MusicParameter = "MusicVolume";
    private const string SFXParameter = "SFXVolume";

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.8f);
        SFXVolume = PlayerPrefs.GetFloat(SFXKey, 0.8f);

        ApplyMusicVolume();
        ApplySFXVolume();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip newMusic = null;

        if (sceneName == campSceneName)
            newMusic = campMusic;
        else if (sceneName == huntingSceneName)
            newMusic = huntingMusic;

        if (newMusic == null || musicSource == null)
            return;

        if (musicSource.clip == newMusic && musicSource.isPlaying)
            return;

        musicSource.Stop();
        musicSource.clip = newMusic;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        ApplyMusicVolume();

        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        ApplySFXVolume();

        PlayerPrefs.SetFloat(SFXKey, SFXVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMusicVolume()
    {
        if (audioMixer != null)
            audioMixer.SetFloat(MusicParameter, ToDecibels(MusicVolume));
    }

    private void ApplySFXVolume()
    {
        if (audioMixer != null)
            audioMixer.SetFloat(SFXParameter, ToDecibels(SFXVolume));
    }

    private float ToDecibels(float value)
    {
        return Mathf.Log10(Mathf.Max(value, 0.0001f)) * 20f;
    }
}
