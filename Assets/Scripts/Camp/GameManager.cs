using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Золото")]
    public int gold = 0;
    public int resourceSellPrice = 2;

    [Header("Ресурси")]
    public int meatCount = 0;
    public int hideCount = 0;

    [Header("Покращення")]
    public bool hasBinoculars = false;
    public int purchasedArrows = 0;
    public int maxExtraArrows = 2;

    [Header("Рюкзак")]
    public GameObject backpackPrefab;
    public float backpackLifetime = 300f;

    [HideInInspector] public int backpackMeat;
    [HideInInspector] public int backpackHide;

    private bool hasBackpack;
    private Vector3 backpackPosition;
    private float backpackExpiresAt;
    private GameObject spawnedBackpack;
    private int meatBeforeHunt;
    private int hideBeforeHunt;
    private int backpackMeatBeforeHunt;
    private int backpackHideBeforeHunt;
    private ActiveQuest questBeforeHunt;
    private int killsBeforeHunt;
    private bool questCompletedBeforeHunt;
    private bool hasHuntSnapshot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (hasBackpack && Time.time >= backpackExpiresAt)
        {
            RemoveBackpack();
            Debug.Log("Час рюкзака вичерпано!");
        }

        if (Keyboard.current == null)
            return;

        
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        hasHuntSnapshot = scene.name == "Hunting";
        if (!hasHuntSnapshot)
            return;

        meatBeforeHunt = meatCount;
        hideBeforeHunt = hideCount;
        backpackMeatBeforeHunt = hasBackpack ? backpackMeat : 0;
        backpackHideBeforeHunt = hasBackpack ? backpackHide : 0;
        questBeforeHunt = QuestManager.Instance != null ? QuestManager.Instance.activeQuest : null;
        killsBeforeHunt = questBeforeHunt?.currentKills ?? 0;
        questCompletedBeforeHunt = questBeforeHunt?.completed ?? false;
        SpawnBackpack();
    }

    public void AbandonHunt()
    {
        if (hasHuntSnapshot)
        {
            hasHuntSnapshot = false;
            meatCount = meatBeforeHunt;
            hideCount = hideBeforeHunt;

            if (!hasBackpack && Time.time < backpackExpiresAt)
            {
                backpackMeat = backpackMeatBeforeHunt;
                backpackHide = backpackHideBeforeHunt;
                hasBackpack = backpackMeat > 0 || backpackHide > 0;
            }

            if (QuestManager.Instance != null)
                QuestManager.Instance.RestoreHuntProgress(questBeforeHunt, killsBeforeHunt, questCompletedBeforeHunt);
        }

        ReturnToCamp();
    }

    public static void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void SpawnBackpack()
    {
        if (!hasBackpack || Time.time >= backpackExpiresAt)
            return;

        if (backpackPrefab == null)
        {
            Debug.LogError("Не призначено Backpack Prefab у GameManager!");
            return;
        }

        if (spawnedBackpack != null)
            Destroy(spawnedBackpack);

        spawnedBackpack = Instantiate(
            backpackPrefab,
            backpackPosition,
            Quaternion.identity
        );

        BackpackLoot loot = spawnedBackpack.GetComponent<BackpackLoot>();

        if (loot != null)
        {
            loot.meatAmount = backpackMeat;
            loot.hideAmount = backpackHide;
        }
    }

    public void PlayerDied(Vector3 deathPosition)
    {
        // Видаляємо попередній рюкзак
        if (spawnedBackpack != null)
            Destroy(spawnedBackpack);

        // Зберігаємо половину ресурсів
        backpackMeat = Mathf.FloorToInt(meatCount * 0.5f);
        backpackHide = Mathf.FloorToInt(hideCount * 0.5f);

        // Решта ресурсів втрачається
        meatCount = 0;
        hideCount = 0;

        backpackPosition = deathPosition;
        hasBackpack = backpackMeat > 0 || backpackHide > 0;

        // Починаємо новий відлік
        backpackExpiresAt = Time.time + backpackLifetime;

        Debug.Log("Створено новий рюкзак!");
        Debug.Log("М'ясо в рюкзаку: " + backpackMeat);
        Debug.Log("Шкіра в рюкзаку: " + backpackHide);
    }

    public bool TryCollectBackpack(out int meat, out int hide)
    {
        meat = 0;
        hide = 0;

        if (!hasBackpack || Time.time >= backpackExpiresAt)
            return false;

        meat = backpackMeat;
        hide = backpackHide;

        hasBackpack = false;
        backpackMeat = 0;
        backpackHide = 0;
        spawnedBackpack = null;

        return true;
    }

    private void RemoveBackpack()
    {
        hasBackpack = false;
        backpackMeat = 0;
        backpackHide = 0;

        if (spawnedBackpack != null)
            Destroy(spawnedBackpack);

        spawnedBackpack = null;
    }

    public void AddGold(int amount)
    {
        gold += amount;
        Debug.Log("Золото: " + gold);
    }

    public bool SpendGold(int amount)
    {
        if (gold < amount)
            return false;

        gold -= amount;
        Debug.Log("Золото: " + gold);
        return true;
    }

    public void AddMeat(int amount)
    {
        meatCount += amount;
    }

    public void AddHide(int amount)
    {
        hideCount += amount;
    }

    public void SellAllResources()
    {
        int totalItems = meatCount + hideCount;

        if (totalItems <= 0)
        {
            Debug.Log("Немає ресурсів для продажу!");
            return;
        }

        int earnings = totalItems * resourceSellPrice;

        meatCount = 0;
        hideCount = 0;

        AddGold(earnings);

        Debug.Log("Продано ресурсів на " + earnings + " золота!");
    }

    public void BuyBinoculars()
    {
        if (hasBinoculars)
            return;

        if (SpendGold(12))
        {
            hasBinoculars = true;
            Debug.Log("Бінокль придбано!");
        }
    }

    public bool BuyArrow()
    {
        if (purchasedArrows >= maxExtraArrows)
            return false;

        if (!SpendGold(5))
            return false;

        purchasedArrows++;
        Debug.Log("Придбано додаткову стрілу!");
        return true;
    }

    public void StartHunting()
{
    ScreenFade fade = GetComponent<ScreenFade>();

    if (fade != null)
        fade.FadeAndLoad("Hunting");
    else
        SceneManager.LoadScene("Hunting");
}

public void ReturnToCamp()
{
    ScreenFade fade = GetComponent<ScreenFade>();

    if (fade != null)
        fade.FadeAndLoad("Camp");
    else
        SceneManager.LoadScene("Camp");
}
}
