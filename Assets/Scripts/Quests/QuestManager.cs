
using System;
using System.Collections.Generic;
using UnityEngine;

public enum QuestAnimalType
{
    Deer,
    Boar
}

[Serializable]
public class QuestDefinition
{
    public string questId;
    public string questName;

    [TextArea]
    public string description;

    public QuestAnimalType targetAnimal;

    public int requiredKills = 1;
    public int rewardGold = 10;
}

[Serializable]
public class ActiveQuest
{
    public string questId;
    public int currentKills;
    public bool completed;
}

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Послідовність квестів")]
    public List<QuestDefinition> questDefinitions =
        new List<QuestDefinition>();

    [Header("Поточний квест")]
    public ActiveQuest activeQuest;

    [Header("Прогрес ланцюжка")]
    public int currentQuestIndex = 0;

    public event Action OnQuestChanged;

    private bool chainCompleted;

    public QuestDefinition CurrentDefinition
    {
        get
        {
            if (chainCompleted)
                return null;

            if (currentQuestIndex < 0 ||
                currentQuestIndex >= questDefinitions.Count)
                return null;

            return questDefinitions[currentQuestIndex];
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void AcceptQuest(string questId)
    {
        QuestDefinition definition = CurrentDefinition;

        if (definition == null)
            return;

        if (activeQuest != null)
            return;

        if (definition.questId != questId)
            return;

        activeQuest = new ActiveQuest
        {
            questId = questId,
            currentKills = 0,
            completed = false
        };

        Debug.Log("Прийнято квест: " + definition.questName);

        OnQuestChanged?.Invoke();
    }

    public void RegisterKill(QuestAnimalType animalType)
    {
        if (activeQuest == null || activeQuest.completed)
            return;

        QuestDefinition definition =
            GetQuestDefinition(activeQuest.questId);

        if (definition == null)
            return;

        if (definition.targetAnimal != animalType)
            return;

        activeQuest.currentKills++;

        if (activeQuest.currentKills > definition.requiredKills)
            activeQuest.currentKills = definition.requiredKills;

        Debug.Log(
            definition.questName + ": " +
            activeQuest.currentKills + "/" +
            definition.requiredKills
        );

        if (activeQuest.currentKills >= definition.requiredKills)
        {
            activeQuest.completed = true;

            Debug.Log("Квест виконано: " + definition.questName);
        }

        OnQuestChanged?.Invoke();
    }

    public void ClaimReward(string questId)
    {
        if (activeQuest == null)
            return;

        if (!activeQuest.completed)
            return;

        if (activeQuest.questId != questId)
            return;

        QuestDefinition definition =
            GetQuestDefinition(questId);

        if (definition == null)
            return;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddGold(definition.rewardGold);
        }
        else
        {
            Debug.LogError("GameManager не знайдено!");
            return;
        }

        Debug.Log(
            "Отримано нагороду: " +
            definition.rewardGold + " золота"
        );

        activeQuest = null;

        currentQuestIndex++;

        if (currentQuestIndex >= questDefinitions.Count)
        {
            chainCompleted = true;
            Debug.Log("Усі квести виконано!");
        }

        OnQuestChanged?.Invoke();
    }

    public QuestDefinition GetQuestDefinition(string questId)
    {
        return questDefinitions.Find(
            quest => quest.questId == questId
        );
    }

    public bool IsQuestActive(string questId)
    {
        return activeQuest != null &&
               activeQuest.questId == questId;
    }
}
