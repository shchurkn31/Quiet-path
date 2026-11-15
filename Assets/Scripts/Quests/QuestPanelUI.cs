
using UnityEngine;
using UnityEngine.UI;

public class QuestPanelUI : MonoBehaviour
{
    [Header("Панель")]
    public GameObject questsPanel;

    [Header("Контейнери")]
    public Transform availableQuestsParent;
    public Transform activeQuestsParent;

    [Header("Prefab картки")]
    public QuestItemUI questItemPrefab;

    private void Start()
    {
        if (questsPanel == null)
            questsPanel = gameObject;

        questsPanel.SetActive(false);
    }

    public void OpenPanel()
    {
        questsPanel.SetActive(true);
        RefreshQuests();
    }

    public void ClosePanel()
    {
        questsPanel.SetActive(false);
    }

    public void RefreshQuests()
    {
        if (QuestManager.Instance == null)
            return;

        ClearContainer(availableQuestsParent);
        ClearContainer(activeQuestsParent);

        foreach (QuestDefinition definition in QuestManager.Instance.questDefinitions)
        {
            if (QuestManager.Instance.IsQuestActive(definition.questId))
                continue;

            QuestItemUI item = Instantiate(
                questItemPrefab,
                availableQuestsParent
            );

            item.SetupAvailable(
                definition,
                () =>
                {
                    QuestManager.Instance.AcceptQuest(definition.questId);
                    RefreshQuests();
                }
            );
        }

        foreach (ActiveQuest activeQuest in QuestManager.Instance.activeQuests)
        {
            QuestDefinition definition =
                QuestManager.Instance.GetQuestDefinition(activeQuest.questId);

            if (definition == null)
                continue;

            QuestItemUI item = Instantiate(
                questItemPrefab,
                activeQuestsParent
            );

            item.SetupActive(
                definition,
                activeQuest,
                () =>
                {
                    QuestManager.Instance.ClaimReward(activeQuest.questId);
                    RefreshQuests();
                }
            );
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        foreach (Transform child in container)
        {
            Destroy(child.gameObject);
        }
    }
}
