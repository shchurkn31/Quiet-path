
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
    [TextArea] public string description;

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

    [Header("Доступні квести")]
    public List<QuestDefinition> questDefinitions = new List<QuestDefinition>();

    [Header("Активні квести")]
    public List<ActiveQuest> activeQuests = new List<ActiveQuest>();

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
        QuestDefinition definition = GetQuestDefinition(questId);

        if (definition == null)
            return;

        if (IsQuestActive(questId))
            return;

        activeQuests.Add(new ActiveQuest
        {
            questId = questId,
            currentKills = 0,
            completed = false
        });

        Debug.Log("Прийнято квест: " + definition.questName);
    }

    public void RegisterKill(QuestAnimalType animalType)
    {
        foreach (ActiveQuest quest in activeQuests)
        {
            if (quest.completed)
                continue;

            QuestDefinition definition = GetQuestDefinition(quest.questId);

            if (definition == null)
                continue;

            if (definition.targetAnimal != animalType)
                continue;

            quest.currentKills++;

            Debug.Log(
                definition.questName + ": " +
                quest.currentKills + "/" + definition.requiredKills
            );

            if (quest.currentKills >= definition.requiredKills)
            {
                quest.completed = true;
                Debug.Log("Квест виконано: " + definition.questName);
            }
        }
    }

    public void ClaimReward(string questId)
    {
        ActiveQuest activeQuest = activeQuests.Find(
            quest => quest.questId == questId
        );

        if (activeQuest == null || !activeQuest.completed)
            return;

        QuestDefinition definition = GetQuestDefinition(questId);

        if (definition == null)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.AddGold(definition.rewardGold);

        Debug.Log("Отримано нагороду: " + definition.rewardGold + " золота");

        activeQuests.Remove(activeQuest);
    }

    public QuestDefinition GetQuestDefinition(string questId)
    {
        return questDefinitions.Find(
            quest => quest.questId == questId
        );
    }

    public bool IsQuestActive(string questId)
    {
        return activeQuests.Exists(
            quest => quest.questId == questId
        );
    }
}
